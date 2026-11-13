using System.Net;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.API
{
    // O polling que a maquete faz a cada 250 ms: uma resposta só com semáforos, viaturas e ocorrências.
    public class SimulacaoApiTests
    {
        [Fact(DisplayName = "UC01 · O polling do estado traz semáforos, viaturas e ocorrências numa resposta só")]
        [Trait("Requisito", "UC01")]
        [Trait("Requisito", "RNF08")]
        public async Task Estado_DeveTrazerTudoNumaResposta_QuandoAMaquetePergunta()
        {
            // Arrange: com uma emergência andando (o registro é conferido: precisa dar 201) e o relógio em 3 s
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);
            api.Avancar(3.0);

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/estado");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var estado = await ApiFactory.Ler<EstadoResponse>(respostaHttp);
            Assert.NotNull(estado);
            Assert.Equal(3.0, estado.TempoSeg, precision: 1);
            Assert.Equal(20, estado.Semaforos.Count);
            Assert.Equal(4, estado.Viaturas.Count);
            Assert.Single(estado.Ocorrencias);
        }
    }
}
