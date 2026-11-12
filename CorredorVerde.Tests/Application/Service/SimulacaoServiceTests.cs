using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Tests.TestSupport;
using static CorredorVerde.Tests.TestSupport.IdsDasViaturas;

namespace CorredorVerde.Tests.Application.Service
{
    // É o que a maquete lê por polling e o que os botões Pausar, Velocidade e Reiniciar usam.
    public class SimulacaoServiceTests
    {
        [Fact(DisplayName = "UC01 · A malha entrega o tamanho da grade, hospitais e bases para a maquete")]
        [Trait("Requisito", "UC01")]
        [Trait("Requisito", "RNF01")]
        public void Malha_DeveEntregarAGradeHospitaisEBases_QuandoALeituraEInicial()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var malha = f.Simulacao.Malha();

            // Assert: grade de 5 x 4 = 20 cruzamentos, 2 hospitais e 2 bases
            Assert.Equal(5, malha.Colunas);
            Assert.Equal(4, malha.Linhas);
            Assert.Equal(20, malha.Cruzamentos.Count);
            Assert.Equal(2, malha.Hospitais.Count);
            Assert.Equal(2, malha.Bases.Count);
        }

        [Fact(DisplayName = "UC01 · O estado começa em 0 s, na velocidade 1, sem pausa e com todas as viaturas livres")]
        [Trait("Requisito", "UC01")]
        public void Estado_DeveComecarLimpo_QuandoASimulacaoAcabaDeIniciar()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var estado = f.Simulacao.Estado();

            // Assert: relógio zerado, rodando e sem nada acontecendo
            Assert.Equal(0.0, estado.TempoSeg);
            Assert.False(estado.Pausado);
            Assert.Equal(1, estado.Velocidade);
            Assert.Equal(20, estado.Semaforos.Count);
            Assert.Empty(estado.Ocorrencias);

