using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // UC03: registrar emergência. RN04: a ambulância tem dois trechos (local e hospital mais próximo).
    // RN15: vai a viatura livre do tipo pedido mais perto do local; em empate, a da patrulha.
    public class OcorrenciaService : IOcorrenciaService
    {
        // Distância de quem não tem caminho até o local (fica por último na escolha)
        private const int SemCaminho = int.MaxValue;

        private readonly Cidade _cidade;
        private readonly IMalhaService _malha;
        private readonly IViaturaService _viatura;
        private readonly ILogEventosService _log;

        public OcorrenciaService(Cidade cidade, IMalhaService malha, IViaturaService viatura, ILogEventosService log)
        {
            _cidade = cidade;
            _malha = malha;
            _viatura = viatura;
            _log = log;
        }

        public Ocorrencia Registrar(string? tipo, string? cruzamento, int? criticidade)
        {
            lock (_cidade.Trava)
            {
                // RNF04: valida tudo antes de mexer na cidade
                var tipoViatura = LerTipo(tipo);
                var local = _malha.ObterCruzamento(cruzamento ?? string.Empty).Id;
                if (criticidade is null || criticidade < 1 || criticidade > 3)
                    throw new ValidacaoException($"Criticidade {criticidade} inválida: use 1, 2 ou 3.");
                var crit = criticidade.Value;

                var candidatas = OrdenarCandidatas(tipoViatura, local);
                if (candidatas.Count == 0)
                    throw new ConflitoException(MensagemSemViaturaLivre(tipoViatura));

                // RN15: a primeira da lista é a escolhida; as outras aparecem no log para explicar a escolha
                var escolhida = candidatas[0];
                var v = escolhida.Viatura;
                var outras = string.Join(", ", candidatas.Skip(1).Select(c => $"{c.Viatura.Id} a {Quarteiroes(c.Distancia)}"));
                var origemDaViatura = v.Modo == ModoViatura.Reserva ? ", sai da reserva"
                    : v.Modo == ModoViatura.Retorno ? ", estava voltando à base"
                    : "";
                _log.Registrar(new[] { "RN15", "UC03" },
                    $"{v.Id} é a viatura livre mais perto de {local} ({Quarteiroes(escolhida.Distancia)}{(outras.Length > 0 ? "; " + outras : "")}){origemDaViatura}.");

                // Quem voltava para a base larga a rota de volta
                if (v.Modo == ModoViatura.Retorno)
                    v.Rota = new List<PassoRota>();
                v.SemRota = false;

                _cidade.SeqOcorrencia++;
                var oc = new Ocorrencia
                {
                    Id = "OC-" + _cidade.SeqOcorrencia.ToString("D3"),
                    Cruzamento = local,
                    Criticidade = crit,
                    Tipo = tipoViatura,
                    ViaturaId = v.Id,
                    Ordem = _cidade.SeqOcorrencia,
                    Status = StatusOcorrencia.ACaminho,
                    RegistradaEmSeg = _cidade.TempoSeg
                };
                _cidade.Ocorrencias.Add(oc);

                v.Modo = ModoViatura.Emergencia;
                v.Ocorrencia = oc;
                v.Trecho = 1;
                v.Destino = local;

                if (v.Tipo == TipoViatura.Ambulancia)
                {
                    // RN04: já calcula o hospital para mostrar o trecho 2 previsto (linha tracejada na maquete)
                    oc.Hospital = _malha.HospitalMaisProximo(local);
                    var trecho2 = oc.Hospital is null ? null
                        : _malha.MenorCaminho(local, oc.Hospital.Cruzamento)
                          ?? _malha.MenorCaminho(local, oc.Hospital.Cruzamento, passarPelasFalhas: true);
                    v.Previsao = new List<string>();
                    if (trecho2 is not null)
                    {
                        v.Previsao.Add(local);
                        v.Previsao.AddRange(trecho2);
                    }

                    _log.Registrar(new[] { "UC03", "RN04" },
                        $"{oc.Id} registrada em {local} (criticidade {crit}) para {v.Id}. Trechos: {v.IndoPara ?? v.Cruzamento} → {local} → {oc.Hospital?.Nome ?? "sem hospital alcançável"}.");
                }
                else
                {
                    v.Previsao = new List<string>();
                    _log.Registrar(new[] { "UC03", "RN11" },
                        $"{oc.Id} registrada em {local} (criticidade {crit}) para {v.Id}. Bombeiros vão só até o local.");
                }

                // RN06: abre o corredor do trecho 1
                _viatura.IniciarTrecho(v);
                return oc;
            }
        }

        // RN15: livres = patrulhando, de reserva ou voltando para a base.
        // Ordem: menos quarteirões; em empate, a da patrulha; depois o ID.
        private List<Candidata> OrdenarCandidatas(TipoViatura tipo, string destino)
        {
            var candidatas = new List<Candidata>();
            foreach (var v in _cidade.Viaturas.Values)
            {
                if (v.Tipo != tipo)
                    continue;
                if (v.Modo != ModoViatura.Patrulha && v.Modo != ModoViatura.Reserva && v.Modo != ModoViatura.Retorno)
                    continue;

                // Andando numa rua, conta a partir do cruzamento à frente, mais o quarteirão que falta
                var origem = v.IndoPara ?? v.Cruzamento;
                var caminho = _malha.MenorCaminho(origem, destino);
                var distancia = caminho is null ? SemCaminho : caminho.Count + (v.IndoPara is null ? 0 : 1);
                candidatas.Add(new Candidata(v, distancia));
            }

            return candidatas
                .OrderBy(c => c.Distancia)
                .ThenBy(c => c.Viatura.Papel == PapelViatura.Patrulha ? 0 : 1)
                .ThenBy(c => c.Viatura.Id, StringComparer.Ordinal)
                .ToList();
        }

        private static TipoViatura LerTipo(string? tipo)
        {
            foreach (var nome in Enum.GetNames<TipoViatura>())
            {
                if (string.Equals(nome, tipo, StringComparison.OrdinalIgnoreCase))
                    return Enum.Parse<TipoViatura>(nome);
            }
            throw new ValidacaoException($"Tipo de viatura {tipo} inválido: use Ambulancia ou Bombeiros.");
        }

        private string MensagemSemViaturaLivre(TipoViatura tipo)
        {
            var nomes = _cidade.Viaturas.Values.Where(v => v.Tipo == tipo).Select(v => v.Id).ToList();
            if (nomes.Count == 0)
                return $"Não há viatura do tipo {tipo} na configuração.";
            if (nomes.Count == 1)
                return $"{nomes[0]} está ocupada. Espere ela terminar.";
            return $"{string.Join(" e ", nomes)} estão ocupadas. Espere uma delas terminar.";
        }

        private static string Quarteiroes(int n) =>
            n == SemCaminho ? "sem caminho" : $"{n} {(n == 1 ? "quarteirão" : "quarteirões")}";

        private record Candidata(Viatura Viatura, int Distancia);
    }
}
