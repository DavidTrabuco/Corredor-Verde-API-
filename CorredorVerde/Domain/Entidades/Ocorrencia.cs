using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class Ocorrencia
    {
        public string Id { get; set; } = string.Empty;
        public string Cruzamento { get; set; } = string.Empty;
        public int Criticidade { get; set; }
        public TipoViatura Tipo { get; set; }
        public string ViaturaId { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public StatusOcorrencia Status { get; set; }
        public double RegistradaEmSeg { get; set; }
        public double? EncerradaEmSeg { get; set; }
        public Hospital? Hospital { get; set; }
    }
}
