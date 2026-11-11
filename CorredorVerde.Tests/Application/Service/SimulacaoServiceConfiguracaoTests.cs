using CorredorVerde.Application.Service;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;
using CorredorVerde.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CorredorVerde.Tests.Application.Service
{
    // A mensagem "Simulação pronta" muda conforme a configuração (RNF12): sem viaturas, com uma só, sem bases...
    public class SimulacaoServiceConfiguracaoTests
    {
        // Monta o SimulacaoService com a configuração que o teste ajustou
        private static (CidadeFactory Fabrica, ISimulacaoService Simulacao) Montar(Action<SimulacaoOptions> ajustar)
        {
            var f = CidadeFactory.Criar(ajustar);
            var simulacao = new SimulacaoService(f.Cidade, f.Semaforo, f.Log, Options.Create(f.Cidade.Config));
            return (f, simulacao);
        }

        [Fact(DisplayName = "RNF12 · Sem nenhuma viatura configurada, a mensagem é só \"Simulação pronta.\"")]
        [Trait("Requisito", "RNF12")]
        public void Reiniciar_DeveDizerSoSimulacaoPronta_QuandoNaoHaViaturas()
        {
            // Arrange
            var (f, simulacao) = Montar(opcoes => opcoes.Viaturas.Clear());

            // Act
            simulacao.Reiniciar();

            // Assert
            Assert.True(f.LogTem("Simulação pronta."));
        }

        [Fact(DisplayName = "RNF12 · Com uma só viatura de patrulha, a mensagem cita só ela")]
        [Trait("Requisito", "RNF12")]
        public void Reiniciar_DeveCitarUmaViatura_QuandoHaSoUmaDePatrulha()
        {
            // Arrange: só o AMB-01 na configuração
            var (f, simulacao) = Montar(opcoes => opcoes.Viaturas.RemoveAll(v => v.Id != "AMB-01"));

            // Act
            simulacao.Reiniciar();

            // Assert
            Assert.True(f.LogTem("Simulação pronta: AMB-01 patrulhando."));
        }

        [Fact(DisplayName = "RNF12 · Com três viaturas de patrulha, a lista sai no formato \"A, B e C\"")]
        [Trait("Requisito", "RNF12")]
        public void Reiniciar_DeveJuntarAsViaturasComVirgulaEE_QuandoHaTresDePatrulha()
        {
            // Arrange: uma terceira viatura de patrulha na configuração
            var (f, simulacao) = Montar(opcoes => opcoes.Viaturas.Add(
                new ViaturaOptions { Id = "AMB-03", Tipo = TipoViatura.Ambulancia, Papel = PapelViatura.Patrulha }));

            // Act
            simulacao.Reiniciar();

            // Assert
            Assert.True(f.LogTem("Simulação pronta: AMB-01, BOM-01 e AMB-03 patrulhando; AMB-02 e BOM-02 de reserva na Base do SAMU (C11) e no Quartel dos Bombeiros (C20)."));
        }

        [Fact(DisplayName = "RNF12 · Sem bases na configuração, a mensagem cita o cruzamento da reserva")]
        [Trait("Requisito", "RNF12")]
        public void Reiniciar_DeveCitarOCruzamento_QuandoAsBasesNaoEstaoNaConfiguracao()
        {
            // Arrange
            var (f, simulacao) = Montar(opcoes => opcoes.Bases.Clear());

            // Act
            simulacao.Reiniciar();

            // Assert
            Assert.True(f.LogTem("Simulação pronta: AMB-01 e BOM-01 patrulhando; AMB-02 e BOM-02 de reserva na base C1 (C1)."));
        }
    }
}
