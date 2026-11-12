using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Xunit.Sdk;

namespace CorredorVerde.Tests.API
{
    public class ViaturaApiTests
    {
        // Uma rota da malha de teste tem no máximo uns 10 cruzamentos; passar disso é sinal de que a viatura travou
        private const int LimiteDeChegadas = 20;

        [Fact(DisplayName = "UC01 · A lista de viaturas traz as 4 da configuração")]
        [Trait("Requisito", "UC01")]
        public async Task Listar_DeveTrazerAsQuatroViaturas_QuandoASimulacaoComeca()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/viaturas");

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var viaturas = await ApiFactory.Ler<List<ViaturaResponse>>(respostaHttp);
            Assert.NotNull(viaturas);
            Assert.Equal(4, viaturas.Count);
        }

        [Fact(DisplayName = "UC01 · Avisar a saída de um cruzamento devolve 204 e a viatura passa a estar na rua")]
        [Trait("Requisito", "UC01")]
        public async Task Saida_DeveDevolver204_QuandoOsCruzamentosSaoVizinhos()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C11", para = "C12" });

            // Assert: a maquete vê, pelo estado, a viatura indo de C11 para C12
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var estado = await ApiFactory.LerEstado(clienteHttp);
            var ambulancia = ApiFactory.ViaturaDe(estado, "AMB-01");
            Assert.Equal("C11", ambulancia.Cruzamento);
            Assert.Equal("C12", ambulancia.IndoPara);
        }

        [Fact(DisplayName = "RNF04 · Saída sem o campo \"para\" vira 400 com a mensagem do campo")]
        [Trait("Requisito", "RNF04")]
        public async Task Saida_DeveDevolver400_QuandoFaltaOCampoPara()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C11" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Informe o cruzamento para onde a viatura vai (para).", detalhesDoErro.Detail);
        }

        [Fact(DisplayName = "RNF04 · Chegada em cruzamento que não existe vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task Chegada_DeveDevolver400_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/chegada", new { cruzamento = "C99" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("Cruzamento C99 não existe na malha.", detalhesDoErro.Detail);
        }

        [Fact(DisplayName = "RNF04 · Saída entre cruzamentos que não são vizinhos é recusada e a viatura não sai do lugar")]
        [Trait("Requisito", "RNF04")]
        [Trait("Requisito", "UC01")]
        public async Task Saida_DeveDevolver400_QuandoOsCruzamentosNaoSaoVizinhos()
        {
            // Arrange: C1 e C20 ficam em pontas opostas da malha
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C1", para = "C20" });

            // Assert: 400 com o motivo, e a posição da viatura continua a de antes
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("C1 e C20 não são vizinhos.", detalhesDoErro.Detail);
            var ambulancia = ApiFactory.ViaturaDe(await ApiFactory.LerEstado(clienteHttp), "AMB-01");
            Assert.Equal("C11", ambulancia.Cruzamento);
            Assert.Null(ambulancia.IndoPara);
        }

        [Fact(DisplayName = "RN09 · Viatura em atendimento não pode sair do cruzamento: a API devolve 409")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "RNF04")]
        public async Task Saida_DeveDevolver409_QuandoAViaturaEstaEmAtendimento()
        {
            // Arrange: BOM-01 é chamada para C15 e chega ao local, começando o atendimento
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Bombeiros", "C15", 2);
            var bombeiros = await AvisarChegadasAteTerminarOTrecho(clienteHttp, "BOM-01");
            Assert.Equal(ModoViatura.Atendimento, bombeiros.Modo);

            // Act: no meio do atendimento, a maquete avisa que ela saiu para C14
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/BOM-01/saida", new { de = "C15", para = "C14" });

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, respostaHttp.StatusCode);
            var detalhesDoErro = await ApiFactory.Ler<ProblemDetails>(respostaHttp);
            Assert.NotNull(detalhesDoErro);
            Assert.Equal("BOM-01 está em atendimento.", detalhesDoErro.Detail);
        }

        [Fact(DisplayName = "RN10 · Se a viatura entra numa rua fora da rota, o servidor recalcula e avisa no log")]
        [Trait("Requisito", "RN10")]
        [Trait("Requisito", "UC07")]
        public async Task Saida_DeveRecalcularARota_QuandoAViaturaEntraEmRuaForaDaRota()
        {
            // Arrange: AMB-01 chamada para C15; a rota pede C12 primeiro
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);

            // Act: em vez de ir para C12, a ambulância sai para C6
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C11", para = "C6" });

            // Assert: 204, a rota nova começa em C6 e o log avisa do desvio
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var ambulancia = ApiFactory.ViaturaDe(await ApiFactory.LerEstado(clienteHttp), "AMB-01");
            Assert.Equal("C6", ambulancia.Rota[0].Cruzamento);
            var eventosDoLog = await ApiFactory.LerLog(clienteHttp);
            var eventoDoDesvio = eventosDoLog.Find(e => e.Mensagem == "AMB-01 saiu da rota em C11 (foi para C6, a rota pedia C12). Recalculando o trecho.");
            Assert.NotNull(eventoDoDesvio);
            Assert.Equal(TipoEvento.Alerta, eventoDoDesvio.Tipo);
        }

        [Fact(DisplayName = "RN10 · Se a viatura chega a um cruzamento fora da rota, o servidor recalcula e avisa no log")]
        [Trait("Requisito", "RN10")]
        [Trait("Requisito", "UC07")]
        public async Task Chegada_DeveRecalcularARota_QuandoAViaturaChegaAUmCruzamentoForaDaRota()
        {
            // Arrange: AMB-01 chamada para C15; a rota pede C12 primeiro
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);

            // Act: a maquete avisa que a ambulância chegou a C6, que não estava na rota
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/viaturas/AMB-01/chegada", new { cruzamento = "C6" });

            // Assert: 204, a rota nova sai de C6 e termina no local da ocorrência, e o log avisa
            Assert.Equal(HttpStatusCode.NoContent, respostaHttp.StatusCode);
            var ambulancia = ApiFactory.ViaturaDe(await ApiFactory.LerEstado(clienteHttp), "AMB-01");
            Assert.NotEmpty(ambulancia.Rota);
            Assert.Equal("C7", ambulancia.Rota[0].Cruzamento);
            Assert.Equal("C15", ambulancia.Rota[ambulancia.Rota.Count - 1].Cruzamento);
            var eventosDoLog = await ApiFactory.LerLog(clienteHttp);
            var eventoDoDesvio = eventosDoLog.Find(e => e.Mensagem == "AMB-01 chegou a C6, fora da rota. Recalculando o trecho.");
            Assert.NotNull(eventoDoDesvio);
        }

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
