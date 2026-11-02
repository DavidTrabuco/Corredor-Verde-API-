using CorredorVerde.Application.DTO.Request;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/cruzamentos")]
    public class CruzamentoController : ControllerBase
    {
        private readonly ISemaforoService _semaforoService;
        private readonly IFalhaService _falhaService;

        public CruzamentoController(ISemaforoService semaforoService, IFalhaService falhaService)
        {
            _semaforoService = semaforoService;
            _falhaService = falhaService;
        }

        // RN07, RN13: a maquete pergunta na faixa de parada se a viatura pode atravessar
        [HttpGet("{id}/pode-atravessar")]
        public IActionResult PodeAtravessar(string id, [FromQuery, BindRequired] Eixo eixo, [FromQuery] string? viaturaId)
        {
            var resultado = _semaforoService.PodeAtravessar(id, eixo, viaturaId);
            return Ok(PodeAtravessarResponse.De(resultado));
        }

        // UC09, RN13: liga ou desliga a falha do semáforo
        [HttpPost("{id}/falha")]
        public IActionResult Falha(string id, FalhaRequest request)
        {
            _falhaService.Definir(id, request.Ativa);
            return NoContent();
        }
    }
}
