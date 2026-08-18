# Estudo — Alive Scenes (v1.2.0, decompilado)

Pasta analisada: `AliveScenes/` (raiz do workspace). O módulo **não está instalado** em
`Modules/` do jogo — hoje é só material de estudo.

## 1. O que o mod faz

Três sistemas independentes, todos ligados a cenas (missions), nada de campanha:

1. **Fala ambiente com balão sobre a cabeça** (o coração do mod) — NPCs de cidade,
   vila, taverna, salão do lorde e **soldados em batalha** soltam falas de um banco
   XML, com balão Gauntlet posicionado no mundo (world→screen).
2. **Multidão** (`Crowds`) — clona os `Townsman`/`Townswoman` da cultura 3–5x por
   spawn point e manda cada clone caminhar entre os pontos originais, deixando a
   cidade cheia.
3. **Ação militar** (`MilitaryActions`) — 3 cavaleiros do dono da cidade entram pelo
   spawn externo, cavalgam até o `sp_prison_guard` e somem (fade out). Puramente
   cosmético.

Configuração em `ModConfiguration.xml` (cooldowns, chance por pessoa, nº de conversas
paralelas, filtro de palavrão, on/off de cada sistema).

## 2. Arquitetura (arquivos-chave)

| Arquivo | Papel |
|---|---|
| `AliveScenes/SubModule.cs` | Portaria: decide em quais missions injetar os behaviors |
| `AliveScenes/Patcher.cs` | Harmony postfix em 9 métodos de `SandBoxMissionViews` (`Open*Mission`) para anexar a `MissionPopDialogueView` |
| `AliveScenes/NavalDynamicPatcher.cs` | Patch **por reflexão** em `NavalDLC.View.NavalViews.OpenNavalBattleMission` — só aplica se o assembly do War Sails existir |
| `ModSystem/ConversationSystem.cs` | Carrega/filtra o banco de falas (singleton) |
| `ModSystem/PopMessageManager.cs` | Cérebro: escolhe ator, testa condições, cooldowns, tempo de leitura, substituição de variáveis |
| `CustomMissionBehavior/MissionConversationLogic.cs` | Tick da missão: sorteia agente perto do jogador e dispara a fala |
| `CustomMissionBehavior/MissionCrowdControlLogic.cs` | Multidão |
| `CustomMissionBehavior/MissionTownExtraTroopsLogic.cs` | Cavalaria cosmética |
| `CustomViewModel/*`, `MissionWidget/*`, `GUI/*` | Balão Gauntlet (posição de tela, fonte por distância, vermelho para inimigo) |

### Portaria do SubModule (importante para compat)
Ele **não** entra em: custom battle, Fourberie (`fb_pit_fight_`, `fb_escape`,
`safehouse_`), cenas `_battle_site_`, missões que já têm `ConversationMissionLogic`,
raides, esconderijos, briga de beco e missão disfarçada. Entra em: batalha campal,
cerco, sally out, **batalha naval**, cidade e vila.

### Fluxo de uma fala
`MissionConversationLogic.OnMissionTick` → a cada `SCAN_COOLDOWN` (7s casual, 22s
batalha terrestre, **42s naval**) pega agentes num raio (10m casual / 25m batalha, com
filtro de altura `|Δz| < 1`), sorteia um, rola `ChancePerPerson`, pede fala ao
`PopMessageManager` → este classifica o `ActorType`, testa as `Condition`s, marca
cooldown do agente e devolve o texto → a VM cria o balão, que some depois de
`length * WaitTimeMultiplier` segundos (mínimo 2s).

Diálogos de dois falantes alternando só disparam para `NOBLE` e `COMPANION`.

## 3. Conteúdo

- **2.284 one-liners** e **145 diálogos** (511 linhas), tudo em `ModData/ConversationData.xml` (510 KB).
- Traduções completas embutidas: RU, DE, FR, SP, TR, CNs (~300 KB cada). Sem PT.
- Distribuição por ator: `ADULT_TOWN` 351, `BATTLE_INFANTRY` 328, `COMPANION` 313,
  `ADULT_VILLAGE` 246, **`SEA_ROW` 175 / `SEA_STANDING` 121**, `GUARD` 144, `NOBLE` 143…
- Condições náuticas: `AT_SEA_ANY` 145, `AT_SEA_RAINY` 80, `AT_SEA_WINDY` 71.
- Variáveis suportadas no texto: `{SETTLEMENT}`, `{SETTLEMENT_NAME}`,
  `{SETTLEMENT_OWNER}`, `{SIEGE_SETTLEMENT_NAME}`, `{CRIMINAL_NAME}`,
  `{CULTURAL_NAME}`, `{RANDOM_FACTION}`, `{ENEMY_FACTION}`, `{PLAYER_NAME}` — tudo
  resolvido em runtime pela cultura/facção da partida, ou seja, **não amarra em lore vanilla**.

