namespace CorredorVerde.Domain.Interfaces
{
    public interface IFalhaService
    {
        void Definir(string cruzamentoId, bool ativa);
    }
}
