using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class PassoRotaResponse
    {
        public string Cruzamento { get; set; } = string.Empty;
        public Eixo Eixo { get; set; }

        public static PassoRotaResponse De(PassoRota passo) => new()
        {
            Cruzamento = passo.Cruzamento,
            Eixo = passo.Eixo
        };
    }
}
