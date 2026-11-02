using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class ViaturaResponse
    {
        public string Id { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }
        public PapelViatura Papel { get; set; }
        public string Base { get; set; } = string.Empty;
        public ModoViatura Modo { get; set; }

        // Último cruzamento por onde passou
        public string Cruzamento { get; set; } = string.Empty;

        // Próximo cruzamento quando está numa rua; null quando está parada num cruzamento
        public string? IndoPara { get; set; }

        public int Trecho { get; set; }
        public string? Destino { get; set; }
        public bool SemRota { get; set; }

        // Cruzamentos que faltam no trecho atual (vazia fora da emergência e do retorno)
        public List<PassoRotaResponse> Rota { get; set; } = new();

        // Trecho 2 previsto (local → hospital), só no trecho 1 da ambulância
        public List<string> Previsao { get; set; } = new();

        public string? OcorrenciaId { get; set; }
        public double AtendimentoRestanteSeg { get; set; }
        public string? Hospital { get; set; }

        public static ViaturaResponse De(Viatura viatura, double tempoSeg) => new()
        {
            Id = viatura.Id,
            Tipo = viatura.Tipo,
            Papel = viatura.Papel,
            Base = viatura.Base,
            Modo = viatura.Modo,
            Cruzamento = viatura.Cruzamento,
            IndoPara = viatura.IndoPara,
            Trecho = viatura.Trecho,
            Destino = viatura.Destino,
            SemRota = viatura.SemRota,
            Rota = viatura.Rota.Select(PassoRotaResponse.De).ToList(),
            Previsao = viatura.Previsao.ToList(),
            OcorrenciaId = viatura.Ocorrencia?.Id,
            AtendimentoRestanteSeg = viatura.Modo == ModoViatura.Atendimento && viatura.AtendimentoAteSeg.HasValue
                ? Math.Round(Math.Max(0, viatura.AtendimentoAteSeg.Value - tempoSeg), 2)
                : 0,
            Hospital = viatura.Ocorrencia?.Hospital?.Nome
        };
    }
}
