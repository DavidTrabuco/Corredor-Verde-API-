using CorredorVerde.Domain.Enums;
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

        [Fact(DisplayName = "RN14 · Chamadas da API vão para o log, mas não contam como evento de negócio")]
        [Trait("Requisito", "RN14")]
        public void Registrar_NaoDeveContarRN14_QuandoEChamadaDaApi()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            f.Log.Registrar(new[] { "API", "UC03" }, "POST /api/v1/ocorrencias", TipoEvento.Api);

            // Assert
            Assert.Equal(0, f.Log.ObterContagem("RN14"));
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

        [Fact(DisplayName = "UC10 · Sem dizer “desde”, a lista traz só os 300 eventos mais recentes")]
        [Trait("Requisito", "UC10")]
        public void Listar_DeveRetornarOsTrezentosMaisRecentes_QuandoNaoInformaDesde()
        {
            // Arrange: 350 eventos
            var f = CidadeFactory.Criar();
            for (var i = 0; i < 350; i++)
                f.Log.Registrar(new[] { "UC10" }, "Evento de teste.");

            // Act
            var lista = f.Log.Listar();

            // Assert: do 51 ao 350
            Assert.Equal(300, lista.Count);
            Assert.Equal(51, lista[0].Seq);
            Assert.Equal(350, lista[299].Seq);
        }

        [Fact(DisplayName = "RNF05 · O log só cresce; a memória guarda os 3.000 últimos e descarta os mais antigos")]
        [Trait("Requisito", "RNF05")]
        public void Registrar_DeveDescartarOsMaisAntigos_QuandoPassaDeTresMilEventos()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: 3.100 eventos
            for (var i = 0; i < 3100; i++)
                f.Log.Registrar(new[] { "UC10" }, "Evento de teste.");

            // Assert: os 100 primeiros saíram
            Assert.Equal(3000, f.Cidade.Log.Count);
            Assert.Equal(101, f.Cidade.Log[0].Seq);
        }
    }
}
