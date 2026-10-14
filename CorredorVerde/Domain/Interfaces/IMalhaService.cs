using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Interfaces
{
    public interface IMalhaService
    {
        Cruzamento ObterCruzamento(string id);
        string? Vizinho(string id, Direcao direcao);
        Direcao? DirecaoEntre(string a, string b);
        List<string>? MenorCaminho(string origem, string destino);
        List<PassoRota> MontarRota(string origem, List<string> caminho);
        Hospital? HospitalMaisProximo(string cruzamento);
    }
}
