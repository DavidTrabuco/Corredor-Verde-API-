namespace CorredorVerde.Application.DTO.Request
{
    public class FalhaRequest
    {
        // true liga a falha (amarelo piscante); false volta ao ciclo normal
        public bool Ativa { get; set; }
    }
}
