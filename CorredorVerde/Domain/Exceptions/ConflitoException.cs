namespace CorredorVerde.Domain.Exceptions
{
    // 409 - estado conflitante (ex.: nenhuma viatura livre)
    public class ConflitoException : DomainException
    {
        public ConflitoException(string mensagem) : base(mensagem)
        {
        }
    }
}
