using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // O tempo anda com Temporizador.Avancar, o mesmo método que o relógio da API chama a cada 100 ms.
    public class TemporizadorServiceTests
    {
        [Fact(DisplayName = "UC02 · O relógio faz o tempo da simulação andar")]
        [Trait("Requisito", "UC02")]
        public void Avancar_DeveSomarOTempo_QuandoORelogioAnda()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: o relógio anda 2,5 s
            f.Temporizador.Avancar(2.5);

            // Assert
            Assert.Equal(2.5, f.Cidade.TempoSeg, precision: 6);
        }

        [Fact(DisplayName = "UC02 · O relógio também atualiza os semáforos: depois de 8 s de verde vem o amarelo")]
        [Trait("Requisito", "UC02")]
        [Trait("Requisito", "RN02")]
        public void Avancar_DeveAtualizarOsSemaforos_QuandoOVerdeAcaba()
        {
            // Arrange: o semáforo de C1 começa agora com verde para Norte-Sul
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C1", Eixo.NS);

            // Act + Assert: 7,9 s ainda é verde; 8 s é amarelo
            f.Temporizador.Avancar(7.9);
            Assert.Equal(Luz.Verde, f.LuzDe("C1", Eixo.NS));
            f.Temporizador.Avancar(0.2);
            Assert.Equal(Luz.Amarelo, f.LuzDe("C1", Eixo.NS));
        }

        [Fact(DisplayName = "UC02 · Um salto grande de tempo não pula nenhuma troca de fase do semáforo")]
        [Trait("Requisito", "UC02")]
        [Trait("Requisito", "RN01")]
        public void Avancar_NaoDevePularFases_QuandoOTempoDaUmSaltoGrande()
        {
            // Arrange: o semáforo de C1 começa agora com verde para Norte-Sul
            var f = CidadeFactory.Criar();
            f.SemaforoComeca("C1", Eixo.NS);

            // Act: 10 s de uma vez (8 s de verde + 2 s de amarelo)
            f.Temporizador.Avancar(10.0);

            // Assert: a fase trocou certinho, e agora o verde é do Leste-Oeste
            Assert.Equal(Luz.Verde, f.LuzDe("C1", Eixo.LO));
            Assert.Equal(Luz.Vermelho, f.LuzDe("C1", Eixo.NS));
        }

        [Fact(DisplayName = "RN09 · Passados os 4 s de atendimento, o relógio encerra o atendimento")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void Avancar_DeveEncerrarOAtendimento_QuandoPassam4Segundos()
        {
            // Arrange: BOM-01 chegou ao local e começou o atendimento
            var f = CidadeFactory.Criar();
            var bombeiros = f.ColocarEmEmergencia("BOM-01", de: "C15", ate: "C15", criticidade: 2);
            var ocorrencia = f.Cidade.Ocorrencias[0];
            f.Frota.ChegouAoDestino(bombeiros);

            // Act + Assert: com 3,5 s ainda está atendendo; com 4,5 s terminou
            f.Temporizador.Avancar(3.5);
            Assert.Equal(ModoViatura.Atendimento, bombeiros.Modo);
            f.Temporizador.Avancar(1.0);
            Assert.Equal(StatusOcorrencia.Encerrada, ocorrencia.Status);
            Assert.Equal(ModoViatura.Patrulha, bombeiros.Modo);
        }

        [Fact(DisplayName = "RN13 · Sem desvio, depois de 1 s a viatura tenta de novo e passa com cautela pela falha")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RNF03")]
        public void Avancar_DeveTentarARotaDeNovo_DepoisDeUmSegundoSemCaminho()
        {
            // Arrange: a coluna inteira de C3, C8, C13 e C18 está em falha, então AMB-01 fica sem rota
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C3", "C8", "C13", "C18");
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            Assert.True(ambulancia.SemRota);

            // Act + Assert: com 0,5 s ainda espera; com 1,1 s tenta de novo e passa pela falha
            f.Temporizador.Avancar(0.5);
            Assert.True(ambulancia.SemRota);
            f.Temporizador.Avancar(0.6);
            Assert.False(ambulancia.SemRota);
            Assert.Equal("C12 → C13 → C14 → C15", f.RotaComoTexto(ambulancia));
        }
    }
}
