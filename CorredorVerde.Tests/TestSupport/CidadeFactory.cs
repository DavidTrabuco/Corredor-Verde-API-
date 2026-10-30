using CorredorVerde.Application.Service;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;

namespace CorredorVerde.Tests.TestSupport
{
    // Monta a cidade de teste com todos os services ligados (sem o relógio de verdade).
    // Os métodos de atalho abaixo deixam os testes curtos e fáceis de ler.
    public class CidadeFactory
    {
        public Cidade Cidade { get; }
        public ILogEventosService Log { get; }
        public IMalhaService Malha { get; }
        public ISemaforoService Semaforo { get; }
        public ICorredorService Corredor { get; }

        private CidadeFactory(SimulacaoOptions opcoes)
        {
            Cidade = new Cidade(opcoes);
            Log = new LogEventosService(Cidade);
            Malha = new MalhaService(Cidade);
            Semaforo = new SemaforoService(Cidade, Malha, Log);
            Corredor = new CorredorService(Cidade, Malha, Semaforo, Log);
        }

        // Cria a cidade com as opções do appsettings.json; "ajustar" permite trocar algum valor
        public static CidadeFactory Criar(Action<SimulacaoOptions>? ajustar = null)
        {
            var opcoes = OpcoesPadrao();
            ajustar?.Invoke(opcoes);
            return new CidadeFactory(opcoes);
        }

        // ---------- tempo ----------

        // Faz o tempo andar em passos de 0,1 s; a cada passo, todos os semáforos se atualizam
        public void Avancar(double segundos)
        {
            var passos = (int)Math.Round(segundos / 0.1);
            for (var i = 0; i < passos; i++)
            {
                Cidade.TempoSeg += 0.1;
                foreach (var cruzamento in Cidade.Cruzamentos.Values)
                    Semaforo.Atualizar(cruzamento);
            }
        }

        // ---------- pegar coisas da cidade ----------

        public Cruzamento PegarCruzamento(string id) => Cidade.Cruzamentos[id];

        public Viatura PegarViatura(string id) => Cidade.Viaturas[id];

        public Luz LuzDe(string cruzamento, Eixo eixo) => Semaforo.Luz(cruzamento, eixo);

        public List<string> IdsDosCruzamentos() => Cidade.Cruzamentos.Keys.ToList();

        // ---------- montar o cenário ----------

        // O semáforo começa agora com verde para o eixo informado
        public void SemaforoComeca(string cruzamento, Eixo eixoVerde)
        {
            var semaforo = PegarCruzamento(cruzamento).Semaforo;
            semaforo.EixoVerde = eixoVerde;
            semaforo.EmAmarelo = false;
            semaforo.DesdeSeg = Cidade.TempoSeg;
        }

        public void ColocarEmFalha(params string[] cruzamentos)
        {
            foreach (var id in cruzamentos)
                PegarCruzamento(id).Semaforo.EmFalha = true;
        }

        // Põe uma reserva de corredor à mão, sem passar pelo CorredorService
        public void Reservar(string cruzamento, string viaturaId, Eixo eixo, int criticidade, int ordem)
        {
            PegarCruzamento(cruzamento).Reservas.Add(new ReservaCorredor
            {
                ViaturaId = viaturaId,
                OcorrenciaId = $"OC-{ordem:000}",
                Eixo = eixo,
                Criticidade = criticidade,
                Ordem = ordem
            });
        }

        // Faz o papel do OcorrenciaService: cria a ocorrência e põe a viatura em emergência
        public Viatura ColocarEmEmergencia(string viaturaId, string de, string ate, int criticidade)
        {
            var viatura = PegarViatura(viaturaId);
            var ordem = ++Cidade.SeqOcorrencia;
            var ocorrencia = new Ocorrencia
            {
                Id = $"OC-{ordem:000}",
                Cruzamento = ate,
                Criticidade = criticidade,
                Tipo = viatura.Tipo,
                ViaturaId = viatura.Id,
                Ordem = ordem,
                Status = StatusOcorrencia.ACaminho,
                RegistradaEmSeg = Cidade.TempoSeg
            };
            Cidade.Ocorrencias.Add(ocorrencia);

            viatura.Cruzamento = de;
            viatura.IndoPara = null;
            viatura.Modo = ModoViatura.Emergencia;
            viatura.Ocorrencia = ocorrencia;
            viatura.Trecho = 1;
            viatura.Destino = ate;
            return viatura;
        }

        // ---------- conferir o resultado ----------

        // Ex.: "C12 → C13 → C14"
        public string RotaComoTexto(Viatura viatura) =>
            string.Join(" → ", viatura.Rota.Select(passo => passo.Cruzamento));

        // Ex.: "C12 → C13", ou "sem caminho"
        public string CaminhoComoTexto(string origem, string destino)
        {
            var caminho = Malha.MenorCaminho(origem, destino);
            return caminho is null ? "sem caminho" : string.Join(" → ", caminho);
        }

        // Ex.: "AMB-01 (LO), BOM-01 (NS)", ou "nenhuma"
        public string ReservasComoTexto(string cruzamento)
        {
            var reservas = PegarCruzamento(cruzamento).Reservas;
            if (reservas.Count == 0)
                return "nenhuma";
            return string.Join(", ", reservas.Select(r => $"{r.ViaturaId} ({r.Eixo})"));
        }

        public bool NenhumCruzamentoReservado() =>
            Cidade.Cruzamentos.Values.All(c => c.Reservas.Count == 0);

        public bool LogTem(string mensagem) =>
            Cidade.Log.Any(evento => evento.Mensagem == mensagem);

        public int QuantosLogsCom(string tag) =>
            Cidade.Log.Count(evento => evento.Tags.Contains(tag));

        // Quantos cruzamentos estão, agora, com verde nas duas direções (o certo é sempre 0)
        public int ContarCruzamentosComDoisVerdes()
        {
            var total = 0;
            foreach (var id in IdsDosCruzamentos())
            {
                if (LuzDe(id, Eixo.NS) == Luz.Verde && LuzDe(id, Eixo.LO) == Luz.Verde)
                    total++;
            }
            return total;
        }

        // Quarteirões andando pela grade: diferença de colunas + diferença de linhas
        public int QuarteiroesEntre(string a, string b)
        {
            var ca = PegarCruzamento(a);
            var cb = PegarCruzamento(b);
            return Math.Abs(ca.Coluna - cb.Coluna) + Math.Abs(ca.Linha - cb.Linha);
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
