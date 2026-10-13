namespace CorredorVerde.Domain.Exceptions
{
    // Base de toda exceção "esperada" do negócio.
    // A mensagem é segura para mostrar ao cliente (o handler global usa isso).
    public abstract class DomainException : Exception
    {
        protected DomainException(string mensagem) : base(mensagem)
        {
        }
    }
}
