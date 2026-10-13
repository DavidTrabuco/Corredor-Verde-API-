using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class Base
    {
        public string Nome { get; set; } = string.Empty;
        public string Cruzamento { get; set; } = string.Empty;
        public TipoViatura Tipo { get; set; }
    }
}
