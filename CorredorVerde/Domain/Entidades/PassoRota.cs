using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class PassoRota
    {
        public string Cruzamento { get; set; } = string.Empty;
        public Eixo Eixo { get; set; }
    }
}
