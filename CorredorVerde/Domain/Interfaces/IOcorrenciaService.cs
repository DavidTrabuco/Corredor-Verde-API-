using CorredorVerde.Domain.Entidades;

namespace CorredorVerde.Domain.Interfaces
{
    public interface IOcorrenciaService
    {
        Ocorrencia Registrar(string? tipo, string? cruzamento, int? criticidade);
    }
}
