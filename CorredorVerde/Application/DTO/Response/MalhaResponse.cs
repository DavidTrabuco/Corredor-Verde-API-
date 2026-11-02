using CorredorVerde.Domain.Entidades;

namespace CorredorVerde.Application.DTO.Response
{
    // GET /api/v1/malha: a maquete lê uma vez, ao abrir
    public class MalhaResponse
    {
        public int Colunas { get; set; }
        public int Linhas { get; set; }
        public List<CruzamentoResponse> Cruzamentos { get; set; } = new();
        public List<HospitalResponse> Hospitais { get; set; } = new();
        public List<BaseResponse> Bases { get; set; } = new();

        public static MalhaResponse De(Cidade cidade) => new()
        {
            Colunas = cidade.Config.Colunas,
            Linhas = cidade.Config.Linhas,
            Cruzamentos = cidade.Cruzamentos.Values.Select(CruzamentoResponse.De).ToList(),
            Hospitais = cidade.Hospitais.Select(HospitalResponse.De).ToList(),
            Bases = cidade.Bases.Select(BaseResponse.De).ToList()
        };
    }
}
