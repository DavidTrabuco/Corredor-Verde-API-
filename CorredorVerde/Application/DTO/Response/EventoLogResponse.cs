using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class EventoLogResponse
    {
        public int Seq { get; set; }
        public double TempoSeg { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public TipoEvento Tipo { get; set; }

        public static EventoLogResponse De(EventoLog evento) => new()
        {
            Seq = evento.Seq,
            TempoSeg = Math.Round(evento.TempoSeg, 2),
            Tags = evento.Tags.ToList(),
            Mensagem = evento.Mensagem,
            Tipo = evento.Tipo
        };
    }
}
