using System.ComponentModel.DataAnnotations;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Options
{
    public class SimulacaoOptions
    {
        public const string Secao = "Simulacao";

        [Range(2, 26)]
        public int Colunas { get; set; } = 5;

        [Range(2, 26)]
        public int Linhas { get; set; } = 4;

        [Range(1, 600)]
        public double VerdeSeg { get; set; } = 8;

        [Range(0.5, 60)]
        public double AmareloSeg { get; set; } = 2;

        [Range(0.5, 600)]
        public double AtendimentoSeg { get; set; } = 4;

        [Range(0, 60)]
        public double CautelaSeg { get; set; } = 1;

        [Range(10, 5000)]
        public int TickMs { get; set; } = 100;

        [Required]
        public List<HospitalOptions> Hospitais { get; set; } = new();

        [Required]
        public List<BaseOptions> Bases { get; set; } = new();

        [Required]
        public List<ViaturaOptions> Viaturas { get; set; } = new();
    }

    public class HospitalOptions
    {
        [Required] public string Id { get; set; } = string.Empty;
        [Required] public string Nome { get; set; } = string.Empty;
        [Required] public string Cruzamento { get; set; } = string.Empty;
    }

    public class BaseOptions
    {
        [Required] public string Nome { get; set; } = string.Empty;
        [Required] public string Cruzamento { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }
    }

    public class ViaturaOptions
    {
        [Required] public string Id { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }
        public PapelViatura Papel { get; set; }
    }
}
