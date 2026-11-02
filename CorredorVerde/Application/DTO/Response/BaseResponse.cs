using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Application.DTO.Response
{
    public class BaseResponse
    {
        public string Nome { get; set; } = string.Empty;
        public string Cruzamento { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }

        public static BaseResponse De(Base baseDaViatura) => new()
        {
            Nome = baseDaViatura.Nome,
            Cruzamento = baseDaViatura.Cruzamento,
            Tipo = baseDaViatura.Tipo
        };
    }
}
