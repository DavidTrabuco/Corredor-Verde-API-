using CorredorVerde.Application.DTO.Request;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.API.Controllers
{
    [ApiController]
    [Route("api/v1/ocorrencias")]
    public class OcorrenciaController : ControllerBase
    {
        private readonly IOcorrenciaService _ocorrenciaService;
        private readonly ISimulacaoService _simulacao;

        public OcorrenciaController(IOcorrenciaService ocorrenciaService, ISimulacaoService simulacao)
        {
            _ocorrenciaService = ocorrenciaService;
            _simulacao = simulacao;
        }

        [HttpGet]
        public IActionResult Listar()
        {
            return Ok(_simulacao.Ocorrencias());
        }

        // UC03: 201 com a viatura escolhida; 400 com dados inválidos; 409 sem viatura livre
        [HttpPost]
        public IActionResult Registrar(CriarOcorrenciaRequest request)
        {
            var ocorrencia = _ocorrenciaService.Registrar(request.Tipo, request.Cruzamento, request.Criticidade);
            return StatusCode(StatusCodes.Status201Created, OcorrenciaResponse.De(ocorrencia));
        }
    }
}
