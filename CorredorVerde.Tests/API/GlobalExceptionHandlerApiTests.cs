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

        // Mensagem interna do erro simulado: o teste garante que ela nunca chega ao cliente
        private const string SegredoInterno = "senha-do-banco";

        [Fact(DisplayName = "RNF04 · Dado inválido vira 400 com ProblemDetails em português e aparece no log")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver400_QuandoACriticidadeEInvalida()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 4 });

            // Assert: status, tipo do conteúdo e o texto do erro
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            Assert.Equal(TipoProblema, respostaHttp.Content.Headers.ContentType?.MediaType);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal(400, detalhesDoErro.Status);
            Assert.Equal("Requisição inválida", detalhesDoErro.Title);
            Assert.Equal("Criticidade 4 inválida: use 1, 2 ou 3.", detalhesDoErro.Detail);
            Assert.Equal("/api/v1/ocorrencias", detalhesDoErro.Instance);

            // Assert: a recusa também foi para o log de eventos como erro
            var eventoDaRecusa = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Equal(TipoEvento.Erro, eventoDaRecusa.Tipo);
            Assert.Contains("RNF04", eventoDaRecusa.Tags);
            Assert.Equal("POST /api/v1/ocorrencias → 400: Criticidade 4 inválida: use 1, 2 ou 3.", eventoDaRecusa.Mensagem);
        }

        [Fact(DisplayName = "RNF04 · Viatura que não existe vira 404")]
        [Trait("Requisito", "RNF04")]
        public async Task Chegada_DeveDevolver404_QuandoAViaturaNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/XYZ-99/chegada", new { cruzamento = "C1" });

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Recurso não encontrado", detalhesDoErro.Title);
            Assert.Equal("Viatura XYZ-99 não existe.", detalhesDoErro.Detail);
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

        [Fact(DisplayName = "RNF04 · Erro inesperado vira 500 com mensagem genérica, sem vazar o detalhe interno")]
        [Trait("Requisito", "RNF04")]
        public async Task Registrar_DeveDevolver500SemVazarODetalhe_QuandoAcontecerUmErroInesperado()
        {
            // Arrange: o service de ocorrências é trocado por um que quebra com uma mensagem interna
            using var api = new ApiFactory(servicos =>
            {
                servicos.AddSingleton<IOcorrenciaService, OcorrenciaQueQuebra>();
            });
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });

            // Assert: o cliente só vê o texto genérico
            Assert.Equal(HttpStatusCode.InternalServerError, respostaHttp.StatusCode);
            var corpoDaResposta = await respostaHttp.Content.ReadAsStringAsync();
            Assert.DoesNotContain(SegredoInterno, corpoDaResposta);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Erro inesperado", detalhesDoErro.Title);
            Assert.Equal("Ocorreu um erro inesperado. Tente novamente mais tarde.", detalhesDoErro.Detail);

            // Assert: o 500 não entra no log de eventos da maquete (só as recusas 4xx entram)
            Assert.Empty(api.Cidade.Log.FindAll(e => e.Tipo == TipoEvento.Erro));
        }

        [Fact(DisplayName = "RNF04 · Rota que não existe também vira 404 em ProblemDetails")]
        [Trait("Requisito", "RNF04")]
        public async Task Get_DeveDevolver404EmProblemDetails_QuandoARotaNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/nao-existe");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, respostaHttp.StatusCode);
            Assert.Equal(TipoProblema, respostaHttp.Content.Headers.ContentType?.MediaType);
        }

        // Service falso: sempre quebra, com uma mensagem que não pode chegar ao cliente
        private class OcorrenciaQueQuebra : IOcorrenciaService
        {
            public Ocorrencia Registrar(string? tipo, string? cruzamento, int? criticidade)
            {
                throw new InvalidOperationException($"falha interna com a {SegredoInterno}");
            }
        }
    }
}
