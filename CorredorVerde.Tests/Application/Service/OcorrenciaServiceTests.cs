using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Aqui a ocorrência é registrada de verdade (Emergencias.Registrar), do jeito que a API faz.
    public class OcorrenciaServiceTests
    {
        [Fact(DisplayName = "RN15 · Com a mesma distância, a viatura da patrulha atende antes da reserva")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveEscolherAPatrulha_QuandoADistanciaEmpata()
        {
            // Arrange: AMB-01 (patrulha) e AMB-02 (reserva) estão as duas na Base do SAMU (C11)
            var f = CidadeFactory.Criar();

            // Act: emergência em C15, a 4 quarteirões das duas
            var ocorrencia = f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Assert: vai a AMB-01, AMB-02 continua livre na base, e o log explica a escolha
            Assert.Equal("AMB-01", ocorrencia.ViaturaId);
            Assert.Equal(ModoViatura.Emergencia, f.PegarViatura("AMB-01").Modo);
            Assert.Equal(ModoViatura.Reserva, f.PegarViatura("AMB-02").Modo);
            Assert.Equal("C11", f.PegarViatura("AMB-02").Cruzamento);
            Assert.Single(f.Cidade.Ocorrencias);
            Assert.True(f.LogTem("AMB-01 é a viatura livre mais perto de C15 (4 quarteirões; AMB-02 a 4 quarteirões)."));
        }

        [Fact(DisplayName = "RN15 · Vai a viatura livre mais perto do local, mesmo sendo a de reserva")]
        [Trait("Requisito", "RN15")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveEscolherAMaisPerto_QuandoReservaEstaMaisPerto()
        {
            // Arrange: AMB-02 (reserva) está em C14, ao lado do local; AMB-01 está na base (C11)
            var f = CidadeFactory.Criar();
            f.PegarViatura("AMB-02").Cruzamento = "C14";

            // Act: emergência em C15
            var ocorrencia = f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Assert: vai a AMB-02, que sai da reserva
            Assert.Equal("AMB-02", ocorrencia.ViaturaId);
            Assert.True(f.LogTem("AMB-02 é a viatura livre mais perto de C15 (1 quarteirão; AMB-01 a 4 quarteirões), sai da reserva."));
        }

        [Fact(DisplayName = "RN04 · A ambulância já nasce com o hospital mais próximo definido")]
        [Trait("Requisito", "RN04")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveDefinirOHospital_QuandoEAmbulancia()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: emergência em C15; o hospital mais perto é o Hospital Sul (C18)
            var ocorrencia = f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Assert
            Assert.Equal("Hospital Sul", ocorrencia.Hospital?.Nome);
            Assert.True(f.LogTem("OC-001 registrada em C15 (criticidade 1) para AMB-01. Trechos: C11 → C15 → Hospital Sul."));
        }

        [Fact(DisplayName = "RN11 · Os bombeiros vão só até o local, sem hospital")]
        [Trait("Requisito", "RN11")]
        [Trait("Requisito", "UC03")]
        public void Registrar_NaoDeveDefinirHospital_QuandoSaoBombeiros()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var ocorrencia = f.Emergencias.Registrar("Bombeiros", "C15", 2);

            // Assert
            Assert.Equal("BOM-01", ocorrencia.ViaturaId);
            Assert.Null(ocorrencia.Hospital);
            Assert.True(f.LogTem("OC-001 registrada em C15 (criticidade 2) para BOM-01. Bombeiros vão só até o local."));
        }

        [Fact(DisplayName = "UC03 · Sem viatura livre do tipo pedido, a emergência é recusada")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "RNF04")]
        public void Registrar_DeveRecusar_QuandoNaoHaViaturaLivre()
        {
            // Arrange: as duas ambulâncias já estão em emergência
            var f = CidadeFactory.Criar();
            f.Emergencias.Registrar("Ambulancia", "C15", 1);
            f.Emergencias.Registrar("Ambulancia", "C8", 1);

            // Act
            var erro = Assert.Throws<ConflitoException>(() => f.Emergencias.Registrar("Ambulancia", "C5", 1));

            // Assert: a mensagem diz quem está ocupado e nenhuma ocorrência nova foi criada
            Assert.Equal("AMB-01 e AMB-02 estão ocupadas. Espere uma delas terminar.", erro.Message);
            Assert.Equal(2, f.Cidade.Ocorrencias.Count);
        }

        [Fact(DisplayName = "RNF04 · Criticidade fora de 1 a 3 é recusada sem mexer na cidade")]
        [Trait("Requisito", "RNF04")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveRecusar_QuandoCriticidadeEInvalida()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var erro = Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar("Ambulancia", "C15", 4));

            // Assert: nada foi alterado
            Assert.Equal("Criticidade 4 inválida: use 1, 2 ou 3.", erro.Message);
            Assert.Empty(f.Cidade.Ocorrencias);
            Assert.Equal(ModoViatura.Patrulha, f.PegarViatura("AMB-01").Modo);
        }

        [Fact(DisplayName = "RNF04 · Tipo de viatura desconhecido é recusado")]
        [Trait("Requisito", "RNF04")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveRecusar_QuandoTipoEInvalido()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var erro = Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar("Helicoptero", "C15", 1));

            // Assert
            Assert.Equal("Tipo de viatura Helicoptero inválido: use Ambulancia ou Bombeiros.", erro.Message);
            Assert.Empty(f.Cidade.Ocorrencias);
        }

        [Fact(DisplayName = "RNF04 · Cruzamento que não existe na malha é recusado")]
        [Trait("Requisito", "RNF04")]
        [Trait("Requisito", "UC03")]
        public void Registrar_DeveRecusar_QuandoCruzamentoNaoExiste()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var erro = Assert.Throws<ValidacaoException>(() => f.Emergencias.Registrar("Ambulancia", "C99", 1));

            // Assert
            Assert.Equal("Cruzamento C99 não existe na malha.", erro.Message);
            Assert.Empty(f.Cidade.Ocorrencias);
        }
    }
}
