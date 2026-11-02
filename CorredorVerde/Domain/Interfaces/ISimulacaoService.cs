using CorredorVerde.Application.DTO.Response;

namespace CorredorVerde.Domain.Interfaces
{
    // As leituras já saem como DTO, montadas dentro do lock:
    // assim o relógio não muda a cidade no meio da cópia.
    public interface ISimulacaoService
    {
        MalhaResponse Malha();
        EstadoResponse Estado();
        List<SemaforoResponse> Semaforos();
        List<ViaturaResponse> Viaturas();
        List<OcorrenciaResponse> Ocorrencias();
        void Controlar(bool? pausado, int? velocidade);
        void Reiniciar();
    }
}
