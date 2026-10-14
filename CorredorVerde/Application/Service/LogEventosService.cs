using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // RN14: toda ocorrência, corredor, desvio, conflito e falha vai para o log.
    // O log só cresce: não existe método para editar nem apagar um evento (RNF05).
    public class LogEventosService : ILogEventosService
    {
        private const int LimiteGuardado = 3000;
        private const int LimiteListagem = 300;

        private readonly Cidade _cidade;

        public LogEventosService(Cidade cidade)
        {
            _cidade = cidade;
        }

        public EventoLog Registrar(IEnumerable<string> tags, string mensagem, TipoEvento tipo = TipoEvento.Evento)
        {
            lock (_cidade.Trava)
            {
                var listaTags = tags.ToList();
                var evento = new EventoLog
                {
                    Seq = ++_cidade.SeqLog,
                    TempoSeg = _cidade.TempoSeg,
                    Tags = listaTags,
                    Mensagem = mensagem,
                    Tipo = tipo
                };
                _cidade.Log.Add(evento);

                if (_cidade.Log.Count > LimiteGuardado)
                    _cidade.Log.RemoveRange(0, _cidade.Log.Count - LimiteGuardado);

                foreach (var tag in listaTags)
                    Contar(tag);

                // Chamadas da API não contam como evento de negócio
                if (tipo != TipoEvento.Api && listaTags.Any(t => t != "API"))
                    Contar("RN14");

                return evento;
            }
        }

        public void Contar(string tag, int n = 1)
        {
            lock (_cidade.Trava)
            {
                _cidade.Contagem[tag] = _cidade.Contagem.GetValueOrDefault(tag) + n;
            }
        }

        public int ObterContagem(string tag)
        {
            lock (_cidade.Trava)
            {
                return _cidade.Contagem.GetValueOrDefault(tag);
            }
        }

        // Com "desde": os eventos com Seq maior, em ordem crescente. Sem "desde": os 300 mais recentes.
        public List<EventoLog> Listar(int? desde = null)
        {
            lock (_cidade.Trava)
            {
                if (desde.HasValue)
                    return _cidade.Log.Where(e => e.Seq > desde.Value).OrderBy(e => e.Seq).ToList();

                return _cidade.Log.OrderBy(e => e.Seq).TakeLast(LimiteListagem).ToList();
            }
        }
    }
}
