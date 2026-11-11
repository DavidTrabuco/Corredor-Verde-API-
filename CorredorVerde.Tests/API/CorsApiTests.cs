using System.Net;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.API
{
    // A maquete roda em outro endereço (Vite, porta 5173), então a API precisa liberar essa origem.
    public class CorsApiTests
    {
        [Fact(DisplayName = "UC01 · A origem da maquete (localhost:5173) é liberada")]
        [Trait("Requisito", "UC01")]
        public async Task Get_DeveLiberarAOrigem_QuandoAChamadaVemDaMaquete()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var pedido = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
            pedido.Headers.Add("Origin", "http://localhost:5173");

            // Act
            var resposta = await cliente.SendAsync(pedido);

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal("http://localhost:5173", string.Join(",", resposta.Headers.GetValues("Access-Control-Allow-Origin")));
        }

        [Fact(DisplayName = "UC01 · Outra origem não recebe a liberação")]
        [Trait("Requisito", "UC01")]
        public async Task Get_NaoDeveLiberarAOrigem_QuandoAChamadaVemDeOutroSite()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            var pedido = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
            pedido.Headers.Add("Origin", "http://site-desconhecido.com");

            // Act
            var resposta = await cliente.SendAsync(pedido);

            // Assert
            Assert.False(resposta.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }
}
