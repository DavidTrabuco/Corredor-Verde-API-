using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Infrastructure.Simulacao;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CorredorVerde.Tests.TestSupport
{
    // Sobe a API inteira (Program.cs, controllers, filtros e tratamento de erros) na memória.
    // O relógio automático é tirado: o tempo só anda quando o teste manda (Avancar), e assim o resultado nunca varia.
    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly Action<IServiceCollection>? _ajustarServicos;

        // Os enums saem como texto ("Verde", "Emergencia"), igual à API
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        // "ajustarServicos" permite trocar um service por um falso (ex.: um que sempre dá erro)
        public ApiFactory(Action<IServiceCollection>? ajustarServicos = null)
        {
            _ajustarServicos = ajustarServicos;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(servicos =>
            {
                RetirarORelogio(servicos);
                _ajustarServicos?.Invoke(servicos);
            });
        }

        // A cidade e os services reais, para montar o cenário ou conferir o resultado por dentro
        public Cidade Cidade => Services.GetRequiredService<Cidade>();

        public IOcorrenciaService Emergencias => Services.GetRequiredService<IOcorrenciaService>();

        // Faz o tempo andar, do mesmo jeito que o relógio da API faria
        public void Avancar(double segundos)
        {
            Services.GetRequiredService<ITemporizadorService>().Avancar(segundos);
        }

        // Lê o corpo da resposta já como objeto
        public static async Task<T?> Ler<T>(HttpResponseMessage resposta)
        {
            return await resposta.Content.ReadFromJsonAsync<T>(Json);
        }

        private static void RetirarORelogio(IServiceCollection servicos)
        {
            for (var i = servicos.Count - 1; i >= 0; i--)
            {
                if (servicos[i].ImplementationType == typeof(RelogioSimulacaoWorker))
                    servicos.RemoveAt(i);
            }
        }
    }
}
