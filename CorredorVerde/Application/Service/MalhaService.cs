using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // RN05: menor caminho em quarteirões, sem passar por semáforo em falha
    public class MalhaService : IMalhaService
    {
        private readonly Cidade _cidade;

        public MalhaService(Cidade cidade)
        {
            _cidade = cidade;
        }

        public Cruzamento ObterCruzamento(string id)
        {
            lock (_cidade.Trava)
            {
                if (!_cidade.Cruzamentos.TryGetValue(id ?? string.Empty, out var cruzamento))
                    throw new ValidacaoException($"Cruzamento {id} não existe na malha.");
                return cruzamento;
            }
        }

        public string? Vizinho(string id, Direcao direcao)
        {
            lock (_cidade.Trava)
            {
                var c = ObterCruzamento(id);
                var (dx, dy) = direcao.Deslocamento();
                var coluna = c.Coluna + dx;
                var linha = c.Linha + dy;

                if (coluna < 0 || linha < 0 || coluna >= _cidade.Config.Colunas || linha >= _cidade.Config.Linhas)
                    return null;

                return _cidade.IdDe(coluna, linha);
            }
        }

        public Direcao? DirecaoEntre(string a, string b)
        {
            lock (_cidade.Trava)
            {
                foreach (var direcao in DirecaoExtensoes.Ordem)
                {
                    if (Vizinho(a, direcao) == b)
                        return direcao;
                }
                return null;
            }
        }

        // BFS na ordem N, L, S, O. Pula cruzamentos em falha, menos o destino.
        // Retorna os cruzamentos depois da origem; lista vazia se origem == destino; null se não há caminho.
        public List<string>? MenorCaminho(string origem, string destino)
        {
            lock (_cidade.Trava)
            {
                ObterCruzamento(origem);
                ObterCruzamento(destino);

                if (origem == destino)
                    return new List<string>();

                var anterior = new Dictionary<string, string?> { [origem] = null };
                var fila = new Queue<string>();
                fila.Enqueue(origem);

                while (fila.Count > 0)
                {
                    var atual = fila.Dequeue();
                    foreach (var direcao in DirecaoExtensoes.Ordem)
                    {
                        var vizinho = Vizinho(atual, direcao);
                        if (vizinho is null || anterior.ContainsKey(vizinho))
                            continue;
                        if (vizinho != destino && _cidade.Cruzamentos[vizinho].Semaforo.EmFalha)
                            continue;

                        anterior[vizinho] = atual;
                        if (vizinho == destino)
                        {
                            var caminho = new List<string>();
                            for (var x = destino; x != origem; x = anterior[x]!)
                                caminho.Insert(0, x);
                            return caminho;
                        }
                        fila.Enqueue(vizinho);
                    }
                }

                return null;
            }
        }

        // Cada passo leva o eixo em que a viatura chega ao cruzamento
        public List<PassoRota> MontarRota(string origem, List<string> caminho)
        {
            lock (_cidade.Trava)
            {
                var rota = new List<PassoRota>();
                var anterior = origem;
                foreach (var cruzamento in caminho)
                {
                    var direcao = DirecaoEntre(anterior, cruzamento)!.Value;
                    rota.Add(new PassoRota { Cruzamento = cruzamento, Eixo = direcao.Eixo() });
                    anterior = cruzamento;
                }
                return rota;
            }
        }

        public Hospital? HospitalMaisProximo(string cruzamento)
        {
            lock (_cidade.Trava)
            {
                Hospital? melhor = null;
                var melhorDistancia = int.MaxValue;

                foreach (var hospital in _cidade.Hospitais)
                {
                    var caminho = MenorCaminho(cruzamento, hospital.Cruzamento);
                    if (caminho is not null && caminho.Count < melhorDistancia)
                    {
                        melhor = hospital;
                        melhorDistancia = caminho.Count;
                    }
                }

                return melhor;
            }
        }
    }
}
