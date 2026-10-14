using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    // Resposta de "posso atravessar?" (RN07). Cautela = semáforo em falha: passa devagar (RN13).
    public record ResultadoTravessia(bool Pode, bool Cautela, Luz Luz);
}
