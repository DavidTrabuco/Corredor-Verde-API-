# Corredor Verde · API

API em ASP.NET Core (.NET 10) do trabalho A3 de Gestão e Qualidade de Software (UNIFACS), eixo Trânsito.

A simulação mostra uma cidade com 20 cruzamentos. Duas ambulâncias e duas viaturas dos bombeiros patrulham as ruas. Quando há uma emergência, a API abre um **corredor verde** nos semáforos do caminho. Todas as regras de negócio (RN01 a RN15) ficam aqui; a simulação visual em React ([CorredorVerde-Maquete](https://github.com/DavidTrabuco/CorredorVerde-Maquete)) só desenha, anima e avisa a API.

## Stack
.NET 10 · ASP.NET Core com controllers · estado em memória (sem banco) · `BackgroundService` com `TimeProvider` · ProblemDetails (RFC 9457) · Swagger · xUnit

## Pré-requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)

## Como rodar

Na raiz do repositório:
```bash
dotnet run --project CorredorVerde
```
- A API sobe em `http://localhost:5080`.
- O Swagger fica em `http://localhost:5080/swagger`.
- O arquivo `CorredorVerde/CorredorVerde.http` tem um exemplo de cada endpoint (abre no Visual Studio ou no VS Code com a extensão REST Client).

Para ver a simulação, rode também a [Maquete](https://github.com/DavidTrabuco/CorredorVerde-Maquete) (`npm run dev`, porta 5173). A origem dela já está liberada no CORS (`Cors:Origens` no `appsettings.json`).

## Configuração (RNF12)

Tudo fica na seção `Simulacao` do `appsettings.json`: tamanho da malha, tempos do semáforo (8 s de verde, 2 s de amarelo), tempo de atendimento (4 s), cautela no semáforo em falha (1 s), passo do relógio (100 ms), hospitais, bases e viaturas. Se faltar algo obrigatório, a API não sobe e diz o que está errado (`ValidateOnStart`).

## Endpoints

Todos começam com `/api/v1`. O formato completo está em `docs/referencias/contrato-api.md`.

| Método | Rota | O que faz | Respostas |
|---|---|---|---|
| GET | `/malha` | Cruzamentos, hospitais e bases | 200 |
| GET | `/estado` | Tempo, semáforos, viaturas e ocorrências numa resposta só (polling de 250 ms) | 200 |
| GET | `/semaforos` · `/viaturas` · `/ocorrencias` | As mesmas listas do `/estado`, separadas | 200 |
| POST | `/ocorrencias` | Registra uma emergência e despacha a viatura livre mais perto (UC03) | 201 · 400 · 409 |
| POST | `/viaturas/{id}/saida` | A viatura saiu de um cruzamento e entrou numa rua (UC01, UC07) | 204 · 400 · 404 · 409 |
| POST | `/viaturas/{id}/chegada` | A viatura chegou a um cruzamento (UC01, UC06) | 204 · 400 · 404 |
| GET | `/cruzamentos/{id}/pode-atravessar?eixo=NS&viaturaId=AMB-01` | Pode passar da faixa de parada? (RN07, RN13) | 200 · 400 |
| POST | `/cruzamentos/{id}/falha` | Liga ou desliga a falha do semáforo (UC09) | 204 · 400 |
| GET | `/log?desde={seq}` | Log de eventos (UC10, RN14) | 200 |
| PUT · DELETE | `/log/{seq}` | Sempre recusado: o log não pode ser alterado (RNF05) | 405 |
| POST | `/simulacao/controle` | Pausar e velocidade (1, 2 ou 4) | 204 · 400 |
| POST | `/simulacao/reiniciar` | Volta tudo ao início | 204 |

Todo erro sai como ProblemDetails, com a explicação em português no campo `detail`:
```json
{ "status": 409, "title": "Conflito", "detail": "AMB-01 e AMB-02 estão ocupadas. Espere uma delas terminar.", "instance": "/api/v1/ocorrencias" }
```

## Estrutura

```
CorredorVerde/
├── API/Controllers/          Malha, Estado, Ocorrencia, Viatura, Cruzamento, Log, Simulacao (só chamam os services)
├── API/Extensao/             CORS da maquete e resposta de validação em português
├── API/Filtros/              RegistroChamadaFilter: os POST vão para o log como tipo "Api"
├── Application/DTO/          Request e Response (cada Response tem o método estático De)
├── Application/Service/      As regras de negócio (uma classe por responsabilidade)
├── Domain/Entidades/         Cidade (o estado da simulação), Cruzamento, Semaforo, Viatura, Ocorrencia...
├── Domain/Enums/  Domain/Exceptions/  Domain/Interfaces/  Domain/Options/
├── Infrastructure/Simulacao/ RelogioSimulacaoWorker: avança o tempo a cada 100 ms
└── Middlewares/              GlobalExceptionHandler: exceção do domínio → ProblemDetails
CorredorVerde.Tests/          Testes xUnit (TestSupport/CidadeFactory monta a cidade sem o relógio)
```

O estado inteiro fica na classe `Cidade`, registrada como Singleton. Todo método público dos services usa `lock (cidade.Trava)`, porque o relógio e as requisições mexem na cidade ao mesmo tempo.

## Services e regras

Cada service só depende dos que vêm antes dele, para não haver ciclo:
`LogEventos ← Malha ← Semaforo ← Corredor ← Viatura ← Ocorrencia`, `Falha → Corredor, Viatura` e `Temporizador → Semaforo, Viatura`.

| Service | Métodos públicos | Regras |
|---|---|---|
| `LogEventosService` | `Registrar`, `Contar`, `ObterContagem`, `Listar` | RN14, RNF05, UC10 |
| `MalhaService` | `ObterCruzamento`, `Vizinho`, `DirecaoEntre`, `MenorCaminho`, `MontarRota`, `HospitalMaisProximo` | RN04, RN05, RNF07 |
| `SemaforoService` | `Luz`, `Vencedor`, `Atualizar`, `PodeAtravessar`, `Liberar` | RN01, RN02, RN03, RN07, RN08, RN12, RN13 |
| `CorredorService` | `IniciarTrecho`, `VerificarConflito`, `LiberarTodas` | RN06, RN08, RN12, RN13, RNF03 |
| `ViaturaService` | `Obter`, `Saida`, `Chegada`, `IniciarTrecho`, `ChegouAoDestino`, `FimDoAtendimento`, `TerminarServico`, `IniciarRetorno`, `ChegouNaBase`, `VoltarAPatrulha` | RN08, RN09, RN10, RN11, RN15, UC01, UC05, UC06, UC07 |
| `OcorrenciaService` | `Registrar` | RN04, RN11, RN15, RNF04, UC03 |
| `FalhaService` | `Definir` | RN05, RN13, UC09 |
| `TemporizadorService` | `Avancar` | UC02, UC05 |
| `SimulacaoService` | `Malha`, `Estado`, `Semaforos`, `Viaturas`, `Ocorrencias`, `Controlar`, `Reiniciar` | RNF08, RNF12 |

## Testes

```bash
dotnet test
dotnet test --filter "Requisito=RN12"      # só os testes de uma regra
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings   # cobertura das regras de negócio (RNF11)
```

Os testes conferem só o que os requisitos pedem: cada regra (RN), cada caso de uso (UC) e os RNF que têm verificação automática (RNF01, 03, 04, 05, 06, 07 e 12). Formato de JSON, CORS e o relógio em segundo plano não têm teste próprio.

Cada teste tem um `DisplayName` com o ID da regra e uma frase em português, e um `[Trait("Requisito", "...")]` para cada requisito coberto. O formato está descrito no documento de progresso do grupo.

Para testar os services novos, ligue-os no `CidadeFactory` na mesma ordem do `Program.cs`:
```csharp
Viatura = new ViaturaService(Cidade, Malha, Semaforo, Corredor, Log);
Ocorrencia = new OcorrenciaService(Cidade, Malha, Viatura, Log);
Falha = new FalhaService(Cidade, Malha, Semaforo, Corredor, Viatura, Log);
Temporizador = new TemporizadorService(Cidade, Semaforo, Viatura);
```
Nos testes, o tempo anda com `Temporizador.Avancar(segundos)`, em passos de 0,1 s, sem esperar de verdade.

Os requisitos que dependem da tela (RNF08, RNF09, RNF10) são conferidos pelo roteiro manual no README da Maquete.
