using CorredorVerde.Application.DTO.Request;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/simulacao")]
    public class SimulacaoController : ControllerBase
    {
        private readonly ISimulacaoService _simulacao;

        public SimulacaoController(ISimulacaoService simulacao)
        {
            _simulacao = simulacao;
        }

        // Botões Pausar e Velocidade (1×, 2×, 4×)
        [HttpPost("controle")]
        public IActionResult Controlar(ControleSimulacaoRequest request)
        {
            _simulacao.Controlar(request.Pausado, request.Velocidade);
            return NoContent();
        }

        // Botão Reiniciar: tudo volta ao estado inicial
        [HttpPost("reiniciar")]
        public IActionResult Reiniciar()
        {
            _simulacao.Reiniciar();
            return NoContent();
        }
    }
}
