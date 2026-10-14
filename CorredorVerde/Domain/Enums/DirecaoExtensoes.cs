namespace CorredorVerde.Domain.Enums
{
    public static class DirecaoExtensoes
    {
        // Ordem em que o BFS e a busca de direção percorrem os vizinhos
        public static readonly Direcao[] Ordem = { Direcao.N, Direcao.L, Direcao.S, Direcao.O };

        public static Eixo Eixo(this Direcao direcao) =>
            direcao is Direcao.N or Direcao.S ? Enums.Eixo.NS : Enums.Eixo.LO;

        public static (int Coluna, int Linha) Deslocamento(this Direcao direcao) => direcao switch
        {
            Direcao.N => (0, -1),
            Direcao.S => (0, 1),
            Direcao.L => (1, 0),
            _ => (-1, 0)
        };
    }
}
