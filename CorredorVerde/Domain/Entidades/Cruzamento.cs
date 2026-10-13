namespace CorredorVerde.Domain.Entidades
{
    public class Cruzamento
    {
        public string Id { get; set; } = string.Empty;
        public int Coluna { get; set; }
        public int Linha { get; set; }
        public Semaforo Semaforo { get; set; } = new();
        public List<ReservaCorredor> Reservas { get; set; } = new();
    }
}
