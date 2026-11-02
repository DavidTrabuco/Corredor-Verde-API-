using CorredorVerde.Application.DTO.Response;
using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Exceptions;
using CorredorVerde.Domain.Interfaces;
using CorredorVerde.Domain.Options;
using Microsoft.Extensions.Options;

namespace CorredorVerde.Application.Service
{
    // Leituras para a maquete (polling), pausa, velocidade e reinício da simulação
    public class SimulacaoService : ISimulacaoService
    {
        private static readonly int[] VelocidadesAceitas = { 1, 2, 4 };

        private readonly Cidade _cidade;
        private readonly ISemaforoService _semaforo;
        private readonly ILogEventosService _log;
        private readonly SimulacaoOptions _opcoes;

        public SimulacaoService(Cidade cidade, ISemaforoService semaforo, ILogEventosService log, IOptions<SimulacaoOptions> opcoes)
        {
            _cidade = cidade;
            _semaforo = semaforo;
            _log = log;
            _opcoes = opcoes.Value;
        }

        public MalhaResponse Malha()
        {
            lock (_cidade.Trava)
            {
                return MalhaResponse.De(_cidade);
            }
        }

        public EstadoResponse Estado()
        {
            lock (_cidade.Trava)
            {
                return new EstadoResponse
                {
                    TempoSeg = Math.Round(_cidade.TempoSeg, 2),
                    Pausado = _cidade.Pausado,
                    Velocidade = _cidade.Velocidade,
                    Semaforos = Semaforos(),
                    Viaturas = Viaturas(),
                    Ocorrencias = Ocorrencias()
                };
            }
        }

        public List<SemaforoResponse> Semaforos()
        {
            lock (_cidade.Trava)
            {
                return _cidade.Cruzamentos.Values
                    .Select(c => SemaforoResponse.De(
                        c,
                        _semaforo.Luz(c.Id, Eixo.NS),
                        _semaforo.Luz(c.Id, Eixo.LO),
                        _semaforo.Vencedor(c)?.ViaturaId))
                    .ToList();
            }
        }

        public List<ViaturaResponse> Viaturas()
        {
            lock (_cidade.Trava)
            {
                return _cidade.Viaturas.Values.Select(v => ViaturaResponse.De(v, _cidade.TempoSeg)).ToList();
            }
        }

        public List<OcorrenciaResponse> Ocorrencias()
        {
            lock (_cidade.Trava)
            {
                return _cidade.Ocorrencias.Select(OcorrenciaResponse.De).ToList();
            }
        }

        // Botões Pausar e Velocidade. O que vier null fica como está.
        public void Controlar(bool? pausado, int? velocidade)
        {
            lock (_cidade.Trava)
            {
                if (velocidade.HasValue && !VelocidadesAceitas.Contains(velocidade.Value))
                    throw new ValidacaoException($"Velocidade {velocidade} inválida: use 1, 2 ou 4.");

                if (pausado.HasValue)
                    _cidade.Pausado = pausado.Value;
                if (velocidade.HasValue)
                    _cidade.Velocidade = velocidade.Value;
            }
        }

        // Volta tudo ao início: semáforos, viaturas nas bases, sem ocorrências e com o log zerado
        public void Reiniciar()
        {
            lock (_cidade.Trava)
            {
                _cidade.Reiniciar(_opcoes);
                _log.Registrar(Array.Empty<string>(), MensagemPronta(), TipoEvento.Info);
            }
        }

        // Ex.: "Simulação pronta: AMB-01 e BOM-01 patrulhando; AMB-02 e BOM-02 de reserva na Base do SAMU (C11) e no Quartel dos Bombeiros (C20)."
        private string MensagemPronta()
        {
            var viaturas = _cidade.Viaturas.Values.ToList();
            var patrulha = viaturas.Where(v => v.Papel == PapelViatura.Patrulha).Select(v => v.Id).ToList();
            var reserva = viaturas.Where(v => v.Papel == PapelViatura.Reserva).ToList();

            var partes = new List<string>();
            if (patrulha.Count > 0)
                partes.Add($"{Juntar(patrulha)} patrulhando");

            if (reserva.Count > 0)
            {
                var locais = reserva
                    .Select(v => v.Base)
                    .Distinct()
                    .Select(cruzamento =>
                    {
                        var nome = _cidade.Bases.FirstOrDefault(b => b.Cruzamento == cruzamento)?.Nome ?? $"base {cruzamento}";
                        return $"{ViaturaService.NomeBase(nome, "em")} ({cruzamento})";
                    })
                    .ToList();
                partes.Add($"{Juntar(reserva.Select(v => v.Id).ToList())} de reserva {Juntar(locais)}");
            }

            return partes.Count == 0
                ? "Simulação pronta."
                : $"Simulação pronta: {string.Join("; ", partes)}.";
        }

        // "A", "A e B", "A, B e C"
        private static string Juntar(List<string> itens)
        {
            if (itens.Count <= 1)
                return string.Join("", itens);
            return string.Join(", ", itens.Take(itens.Count - 1)) + " e " + itens[^1];
        }
    }
}
