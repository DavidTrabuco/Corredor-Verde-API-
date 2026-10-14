using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Interfaces
{
    public interface ILogEventosService
    {
        EventoLog Registrar(IEnumerable<string> tags, string mensagem, TipoEvento tipo = TipoEvento.Evento);
        void Contar(string tag, int n = 1);
        int ObterContagem(string tag);
        List<EventoLog> Listar(int? desde = null);
    }
}
