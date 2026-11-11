using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Infrastructure.Simulacao;
using CorredorVerde.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace CorredorVerde.Tests.Infrastructure
{
    // O relógio de verdade usa um timer. Aqui o tempo é falso (FakeTimeProvider): o teste manda cada "tick" andar,
    // sem esperar 100 ms de verdade e sem depender da velocidade da máquina.
    public class RelogioSimulacaoWorkerTests
    {
        [Fact(DisplayName = "UC02 · A cada tick de 100 ms, o relógio avança 0,1 s no tempo da simulação")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveAvancarOTempo_QuandoPassaUmTick()
        {
            // Arrange
            using var relogio = new RelogioDeTeste();
            await relogio.Ligar();

            // Act
            await relogio.PassarUmTick();

            // Assert
            Assert.Single(relogio.Temporizador.Chamadas);
            Assert.Equal(0.1, relogio.Temporizador.Chamadas[0], precision: 6);
        }

        [Fact(DisplayName = "UC10 · Na velocidade 4×, cada tick avança 4 vezes mais")]
        [Trait("Requisito", "UC10")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveMultiplicarPelaVelocidade_QuandoAVelocidadeEMaior()
        {
            // Arrange
            using var relogio = new RelogioDeTeste();
            relogio.Fabrica.Cidade.Velocidade = 4;
            await relogio.Ligar();

            // Act
            await relogio.PassarUmTick();

            // Assert: 0,1 s × 4 = 0,4 s
            Assert.Equal(0.4, relogio.Temporizador.Chamadas[0], precision: 6);
        }

        [Fact(DisplayName = "UC10 · Com a simulação pausada, o tempo não anda")]
        [Trait("Requisito", "UC10")]
        public async Task ExecuteAsync_NaoDeveAvancarOTempo_QuandoEstaPausado()
        {
            // Arrange
            using var relogio = new RelogioDeTeste();
            relogio.Fabrica.Cidade.Pausado = true;
            await relogio.Ligar();

            // Act: passam 3 ticks
            await relogio.PassarUmTick();
            await relogio.PassarUmTick();
            await relogio.PassarUmTick();

            // Assert: o relógio nem chamou o temporizador
            Assert.Empty(relogio.Temporizador.Chamadas);
        }

        [Fact(DisplayName = "UC10 · Ao tirar a pausa, o tempo volta a andar")]
        [Trait("Requisito", "UC10")]
        public async Task ExecuteAsync_DeveVoltarAAvancar_QuandoAPausaTermina()
        {
            // Arrange: começa pausado e deixa um tick passar parado
            using var relogio = new RelogioDeTeste();
            relogio.Fabrica.Cidade.Pausado = true;
            await relogio.Ligar();
            await relogio.PassarUmTick();
            Assert.Empty(relogio.Temporizador.Chamadas);

            // Act: a pausa é tirada e passa mais um tick
            relogio.Fabrica.Cidade.Pausado = false;
            await relogio.PassarUmTick();

            // Assert
            Assert.Single(relogio.Temporizador.Chamadas);
        }

        [Fact(DisplayName = "RNF12 · O tamanho do tick vem da configuração (TickMs)")]
        [Trait("Requisito", "RNF12")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveUsarOTickDaConfiguracao_QuandoTickMsMuda()
        {
            // Arrange: tick de 50 ms em vez de 100 ms
            using var relogio = new RelogioDeTeste(tickMs: 50);
            await relogio.Ligar();

            // Act
            await relogio.PassarUmTick();

            // Assert
            Assert.Equal(0.05, relogio.Temporizador.Chamadas[0], precision: 6);
        }

        [Fact(DisplayName = "RNF02 · Um erro num tick não para o relógio: o tick seguinte acontece normalmente")]
        [Trait("Requisito", "RNF02")]
        public async Task ExecuteAsync_NaoDevePararORelogio_QuandoUmTickDaErro()
        {
            // Arrange: o primeiro tick quebra
            using var relogio = new RelogioDeTeste();
            relogio.Temporizador.QuebrarNaPrimeiraChamada = true;
            await relogio.Ligar();

            // Act: o tick que quebra e mais um
            await relogio.PassarUmTick();
            await relogio.PassarUmTick();

            // Assert: houve duas chamadas, então o relógio continuou depois do erro
            Assert.Equal(2, relogio.Temporizador.Chamadas.Count);
        }

        [Fact(DisplayName = "UC02 · Com o temporizador de verdade, dez ticks fazem a simulação andar 1 s")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveFazerOTempoAndar_QuandoUsaOTemporizadorReal()
        {
            // Arrange: sem o falso, o relógio chama o TemporizadorService de verdade
            using var relogio = new RelogioDeTeste(usarTemporizadorReal: true);
            await relogio.Ligar();

            // Act: 10 ticks de 100 ms
            for (var i = 0; i < 10; i++)
                await relogio.PassarUmTick();

            // Assert
            Assert.Equal(1.0, relogio.Fabrica.Cidade.TempoSeg, precision: 6);
        }

        [Fact(DisplayName = "RNF02 · Ao desligar a API, o relógio para sem erro e não anda mais")]
        [Trait("Requisito", "RNF02")]
        public async Task StopAsync_DevePararORelogio_QuandoAApiDesliga()
        {
            // Arrange
            using var relogio = new RelogioDeTeste();
            await relogio.Ligar();
            await relogio.PassarUmTick();

            // Act
            await relogio.Desligar();
            relogio.Tempo.Advance(TimeSpan.FromMilliseconds(500));
            await Task.Delay(50);

            // Assert: continua só com o tick de antes
            Assert.Single(relogio.Temporizador.Chamadas);
        }

        // Temporizador falso: só anota com quantos segundos foi chamado (e pode quebrar na primeira vez)
        private class TemporizadorGravador : ITemporizadorService
        {
            private readonly object _trava = new();
            private readonly List<double> _chamadas = new();
            private bool _jaQuebrou;

            public bool QuebrarNaPrimeiraChamada { get; set; }

            public List<double> Chamadas
            {
                get
                {
                    lock (_trava)
                    {
                        return new List<double>(_chamadas);
                    }
                }
            }

            public void Avancar(double segundos)
            {
                lock (_trava)
                {
                    _chamadas.Add(segundos);
                    if (QuebrarNaPrimeiraChamada && !_jaQuebrou)
                    {
                        _jaQuebrou = true;
                        throw new InvalidOperationException("tick que quebra");
                    }
                }
            }
        }

        // Monta o relógio com tempo falso e deixa cada tick passar de forma previsível
        private class RelogioDeTeste : IDisposable
        {
            private readonly TimeSpan _tick;
            private readonly bool _usarTemporizadorReal;
            private int _ticksJaVistos;

            public CidadeFactory Fabrica { get; }
            public FakeTimeProvider Tempo { get; } = new();
            public TemporizadorGravador Temporizador { get; } = new();
            public RelogioExposto Worker { get; }
            private readonly CancellationTokenSource _parada = new();
            private Task _execucao = Task.CompletedTask;

            public RelogioDeTeste(int tickMs = 100, bool usarTemporizadorReal = false)
            {
                _tick = TimeSpan.FromMilliseconds(tickMs);
                _usarTemporizadorReal = usarTemporizadorReal;
                Fabrica = CidadeFactory.Criar(opcoes => opcoes.TickMs = tickMs);

                ITemporizadorService temporizador = usarTemporizadorReal ? Fabrica.Temporizador : Temporizador;
                Worker = new RelogioExposto(
                    Fabrica.Cidade,
                    temporizador,
                    Tempo,
                    Options.Create(Fabrica.Cidade.Config),
                    NullLogger<RelogioSimulacaoWorker>.Instance);
            }

            // Roda o ExecuteAsync direto: o timer já nasce aqui, antes do primeiro tick (o StartAsync do .NET 10 não garante isso)
            public Task Ligar()
            {
                _execucao = Worker.Rodar(_parada.Token);
                return Task.CompletedTask;
            }

            // Desliga como a API faz ao parar, e espera o relógio terminar
            public async Task Desligar()
            {
                _parada.Cancel();
                await _execucao;
            }

            // Faz o tempo falso andar um tick e espera o relógio terminar de tratar esse tick
            public async Task PassarUmTick()
            {
                Tempo.Advance(_tick);
                _ticksJaVistos++;
                await EsperarOTick();
            }

            // O relógio roda em outra thread: espera (até 2 s) ele tratar o tick, ou dá um tempo curto quando nada deve acontecer
            private async Task EsperarOTick()
            {
                var limite = DateTime.UtcNow.AddSeconds(2);
                if (!_usarTemporizadorReal && !Fabrica.Cidade.Pausado)
                {
                    while (Temporizador.Chamadas.Count < _ticksJaVistos && DateTime.UtcNow < limite)
                        await Task.Delay(5);
                    return;
                }

                if (_usarTemporizadorReal)
                {
                    var esperado = _ticksJaVistos * _tick.TotalSeconds;
                    while (Fabrica.Cidade.TempoSeg < esperado - 1e-9 && DateTime.UtcNow < limite)
                        await Task.Delay(5);
                    return;
                }

                // Pausado: nada vai acontecer, então só dá tempo de o relógio olhar o tick
                await Task.Delay(30);
                _ticksJaVistos--;
            }

            public void Dispose()
            {
                Desligar().GetAwaiter().GetResult();
                _parada.Dispose();
                Worker.Dispose();
            }
        }

        // O mesmo relógio da API, só deixando o teste chamar o ExecuteAsync (que é protected)
        private class RelogioExposto : RelogioSimulacaoWorker
        {
            public RelogioExposto(Domain.Entidades.Cidade cidade, ITemporizadorService temporizador, TimeProvider tempo,
                IOptions<Domain.Options.SimulacaoOptions> opcoes, Microsoft.Extensions.Logging.ILogger<RelogioSimulacaoWorker> logger)
                : base(cidade, temporizador, tempo, opcoes, logger)
            {
            }

            public Task Rodar(CancellationToken parada) => ExecuteAsync(parada);
        }
    }
}
