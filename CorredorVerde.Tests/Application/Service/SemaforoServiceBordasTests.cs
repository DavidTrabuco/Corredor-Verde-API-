using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class SemaforoServiceBordasTests
    {
        [Fact(DisplayName = "RN07 · Viatura que a cidade não conhece espera no vermelho, como qualquer veículo comum")]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DeveBarrar_QuandoAViaturaNaoExiste()
        {
            // Arrange: em 0 s, C1 está com vermelho para Leste-Oeste
            var f = CidadeFactory.Criar();

            // Act: a maquete pergunta por uma viatura que não existe
            var resultado = f.Semaforo.PodeAtravessar("C1", Eixo.LO, "XYZ-99");

            // Assert: não passa, e a espera não vai para o log (só conta)
            Assert.False(resultado.Pode);
            Assert.False(resultado.Cautela);
            Assert.Equal(Luz.Vermelho, resultado.Luz);
            Assert.Empty(f.Cidade.Log);
        }
    }
}
