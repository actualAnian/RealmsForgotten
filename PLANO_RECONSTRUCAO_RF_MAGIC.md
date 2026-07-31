# Reconstrução do RF_Magic — o que é SOTOR e o que é RF

## ACHADO 2026-07-29 — a causa dos marcadores quebrados não era código

Durante a fase 1 a comparação `ModuleData` entre SOTOR e RF_Magic revelou **17
XMLs de template vazios** que o RF_Magic publicava e o SOTOR não:

    action_sets.xml       action_types.xml      collision_infos.xml
    combat_parameters.xml face_animations.xml   item_holsters.xml
    native_parameters.xml physics_materials.xml skins.xml
    items.xml             partyTemplates.xml    settlements.xml
    spclans.xml           spcultures.xml        spkingdoms.xml
    spnpccharacters.xml   spworkshops.xml

Os nove primeiros continham literalmente `<replace_this_with_actual_nodes/>`.
Tamanhos: `physics_materials.xml` 238 B contra 10.892 B do Native;
`collision_infos.xml` 302 B contra 97.840 B; `action_sets.xml` 214 B contra
956.569 B.

Por que passou por toda auditoria de código: esses XMLs de engine **não são
declarados em `<Xmls>`**. O jogo os carrega por convenção de nome, de todo
módulo ativo. Nenhuma leitura de C#, patch Harmony ou diff de assets os
alcançaria.

Por que só o RF_Magic: vieram do template de módulo novo do Bannerlord, criado
quando o RF_Magic nasceu. O SOTOR, sendo um módulo antigo, não os tem — e
**nenhum outro módulo instalado tem** (varredura confirmou).

`physics_materials` e `collision_infos` definem material e colisão de
renderização. Um nó inválido ali é geometria com material quebrado — preto e
vermelho, fragmentada.

Todos removidos, backup em `_RF_Magic_stubs_removidos_2026-07-29/`.

Lição para a regra abaixo: **módulo novo criado por template chega com lixo que
sobrescreve o jogo base. Conferir `ModuleData` contra o Native antes de
qualquer outra coisa.**

---


Documento de regra, não de desejo. Se uma mudança não couber numa das
categorias abaixo, ela **não entra**.

## 0. O erro que causou a reconstrução

A versão anterior editava lógica RF **dentro** dos arquivos do SOTOR. O diff
contra o SOTOR original virou 2750 linhas em 40+ arquivos, misturando três
coisas indistinguíveis entre si: correção de decompilação, gancho de RF e
regra de jogo RF.

Consequência prática: quando algo quebrou no jogo, não havia como responder
"isto é SOTOR ou isto é nosso?" sem ler tudo. Foi assim que um método meu
(`ApplyLoreMaterial`) ficou escondido dentro de `AbilityCrosshair` do SOTOR
por rodadas de teste — trocando material de mesh compartilhado, sem ninguém
ver.

A regra abaixo existe para que essa pergunta tenha resposta em um comando.

## 1. Regra de ouro

> **Arquivo em namespace `SOTOR.*` é código de terceiro. Somente leitura,
> com três exceções nomeadas e marcadas.**
>
> **Toda lógica RF vive em `SOTOR.RFIntegration`.**

Verificação, a qualquer momento:

```bash
diff -ru SOTOR_src RF_Warsails_AI/RF_Magic
```

O resultado só pode conter linhas das categorias **A**, **B** e **C**. Qualquer
outra coisa é violação da regra, não "um ajuste pequeno".

## 2. As três categorias permitidas em arquivo do SOTOR

### A — Correção de artefato de decompilação
O ILSpy produz código que não compila ou que se comporta diferente do
original. Corrigir é obrigatório, mas é **restauração**, não mudança.

Exemplos reais já encontrados:
- `((MissionBehavior)this).OnBehaviorInitialize()` — cast em `this` não desliga
  dispatch virtual em C#; o método chamava a si mesmo (StackOverflow ao entrar
  em batalha). Original era `base.`
- `ScreenPointToWorldRay(..., ref a, ref b)` onde a assinatura é `out`
- `using` faltando (`MissionAgentSpawnLogic`)
- Negação De Morgan sem inverter a comparação (quebrou o teste de linha de
  visão da IA de conjuração)

Marca obrigatória na linha:
```csharp
// [RF-A] artefato ILSpy: <o que o decompilador emitiu> -> <o correto>, <por quê>
```

### B — Gancho para o RF
**Uma linha**, chamando `SOTOR.RFIntegration`. Nunca lógica inline, nunca um
`if` com regra de jogo, nunca um cálculo.

Certo:
```csharp
// [RF-B] gancho
radius *= RFSpellTuning.RadiusMultiplier(caster);
```

Errado (isto é lógica RF dentro do SOTOR):
```csharp
if (caster.GetHero()?.GetPerkValue(...) == true) radius *= 1.25f;
```

Se o gancho precisa de mais de uma linha, ele está no lugar errado: mova o
corpo para `RFIntegration` e deixe só a chamada.

### D — Identidade do módulo
O SOTOR localiza os próprios arquivos por `ModuleHelper.GetModuleFullPath("SOTOR")`.
O fork mora em `RF_Magic`, então esse id não existe e a chamada lança
`KeyNotFoundException` — crash em `AbilityFactory.LoadTemplates`.

Isto não é artefato de decompilação (o código estava certo para o módulo dele)
nem lógica RF. É **identidade**: qual pasta hospeda o assembly.

