using CorredorVerde.Application.Service;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Casos de borda do ViaturaService: situações menos comuns, mas que a maquete pode provocar.
    public class ViaturaServiceBordasTests
    {
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

        [Fact(DisplayName = "RN15 · A reserva que termina o serviço já dentro da base fica de reserva na hora")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC05")]
        public void FimDoAtendimento_DeveFicarDeReserva_QuandoOLocalDaOcorrenciaEAPropriaBase()
        {
            // Arrange: BOM-02 (reserva) atende uma ocorrência no próprio Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C20", ate: "C20", criticidade: 2);

            // Act
            f.Frota.FimDoAtendimento(reserva);

            // Assert: não precisa voltar, porque já está na base
            Assert.Equal(ModoViatura.Reserva, reserva.Modo);
            Assert.Empty(reserva.Rota);
            Assert.Null(reserva.Destino);
            Assert.True(f.LogTem("BOM-02 chegou ao Quartel dos Bombeiros e ficou de reserva."));
        }

        [Fact(DisplayName = "RN15 · A viatura de patrulha que passa pela base volta a patrulhar")]
        [Trait("Requisito", "RN15")]
        public void ChegouNaBase_DeveVoltarAPatrulhar_QuandoEViaturaDePatrulha()
        {
            // Arrange: AMB-01 é de patrulha e a base dela é a Base do SAMU (C11)
            var f = CidadeFactory.Criar();
            var ambulancia = f.PegarViatura("AMB-01");

            // Act
            f.Frota.ChegouNaBase(ambulancia);

            // Assert
            Assert.Equal(ModoViatura.Patrulha, ambulancia.Modo);
            Assert.True(f.LogTem("AMB-01 passou pela Base do SAMU e voltou a patrulhar."));
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

        [Fact(DisplayName = "RN15 · Quem volta para a base e entra na rua da rota não tem o caminho refeito")]
        [Trait("Requisito", "RN15")]
        public void Saida_NaoDeveRefazerOCaminhoDeVolta_QuandoAViaturaEntraNaRuaDaRota()
        {
            // Arrange: BOM-02 voltando de C15 para o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);
            f.Frota.FimDoAtendimento(reserva);
            Assert.Equal("C20", f.RotaComoTexto(reserva));

            // Act: a viatura sai de C15 pela rua certa (para C20)
            f.Frota.Saida("BOM-02", de: "C15", para: "C20");

            // Assert: continua voltando com a mesma rota, sem recalcular
            Assert.Equal(ModoViatura.Retorno, reserva.Modo);
            Assert.Equal("C20", f.RotaComoTexto(reserva));
        }

        [Fact(DisplayName = "RN13 · Sem rota e fora do caminho, a viatura em emergência não recalcula a cada chegada")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN10")]
        public void Chegada_NaoDeveRecalcular_QuandoAViaturaEstaSemRota()
        {
            // Arrange: a coluna C3, C8, C13 e C18 em falha deixa AMB-01 sem rota
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C3", "C8", "C13", "C18");
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            Assert.True(ambulancia.SemRota);

            // Act: a ambulância chega a C12
            f.Frota.Chegada("AMB-01", "C12");

            // Assert: não houve aviso de rota errada; continua esperando o temporizador tentar de novo
            Assert.False(f.LogTem("AMB-01 chegou a C12, fora da rota. Recalculando o trecho."));
            Assert.True(ambulancia.SemRota);
        }

        [Fact(DisplayName = "RN09 · Ao chegar ao hospital sem ocorrência com hospital definido, o log usa o cruzamento")]
        [Trait("Requisito", "RN09")]
        public void ChegouAoDestino_DeveUsarOCruzamentoNoLog_QuandoAOcorrenciaNaoTemHospital()
        {
            // Arrange: AMB-01 está no trecho 2 em C18, mas a ocorrência não tem hospital guardado
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C18", ate: "C15", criticidade: 1);
            ambulancia.Trecho = 2;
            ambulancia.Destino = "C18";

            // Act
            f.Frota.ChegouAoDestino(ambulancia);

            // Assert: a ocorrência é encerrada e o log fala do cruzamento onde a viatura está
            Assert.Equal(StatusOcorrencia.Encerrada, f.Cidade.Ocorrencias[0].Status);
            Assert.True(f.LogTem("AMB-01 chegou ao C18. OC-001 encerrada."));
        }

        [Fact(DisplayName = "RN15 · Sem a base na configuração, a volta cita só o cruzamento da base")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "RNF12")]
        public void FimDoAtendimento_DeveCitarOCruzamentoDaBase_QuandoABaseNaoEstaNaConfiguracao()
        {
            // Arrange: a configuração não tem nenhuma base, então as viaturas ficam em C1
            var f = CidadeFactory.Criar(opcoes => opcoes.Bases.Clear());
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);

            // Act
            f.Frota.FimDoAtendimento(reserva);

            // Assert
            Assert.True(f.LogTem("BOM-02 é de reserva: terminou o serviço e volta para a base C1, sem corredor verde."));
        }

        [Fact(DisplayName = "RNF04 · Terminar o atendimento sem ocorrência é erro de programa, não de dado")]
        [Trait("Requisito", "RNF04")]
        public void FimDoAtendimento_DeveFalhar_QuandoAViaturaNaoTemOcorrencia()
        {
            // Arrange: AMB-01 está em patrulha, sem ocorrência nenhuma
            var f = CidadeFactory.Criar();
            var ambulancia = f.PegarViatura("AMB-01");

            // Act
            var erro = Assert.Throws<InvalidOperationException>(() => f.Frota.FimDoAtendimento(ambulancia));

            // Assert
            Assert.Equal("AMB-01 terminou um atendimento sem ocorrência.", erro.Message);
        }

        [Fact(DisplayName = "RNF04 · Chegar ao destino sem ocorrência é erro de programa, não de dado")]
        [Trait("Requisito", "RNF04")]
        public void ChegouAoDestino_DeveFalhar_QuandoAViaturaNaoTemOcorrencia()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var ambulancia = f.PegarViatura("AMB-01");

            // Act
            var erro = Assert.Throws<InvalidOperationException>(() => f.Frota.ChegouAoDestino(ambulancia));

            // Assert
            Assert.Equal("AMB-01 chegou ao destino sem ocorrência.", erro.Message);
        }

        [Theory(DisplayName = "RN15 · O nome da base ganha a preposição certa (a, por, para, em)")]
        [Trait("Requisito", "RN15")]
        [InlineData("Base do SAMU", "a", "à Base do SAMU")]
        [InlineData("Quartel dos Bombeiros", "a", "ao Quartel dos Bombeiros")]
        [InlineData("Base do SAMU", "por", "pela Base do SAMU")]
        [InlineData("Quartel dos Bombeiros", "por", "pelo Quartel dos Bombeiros")]
        [InlineData("Base do SAMU", "para", "para a Base do SAMU")]
        [InlineData("Quartel dos Bombeiros", "para", "para o Quartel dos Bombeiros")]
        [InlineData("Base do SAMU", "em", "na Base do SAMU")]
        [InlineData("Quartel dos Bombeiros", "em", "no Quartel dos Bombeiros")]
        [InlineData("Base do SAMU", "de", "de Base do SAMU")]
        public void NomeBase_DeveUsarAPreposicaoCerta_QuandoOGeneroMuda(string nome, string preposicao, string esperado)
        {
            // Act
            var texto = ViaturaService.NomeBase(nome, preposicao);

            // Assert
            Assert.Equal(esperado, texto);
        }
    }
}
