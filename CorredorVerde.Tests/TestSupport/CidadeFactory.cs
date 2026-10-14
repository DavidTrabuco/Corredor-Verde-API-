using CorredorVerde.Application.Service;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;

namespace CorredorVerde.Tests.TestSupport
{
    // Monta a Cidade e os services ligados, sem DI e sem o relógio de verdade.
    // Os testes avançam o tempo chamando o service direto, em passos de 0,1 s.
    public class CidadeFactory
    {
        public Cidade Cidade { get; }
        public ILogEventosService Log { get; }
        public IMalhaService Malha { get; }
        public ISemaforoService Semaforo { get; }

        private CidadeFactory(SimulacaoOptions opcoes)
        {
            Cidade = new Cidade(opcoes);
            Log = new LogEventosService(Cidade);
            Malha = new MalhaService(Cidade);
            Semaforo = new SemaforoService(Cidade, Malha, Log);
        }

        // Avança o relógio em passos de 0,1 s e atualiza os semáforos a cada passo.
        // Na etapa do TemporizadorService isto passa a chamar Avancar(...) dele.
        public void Avancar(double segundos, double passo = 0.1)
        {
            var passos = (int)Math.Round(segundos / passo);
            for (var i = 0; i < passos; i++)
            {
                Cidade.TempoSeg += passo;
                foreach (var cruzamento in Cidade.Cruzamentos.Values)
                    Semaforo.Atualizar(cruzamento);
            }
        }

        public static CidadeFactory Criar(Action<SimulacaoOptions>? ajustar = null)
        {
            var opcoes = OpcoesPadrao();
            ajustar?.Invoke(opcoes);
            return new CidadeFactory(opcoes);
        }

        // Mesmos valores do appsettings.json
        public static SimulacaoOptions OpcoesPadrao() => new()
        {
            Colunas = 5,
            Linhas = 4,
            VerdeSeg = 8,
            AmareloSeg = 2,
            AtendimentoSeg = 4,
            CautelaSeg = 1,
            TickMs = 100,
            Hospitais =
            {
                new HospitalOptions { Id = "H1", Nome = "Hospital Norte", Cruzamento = "C3" },
                new HospitalOptions { Id = "H2", Nome = "Hospital Sul", Cruzamento = "C18" }
            },
            Bases =
            {
                new BaseOptions { Nome = "Base do SAMU", Cruzamento = "C11", Tipo = TipoViatura.Ambulancia },
                new BaseOptions { Nome = "Quartel dos Bombeiros", Cruzamento = "C20", Tipo = TipoViatura.Bombeiros }
            },
            Viaturas =
            {
                new ViaturaOptions { Id = "AMB-01", Tipo = TipoViatura.Ambulancia, Papel = PapelViatura.Patrulha },
                new ViaturaOptions { Id = "AMB-02", Tipo = TipoViatura.Ambulancia, Papel = PapelViatura.Reserva },
                new ViaturaOptions { Id = "BOM-01", Tipo = TipoViatura.Bombeiros, Papel = PapelViatura.Patrulha },
                new ViaturaOptions { Id = "BOM-02", Tipo = TipoViatura.Bombeiros, Papel = PapelViatura.Reserva }
            }
        };
    }
}
