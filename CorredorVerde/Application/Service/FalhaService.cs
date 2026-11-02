using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // UC09, RN13: semáforo em falha fica em amarelo piscante e sai das rotas (as que passavam por ele são recalculadas)
    public class FalhaService : IFalhaService
    {
        private readonly Cidade _cidade;
        private readonly IMalhaService _malha;
        private readonly ISemaforoService _semaforo;
        private readonly ICorredorService _corredor;
        private readonly IViaturaService _viatura;
        private readonly ILogEventosService _log;

        public FalhaService(Cidade cidade, IMalhaService malha, ISemaforoService semaforo, ICorredorService corredor,
            IViaturaService viatura, ILogEventosService log)
        {
            _cidade = cidade;
            _malha = malha;
            _semaforo = semaforo;
            _corredor = corredor;
            _viatura = viatura;
            _log = log;
        }

        public void Definir(string cruzamentoId, bool ativa)
        {
            lock (_cidade.Trava)
            {
                var cruzamento = _malha.ObterCruzamento(cruzamentoId);
                var id = cruzamento.Id;
                var semaforo = cruzamento.Semaforo;

                // Já está do jeito pedido: nada muda e nada vai para o log
                if (ativa == semaforo.EmFalha)
                    return;

                if (ativa)
                    Ligar(id, semaforo);
                else
                    Desligar(cruzamento, semaforo);
            }
        }

        private void Ligar(string id, Semaforo semaforo)
        {
            semaforo.EmFalha = true;
            _log.Registrar(new[] { "RN13", "UC09" },
                $"Semáforo de {id} em falha: amarelo piscante. Veículos param e seguem com cautela.",
                TipoEvento.Alerta);

            foreach (var v in _cidade.Viaturas.Values.ToList())
            {
                // Voltando para a base: refaz o caminho sem avisar (não há corredor)
                if (v.Modo == ModoViatura.Retorno
                    && v.Rota.Any(r => r.Cruzamento == id && r.Cruzamento != v.Base)
                    && v.IndoPara != id)
                {
                    _viatura.IniciarRetorno(v);
                    continue;
                }

                if (v.Modo != ModoViatura.Emergencia)
                    continue;
                // O destino pode estar em falha; só os cruzamentos do meio do caminho são evitados
                if (!v.Rota.Any(r => r.Cruzamento == id && r.Cruzamento != v.Destino))
                    continue;
                // Já está entrando nele: segue com cautela
                if (v.IndoPara == id)
                    continue;

                _log.Registrar(new[] { "RN13", "RN05", "UC09" },
                    $"A rota de {v.Id} passava por {id}. Recalculando para desviar.");
                _corredor.LiberarTodas(v);
                _viatura.IniciarTrecho(v);
            }
        }

        private void Desligar(Cruzamento cruzamento, Semaforo semaforo)
        {
            // Recomeça o ciclo normal com verde para Norte-Sul
            semaforo.EmFalha = false;
            semaforo.EixoVerde = Eixo.NS;
            semaforo.EmAmarelo = false;
            semaforo.DesdeSeg = _cidade.TempoSeg;
            _log.Registrar(new[] { "RN13", "UC09" },
                $"Falha de {cruzamento.Id} desligada: o ciclo normal recomeça.");
            _semaforo.Atualizar(cruzamento);

            // Quem estava sem caminho tenta de novo na hora
            foreach (var v in _cidade.Viaturas.Values.ToList())
            {
                if (v.Modo == ModoViatura.Emergencia && v.SemRota)
                    _viatura.IniciarTrecho(v);
                if (v.Modo == ModoViatura.Retorno && v.SemRota)
                    _viatura.IniciarRetorno(v);
            }
        }
    }
}
