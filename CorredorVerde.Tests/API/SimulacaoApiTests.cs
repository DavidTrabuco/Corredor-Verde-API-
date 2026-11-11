using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    // Os endpoints que a maquete usa: ler a malha uma vez, fazer o polling do estado e usar os botões.
    public class SimulacaoApiTests
    {
        [Fact(DisplayName = "UC01 · A maquete lê a malha ao abrir: grade 5 x 4 com hospitais e bases")]
        [Trait("Requisito", "UC01")]
        public async Task Malha_DeveEntregarAGrade_QuandoAMaquetePede()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/malha");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var malha = await ApiFactory.Ler<MalhaResponse>(resposta);
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
            // Arrange: com uma emergência andando e o relógio em 3 s
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });
            api.Avancar(3.0);

            // Act
            var resposta = await cliente.GetAsync("/api/v1/estado");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var estado = await ApiFactory.Ler<EstadoResponse>(resposta);
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
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/semaforos");

            // Assert
            var semaforos = await ApiFactory.Ler<List<SemaforoResponse>>(resposta);
            Assert.NotNull(semaforos);
            Assert.Equal(20, semaforos.Count);
        }

        [Fact(DisplayName = "UC10 · Pausar e mudar a velocidade pela API devolve 204 e vale no estado")]
        [Trait("Requisito", "UC10")]
        public async Task Controle_DeveDevolver204_QuandoPausaEMudaAVelocidade()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/simulacao/controle", new { pausado = true, velocidade = 2 });

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            var estado = await ApiFactory.Ler<EstadoResponse>(await cliente.GetAsync("/api/v1/estado"));
            Assert.NotNull(estado);
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
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/simulacao/controle", new { velocidade = 3 });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Velocidade 3 inválida: use 1, 2 ou 4.", problema.Detail);
        }

        [Fact(DisplayName = "UC10 · Reiniciar pela API devolve 204 e apaga as ocorrências")]
        [Trait("Requisito", "UC10")]
        public async Task Reiniciar_DeveDevolver204_QuandoHaEmergenciaEmAndamento()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });
            api.Avancar(2.0);

            // Act
            var resposta = await cliente.PostAsync("/api/v1/simulacao/reiniciar", null);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            var estado = await ApiFactory.Ler<EstadoResponse>(await cliente.GetAsync("/api/v1/estado"));
            Assert.NotNull(estado);
            Assert.Equal(0.0, estado.TempoSeg);
            Assert.Empty(estado.Ocorrencias);
            Assert.Equal(ModoViatura.Patrulha, estado.Viaturas[0].Modo);
        }
    }
}
