using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/log")]
    public class LogController : ControllerBase
    {
        private readonly ILogEventosService _logService;

        public LogController(ILogEventosService logService)
        {
            _logService = logService;
        }

        // UC10, RN14: com "desde", só os eventos depois desse número; sem ele, os 300 mais recentes
        [HttpGet]
        public IActionResult Listar([FromQuery] int? desde)
        {
            return Ok(_logService.Listar(desde).Select(EventoLogResponse.De));
        }

        // RNF05: o log não pode ser editado nem apagado
        [HttpPut("{seq}")]
        [HttpDelete("{seq}")]
        public IActionResult Alterar(string seq)
        {
            var metodo = Request.Method;
            _logService.Registrar(new[] { "API", "RNF05" },
                $"{metodo} {Request.Path} → 405: o log de eventos não pode ser alterado.",
                TipoEvento.Api);

            Response.Headers.Allow = "GET";
            return Problem(
                statusCode: StatusCodes.Status405MethodNotAllowed,
                title: "Método não permitido",
                detail: "O log de eventos é somente leitura.");
        }
    }
}
