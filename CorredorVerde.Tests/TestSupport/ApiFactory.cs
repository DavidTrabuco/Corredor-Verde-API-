using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CorredorVerde.Application.DTO.Response;
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

        // Registra uma emergência pela API e já confere que deu certo (201), para a preparação do teste nunca falhar em silêncio
        public static async Task<OcorrenciaResponse> RegistrarEmergencia(HttpClient clienteHttp, string tipo, string cruzamento, int criticidade)
        {
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias", new { tipo, cruzamento, criticidade });
            Assert.Equal(HttpStatusCode.Created, respostaHttp.StatusCode);
            var ocorrencia = await Ler<OcorrenciaResponse>(respostaHttp);
            Assert.NotNull(ocorrencia);
            return ocorrencia;
        }

        // Lê o estado como a maquete: GET /api/v1/estado
        public static async Task<EstadoResponse> LerEstado(HttpClient clienteHttp)
        {
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/estado");
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var estado = await Ler<EstadoResponse>(respostaHttp);
            Assert.NotNull(estado);
            return estado;
        }

        // Lê o log de eventos como a maquete: GET /api/v1/log
        public static async Task<List<EventoLogResponse>> LerLog(HttpClient clienteHttp)
        {
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/log");
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var eventos = await Ler<List<EventoLogResponse>>(respostaHttp);
            Assert.NotNull(eventos);
            return eventos;
        }

        // Procura a viatura pelo id (e não pela posição na lista, que pode mudar)
        public static ViaturaResponse ViaturaDe(EstadoResponse estado, string idDaViatura)
        {
            var viatura = estado.Viaturas.Find(v => v.Id == idDaViatura);
            Assert.NotNull(viatura);
            return viatura;
        }

        // Procura o semáforo pelo cruzamento
        public static SemaforoResponse SemaforoDe(EstadoResponse estado, string idDoCruzamento)
        {
            var semaforo = estado.Semaforos.Find(s => s.Cruzamento == idDoCruzamento);
            Assert.NotNull(semaforo);
            return semaforo;
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
