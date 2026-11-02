using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class SemaforoResponse
    {
        public string Cruzamento { get; set; } = string.Empty;
        public Luz Ns { get; set; }
        public Luz Lo { get; set; }
        public bool Falha { get; set; }
        public List<ReservaResponse> Reservas { get; set; } = new();

        // Viatura com a prioridade no cruzamento (RN12); null quando não há reserva
        public string? Vencedor { get; set; }

        // As luzes e o vencedor vêm do SemaforoService (é ele quem sabe as regras)
        public static SemaforoResponse De(Cruzamento cruzamento, Luz ns, Luz lo, string? vencedor) => new()
        {
            Cruzamento = cruzamento.Id,
            Ns = ns,
            Lo = lo,
            Falha = cruzamento.Semaforo.EmFalha,
            Reservas = cruzamento.Reservas.Select(ReservaResponse.De).ToList(),
            Vencedor = vencedor
        };
    }
}
