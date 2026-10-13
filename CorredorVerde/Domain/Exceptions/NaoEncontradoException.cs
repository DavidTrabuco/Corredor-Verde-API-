namespace CorredorVerde.Domain.Exceptions
{
    // 404 - recurso não existe
    public class NaoEncontradoException : DomainException
    {
        public NaoEncontradoException(string mensagem) : base(mensagem)
        {
        }
    }
}
