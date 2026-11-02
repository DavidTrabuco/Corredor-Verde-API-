using System.Text.Json;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CorredorVerde.API.Filtros
{
    // Toda chamada POST que deu certo vai para o log de eventos como tipo "Api"
    // (a maquete mostra essas linhas só quando o usuário liga "mostrar chamadas da API").
    public class RegistroChamadaFilter : IAsyncActionFilter
    {
        private readonly ILogEventosService _log;

        public RegistroChamadaFilter(ILogEventosService log)
        {
            _log = log;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate proximo)
        {
            var executado = await proximo();

            var request = contexto.HttpContext.Request;
            // Erros já vão para o log pelo GlobalExceptionHandler
            if (!HttpMethods.IsPost(request.Method) || executado.Exception is not null)
                return;

            var status = (executado.Result as IStatusCodeActionResult)?.StatusCode ?? contexto.HttpContext.Response.StatusCode;
            var corpo = contexto.ActionArguments.Values.FirstOrDefault(a => a is not null && a is not string);
            var json = corpo is null ? "" : " " + JsonSerializer.Serialize(corpo, corpo.GetType(), JsonSerializerOptions.Web);
            var caminho = request.Path.Value ?? string.Empty;

            _log.Registrar(TagsDe(caminho), $"POST {caminho}{json} → {status}", TipoEvento.Api);
        }

        private static string[] TagsDe(string caminho)
        {
            if (caminho.EndsWith("/saida") || caminho.EndsWith("/chegada"))
                return new[] { "API", "UC01" };
            if (caminho.Contains("/ocorrencias"))
                return new[] { "API", "UC03" };
            if (caminho.EndsWith("/falha"))
                return new[] { "API", "UC09" };
            return new[] { "API" };
        }
    }
}
