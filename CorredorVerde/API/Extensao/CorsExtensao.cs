using CorredorVerde.Domain.Options;

namespace CorredorVerde.API.Extensao
{
    public static class CorsExtensao
    {
        public const string Politica = "Maquete";

        // Libera a maquete (Vite em http://localhost:5173) a chamar a API. As origens ficam em Cors:Origens no appsettings.
        public static IServiceCollection AddCorsMaquete(this IServiceCollection services, IConfiguration configuracao)
        {
            var origens = configuracao.GetSection(CorsOptions.Secao).Get<CorsOptions>()?.Origens ?? Array.Empty<string>();

            services.AddCors(opcoes =>
            {
                opcoes.AddPolicy(Politica, politica => politica
                    .WithOrigins(origens)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            return services;
        }
    }
}
