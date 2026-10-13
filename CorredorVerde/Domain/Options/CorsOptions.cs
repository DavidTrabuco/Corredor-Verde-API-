using System.ComponentModel.DataAnnotations;

namespace CorredorVerde.Domain.Options
{
    public class CorsOptions
    {
        public const string Secao = "Cors";

        [Required, MinLength(1)]
        public string[] Origens { get; set; } = Array.Empty<string>();
    }
}
