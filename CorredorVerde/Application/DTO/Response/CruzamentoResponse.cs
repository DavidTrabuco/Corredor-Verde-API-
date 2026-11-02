using CorredorVerde.Domain.Entidades;

namespace CorredorVerde.Application.DTO.Response
{
    public class CruzamentoResponse
    {
        public string Id { get; set; } = string.Empty;
        public int Coluna { get; set; }
        public int Linha { get; set; }

        public static CruzamentoResponse De(Cruzamento cruzamento) => new()
        {
            Id = cruzamento.Id,
            Coluna = cruzamento.Coluna,
            Linha = cruzamento.Linha
        };
    }
}
