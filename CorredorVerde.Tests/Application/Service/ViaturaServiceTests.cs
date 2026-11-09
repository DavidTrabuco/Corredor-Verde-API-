using CorredorVerde.Domain.Enums;
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
            // Arrange: AMB-01 saiu da Base do SAMU (C11) para a ocorrência em C15
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            var ocorrencia = f.Cidade.Ocorrencias[0];

            // Act: a ambulância chega a C15
            f.Frota.ChegouAoDestino(ambulancia);

            // Assert: está em atendimento até 4 s depois, e a ocorrência também
            Assert.Equal(ModoViatura.Atendimento, ambulancia.Modo);
            Assert.Equal(4.0, ambulancia.AtendimentoAteSeg);
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

            // Act: a ambulância chega ao hospital
            f.Frota.ChegouAoDestino(ambulancia);

            // Assert: ocorrência encerrada agora, e a ambulância libera para patrulhar
            Assert.Equal(StatusOcorrencia.Encerrada, ocorrencia.Status);
            Assert.Equal(0.0, ocorrencia.EncerradaEmSeg);
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
    }
}
