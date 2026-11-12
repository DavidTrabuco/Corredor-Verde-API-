using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Em 0 s, o semáforo de C1 está com vermelho para Leste-Oeste. Os dois testes abaixo se comparam:
    // quem não é viatura em emergência (conhecida ou não) só espera e conta; quem é viatura em emergência também vai para o log.
    public class SemaforoServiceBordasTests
    {
        [Fact(DisplayName = "RN07 · Viatura desconhecida é tratada como veículo comum: espera no vermelho, igual a uma viatura conhecida em patrulha")]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DeveTratarComoVeiculoComum_QuandoAViaturaNaoExiste()
        {
            // Arrange: AMB-01 é conhecida, mas está em patrulha (não é emergência)
            var f = CidadeFactory.Criar();

            // Act: as duas perguntam no mesmo cruzamento e no mesmo eixo vermelho
            var conhecidaEmPatrulha = f.Semaforo.PodeAtravessar("C1", Eixo.LO, "AMB-01");
            var desconhecida = f.Semaforo.PodeAtravessar("C1", Eixo.LO, "XYZ-99");

            // Assert: a resposta é a mesma para as duas
            Assert.False(conhecidaEmPatrulha.Pode);
            Assert.False(desconhecida.Pode);
            Assert.Equal(conhecidaEmPatrulha.Cautela, desconhecida.Cautela);
            Assert.Equal(conhecidaEmPatrulha.Luz, desconhecida.Luz);
            Assert.Equal(Luz.Vermelho, desconhecida.Luz);

            // Assert: nenhuma das duas vai para o log, e as duas esperas só foram contadas
            Assert.Empty(f.Cidade.Log);
            Assert.Equal(2, f.Log.ObterContagem("RN07"));
        }

        [Fact(DisplayName = "RN07 · Já a viatura conhecida em emergência, na mesma situação, tem a espera registrada no log (uma vez só)")]
        [Trait("Requisito", "RN07")]
        [Trait("Requisito", "UC01")]
        public void PodeAtravessar_DeveRegistrarAEspera_QuandoAViaturaEstaEmEmergencia()
        {
            // Arrange: AMB-01 em emergência, parada diante do mesmo vermelho de C1
            var f = CidadeFactory.Criar();
            f.ColocarEmEmergencia("AMB-01", de: "C1", ate: "C15", criticidade: 1);

            // Act: a maquete pergunta duas vezes
            var primeiraPergunta = f.Semaforo.PodeAtravessar("C1", Eixo.LO, "AMB-01");
            f.Semaforo.PodeAtravessar("C1", Eixo.LO, "AMB-01");

            // Assert: espera igual à das outras, mas agora o evento aparece no log, e só uma vez
            Assert.False(primeiraPergunta.Pode);
            Assert.Equal(Luz.Vermelho, primeiraPergunta.Luz);
            Assert.Single(f.Cidade.Log);
            Assert.True(f.LogTem("AMB-01 parou na faixa de C1: vermelho para Leste-Oeste. Espera o verde."));
        }
    }
}
