using CorredorVerde.Application.DTO.Request;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/viaturas")]
    public class ViaturaController : ControllerBase
    {
        private readonly IViaturaService _viaturaService;
        private readonly ISimulacaoService _simulacao;

        public ViaturaController(IViaturaService viaturaService, ISimulacaoService simulacao)
        {
            _viaturaService = viaturaService;
            _simulacao = simulacao;
        }

        [HttpGet]
        public IActionResult Listar()
        {
            return Ok(_simulacao.Viaturas());
        }

        // UC01, UC07: a viatura saiu de um cruzamento e entrou numa rua
        [HttpPost("{id}/saida")]
        public IActionResult Saida(string id, SaidaRequest request)
        {
            _viaturaService.Saida(id, request.De, request.Para);
            return NoContent();
        }

        // UC01, UC06: a viatura chegou ao centro de um cruzamento
        [HttpPost("{id}/chegada")]
        public IActionResult Chegada(string id, ChegadaRequest request)
        {
            _viaturaService.Chegada(id, request.Cruzamento);
            return NoContent();
        }
    }
}
