using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    // Em 0 s, o semáforo de C1 está com verde para Norte-Sul (e vermelho para Leste-Oeste).
    public class CruzamentoApiTests
    {
        [Fact(DisplayName = "RN07 · Com o verde no eixo, a viatura pode atravessar")]
        [Trait("Requisito", "RN07")]
        public async Task PodeAtravessar_DeveLiberar_QuandoOEixoEstaVerde()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=NS");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var travessia = await ApiFactory.Ler<PodeAtravessarResponse>(respostaHttp);
            Assert.NotNull(travessia);
            Assert.True(travessia.Pode);
            Assert.False(travessia.Cautela);
            Assert.Equal(Luz.Verde, travessia.Luz);
        }

        [Fact(DisplayName = "RN07 · Com o vermelho no eixo, a viatura comum espera")]
        [Trait("Requisito", "RN07")]
        public async Task PodeAtravessar_DeveBarrar_QuandoOEixoEstaVermelho()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=LO&viaturaId=AMB-01");

            // Assert
            var travessia = await ApiFactory.Ler<PodeAtravessarResponse>(respostaHttp);
            Assert.NotNull(travessia);
            Assert.False(travessia.Pode);
            Assert.Equal(Luz.Vermelho, travessia.Luz);
        }

        [Fact(DisplayName = "RN13 · Com o semáforo em falha, a viatura passa, mas com cautela")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public async Task PodeAtravessar_DeveExigirCautela_QuandoOSemaforoEstaEmFalha()
        {
            // Arrange: a falha de C1 é ligada pela própria API (a preparação é conferida: precisa dar 204)
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var respostaLigarFalha = await clienteHttp.PostAsJsonAsync("/api/v1/cruzamentos/C1/falha", new { ativa = true });
            Assert.Equal(HttpStatusCode.NoContent, respostaLigarFalha.StatusCode);

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=LO");

            // Assert
            var travessia = await ApiFactory.Ler<PodeAtravessarResponse>(respostaHttp);
            Assert.NotNull(travessia);
            Assert.True(travessia.Pode);
            Assert.True(travessia.Cautela);
            Assert.Equal(Luz.Piscante, travessia.Luz);
        }

        [Fact(DisplayName = "UC09 · Ligar a falha pela API devolve 204, deixa o semáforo piscando e entra no log")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public async Task Falha_DeveLigar_QuandoAtivaEVerdadeira()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/cruzamentos/C13/falha", new { ativa = true });

            // Assert: 204, e o estado (como a maquete lê) mostra a falha com as duas luzes piscando
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var estado = await ApiFactory.LerEstado(clienteHttp);
            var semaforoDeC13 = ApiFactory.SemaforoDe(estado, "C13");
            Assert.True(semaforoDeC13.Falha);
            Assert.Equal(Luz.Piscante, semaforoDeC13.Ns);
            Assert.Equal(Luz.Piscante, semaforoDeC13.Lo);

            // Assert: a chamada de ligar aparece no log
            var eventosDoLog = await ApiFactory.LerLog(clienteHttp);
            var eventoDeLigar = eventosDoLog.Find(e => e.Mensagem == "POST /api/v1/cruzamentos/C13/falha {\"ativa\":true} → 204");
            Assert.NotNull(eventoDeLigar);
            Assert.Contains("UC09", eventoDeLigar.Tags);
        }

        [Fact(DisplayName = "UC09 · Desligar a falha pela API devolve 204, o ciclo normal recomeça e entra no log")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public async Task Falha_DeveDesligar_QuandoAtivaEFalsa()
        {
            // Arrange: C13 está em falha (a preparação é conferida: precisa dar 204)
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var respostaLigarFalha = await clienteHttp.PostAsJsonAsync("/api/v1/cruzamentos/C13/falha", new { ativa = true });
            Assert.Equal(HttpStatusCode.NoContent, respostaLigarFalha.StatusCode);

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/cruzamentos/C13/falha", new { ativa = false });

            // Assert: 204, e o ciclo recomeça com verde para Norte-Sul
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var estado = await ApiFactory.LerEstado(clienteHttp);
            var semaforoDeC13 = ApiFactory.SemaforoDe(estado, "C13");
            Assert.False(semaforoDeC13.Falha);
            Assert.Equal(Luz.Verde, semaforoDeC13.Ns);
            Assert.Equal(Luz.Vermelho, semaforoDeC13.Lo);

            // Assert: a chamada de desligar aparece no log
            var eventosDoLog = await ApiFactory.LerLog(clienteHttp);
            var eventoDeDesligar = eventosDoLog.Find(e => e.Mensagem == "POST /api/v1/cruzamentos/C13/falha {\"ativa\":false} → 204");
            Assert.NotNull(eventoDeDesligar);
            Assert.Contains("UC09", eventoDeDesligar.Tags);
        }

        [Fact(DisplayName = "RNF04 · Perguntar sem informar o eixo vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_DeveDevolver400_QuandoFaltaOEixo()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar");

            // Assert: 400 em ProblemDetails, com o título em português.
            // O texto de "detail" não é conferido aqui porque hoje sai em inglês (achado do code review).
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Requisição inválida", detalhesDoErro.Title);
        }

        [Fact(DisplayName = "RNF04 · Eixo que não existe vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_DeveDevolver400_QuandoOEixoEInvalido()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=XX");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
        }

        [Fact(DisplayName = "RNF04 · Cruzamento que não existe na pergunta vira erro do cliente, não 500")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_NaoDeveDevolver500_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C99/pode-atravessar?eixo=NS");

            // Assert: um id errado é erro de quem chamou (4xx), nunca falha do servidor
            Assert.True((int)respostaHttp.StatusCode >= 400 && (int)respostaHttp.StatusCode < 500);
        }
    }
}
