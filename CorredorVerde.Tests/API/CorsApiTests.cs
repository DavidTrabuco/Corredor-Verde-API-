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
            // Arrange: uma requisição GET ao estado, vinda da origem da maquete
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var requisicaoHttp = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
            requisicaoHttp.Headers.Add("Origin", "http://localhost:5173");

            // Act
            var respostaHttp = await clienteHttp.SendAsync(requisicaoHttp);

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            Assert.Equal("http://localhost:5173", string.Join(",", respostaHttp.Headers.GetValues("Access-Control-Allow-Origin")));
        }

        [Fact(DisplayName = "UC01 · Outra origem não recebe a liberação")]
        [Trait("Requisito", "UC01")]
        public async Task Get_NaoDeveLiberarAOrigem_QuandoAChamadaVemDeOutroSite()
        {
            // Arrange: uma requisição GET ao estado, vinda de um site desconhecido
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var requisicaoHttp = new HttpRequestMessage(HttpMethod.Get, "/api/v1/estado");
            requisicaoHttp.Headers.Add("Origin", "http://site-desconhecido.com");

            // Act
            var respostaHttp = await clienteHttp.SendAsync(requisicaoHttp);

            // Assert
            Assert.False(respostaHttp.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }
}
