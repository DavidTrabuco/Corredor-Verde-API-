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
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            var ocorrencia = await ApiFactory.Ler<OcorrenciaResponse>(resposta);
            Assert.NotNull(ocorrencia);
            Assert.Equal("OC-001", ocorrencia.Id);
            Assert.Equal("AMB-01", ocorrencia.ViaturaId);
            Assert.Equal("C15", ocorrencia.Cruzamento);
            Assert.Equal(StatusOcorrencia.ACaminho, ocorrencia.Status);
            Assert.Equal("Hospital Sul", ocorrencia.Hospital);
        }

        [Fact(DisplayName = "UC03 · A chamada que deu certo vai para o log de eventos como tipo Api")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RN14")]
        public async Task Registrar_DeveRegistrarAChamadaNoLog_QuandoDaCerto()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Bombeiros", cruzamento = "C15", criticidade = 2 });

            // Assert: o último evento é a chamada, com as tags da API e do UC03
            var evento = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Equal(TipoEvento.Api, evento.Tipo);
            Assert.Contains("UC03", evento.Tags);
            Assert.Equal("POST /api/v1/ocorrencias {\"tipo\":\"Bombeiros\",\"cruzamento\":\"C15\",\"criticidade\":2} → 201", evento.Mensagem);
        }

        [Fact(DisplayName = "UC03 · A lista de ocorrências mostra o que foi registrado")]
        [Trait("Requisito", "UC03")]
        public async Task Listar_DeveMostrarAsOcorrencias_QuandoJaHaRegistros()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });

            // Act
            var resposta = await cliente.GetAsync("/api/v1/ocorrencias");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var lista = await ApiFactory.Ler<List<OcorrenciaResponse>>(resposta);
            Assert.NotNull(lista);
            Assert.Single(lista);
            Assert.Equal("OC-001", lista[0].Id);
        }

        [Fact(DisplayName = "RNF04 · Cruzamento que não existe vira 400 e nenhuma ocorrência é criada")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C99", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Cruzamento C99 não existe na malha.", problema.Detail);
            Assert.Empty(api.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RNF04 · Corpo com JSON quebrado vira 400 em português")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoOJsonEstaQuebrado()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var corpo = new StringContent("{ isto não é json", System.Text.Encoding.UTF8, "application/json");

            // Act
            var resposta = await cliente.PostAsync("/api/v1/ocorrencias", corpo);

            // Assert: 400 em ProblemDetails, com o título em português e o aviso no log de eventos.
            // O texto de "detail" não é conferido aqui porque hoje sai em inglês (achado do code review).
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Requisição inválida", problema.Title);
            var evento = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Contains("RNF04", evento.Tags);
        }
    }
}
