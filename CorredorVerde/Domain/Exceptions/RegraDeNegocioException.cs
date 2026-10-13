namespace CorredorVerde.Domain.Exceptions
{
    // 422 - regra de negócio violada
    public class RegraDeNegocioException : DomainException
    {
        public RegraDeNegocioException(string mensagem) : base(mensagem)
        {
        }
    }
}