### Compatibilidade de lore com o RF
- Zero menções a Vlandia/Battania/Aserai/Sturgia/Khuzait/Empire.
- **1 fala cita "Calradia"** (`BLAS24o95d`, ator COMPANION) — trocar se for usar.
- 17 menções genéricas a "gods"/"the gods" — checar contra `RFReligions`.
- Palavrão vem marcado com tags `<profanity>…</profanity>` (ver bug #3).

## 4. Bugs e problemas encontrados

Em ordem de gravidade:

1. **[CRÍTICO] Mutação de `culture.NotableTemplates`** — `MissionCrowdControlLogic.SpawnNewAgent`
   faz `((List<CharacterObject>)culture.NotableTemplates).Add(...)` com
   `RangedEliteMilitiaTroop`, `EliteBasicTroop` (duas vezes), `BasicTroop`, `Villager`,
   `ShopWorker` e mais `RebelliousHeroTemplates` **a cada agente clonado**. Com 3–5 clones
   por spawn point são centenas de inserções por visita à cidade, na lista viva da
   `CultureObject`. Efeitos: a lista cresce sem parar na sessão e o jogo passa a poder
   criar notáveis a partir de tropas básicas (`NotableTemplates` alimenta o `HeroCreator`
   da campanha). Só volta ao normal reiniciando o jogo.
2. **[ALTO] `GetRandomElement` em lista vazia** — em `BattleConversations()` e
   `CasualConversations()` a lista é sorteada antes do teste de vazio e, dentro do laço,
   logo depois do `Remove`. Se não houver agente por perto (ou o último for removido),
   sorteia de lista vazia. Não há try/catch no `OnMissionTick`.
3. **[ALTO] Filtro de palavrão invertido** — `ProcessProfanity(text, !ProfanityFilterEnabled)`
   e, dentro, `if (!isProfanityAllowed) return value;` (devolve sem censura). Na prática:
   `ProfanityFilter Enabled="False"` **censura**; `Enabled="True"` mostra tudo.
4. **[MÉDIO] `Finalizer` que engole exceções alheias** — `BattleAgentLogic_OnAgentHit_Patch`
   descarta **qualquer** exceção de `BattleAgentLogic.OnAgentHit` enquanto
   `IgnoreError == true` (ligado em toda cena de settlement). Isso mascara erros de
   outros módulos nossos (RF_Magic, RBM_RF, RF_DualWield) justamente onde eles doem.
5. **[MÉDIO] `InformationManager.ClearAllMessages()`** em
   `MissionPopDialogueView.OnMissionScreenFinalize` — limpa o log de mensagens de todo
   mundo ao sair de qualquer cena patchada.
6. **[MÉDIO] Config lida do nó errado** — `CasualChat.Cooldown` é lido de `TavernChat`
   (`ModConfiguration.cs:104`), então o valor 40 do XML nunca vale (vira 15).
7. **[MÉDIO] `GetBoolAttribute`/`GetBoolValue` fazem `TryParse(...) & result`** — com
   atributo inválido retornam `false` em vez do `defaultValue`.
8. **[MÉDIO] `BattleChat/EnemyChat Enabled` nunca é consumido** — a opção existe na
   config e na classe, mas nada lê `EnemyChatEnabled`; falas de inimigo sempre aparecem.
9. **[MÉDIO] `RemoveAgentTarget` usa `SingleOrDefault`** — lança se o mesmo agente
   estiver com dois balões vivos; `FirstOrDefault` seria seguro.
10. **[MENOR] Conteúdo morto** — `WEAPONSMITH` tem **70 falas** no XML mas
    `DetermineActorType` nunca devolve esse tipo (só BLACKSMITH/ARMORER); idem
    `NOTABLE_CITY`/`NOTABLE_VILLAGE` (sem código e sem falas). `_isInPort` é calculado e
    nunca usado — seria o gancho natural para falas de porto no War Sails.
11. **[MENOR] Variável sobrescrita** — em `GetMessageForAgent`, `flag5` recebe
    `IsFemale` e na linha seguinte é sobrescrita por `MovementVelocity != Vec2.Zero`;
    o sexo do agente acaba não influenciando nada.
12. **[MENOR/VERIFICAR] Ator naval possivelmente invertido** — `agent.IsUsingGameObject`
    → `SEA_STANDING`, senão `SEA_ROW`. Quem está usando um game object no navio costuma
    ser o remador; parece trocado. Vale confirmar em jogo com War Sails.
13. **[MENOR] `Dialogue.CurrentIndex` não reseta entre missões** — `BootstrapData` refaz
    o dicionário com as **mesmas instâncias**; diálogo interrompido recomeça do meio na
    cena seguinte.
14. **[MENOR] IDs de item hardcoded** em `MissionTownExtraTroopsLogic`:
    `t2_battania_horse` e `light_harness` existem no RF (`rfitems/rf_horses_and_others.xml`),
    mas `western_spear_3_t3` **não existe** — está protegido por null-check, só perde a lança.

## 5. Compatibilidade com o RF / War Sails

**A favor**
- O suporte naval **já existe** e é feito por reflexão (`NavalDynamicPatcher`): se o
  `NavalDLC.View` não estiver carregado, não faz nada. Tem cooldown naval próprio (42s)
  e 296 falas de mar prontas.
- Todos os patches são *postfix aditivos* em `SandBoxMissionViews` — risco baixo de
  brigar com RBM, RF_Magic ou RF_DualWield.
- `RF_AIDialog` (BattleShout com LLM) é **complementar**, não concorrente: lá quem fala é
  o jogador e as respostas vão para o log; aqui é ambiente, determinístico, em balão. O
  único cuidado é o ruído somado em batalha.
- `RF_LivingWorld` (rumores) atua na campanha, não em cena — sem sobreposição.

**Riscos**
- **Performance**: a multidão multiplica os NPCs de cidade por 4–6. Em cenas grandes do
  `RF_Map` somadas a `RF_PartyVisuals` e `RBM_RF` isso pesa; `MultiplicationMin/Max` resolve.
- **`RF_Races`/`RFMonsters`**: o clone usa `BodyProperties.GetRandomBodyProperties` com
  raça/sexo vindos dos `NotableTemplates` da cultura. Com raça customizada dá para sair
  corpo/cabeça errados — e é o mesmo trecho do bug #1.
- **`CanBeCloned`** só aceita `Culture.Townsman`/`Townswoman`; cultura RF que não defina
  esses campos simplesmente não gera multidão (falha silenciosa).
- **Cavalaria cosmética** depende das tags de cena `sp_prison_guard` e
  `spawnpoint_player_outside`; cenas próprias do `RFCustomScenes` podem não ter — nesse
  caso ela se desliga sozinha (`_canGenerate = false`), sem crash.
- O `Finalizer` do item #4 pode esconder crash de outros módulos nossos.

## 6. Decisão tomada (2026-08-18): caminho B

Foi criado o projeto **`RF_AliveScenes`** na solution, compilando para dentro do módulo
`RealmsForgotten` — ver [RF_AliveScenes/README.md](RF_AliveScenes/README.md) para o que
entrou, o que ficou de fora e as 13 correções aplicadas. Build limpo, ainda **sem teste
em jogo**. O resto desta seção fica como registro das alternativas consideradas.

Três caminhos:

- **A — usar como módulo separado, sem tocar**: instalar em `Modules/AliveScenes` e
  carregar depois do RF. Ganho imediato, mas carrega os bugs #1–#5.
- **B — absorver como `RF_AliveScenes`** (mesmo padrão do `RF_Homesteads`): recompilar
  com as correções mínimas abaixo e revisar o banco de falas para o lore do RF.
- **C — só extrair as ideias**: o balão sobre a cabeça com banco XML condicionado é
  simples de reescrever; multidão e cavalaria são pequenas.

**Correções mínimas antes de qualquer teste sério (caminho B):**
1. Não mutar `NotableTemplates` — montar lista local (`new List<CharacterObject>(culture.NotableTemplates)`) e sortear dela.
2. Proteger o sorteio de lista vazia nos dois laços de conversa.
3. Desinverter o filtro de palavrão.
4. Restringir ou remover o `Finalizer` do `OnAgentHit` (logar em vez de engolir).
5. Tirar o `ClearAllMessages()` (ou limitar aos balões).
6. Ler `CasualChat/Cooldown` do nó certo e respeitar `EnemyChat Enabled`.
7. Trocar `SingleOrDefault` por `FirstOrDefault` em `RemoveAgentTarget`.
8. Conteúdo: trocar a fala com "Calradia", revisar as 17 menções a "gods" e decidir se
   `WEAPONSMITH` ganha classificação no código ou se as 70 falas viram BLACKSMITH.

**Crédito**: é mod de terceiro (`com.bloc.alivescenes`), aqui decompilado. Se for
redistribuído dentro do RF vale o mesmo cuidado de crédito/permissão dos outros
conteúdos emprestados.
