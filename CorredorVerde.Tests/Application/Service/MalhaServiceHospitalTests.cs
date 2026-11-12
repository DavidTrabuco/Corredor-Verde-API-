using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.Application.Service
{
    // Escolha do hospital mais próximo quando há semáforos em falha (RN04 e RN13).
    public class MalhaServiceHospitalTests
    {
        [Fact(DisplayName = "RN04 · O hospital escolhido é o mais perto do local")]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveEscolherOMaisPerto_QuandoNaoHaFalhas()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act
            var hospital = f.Malha.HospitalMaisProximo("C15");

            // Assert: de C15 o Hospital Sul (C18) está a 3 quarteirões e o Norte (C3), a 4 (conferido na própria malha)
            Assert.Equal(3, f.QuarteiroesEntre("C15", "C18"));
            Assert.Equal(4, f.QuarteiroesEntre("C15", "C3"));
            Assert.NotNull(hospital);
            Assert.Equal("Hospital Sul", hospital.Nome);
        }

        [Fact(DisplayName = "RN13 · Se as falhas fecham todos os acessos, o hospital é escolhido passando pelas falhas")]
        [Trait("Requisito", "RN13")]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveEscolherPassandoPelasFalhas_QuandoNaoHaCaminhoLivre()
        {
            // Arrange: os acessos dos dois hospitais (C13, C17, C19 e C2, C4, C8) estão em falha
            var f = CidadeFactory.Criar();
            f.ColocarEmFalha("C13", "C17", "C19", "C2", "C4", "C8");

            // Act
            var hospital = f.Malha.HospitalMaisProximo("C15");

            // Assert: ainda escolhe o mais perto (Hospital Sul), mesmo tendo de passar com cautela
            Assert.NotNull(hospital);
            Assert.Equal("Hospital Sul", hospital.Nome);
        }

        [Fact(DisplayName = "RN04 · Sem nenhum hospital configurado, não há hospital para escolher")]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveDevolverVazio_QuandoNaoHaHospitais()
        {
            // Arrange
            var f = CidadeFactory.Criar(opcoes => opcoes.Hospitais.Clear());

            // Act
            var hospital = f.Malha.HospitalMaisProximo("C15");

            // Assert
            Assert.Null(hospital);
        }

        [Fact(DisplayName = "RN04 · Perto do Hospital Norte, o escolhido é o Norte")]
        [Trait("Requisito", "RN04")]
        public void HospitalMaisProximo_DeveEscolherONorte_QuandoOLocalEstaPertoDele()
        {
            // Arrange
            var f = CidadeFactory.Criar();

            // Act: C8 fica a 1 quarteirão do Hospital Norte (C3) e a 2 do Sul (C18)
            var hospital = f.Malha.HospitalMaisProximo("C8");

            // Assert
            Assert.NotNull(hospital);
            Assert.Equal("Hospital Norte", hospital.Nome);
        }
    }
}
