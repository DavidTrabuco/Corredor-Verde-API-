using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // A falha é ligada e desligada de verdade (Falhas.Definir), do jeito que a API faz.
    public class FalhaServiceTests
    {
        [Fact(DisplayName = "RN13 · Semáforo em falha fica em amarelo piscante nos dois sentidos")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public void Definir_DeveColocarEmAmareloPiscante_QuandoLigaAFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            f.Falhas.Definir("C13", ativa: true);

            // Assert: piscante nos dois eixos e o evento vai para o log
            Assert.Equal(Luz.Piscante, f.LuzDe("C13", Eixo.NS));
            Assert.Equal(Luz.Piscante, f.LuzDe("C13", Eixo.LO));
            Assert.True(f.LogTem("Semáforo de C13 em falha: amarelo piscante. Veículos param e seguem com cautela."));
        }

        [Fact(DisplayName = "UC09 · Ligar uma falha que já está ligada não muda nada nem repete o log")]
        [Trait("Requisito", "UC09")]
        public void Definir_NaoDeveRepetirOLog_QuandoAFalhaJaEstaLigada()
        {
            // Arrange: a falha de C13 já foi ligada uma vez
            var f = CidadeFactory.Criar();
            f.Falhas.Definir("C13", ativa: true);

            // Act: liga de novo
            f.Falhas.Definir("C13", ativa: true);

            // Assert: só um evento de falha no log
            Assert.Equal(1, f.QuantosLogsCom("UC09"));
        }

        [Fact(DisplayName = "RN05 · Se a rota passava pelo semáforo que falhou, ela é recalculada para desviar")]
        [Trait("Requisito", "RN05")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public void Definir_DeveDesviarARota_QuandoFalhaNoMeioDoCaminho()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15, passando por C13
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act: o semáforo de C13 falha
            f.Falhas.Definir("C13", ativa: true);

            // Assert: a rota nova dá a volta por cima (C6 a C10) sem passar por C13, e a reserva antiga de C13 foi solta
            Assert.True(f.LogTem("A rota de AMB-01 passava por C13. Recalculando para desviar."));
            Assert.Equal("C6 → C7 → C8 → C9 → C10 → C15", f.RotaComoTexto(ambulancia));
            Assert.Equal("nenhuma", f.ReservasComoTexto("C13"));
        }

        [Fact(DisplayName = "RN13 · Se o destino é que falhou, a rota não muda")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public void Definir_NaoDeveDesviarARota_QuandoFalhaNoDestino()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act: o semáforo do destino (C15) falha
            f.Falhas.Definir("C15", ativa: true);

            // Assert: não tem como evitar o destino, então a rota continua a mesma
            Assert.False(f.LogTem("A rota de AMB-01 passava por C15. Recalculando para desviar."));
            Assert.Equal("C12 → C13 → C14 → C15", f.RotaComoTexto(ambulancia));
        }

        [Fact(DisplayName = "UC09 · Ao desligar a falha, o semáforo recomeça o ciclo normal com verde para Norte-Sul")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public void Definir_DeveRecomecarOCiclo_QuandoDesligaAFalha()
        {
            // Arrange: C13 está em falha
            var f = CidadeFactory.Criar();
            f.Falhas.Definir("C13", ativa: true);

            // Act
            f.Falhas.Definir("C13", ativa: false);

            // Assert: voltou ao ciclo normal
            Assert.Equal(Luz.Verde, f.LuzDe("C13", Eixo.NS));
            Assert.Equal(Luz.Vermelho, f.LuzDe("C13", Eixo.LO));
            Assert.True(f.LogTem("Falha de C13 desligada: o ciclo normal recomeça."));
        }

        [Fact(DisplayName = "UC09 · Ao desligar a falha, quem estava sem caminho tenta a rota de novo na hora")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public void Definir_DeveRefazerARota_QuandoViaturaEstavaSemCaminho()
        {
            // Arrange: a coluna inteira de C3, C8, C13 e C18 está em falha, então não há desvio para AMB-01
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C3", "C8", "C13", "C18");
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            Assert.True(ambulancia.SemRota);

            // Act: a falha de C13 é desligada
            f.Falhas.Definir("C13", ativa: false);

            // Assert: a viatura achou o caminho na hora
            Assert.False(ambulancia.SemRota);
            Assert.Equal("C12 → C13 → C14 → C15", f.RotaComoTexto(ambulancia));
        }
    }
}
