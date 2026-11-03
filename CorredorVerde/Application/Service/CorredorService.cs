using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // RN06: abre o corredor verde em todos os cruzamentos do trecho.
    // RN08: tira as reservas da viatura. RN12: avisa no log quando dois corredores disputam um cruzamento.
    public class CorredorService : ICorredorService
    {
        private readonly Cidade _cidade;
        private readonly IMalhaService _malha;
        private readonly ISemaforoService _semaforo;
        private readonly ILogEventosService _log;

        public CorredorService(Cidade cidade, IMalhaService malha, ISemaforoService semaforo, ILogEventosService log)
        {
            _cidade = cidade;
            _malha = malha;
            _semaforo = semaforo;
            _log = log;
        }

        // Calcula o menor caminho até o destino do trecho e reserva cada cruzamento no eixo em que a viatura chega.
        // Não chama o atendimento direto: devolve JaNoDestino e o ViaturaService decide (evita dependência circular).
        public ResultadoTrecho IniciarTrecho(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                var destino = viatura.Destino
                    ?? throw new InvalidOperationException($"{viatura.Id} não tem destino para abrir corredor.");

                // Andando numa rua, o trecho começa no cruzamento para onde ela está indo
                var origem = viatura.IndoPara ?? viatura.Cruzamento;
                var caminho = _malha.MenorCaminho(origem, destino);

                // RN13: sem desvio, a viatura é avisada e aguarda 1 s (bloco abaixo).
                // Se na nova tentativa ainda não houver desvio, ela passa com cautela pelos cruzamentos em falha.
                if (caminho is null && viatura.SemRota)
                {
                    caminho = _malha.MenorCaminho(origem, destino, passarPelasFalhas: true);
                    if (caminho is not null)
                    {
                        var emFalha = caminho.Where(id => id != destino && _cidade.Cruzamentos[id].Semaforo.EmFalha);
                        _log.Registrar(new[] { "RN13", "UC04" },
                            $"Sem desvio de {origem} até {destino}: {viatura.Id} passa com cautela por {string.Join(", ", emFalha)} (semáforo em falha).",
                            TipoEvento.Alerta);
                    }
                }

                if (caminho is null)
                {
                    viatura.SemRota = true;
                    viatura.Rota = new List<PassoRota>();
                    viatura.ProximaTentativaSeg = _cidade.TempoSeg + 1;

                    // Avisa uma vez só por viatura, destino e trecho; o temporizador tenta de novo a cada 1 s
                    var chave = $"semrota:{viatura.Id}:{destino}:{viatura.Trecho}";
                    if (_cidade.JaLogado.Add(chave))
                    {
                        _log.Registrar(new[] { "RN13", "RNF03", "UC04" },
                            $"Sem caminho de {origem} até {destino}: semáforos em falha bloqueiam a passagem. {viatura.Id} foi avisada e aguarda.",
                            TipoEvento.Alerta);
                    }
                    return ResultadoTrecho.SemCaminho;
                }

                viatura.SemRota = false;
                var rota = new List<PassoRota>();

                // A rua em que ela já está entra como primeiro passo da rota
                if (viatura.IndoPara is not null)
                {
                    var direcao = _malha.DirecaoEntre(viatura.Cruzamento, viatura.IndoPara)!.Value;
                    rota.Add(new PassoRota { Cruzamento = viatura.IndoPara, Eixo = direcao.Eixo() });
                }
                rota.AddRange(_malha.MontarRota(origem, caminho));
                viatura.Rota = rota;

                if (rota.Count == 0)
                    return ResultadoTrecho.JaNoDestino;

                var ocorrencia = viatura.Ocorrencia
                    ?? throw new InvalidOperationException($"{viatura.Id} não tem ocorrência para abrir corredor.");

                foreach (var passo in rota)
                {
                    _cidade.Cruzamentos[passo.Cruzamento].Reservas.Add(new ReservaCorredor
                    {
                        ViaturaId = viatura.Id,
                        OcorrenciaId = ocorrencia.Id,
                        Eixo = passo.Eixo,
                        Criticidade = ocorrencia.Criticidade,
                        Ordem = ocorrencia.Ordem
                    });
                }

                _log.Contar("RN05");
                var plural = rota.Count > 1 ? "s" : "";
                _log.Registrar(new[] { "RN06", "UC04" },
                    $"Corredor verde aberto para {viatura.Id} (trecho {viatura.Trecho}, {rota.Count} cruzamento{plural}): {string.Join(" → ", rota.Select(r => r.Cruzamento))}.");

                // Só depois de reservar tudo: confere conflitos e já começa a puxar o verde (amarelo de 2 s antes)
                foreach (var passo in rota)
                {
                    var cruzamento = _cidade.Cruzamentos[passo.Cruzamento];
                    VerificarConflito(cruzamento);
                    _semaforo.Atualizar(cruzamento);
                }

                return ResultadoTrecho.CorredorAberto;
            }
        }

        // RN12: com dois corredores em eixos diferentes, registra quem passa primeiro e o motivo
        public void VerificarConflito(Cruzamento cruzamento)
        {
            lock (_cidade.Trava)
            {
                if (cruzamento.Reservas.Count < 2)
                    return;

                var vencedor = _semaforo.Vencedor(cruzamento)!;
                foreach (var reserva in cruzamento.Reservas)
                {
                    // Mesmo eixo não é conflito: as duas passam no mesmo verde
                    if (reserva.Eixo == vencedor.Eixo)
                        continue;

                    var chave = $"conflito:{cruzamento.Id}:{vencedor.OcorrenciaId}:{reserva.OcorrenciaId}";
                    if (!_cidade.JaLogado.Add(chave))
                        continue;

                    var motivo = vencedor.Criticidade < reserva.Criticidade
                        ? $"criticidade {vencedor.Criticidade} vence {reserva.Criticidade}"
                        : $"mesma criticidade ({vencedor.Criticidade}); {vencedor.OcorrenciaId} foi registrada primeiro";

                    _log.Registrar(new[] { "RN12", "UC08" },
                        $"Conflito em {cruzamento.Id}: {vencedor.ViaturaId} ({SemaforoService.NomeEixo(vencedor.Eixo)}) passa antes de {reserva.ViaturaId} ({SemaforoService.NomeEixo(reserva.Eixo)}), {motivo}.",
                        TipoEvento.Alerta);
                }
            }
        }

        // RN08: tira todas as reservas da viatura e devolve os cruzamentos que voltaram ao ciclo normal
        public List<string> LiberarTodas(Viatura viatura)
        {
            lock (_cidade.Trava)
            {
                var liberados = new List<string>();
                foreach (var cruzamento in _cidade.Cruzamentos.Values)
                {
                    if (_semaforo.Liberar(cruzamento, viatura.Id))
                        liberados.Add(cruzamento.Id);
                }
                return liberados;
            }
        }
    }
}
