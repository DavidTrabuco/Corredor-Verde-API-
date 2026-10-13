using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class Viatura
    {
        public string Id { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }
        public PapelViatura Papel { get; set; }
        public string Base { get; set; } = string.Empty;
        public ModoViatura Modo { get; set; }

        // Último cruzamento por onde a viatura passou
        public string Cruzamento { get; set; } = string.Empty;

        // Próximo cruzamento, quando está andando numa rua; null quando parada num cruzamento
        public string? IndoPara { get; set; }

        public Ocorrencia? Ocorrencia { get; set; }
        public int Trecho { get; set; }
        public string? Destino { get; set; }
        public List<PassoRota> Rota { get; set; } = new();
        public List<string> Previsao { get; set; } = new();
        public bool SemRota { get; set; }
        public double? AtendimentoAteSeg { get; set; }
        public double ProximaTentativaSeg { get; set; }
    }
}
