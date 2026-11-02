using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/malha")]
    public class MalhaController : ControllerBase
    {
        private readonly ISimulacaoService _simulacao;

        public MalhaController(ISimulacaoService simulacao)
        {
            _simulacao = simulacao;
        }

        // Cruzamentos, hospitais e bases. A maquete chama uma vez, ao abrir.
        [HttpGet]
        public IActionResult Obter()
        {
            return Ok(_simulacao.Malha());
        }
    }
}
