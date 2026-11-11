using System.Net;
using System.Net.Http.Json;
using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace CorredorVerde.Tests.API
{
    public class ViaturaApiTests
    {
        [Fact(DisplayName = "UC01 · A lista de viaturas traz as 4 da configuração")]
        [Trait("Requisito", "UC01")]
        public async Task Listar_DeveTrazerAsQuatroViaturas_QuandoASimulacaoComeca()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.GetAsync("/api/v1/viaturas");

            // Assert
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var viaturas = await ApiFactory.Ler<List<ViaturaResponse>>(resposta);
            Assert.NotNull(viaturas);
            Assert.Equal(4, viaturas.Count);
        }

        [Fact(DisplayName = "UC01 · Avisar a saída de um cruzamento devolve 204 e a viatura passa a estar na rua")]
        [Trait("Requisito", "UC01")]
        public async Task Saida_DeveDevolver204_QuandoOsCruzamentosSaoVizinhos()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C11", para = "C12" });

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal("C12", api.Cidade.Viaturas["AMB-01"].IndoPara);
        }

        [Fact(DisplayName = "RNF04 · Saída sem o campo \"para\" vira 400 com a mensagem do campo")]
        [Trait("Requisito", "RNF04")]
        public async Task Saida_DeveDevolver400_QuandoFaltaOCampoPara()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/viaturas/AMB-01/saida", new { de = "C11" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Informe o cruzamento para onde a viatura vai (para).", problema.Detail);
        }

        [Fact(DisplayName = "RNF04 · Chegada em cruzamento que não existe vira 400")]
        [Trait("Requisito", "RNF04")]
        public async Task Chegada_DeveDevolver400_QuandoOCruzamentoNaoExiste()
        {
            // Arrange
            using var api = new ApiFactory();
            var cliente = api.CreateClient();

            // Act
            var resposta = await cliente.PostAsJsonAsync("/api/v1/viaturas/AMB-01/chegada", new { cruzamento = "C99" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var problema = await ApiFactory.Ler<ProblemDetails>(resposta);
            Assert.NotNull(problema);
            Assert.Equal("Cruzamento C99 não existe na malha.", problema.Detail);
        }

        [Fact(DisplayName = "UC03 · Uma emergência de ponta a ponta: do chamado ao hospital, só pela API")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "UC05")]
        [Trait("Requisito", "RN08")]
        [Trait("Requisito", "RN09")]
        public async Task Emergencia_DeveSerEncerrada_QuandoAAmbulanciaCumpreOsDoisTrechos()
        {
            // Arrange: a ambulância AMB-01 é chamada para C15
            using var api = new ApiFactory();
            var cliente = api.CreateClient();
            await cliente.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 1 });
            var ambulancia = api.Cidade.Viaturas["AMB-01"];

            // Act 1: a maquete avisa cada chegada até o local da emergência
            while (ambulancia.Modo == ModoViatura.Emergencia && ambulancia.Trecho == 1)
            {
                var proximo = ambulancia.Rota[0].Cruzamento;
                await cliente.PostAsJsonAsync("/api/v1/viaturas/AMB-01/chegada", new { cruzamento = proximo });
            }

            // Assert 1: chegou ao local e começou o atendimento
            Assert.Equal(ModoViatura.Atendimento, ambulancia.Modo);

            // Act 2: passam os 4 s de atendimento, e a ambulância segue ao hospital
            api.Avancar(4.5);
            Assert.Equal(ModoViatura.Emergencia, ambulancia.Modo);
            Assert.Equal(2, ambulancia.Trecho);
            while (ambulancia.Modo == ModoViatura.Emergencia)
            {
                var proximo = ambulancia.Rota[0].Cruzamento;
                await cliente.PostAsJsonAsync("/api/v1/viaturas/AMB-01/chegada", new { cruzamento = proximo });
            }

            // Assert 2: ocorrência encerrada, ambulância livre e nenhum cruzamento preso
            var resposta = await cliente.GetAsync("/api/v1/ocorrencias");
            var lista = await ApiFactory.Ler<List<OcorrenciaResponse>>(resposta);
            Assert.NotNull(lista);
            Assert.Equal(StatusOcorrencia.Encerrada, lista[0].Status);
            Assert.Equal(ModoViatura.Patrulha, ambulancia.Modo);
            foreach (var cruzamento in api.Cidade.Cruzamentos.Values)
                Assert.Empty(cruzamento.Reservas);
        }
    }
}
