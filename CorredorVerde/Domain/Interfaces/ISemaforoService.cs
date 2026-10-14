using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Interfaces
{
    public interface ISemaforoService
    {
        Luz Luz(string cruzamentoId, Eixo eixo);
        ReservaCorredor? Vencedor(Cruzamento cruzamento);
        void Atualizar(Cruzamento cruzamento);
        ResultadoTravessia PodeAtravessar(string cruzamentoId, Eixo eixo, string? viaturaId);
        bool Liberar(Cruzamento cruzamento, string viaturaId);
    }
}
