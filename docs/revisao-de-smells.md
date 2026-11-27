# Revisão de code smells — Corredor Verde API

Lista única dos problemas encontrados no código de produção e nos testes. Cada item vira um PR pequeno, que o colega revisa.
Situação: **pendente**, **em PR** ou **corrigido**. Itens marcados com (a conferir) foram vistos por amostragem e precisam de leitura completa antes do PR.

## Parte 1 — Código de produção

| # | Smell | Onde | Como corrigir | PR | Situação |
|---|---|---|---|---|---|
| P1 | Formatação: falta linha em branco entre métodos | `ViaturaService.cs:96` | Inserir a linha | A | pendente |
| P2 | Código duplicado: `Chegada` repete o bloco "RN10, recalcula o trecho" | `ViaturaService.cs:138-143` | Reaproveitar `RecalcularTrechoForaDaRota` | A | pendente |
| P3 | Código duplicado: montagem da rota (rua atual + `MontarRota`) em dois services | `ViaturaService.IniciarRetorno`, `CorredorService.IniciarTrecho` | Um único método de montagem | D | pendente |
| P4 | Código duplicado: blocos de "zerar o estado da viatura" | `VoltarAPatrulha`, `IniciarRetorno`, `ChegouNaBase`, `FimDoAtendimento` | Método `LimparServico` na `Viatura` | C | pendente |
| P5 | Números mágicos: `Trecho == 1/2`, retentativa `+ 1`, criticidade `1..3`, velocidades `1,2,4` | `ViaturaService`, `CorredorService`, `OcorrenciaService`, `SimulacaoService` | Enum/constantes com nome | D e SAB4 | pendente |
| P6 | Strings mágicas: cerca de 30 tags de log (`"RN10"`, `"UC07"`...) e chaves de `JaLogado` (`"espera:"`, `"semrota:"`, `"conflito:"`) | 8 arquivos | Classe de constantes | D | pendente |
| P7 | `Epsilon` declarado duas vezes | `SemaforoService`, `TemporizadorService` | Constante única | D | pendente |
| P8 | Classe grande: 320 linhas e 3 responsabilidades (movimento, atendimento, retorno) | `ViaturaService` | Separar o retorno à base em outro service | G | pendente |
| P9 | Métodos longos | `OcorrenciaService.Registrar` (78 linhas), `CorredorService.IniciarTrecho` (87 linhas) | Extrair etapas com nome | G | pendente |
| P10 | Efeito colateral antes de validar: `viatura.Rota` é preenchida antes de conferir a ocorrência | `CorredorService.cs:78-84` | Validar antes de alterar | G | pendente |
| P11 | Mensagem em inglês no 400 (viola o RNF04) e detecção frágil por `"The "` | `ValidacaoExtensao.cs:24` | Não depender do texto do framework | F | pendente |
| P12 | Camadas: `Domain` importa DTO e `Options` | `ISimulacaoService` | Mover a dependência para `Application` | G | pendente |
| P13 | Código morto: `RegraDeNegocioException` e o 422 do handler nunca são usados | `Domain/Exceptions`, `GlobalExceptionHandler` | Remover | H | pendente |
| P14 | Guardas redundantes: `v.Rota = new List<PassoRota>()` ao chamar quem voltava; `v.IndoPara != id` no ramo de retorno | `OcorrenciaService:54`, `FalhaService` | Remover | H | pendente |
| P15 | Defensivos que a API nunca aciona: `Obter(null)`, `?? string.Empty` | `ViaturaService.Obter`, `OcorrenciaService.Registrar` | Remover ou documentar | H | pendente |
| P16 | Mensagens: espaço duplo quando o campo é nulo; "a sem caminho" (gramática) | `MalhaService`, `OcorrenciaService.Quarteiroes` | Corrigir o texto | H | pendente |
| P17 | `Cidade.Reiniciar` põe viatura sem base em C1 em silêncio e ignora hospital inválido | `Cidade.cs:79-88` | Avisar no log | H | pendente |
| P18 | A `Chegada` não confere se o cruzamento é vizinho da posição anterior (só a `Saida` confere) | `ViaturaService.Chegada` | Decidir se vira regra | (a decidir) | pendente |

## Parte 2 — Testes

| # | Smell | O que encontrei | Como corrigir | PR | Situação |
|---|---|---|---|---|---|
| T1 | Números mágicos | `36000`, `0.1`, `7.9`, `2.1`, `10.2`, `30.0`, `4.5`, `4.0` em `SemaforoServiceTests` e `ViaturaApiTests` | Constantes com nome (`VerdeSeg`, `AmareloSeg`, `AtendimentoSeg`) | T-final | pendente |
| T2 | Valores importantes escondidos | 72 usos literais de `"AMB-01"`, `"BOM-01"`... em 9 arquivos; `IdsDasViaturas` só cobre parte | Usar `IdsDasViaturas` em todos | T-final | pendente |
| T3 | Nome genérico | `var f` em 27 pontos de 5 arquivos | Renomear para `cidade` ou `cenario` | T-final | pendente |
| T4 | Comparação de mensagem exata | 22 usos de `LogTem("texto completo")` | Conferir só o trecho que carrega a regra | T-final | pendente |
| T5 | Assertiva excessiva / teste muito abrangente | `ViaturaApiTests` tem 13 `Assert` em um teste que percorre chamado, atendimento, hospital e retorno | Dividir ou assumir como teste de ponta a ponta, com nome que diga isso | T-final | pendente |
| T6 | Vários Act + Assert em sequência | RN03 e RN12 em `SemaforoServiceTests` | Uma ação por teste (`Theory` para os instantes de tempo) | T-final | pendente |
| T7 | Ausência de fronteira relevante | Criticidade só testa 4; faltam `0` e `-1`; velocidade fora de 1, 2 e 4 (a conferir) | Teste de limites | SAB4 | pendente |
| T8 | Teste de infraestrutura como regra | Testes de API que só confirmam formato ou status de resposta (a conferir `LogApiTests` e `SimulacaoApiTests`) | Manter só o que prova regra ou RNF | SAB3 | pendente |
| T9 | Assertiva fraca | `Assert.NotNull`/`NotEmpty` em 13 pontos; a maioria é guarda antes de outras verificações (a conferir um a um) | Trocar por verificação do valor esperado | T-final | pendente |
| T10 | Preparação excessiva (a conferir) | `ViaturaServiceTests` (305 linhas, 15 testes) | Helpers no `CidadeFactory` | T-final | pendente |
| T11 | Teste duplicado | Reduzido pelo PR #13 (de 167 para 66 testes) | Reavaliar depois das mudanças | — | corrigido em parte |

Verificado e **sem problema**: não há `Task.Delay`, `Thread.Sleep`, `DateTime.Now` nem `Random` nos testes (o tempo é simulado); não há mocks (os testes usam os services reais); cada teste cria a sua própria cidade (sem estado compartilhado nem dependência entre testes).
