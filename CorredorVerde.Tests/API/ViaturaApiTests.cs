using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Xunit.Sdk;

namespace CorredorVerde.Tests.API
{
    public class ViaturaApiTests
    {
        // Uma rota da malha de teste tem no máximo uns 10 cruzamentos; passar disso é sinal de que a viatura travou
        private const int LimiteDeChegadas = 20;

        [Fact(DisplayName = "UC03 · Uma emergência de ponta a ponta pela API: do chamado ao hospital, com a maquete avisando cada chegada")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "UC05")]
        [Trait("Requisito", "RN08")]
        [Trait("Requisito", "RN09")]
        public async Task Emergencia_DeveSerEncerrada_QuandoAAmbulanciaCumpreOsDoisTrechos()
        {
            // Arrange: a ambulância AMB-01 é chamada para C15 (o registro é conferido: precisa dar 201)
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            var ocorrencia = await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);
            Assert.Equal(StatusOcorrencia.ACaminho, ocorrencia.Status);

            // Act 1: a maquete avisa cada chegada, seguindo a rota que o estado mostra, até o local da emergência
            var noLocal = await AvisarChegadasAteTerminarOTrecho(clienteHttp, "AMB-01");

            // Assert 1: começou o atendimento, com 4 s para cumprir
            Assert.Equal(ModoViatura.Atendimento, noLocal.Modo);
            Assert.Equal(4.0, noLocal.AtendimentoRestanteSeg, precision: 1);
            var estadoNoLocal = await ApiFactory.LerEstado(clienteHttp);
            Assert.Equal(StatusOcorrencia.EmAtendimento, estadoNoLocal.Ocorrencias[0].Status);

            // Act 2: passam os 4 s de atendimento (o relógio é o único atalho: na API de verdade ele roda sozinho)
            api.Avancar(4.5);

            // Assert 2: a ambulância segue ao hospital, no trecho 2
            var aCaminhoDoHospital = ApiFactory.ViaturaDe(await ApiFactory.LerEstado(clienteHttp), "AMB-01");
            Assert.Equal(ModoViatura.Emergencia, aCaminhoDoHospital.Modo);
            Assert.Equal(2, aCaminhoDoHospital.Trecho);
            Assert.Equal("C18", aCaminhoDoHospital.Destino);
            Assert.Equal("Hospital Sul", aCaminhoDoHospital.Hospital);

            // Act 3: a maquete avisa cada chegada até o hospital
            var noHospital = await AvisarChegadasAteTerminarOTrecho(clienteHttp, "AMB-01");

            // Assert 3: ocorrência encerrada, ambulância de volta à patrulha e nenhum cruzamento com reserva
            Assert.Equal(ModoViatura.Patrulha, noHospital.Modo);
            var estadoFinal = await ApiFactory.LerEstado(clienteHttp);
            Assert.Equal(StatusOcorrencia.Encerrada, estadoFinal.Ocorrencias[0].Status);
            foreach (var semaforo in estadoFinal.Semaforos)
                Assert.Empty(semaforo.Reservas);
        }

        // Faz o papel da maquete: lê o estado, vê o próximo cruzamento da rota e avisa a chegada, até a viatura sair da emergência.
        // O número de chegadas é limitado: se algo travar, o teste falha com uma mensagem clara em vez de rodar para sempre.
        private static async Task<ViaturaResponse> AvisarChegadasAteTerminarOTrecho(HttpClient clienteHttp, string idDaViatura)
        {
            for (var chegada = 1; chegada <= LimiteDeChegadas; chegada++)
            {
                var viatura = ApiFactory.ViaturaDe(await ApiFactory.LerEstado(clienteHttp), idDaViatura);
                if (viatura.Modo != ModoViatura.Emergencia)
                    return viatura;

                Assert.NotEmpty(viatura.Rota);
                var proximoCruzamento = viatura.Rota[0].Cruzamento;
                var respostaDaChegada = await clienteHttp.PostAsJsonAsync($"/api/v1/viaturas/{idDaViatura}/chegada", new { cruzamento = proximoCruzamento });
                Assert.Equal(HttpStatusCode.NoContent, respostaDaChegada.StatusCode);
            }

            throw new XunitException($"{idDaViatura} não terminou o trecho depois de {LimiteDeChegadas} chegadas avisadas.");
        }
    }
}
