using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    public class LogApiTests
    {
        [Fact(DisplayName = "RN14 · O log começa com o evento \"Simulação pronta\"")]
        [Trait("Requisito", "RN14")]
        public async Task Listar_DeveComecarComSimulacaoPronta_QuandoASimulacaoAcabaDeSubir()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/log");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var eventos = await ApiFactory.Ler<List<EventoLogResponse>>(resposta);
            Assert.NotNull(eventos);
            Assert.Single(eventos);
            Assert.Equal(TipoEvento.Info, eventos[0].Tipo);
            Assert.StartsWith("Simulação pronta:", eventos[0].Mensagem);
        }

        [Fact(DisplayName = "RN14 · Com \"desde\", o log devolve só os eventos depois daquele número")]
        [Trait("Requisito", "RN14")]
        [Trait("Requisito", "UC10")]
        public async Task Listar_DeveDevolverSoOsNovos_QuandoInformaDesde()
        {
            // Arrange: a maquete já viu o primeiro evento; depois acontece uma chamada
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var primeiros = await ApiFactory.Ler<List<EventoLogResponse>>(await cliente.GetAsync("/api/v1/log"));
            Assert.NotNull(primeiros);
            var ultimoVisto = primeiros[primeiros.Count - 1].Seq;
            await cliente.PostAsJsonAsync("/api/v1/simulacao/controle", new { pausado = true });

            // Act
            var resposta = await cliente.GetAsync($"/api/v1/log?desde={ultimoVisto}");

            // Assert: só o evento novo, e a lista completa continua com os dois
            var novos = await ApiFactory.Ler<List<EventoLogResponse>>(resposta);
            Assert.NotNull(novos);
            Assert.Single(novos);
            Assert.Equal(TipoEvento.Api, novos[0].Tipo);
            Assert.True(novos[0].Seq > ultimoVisto);
        }

        [Theory(DisplayName = "RNF05 · O log é somente leitura: PUT e DELETE devolvem 405")]
        [Trait("Requisito", "RNF05")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task Alterar_DeveDevolver405_QuandoTentaMexerNoLog(string metodo)
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var pedido = new HttpRequestMessage(new HttpMethod(metodo), "/api/v1/log/1");

            // Act
            var resposta = await cliente.SendAsync(pedido);

            // Assert: 405, só GET permitido e o evento da tentativa fica registrado
            Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
            Assert.Equal("GET", string.Join(",", resposta.Content.Headers.Allow));
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("O log de eventos é somente leitura.", problema.Detail);
            var evento = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Contains("RNF05", evento.Tags);
            Assert.Equal($"{metodo} /api/v1/log/1 → 405: o log de eventos não pode ser alterado.", evento.Mensagem);
        }
    }
}
