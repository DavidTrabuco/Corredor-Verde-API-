namespace CorredorVerde.Application.DTO.Request
{
    // Os campos aceitam qualquer valor de propósito: quem valida é o OcorrenciaService,
    // que devolve a mensagem em português no ProblemDetails (RNF04)
    public class CriarOcorrenciaRequest
    {
        public string? Tipo { get; set; }
        public string? Cruzamento { get; set; }
        public int? Criticidade { get; set; }
    }
}