Resolvido num lugar só, `RFIntegration/RFModulePath`, que tenta `RF_Magic` e
cai em `SOTOR` como plano B. Marca `// [RF-D]`.

Atenção: nem todo literal `"SOTOR"` é identidade de pasta. O id do UIExtender,
o nome da pasta de log e o `FolderName` do MCM são rótulos e **continuam
"SOTOR"** — trocá-los sem necessidade quebraria a UI e perderia as configurações
já gravadas do usuário.

### C — Reskin de UI
Layout de tela e nome visível. Fica confinado a:
- `GUI/Prefabs/**` (XML)
- os `*VM.cs` da tela mexida
- `ModuleData/Languages/**` para texto

Nada de regra de jogo aqui. "Winds of Magic" → "Mana" é **tradução**, não
código: entra em Languages, nunca num literal C#.

## 3. Onde cada funcionalidade do RF mora

| Funcionalidade | Onde mora | Categoria no SOTOR |
|---|---|---|
| Foco arcano define quem conjura | `RFIntegration/ArcaneFocus*` | B: 1 gancho na criação de agente |
| Cultura define escolas | `RFIntegration/RFCultureLores` + `ModuleData/rf_culture_lores.xml` | B: 1 gancho no repertório |
| Cajado dá afinidade (custo/cooldown) | `RFIntegration/ArcaneFocusAffinity` | B: 1 gancho em custo, 1 em cooldown |
| Pool de mana de tropa + regeneração | `RFIntegration/TroopWindsPool` | B: 1 gancho no tick da missão |
| Requisito de escola por item | `RFIntegration/RFLoreRequirements` + XML | B: 1 gancho |
| Skill "arcane" no lugar de Cartridge | `RFIntegration/ArcaneFocusSkillPatch` | nenhuma — é Harmony próprio |
| Ajuste da IA de conjuração | **decidir caso a caso** — ver §4 | A ou B |
| Layout do grimório, botão, HUD | GUI XML + VMs | C |
| "Winds of Magic" → "Mana" | `ModuleData/Languages` | C |
| Cajados Musket→Polearm | `RealmsForgotten/ModuleData/rfitems` | **fora do RF_Magic** |
| Magia legada desligada, necromancer/gandalf preservados | `RealmsForgottenMain` | **fora do RF_Magic** |

Duas linhas importam nessa tabela: as duas últimas **não são RF_Magic**. Já
estavam certas antes e não voltam para cá.

## 4. O caso ambíguo: IA de conjuração

Aqui é onde a disciplina custa. Mudança na IA do SOTOR pode ser:

- **A** se o comportamento original estava certo e a decompilação o quebrou.
  Ex.: o `&&` de 4 termos do teste de linha de visão que virou 3.
- **B** se é regra nossa (tropa só conjura com foco, respeita pool de mana,
  intervalo mínimo entre conjurações).
- **Nem uma nem outra** se é "eu acho que fica melhor assim". Isso **não entra
  agora**. Fica anotado e volta depois que a base estiver verificada.

Ajuste de números (limiares, pesos de eixo) é B: vai para uma classe de
tuning em `RFIntegration`, não espalhado por `Axis` e `CommonAIDecisionFunctions`.

## 5. Ordem de execução, com portão de teste

Cada fase termina em build + teste **no jogo**. Só passa para a seguinte se o
teste passar. É isto que evita repetir o erro: quando quebrar, quebrou na
última camada, não em algum lugar de 2750 linhas.

| Fase | Conteúdo | Como se testa |
|---|---|---|
| **1** | Base pristina do SOTOR (feito) | — |
| **2** | Só categoria **A**: compilar e não crashar | **Portão crítico.** RF_Magic sozinho no vanilla. Marcadores de tropa OK? Magia do SOTOR funciona? |
| **3** | `RFIntegration` reintegrado, **inerte** (nenhum gancho ligado) | Build + entrar em batalha. Nada deve mudar em relação à fase 2. |
| **4** | Ganchos **B** de foco arcano e cultura | Jogador com cajado conjura as escolas da cultura |
| **5** | Ganchos **B** de afinidade e pool de mana | Custo/cooldown mudam com o cajado certo; tropa regenera |
| **6** | IA de conjuração (A e B separados em commits distintos) | Tropas conjuram; marcadores continuam OK |
| **7** | **C**: grimório, HUD, "Mana" | UI |

### O portão da fase 2 é o mais importante

Se na fase 2 — SOTOR puro, só com artefatos de decompilação corrigidos — os
marcadores de posicionamento de tropa **ainda quebrarem**, então o problema
nunca esteve no código C# que eu escrevi, e a busca muda de lugar
(ModuleData, GUI, SubModule.xml, ordem de carregamento, ou interação com
outro módulo).

Se **não** quebrarem, a base está limpa e cada camada seguinte tem um culpado
identificável.

Em ambos os casos a resposta é útil. É por isso que essa fase existe antes de
qualquer funcionalidade.

## 6. O que fica de fora nesta rodada

Registrado para não se perder, e para não voltar por reflexo:

- Runa de mira com material por escola. Os materiais `rf_rune_*` **não existem**
  no módulo. Quando existirem, a implementação correta faz **cópia** do material
  por entidade e confere o recurso pelo nome — nunca confia em `null`.
- Classes de diagnóstico (`SceneEntityDumper` e afins). Nenhuma delas volta
  para dentro do projeto; se precisar medir, entra, mede e sai.
- Nada de Harmony patch em `OrderTroopPlacer`, `MissionScreen` ou qualquer
  MissionView do jogo base. O RF_Magic não tem motivo para tocar nisso.
