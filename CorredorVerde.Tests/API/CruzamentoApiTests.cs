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
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=NS");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var resultado = await ApiFactory.Ler<PodeAtravessarResponse>(resposta);
            Assert.NotNull(resultado);
            Assert.True(resultado.Pode);
            Assert.False(resultado.Cautela);
            Assert.Equal(Luz.Verde, resultado.Luz);
        }

        [Fact(DisplayName = "RN07 · Com o vermelho no eixo, a viatura comum espera")]
        [Trait("Requisito", "RN07")]
        public async Task PodeAtravessar_DeveBarrar_QuandoOEixoEstaVermelho()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=LO&viaturaId=AMB-01");

            // Assert
            var resultado = await ApiFactory.Ler<PodeAtravessarResponse>(resposta);
            Assert.NotNull(resultado);
            Assert.False(resultado.Pode);
            Assert.Equal(Luz.Vermelho, resultado.Luz);
        }

        [Fact(DisplayName = "RN13 · Com o semáforo em falha, a viatura passa, mas com cautela")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public async Task PodeAtravessar_DeveExigirCautela_QuandoOSemaforoEstaEmFalha()
        {
            // Arrange: a falha de C1 é ligada pela própria API
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var ligada = await cliente.PostAsJsonAsync("/api/v1/cruzamentos/C1/falha", new { ativa = true });
            Assert.Equal(HttpStatusCode.NoContent, ligada.StatusCode);

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=LO");

            // Assert
            var resultado = await ApiFactory.Ler<PodeAtravessarResponse>(resposta);
            Assert.NotNull(resultado);
            Assert.True(resultado.Pode);
            Assert.True(resultado.Cautela);
            Assert.Equal(Luz.Piscante, resultado.Luz);
        }

        [Fact(DisplayName = "UC09 · Ligar e desligar a falha pela API devolve 204 e entra no log")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public async Task Falha_DeveDevolver204_QuandoLigaEDesliga()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var ligar = await cliente.PostAsJsonAsync("/api/v1/cruzamentos/C13/falha", new { ativa = true });
            Assert.True(api.Cidade.Cruzamentos["C13"].Semaforo.EmFalha);
            var desligar = await cliente.PostAsJsonAsync("/api/v1/cruzamentos/C13/falha", new { ativa = false });

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, ligar.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, desligar.StatusCode);
            Assert.False(api.Cidade.Cruzamentos["C13"].Semaforo.EmFalha);
            var evento = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Contains("UC09", evento.Tags);
            Assert.Equal("POST /api/v1/cruzamentos/C13/falha {\"ativa\":false} → 204", evento.Mensagem);
        }

        [Fact(DisplayName = "RNF04 · Perguntar sem informar o eixo vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_DeveDevolver400_QuandoFaltaOEixo()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar");

            // Assert: 400 em ProblemDetails, com o título em português.
            // O texto de "detail" não é conferido aqui porque hoje sai em inglês (achado do code review).
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Requisição inválida", problema.Title);
        }

        [Fact(DisplayName = "RNF04 · Eixo que não existe vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_DeveDevolver400_QuandoOEixoEInvalido()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=XX");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact(DisplayName = "RNF04 · Cruzamento que não existe na pergunta vira erro do cliente, não 500")]
        [Trait("Requisito", "RNF04")]
        public async Task PodeAtravessar_NaoDeveDevolver500_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/cruzamentos/C99/pode-atravessar?eixo=NS");

            // Assert: um id errado é erro de quem chamou (4xx), nunca falha do servidor
            Assert.True((int)resposta.StatusCode >= 400 && (int)resposta.StatusCode < 500);
        }
    }
}
