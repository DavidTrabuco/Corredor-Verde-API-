using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Casos de borda do registro de emergências: viatura voltando para a base, configuração diferente,
    // falhas bloqueando o caminho e dados que chegam vazios pela API.
    public class OcorrenciaServiceBordasTests
    {
        [Fact(DisplayName = "RN15 · Quem estava voltando para a base pode ser chamada de novo e larga a rota de volta")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveChamarQuemVoltavaParaABase_QuandoElaEAMaisPerto()
        {
            // Arrange: BOM-02 (reserva) acabou de atender em C15 e está voltando para o Quartel (C20)
            var f = CidadeFactory.Criar();
            var reserva = f.ColocarEmEmergencia("BOM-02", de: "C15", ate: "C15", criticidade: 2);
            f.Frota.FimDoAtendimento(reserva);
            Assert.Equal(ModoViatura.Retorno, reserva.Modo);

            // Act: nova emergência de bombeiros em C14, ao lado de C15
            var ocorrencia = f.Emergencias.Registrar("Bombeiros", "C14", 2);

            // Assert: BOM-02 é a mais perto, deixa de voltar e vai à nova ocorrência
            Assert.Equal("BOM-02", ocorrencia.ViaturaId);
            Assert.Equal(ModoViatura.Emergencia, reserva.Modo);
            Assert.Equal("C14", f.RotaComoTexto(reserva));
            Assert.True(f.LogTem("BOM-02 é a viatura livre mais perto de C14 (1 quarteirão; BOM-01 a 2 quarteirões), estava voltando à base."));
        }

        [Fact(DisplayName = "UC03 · Sem nenhuma viatura do tipo pedido na configuração, a recusa explica isso")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RNF12")]
        public void Registrar_DeveRecusar_QuandoNaoHaViaturaDoTipoNaConfiguracao()
        {
            // Arrange: a configuração só tem ambulâncias
            var f = CidadeFactory.Criar(opcoes => opcoes.Viaturas.RemoveAll(v => v.Tipo == TipoViatura.Bombeiros));

            // Act
            var erro = Assert.Throws<ConflitoException>(() => f.Emergencias.Registrar("Bombeiros", "C15", 2));

            // Assert
            Assert.Equal("Não há viatura do tipo Bombeiros na configuração.", erro.Message);
            Assert.Empty(f.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "UC03 · Com uma só viatura do tipo e ela ocupada, a recusa fala no singular")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RNF04")]
        public void Registrar_DeveRecusar_QuandoAUnicaViaturaDoTipoEstaOcupada()
        {
            // Arrange: só existe o BOM-01, e ele já está numa emergência
            var f = CidadeFactory.Criar(opcoes => opcoes.Viaturas.RemoveAll(v => v.Id == "BOM-02"));
            f.Emergencias.Registrar("Bombeiros", "C15", 2);

            // Act
            var erro = Assert.Throws<ConflitoException>(() => f.Emergencias.Registrar("Bombeiros", "C8", 2));

            // Assert
            Assert.Equal("BOM-01 está ocupada. Espere ela terminar.", erro.Message);
            Assert.Single(f.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RN04 · Sem hospital configurado, a ambulância vai ao local e o log avisa que não há hospital")]
        [Trait("Requisito", "RN04")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveAvisarQueNaoHaHospital_QuandoACidadeNaoTemHospital()
        {
            // Arrange
            var f = CidadeFactory.Criar(opcoes => opcoes.Hospitais.Clear());

            // Act
            var ocorrencia = f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Assert: a ocorrência existe, mas sem hospital e sem trecho 2 previsto
            Assert.Null(ocorrencia.Hospital);
            Assert.Empty(f.PegarViatura("AMB-01").Previsao);
            Assert.True(f.LogTem("OC-001 registrada em C15 (criticidade 1) para AMB-01. Trechos: C11 → C15 → sem hospital alcançável."));
        }

        [Fact(DisplayName = "RN13 · Se as falhas bloqueiam todos os caminhos, a emergência é aceita e o log diz \"sem caminho\"")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN15")]
        public void Registrar_DeveAceitarAEmergencia_QuandoNenhumaViaturaTemCaminhoLivre()
        {
            // Arrange: os três acessos de C15 (C10, C14 e C20) estão em falha
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C10", "C14", "C20");

            // Act
            var ocorrencia = f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Assert: a AMB-01 é escolhida mesmo assim, e fica sem rota esperando (RN13)
            Assert.Equal("AMB-01", ocorrencia.ViaturaId);
            Assert.True(f.PegarViatura("AMB-01").SemRota);
            Assert.True(f.LogTem("AMB-01 é a viatura livre mais perto de C15 (sem caminho; AMB-02 a sem caminho)."));
            Assert.True(f.LogTem("Sem caminho de C11 até C15: semáforos em falha bloqueiam a passagem. AMB-01 foi avisada e aguarda."));
        }

        [Fact(DisplayName = "RNF04 · Sem informar o tipo da viatura, a emergência é recusada")]
        [Trait("Requisito", "RNF04")]
        public void Registrar_DeveRecusar_QuandoOTipoNaoFoiInformado()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert: a API deixa o campo chegar vazio de propósito, e o service é quem recusa
            Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar(null, "C15", 1));
            Assert.Empty(f.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RNF04 · Sem informar o cruzamento, a emergência é recusada")]
        [Trait("Requisito", "RNF04")]
        public void Registrar_DeveRecusar_QuandoOCruzamentoNaoFoiInformado()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert
            Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar("Ambulancia", null, 1));
            Assert.Empty(f.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RNF04 · Sem informar a criticidade, a emergência é recusada")]
        [Trait("Requisito", "RNF04")]
        public void Registrar_DeveRecusar_QuandoACriticidadeNaoFoiInformada()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert
            Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar("Ambulancia", "C15", null));
            Assert.Empty(f.Cidade.Ocorrencias);
        }
    }
}
