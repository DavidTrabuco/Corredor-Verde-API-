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

        [Fact(DisplayName = "UC03 · Sem viatura livre do tipo pedido, a API devolve 409")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver409_QuandoNaoHaViaturaLivre()
        {
            // Arrange: as duas ambulâncias já estão em emergência (cada registro é conferido: precisa dar 201)
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C8", 1);

            // Act: terceiro pedido de ambulância
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C5", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Conflito", detalhesDoErro.Title);
            Assert.Equal("AMB-01 e AMB-02 estão ocupadas. Espere uma delas terminar.", detalhesDoErro.Detail);
        }
    }
}
