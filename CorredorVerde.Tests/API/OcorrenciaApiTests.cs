using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    public class OcorrenciaApiTests
    {
        [Fact(DisplayName = "UC03 · Registrar uma emergência devolve 201 com a viatura escolhida e o hospital")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RN04")]
        public async Task Registrar_DeveDevolver201_QuandoOsDadosSaoValidos()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.Created, respostaHttp.StatusCode);
            var ocorrenciaRegistrada = await ApiFactory.Ler<OcorrenciaResponse>(respostaHttp);
            Assert.NotNull(ocorrenciaRegistrada);
            Assert.Equal("OC-001", ocorrenciaRegistrada.Id);
            Assert.Equal("AMB-01", ocorrenciaRegistrada.ViaturaId);
            Assert.Equal("C15", ocorrenciaRegistrada.Cruzamento);
            Assert.Equal(StatusOcorrencia.ACaminho, ocorrenciaRegistrada.Status);
            Assert.Equal("Hospital Sul", ocorrenciaRegistrada.Hospital);
        }

        [Fact(DisplayName = "UC03 · A chamada que deu certo vai para o log de eventos como tipo Api")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RN14")]
        public async Task Registrar_DeveRegistrarAChamadaNoLog_QuandoDaCerto()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Bombeiros", "C15", 2);

            // Assert: o último evento é a chamada, com as tags da API e do UC03
            var eventoDaChamada = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Equal(TipoEvento.Api, eventoDaChamada.Tipo);
            Assert.Contains("UC03", eventoDaChamada.Tags);
            Assert.Equal("POST /api/v1/ocorrencias {\"tipo\":\"Bombeiros\",\"cruzamento\":\"C15\",\"criticidade\":2} → 201", eventoDaChamada.Mensagem);
        }

        [Fact(DisplayName = "UC03 · A lista de ocorrências mostra o que foi registrado")]
        [Trait("Requisito", "UC03")]
        public async Task Listar_DeveMostrarAsOcorrencias_QuandoJaHaRegistros()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/ocorrencias");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var ocorrenciasListadas = await ApiFactory.Ler<List<OcorrenciaResponse>>(respostaHttp);
            Assert.NotNull(ocorrenciasListadas);
            Assert.Single(ocorrenciasListadas);
            Assert.Equal("OC-001", ocorrenciasListadas[0].Id);
        }

        [Fact(DisplayName = "RNF04 · Cruzamento que não existe vira 400 e nenhuma ocorrência é criada")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C99", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Cruzamento C99 não existe na malha.", detalhesDoErro.Detail);
            Assert.Empty(api.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RNF04 · Corpo com JSON quebrado vira 400 em português")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoOJsonEstaQuebrado()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var corpoComJsonQuebrado = new StringContent("{ isto não é json", System.Text.Encoding.UTF8, "application/json");

            // Act
            var respostaHttp = await clienteHttp.PostAsync("/api/v1/ocorrencias", corpoComJsonQuebrado);

            // Assert: 400 em ProblemDetails, com o título em português e o aviso no log de eventos.
            // O texto de "detail" não é conferido aqui porque hoje sai em inglês (achado do code review).
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            Assert.Equal("application/problem+json", respostaHttp.Content.Headers.ContentType?.MediaType);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Requisição inválida", detalhesDoErro.Title);
            var eventoDaRecusa = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Contains("RNF04", eventoDaRecusa.Tags);
        }
    }
}
