using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Options;

namespace CorredorVerde.Domain.Entidades
{
    // Estado inteiro da simulação, em memória. Singleton: todos os services compartilham esta instância.
    public class Cidade
    {
        // Todo método público dos services que lê ou altera a cidade usa lock (Trava)
        public object Trava { get; } = new();

        public SimulacaoOptions Config { get; private set; } = new();

        public Dictionary<string, Cruzamento> Cruzamentos { get; } = new();
        public Dictionary<string, Viatura> Viaturas { get; } = new();
        public List<Ocorrencia> Ocorrencias { get; } = new();
        public List<Hospital> Hospitais { get; } = new();
        public List<Base> Bases { get; } = new();
        public List<EventoLog> Log { get; } = new();
        public Dictionary<string, int> Contagem { get; } = new();
        public HashSet<string> JaLogado { get; } = new();

        public double TempoSeg { get; set; }
        public bool Pausado { get; set; }
        public int Velocidade { get; set; } = 1;
        public int SeqOcorrencia { get; set; }
        public int SeqLog { get; set; }

        public Cidade(SimulacaoOptions opcoes)
        {
            Reiniciar(opcoes);
        }

        public string IdDe(int coluna, int linha) => "C" + (linha * Config.Colunas + coluna + 1);

        // Monta a malha, os semáforos e as viaturas a partir da configuração
        public void Reiniciar(SimulacaoOptions opcoes)
        {
            lock (Trava)
            {
                Config = opcoes;
                Cruzamentos.Clear();
                Viaturas.Clear();
                Ocorrencias.Clear();
                Hospitais.Clear();
                Bases.Clear();
                Log.Clear();
                Contagem.Clear();
                JaLogado.Clear();
                TempoSeg = 0;
                Pausado = false;
                Velocidade = 1;
                SeqOcorrencia = 0;
                SeqLog = 0;

                for (var linha = 0; linha < opcoes.Linhas; linha++)
                {
                    for (var coluna = 0; coluna < opcoes.Colunas; coluna++)
                    {
                        var id = IdDe(coluna, linha);
                        // fases defasadas para a malha não trocar toda de uma vez
                        var defasagem = (coluna * 3 + linha * 5) % 10;
                        Cruzamentos[id] = new Cruzamento
                        {
                            Id = id,
                            Coluna = coluna,
                            Linha = linha,
                            Semaforo = new Semaforo
                            {
                                EixoVerde = (coluna + linha) % 2 == 0 ? Eixo.NS : Eixo.LO,
                                EmAmarelo = false,
                                DesdeSeg = -defasagem,
                                EmFalha = false
                            }
                        };
                    }
                }

                foreach (var h in opcoes.Hospitais.Where(h => Cruzamentos.ContainsKey(h.Cruzamento)))
                    Hospitais.Add(new Hospital { Id = h.Id, Nome = h.Nome, Cruzamento = h.Cruzamento });

                foreach (var b in opcoes.Bases)
                    Bases.Add(new Base { Nome = b.Nome, Cruzamento = b.Cruzamento, Tipo = b.Tipo });

                foreach (var v in opcoes.Viaturas)
                {
                    var baseDoTipo = Bases.FirstOrDefault(b => b.Tipo == v.Tipo && Cruzamentos.ContainsKey(b.Cruzamento));
                    var cruzamento = baseDoTipo?.Cruzamento ?? IdDe(0, 0);
                    Viaturas[v.Id] = new Viatura
                    {
                        Id = v.Id,
                        Tipo = v.Tipo,
                        Papel = v.Papel,
                        Base = cruzamento,
                        Cruzamento = cruzamento,
                        Modo = v.Papel == PapelViatura.Reserva ? ModoViatura.Reserva : ModoViatura.Patrulha
                    };
                }
            }
        }
    }
}
