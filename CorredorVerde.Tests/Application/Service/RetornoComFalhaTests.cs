using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // A viatura de reserva volta para a base sem corredor verde (RN15). Aqui o que se testa é essa volta
    // quando há semáforo em falha no caminho (RN13): o ViaturaService, o FalhaService e o TemporizadorService juntos.
    public class RetornoComFalhaTests
    {
        // BOM-02 (reserva) termina o atendimento em "local" e começa a voltar para o Quartel (C20)
        private static Viatura ReservaVoltandoDe(CidadeFactory f, string local)
        {
            var reserva = f.ColocarEmEmergencia("BOM-02", de: local, ate: local, criticidade: 2);
            f.Frota.FimDoAtendimento(reserva);
            return reserva;
        }

        [Fact(DisplayName = "RN13 · Voltando para a base sem caminho livre, a viatura avisa que está sem rota e espera")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN15")]
        public void FimDoAtendimento_DeveFicarSemRota_QuandoOsAcessosDaBaseEstaoEmFalha()
        {
            // Arrange: C15 e C19 são os dois acessos do Quartel (C20) e estão em falha
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C15", "C19");

            // Act: BOM-02 termina o atendimento em C1 e tenta voltar
            var reserva = ReservaVoltandoDe(f, "C1");

            // Assert: continua em retorno, mas sem rota e com nova tentativa daqui a 1 s
            Assert.Equal(ModoViatura.Retorno, reserva.Modo);
            Assert.True(reserva.SemRota);
            Assert.Empty(reserva.Rota);
            Assert.Equal(1.0, reserva.ProximaTentativaSeg, precision: 6);
        }

        [Fact(DisplayName = "RN13 · Depois de 1 s sem desvio, quem volta para a base passa com cautela pela falha")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN15")]
        public void Avancar_DeveRefazerOCaminhoDeVolta_DepoisDeUmSegundoSemRota()
        {
            // Arrange: BOM-02 está sem rota de volta, com C15 e C19 em falha
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C15", "C19");
            var reserva = ReservaVoltandoDe(f, "C1");

            // Act: passam 1,1 s
            f.Temporizador.Avancar(1.1);

            // Assert: achou o caminho passando pela falha e terminou na base
            Assert.False(reserva.SemRota);
            Assert.Equal("C2 → C3 → C4 → C5 → C10 → C15 → C20", f.RotaComoTexto(reserva));
        }

        [Fact(DisplayName = "RN13 · Falha num cruzamento do caminho de volta: a rota é refeita para desviar, sem corredor")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN15")]
        public void Definir_DeveRefazerOCaminhoDeVolta_QuandoFalhaNoMeioDoRetorno()
        {
            // Arrange: BOM-02 voltando de C1 para C20
            var f = CidadeFactory.Criar();
            var reserva = ReservaVoltandoDe(f, "C1");
            Assert.Equal("C2 → C3 → C4 → C5 → C10 → C15 → C20", f.RotaComoTexto(reserva));

            // Act: C4, no meio do caminho, entra em falha
            f.Falhas.Definir("C4", ativa: true);

            // Assert: a rota nova desce em C3 → C8, não passa por C4 e ainda termina na base
            Assert.Equal("C2 → C3 → C8 → C9 → C10 → C15 → C20", f.RotaComoTexto(reserva));
            Assert.True(f.NenhumCruzamentoReservado());
        }

        [Fact(DisplayName = "RN13 · Falha no cruzamento em que a viatura já está entrando não muda a rota de volta")]
        [Trait("Requisito", "RN13")]
        public void Definir_NaoDeveMudarARotaDeVolta_QuandoAViaturaJaEstaEntrandoNoCruzamento()
        {
            // Arrange: BOM-02 voltando; ela já saiu de C1 em direção ao primeiro cruzamento da rota
            var f = CidadeFactory.Criar();
            var reserva = ReservaVoltandoDe(f, "C1");
            var primeiro = reserva.Rota[0].Cruzamento;
            f.Frota.Saida("BOM-02", de: "C1", para: primeiro);
            var rotaAntes = f.RotaComoTexto(reserva);

            // Act: justo esse cruzamento entra em falha
            f.Falhas.Definir(primeiro, ativa: true);

            // Assert: segue com cautela pela mesma rota
            Assert.Equal(rotaAntes, f.RotaComoTexto(reserva));
        }

        [Fact(DisplayName = "RN13 · Falha na própria base não muda a rota de volta (a base é o destino)")]
        [Trait("Requisito", "RN13")]
        public void Definir_NaoDeveMudarARotaDeVolta_QuandoAFalhaEEmNaBase()
        {
            // Arrange: BOM-02 voltando de C1 para o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = ReservaVoltandoDe(f, "C1");
            var rotaAntes = f.RotaComoTexto(reserva);

            // Act: o semáforo da base entra em falha
            f.Falhas.Definir("C20", ativa: true);

            // Assert: não tem como evitar o destino, então a rota é a mesma
            Assert.Equal(rotaAntes, f.RotaComoTexto(reserva));
        }

        [Fact(DisplayName = "UC09 · Ao desligar a falha, quem voltava sem rota acha o caminho na hora")]
        [Trait("Requisito", "UC09")]
        [Trait("Requisito", "RN13")]
        public void Definir_DeveRefazerOCaminhoDeVolta_QuandoViaturaEstavaSemRota()
        {
            // Arrange: C15 e C19 em falha deixam BOM-02 sem rota de volta
            var f = CidadeFactory.Criar();
            f.Falhas.Definir("C15", ativa: true);
            f.Falhas.Definir("C19", ativa: true);
            var reserva = ReservaVoltandoDe(f, "C1");
            Assert.True(reserva.SemRota);

            // Act: a falha de C15 é desligada
            f.Falhas.Definir("C15", ativa: false);

            // Assert: achou o caminho sem precisar esperar o segundo
            Assert.False(reserva.SemRota);
            Assert.Equal("C2 → C3 → C4 → C5 → C10 → C15 → C20", f.RotaComoTexto(reserva));
        }

        [Fact(DisplayName = "RN13 · Em emergência e sem rota, ao entrar numa rua nova a viatura tenta o trecho de novo")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN10")]
        public void Saida_DeveTentarOTrechoDeNovo_QuandoViaturaEstavaSemRota()
        {
            // Arrange: a coluna C3, C8, C13 e C18 em falha deixa AMB-01 sem rota; depois C8 volta ao normal sem aviso
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C3", "C8", "C13", "C18");
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            Assert.True(ambulancia.SemRota);
            f.PegarCruzamento("C8").Semaforo.EmFalha = false;

            // Act: a ambulância entra na rua que vai para C6
            f.Frota.Saida("AMB-01", de: "C11", para: "C6");

            // Assert: com a nova posição, o caminho por C8 existe
            Assert.False(ambulancia.SemRota);
            Assert.Equal("C6 → C7 → C8 → C9 → C10 → C15", f.RotaComoTexto(ambulancia));
        }

        [Fact(DisplayName = "RN15 · Em retorno e sem rota, ao chegar num cruzamento qualquer a viatura refaz o caminho")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "RN13")]
        public void Chegada_DeveRefazerOCaminhoDeVolta_QuandoViaturaEstavaSemRota()
        {
            // Arrange: BOM-02 voltando e sem rota (C15 e C19 em falha)
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C15", "C19");
            var reserva = ReservaVoltandoDe(f, "C1");
            Assert.True(reserva.SemRota);

            // Act: a viatura chega ao centro de C2
            f.Frota.Chegada("BOM-02", "C2");

            // Assert: refez o caminho a partir de C2, passando pela falha, e terminou na base
            Assert.False(reserva.SemRota);
            Assert.Equal("C3 → C4 → C5 → C10 → C15 → C20", f.RotaComoTexto(reserva));
        }
    }
}
