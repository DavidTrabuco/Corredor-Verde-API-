using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class LogEventosServiceTests
    {
        [Fact]
        [Trait("Requisito", "RN14")]
        public void Registrar_DeveNumerarOsEventosEContarAsTags()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var primeiro = f.Log.Registrar(new[] { "RN06", "UC04" }, "Corredor aberto.");
            var segundo = f.Log.Registrar(new[] { "RN06" }, "Outro corredor.");

            // Assert
            Assert.Equal(1, primeiro.Seq);
            Assert.Equal(2, segundo.Seq);
            Assert.Equal(2, f.Log.ObterContagem("RN06"));
            Assert.Equal(1, f.Log.ObterContagem("UC04"));
            Assert.Equal(2, f.Log.ObterContagem("RN14"));
        }

        [Fact]
        [Trait("Requisito", "RN14")]
        public void Registrar_NaoDeveContarRN14_QuandoEChamadaDaApi()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            f.Log.Registrar(new[] { "API", "UC03" }, "POST /api/v1/ocorrencias", TipoEvento.Api);

            // Assert
            Assert.Equal(0, f.Log.ObterContagem("RN14"));
            Assert.Equal(1, f.Log.ObterContagem("UC03"));
        }

        [Fact]
        [Trait("Requisito", "RN14")]
        public void Registrar_DeveGuardarOTempoSimuladoDoEvento()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Cidade.TempoSeg = 41.2;

            // Act
            var evento = f.Log.Registrar(new[] { "RN15" }, "Viatura escolhida.");

            // Assert
            Assert.Equal(41.2, evento.TempoSeg);
            Assert.Equal(TipoEvento.Evento, evento.Tipo);
        }

        [Fact]
        [Trait("Requisito", "UC10")]
        public void Listar_DeveRetornarSoOsEventosDepoisDoSeqInformado_EmOrdemCrescente()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            for (var i = 0; i < 5; i++)
                f.Log.Registrar(new[] { "UC10" }, $"Evento {i}");

            // Act
            var lista = f.Log.Listar(desde: 3);

            // Assert
            Assert.Equal(new[] { 4, 5 }, lista.Select(e => e.Seq));
        }

        [Fact]
        [Trait("Requisito", "UC10")]
        public void Listar_DeveRetornarOsTrezentosMaisRecentes_QuandoNaoInformaDesde()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            for (var i = 0; i < 350; i++)
                f.Log.Registrar(new[] { "UC10" }, $"Evento {i}");

            // Act
            var lista = f.Log.Listar();

            // Assert
            Assert.Equal(300, lista.Count);
            Assert.Equal(51, lista[0].Seq);
            Assert.Equal(350, lista[^1].Seq);
        }

        [Fact]
        [Trait("Requisito", "RNF05")]
        public void Registrar_DeveDescartarOsMaisAntigos_QuandoPassaDeTresMilEventos()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            for (var i = 0; i < 3100; i++)
                f.Log.Registrar(new[] { "UC10" }, $"Evento {i}");

            // Assert
            Assert.Equal(3000, f.Cidade.Log.Count);
            Assert.Equal(101, f.Cidade.Log[0].Seq);
        }
    }
}
