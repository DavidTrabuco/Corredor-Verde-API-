namespace CorredorVerde.Application.DTO.Request
{
    // Os dois campos são opcionais: o que não vier fica como está
    public class ControleSimulacaoRequest
    {
        public bool? Pausado { get; set; }

        // 1, 2 ou 4 (o SimulacaoService valida)
        public int? Velocidade { get; set; }
    }
}
