using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Extensao
{
    public static class ValidacaoExtensao
    {
        // Quando o [ApiController] recusa o corpo ou a query (JSON quebrado, campo obrigatório faltando, eixo inválido...),
        // a resposta sai no mesmo formato dos outros erros: ProblemDetails com o "detail" em português (RNF04).
        public static IActionResult RespostaDeErro(ActionContext contexto)
        {
            var mensagens = new List<string>();
            foreach (var (campo, entrada) in contexto.ModelState)
            {
                if (entrada is null)
                    continue;

                foreach (var erro in entrada.Errors)
                {
                    // Mensagens nossas (ErrorMessage dos DTOs) vão como estão; as do framework vêm em inglês e são trocadas
                    var nossa = erro.Exception is null
                        && !string.IsNullOrWhiteSpace(erro.ErrorMessage)
                        && !erro.ErrorMessage.StartsWith("The ", StringComparison.Ordinal)
                        && !erro.ErrorMessage.StartsWith("A non-empty", StringComparison.Ordinal);

                    var nomeCampo = campo.TrimStart('$', '.');
                    mensagens.Add(nossa
                        ? erro.ErrorMessage
                        : string.IsNullOrEmpty(nomeCampo) || nomeCampo == "request"
                            ? "Corpo da requisição ausente ou inválido."
                            : $"Valor inválido em \"{nomeCampo}\".");
                }
            }

            var detalhe = string.Join(" ", mensagens.Distinct());
            var http = contexto.HttpContext;

            var log = http.RequestServices.GetRequiredService<ILogEventosService>();
            log.Registrar(new[] { "API", "RNF04" }, $"{http.Request.Method} {http.Request.Path} → 400: {detalhe}", TipoEvento.Erro);

            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
                Detail = detalhe,
                Instance = http.Request.Path
            };
            return new BadRequestObjectResult(problema) { ContentTypes = { "application/problem+json" } };
        }
    }
}
