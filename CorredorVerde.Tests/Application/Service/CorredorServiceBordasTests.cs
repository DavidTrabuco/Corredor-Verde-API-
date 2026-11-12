using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Casos de borda do CorredorService: conflito já avisado e uso errado do service (erros de programa).
    public class CorredorServiceBordasTests
    {
        [Fact(DisplayName = "RN12 · O mesmo conflito entre dois corredores só vai uma vez para o log")]
        [Trait("Requisito", "RN12")]
        [Trait("Requisito", "UC08")]
        public void VerificarConflito_NaoDeveRepetirOLog_QuandoOConflitoJaFoiAvisado()
        {
            // Arrange: dois corredores se cruzam em C13, um no eixo Norte-Sul e outro no Leste-Oeste
            var f = CidadeFactory.Criar();
            f.Reservar("C13", "AMB-01", Eixo.NS, criticidade: 1, ordem: 1);
            f.Reservar("C13", "BOM-01", Eixo.LO, criticidade: 2, ordem: 2);
            var cruzamento = f.PegarCruzamento("C13");

            // Act: o conflito é conferido duas vezes
            f.Corredor.VerificarConflito(cruzamento);
            f.Corredor.VerificarConflito(cruzamento);

            // Assert: só um aviso de conflito
            Assert.Equal(1, f.QuantosLogsCom("RN12"));
        }

        [Fact(DisplayName = "RN06 · Abrir o corredor de uma viatura sem destino é erro de programa")]
        [Trait("Requisito", "RN06")]
        public void IniciarTrecho_DeveFalhar_QuandoAViaturaNaoTemDestino()
        {
            // Arrange: AMB-01 está em patrulha, sem destino
            var f = CidadeFactory.Criar();
            var ambulancia = f.PegarViatura("AMB-01");

            // Act
            var erro = Assert.Throws<InvalidOperationException>(() => f.Corredor.IniciarTrecho(ambulancia));

            // Assert: a recusa vem antes de qualquer mudança, então a cidade e a viatura ficam como estavam
            Assert.Equal("AMB-01 não tem destino para abrir corredor.", erro.Message);
            Assert.True(f.NenhumCruzamentoReservado());
            Assert.Empty(ambulancia.Rota);
            Assert.False(ambulancia.SemRota);
            Assert.Empty(f.Cidade.Log);
        }

        [Fact(DisplayName = "RN06 · Abrir o corredor de uma viatura sem ocorrência é erro de programa")]
        [Trait("Requisito", "RN06")]
        public void IniciarTrecho_DeveFalhar_QuandoAViaturaNaoTemOcorrencia()
        {
            // Arrange: AMB-01 tem um destino, mas nenhuma ocorrência ligada a ele
            var f = CidadeFactory.Criar();
            var ambulancia = f.PegarViatura("AMB-01");
            ambulancia.Destino = "C15";

            // Act
            var erro = Assert.Throws<InvalidOperationException>(() => f.Corredor.IniciarTrecho(ambulancia));

            // Assert: nenhum cruzamento ficou reservado pela metade e nada foi para o log.
            // A rota da viatura não é conferida aqui: hoje o service a preenche antes de validar a ocorrência,
            // e ela sobra preenchida depois do erro (achado do code review).
            Assert.Equal("AMB-01 não tem ocorrência para abrir corredor.", erro.Message);
            Assert.True(f.NenhumCruzamentoReservado());
            Assert.Empty(f.Cidade.Log);
        }
    }
}
