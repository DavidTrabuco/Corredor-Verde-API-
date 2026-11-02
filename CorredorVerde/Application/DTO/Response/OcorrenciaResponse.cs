using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class OcorrenciaResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Cruzamento { get; set; } = string.Empty;
        public int Criticidade { get; set; }
        public TipoViatura Tipo { get; set; }
        public string ViaturaId { get; set; } = string.Empty;
        public StatusOcorrencia Status { get; set; }
        public double RegistradaEmSeg { get; set; }
        public double? EncerradaEmSeg { get; set; }

        // Nome do hospital (só ambulância)
        public string? Hospital { get; set; }

        public static OcorrenciaResponse De(Ocorrencia ocorrencia) => new()
        {
            Id = ocorrencia.Id,
            Cruzamento = ocorrencia.Cruzamento,
            Criticidade = ocorrencia.Criticidade,
            Tipo = ocorrencia.Tipo,
            ViaturaId = ocorrencia.ViaturaId,
            Status = ocorrencia.Status,
            RegistradaEmSeg = Math.Round(ocorrencia.RegistradaEmSeg, 2),
            EncerradaEmSeg = ocorrencia.EncerradaEmSeg.HasValue ? Math.Round(ocorrencia.EncerradaEmSeg.Value, 2) : null,
            Hospital = ocorrencia.Hospital?.Nome
        };
    }
}
