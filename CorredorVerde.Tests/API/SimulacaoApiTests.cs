using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    // Os endpoints que a maquete usa: ler a malha uma vez, fazer o polling do estado e usar os botões.
    // O formato exato do JSON (nomes dos campos e dos enums) é conferido no ContratoJsonApiTests.
    public class SimulacaoApiTests
    {
        [Fact(DisplayName = "UC01 · A maquete lê a malha ao abrir: grade 5 x 4 com hospitais e bases")]
        [Trait("Requisito", "UC01")]
        public async Task Malha_DeveEntregarAGrade_QuandoAMaquetePede()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/malha");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var malha = await ApiFactory.Ler<MalhaResponse>(respostaHttp);
            Assert.NotNull(malha);
            Assert.Equal(5, malha.Colunas);
            Assert.Equal(4, malha.Linhas);
            Assert.Equal(20, malha.Cruzamentos.Count);
            Assert.Equal(2, malha.Hospitais.Count);
            Assert.Equal(2, malha.Bases.Count);
        }

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

        [Fact(DisplayName = "UC01 · A lista de semáforos traz os 20 cruzamentos")]
        [Trait("Requisito", "UC01")]
        public async Task Semaforos_DeveTrazerOsVinteCruzamentos_QuandoAMaquetePede()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/semaforos");

            // Assert
            var semaforos = await ApiFactory.Ler<List<SemaforoResponse>>(respostaHttp);
            Assert.NotNull(semaforos);
            Assert.Equal(20, semaforos.Count);
        }

        [Fact(DisplayName = "UC10 · Pausar e mudar a velocidade pela API devolve 204 e vale no estado")]
        [Trait("Requisito", "UC10")]
        public async Task Controle_DeveDevolver204_QuandoPausaEMudaAVelocidade()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/simulacao/controle", new { pausado = true, velocidade = 2 });

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var estado = await ApiFactory.LerEstado(clienteHttp);
            Assert.True(estado.Pausado);
            Assert.Equal(2, estado.Velocidade);
        }

        [Fact(DisplayName = "UC10 · Velocidade que não é 1, 2 ou 4 vira 400")]
        [Trait("Requisito", "UC10")]
        [Trait("Requisito", "RNF04")]
        public async Task Controle_DeveDevolver400_QuandoAVelocidadeEInvalida()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/simulacao/controle", new { velocidade = 3 });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Velocidade 3 inválida: use 1, 2 ou 4.", detalhesDoErro.Detail);
        }

        [Fact(DisplayName = "UC10 · Reiniciar pela API devolve 204, apaga as ocorrências e põe as quatro viaturas de volta")]
        [Trait("Requisito", "UC10")]
        public async Task Reiniciar_DeveDevolver204_QuandoHaEmergenciaEmAndamento()
        {
            // Arrange: uma emergência andando (o registro é conferido) e o relógio em 2 s
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);
            api.Avancar(2.0);

            // Arrange: antes de reiniciar, confirma que havia mesmo uma ocorrência e a AMB-01 em emergência
            var estadoAntes = await ApiFactory.LerEstado(clienteHttp);
            Assert.Single(estadoAntes.Ocorrencias);
            Assert.Equal(ModoViatura.Emergencia, ApiFactory.ViaturaDe(estadoAntes, "AMB-01").Modo);
            Assert.True(estadoAntes.TempoSeg > 0);

            // Act
            var respostaHttp = await clienteHttp.PostAsync("/api/v1/simulacao/reiniciar", null);

            // Assert: 204, relógio zerado, sem ocorrências e cada viatura no modo do seu papel
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var estadoDepois = await ApiFactory.LerEstado(clienteHttp);
            Assert.Equal(0.0, estadoDepois.TempoSeg);
            Assert.Empty(estadoDepois.Ocorrencias);
            Assert.Equal(4, estadoDepois.Viaturas.Count);
            Assert.Equal(ModoViatura.Patrulha, ApiFactory.ViaturaDe(estadoDepois, "AMB-01").Modo);
            Assert.Equal(ModoViatura.Reserva, ApiFactory.ViaturaDe(estadoDepois, "AMB-02").Modo);
            Assert.Equal(ModoViatura.Patrulha, ApiFactory.ViaturaDe(estadoDepois, "BOM-01").Modo);
            Assert.Equal(ModoViatura.Reserva, ApiFactory.ViaturaDe(estadoDepois, "BOM-02").Modo);
        }
    }
}
