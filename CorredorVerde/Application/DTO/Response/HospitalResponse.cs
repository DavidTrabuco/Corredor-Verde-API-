using CorredorVerde.Domain.Entidades;

namespace CorredorVerde.Application.DTO.Response
{
    public class HospitalResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cruzamento { get; set; } = string.Empty;

        public static HospitalResponse De(Hospital hospital) => new()
        {
            Id = hospital.Id,
            Nome = hospital.Nome,
            Cruzamento = hospital.Cruzamento
        };
    }
}