            // Assert: as quatro viaturas estão livres, cada uma no modo do seu papel, sem ocorrência
            Assert.Equal(4, estado.Viaturas.Count);
            Assert.Equal(ModoViatura.Patrulha, estado.Viaturas.Find(v => v.Id == AmbulanciaPatrulha)?.Modo);
            Assert.Equal(ModoViatura.Reserva, estado.Viaturas.Find(v => v.Id == AmbulanciaReserva)?.Modo);
            Assert.Equal(ModoViatura.Patrulha, estado.Viaturas.Find(v => v.Id == BombeirosPatrulha)?.Modo);
            Assert.Equal(ModoViatura.Reserva, estado.Viaturas.Find(v => v.Id == BombeirosReserva)?.Modo);
            foreach (var viatura in estado.Viaturas)
                Assert.Null(viatura.OcorrenciaId);
        }

        [Theory(DisplayName = "UC01 · O tempo do estado é arredondado para o valor mais próximo, em 2 casas")]
        [Trait("Requisito", "UC01")]
        [InlineData(12.3456, 12.35)]
        [InlineData(12.3412, 12.34)]
        public void Estado_DeveArredondarOTempo_QuandoOTempoTemMuitasCasas(double tempo, double esperado)
        {
            // Arrange: 12,3456 passa da metade (sobe) e 12,3412 não passa (fica), então só o arredondamento de verdade acerta os dois
            var f = CidadeFactory.Criar();
            f.Cidade.TempoSeg = tempo;

            // Act
            var estado = f.Simulacao.Estado();

            // Assert
            Assert.Equal(esperado, estado.TempoSeg);
        }

        [Fact(DisplayName = "UC03 · Depois de registrar uma emergência, o estado mostra a ocorrência e a viatura em emergência")]
        [Trait("Requisito", "UC03")]
        [Trait("Requisito", "UC01")]
        public void Estado_DeveMostrarAEmergencia_QuandoUmaOcorrenciaFoiRegistrada()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Emergencias.Registrar("Ambulancia", "C15", 1);

            // Act
            var estado = f.Simulacao.Estado();

            // Assert: uma ocorrência a caminho, a AMB-01 em emergência e o Hospital Sul definido
            Assert.Single(estado.Ocorrencias);
            Assert.Equal("OC-001", estado.Ocorrencias[0].Id);
            Assert.Equal(StatusOcorrencia.ACaminho, estado.Ocorrencias[0].Status);
            Assert.Equal("Hospital Sul", estado.Ocorrencias[0].Hospital);

            var ambulancia = estado.Viaturas.Find(v => v.Id == AmbulanciaPatrulha);
            Assert.NotNull(ambulancia);
            Assert.Equal(ModoViatura.Emergencia, ambulancia.Modo);
            Assert.Equal("OC-001", ambulancia.OcorrenciaId);
        }

        [Fact(DisplayName = "RN12 · Num cruzamento reservado, o estado aponta a viatura que tem a prioridade")]
        [Trait("Requisito", "RN12")]
        [Trait("Requisito", "UC06")]
        public void Semaforos_DeveApontarOVencedor_QuandoHaReservaNoCruzamento()
        {
            // Arrange: o corredor de AMB-01 está aberto de C11 a C15
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia(AmbulanciaPatrulha, de: "C11", ate: "C15", criticidade: 1);
            f.Frota.IniciarTrecho(ambulancia);

            // Act
            var semaforos = f.Simulacao.Semaforos();

            // Assert: C13 tem a reserva e o vencedor é a AMB-01; C1 não tem nada
            var c13 = semaforos.Find(s => s.Cruzamento == "C13");
            var c1 = semaforos.Find(s => s.Cruzamento == "C1");
            Assert.NotNull(c13);
            Assert.NotNull(c1);
            Assert.Equal(AmbulanciaPatrulha, c13.Vencedor);
            Assert.Single(c13.Reservas);
            Assert.Null(c1.Vencedor);
            Assert.Empty(c1.Reservas);
        }

        [Fact(DisplayName = "RN13 · Um semáforo em falha aparece com a falha marcada e as duas luzes piscando")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "UC09")]
        public void Semaforos_DeveMarcarAFalha_QuandoOSemaforoEstaEmFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Falhas.Definir("C13", ativa: true);

            // Act
            var semaforos = f.Simulacao.Semaforos();

            // Assert
            var c13 = semaforos.Find(s => s.Cruzamento == "C13");
            Assert.NotNull(c13);
            Assert.True(c13.Falha);
            Assert.Equal(Luz.Piscante, c13.Ns);
            Assert.Equal(Luz.Piscante, c13.Lo);
        }

        [Fact(DisplayName = "UC01 · Durante o atendimento, a viatura informa quanto falta (a maquete mostra a contagem)")]
        [Trait("Requisito", "UC01")]
        [Trait("Requisito", "RN09")]
        public void Viaturas_DeveInformarOTempoRestante_QuandoEstaEmAtendimento()
        {
            // Arrange: BOM-01 chegou ao local em 10 s, então o atendimento vai até 14 s
            var f = CidadeFactory.Criar();
            var bombeiros = f.ColocarEmEmergencia(BombeirosPatrulha, de: "C15", ate: "C15", criticidade: 2);
            f.Cidade.TempoSeg = 10.0;
            f.Frota.ChegouAoDestino(bombeiros);

            // Act: o relógio anda até 11,5 s
            f.Cidade.TempoSeg = 11.5;
            var viaturas = f.Simulacao.Viaturas();

            // Assert: faltam 2,5 s
            var resposta = viaturas.Find(v => v.Id == BombeirosPatrulha);
            Assert.NotNull(resposta);
            Assert.Equal(ModoViatura.Atendimento, resposta.Modo);
            Assert.Equal(2.5, resposta.AtendimentoRestanteSeg);
        }

        [Fact(DisplayName = "UC10 · Pausar e mudar a velocidade mexem só no que foi enviado")]
        [Trait("Requisito", "UC10")]
        public void Controlar_DeveMudarSoOQueFoiEnviado_QuandoUmDosValoresVemVazio()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act + Assert: só pausa; a velocidade continua 1
            f.Simulacao.Controlar(pausado: true, velocidade: null);
            Assert.True(f.Cidade.Pausado);
            Assert.Equal(1, f.Cidade.Velocidade);

            // Act + Assert: só troca a velocidade; continua pausado
            f.Simulacao.Controlar(pausado: null, velocidade: 4);
            Assert.True(f.Cidade.Pausado);
            Assert.Equal(4, f.Cidade.Velocidade);
        }

        [Fact(DisplayName = "UC10 · Velocidade fora de 1, 2 e 4 é recusada sem mudar nada")]
        [Trait("Requisito", "UC10")]
        [Trait("Requisito", "RNF04")]
        public void Controlar_DeveRecusar_QuandoAVelocidadeEInvalida()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: o pedido também tenta pausar, mas a velocidade 3 não existe
            var erro = Assert.Throws<ValidacaoException>(() => f.Simulacao.Controlar(pausado: true, velocidade: 3));

            // Assert: nem a velocidade nem a pausa mudaram
            Assert.Equal("Velocidade 3 inválida: use 1, 2 ou 4.", erro.Message);
            Assert.False(f.Cidade.Pausado);
            Assert.Equal(1, f.Cidade.Velocidade);
        }

        [Fact(DisplayName = "UC10 · Reiniciar volta tudo ao começo e deixa só a mensagem de simulação pronta no log")]
        [Trait("Requisito", "UC10")]
        public void Reiniciar_DeveVoltarAoComeco_QuandoHaEmergenciaEmAndamento()
        {
            // Arrange: uma emergência registrada, tempo andando, pausado e na velocidade 4
            var f = CidadeFactory.Criar();
            f.Emergencias.Registrar("Ambulancia", "C15", 1);
            f.Temporizador.Avancar(5.0);
            f.Simulacao.Controlar(pausado: true, velocidade: 4);

            // Act
            f.Simulacao.Reiniciar();

            // Assert: sem ocorrências, relógio zerado, velocidade 1 e só um evento no log
            Assert.Empty(f.Cidade.Ocorrencias);
            Assert.Equal(0.0, f.Cidade.TempoSeg);
            Assert.False(f.Cidade.Pausado);
            Assert.Equal(1, f.Cidade.Velocidade);
            Assert.Single(f.Cidade.Log);
            Assert.True(f.LogTem($"Simulação pronta: {AmbulanciaPatrulha} e {BombeirosPatrulha} patrulhando; {AmbulanciaReserva} e {BombeirosReserva} de reserva na Base do SAMU (C11) e no Quartel dos Bombeiros (C20)."));

            // Assert: as quatro viaturas voltaram para a base, cada uma no modo do seu papel
            Assert.Equal(ModoViatura.Patrulha, f.PegarViatura(AmbulanciaPatrulha).Modo);
            Assert.Equal("C11", f.PegarViatura(AmbulanciaPatrulha).Cruzamento);
            Assert.Equal(ModoViatura.Reserva, f.PegarViatura(AmbulanciaReserva).Modo);
            Assert.Equal("C11", f.PegarViatura(AmbulanciaReserva).Cruzamento);
            Assert.Equal(ModoViatura.Patrulha, f.PegarViatura(BombeirosPatrulha).Modo);
            Assert.Equal("C20", f.PegarViatura(BombeirosPatrulha).Cruzamento);
            Assert.Equal(ModoViatura.Reserva, f.PegarViatura(BombeirosReserva).Modo);
            Assert.Equal("C20", f.PegarViatura(BombeirosReserva).Cruzamento);
        }
    }
}
