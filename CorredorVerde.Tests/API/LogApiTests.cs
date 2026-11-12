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
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/log");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var eventosDoLog = await ApiFactory.Ler<List<EventoLogResponse>>(respostaHttp);
            Assert.NotNull(eventosDoLog);
            Assert.Single(eventosDoLog);
            Assert.Equal(TipoEvento.Info, eventosDoLog[0].Tipo);
            Assert.StartsWith("Simulação pronta:", eventosDoLog[0].Mensagem);
        }

        [Fact(DisplayName = "RN14 · Com \"desde\", o log devolve só os eventos depois daquele número")]
        [Trait("Requisito", "RN14")]
        [Trait("Requisito", "UC10")]
        public async Task Listar_DeveDevolverSoOsNovos_QuandoInformaDesde()
        {
            // Arrange: a maquete já viu o primeiro evento; depois acontece uma chamada (o POST de pausar é conferido)
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var eventosIniciais = await ApiFactory.LerLog(clienteHttp);
            var ultimoSeqVisto = eventosIniciais[eventosIniciais.Count - 1].Seq;
            var respostaDoControle = await clienteHttp.PostAsJsonAsync("/api/v1/simulacao/controle", new { pausado = true });
            Assert.Equal(HttpStatusCode.NoContent, respostaDoControle.StatusCode);

            // Act
            var respostaHttp = await clienteHttp.GetAsync($"/api/v1/log?desde={ultimoSeqVisto}");

            // Assert: só o evento novo, depois do número informado
            var eventosNovos = await ApiFactory.Ler<List<EventoLogResponse>>(respostaHttp);
            Assert.NotNull(eventosNovos);
            Assert.Single(eventosNovos);
            Assert.Equal(TipoEvento.Api, eventosNovos[0].Tipo);
            Assert.True(eventosNovos[0].Seq > ultimoSeqVisto);
        }

        [Theory(DisplayName = "RNF05 · O log é somente leitura: PUT e DELETE devolvem 405")]
        [Trait("Requisito", "RNF05")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task Alterar_DeveDevolver405_QuandoTentaMexerNoLog(string metodoHttp)
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var requisicaoHttp = new HttpRequestMessage(new HttpMethod(metodoHttp), "/api/v1/log/1");

            // Act
            var respostaHttp = await clienteHttp.SendAsync(requisicaoHttp);

            // Assert: 405, só GET permitido e o evento da tentativa fica registrado
            Assert.Equal(HttpStatusCode.MethodNotAllowed, respostaHttp.StatusCode);
            Assert.Equal("GET", string.Join(",", respostaHttp.Content.Headers.Allow));
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("O log de eventos é somente leitura.", detalhesDoErro.Detail);
            var eventoDaTentativa = api.Cidade.Log[api.Cidade.Log.Count - 1];
            Assert.Contains("RNF05", eventoDaTentativa.Tags);
            Assert.Equal($"{metodoHttp} /api/v1/log/1 → 405: o log de eventos não pode ser alterado.", eventoDaTentativa.Mensagem);
        }
    }
}
