using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class LogEventosServiceTests
    {
        [Fact(DisplayName = "RN14 · Cada evento do log ganha um número em sequência, e cada regra citada é contada")]
        [Trait("Requisito", "RN14")]
        public void Registrar_DeveNumerarOsEventosEContarAsTags()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: as "tags" são as regras que o evento cumpre
            var primeiro = f.Log.Registrar(new[] { "RN06", "UC04" }, "Corredor aberto.");
            var segundo = f.Log.Registrar(new[] { "RN06" }, "Outro corredor.");

            // Assert
            Assert.Equal(1, primeiro.Seq);
            Assert.Equal(2, segundo.Seq);
            Assert.Equal(2, f.Log.ObterContagem("RN06"));
            Assert.Equal(1, f.Log.ObterContagem("UC04"));
        }

        [Fact(DisplayName = "RN14 · O evento guarda o momento da simulação em que aconteceu")]
        [Trait("Requisito", "RN14")]
        public void Registrar_DeveGuardarOTempoSimuladoDoEvento()
        {
            // Arrange: a simulação está em 41,2 s
            var f = CidadeFactory.Criar();
            f.Cidade.TempoSeg = 41.2;

            // Act
            var evento = f.Log.Registrar(new[] { "RN15" }, "Viatura escolhida.");

            // Assert
            Assert.Equal(41.2, evento.TempoSeg);
        }

        [Fact(DisplayName = "UC10 · A maquete pede “só os eventos depois do número 3” e recebe o 4 e o 5")]
        [Trait("Requisito", "UC10")]
        public void Listar_DeveRetornarSoOsEventosDepoisDoSeqInformado_EmOrdemCrescente()
        {
            // Arrange: 5 eventos, numerados de 1 a 5
            var f = CidadeFactory.Criar();
            for (var i = 0; i < 5; i++)
                f.Log.Registrar(new[] { "UC10" }, "Evento de teste.");

            // Act
            var lista = f.Log.Listar(desde: 3);

            // Assert
            Assert.Equal(2, lista.Count);
            Assert.Equal(4, lista[0].Seq);
            Assert.Equal(5, lista[1].Seq);
        }
    }
}
