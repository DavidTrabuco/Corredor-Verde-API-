using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class EstadoController : ControllerBase
    {
        private readonly ISimulacaoService _simulacao;

        public EstadoController(ISimulacaoService simulacao)
        {
            _simulacao = simulacao;
        }

        // Polling principal da maquete, a cada 250 ms (RNF08): semáforos, viaturas e ocorrências numa resposta só
        [HttpGet("estado")]
        public IActionResult Estado()
        {
            return Ok(_simulacao.Estado());
        }

        [HttpGet("semaforos")]
        public IActionResult Semaforos()
        {
            return Ok(_simulacao.Semaforos());
        }
    }
}
