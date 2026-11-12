using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CorredorVerde.Tests.TestSupport;

namespace CorredorVerde.Tests.API
{
    // O contrato é o que a maquete (React) espera receber: os NOMES dos campos e o FORMATO dos valores.
    // Aqui o JSON é lido "cru", sem as classes da API. Se alguém renomear um campo (colunas → cols) ou trocar
    // um enum de texto ("Verde") por número (0), o teste falha, e a maquete não quebra sem ninguém perceber.
    public class ContratoJsonApiTests
    {
        [Fact(DisplayName = "UC01 · Contrato de GET /malha: nomes dos campos e tipos dos valores")]
        [Trait("Requisito", "UC01")]
        public async Task Malha_DeveManterOContratoDoJson_QuandoAMaquetePede()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/malha");
            var json = await LerJson(respostaHttp);

            // Assert: campos da raiz
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            ConferirNumero(json, "colunas", 5);
            ConferirNumero(json, "linhas", 4);

            // Assert: cada cruzamento tem id, coluna e linha
            var primeiroCruzamento = Campo(json, "cruzamentos")[0];
            ConferirTexto(primeiroCruzamento, "id", "C1");
            ConferirNumero(primeiroCruzamento, "coluna", 0);
            ConferirNumero(primeiroCruzamento, "linha", 0);

            // Assert: hospital e base, com o tipo da viatura como texto
            var primeiroHospital = Campo(json, "hospitais")[0];
            ConferirTexto(primeiroHospital, "id", "H1");
            ConferirTexto(primeiroHospital, "nome", "Hospital Norte");
            ConferirTexto(primeiroHospital, "cruzamento", "C3");
            var primeiraBase = Campo(json, "bases")[0];
            ConferirTexto(primeiraBase, "nome", "Base do SAMU");
            ConferirTexto(primeiraBase, "cruzamento", "C11");
            ConferirTexto(primeiraBase, "tipo", "Ambulancia");
        }

        [Fact(DisplayName = "UC01 · Contrato de GET /estado: semáforos, viaturas e ocorrências, com enums em texto")]
        [Trait("Requisito", "UC01")]
        [Trait("Requisito", "RNF08")]
        public async Task Estado_DeveManterOContratoDoJson_QuandoAMaquetePergunta()
        {
            // Arrange: uma emergência registrada, para a lista de ocorrências não ficar vazia
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();
            await ApiFactory.RegistrarEmergencia(clienteHttp, "Ambulancia", "C15", 1);

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/estado");
            var json = await LerJson(respostaHttp);

            // Assert: campos da raiz
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            ConferirTipo(json, "tempoSeg", JsonValueKind.Number);
            ConferirTipo(json, "pausado", JsonValueKind.False);
            ConferirNumero(json, "velocidade", 1);

            // Assert: semáforo de C1 em 0 s: luzes como texto, falha, reservas e vencedor
            var semaforoDeC1 = Campo(json, "semaforos")[0];
            ConferirTexto(semaforoDeC1, "cruzamento", "C1");
            ConferirTexto(semaforoDeC1, "ns", "Verde");
            ConferirTexto(semaforoDeC1, "lo", "Vermelho");
            ConferirTipo(semaforoDeC1, "falha", JsonValueKind.False);
            ConferirTipo(semaforoDeC1, "reservas", JsonValueKind.Array);
            ConferirTipo(semaforoDeC1, "vencedor", JsonValueKind.Null);

            // Assert: a AMB-01 em emergência, com todos os campos que a maquete lê
            var ambulancia = Campo(json, "viaturas")[0];
            ConferirTexto(ambulancia, "id", "AMB-01");
            ConferirTexto(ambulancia, "tipo", "Ambulancia");
            ConferirTexto(ambulancia, "papel", "Patrulha");
            ConferirTexto(ambulancia, "base", "C11");
            ConferirTexto(ambulancia, "modo", "Emergencia");
            ConferirTexto(ambulancia, "cruzamento", "C11");
            ConferirTipo(ambulancia, "indoPara", JsonValueKind.Null);
            ConferirNumero(ambulancia, "trecho", 1);
            ConferirTexto(ambulancia, "destino", "C15");
            ConferirTipo(ambulancia, "semRota", JsonValueKind.False);
            ConferirTipo(ambulancia, "rota", JsonValueKind.Array);
            ConferirTipo(ambulancia, "previsao", JsonValueKind.Array);
            ConferirTexto(ambulancia, "ocorrenciaId", "OC-001");
            ConferirNumero(ambulancia, "atendimentoRestanteSeg", 0);
            ConferirTexto(ambulancia, "hospital", "Hospital Sul");

            // Assert: cada passo da rota tem o cruzamento e o eixo como texto
            var primeiroPasso = Campo(ambulancia, "rota")[0];
            ConferirTexto(primeiroPasso, "cruzamento", "C12");
            ConferirTexto(primeiroPasso, "eixo", "LO");

            // Assert: a ocorrência, com o status como texto
            var ocorrencia = Campo(json, "ocorrencias")[0];
            ConferirTexto(ocorrencia, "id", "OC-001");
            ConferirTexto(ocorrencia, "cruzamento", "C15");
            ConferirNumero(ocorrencia, "criticidade", 1);
            ConferirTexto(ocorrencia, "tipo", "Ambulancia");
            ConferirTexto(ocorrencia, "viaturaId", "AMB-01");
            ConferirTexto(ocorrencia, "status", "ACaminho");
            ConferirTipo(ocorrencia, "registradaEmSeg", JsonValueKind.Number);
            ConferirTipo(ocorrencia, "encerradaEmSeg", JsonValueKind.Null);
            ConferirTexto(ocorrencia, "hospital", "Hospital Sul");
        }

