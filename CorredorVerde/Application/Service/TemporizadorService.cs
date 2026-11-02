using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Domain.Interfaces;

namespace CorredorVerde.Application.Service
{
    // UC02: a cada passo do relógio, todos os semáforos se atualizam.
    // UC05: encerra os atendimentos que acabaram e tenta de novo as rotas que estavam sem caminho.
    public class TemporizadorService : ITemporizadorService
    {
        // Passo máximo do relógio: com 4× de velocidade, 0,4 s viram 4 passos de 0,1 s (o amarelo continua com 2 s certinhos)
        private const double PassoSeg = 0.1;
        private const double Epsilon = 1e-9;

        private readonly Cidade _cidade;
        private readonly ISemaforoService _semaforo;
        private readonly IViaturaService _viatura;

        public TemporizadorService(Cidade cidade, ISemaforoService semaforo, IViaturaService viatura)
        {
            _cidade = cidade;
            _semaforo = semaforo;
            _viatura = viatura;
        }

        public void Avancar(double segundos)
        {
            lock (_cidade.Trava)
            {
                var resto = segundos;
                while (resto > Epsilon)
                {
                    var passo = Math.Min(PassoSeg, resto);
                    Passo(passo);
                    resto -= passo;
                }
            }
        }

        private void Passo(double segundos)
        {
            _cidade.TempoSeg += segundos;
            var agora = _cidade.TempoSeg;

            foreach (var cruzamento in _cidade.Cruzamentos.Values)
                _semaforo.Atualizar(cruzamento);

            foreach (var v in _cidade.Viaturas.Values.ToList())
            {
                if (v.Modo == ModoViatura.Atendimento && v.AtendimentoAteSeg.HasValue && agora >= v.AtendimentoAteSeg.Value - Epsilon)
                    _viatura.FimDoAtendimento(v);
                else if (v.Modo == ModoViatura.Emergencia && v.SemRota && agora >= v.ProximaTentativaSeg)
                    _viatura.IniciarTrecho(v);
                else if (v.Modo == ModoViatura.Retorno && v.SemRota && agora >= v.ProximaTentativaSeg)
                    _viatura.IniciarRetorno(v);
            }
        }
    }
}
