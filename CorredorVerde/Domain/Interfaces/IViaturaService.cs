using CorredorVerde.Domain.Entidades;

namespace CorredorVerde.Domain.Interfaces
{
    public interface IViaturaService
    {
        Viatura Obter(string id);
        void Saida(string viaturaId, string de, string para);
        void Chegada(string viaturaId, string cruzamento);
        void IniciarTrecho(Viatura viatura);
        void ChegouAoDestino(Viatura viatura);
        void FimDoAtendimento(Viatura viatura);
        void TerminarServico(Viatura viatura);
        void IniciarRetorno(Viatura viatura);
        void ChegouNaBase(Viatura viatura);
        void VoltarAPatrulha(Viatura viatura);
    }
}
