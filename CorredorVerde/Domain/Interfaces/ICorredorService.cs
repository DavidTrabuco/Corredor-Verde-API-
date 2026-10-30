using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Interfaces
{
    public interface ICorredorService
    {
        ResultadoTrecho IniciarTrecho(Viatura viatura);
        void VerificarConflito(Cruzamento cruzamento);
        List<string> LiberarTodas(Viatura viatura);
    }
}
