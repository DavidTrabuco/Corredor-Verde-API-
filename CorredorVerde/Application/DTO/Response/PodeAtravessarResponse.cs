using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class PodeAtravessarResponse
    {
        public bool Pode { get; set; }

        // Semáforo em falha: passa, mas espera CautelaSeg na faixa antes (RN13)
        public bool Cautela { get; set; }

        public Luz Luz { get; set; }

        public static PodeAtravessarResponse De(ResultadoTravessia resultado) => new()
        {
            Pode = resultado.Pode,
            Cautela = resultado.Cautela,
            Luz = resultado.Luz
        };
    }
}
