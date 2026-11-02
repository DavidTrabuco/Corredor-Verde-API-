using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;
using Microsoft.Extensions.Options;

namespace CorredorVerde.Infrastructure.Simulacao
{
    // Relógio da simulação: a cada TickMs (100 ms) avança o tempo simulado em "tick × velocidade".
    // Usa o TimeProvider injetado; nos testes de integração este worker é removido para o tempo não andar sozinho.
    public class RelogioSimulacaoWorker : BackgroundService
    {
        private readonly Cidade _cidade;
        private readonly ITemporizadorService _temporizador;
        private readonly TimeProvider _tempo;
        private readonly SimulacaoOptions _opcoes;
        private readonly ILogger<RelogioSimulacaoWorker> _logger;

        public RelogioSimulacaoWorker(Cidade cidade, ITemporizadorService temporizador, TimeProvider tempo,
            IOptions<SimulacaoOptions> opcoes, ILogger<RelogioSimulacaoWorker> logger)
        {
            _cidade = cidade;
            _temporizador = temporizador;
            _tempo = tempo;
            _opcoes = opcoes.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tick = TimeSpan.FromMilliseconds(_opcoes.TickMs);
            using var timer = new PeriodicTimer(tick, _tempo);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        lock (_cidade.Trava)
                        {
                            if (!_cidade.Pausado)
                                _temporizador.Avancar(tick.TotalSeconds * _cidade.Velocidade);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Um erro num tick não pode parar o relógio da simulação inteira (RNF02)
                        _logger.LogError(ex, "Erro ao avançar o relógio da simulação");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // A API está desligando
            }
        }
    }
}
