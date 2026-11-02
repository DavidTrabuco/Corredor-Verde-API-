using System.ComponentModel.DataAnnotations;

namespace CorredorVerde.Application.DTO.Request
{
    public class ChegadaRequest
    {
        [Required(ErrorMessage = "Informe o cruzamento onde a viatura chegou.")]
        public string Cruzamento { get; set; } = string.Empty;
    }
}
