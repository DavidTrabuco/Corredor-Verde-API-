using System.Net;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    public class LogApiTests
    {
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
