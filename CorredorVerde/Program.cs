using System.Text.Json.Serialization;
using CorredorVerde.API.Extensao;
using CorredorVerde.API.Filtros;
using CorredorVerde.Application.Service;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;
using CorredorVerde.Infrastructure.Simulacao;
using CorredorVerde.Middlewares;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// RNF12: tempos e malha vêm do appsettings; configuração inválida derruba a API logo ao subir
builder.Services.AddOptions<SimulacaoOptions>()
    .Bind(builder.Configuration.GetSection(SimulacaoOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers(o => o.Filters.Add<RegistroChamadaFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // Erros de validação do [ApiController] também saem como ProblemDetails em português (RNF04)
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ValidacaoExtensao.RespostaDeErro);
builder.Services.AddEndpointsApiExplorer();

// Erros padronizados: tudo sai como ProblemDetails (RFC 9457)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSwaggerGen();

// Estado da simulação em memória: uma cidade só, compartilhada por todos os services (por isso tudo é Singleton)
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new Cidade(sp.GetRequiredService<IOptions<SimulacaoOptions>>().Value));
builder.Services.AddSingleton<ILogEventosService, LogEventosService>();
builder.Services.AddSingleton<IMalhaService, MalhaService>();
builder.Services.AddSingleton<ISemaforoService, SemaforoService>();
builder.Services.AddSingleton<ICorredorService, CorredorService>();
builder.Services.AddSingleton<IViaturaService, ViaturaService>();
builder.Services.AddSingleton<IOcorrenciaService, OcorrenciaService>();
builder.Services.AddSingleton<IFalhaService, FalhaService>();
builder.Services.AddSingleton<ITemporizadorService, TemporizadorService>();
builder.Services.AddSingleton<ISimulacaoService, SimulacaoService>();

// Relógio da simulação (avança o tempo a cada 100 ms)
builder.Services.AddHostedService<RelogioSimulacaoWorker>();

builder.Services.AddCorsMaquete(builder.Configuration);

var app = builder.Build();

// Começa com o evento "Simulação pronta" no log
app.Services.GetRequiredService<ISimulacaoService>().Reiniciar();

// Primeiro do pipeline: envolve tudo o que vem depois
app.UseExceptionHandler();
// 404/405 sem corpo também viram ProblemDetails
app.UseStatusCodePages();

// Swagger sempre ligado (projeto de estudo)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsExtensao.Politica);

app.MapControllers();

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração
public partial class Program { }
