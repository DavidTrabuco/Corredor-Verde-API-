using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class FalhaServiceBordasTests
    {
        [Fact(DisplayName = "RN13 · Falha no cruzamento em que a ambulância já está entrando: ela segue com cautela, sem recalcular")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public void Definir_NaoDeveDesviarARota_QuandoAViaturaJaEstaEntrandoNoCruzamento()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15, já na rua que entra em C12
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            f.Frota.Saida("AMB-01", de: "C11", para: "C12");

            // Act: justo C12 entra em falha
            f.Falhas.Definir("C12", ativa: true);

            // Assert: não dá mais para parar no meio da rua, então a rota e o corredor ficam como estavam
            Assert.False(f.LogTem("A rota de AMB-01 passava por C12. Recalculando para desviar."));
            Assert.Equal("C12 → C13 → C14 → C15", f.RotaComoTexto(ambulancia));
        }
    }
}
