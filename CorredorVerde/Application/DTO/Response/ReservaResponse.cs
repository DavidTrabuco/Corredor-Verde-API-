using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class ReservaResponse
    {
        public string ViaturaId { get; set; } = string.Empty;
        public Eixo Eixo { get; set; }

        public static ReservaResponse De(ReservaCorredor reserva) => new()
        {
            ViaturaId = reserva.ViaturaId,
            Eixo = reserva.Eixo
        };
    }
}
