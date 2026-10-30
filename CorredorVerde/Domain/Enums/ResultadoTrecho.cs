namespace CorredorVerde.Domain.Enums
{
    // O que aconteceu ao tentar abrir o corredor de um trecho.
    // Quem chama decide o próximo passo (por exemplo, começar o atendimento quando já está no destino).
    public enum ResultadoTrecho
    {
        CorredorAberto,
        SemCaminho,
        JaNoDestino
    }
}
