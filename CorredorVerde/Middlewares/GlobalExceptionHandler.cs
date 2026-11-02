using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Middlewares
{
    // Ponto único que transforma QUALQUER exceção não tratada em ProblemDetails.
    // Service só lança; quem decide o status HTTP é esta classe.
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetails;
        private readonly ILogEventosService _logEventos;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogEventosService logEventos, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetails = problemDetails;
            _logEventos = logEventos;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
        {
            var (status, titulo, detalhe) = ex switch
            {
                ValidacaoException      => (StatusCodes.Status400BadRequest, "Requisição inválida", ex.Message),
                NaoEncontradoException  => (StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message),
                ConflitoException       => (StatusCodes.Status409Conflict, "Conflito", ex.Message),
                RegraDeNegocioException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", ex.Message),

                // Qualquer outra coisa é bug: mensagem genérica, NUNCA ex.Message (pode vazar detalhes internos)
                _ => (StatusCodes.Status500InternalServerError, "Erro inesperado", "Ocorreu um erro inesperado. Tente novamente mais tarde.")
            };

            // Só o 500 é erro de verdade. 4xx de domínio é comportamento esperado, não vai como Error.
            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", http.Request.Method, http.Request.Path);
            }
            else
            {
                _logger.LogInformation("{Excecao} em {Caminho}: {Mensagem}", ex.GetType().Name, http.Request.Path, ex.Message);

                // RNF04: a recusa também aparece no log de eventos da maquete
                _logEventos.Registrar(new[] { "API", "RNF04" },
                    $"{http.Request.Method} {http.Request.Path} → {status}: {ex.Message}",
                    TipoEvento.Erro);
            }

            http.Response.StatusCode = status;

            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                Exception = ex,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = titulo,
                    Detail = detalhe,
                    Instance = http.Request.Path
                }
            });
        }
    }
}
