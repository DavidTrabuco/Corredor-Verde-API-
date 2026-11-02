namespace CorredorVerde.Application.DTO.Response
{
    // GET /api/v1/estado: tudo o que a maquete precisa a cada 250 ms (RNF08)
    public class EstadoResponse
    {
        public double TempoSeg { get; set; }
        public bool Pausado { get; set; }
        public int Velocidade { get; set; }
        public List<SemaforoResponse> Semaforos { get; set; } = new();
        public List<ViaturaResponse> Viaturas { get; set; } = new();
        public List<OcorrenciaResponse> Ocorrencias { get; set; } = new();
    }
}
