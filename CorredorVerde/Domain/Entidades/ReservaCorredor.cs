using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    // Pedido de corredor verde de uma viatura num cruzamento (RN06)
    public class ReservaCorredor
    {
        public string ViaturaId { get; set; } = string.Empty;
        public string OcorrenciaId { get; set; } = string.Empty;
        public Eixo Eixo { get; set; }
        public int Criticidade { get; set; }

        // Ordem de registro da ocorrência: desempate da RN12
        public int Ordem { get; set; }
    }
}