        [Fact(DisplayName = "RN07 · Contrato de GET /pode-atravessar: pode, cautela e a luz em texto")]
        [Trait("Requisito", "RN07")]
        public async Task PodeAtravessar_DeveManterOContratoDoJson_QuandoAMaquetePergunta()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/cruzamentos/C1/pode-atravessar?eixo=NS");
            var json = await LerJson(respostaHttp);

            // Assert
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            ConferirTipo(json, "pode", JsonValueKind.True);
            ConferirTipo(json, "cautela", JsonValueKind.False);
            ConferirTexto(json, "luz", "Verde");
        }

        [Fact(DisplayName = "UC03 · Contrato de POST /ocorrencias: a ocorrência criada sai com os mesmos campos do estado")]
        [Trait("Requisito", "UC03")]
        public async Task Registrar_DeveManterOContratoDoJson_QuandoCriaUmaOcorrencia()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Bombeiros", cruzamento = "C15", criticidade = 2 });
            var json = await LerJson(respostaHttp);

            // Assert: bombeiros não têm hospital, e o campo vem como null (não some do JSON)
            Assert.Equal(HttpStatusCode.Created, respostaHttp.StatusCode);
            ConferirTexto(json, "id", "OC-001");
            ConferirTexto(json, "cruzamento", "C15");
            ConferirNumero(json, "criticidade", 2);
            ConferirTexto(json, "tipo", "Bombeiros");
            ConferirTexto(json, "viaturaId", "BOM-01");
            ConferirTexto(json, "status", "ACaminho");
            ConferirTipo(json, "registradaEmSeg", JsonValueKind.Number);
            ConferirTipo(json, "encerradaEmSeg", JsonValueKind.Null);
            ConferirTipo(json, "hospital", JsonValueKind.Null);
        }

        [Fact(DisplayName = "RN14 · Contrato de GET /log: seq, tempo, tags, mensagem e o tipo em texto")]
        [Trait("Requisito", "RN14")]
        public async Task Log_DeveManterOContratoDoJson_QuandoAMaquetePede()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.GetAsync("/api/v1/log");
            var json = await LerJson(respostaHttp);

            // Assert: o primeiro evento é o "Simulação pronta"
            Assert.Equal(HttpStatusCode.OK, respostaHttp.StatusCode);
            var primeiroEvento = json[0];
            ConferirTipo(primeiroEvento, "seq", JsonValueKind.Number);
            ConferirTipo(primeiroEvento, "tempoSeg", JsonValueKind.Number);
            ConferirTipo(primeiroEvento, "tags", JsonValueKind.Array);
            ConferirTipo(primeiroEvento, "mensagem", JsonValueKind.String);
            ConferirTexto(primeiroEvento, "tipo", "Info");
        }

        [Fact(DisplayName = "RNF04 · Contrato dos erros: ProblemDetails com status, título, detalhe e instância")]
        [Trait("Requisito", "RNF04")]
        public async Task Erro_DeveManterOContratoDoProblemDetails_QuandoARequisicaoEInvalida()
        {
            // Arrange
            using var api = new ApiFactory();
            var clienteHttp = api.CreateClient();

            // Act
            var respostaHttp = await clienteHttp.PostAsJsonAsync("/api/v1/ocorrencias",
                new { tipo = "Ambulancia", cruzamento = "C15", criticidade = 4 });
            var json = await LerJson(respostaHttp);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, respostaHttp.StatusCode);
            Assert.Equal("application/problem+json", respostaHttp.Content.Headers.ContentType?.MediaType);
            ConferirNumero(json, "status", 400);
            ConferirTexto(json, "title", "Requisição inválida");
            ConferirTexto(json, "detail", "Criticidade 4 inválida: use 1, 2 ou 3.");
            ConferirTexto(json, "instance", "/api/v1/ocorrencias");
        }

        // ---------- ajudantes: leitura do JSON cru ----------

        private static async Task<JsonElement> LerJson(HttpResponseMessage respostaHttp)
        {
            var texto = await respostaHttp.Content.ReadAsStringAsync();
            using var documento = JsonDocument.Parse(texto);
            return documento.RootElement.Clone();
        }

        // O nome do campo precisa ser exatamente este (maiúsculas e minúsculas contam)
        private static JsonElement Campo(JsonElement objeto, string nome)
        {
            Assert.True(objeto.TryGetProperty(nome, out var valor), $"O JSON não tem o campo \"{nome}\".");
            return valor;
        }

        private static void ConferirTipo(JsonElement objeto, string nome, JsonValueKind tipoEsperado)
        {
            Assert.Equal(tipoEsperado, Campo(objeto, nome).ValueKind);
        }

        // Texto: o enum precisa vir como texto ("Verde"), nunca como número (0)
        private static void ConferirTexto(JsonElement objeto, string nome, string textoEsperado)
        {
            var valor = Campo(objeto, nome);
            Assert.Equal(JsonValueKind.String, valor.ValueKind);
            Assert.Equal(textoEsperado, valor.GetString());
        }

        private static void ConferirNumero(JsonElement objeto, string nome, double numeroEsperado)
        {
            var valor = Campo(objeto, nome);
            Assert.Equal(JsonValueKind.Number, valor.ValueKind);
            Assert.Equal(numeroEsperado, valor.GetDouble());
        }
    }
}
