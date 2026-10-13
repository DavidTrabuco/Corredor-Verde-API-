using CorredorVerde.Domain.Enums;

namespace CorredorVerde.Domain.Entidades
{
    public class Semaforo
    {
        // Direção que está com a fase atual (verde ou amarelo); a outra fica em vermelho
        public Eixo EixoVerde { get; set; }
        public bool EmAmarelo { get; set; }
        public double DesdeSeg { get; set; }
        public bool EmFalha { get; set; }
    }
}
