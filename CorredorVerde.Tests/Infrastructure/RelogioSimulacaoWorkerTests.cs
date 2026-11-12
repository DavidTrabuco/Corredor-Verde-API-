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
            using var cenario = new CenarioDoRelogio();
            await cenario.LigarORelogio();

            // Act
            await cenario.PassarUmTick();

            // Assert
            Assert.Single(cenario.TemporizadorFalso.SegundosRecebidos);
            Assert.Equal(0.1, cenario.TemporizadorFalso.SegundosRecebidos[0], precision: 6);
        }

        [Fact(DisplayName = "UC10 · Na velocidade 4×, cada tick avança 4 vezes mais")]
        [Trait("Requisito", "UC10")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveMultiplicarPelaVelocidade_QuandoAVelocidadeEMaior()
        {
            // Arrange
            using var cenario = new CenarioDoRelogio();
            cenario.FabricaDaCidade.Cidade.Velocidade = 4;
            await cenario.LigarORelogio();

            // Act
            await cenario.PassarUmTick();

            // Assert: 0,1 s × 4 = 0,4 s
            Assert.Equal(0.4, cenario.TemporizadorFalso.SegundosRecebidos[0], precision: 6);
        }

        [Fact(DisplayName = "UC10 · Com a simulação pausada, o tempo não anda")]
        [Trait("Requisito", "UC10")]
        public async Task ExecuteAsync_NaoDeveAvancarOTempo_QuandoEstaPausado()
        {
            // Arrange
            using var cenario = new CenarioDoRelogio();
            cenario.FabricaDaCidade.Cidade.Pausado = true;
            await cenario.LigarORelogio();

            // Act: passam 3 ticks
            await cenario.PassarUmTick();
            await cenario.PassarUmTick();
            await cenario.PassarUmTick();

            // Assert: o relógio nem chamou o temporizador
            Assert.Empty(cenario.TemporizadorFalso.SegundosRecebidos);
        }

        [Fact(DisplayName = "UC10 · Ao tirar a pausa, o tempo volta a andar")]
        [Trait("Requisito", "UC10")]
        public async Task ExecuteAsync_DeveVoltarAAvancar_QuandoAPausaTermina()
        {
            // Arrange: começa pausado e deixa um tick passar parado
            using var cenario = new CenarioDoRelogio();
            cenario.FabricaDaCidade.Cidade.Pausado = true;
            await cenario.LigarORelogio();
            await cenario.PassarUmTick();
            Assert.Empty(cenario.TemporizadorFalso.SegundosRecebidos);

            // Act: a pausa é tirada e passa mais um tick
            cenario.FabricaDaCidade.Cidade.Pausado = false;
            await cenario.PassarUmTick();

            // Assert
            Assert.Single(cenario.TemporizadorFalso.SegundosRecebidos);
        }

        [Fact(DisplayName = "RNF12 · O tamanho do tick vem da configuração (TickMs)")]
        [Trait("Requisito", "RNF12")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveUsarOTickDaConfiguracao_QuandoTickMsMuda()
        {
            // Arrange: tick de 50 ms em vez de 100 ms
            using var cenario = new CenarioDoRelogio(tickMs: 50);
            await cenario.LigarORelogio();

            // Act
            await cenario.PassarUmTick();

            // Assert
            Assert.Equal(0.05, cenario.TemporizadorFalso.SegundosRecebidos[0], precision: 6);
        }

        [Fact(DisplayName = "RNF02 · Um erro num tick não para o relógio: o tick seguinte acontece normalmente")]
        [Trait("Requisito", "RNF02")]
        public async Task ExecuteAsync_NaoDevePararORelogio_QuandoUmTickDaErro()
        {
            // Arrange: o primeiro tick quebra
            using var cenario = new CenarioDoRelogio();
            cenario.TemporizadorFalso.QuebrarNaPrimeiraChamada = true;
            await cenario.LigarORelogio();

            // Act: o tick que quebra e mais um
            await cenario.PassarUmTick();
            await cenario.PassarUmTick();

            // Assert: houve duas chamadas, então o relógio continuou depois do erro
            Assert.Equal(2, cenario.TemporizadorFalso.SegundosRecebidos.Count);
        }

        [Fact(DisplayName = "UC02 · Com o temporizador de verdade, dez ticks fazem a simulação andar 1 s")]
        [Trait("Requisito", "UC02")]
        public async Task ExecuteAsync_DeveFazerOTempoAndar_QuandoUsaOTemporizadorReal()
        {
            // Arrange: sem o falso, o relógio chama o TemporizadorService de verdade
            using var cenario = new CenarioDoRelogio(usarTemporizadorReal: true);
            await cenario.LigarORelogio();

            // Act: 10 ticks de 100 ms
            for (var i = 0; i < 10; i++)
                await cenario.PassarUmTick();

            // Assert
            Assert.Equal(1.0, cenario.FabricaDaCidade.Cidade.TempoSeg, precision: 6);
        }

        [Fact(DisplayName = "RNF02 · Ao desligar a API, o relógio para sem erro e não anda mais")]
        [Trait("Requisito", "RNF02")]
        public async Task StopAsync_DevePararORelogio_QuandoAApiDesliga()
        {
            // Arrange
            using var cenario = new CenarioDoRelogio();
            await cenario.LigarORelogio();
            await cenario.PassarUmTick();

            // Act
            await cenario.DesligarORelogio();
            cenario.TempoFalso.Advance(TimeSpan.FromMilliseconds(500));
            await Task.Delay(50);

            // Assert: continua só com o tick de antes
            Assert.Single(cenario.TemporizadorFalso.SegundosRecebidos);
        }


        // Temporizador falso: só anota com quantos segundos foi chamado (e pode quebrar na primeira vez)
        private class TemporizadorGravador : ITemporizadorService
        {
            private readonly object _trava = new();
            private readonly List<double> _segundosRecebidos = new();
            private bool _jaQuebrou;

            public bool QuebrarNaPrimeiraChamada { get; set; }

            // Cada item é o valor de "segundos" de uma chamada ao Avancar, na ordem em que aconteceram
            public List<double> SegundosRecebidos
            {
                get
                {
                    lock (_trava)
                    {
                        return new List<double>(_segundosRecebidos);
                    }
                }
            }

            public void Avancar(double segundos)
            {
                lock (_trava)
                {
                    _segundosRecebidos.Add(segundos);
                    if (QuebrarNaPrimeiraChamada && !_jaQuebrou)
                    {
                        _jaQuebrou = true;
                        throw new InvalidOperationException("tick que quebra");
                    }
                }
            }
        }

        // Tudo o que um teste do relógio precisa: a cidade, o tempo falso, o temporizador que anota e o relógio da API.
        // Cada tick passa de forma previsível (o teste manda o tempo andar e espera o relógio tratar).
        private class CenarioDoRelogio : IDisposable
        {
            private readonly TimeSpan _duracaoDoTick;
            private readonly bool _usaTemporizadorReal;
            private readonly CancellationTokenSource _pedidoDeParada = new();
            private Task _tarefaDoRelogio = Task.CompletedTask;
            private int _ticksPassados;

            // A cidade de teste (com as bases, viaturas e semáforos) e o service real que ela traz
            public CidadeFactory FabricaDaCidade { get; }

            // O tempo que o teste controla: só anda quando o teste manda (Advance)
            public FakeTimeProvider TempoFalso { get; } = new();

            // O temporizador falso, que anota quantos segundos o relógio mandou avançar
            public TemporizadorGravador TemporizadorFalso { get; } = new();

            // O mesmo relógio que a API usa
            public RelogioDaApiParaTeste RelogioDaApi { get; }

            public CenarioDoRelogio(int tickMs = 100, bool usarTemporizadorReal = false)
            {
                _duracaoDoTick = TimeSpan.FromMilliseconds(tickMs);
                _usaTemporizadorReal = usarTemporizadorReal;
                FabricaDaCidade = CidadeFactory.Criar(opcoes => opcoes.TickMs = tickMs);

                ITemporizadorService temporizadorUsado = usarTemporizadorReal ? FabricaDaCidade.Temporizador : TemporizadorFalso;
                RelogioDaApi = new RelogioDaApiParaTeste(
                    FabricaDaCidade.Cidade,
                    temporizadorUsado,
                    TempoFalso,
                    Options.Create(FabricaDaCidade.Cidade.Config),
                    NullLogger<RelogioSimulacaoWorker>.Instance);
            }

            // Roda o ExecuteAsync direto: o timer já nasce aqui, antes do primeiro tick (o StartAsync do .NET 10 não garante isso)
            public Task LigarORelogio()
            {
                _tarefaDoRelogio = RelogioDaApi.Rodar(_pedidoDeParada.Token);
                return Task.CompletedTask;
            }

            // Desliga como a API faz ao parar, e espera o relógio terminar
            public async Task DesligarORelogio()
            {
                _pedidoDeParada.Cancel();
                await _tarefaDoRelogio;
            }

            // Faz o tempo falso andar um tick e espera o relógio terminar de tratar esse tick
            public async Task PassarUmTick()
            {
                TempoFalso.Advance(_duracaoDoTick);
                _ticksPassados++;
                await EsperarOTick();
            }

            // O relógio roda em outra thread: espera (até 2 s) ele tratar o tick, ou dá um tempo curto quando nada deve acontecer
            private async Task EsperarOTick()
            {
                var prazoMaximo = DateTime.UtcNow.AddSeconds(2);
                if (!_usaTemporizadorReal && !FabricaDaCidade.Cidade.Pausado)
                {
                    while (TemporizadorFalso.SegundosRecebidos.Count < _ticksPassados && DateTime.UtcNow < prazoMaximo)
                        await Task.Delay(5);
                    return;
                }

                if (_usaTemporizadorReal)
                {
                    var tempoEsperadoSeg = _ticksPassados * _duracaoDoTick.TotalSeconds;
                    while (FabricaDaCidade.Cidade.TempoSeg < tempoEsperadoSeg - 1e-9 && DateTime.UtcNow < prazoMaximo)
                        await Task.Delay(5);
                    return;
                }

                // Pausado: nada vai acontecer, então só dá tempo de o relógio olhar o tick
                await Task.Delay(30);
                _ticksPassados--;
            }

            public void Dispose()
            {
                DesligarORelogio().GetAwaiter().GetResult();
                _pedidoDeParada.Dispose();
                RelogioDaApi.Dispose();
            }
        }

        // O mesmo relógio da API, só deixando o teste chamar o ExecuteAsync (que é protected)
        private class RelogioDaApiParaTeste : RelogioSimulacaoWorker
        {
            public RelogioDaApiParaTeste(Domain.Entidades.Cidade cidade, ITemporizadorService temporizador, TimeProvider tempo,
                IOptions<Domain.Options.SimulacaoOptions> opcoes, Microsoft.Extensions.Logging.ILogger<RelogioSimulacaoWorker> logger)
                : base(cidade, temporizador, tempo, opcoes, logger)
            {
            }

            public Task Rodar(CancellationToken pedidoDeParada) => ExecuteAsync(pedidoDeParada);
        }
    }
}
