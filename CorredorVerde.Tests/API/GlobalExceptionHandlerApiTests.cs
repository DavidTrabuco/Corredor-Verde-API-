using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CorredorVerde.Tests.API
{
    // Aqui o pedido passa pela API de verdade, e o que se confere é o que o cliente (a maquete) recebe:
    // o status, o ProblemDetails em português e o aviso no log de eventos.
    public class GlobalExceptionHandlerApiTests
    {
        private const string TipoProblema = "application/problem+json";

        [Fact(DisplayName = "RNF04 · Dado inválido vira 400 com ProblemDetails em português e aparece no log")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoACriticidadeEInvalida()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 4 });

            // Assert: status, tipo do conteúdo e o texto do erro
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal(400, problema.Status);
            Assert.Equal("Requisição inválida", problema.Title);
            Assert.Equal("Criticidade 4 inválida: use 1, 2 ou 3.", problema.Detail);
            Assert.Equal("/api/v1/ocorrencias", problema.Instance);

            // Assert: a recusa também foi para o log de eventos como erro
            var evento = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Equal(TipoEvento.Erro, evento.Tipo);
            Assert.Contains("RNF04", evento.Tags);
            Assert.Equal("POST /api/v1/ocorrencias → 400: Criticidade 4 inválida: use 1, 2 ou 3.", evento.Mensagem);
        }

        [Fact(DisplayName = "RNF04 · Viatura que não existe vira 404")]
        [Trait("Requisito", "RNF04")]
        public async Task Chegada_DeveDevolver404_QuandoAViaturaNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/viaturas/XYZ-99/chegada", new { cruzamento = "C1" });

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Recurso não encontrado", problema.Title);
            Assert.Equal("Viatura XYZ-99 não existe.", problema.Detail);
        }

        [Fact(DisplayName = "UC03 · Sem viatura livre do tipo pedido, a API devolve 409")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver409_QuandoNaoHaViaturaLivre()
        {
            // Arrange: as duas ambulâncias já estão em emergência
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C8", criticidade = 1 });

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C5", criticidade = 1 });

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Conflito", problema.Title);
            Assert.Equal("AMB-01 e AMB-02 estão ocupadas. Espere uma delas terminar.", problema.Detail);
        }

        [Fact(DisplayName = "RNF04 · Erro inesperado vira 500 com mensagem genérica, sem vazar o detalhe interno")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver500SemVazarODetalhe_QuandoAcontecerUmErroInesperado()
        {
            // Arrange: o service de ocorrências é trocado por um que quebra com uma mensagem interna
            using var api = new ApiFactory(servicos =>
            {
                servicos.AddSingleton<IOcorrenciaService, OcorrenciaQueQuebra>();
            });
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });

            // Assert: o cliente só vê o texto genérico
            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            var corpo = await resposta.Content.ReadAsStringAsync();
            Assert.DoesNotContain("senha-do-banco", corpo);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Erro inesperado", problema.Title);
            Assert.Equal("Ocorreu um erro inesperado. Tente novamente mais tarde.", problema.Detail);

            // Assert: o 500 não entra no log de eventos da maquete (só as recusas 4xx entram)
            Assert.Empty(api.Cidade.Log.FindAll(e => e.Tipo == TipoEvento.Erro));
        }

        [Fact(DisplayName = "RNF04 · Rota que não existe também vira 404 em ProblemDetails")]
        [Trait("Requisito", "RNF04")]
        public async Task Get_DeveDevolver404EmProblemDetails_QuandoARotaNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/nao-existe");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        // Service falso: sempre quebra, com uma mensagem que não pode chegar ao cliente
        private class OcorrenciaQueQuebra : IOcorrenciaService
        {
            public Ocorrencia Registrar(string? tipo, string? cruzamento, int? criticidade)
            {
                throw new InvalidOperationException("falha interna com a senha-do-banco");
            }
        }
    }
}
