using System.Diagnostics;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class MalhaServiceTests
    {
        [Fact(DisplayName = "RN05 · De qualquer cruzamento para qualquer outro, a rota tem o menor número de quarteirões")]
        [Trait("Requisito", "RN05")]
        public void MenorCaminho_DeveTerOMenorNumeroDeQuarteiroes_ParaTodosOsPares()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert: testa os 20 × 20 = 400 pares de cruzamentos
            foreach (var origem in f.IdsDosCruzamentos())
            {
                foreach (var destino in f.IdsDosCruzamentos())
                {
                    var caminho = f.Malha.MenorCaminho(origem, destino);

                    Assert.NotNull(caminho);
                    Assert.Equal(f.QuarteiroesEntre(origem, destino), caminho.Count);
                }
            }
        }

        [Fact(DisplayName = "RN05 · Com C12 em falha, a rota de C11 a C13 dá a volta por cima")]
        [Trait("Requisito", "RN05")]
        [Trait("Requisito", "RN13")]
        public void MenorCaminho_DeveDesviar_QuandoCruzamentoEstaEmFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C12");

            // Act
            var caminho = f.CaminhoComoTexto("C11", "C13");

            // Assert: sem falha seria "C12 → C13"
            Assert.Equal("C6 → C7 → C8 → C13", caminho);
        }

        [Fact(DisplayName = "RN13 · Se todos os vizinhos do destino estão em falha, não existe caminho")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RNF03")]
        public void MenorCaminho_DeveRetornarNulo_QuandoDestinoEstaCercadoPorFalhas()
        {
            // Arrange: C15 só tem três vizinhos, C10, C14 e C20
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C10", "C14", "C20");

            // Act
            var caminho = f.Malha.MenorCaminho("C11", "C15");

            // Assert: null = "não tem caminho"
            Assert.Null(caminho);
        }

        [Fact(DisplayName = "RN05 · O destino pode estar em falha; só os cruzamentos do meio do caminho são evitados")]
        [Trait("Requisito", "RN05")]
        public void MenorCaminho_DeveAceitarDestinoEmFalha_QuandoEleEOProprioDestino()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C13");

            // Act
            var caminho = f.CaminhoComoTexto("C11", "C13");

            // Assert
            Assert.Equal("C12 → C13", caminho);
        }

        [Fact(DisplayName = "RNF07 · Calcular a rota mais longa da malha (C1 até C20) leva no máximo 50 ms")]
        [Trait("Requisito", "RNF07")]
        public void MenorCaminho_DeveLevarAteCinquentaMs_NaMalhaDeVinteCruzamentos()
        {
            // Arrange: a primeira chamada só "aquece" o código, que o .NET compila na primeira vez
            var f = CidadeFactory.Criar();
            f.Malha.MenorCaminho("C1", "C20");
            var cronometro = new Stopwatch();

            // Act
            cronometro.Start();
            f.Malha.MenorCaminho("C1", "C20");
            cronometro.Stop();

            // Assert
            Assert.True(cronometro.ElapsedMilliseconds <= 50);
        }

        [Fact(DisplayName = "RN04 · A ambulância leva o paciente ao hospital mais perto do local")]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveEscolherOHospitalComMenorCaminho()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: C14 fica a 2 quarteirões do Hospital Sul; C2 fica a 1 do Hospital Norte
            var hospitalDeC14 = f.Malha.HospitalMaisProximo("C14");
            var hospitalDeC2 = f.Malha.HospitalMaisProximo("C2");

            // Assert
            Assert.Equal("Hospital Sul", hospitalDeC14?.Nome);
            Assert.Equal("Hospital Norte", hospitalDeC2?.Nome);
        }

        [Fact(DisplayName = "RN06 · Cada passo da rota diz por qual eixo a viatura chega (é esse eixo que ganha o verde)")]
        [Trait("Requisito", "RN06")]
        public void MontarRota_DeveInformarOEixoDeChegadaEmCadaCruzamento()
        {
            // Arrange: de C11 a C8 o caminho é C6 → C7 → C8
            var f = CidadeFactory.Criar();
            var caminho = f.Malha.MenorCaminho("C11", "C8");
            Assert.NotNull(caminho);

            // Act
            var rota = f.Malha.MontarRota("C11", caminho);

            // Assert: sobe para C6 (Norte-Sul) e depois anda para o Leste até C8 (Leste-Oeste)
            Assert.Equal(Eixo.NS, rota[0].Eixo);
            Assert.Equal(Eixo.LO, rota[1].Eixo);
            Assert.Equal(Eixo.LO, rota[2].Eixo);
        }

        [Fact(DisplayName = "RNF04 · Pedir um cruzamento que não existe dá erro de validação (vira 400 na API)")]
        [Trait("Requisito", "RNF04")]
        public void ObterCruzamento_DeveLancarValidacaoException_QuandoNaoExiste()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert: o código dentro de "() =>" tem que lançar a exceção
            Assert.Throws<ValidacaoException>(() => f.Malha.ObterCruzamento("C99"));
        }

        [Fact(DisplayName = "RN05 · A malha sabe a direção entre dois vizinhos, e que C11 e C20 não são vizinhos")]
        [Trait("Requisito", "RN05")]
        public void DirecaoEntre_DeveRetornarNulo_QuandoNaoSaoVizinhos()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert
            Assert.Equal(Direcao.L, f.Malha.DirecaoEntre("C11", "C12"));
            Assert.Equal(Direcao.N, f.Malha.DirecaoEntre("C11", "C6"));
            Assert.Null(f.Malha.DirecaoEntre("C11", "C20"));
        }
    }
}
