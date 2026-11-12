using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // A fronteira é o cruzamento em que a viatura JÁ está entrando: se ele falha, não dá para parar no meio da rua;
    // se falha um cruzamento DEPOIS dele, ainda dá tempo de desviar.
    public class FalhaServiceBordasTests
    {
        [Fact(DisplayName = "RN13 · Falha no cruzamento em que a ambulância já está entrando: ela segue com cautela e a rota continua depois da chegada")]
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

            // Act: a ambulância chega ao centro de C12 (passa com cautela)
            f.Frota.Chegada("AMB-01", "C12");

            // Assert: a viatura segue pelo resto da rota, sem recalcular nem ficar sem caminho
            Assert.Equal("C13 → C14 → C15", f.RotaComoTexto(ambulancia));
            Assert.False(ambulancia.SemRota);
            Assert.False(f.LogTem("A rota de AMB-01 passava por C12. Recalculando para desviar."));
        }

        [Fact(DisplayName = "RN13 · Falha num cruzamento depois do que a ambulância está entrando: a rota é recalculada para desviar")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN05")]
        [Trait("Requisito", "UC09")]
        public void Definir_DeveDesviarARota_QuandoAFalhaEDepoisDoCruzamentoEmQueAViaturaEntra()
        {
            // Arrange: AMB-01 com o corredor aberto de C11 a C15, já na rua que entra em C12
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);
            f.Frota.Saida("AMB-01", de: "C11", para: "C12");

            // Act: C13, que vem depois de C12, entra em falha
            f.Falhas.Definir("C13", ativa: true);

            // Assert: ainda dá tempo de desviar; a rota continua começando em C12 (a rua em que ela já está) e evita C13
            Assert.True(f.LogTem("A rota de AMB-01 passava por C13. Recalculando para desviar."));
            Assert.Equal("C12 → C7 → C8 → C9 → C10 → C15", f.RotaComoTexto(ambulancia));
            Assert.Equal("nenhuma", f.ReservasComoTexto("C13"));
        }
    }
}
