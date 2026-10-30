using CorredorVerde.Domain.Enums;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // A ocorrência é montada à mão (ColocarEmEmergencia), para testar só o corredor.
    public class CorredorServiceTests
    {
        [Fact(DisplayName = "RN06 · Ao abrir o corredor, todos os cruzamentos da rota ficam reservados para a viatura")]
        [Trait("Requisito", "RN06")]
        [Trait("Requisito", "UC04")]
        public void IniciarTrecho_DeveReservarTodosOsCruzamentos_NoEixoEmQueAViaturaChega()
        {
            // Arrange: AMB-01 na Base do SAMU (C11) e ocorrência em C15, na mesma rua
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);

            // Act
            var resultado = f.Corredor.IniciarTrecho(ambulancia);

            // Assert: segue reto para o Leste, e cada cruzamento fica reservado no Leste-Oeste (LO)
            Assert.Equal(ResultadoTrecho.CorredorAberto, resultado);
            Assert.Equal("C12 → C13 → C14 → C15", f.RotaComoTexto(ambulancia));
            Assert.Equal("AMB-01 (LO)", f.ReservasComoTexto("C12"));
            Assert.Equal("AMB-01 (LO)", f.ReservasComoTexto("C15"));
            Assert.True(f.LogTem("Corredor verde aberto para AMB-01 (trecho 1, 4 cruzamentos): C12 → C13 → C14 → C15."));
        }

        [Fact(DisplayName = "RNF06 · 2 s depois de abrir o corredor, todos os cruzamentos da rota já estão verdes")]
        [Trait("Requisito", "RN06")]
        [Trait("Requisito", "RNF06")]
        public void IniciarTrecho_DeveDeixarTodoOCorredorVerde_EmAteDoisSegundos()
        {
            // Arrange
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            f.Corredor.IniciarTrecho(ambulancia);

            // Act: 2 s é o tempo do amarelo de quem estava verde para o outro lado
            f.Avancar(2.0);

            // Assert
            Assert.Equal(Luz.Verde, f.LuzDe("C12", Eixo.LO));
            Assert.Equal(Luz.Verde, f.LuzDe("C13", Eixo.LO));
            Assert.Equal(Luz.Verde, f.LuzDe("C14", Eixo.LO));
            Assert.Equal(Luz.Verde, f.LuzDe("C15", Eixo.LO));
        }

        [Fact(DisplayName = "RN06 · Se a viatura já está andando numa rua, a rota começa no cruzamento à frente dela")]
        [Trait("Requisito", "RN06")]
        [Trait("Requisito", "UC04")]
        public void IniciarTrecho_DeveComecarNaRuaAtual_QuandoViaturaEstaAndando()
        {
            // Arrange: a AMB-01 saiu de C11 e está subindo para C6
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);
            ambulancia.IndoPara = "C6";

            // Act
            f.Corredor.IniciarTrecho(ambulancia);

            // Assert: começa em C6, chegando pelo Norte-Sul (NS)
            Assert.Equal("C6 → C7 → C8 → C9 → C10 → C15", f.RotaComoTexto(ambulancia));
            Assert.Equal("AMB-01 (NS)", f.ReservasComoTexto("C6"));
        }

        [Fact(DisplayName = "RN12 · No cruzamento disputado, passa primeiro a ocorrência mais grave (criticidade 1)")]
        [Trait("Requisito", "RN12")]
        [Trait("Requisito", "UC08")]
        public void IniciarTrecho_DevePriorizarMaiorCriticidade_QuandoCorredoresConflitam()
        {
            // Arrange: as duas rotas passam por C13, a ambulância no Leste-Oeste e o bombeiro no Norte-Sul
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 2);
            var bombeiro = f.ColocarEmEmergencia("BOM-01", de: "C18", ate: "C3", criticidade: 1);

            // Act
            f.Corredor.IniciarTrecho(ambulancia);
            f.Corredor.IniciarTrecho(bombeiro);
            f.Avancar(2.0);

            // Assert: o bombeiro (criticidade 1) fica com o verde em C13
            Assert.Equal("AMB-01 (LO), BOM-01 (NS)", f.ReservasComoTexto("C13"));
            Assert.Equal(Luz.Verde, f.LuzDe("C13", Eixo.NS));
            Assert.Equal(Luz.Vermelho, f.LuzDe("C13", Eixo.LO));
            Assert.True(f.LogTem("Conflito em C13: BOM-01 (Norte-Sul) passa antes de AMB-01 (Leste-Oeste), criticidade 1 vence 2."));
        }

        [Fact(DisplayName = "RN12 · Com a mesma criticidade, passa primeiro a ocorrência registrada antes")]
        [Trait("Requisito", "RN12")]
        [Trait("Requisito", "UC08")]
        public void IniciarTrecho_DevePriorizarORegistradoPrimeiro_QuandoCriticidadeEmpata()
        {
            // Arrange: as duas com criticidade 2; a ambulância foi registrada primeiro (OC-001)
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 2);
            var bombeiro = f.ColocarEmEmergencia("BOM-01", de: "C18", ate: "C3", criticidade: 2);

            // Act
            f.Corredor.IniciarTrecho(ambulancia);
            f.Corredor.IniciarTrecho(bombeiro);
            f.Avancar(2.0);

            // Assert: a ambulância fica com o verde, e o conflito vai uma vez só para o log
            Assert.Equal(Luz.Verde, f.LuzDe("C13", Eixo.LO));
            Assert.Equal(Luz.Vermelho, f.LuzDe("C13", Eixo.NS));
            Assert.Equal(1, f.QuantosLogsCom("RN12"));
            Assert.True(f.LogTem("Conflito em C13: AMB-01 (Leste-Oeste) passa antes de BOM-01 (Norte-Sul), mesma criticidade (2); OC-001 foi registrada primeiro."));
        }

        [Fact(DisplayName = "RN13 · Sem caminho possível, a viatura fica aguardando e o sistema avisa uma vez, sem travar")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RNF03")]
        public void IniciarTrecho_DeveMarcarSemRotaEAvisarUmaVez_QuandoNaoHaCaminho()
        {
            // Arrange: os três vizinhos de C15 em falha
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C10", "C14", "C20");
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 1);

            // Act: tenta duas vezes
            var resultado = f.Corredor.IniciarTrecho(ambulancia);
            f.Corredor.IniciarTrecho(ambulancia);

            // Assert: fica sem rota, tenta de novo em 1 s e o aviso aparece uma vez só
            Assert.Equal(ResultadoTrecho.SemCaminho, resultado);
            Assert.True(ambulancia.SemRota);
            Assert.Equal(1.0, ambulancia.ProximaTentativaSeg);
            Assert.True(f.NenhumCruzamentoReservado());
            Assert.Equal(1, f.QuantosLogsCom("RNF03"));
        }

        [Fact(DisplayName = "RN06 · Se a ocorrência é onde a viatura já está, não há corredor para abrir")]
        [Trait("Requisito", "RN06")]
        public void IniciarTrecho_DeveDevolverJaNoDestino_SemReservarNada_QuandoViaturaEstaNoLocal()
        {
            // Arrange: viatura em C11, ocorrência em C11
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C11", criticidade: 1);

            // Act
            var resultado = f.Corredor.IniciarTrecho(ambulancia);

            // Assert
            Assert.Equal(ResultadoTrecho.JaNoDestino, resultado);
            Assert.True(f.NenhumCruzamentoReservado());
        }

        [Fact(DisplayName = "RN08 · Liberar a viatura tira só as reservas dela; as de outra viatura continuam")]
        [Trait("Requisito", "RN08")]
        public void LiberarTodas_DeveTirarSoAsReservasDaViatura_EDevolverOsCruzamentosLiberados()
        {
            // Arrange: dois corredores que se cruzam em C13
            var f = CidadeFactory.Criar();
            var ambulancia = f.ColocarEmEmergencia("AMB-01", de: "C11", ate: "C15", criticidade: 2);
            var bombeiro = f.ColocarEmEmergencia("BOM-01", de: "C18", ate: "C3", criticidade: 1);
            f.Corredor.IniciarTrecho(ambulancia);
            f.Corredor.IniciarTrecho(bombeiro);

            // Act
            var liberados = f.Corredor.LiberarTodas(ambulancia);

            // Assert: saíram os 4 da ambulância; em C13 sobrou só o bombeiro
            Assert.Equal(new[] { "C12", "C13", "C14", "C15" }, liberados);
            Assert.Equal("nenhuma", f.ReservasComoTexto("C12"));
            Assert.Equal("BOM-01 (NS)", f.ReservasComoTexto("C13"));
        }
    }
}
