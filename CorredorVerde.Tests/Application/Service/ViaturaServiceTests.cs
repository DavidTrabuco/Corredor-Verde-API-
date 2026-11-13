using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // A ocorrência é montada à mão (ColocarEmEmergencia), para testar só a viatura.
    public class ViaturaServiceTests
    {
        [Fact(DisplayName = "RN09 · Ao chegar ao local da emergência, a viatura começa 4 s de atendimento")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void ChegouAoDestino_DeveComecarOAtendimento_QuandoChegaAoLocalDaEmergencia()
        {
            // Arrange: AMB-01 saiu da Base do SAMU (C11) para a ocorrência em C15, e o relógio já está em 10 s
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];
            f.Cidade.TempoSeg = 10.0;

            // Act: a ambulância chega a C15
            f.Frota.ChegouAoDestino(ambulancia);

            // Assert: está em atendimento até 4 s depois de agora (10 + 4), e a ocorrência também
            Assert.Equal(ModoViatura.Atendimento, ambulancia.Modo);
            Assert.Equal(14.0, ambulancia.AtendimentoAteSeg);
            Assert.Equal(StatusOcorrencia.EmAtendimento, ocorrencia.Status);
            Assert.True(f.LogTem("AMB-01 chegou ao local de OC-001 (C15). Atendimento de 4 s."));
        }

        [Fact(DisplayName = "RN09 · Depois dos 4 s no local, a ambulância segue ao hospital mais próximo")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "RN04")]
        [Trait("Requisito", "UC05")]
        public void FimDoAtendimento_DeveSeguirAoHospital_QuandoEAmbulancia()
        {
            // Arrange: AMB-01 está atendendo em C15; o hospital mais perto é o Hospital Sul (C18)
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C15", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];

            // Act: terminam os 4 s de atendimento
            f.Frota.FimDoAtendimento(ambulancia);

            // Assert: a ambulância vai para o hospital (trecho 2) e a ocorrência acompanha
            Assert.Equal(ModoViatura.Emergencia, ambulancia.Modo);
            Assert.Equal(2, ambulancia.Trecho);
            Assert.Equal("C18", ambulancia.Destino);
            Assert.Equal(StatusOcorrencia.ACaminhoHospital, ocorrencia.Status);
            Assert.True(f.LogTem("Atendimento de OC-001 concluído. AMB-01 segue ao Hospital Sul, o hospital mais próximo."));
        }

        [Fact(DisplayName = "RN09 · Ao chegar ao hospital, a ocorrência é encerrada e a ambulância volta a patrulhar")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void ChegouAoDestino_DeveEncerrarAOcorrencia_QuandoChegaAoHospital()
        {
            // Arrange: AMB-01 está no segundo trecho, indo para o Hospital Sul (C18)
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C18", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];
            ocorrencia.Hospital = f.Cidade.Hospitais[1];
            ambulancia.Trecho = 2;
            ambulancia.Destino = "C18";
            f.Cidade.TempoSeg = 25.0;

            // Act: a ambulância chega ao hospital, com o relógio em 25 s
            f.Frota.ChegouAoDestino(ambulancia);

            // Assert: ocorrência encerrada agora (25 s), e a ambulância libera para patrulhar
            Assert.Equal(StatusOcorrencia.Encerrada, ocorrencia.Status);
            Assert.Equal(25.0, ocorrencia.EncerradaEmSeg);
            Assert.Equal(ModoViatura.Patrulha, ambulancia.Modo);
            Assert.True(f.LogTem("AMB-01 chegou ao Hospital Sul (C18). OC-001 encerrada."));
        }

        [Fact(DisplayName = "RN11 · Os bombeiros encerram a ocorrência no local e não vão ao hospital")]
        [Trait("Requisito", "RN11")]
        [Trait("Requisito", "UC05")]
        public void FimDoAtendimento_DeveEncerrarNoLocal_QuandoSaoBombeiros()
        {
            // Arrange: BOM-01 está atendendo em C15
            var f = CidadeFactory.Criar();
            var bombeiros = f.ColocarEmEmergencia("BOM-01", de: "C15", ate: "C15", criticidade: 2);
            var ocorrencia = f.Cidade.Ocorrencias[0];

            // Act: terminam os 4 s de atendimento
            f.Frota.FimDoAtendimento(bombeiros);

            // Assert: ocorrência encerrada ali mesmo e os bombeiros voltam a patrulhar
            Assert.Equal(StatusOcorrencia.Encerrada, ocorrencia.Status);
            Assert.Equal(ModoViatura.Patrulha, bombeiros.Modo);
            Assert.True(f.LogTem("BOM-01 encerrou OC-001 no local. Bombeiros não seguem ao hospital."));
        }

        [Fact(DisplayName = "RN10 · Se a viatura entra numa rua fora da rota, o trecho é recalculado")]
        [Trait("Requisito", "RN10")]
        [Trait("Requisito", "UC07")]
        public void Saida_DeveRecalcularARota_QuandoViaturaEntraEmRuaForaDaRota()
        {
            // Arrange: AMB-01 em C11 com o corredor aberto para C15; a rota pede C12 primeiro
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act: em vez de ir para C12, a ambulância sobe para C6
            f.Frota.Saida("AMB-01", de: "C11", para: "C6");

            // Assert: o log avisa do desvio e a nova rota começa em C6
            Assert.True(f.LogTem("AMB-01 saiu da rota em C11 (foi para C6, a rota pedia C12). Recalculando o trecho."));
            Assert.Equal("C6", ambulancia.Rota[0].Cruzamento);
        }

        [Fact(DisplayName = "RN15 · A viatura de reserva volta para a base sem corredor verde")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC05")]
        public void FimDoAtendimento_DeveVoltarParaABase_QuandoViaturaEDeReserva()
        {
            // Arrange: BOM-02 (reserva) está atendendo em C15; a base dela é o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);

            // Act: terminam os 4 s de atendimento
            f.Frota.FimDoAtendimento(reserva);

            // Assert: volta para a base pelo caminho mais curto, sem reservar nenhum cruzamento
            Assert.Equal(ModoViatura.Retorno, reserva.Modo);
            Assert.Equal("C20", reserva.Destino);
            Assert.Equal("C20", f.RotaComoTexto(reserva));
            Assert.True(f.NenhumCruzamentoReservado());
            Assert.True(f.LogTem("BOM-02 é de reserva: terminou o serviço e volta para o Quartel dos Bombeiros, sem corredor verde."));
        }

        [Fact(DisplayName = "RN15 · Ao chegar na base, a viatura de reserva fica de reserva")]
        [Trait("Requisito", "RN15")]
        public void Chegada_DeveFicarDeReserva_QuandoViaturaChegaNaBase()
        {
            // Arrange: BOM-02 (reserva) já está voltando para o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);
            f.Frota.FimDoAtendimento(reserva);

            // Act: a viatura chega ao Quartel
            f.Frota.Chegada("BOM-02", "C20");

            // Assert: fica de reserva, sem destino
            Assert.Equal(ModoViatura.Reserva, reserva.Modo);
            Assert.Null(reserva.Destino);
            Assert.True(f.LogTem("BOM-02 chegou ao Quartel dos Bombeiros e ficou de reserva."));
        }

        [Fact(DisplayName = "RN08 · Ao passar por um cruzamento do corredor, ele é liberado e volta ao ciclo normal")]
        [Trait("Requisito", "RN08")]
        [Trait("Requisito", "UC06")]
        public void Chegada_DeveLiberarOCruzamento_QuandoPassaPeloCruzamentoDaRota()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15 (C12, C13, C14 e C15 reservados)
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act: a ambulância chega ao centro de C12
            f.Frota.Chegada("AMB-01", "C12");

            // Assert: C12 foi liberado e saiu da rota; C13 continua reservado
            Assert.Equal("nenhuma", f.ReservasComoTexto("C12"));
            Assert.Equal("AMB-01 (LO)", f.ReservasComoTexto("C13"));
            Assert.Equal("C13 → C14 → C15", f.RotaComoTexto(ambulancia));
            Assert.True(f.LogTem("AMB-01 passou por C12: cruzamento liberado, volta ao ciclo normal."));
        }

        [Fact(DisplayName = "RN10 · Se a viatura chega a um cruzamento fora da rota, o trecho é recalculado")]
        [Trait("Requisito", "RN10")]
        [Trait("Requisito", "UC07")]
        public void Chegada_DeveRecalcularARota_QuandoChegaAUmCruzamentoForaDaRota()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15; a rota pede C12 primeiro
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act: a ambulância aparece em C6, que não estava na rota
            f.Frota.Chegada("AMB-01", "C6");

            // Assert: o log avisa e a rota nova sai de C6 até C15
            Assert.True(f.LogTem("AMB-01 chegou a C6, fora da rota. Recalculando o trecho."));
            Assert.Equal("C7 → C8 → C9 → C10 → C15", f.RotaComoTexto(ambulancia));
        }

        [Fact(DisplayName = "RN09 · Ao chegar ao cruzamento da ocorrência pela rota, começa o atendimento")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void Chegada_DeveComecarOAtendimento_QuandoChegaAoDestinoPelaRota()
        {
            // Arrange: AMB-01 em C14, com o corredor aberto até C15
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C14", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];
            f.Frota.IniciarTrecho(ambulancia);

            // Act: a ambulância chega ao centro de C15
            f.Frota.Chegada("AMB-01", "C15");

            // Assert: começou o atendimento e o corredor foi todo liberado
            Assert.Equal(ModoViatura.Atendimento, ambulancia.Modo);
            Assert.Equal(StatusOcorrencia.EmAtendimento, ocorrencia.Status);
            Assert.True(f.NenhumCruzamentoReservado());
        }

        [Fact(DisplayName = "RN15 · Quem volta para a base e pega outra rua tem o caminho de volta refeito")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC07")]
        public void Saida_DeveRefazerOCaminhoDeVolta_QuandoViaturaEmRetornoPegaOutraRua()
        {
            // Arrange: BOM-02 (reserva) está voltando de C15 para o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);
            f.Frota.FimDoAtendimento(reserva);

            // Act: em vez de ir para C20, a viatura sai para C14
            f.Frota.Saida("BOM-02", de: "C15", para: "C14");

            // Assert: continua voltando, mas a rota nova começa em C14 e ainda termina na base
            Assert.Equal(ModoViatura.Retorno, reserva.Modo);
            Assert.Equal("C14 → C15 → C20", f.RotaComoTexto(reserva));
            Assert.True(f.NenhumCruzamentoReservado());
        }

        [Fact(DisplayName = "RN09 · Sem hospital configurado, a ambulância encerra a ocorrência no local")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void FimDoAtendimento_DeveEncerrarNoLocal_QuandoNaoHaHospital()
        {
            // Arrange: AMB-01 está atendendo em C15 e a cidade não tem nenhum hospital
            var f = CidadeFactory.Criar();
            f.Cidade.Hospitais.Clear();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C15", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];

            // Act: terminam os 4 s de atendimento
            f.Frota.FimDoAtendimento(ambulancia);

            // Assert: a ocorrência é encerrada ali e a ambulância volta a patrulhar
            Assert.Equal(StatusOcorrencia.Encerrada, ocorrencia.Status);
            Assert.Equal(ModoViatura.Patrulha, ambulancia.Modo);
            Assert.True(f.LogTem("AMB-01 encerrou OC-001 no local: não há hospital configurado."));
        }

        [Fact(DisplayName = "RN09 · Viatura em atendimento não pode sair do cruzamento")]
        [Trait("Requisito", "RNF04")]
        [Trait("Requisito", "RN09")]
        public void Saida_DeveRecusar_QuandoViaturaEstaEmAtendimento()
        {
            // Arrange: BOM-01 chegou ao local e está atendendo em C15
            var f = CidadeFactory.Criar();
            var bombeiros = f.ColocarEmEmergencia("BOM-01", de: "C15", ate: "C15", criticidade: 2);
            f.Frota.ChegouAoDestino(bombeiros);

            // Act
            var erro = Assert.Throws<ConflitoException>(() => f.Frota.Saida("BOM-01", "C15", "C14"));

            // Assert
            Assert.Equal("BOM-01 está em atendimento.", erro.Message);
            Assert.Null(bombeiros.IndoPara);
        }

        [Fact(DisplayName = "RN09 · Se a viatura já está no cruzamento da ocorrência, o atendimento começa direto")]
        [Trait("Requisito", "RN09")]
        [Trait("Requisito", "UC05")]
        public void IniciarTrecho_DeveComecarOAtendimento_QuandoJaEstaNoDestino()
        {
            // Arrange: BOM-01 já está parado em C15, onde fica a ocorrência
            var f = CidadeFactory.Criar();
            var bombeiros = f.ColocarEmEmergencia("BOM-01", de: "C15", ate: "C15", criticidade: 2);
            var ocorrencia = f.Cidade.Ocorrencias[0];

            // Act
            f.Frota.IniciarTrecho(bombeiros);

            // Assert: não abriu corredor nenhum e já está atendendo
            Assert.Equal(ModoViatura.Atendimento, bombeiros.Modo);
            Assert.Equal(StatusOcorrencia.EmAtendimento, ocorrencia.Status);
            Assert.True(f.NenhumCruzamentoReservado());
        }

        [Fact(DisplayName = "UC01 · Viatura em patrulha que chega a um cruzamento só tem a posição atualizada")]
        [Trait("Requisito", "UC01")]
        public void Chegada_DeveSoAtualizarAPosicao_QuandoAViaturaEstaEmPatrulha()
        {
            // Arrange: AMB-01 está em patrulha, na base (C11), indo para C12
            var f = CidadeFactory.Criar();
            f.Frota.Saida("AMB-01", de: "C11", para: "C12");

            // Act: chega ao centro de C12
            f.Frota.Chegada("AMB-01", "C12");

            // Assert: está em C12, parada, ainda em patrulha, e nada foi para o log
            var ambulancia = f.PegarViatura("AMB-01");
            Assert.Equal("C12", ambulancia.Cruzamento);
            Assert.Null(ambulancia.IndoPara);
            Assert.Equal(ModoViatura.Patrulha, ambulancia.Modo);
            Assert.Empty(f.Cidade.Log);
        }
    }
}
