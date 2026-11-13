using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class SemaforoServiceTests
    {
        [Fact(DisplayName = "RN01 · Norte-Sul e Leste-Oeste nunca ficam verdes juntos, nem em 1 hora de simulação")]
        [Trait("Requisito", "RN01")]
        [Trait("Requisito", "RNF01")]
        public void Avancar_NuncaDeixaDuasDirecoesVerdes_DuranteUmaHoraDeSimulacao()
        {
            // Arrange: em C7, duas viaturas pedem corredor em direções diferentes
            var f = CidadeFactory.Criar();
            f.Reservar("C7", "AMB-01", Eixo.NS, criticidade: 2, ordem: 1);
            f.Reservar("C7", "BOM-01", Eixo.LO, criticidade: 1, ordem: 2);
            var vezesComDoisVerdes = 0;

            // Act: 36.000 passos de 0,1 s = 1 hora
            for (var i = 0; i < 36000; i++)
            {
                f.Avancar(0.1);
                vezesComDoisVerdes += f.ContarCruzamentosComDoisVerdes();
            }

            // Assert
            Assert.Equal(0, vezesComDoisVerdes);
        }

        [Fact(DisplayName = "RN02 · Mesmo quando o corredor pede a troca, o verde passa 2 s no amarelo antes do vermelho")]
        [Trait("Requisito", "RN02")]
        public void Avancar_PassaPorAmareloDeDoisSegundos_AntesDoVermelho()
        {
            // Arrange: C7 com verde para Norte-Sul
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C7", Eixo.NS);

            // Act: chega uma ambulância pelo Leste-Oeste
            f.Reservar("C7", "AMB-01", Eixo.LO, criticidade: 1, ordem: 1);
            f.Semaforo.Atualizar(f.PegarCruzamento("C7"));

            // Assert: na hora, o Norte-Sul vai para amarelo (não pula direto para vermelho)
            Assert.Equal(Luz.Amarelo, f.LuzDe("C7", Eixo.NS));

            // Assert: com 1,9 s ainda é amarelo
            f.Avancar(1.9);
            Assert.Equal(Luz.Amarelo, f.LuzDe("C7", Eixo.NS));

            // Assert: com 2 s o Norte-Sul fecha e o Leste-Oeste abre
            f.Avancar(0.1);
            Assert.Equal(Luz.Vermelho, f.LuzDe("C7", Eixo.NS));
            Assert.Equal(Luz.Verde, f.LuzDe("C7", Eixo.LO));
        }

        [Fact(DisplayName = "RN03 · Sem emergência, o semáforo fica 8 s verde e 2 s amarelo em cada direção")]
        [Trait("Requisito", "RN03")]
        [Trait("Requisito", "UC02")]
        public void Avancar_AlternaOitoSegundosDeVerdeEDoisDeAmarelo_QuandoNaoHaCorredor()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C1", Eixo.NS);

            // Act + Assert: 7,9 s → Norte-Sul ainda verde
            f.Avancar(7.9);
            Assert.Equal(Luz.Verde, f.LuzDe("C1", Eixo.NS));

            // Act + Assert: 8 s → Norte-Sul amarelo
            f.Avancar(0.1);
            Assert.Equal(Luz.Amarelo, f.LuzDe("C1", Eixo.NS));

            // Act + Assert: 10 s → a vez do Leste-Oeste
            f.Avancar(2.0);
            Assert.Equal(Luz.Vermelho, f.LuzDe("C1", Eixo.NS));
            Assert.Equal(Luz.Verde, f.LuzDe("C1", Eixo.LO));

            // Act + Assert: 18 s → Leste-Oeste amarelo
            f.Avancar(8.0);
            Assert.Equal(Luz.Amarelo, f.LuzDe("C1", Eixo.LO));
        }

        [Fact(DisplayName = "RN07 · Só pode atravessar no verde; vermelho e amarelo seguram a viatura na faixa")]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DeveNegar_QuandoVermelhoOuAmarelo()
        {
            // Arrange: C7 verde para Norte-Sul (logo, vermelho para Leste-Oeste)
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C7", Eixo.NS);

            // Act
            var noVerde = f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01");
            var noVermelho = f.Semaforo.PodeAtravessar("C7", Eixo.LO, "AMB-01");
            f.PegarCruzamento("C7").Semaforo.EmAmarelo = true;
            var noAmarelo = f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01");

            // Assert
            Assert.True(noVerde.Pode);
            Assert.False(noVermelho.Pode);
            Assert.False(noAmarelo.Pode);
        }

        [Fact(DisplayName = "RN13 · Semáforo em falha fica piscante, e a viatura passa com cautela")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DevePermitirComCautela_QuandoSemaforoEstaEmFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C7");

            // Act
            var resposta = f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01");

            // Assert
            Assert.True(resposta.Pode);
            Assert.True(resposta.Cautela);
            Assert.Equal(Luz.Piscante, resposta.Luz);
        }

        [Fact(DisplayName = "RNF12 · Os tempos do semáforo vêm da configuração, sem mudar o código")]
        [Trait("Requisito", "RNF12")]
        public void Avancar_DeveUsarOsTemposDoAppsettings_QuandoConfiguracaoMuda()
        {
            // Arrange: 5 s de verde e 1 s de amarelo, em vez de 8 e 2
            var f = CidadeFactory.Criar(opcoes =>
            {
                opcoes.VerdeSeg = 5;
                opcoes.AmareloSeg = 1;
            });
            f.SemaforoComeca("C1", Eixo.NS);

            // Act + Assert: com 5 s já é amarelo
            f.Avancar(5.0);
            Assert.Equal(Luz.Amarelo, f.LuzDe("C1", Eixo.NS));

            // Act + Assert: 1 s depois, a vez do Leste-Oeste
            f.Avancar(1.0);
            Assert.Equal(Luz.Verde, f.LuzDe("C1", Eixo.LO));
        }

        [Fact(DisplayName = "RN12 · No conflito vence a criticidade mais grave (1); no empate, quem foi registrado primeiro")]
        [Trait("Requisito", "RN12")]
        public void Vencedor_DevePriorizarMaiorCriticidade_EDepoisOMaisAntigo()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var c13 = f.PegarCruzamento("C13");

            // Act + Assert: sem reserva, ninguém vence
            Assert.Null(f.Semaforo.Vencedor(c13));

            // Act + Assert: criticidade 1 vence a 2, mesmo registrada depois
            f.Reservar("C13", "AMB-01", Eixo.LO, criticidade: 2, ordem: 1);
            f.Reservar("C13", "BOM-01", Eixo.NS, criticidade: 1, ordem: 2);
            Assert.Equal("BOM-01", f.Semaforo.Vencedor(c13)?.ViaturaId);

            // Act + Assert: com as duas em criticidade 2, vence a ordem 1
            c13.Reservas.Clear();
            f.Reservar("C13", "BOM-01", Eixo.NS, criticidade: 2, ordem: 2);
            f.Reservar("C13", "AMB-01", Eixo.LO, criticidade: 2, ordem: 1);
            Assert.Equal("AMB-01", f.Semaforo.Vencedor(c13)?.ViaturaId);
        }

        [Fact(DisplayName = "RN06 · Com corredor, o verde vai para a direção da viatura e fica parado ali")]
        [Trait("Requisito", "RN06")]
        public void Atualizar_DeveAbrirOVerdeParaADirecaoDoCorredor_ESegurarAteLiberar()
        {
            // Arrange: C7 verde para Norte-Sul; a ambulância vem pelo Leste-Oeste
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C7", Eixo.NS);
            f.Reservar("C7", "AMB-01", Eixo.LO, criticidade: 1, ordem: 1);

            // Act + Assert: depois dos 2 s de amarelo, o verde é do Leste-Oeste
            f.Avancar(2.1);
            Assert.Equal(Luz.Verde, f.LuzDe("C7", Eixo.LO));

            // Act + Assert: 30 s depois continua verde (no normal trocaria em 8 s)
            f.Avancar(30.0);
            Assert.Equal(Luz.Verde, f.LuzDe("C7", Eixo.LO));
        }

        [Fact(DisplayName = "RN08 · Quando a reserva sai, o cruzamento volta a alternar no ciclo normal")]
        [Trait("Requisito", "RN08")]
        public void Liberar_DeveTirarAReserva_EVoltarAoCicloNormal()
        {
            // Arrange: C7 em corredor para o Leste-Oeste
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C7", Eixo.NS);
            f.Reservar("C7", "AMB-01", Eixo.LO, criticidade: 1, ordem: 1);
            f.Avancar(2.1);

            // Act
            var liberou = f.Semaforo.Liberar(f.PegarCruzamento("C7"), "AMB-01");
            f.Avancar(10.2);

            // Assert: sem reserva, depois de 10 s (8 verde + 2 amarelo) a vez volta ao Norte-Sul
            Assert.True(liberou);
            Assert.Equal("nenhuma", f.ReservasComoTexto("C7"));
            Assert.Equal(Luz.Verde, f.LuzDe("C7", Eixo.NS));
        }
    }
}
