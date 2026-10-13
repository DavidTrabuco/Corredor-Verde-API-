using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class EventoLog
    {
        public int Seq { get; set; }
        public double TempoSeg { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public TipoEvento Tipo { get; set; }
    }
}
