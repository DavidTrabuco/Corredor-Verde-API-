using System.Diagnostics;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class MalhaServiceTests
    {
        [Fact]
        [Trait("Requisito", "RN05")]
        public void MenorCaminho_DeveTerOMenorNumeroDeQuarteiroes_ParaTodosOsPares()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var ids = f.Cidade.Cruzamentos.Keys.ToList();

            foreach (var a in ids)
            {
                foreach (var b in ids)
                {
                    // Act
                    var caminho = f.Malha.MenorCaminho(a, b);

                    // Assert: numa grade, o menor caminho é a distância de Manhattan
                    var ca = f.Cidade.Cruzamentos[a];
                    var cb = f.Cidade.Cruzamentos[b];
                    var manhattan = Math.Abs(ca.Coluna - cb.Coluna) + Math.Abs(ca.Linha - cb.Linha);
                    Assert.NotNull(caminho);
                    Assert.Equal(manhattan, caminho!.Count);
                }
            }
        }

        [Fact]
        [Trait("Requisito", "RN05")]
        [Trait("Requisito", "RN13")]
        public void MenorCaminho_DeveDesviar_QuandoCruzamentoEstaEmFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Cidade.Cruzamentos["C12"].Semaforo.EmFalha = true;

            // Act
            var caminho = f.Malha.MenorCaminho("C11", "C13");

            // Assert
            Assert.NotNull(caminho);
            Assert.DoesNotContain("C12", caminho!);
            Assert.Equal("C13", caminho[^1]);
        }

        [Fact]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RNF03")]
        public void MenorCaminho_DeveRetornarNulo_QuandoDestinoEstaCercadoPorFalhas()
        {
            // Arrange: C15 (borda leste) só tem os vizinhos C10, C14 e C20
            var f = CidadeFactory.Criar();
            f.Cidade.Cruzamentos["C10"].Semaforo.EmFalha = true;
            f.Cidade.Cruzamentos["C14"].Semaforo.EmFalha = true;
            f.Cidade.Cruzamentos["C20"].Semaforo.EmFalha = true;

            // Act
            var caminho = f.Malha.MenorCaminho("C11", "C15");

            // Assert
            Assert.Null(caminho);
        }

        [Fact]
        [Trait("Requisito", "RN05")]
        public void MenorCaminho_DeveAceitarDestinoEmFalha_QuandoEleEOProprioDestino()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Cidade.Cruzamentos["C13"].Semaforo.EmFalha = true;

            // Act
            var caminho = f.Malha.MenorCaminho("C11", "C13");

            // Assert: a ocorrência pode estar num cruzamento com falha
            Assert.NotNull(caminho);
            Assert.Equal("C13", caminho![^1]);
        }

        [Fact]
        [Trait("Requisito", "RNF07")]
        public void MenorCaminho_DeveLevarAteCinquentaMs_NaMalhaDeVinteCruzamentos()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Malha.MenorCaminho("C1", "C20"); // aquece o JIT
            var pior = 0.0;

            for (var i = 0; i < 400; i++)
            {
                // Act
                var inicio = Stopwatch.GetTimestamp();
                f.Malha.MenorCaminho("C1", "C20");
                var ms = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
                pior = Math.Max(pior, ms);
            }

            // Assert
            Assert.True(pior <= 50, $"Pior caso {pior:F2} ms.");
        }

        [Fact]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveEscolherOHospitalComMenorCaminho()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var perto = f.Malha.HospitalMaisProximo("C14");
            var norte = f.Malha.HospitalMaisProximo("C2");

            // Assert: C14 está a 2 quarteirões de C18 (Sul); C2 a 1 de C3 (Norte)
            Assert.Equal("Hospital Sul", perto!.Nome);
            Assert.Equal("Hospital Norte", norte!.Nome);
        }

        [Fact]
        [Trait("Requisito", "RN06")]
        public void MontarRota_DeveInformarOEixoDeChegadaEmCadaCruzamento()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var caminho = f.Malha.MenorCaminho("C11", "C8")!;

            // Act
            var rota = f.Malha.MontarRota("C11", caminho);

            // Assert: C11 → C6 é Norte-Sul; C6 → C7 e C7 → C8 são Leste-Oeste
            Assert.Equal(3, rota.Count);
            Assert.Equal(Eixo.NS, rota[0].Eixo);
            Assert.Equal(Eixo.LO, rota[1].Eixo);
            Assert.Equal(Eixo.LO, rota[2].Eixo);
            Assert.Equal("C8", rota[^1].Cruzamento);
        }

        [Fact]
        [Trait("Requisito", "RNF04")]
        public void ObterCruzamento_DeveLancarValidacaoException_QuandoNaoExiste()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert
            Assert.Throws<ValidacaoException>(() => f.Malha.ObterCruzamento("C99"));
        }

        [Fact]
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
