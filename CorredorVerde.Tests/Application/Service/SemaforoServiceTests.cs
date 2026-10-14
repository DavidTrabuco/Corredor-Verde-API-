using CorredorVerde.Domain.Entidades;
using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    public class SemaforoServiceTests
    {
        private const double Passo = 0.1;

        private static ReservaCorredor Reserva(string viatura, Eixo eixo, int criticidade, int ordem) => new()
        {
            ViaturaId = viatura,
            OcorrenciaId = $"OC-{ordem:000}",
            Eixo = eixo,
            Criticidade = criticidade,
            Ordem = ordem
        };

        [Fact]
        [Trait("Requisito", "RN01")]
        [Trait("Requisito", "RNF01")]
        public void Avancar_NuncaDeixaDuasDirecoesVerdes_DuranteUmaHoraDeSimulacao()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            // Dois corredores em direções diferentes no mesmo cruzamento, para exercitar a troca forçada
            var c7 = f.Cidade.Cruzamentos["C7"];
            c7.Reservas.Add(Reserva("AMB-01", Eixo.NS, 2, 1));
            c7.Reservas.Add(Reserva("BOM-01", Eixo.LO, 1, 2));
            var verdesDuplos = 0;

            // Act
            for (var i = 0; i < 36000; i++)
            {
                f.Avancar(Passo);
                foreach (var c in f.Cidade.Cruzamentos.Values)
                {
                    if (f.Semaforo.Luz(c.Id, Eixo.NS) == Luz.Verde && f.Semaforo.Luz(c.Id, Eixo.LO) == Luz.Verde)
                        verdesDuplos++;
                }
            }

            // Assert
            Assert.Equal(0, verdesDuplos);
        }

        [Fact]
        [Trait("Requisito", "RN02")]
        public void Avancar_PassaPorAmareloDeDoisSegundos_AntesDoVermelho()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var anterior = new Dictionary<string, Luz>();
            var inicioAmarelo = new Dictionary<string, double>();
            var semAmarelo = 0;
            var amareloCurto = 0;

            // Act
            for (var i = 0; i < 36000; i++)
            {
                f.Avancar(Passo);
                foreach (var c in f.Cidade.Cruzamentos.Values)
                {
                    foreach (var eixo in new[] { Eixo.NS, Eixo.LO })
                    {
                        var chave = c.Id + eixo;
                        var luz = f.Semaforo.Luz(c.Id, eixo);
                        anterior.TryGetValue(chave, out var antes);
                        var tinhaAnterior = anterior.ContainsKey(chave);

                        if (luz == Luz.Amarelo && (!tinhaAnterior || antes != Luz.Amarelo))
                            inicioAmarelo[chave] = f.Cidade.TempoSeg;
                        if (tinhaAnterior && antes == Luz.Verde && luz == Luz.Vermelho)
                            semAmarelo++;
                        if (tinhaAnterior && antes == Luz.Amarelo && luz == Luz.Vermelho
                            && f.Cidade.TempoSeg - inicioAmarelo[chave] < f.Cidade.Config.AmareloSeg - 1e-6)
                            amareloCurto++;

                        anterior[chave] = luz;
                    }
                }
            }

            // Assert
            Assert.Equal(0, semAmarelo);
            Assert.Equal(0, amareloCurto);
        }

        [Fact]
        [Trait("Requisito", "RN03")]
        [Trait("Requisito", "UC02")]
        public void Avancar_AlternaOitoSegundosDeVerdeEDoisDeAmarelo_QuandoNaoHaCorredor()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var s = f.Cidade.Cruzamentos["C1"].Semaforo;
            s.EixoVerde = Eixo.NS;
            s.EmAmarelo = false;
            s.DesdeSeg = 0;
            var trocas = new List<(string Fase, double Tempo)>();
            var anterior = "NS:Verde";

            // Act
            for (var i = 0; i < 300; i++)
            {
                f.Avancar(Passo);
                var atual = $"{s.EixoVerde}:{(s.EmAmarelo ? "Amarelo" : "Verde")}";
                if (atual != anterior)
                {
                    trocas.Add((atual, f.Cidade.TempoSeg));
                    anterior = atual;
                }
            }

            // Assert: NS verde até 8 s, amarelo até 10 s, então LO verde até 18 s
            var esperado = new[] { ("NS:Amarelo", 8.0), ("LO:Verde", 10.0), ("LO:Amarelo", 18.0) };
            Assert.True(trocas.Count >= 3, "O ciclo não trocou de fase.");
            for (var i = 0; i < esperado.Length; i++)
            {
                Assert.Equal(esperado[i].Item1, trocas[i].Fase);
                Assert.InRange(trocas[i].Tempo, esperado[i].Item2 - 0.11, esperado[i].Item2 + 0.11);
            }
        }

        [Fact]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DeveNegar_QuandoVermelhoOuAmarelo()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var s = f.Cidade.Cruzamentos["C7"].Semaforo;
            s.EixoVerde = Eixo.NS;
            s.EmAmarelo = false;

            // Act + Assert: verde libera, vermelho segura
            Assert.True(f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01").Pode);
            Assert.False(f.Semaforo.PodeAtravessar("C7", Eixo.LO, "AMB-01").Pode);

            // Act + Assert: amarelo segura
            s.EmAmarelo = true;
            Assert.False(f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01").Pode);
        }

        [Fact]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DeveLogarAEspera_SomenteQuandoAViaturaEstaEmEmergencia()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Cidade.Cruzamentos["C7"].Semaforo.EixoVerde = Eixo.NS;

            // Act: em patrulha só conta
            f.Semaforo.PodeAtravessar("C7", Eixo.LO, "AMB-01");
            var logsEmPatrulha = f.Cidade.Log.Count;

            // Act: em emergência vai para o log, uma vez só por cruzamento
            f.Cidade.Viaturas["AMB-01"].Modo = ModoViatura.Emergencia;
            f.Semaforo.PodeAtravessar("C7", Eixo.LO, "AMB-01");
            f.Semaforo.PodeAtravessar("C7", Eixo.LO, "AMB-01");

            // Assert
            Assert.Equal(0, logsEmPatrulha);
            Assert.Single(f.Cidade.Log);
            Assert.Contains("AMB-01 parou na faixa de C7: vermelho para Leste-Oeste", f.Cidade.Log[0].Mensagem);
        }

        [Fact]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN07")]
        public void PodeAtravessar_DevePermitirComCautela_QuandoSemaforoEstaEmFalha()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            f.Cidade.Cruzamentos["C7"].Semaforo.EmFalha = true;

            // Act
            var resultado = f.Semaforo.PodeAtravessar("C7", Eixo.NS, "AMB-01");

            // Assert
            Assert.True(resultado.Pode);
            Assert.True(resultado.Cautela);
            Assert.Equal(Luz.Piscante, resultado.Luz);
        }

        [Fact]
        [Trait("Requisito", "RNF12")]
        public void Avancar_DeveUsarOsTemposDoAppsettings_QuandoConfiguracaoMuda()
        {
            // Arrange
            var f = CidadeFactory.Criar(o =>
            {
                o.VerdeSeg = 5;
                o.AmareloSeg = 1;
            });
            var s = f.Cidade.Cruzamentos["C1"].Semaforo;
            s.EixoVerde = Eixo.NS;
            s.EmAmarelo = false;
            s.DesdeSeg = 0;

            // Act + Assert: com verdeSeg = 5, o amarelo começa em 5 s
            f.Avancar(5.0);
            Assert.True(s.EmAmarelo);

            // Act + Assert: com amareloSeg = 1, a troca ocorre 1 s depois
            f.Avancar(1.0);
            Assert.Equal(Eixo.LO, s.EixoVerde);
            Assert.False(s.EmAmarelo);
        }

        [Fact]
        [Trait("Requisito", "RN12")]
        public void Vencedor_DevePriorizarMaiorCriticidade_EDepoisOMaisAntigo()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var c = f.Cidade.Cruzamentos["C13"];

            // Act + Assert: sem reserva, ninguém vence
            Assert.Null(f.Semaforo.Vencedor(c));

            // Act + Assert: criticidade 1 vence a 2, mesmo registrada depois
            c.Reservas.Add(Reserva("AMB-01", Eixo.LO, 2, 1));
            c.Reservas.Add(Reserva("BOM-01", Eixo.NS, 1, 2));
            Assert.Equal("BOM-01", f.Semaforo.Vencedor(c)!.ViaturaId);

            // Act + Assert: em empate, a registrada primeiro
            c.Reservas.Clear();
            c.Reservas.Add(Reserva("BOM-01", Eixo.NS, 2, 2));
            c.Reservas.Add(Reserva("AMB-01", Eixo.LO, 2, 1));
            Assert.Equal("AMB-01", f.Semaforo.Vencedor(c)!.ViaturaId);
        }

        [Fact]
        [Trait("Requisito", "RN06")]
        public void Atualizar_DeveAbrirOVerdeParaODirecaoDoCorredor_ESegurarAte_Liberar()
        {
            // Arrange: C7 começa com verde para Norte-Sul
            var f = CidadeFactory.Criar();
            var c7 = f.Cidade.Cruzamentos["C7"];
            c7.Reservas.Add(Reserva("AMB-01", Eixo.LO, 1, 1));
            f.Semaforo.Atualizar(c7);

            // Act: depois do amarelo de 2 s, o verde é do Leste-Oeste
            f.Avancar(2.0);
            Assert.Equal(Luz.Verde, f.Semaforo.Luz("C7", Eixo.LO));
            Assert.Equal(Luz.Vermelho, f.Semaforo.Luz("C7", Eixo.NS));

            // Assert: o verde fica parado enquanto a reserva existir (muito além dos 8 s normais)
            f.Avancar(30.0);
            Assert.Equal(Luz.Verde, f.Semaforo.Luz("C7", Eixo.LO));
        }

        [Fact]
        [Trait("Requisito", "RN08")]
        public void Liberar_DeveTirarAReserva_EVoltarAoCicloNormal()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var c7 = f.Cidade.Cruzamentos["C7"];
            c7.Reservas.Add(Reserva("AMB-01", Eixo.LO, 1, 1));
            f.Semaforo.Atualizar(c7);
            f.Avancar(2.0);

            // Act
            var liberou = f.Semaforo.Liberar(c7, "AMB-01");
            var liberouDeNovo = f.Semaforo.Liberar(c7, "AMB-01");
            f.Avancar(f.Cidade.Config.VerdeSeg + f.Cidade.Config.AmareloSeg + 0.2);

            // Assert: sem reserva, as fases voltam a alternar
            Assert.True(liberou);
            Assert.False(liberouDeNovo);
            Assert.Empty(c7.Reservas);
            Assert.Equal(Eixo.NS, c7.Semaforo.EixoVerde);
        }
    }
}
