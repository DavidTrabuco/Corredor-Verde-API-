using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // UC01, UC06, UC07: a maquete avisa quando a viatura sai de um cruzamento e quando chega ao próximo.
    // RN09: atendimento no local e ida ao hospital. RN10: recalcula a rota quando a viatura sai dela.
    // RN11: bombeiros não vão ao hospital. RN15: a patrulha volta a patrulhar; a reserva volta à base sem corredor.
    public class ViaturaService : IViaturaService
    {
        private readonly Cidade _cidade;
        private readonly IMalhaService _malha;
        private readonly ISemaforoService _semaforo;
        private readonly ICorredorService _corredor;
        private readonly ILogEventosService _log;

        public ViaturaService(Cidade cidade, IMalhaService malha, ISemaforoService semaforo, ICorredorService corredor, ILogEventosService log)
        {
            _cidade = cidade;
            _malha = malha;
            _semaforo = semaforo;
            _corredor = corredor;
            _log = log;
        }

        public Viatura Obter(string id)
        {
            lock (_cidade.Trava)
            {
                if (!_cidade.Viaturas.TryGetValue(id ?? string.Empty, out var viatura))
                    throw new NaoEncontradoException($"Viatura {id} não existe.");
                return viatura;
            }
        }

        // A viatura saiu de "de" e entrou na rua que vai até "para"
        public void Saida(string viaturaId, string de, string para)
        {
            lock (_cidade.Trava)
            {
                var v = Obter(viaturaId);
                _malha.ObterCruzamento(de);
                _malha.ObterCruzamento(para);
                if (_malha.DirecaoEntre(de, para) is null)
                    throw new ValidacaoException($"{de} e {para} não são vizinhos.");
                if (v.Modo == ModoViatura.Atendimento)
                    throw new ConflitoException($"{v.Id} está em atendimento.");

                v.Cruzamento = de;
                v.IndoPara = para;

                // Voltando para a base: se pegou outra rua, só refaz o caminho (não tem corredor para liberar)
                if (v.Modo == ModoViatura.Retorno)
                {
                    if (v.SemRota || (v.Rota.Count > 0 && v.Rota[0].Cruzamento != para))
                        IniciarRetorno(v);
                    return;
                }

                if (v.Modo != ModoViatura.Emergencia)
                    return;

                // Estava sem caminho: tenta de novo a partir da rua nova
                if (v.SemRota)
                {
                    IniciarTrecho(v);
                    return;
                }

                // RN10: entrou numa rua que não é a da rota
                if (v.Rota.Count > 0 && v.Rota[0].Cruzamento != para)
                {
                    var antes = v.Rota.Select(r => r.Cruzamento).ToList();
                    _log.Registrar(new[] { "RN10", "UC07" },
                        $"{v.Id} saiu da rota em {de} (foi para {para}, a rota pedia {antes[0]}). Recalculando o trecho.",
                        TipoEvento.Alerta);

                    _corredor.LiberarTodas(v);
                    IniciarTrecho(v);

                    var depois = v.Rota.Select(r => r.Cruzamento).ToHashSet();
                    var soltos = antes.Where(c => !depois.Contains(c)).ToList();
                    if (soltos.Count > 0)
                    {
                        _log.Registrar(new[] { "RN10", "RN08" },
                            $"Saíram do trajeto e voltaram ao ciclo normal: {string.Join(", ", soltos)}.");
                    }
                }
            }
        }

        // A viatura chegou ao centro do cruzamento
        public void Chegada(string viaturaId, string cruzamento)
        {
            lock (_cidade.Trava)
            {
                var v = Obter(viaturaId);
                _malha.ObterCruzamento(cruzamento);

                v.Cruzamento = cruzamento;
                v.IndoPara = null;
                // A próxima espera neste cruzamento pode ir para o log de novo (RN07)
                _cidade.JaLogado.Remove($"espera:{v.Id}:{cruzamento}");
                _log.Contar("UC01");

                if (v.Modo == ModoViatura.Retorno)
                {
                    if (v.Rota.Count > 0 && v.Rota[0].Cruzamento == cruzamento)
                        v.Rota.RemoveAt(0);

                    if (cruzamento == v.Base)
                        ChegouNaBase(v);
                    else if (v.Rota.Count == 0 || v.SemRota)
                        IniciarRetorno(v);
                    return;
                }

                if (v.Modo != ModoViatura.Emergencia)
                    return;

                if (v.Rota.Count > 0 && v.Rota[0].Cruzamento == cruzamento)
                {
                    // RN08: passou pelo cruzamento do corredor, ele volta ao ciclo normal
                    v.Rota.RemoveAt(0);
                    if (_semaforo.Liberar(_cidade.Cruzamentos[cruzamento], v.Id))
                    {
                        _log.Registrar(new[] { "RN08", "UC06" },
                            $"{v.Id} passou por {cruzamento}: cruzamento liberado, volta ao ciclo normal.");
                    }
                }
                else if (cruzamento != v.Destino && !v.SemRota)
                {
                    // RN10: chegou a um cruzamento que não estava na rota
                    _log.Registrar(new[] { "RN10", "UC07" },
                        $"{v.Id} chegou a {cruzamento}, fora da rota. Recalculando o trecho.",
                        TipoEvento.Alerta);
                    _corredor.LiberarTodas(v);
                    IniciarTrecho(v);
                }

                if (v.Modo == ModoViatura.Emergencia && cruzamento == v.Destino && v.Rota.Count == 0)
                    ChegouAoDestino(v);
            }
        }

        // Abre o corredor do trecho atual. Se a viatura já está no destino, começa o atendimento direto.
        public void IniciarTrecho(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                if (_corredor.IniciarTrecho(viatura) == ResultadoTrecho.JaNoDestino)
                    ChegouAoDestino(viatura);
            }
        }

        // RN09: no local, 4 s de atendimento. No hospital (trecho 2), a ocorrência é encerrada.
        public void ChegouAoDestino(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                var oc = viatura.Ocorrencia
                    ?? throw new InvalidOperationException($"{viatura.Id} chegou ao destino sem ocorrência.");

                _corredor.LiberarTodas(viatura);
                viatura.Rota = new List<PassoRota>();

                if (viatura.Trecho == 1)
                {
                    viatura.Modo = ModoViatura.Atendimento;
                    viatura.AtendimentoAteSeg = _cidade.TempoSeg + _cidade.Config.AtendimentoSeg;
                    oc.Status = StatusOcorrencia.EmAtendimento;
                    _log.Registrar(new[] { "RN09", "UC05" },
                        $"{viatura.Id} chegou ao local de {oc.Id} ({oc.Cruzamento}). Atendimento de {_cidade.Config.AtendimentoSeg} s.");
                    return;
                }

                oc.Status = StatusOcorrencia.Encerrada;
                oc.EncerradaEmSeg = _cidade.TempoSeg;
                var hospital = oc.Hospital;
                var lugar = hospital is null ? viatura.Cruzamento : $"{hospital.Nome} ({hospital.Cruzamento})";
                _log.Registrar(new[] { "RN09", "UC05" },
                    $"{viatura.Id} chegou ao {lugar}. {oc.Id} encerrada.");
                TerminarServico(viatura);
            }
        }

        // Fim dos 4 s no local: bombeiros encerram ali (RN11); a ambulância segue ao hospital mais próximo (RN09, RN04)
        public void FimDoAtendimento(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                var oc = viatura.Ocorrencia
                    ?? throw new InvalidOperationException($"{viatura.Id} terminou um atendimento sem ocorrência.");

                if (viatura.Tipo != TipoViatura.Ambulancia)
                {
                    EncerrarNoLocal(viatura, oc, new[] { "RN11", "UC05" },
                        $"{viatura.Id} encerrou {oc.Id} no local. Bombeiros não seguem ao hospital.");
                    return;
                }

                // Recalcula: pode ter surgido uma falha no caminho desde o registro
                var hospital = _malha.HospitalMaisProximo(oc.Cruzamento) ?? oc.Hospital ?? _cidade.Hospitais.FirstOrDefault();
                if (hospital is null)
                {
                    EncerrarNoLocal(viatura, oc, new[] { "RN09", "UC05" },
                        $"{viatura.Id} encerrou {oc.Id} no local: não há hospital configurado.");
                    return;
                }

                oc.Hospital = hospital;
                oc.Status = StatusOcorrencia.ACaminhoHospital;
                viatura.Modo = ModoViatura.Emergencia;
                viatura.Trecho = 2;
                viatura.Destino = hospital.Cruzamento;
                viatura.Previsao = new List<string>();
                viatura.AtendimentoAteSeg = null;

                _log.Registrar(new[] { "RN09", "RN04", "UC05" },
                    $"Atendimento de {oc.Id} concluído. {viatura.Id} segue ao {hospital.Nome}, o hospital mais próximo.");
                IniciarTrecho(viatura);
            }
        }

        // RN15: a viatura de reserva volta para a base; a de patrulha volta a patrulhar dali mesmo
        public void TerminarServico(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                if (viatura.Papel == PapelViatura.Reserva)
                {
                    _log.Registrar(new[] { "RN15", "UC05" },
                        $"{viatura.Id} é de reserva: terminou o serviço e volta {NomeBase(viatura, "para")}, sem corredor verde.");
                    IniciarRetorno(viatura);
                    return;
                }

                VoltarAPatrulha(viatura);
                _log.Registrar(new[] { "RN15", "UC05" },
                    $"{viatura.Id} terminou o serviço e volta a patrulhar a partir de {viatura.IndoPara ?? viatura.Cruzamento}.");
            }
        }

        // RN15: volta para a base pelo menor caminho, sem corredor verde (não reserva cruzamentos)
        public void IniciarRetorno(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                viatura.Modo = ModoViatura.Retorno;
                viatura.Ocorrencia = null;
                viatura.Trecho = 0;
                viatura.Destino = viatura.Base;
                viatura.Previsao = new List<string>();
                viatura.AtendimentoAteSeg = null;

                if (viatura.IndoPara is null && viatura.Cruzamento == viatura.Base)
                {
                    ChegouNaBase(viatura);
                    return;
                }

                var origem = viatura.IndoPara ?? viatura.Cruzamento;
                var caminho = _malha.MenorCaminho(origem, viatura.Base);
                // RN13: já esperou 1 s sem desvio, então volta passando com cautela pelos cruzamentos em falha
                if (caminho is null && viatura.SemRota)
                    caminho = _malha.MenorCaminho(origem, viatura.Base, passarPelasFalhas: true);
                if (caminho is null)
                {
                    // O temporizador tenta de novo em 1 s
                    viatura.SemRota = true;
                    viatura.Rota = new List<PassoRota>();
                    viatura.ProximaTentativaSeg = _cidade.TempoSeg + 1;
                    return;
                }

                viatura.SemRota = false;
                var rota = new List<PassoRota>();
                if (viatura.IndoPara is not null)
                {
                    var direcao = _malha.DirecaoEntre(viatura.Cruzamento, viatura.IndoPara);
                    if (direcao is not null)
                        rota.Add(new PassoRota { Cruzamento = viatura.IndoPara, Eixo = direcao.Value.Eixo() });
                }
                rota.AddRange(_malha.MontarRota(origem, caminho));
                viatura.Rota = rota;
            }
        }

        public void ChegouNaBase(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                viatura.Rota = new List<PassoRota>();
                viatura.Destino = null;
                viatura.SemRota = false;
                viatura.Cruzamento = viatura.Base;
                viatura.IndoPara = null;

                if (viatura.Papel == PapelViatura.Reserva)
                {
                    viatura.Modo = ModoViatura.Reserva;
                    _log.Registrar(new[] { "RN15" }, $"{viatura.Id} chegou {NomeBase(viatura, "a")} e ficou de reserva.");
                    return;
                }

                VoltarAPatrulha(viatura);
                _log.Registrar(new[] { "RN15" }, $"{viatura.Id} passou {NomeBase(viatura, "por")} e voltou a patrulhar.");
            }
        }

        public void VoltarAPatrulha(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                viatura.Modo = ModoViatura.Patrulha;
                viatura.Ocorrencia = null;
                viatura.Trecho = 0;
                viatura.Destino = null;
                viatura.Rota = new List<PassoRota>();
                viatura.Previsao = new List<string>();
                viatura.SemRota = false;
                viatura.AtendimentoAteSeg = null;
            }
        }

        // "à Base do SAMU", "ao Quartel dos Bombeiros", "pela Base do SAMU", "no Quartel dos Bombeiros"...
        public static string NomeBase(string nome, string preposicao)
        {
            var masculino = nome.StartsWith("Quartel", StringComparison.OrdinalIgnoreCase);
            var forma = preposicao switch
            {
                "a" => masculino ? "ao" : "à",
                "por" => masculino ? "pelo" : "pela",
                "para" => masculino ? "para o" : "para a",
                "em" => masculino ? "no" : "na",
                _ => preposicao
            };
            return $"{forma} {nome}";
        }

        private string NomeBase(Viatura viatura, string preposicao)
        {
            var baseDaViatura = _cidade.Bases.FirstOrDefault(b => b.Cruzamento == viatura.Base);
            var nome = baseDaViatura?.Nome ?? $"base {viatura.Base}";
            return NomeBase(nome, preposicao);
        }

        private void EncerrarNoLocal(Viatura viatura, Ocorrencia oc, string[] tags, string mensagem)
        {
            oc.Status = StatusOcorrencia.Encerrada;
            oc.EncerradaEmSeg = _cidade.TempoSeg;
            _log.Registrar(tags, mensagem);
            TerminarServico(viatura);
        }
    }
}
