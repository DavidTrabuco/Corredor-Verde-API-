using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // RN01, RN02, RN03: ciclo fixo com amarelo. RN07: só atravessa com verde. RN13: falha = piscante.
    public class SemaforoService : ISemaforoService
    {
        // Tolerância para a soma de passos de 0,1 s não perder um tick por arredondamento
        private const double Epsilon = 1e-9;

        private readonly Cidade _cidade;
        private readonly IMalhaService _malha;
        private readonly ILogEventosService _log;

        public SemaforoService(Cidade cidade, IMalhaService malha, ILogEventosService log)
        {
            _cidade = cidade;
            _malha = malha;
            _log = log;
        }

        public Luz Luz(string cruzamentoId, Eixo eixo)
        {
            lock (_cidade.Trava)
            {
                var s = _malha.ObterCruzamento(cruzamentoId).Semaforo;
                if (s.EmFalha)
                    return Domain.Enums.Luz.Piscante;
                if (s.EixoVerde != eixo)
                    return Domain.Enums.Luz.Vermelho;
                return s.EmAmarelo ? Domain.Enums.Luz.Amarelo : Domain.Enums.Luz.Verde;
            }
        }

        // RN12: maior criticidade (1 é a mais grave); em empate, a ocorrência registrada primeiro
        public ReservaCorredor? Vencedor(Cruzamento cruzamento)
        {
            lock (_cidade.Trava)
            {
                return cruzamento.Reservas
                    .OrderBy(r => r.Criticidade)
                    .ThenBy(r => r.Ordem)
                    .FirstOrDefault();
            }
        }

        public void Atualizar(Cruzamento cruzamento)
        {
            lock (_cidade.Trava)
            {
                var s = cruzamento.Semaforo;
                var cfg = _cidade.Config;
                var t = _cidade.TempoSeg;

                if (s.EmFalha)
                    return;

                var vencedor = Vencedor(cruzamento);
                if (vencedor is not null)
                {
                    if (s.EixoVerde == vencedor.Eixo)
                    {
                        // Amarelo da direção do corredor volta ao verde: ninguém perde o verde, a RN02 continua valendo
                        if (s.EmAmarelo)
                        {
                            s.EmAmarelo = false;
                            s.DesdeSeg = t;
                        }
                        return;
                    }

                    if (!s.EmAmarelo)
                    {
                        s.EmAmarelo = true;
                        s.DesdeSeg = t;
                        _log.Contar("RN02");
                        return;
                    }

                    if (t - s.DesdeSeg >= cfg.AmareloSeg - Epsilon)
                        TrocarFase(s);
                    return;
                }

                if (!s.EmAmarelo && t - s.DesdeSeg >= cfg.VerdeSeg - Epsilon)
                {
                    s.EmAmarelo = true;
                    s.DesdeSeg = t;
                    _log.Contar("RN02");
                }
                else if (s.EmAmarelo && t - s.DesdeSeg >= cfg.AmareloSeg - Epsilon)
                {
                    TrocarFase(s);
                    _log.Contar("RN03");
                }
            }
        }

        // RN07: só atravessa com verde; em falha passa com cautela (RN13)
        public ResultadoTravessia PodeAtravessar(string cruzamentoId, Eixo eixo, string? viaturaId)
        {
            lock (_cidade.Trava)
            {
                var luz = Luz(cruzamentoId, eixo);
                if (luz == Domain.Enums.Luz.Verde)
                    return new ResultadoTravessia(true, false, luz);
                if (luz == Domain.Enums.Luz.Piscante)
                    return new ResultadoTravessia(true, true, luz);

                Viatura? viatura = null;
                if (viaturaId is not null)
                    _cidade.Viaturas.TryGetValue(viaturaId, out viatura);

                // A espera só vai para o log quando a viatura está em emergência; fora disso, só conta
                if (viatura is null || viatura.Modo != ModoViatura.Emergencia)
                {
                    _log.Contar("RN07");
                    return new ResultadoTravessia(false, false, luz);
                }

                var chave = $"espera:{viaturaId}:{cruzamentoId}";
                if (_cidade.JaLogado.Add(chave))
                {
                    _log.Registrar(new[] { "RN07", "UC01" },
                        $"{viaturaId} parou na faixa de {cruzamentoId}: {luz.ToString().ToLowerInvariant()} para {NomeEixo(eixo)}. Espera o verde.");
                }

                return new ResultadoTravessia(false, false, luz);
            }
        }

        // RN08: tira a reserva da viatura; sem outras reservas, o cruzamento volta ao ciclo normal
        public bool Liberar(Cruzamento cruzamento, string viaturaId)
        {
            lock (_cidade.Trava)
            {
                var antes = cruzamento.Reservas.Count;
                cruzamento.Reservas = cruzamento.Reservas.Where(r => r.ViaturaId != viaturaId).ToList();
                if (cruzamento.Reservas.Count == antes)
                    return false;

                if (cruzamento.Reservas.Count == 0)
                    cruzamento.Semaforo.DesdeSeg = _cidade.TempoSeg;

                Atualizar(cruzamento);
                return true;
            }
        }

        private void TrocarFase(Semaforo s)
        {
            s.EixoVerde = s.EixoVerde == Eixo.NS ? Eixo.LO : Eixo.NS;
            s.EmAmarelo = false;
            s.DesdeSeg = _cidade.TempoSeg;
            _log.Contar("UC02");
            _log.Contar("RN01");
        }

        public static string NomeEixo(Eixo eixo) => eixo == Eixo.NS ? "Norte-Sul" : "Leste-Oeste";
    }
}
