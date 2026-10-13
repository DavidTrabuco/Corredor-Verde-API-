namespace CorredorVerde.Domain.Exceptions
{
    // 400 - dado de entrada inválido (ex.: criticidade fora de 1 a 3)
    public class ValidacaoException : DomainException
    {
        public ValidacaoException(string mensagem) : base(mensagem)
        {
        }
    }
}
