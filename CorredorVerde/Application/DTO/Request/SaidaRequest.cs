using System.ComponentModel.DataAnnotations;

namespace CorredorVerde.Application.DTO.Request
{
    public class SaidaRequest
    {
        [Required(ErrorMessage = "Informe o cruzamento de onde a viatura saiu (de).")]
        public string De { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o cruzamento para onde a viatura vai (para).")]
        public string Para { get; set; } = string.Empty;
    }
}
