# Reescrita do RF_DualWield — diagnóstico do CTD e nova implementação

Data: 2026-08-10
Escopo: módulo `RF_DualWield` apenas. Nenhum outro módulo do RF foi alterado (leitura só).
Estado: **build limpo (0 erros / 0 warnings) + deploy feito**. Não testado em jogo — isso é do autor.

---

## (a) Diagnóstico da Fase A — a causa do CTD, com evidência

### A causa: 82 `action_type` declarados, ZERO com animação ligada

O crash **não estava no C#**. O C# anterior tinha 35 linhas em 2 arquivos (um `MBSubModuleBase` que só chamava
`PatchAll` e um postfix de colisão), nenhum uso de `ActionIndexCache`, nenhum `SetActionChannel`, nenhuma
chamada de agente. Nada ali podia derrubar a engine.

O crash estava na **cadeia de assets**:

| Arquivo | O que declara |
|---|---|
| `ModuleData/action_types.xml` | **82** `<action name="act_dual_*" />` + `act_*_1h_with_d_shld` |
| `ModuleData/item_usage_sets.xml` | usa `act_dual_ready_thrust_1h`, `act_dual_release_thrust_1h`, ... |
| `ModuleData/movement_sets.xml` | usa `act_walk_idle_1h_with_d_shld`, `act_run_forward_1h_with_d_shld`, ... |
| `ModuleData/action_sets.xml` | **1 linha**: `<action_set id="as_rf_dualwield_registration_anchor" base_set="as_human_warrior" />` — **nenhum binding** |

No Bannerlord, `action_types.xml` só *declara* a existência da action. Quem liga action → clipe de animação é
`action_sets.xml` (`<action type="..." animation="..." />`). Sem binding, `MBActionSet.GetAnimationIndexOfAction`
devolve **-1**, e o sistema de animação nativo indexa o array de clipes com esse -1 → **access violation, CTD sem
exceção gerenciada** (é por isso que o Visual Studio não mostra nada).

### A evidência

Dois `rgl_log` de 2026-08-10 **terminam abruptamente** (sem `Closing log file...`, sem shutdown) exatamente nestas
linhas — `rgl_log_31292.txt` (15:44) e `rgl_log_36692.txt` (16:59), idênticos:

```
[15:44:34.058] SetOrder AIControlOffon team
[15:44:34.058] Selected formations being cleared.
[15:44:34.301] as_human_warrior does not contain act_dual_quick_release_thrust_1h
[15:44:34.301] 5013 -1 6218
[15:44:34.301] as_human_warrior does not contain act_dual_quick_release_thrust_1h
[15:44:34.301] as_human_warrior does not contain act_dual_release_thrust_1h
[15:44:34.301] as_human_warrior does not contain act_dual_quick_release_slashleft_1h
[15:44:34.301] 5013 -1 6218
[15:44:34.301] as_human_warrior does not contain act_dual_quick_release_slashleft_1h
[15:44:34.302] as_human_warrior does not contain act_run_idle_1h_with_d_shld
[15:44:34.302] as_human_warrior does not contain act_run_forward_1h_with_d_shld   <-- fim do log
```

`5013 -1 6218` = (índice da action, **índice de animação = -1**, índice do set). O log morre na linha seguinte.
Momento: deploy de batalha (as linhas anteriores são `SetOrder`/formações) — ou seja, o agente com as duas armas
entrando em cena. É a assinatura exata do crash nativo, reproduzida duas vezes.

O `ModuleData/action_sets.xml.pre_actionset_fix_20260805.bak` contém literalmente `<action_sets />` — o arquivo
sempre esteve vazio; o "fix" de 05/08 só trocou vazio por um action_set âncora inútil. **Nenhuma versão do módulo
jamais teve os bindings**, e o mod original (`UniqueCombat`) também não: o `action_sets.xml` dele (2074 bytes)
tem 0 ocorrências de `act_dual` — o dual wield nunca funcionou lá tampouco, era protótipo inacabado.

### Bug secundário encontrado: `project.mbproj` fora de sincronia

O engine só abre XMLs de animação declarados no `project.mbproj`. Comprovado no log:

```
opening ..\..\Modules\Native/ModuleData/action_sets.xml
opening ..\..\Modules\RF_Races/ModuleData/action_sets.xml
opening ..\..\Modules\RF_Extension/ModuleData/action_sets.xml
opening ..\..\Modules\RF_Magic/ModuleData/sotor_action_sets.xml
opening ..\..\Modules\RF_DualWield/ModuleData/action_sets.xml
```

O `project.mbproj` **do projeto** (`_Module/ModuleData/project.mbproj`) **não tinha** a linha `soln_action_sets`;
só a cópia dentro da pasta do jogo tinha (editada à mão). Como o PostBuild do `.csproj` copia
`_Module\ModuleData\*.*` para o módulo instalado, **o próximo build ia sobrescrever o mbproj do jogo e
desregistrar o `action_sets.xml`** — a correção teria se auto-destruído no build seguinte. Corrigido.

### Como sei que dá para MESCLAR em `as_human_warrior`

Não é chute: `RF_Magic` — o módulo de magia vigente e validado em jogo — faz exatamente isso em
`ModuleData/sotor_action_sets.xml`:

```xml
<action_sets>
    <action_set id="as_human_warrior" skeleton="human_skeleton" movement_system="bipedal">
        <action type="act_spellcasting_idle" animation="spellcasting_stance_idle" />
        <action type="act_raisefromground" animation="spellcasting_raisefromground" />
    </action_set>
</action_sets>
```

O arquivo é aberto pelo engine (log acima), as animações de magia funcionam, e as animações humanas do vanilla
**não** somem. Logo o loader nativo faz **merge por id**, não replace. Adotei o mesmo padrão, atributo por atributo.

(Nota: o `RealmsForgotten/ModuleData/action_sets.xml` *parece* provar o mesmo, mas não prova — o mbproj dele não
declara `soln_action_sets`, então aquele arquivo nunca é carregado. Ver §(e), item de risco latente.)

### Os outros vetores da Fase A — verificados, todos limpos

| Vetor investigado | Resultado |
|---|---|
| Colisão de `action_type` id entre módulos ativos | **Nenhuma.** `grep act_dual` em Native / RealmsForgotten / RF_Extension = 0. RF_Races e RF_Magic não declaram ids dual. `UniqueCombat` (que colidiria em 82 ids) está **desmarcado** no launcher e não aparece nos `_MODULES_` do log |
| Colisão de `movement_set` / `item_usage_set` / `full_movement_set` / `item_holster` id | **Nenhuma** entre os módulos ativos |
| `.tpac` referenciados existem? | **Sim, 24/24.** Extraí os nomes de clipe de dentro de cada `_anm.tpac` por leitura binária; todos os 24 batem com o nome do arquivo menos o sufixo `_anm` e todos os 24 são usados agora |
| `combat_parameters.xml` vs RF_Extension | **Não havia conflito de id** (os ids são disjuntos: `*_dual_*` vs `polearm_*_elephant`). Havia coisa pior e diferente: o nosso redeclarava **23 `<def>` do vanilla**. Conferi um por um contra `Native/ModuleData/combat_parameters.xml` — **todos os 23 valores eram idênticos ao vanilla**, então era um no-op hoje, mas uma bomba armada. Removido (ver §(d)) |
| `ActionIndexCache` mal usado no C# | Não havia uso nenhum |
| Harmony com assinatura errada | O único patch (`IsCollisionBoneDifferentThanWeaponAttachBone`) está **correto**; confirmei por reflexão nos assemblies reais da 1.4.8: `public static bool (in AttackCollisionData collisionData, int weaponAttachBoneIndex)` |
| Chamadas de agente sem guard | Não havia chamadas de agente |
| Crash dumps | Não existe pasta `crashes`; usei os `rgl_log` de `C:\ProgramData\Mount and Blade II Bannerlord\logs` |

---

## (b) O que a nova versão faz — e o que deliberadamente NÃO faz

### Faz

1. **Liga todas as 82 actions a clipes reais**, mesclando em `as_human_warrior`. 24 clipes são do próprio módulo
   (`.tpac`); 23 são animações vanilla usadas como fallback onde não existe clipe dual (ex.: não há clipe dual para
   `ready_slashright`, nem versões `_left_stance` dos ataques). **Nenhuma action pode mais resolver para -1.**
2. **Valida em runtime, uma vez por missão.** No `OnBehaviorInitialize`: resolve os 82 `ActionIndexCache`
   (`ActionIndexCache.Create`, checando `Index >= 0` e `!= act_none.Index`) e depois confere cada um contra o
   action set com `MBActionSet.CheckActionAnimationClipExists(set, in action)` — API pública, verificada por
   reflexão na 1.4.8. Se **qualquer** um falhar, o sistema é **desligado** com uma linha de log que nomeia a action
   exata que falta, e o jogo segue vivo sem dual wield.
3. **Rede de segurança por agente.** No `OnAgentBuild`: se o agente carrega a arma de offhand (`item_usage="dual_shield"`)
   mas o action set *dele* não tem os clipes, a arma de offhand é **removida** (`Agent.RemoveEquippedWeapon`).
   Isso importa de verdade no RF: **`RF_Races` define ~200 action sets raciais próprios**
   (`as_elvean_warrior`, `as_urukhai_warrior`, `as_dwarf_female_warrior`, ...) que **não** herdam `as_human_warrior`
   e portanto **não** recebem nossos bindings. Sem esse guard, um elfo ou orc com as duas armas reproduziria o CTD.
   Com ele: raça não-humana luta normal, sem dual wield, sem crash.
4. **Mantém o patch de colisão** — é o que faz o golpe da mão esquerda registrar. O golpe da offhand colide no osso
   `ItemL` (20) enquanto a arma empunhada está anexada em `ItemR` (27); a engine descarta como colisão inválida.
   O postfix diz "não é diferente" só para esse par exato. Agora com dois guards a mais: só age se `__result` era
   `true` e só se a validação da missão passou.
5. **Todo acesso a agente com guard**: `agent != null`, `IsHuman`, `!IsMount`, `IsActive()`, `Equipment != null`,
   `MissionWeapon.IsEmpty`, `CurrentUsageItem != null`. Cache de veredito por action set (chave = nome do set,
   porque `MBActionSet.Index` é `internal`), para não validar 82 actions por agente.
6. **Harmony contido**: `PatchAll` dentro de `try/catch`. Se um alvo mudar numa atualização do jogo, o módulo loga e
   segue carregado em modo apenas-segurança, em vez de lançar dentro de `OnSubModuleLoad` e derrubar o startup.

### NÃO faz (por decisão, não por esquecimento)

- Nenhuma hotkey. Nenhum cheat. Nenhum `InformationManager.DisplayMessage` — todo log vai para o `rgl_log` via
  `TaleWorlds.Library.Debug.Print` com prefixo `[RF_DualWield]`.
- Nenhum `ExtraCowardComponent`, nenhum `AgentComponent` customizado, nenhum patch em `Mission.RegisterBlow`,
  nenhum knockdown de 25%, nada que toque dano, moral ou cálculo de golpe global (o RBM está instalado; ficar longe
  de `RegisterBlow` é obrigatório).
- **Coice de cavalo com AoE: ficou de fora.** A auditoria citou como ideia boa, mas na 1.4.8 exigiria
  `AgentComponent.OnHit` com 5 parâmetros, `Blow.GlobalPosition`, varredura de agentes por raio e
  `BlowFlags.KnockDown` — isto é, superfície de risco no hot path de golpes, justamente onde o RBM atua. Não vale
  acoplar isso à correção de um CTD. Anotado como trabalho futuro separado.
- **Nenhum action set racial foi gerado.** Cobrir as ~200 sets do `RF_Races` significaria ~16.000 linhas de XML
  (200 × 82) e custo de load. A rede de segurança do item 3 cobre o caso com falha limpa. Se o autor quiser dual
  wield para elfos/orcs depois, o `scripts/gen_action_sets.ps1` já tem o mapa pronto — basta repetir o bloco
  `<action_set>` para cada id racial (o merge por id funciona igual, e **não precisa tocar no `RF_Races`**).
- Nenhum módulo novo, nenhum id novo de módulo, mesmo `.csproj`.

---

## (c) Mapa dos arquivos

### C# — 100% novo (`RF_Warsails_AI/RF_DualWield/`)

| Arquivo | Papel |
|---|---|
| `SubModule.cs` | `MBSubModuleBase`. `PatchAll` em `try/catch` + registra o behavior via `OnMissionBehaviorInitialize(Mission)` |
| `DualWieldMissionBehavior.cs` | `MissionBehavior` (`MissionBehaviorType.Other`). Validação na inicialização da missão + guard por agente em `OnAgentBuild` + limpeza em `OnEndMission` |
| `DualWieldActions.cs` | Resolve os `ActionIndexCache` (struct, sempre por `in`) e valida contra `MBActionSet`. Cache de veredito por action set |
| `DualWieldActionTable.g.cs` | **Gerado.** Os 82 nomes de action. Sai do mesmo script que gera o XML, então C# e XML não podem divergir |
| `DualWieldCollisionPatch.cs` | Reescrito: o único patch Harmony, agora com guard de `__result` e de saúde do sistema |
| `DualWieldLog.cs` | Log único para o `rgl_log`. Sem UI |
| `scripts/gen_action_sets.ps1` | **Novo.** Gera `action_sets.xml` + `DualWieldActionTable.g.cs` a partir de `action_types.xml`. Aborta se alguma action ficar sem mapeamento, se algum mapeamento sobrar sem action, ou se algum clipe `dual` não tiver `.tpac` |
| `_old_gpt_backup/` | Fontes e XMLs antigos, **excluídos do build** (`<Compile Remove="_old_gpt_backup\**\*.cs" />`) |

`_old_gpt_backup/` contém: `SubModule.cs`, `DualWieldCollisionPatch.cs`,
`action_sets.xml.deployed_before_rewrite`, `combat_parameters.xml.before_rewrite`.

### `.csproj`

- `+ TaleWorlds.Library` (para `Debug.Print`) e `+ TaleWorlds.DotNet` (as sobrecargas de `Debug` tocam `DotNetObject`).
- `+ <Compile Remove="_old_gpt_backup\**\*.cs" />`.
- PostBuild de deploy **inalterado** — é ele que copia DLL + `_Module\**` para a pasta do jogo.

---

## (d) O que mudou nos XMLs

| Arquivo | Mudança |
|---|---|
| `action_sets.xml` | **Reescrito.** De 1 action set âncora vazio → 82 bindings mesclados em `as_human_warrior`. É **a** correção do CTD. Gerado pelo script |
| `project.mbproj` | **+ `<file id="soln_action_sets" ... />`** (depois de `soln_action_types`). Sem isso o `action_sets.xml` não é lido, e o deploy apagava a linha que só existia na pasta do jogo |
| `combat_parameters.xml` | **Bloco `<definitions>` removido inteiro** (23 `def` de nome vanilla, valores idênticos ao Native). Os valores foram inlinados como literais nos nossos 4 `combat_parameter`. Agora o arquivo define **só os nossos 4 ids** — `1h_dual_others`, `onehanded_dual_thrust`, `onehanded_dual_right`, `onehanded_dual_right_balanced`. Mesmo padrão que o `RF_Extension` usa. Zero colisão de id, zero sobreposição global, e **o RF_Extension não foi tocado** |
| `action_types.xml` | **Inalterado** (os 82 ids não colidem com nada ativo; renomear com prefixo `rf_dw_` só quebraria os `.tpac` e os `item_usage_sets` sem ganho) |
| `item_usage_sets.xml` | **Inalterado.** Conferido contra o original do `UniqueCombat`: é um subconjunto fiel e correto (a usage de thrust + 10 guards). Só o `item_usage_set` extra `drunk` foi (corretamente) deixado de fora |
| `movement_sets.xml`, `full_movement_sets.xml`, `item_holsters.xml`, `rf_dual_wield_items.xml`, `Assets/*.tpac` | **Inalterados**. Os 8 movement sets referenciados pelo full set existem; o holster `dual_back` herda de `thorax_back_near_left`, que existe no Native |

Sobrou na pasta do jogo um `action_sets.xml.pre_actionset_fix_20260805.bak` (16 bytes) — não está no `project.mbproj`,
logo o engine nunca o abre. Inofensivo; deixei porque a pasta do jogo é read-only fora do deploy.

### Verificações automáticas que passaram

```
action_types declaradas: 82
todas as action_types tem binding: OK (82/82)
clipes usados: proprios=24 / vanilla=23
clipes .tpac nao usados: nenhum
refs a clipe 'dual' sem .tpac correspondente: 0
XML bem-formado: 8/8 arquivos
combat_parameters: 0 <def>  (antes: 23)
project.mbproj deployado contem soln_action_sets: sim
```

---

## (e) Build, deploy e roteiro de teste

### Build

```
$env:BANNERLORD_GAME_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
cd C:\Users\gupol\Documents\Bannerlord_Warsails_AI\RF_Warsails_AI\RF_DualWield
dotnet build RF_DualWield.csproj -c Debug
```

Resultado: **Build succeeded — 0 Warning(s), 0 Error(s)**.
(O `RF_DualWield` não está no `RealmsForgotten.sln`; é projeto standalone, como já era.)

### Deploy (feito pelo PostBuild do próprio csproj)

```
Modules\RF_DualWield\bin\Win64_Shipping_Client\RF_DualWield.dll   18.944 bytes, 17:54
Modules\RF_DualWield\ModuleData\action_sets.xml                   82 bindings
Modules\RF_DualWield\ModuleData\project.mbproj                    com soln_action_sets
Modules\RF_DualWield\ModuleData\combat_parameters.xml             só os 4 ids nossos
```

### Roteiro de teste in-game — nesta ordem

O log é a fonte de verdade. Depois de cada teste, abrir o `rgl_log` mais novo em
`C:\ProgramData\Mount and Blade II Bannerlord\logs` e procurar por `[RF_DualWield]`.

**Sinal de sucesso (deve aparecer no início de toda missão):**
```
[RF_DualWield] validacao OK (82 actions com clipe em 'as_human_warrior'). Dual wield ativo.
```
**Sinal de falha controlada (o jogo continua, o dual wield desliga):**
```
[RF_DualWield] AVISO: dual wield DESLIGADO nesta missao: ... nao tem clipe de animacao para 'act_...'
```
**Sinal de que a correção não pegou:** qualquer linha `as_human_warrior does not contain act_dual_...`
seguida de `... -1 ...`. Se isso aparecer, o `action_sets.xml` não foi carregado — checar o `project.mbproj`
do módulo instalado.

| # | Cenário | O que fazer | O que observar |
|---|---|---|---|
| 1 | **Custom battle, personagem HUMANO** — é o cenário que crashava | Custom battle, tropas humanas, equipar `Dual Blade - Main Hand` num slot de arma e `Dual Blade - Off Hand` em outro. Entrar na batalha e deixar o deploy terminar | Não cair. É o teste que reproduz o log de 15:44 e 16:59. Confirmar a linha `validacao OK` |
| 2 | **Ataque com a mão esquerda** | Ainda no custom battle: atacar com thrust (ataque para baixo) contra um inimigo | O golpe deve **causar dano** (é o que o patch de colisão habilita). Animação de thrust dual deve tocar. Sem T-pose |
| 3 | **Locomoção** | Andar, correr, agachar, andar em stance esquerdo, com as duas armas empunhadas | Animações `dual_stand_1h` / `dual_walk_forward_1h` / `dual_run_forward_1h_with_hand_shield`. Agachado usa animação de escudo vanilla (não há clipe dual de crouch) — esperado, não é bug |
| 4 | **MONTADO** | Montar a cavalo com as duas armas. Atacar montado | Não cair. Não existe clipe dual montado; a engine deve cair nas usages herdadas de `onehanded_shield_swing`. Se aparecer T-pose montado, me reporte — a correção é adicionar um `is_mounted="True"` no `item_usage_sets.xml` |
| 5 | **Raça NÃO humana** (elfo, orc, anão...) | Criar/usar personagem de raça do `RF_Races` e equipar as duas armas em batalha | **Não cair.** Esperado: a arma de offhand desaparece do agente e aparece no log `AVISO: action set 'as_elvean_warrior' nao tem clipe para 'act_dual_...'. Removendo a arma de offhand`. Isso é o comportamento correto, não uma falha |
| 6 | **Taverna / cena de settlement** | Entrar numa taverna e num vilarejo com as duas armas equipadas | Missão sem combate: a validação roda igual, o guard roda igual. Não cair, sem NPC em T-pose |
| 7 | **Batalha de campanha grande** | Batalha normal de campanha, 200v200, com o jogador dual wielding | Estabilidade e FPS. O guard só roda uma vez por agente no build; o cache de action set evita revalidar |
| 8 | **Batalha naval (War Sails)** | Combate de navio com as duas armas | O módulo não toca em nada naval, mas o patch de colisão é global — confirmar que golpe normal de navio segue registrando |
| 9 | **Sem as armas** | Uma batalha inteira sem tocar nos itens dual | Nada de anormal. `validacao OK` no log e mais nada |

### Risco latente encontrado fora do escopo (NÃO corrigi — é do Anian)

`RealmsForgotten/ModuleData/action_types.xml` declara `act_human_blow_horn`. O binding existe em
`RealmsForgotten/ModuleData/action_sets.xml`, **mas aquele arquivo nunca é carregado** porque o
`project.mbproj` do RealmsForgotten não declara `soln_action_sets` (confirmado no log: RealmsForgotten não
aparece na lista de `action_sets.xml` abertos). E o Native não tem binding para essa action (grep = 0).
É **exatamente a mesma classe de bug** que causava o CTD do dual wield: se algum C# do RF chamar
`SetActionChannel` com `act_human_blow_horn`, cai do mesmo jeito. Não apareceu nos logs analisados, então ou
ninguém chama, ou nunca foi acionado. Precisa de decisão do autor antes de mexer.

---

# Rodada 2 — 2026-08-10, depois do 1º teste in-game

Log do teste: `C:\ProgramData\Mount and Blade II Bannerlord\logs\rgl_log_8884.txt` (18:15 → 18:37, build 119303).

## O que o teste provou

**O CTD morreu.** O jogo rodou 22 minutos, `rgl_log_errors_8884.txt` limpo, missão `Battle` aberta às 18:36:30 com
deploy de formações completo — exatamente o ponto onde os logs de 15:44 e 16:59 morriam. Todos os XMLs carregaram
(`action_types` 3619, `combat_parameters` 3974, `action_sets` 5770, `movement/item_usage/holsters` 9142-9152,
`rf_dual_wield_items` 9705). A correção da Fase A está confirmada.

Sobraram dois sintomas, e eles têm **uma única causa** — e a causa era **minha**, introduzida na rodada 1.

## A causa raiz: registrei o MissionBehavior no hook errado

`Mission.AfterStart()` no vanilla decompilado (`Vanilla_1.3.x/MEGA_015.md:94141-94158`):

```csharp
foreach (MBSubModuleBase s in _cachedSubModuleList) s.OnBeforeMissionBehaviorInitialize(this);   // 1
for (int i = 0; i < MissionBehaviors.Count; i++) MissionBehaviors[i].OnBehaviorInitialize();     // 2
foreach (MBSubModuleBase s in _cachedSubModuleList) s.OnMissionBehaviorInitialize(this);         // 3
foreach (MissionBehavior b in MissionBehaviors) b.EarlyStart();                                  // 4
```

Eu adicionava o behavior no **passo 3**. O loop de `OnBehaviorInitialize()` já tinha rodado no **passo 2**. Ou seja:
**o meu `OnBehaviorInitialize` nunca foi chamado.**

Consequência em cascata:

1. **A linha `validacao OK` nunca saiu** — porque o método que a emite nunca rodou. (Que `Debug.Print` chega ao
   `rgl_log` está provado: `RF_Magic/SOTOR/SotorLog.cs:78` usa exatamente `TaleWorlds.Library.Debug.Print` e as
   linhas `[SOTOR]` aparecem no log. Ausência da minha linha = código não executado, não canal errado.)
2. **`IsSystemHealthy` ficou `false`** (valor padrão de `bool`).
3. **`OnAgentBuild` rodou normalmente** (behaviors adicionados no passo 3 continuam na lista e recebem os hooks
   seguintes — o `[SOTOR] AbilityManager EarlyStart` às 18:36:33 prova isso). E o meu código da rodada 1 era:

```csharp
if (IsSystemHealthy)          // false ->  pula o bloco inteiro
{
    if (supports) return;
    if (!_loggedStrip) { log... }
}
agent.RemoveEquippedWeapon(offhandSlot);   // executa SEM log nenhum
```

Com `IsSystemHealthy == false` o fluxo caía direto no `RemoveEquippedWeapon` **sem logar nada**. A minha própria
rede de segurança arrancou a arma de offhand de todo agente, silenciosamente.

**É a explicação completa dos dois sintomas com uma causa só**: a offhand foi corretamente para o
`BattleEquipment` (log 18:35:50: `TransferItem Name: Dual Blade - Off Hand | From: OtherInventory To: BattleEquipment`),
o agente nasceu com ela, e eu a removi antes de o jogador ver.

## Sobre o wield (item B da checagem): o vanilla já faz, e eu estava atrapalhando

Não precisa de código para empunhar. `Equipment.GetInitialWeaponIndicesToEquip`
(`Vanilla_1.3.x/MEGA_011.md:15061`) é puramente data-driven:

```csharp
else if (offHandWeaponIndex == EquipmentIndex.None && item.ItemFlags.HasAnyFlag(ItemFlags.HeldInOffHand))
{
    offHandWeaponIndex = equipmentIndex;
}
```

e `Agent.WieldInitialWeapons` (`MEGA_015.md:30946`) chama `TryToWieldWeaponInSlot(offHandWeaponIndex, ...)`.
O nosso `rf_dual_blade_offhand_test` **já tem** `HeldInOffHand="true"` em `<Flags>` — e confirmei que é
`ItemFlags` (não `WeaponFlags`) e que os escudos vanilla usam exatamente o mesmo trio
`WoodenParry` / `ForceAttachOffHandPrimaryItemBone` / `HeldInOffHand` (`Native/ModuleData/mpitems.xml`, 53 itens).
**O item XML está correto — item C da checagem: nada a corrigir.** `Type="OneHandedWeapon"` não interfere:
a seleção de offhand olha só a ItemFlag.

E descobri um segundo erro de ordem meu, em `Mission.SpawnTroop` (`MEGA_015.md:94787-94799`):

```
BuildAgent(agent) -> EquipItemsFromSpawnEquipment -> OnAgentBuild(agent)   [nós]
... e SÓ DEPOIS ...  agent.WieldInitialWeapons()
```

Em `OnAgentBuild` **o wield ainda não aconteceu**. Então mexer no equipamento ali sabota o wield que vem depois —
e *forçar* o wield ali seria sobrescrito logo em seguida. `OnAgentBuild` é o lugar certo para **decidir**, e o lugar
errado para **agir sobre o wield**.

## O que mudou no código

| Arquivo | Mudança |
|---|---|
| `SubModule.cs` | Registro movido de `OnMissionBehaviorInitialize` → **`OnBeforeMissionBehaviorInitialize`** (passo 1), com guarda anti-duplicata via `mission.GetMissionBehavior<DualWieldMissionBehavior>()`. Agora `OnBehaviorInitialize` é chamado |
| `DualWieldMissionBehavior.cs` | **Validação preguiçosa e idempotente** (`EnsureValidated()`), chamada em `OnBehaviorInitialize`, em `EarlyStart` **e** no primeiro `OnAgentBuild`. Não depende mais de ordem de hook nenhuma |
| `DualWieldMissionBehavior.cs` | **Nunca mais remove em silêncio.** O caminho `IsSystemHealthy == false` agora tem log próprio antes do `RemoveEquippedWeapon` |
| `DualWieldMissionBehavior.cs` | Quando o action set do agente **passa** a validação, `OnAgentBuild` não toca em equipamento: só enfileira o agente em `_pendingWieldCheck` |
| `DualWieldMissionBehavior.cs` | **Novo `OnMissionTick`**: um tick depois do build (já depois do `WieldInitialWeapons` do vanilla), se a offhand está no slot mas `GetOffhandWieldedItemIndex()` não aponta para ele, força `TryToWieldWeaponInSlot(slot, Instant, false)` e loga o resultado. É no-op se o caminho de dados funcionou — e diz ao autor qual caminho valeu |
| `DualWieldMissionBehavior.cs` | `OnAgentDeleted` limpa a lista de pendentes; `OnEndMission` reseta tudo |

Assinaturas conferidas por reflexão nos assemblies reais da 1.4.8: `MBSubModuleBase.OnBeforeMissionBehaviorInitialize`
(public virtual), `Mission.GetMissionBehavior<T>()`, `MissionBehavior.OnMissionTick(float)` / `EarlyStart()` /
`OnAgentDeleted(Agent)` (public), `OnEndMission()` (protected), `Agent.TryToWieldWeaponInSlot(EquipmentIndex,
Agent.WeaponWieldActionType, bool)`, `Agent.GetOffhandWieldedItemIndex()`.

**Nenhum XML mudou nesta rodada.** Nenhum outro módulo foi tocado.

Build: **0 erros, 0 warnings** (rebuild limpo). Deploy: `RF_DualWield.dll` 20.480 bytes, 18:47.

## O que o autor testa agora

### ONDE conferir a linha de validação

Arquivo: `C:\ProgramData\Mount and Blade II Bannerlord\logs\rgl_log_<pid>.txt` — **o mais recente por data**.
Procure por `DualWield`. A linha sai **no começo de cada missão**, junto das linhas
`----------Mission-AddTeam-...` / `[SOTOR] Mission behaviors init.` (foi ali, às 18:36:33, que ela deveria ter
aparecido no teste anterior e não apareceu):

```
[RF_DualWield] validacao OK (82 actions com clipe em 'as_human_warrior'). Dual wield ativo.
```

**Se essa linha aparecer, o item A está resolvido.** Se não aparecer, pare e me mande o log — o behavior não
está sendo registrado, e não vale testar mais nada.

### Cenários, em ordem

| # | Cenário | O que observar |
|---|---|---|
| 1 | **Repetir o teste de ontem**: campanha, inventário, `Dual Blade - Main Hand` + `Dual Blade - Off Hand` no BattleEquipment, entrar numa batalha | (a) linha `validacao OK` no log; (b) **a lâmina da mão esquerda aparece na mão**. Este é o teste que falhou |
| 2 | **Qual caminho empunhou** | Se aparecer `[RF_DualWield] wield da offhand forcado por codigo ...`, o caminho de dados do vanilla não bastou e o fallback entrou. Se **não** aparecer nada além do `validacao OK`, foi o `HeldInOffHand` do XML — o ideal. Se aparecer `... AINDA NAO empunhada`, me mande o log: aí o problema é no `item_usage`/holster do item |
| 3 | **Golpe com a esquerda** | Atacar com thrust (ataque para baixo). Deve **causar dano** e tocar animação dual, sem T-pose |
| 4 | **Locomoção** | Andar/correr/stance esquerdo com as duas empunhadas. Agachado cai em animação de escudo vanilla — esperado |
| 5 | **Montado** | Montar e atacar. Não cair. Sem clipe dual montado, deve usar as usages herdadas de `onehanded_shield_swing` |
| 6 | **Raça não-humana** (elfo/orc/anão) | **Não cair.** Esperado no log: `[RF_DualWield] AVISO: action set 'as_elvean_warrior' nao tem clipe para 'act_dual_...'. Removendo a arma de offhand`. A arma de offhand desaparecer AQUI é o comportamento correto — e agora vem com log, ao contrário de ontem |
| 7 | **Taverna / vilarejo** | Entrar com as duas equipadas. `validacao OK` no log, ninguém em T-pose |
| 8 | **Batalha grande + naval (War Sails)** | Estabilidade; golpe normal de navio continua registrando |

### Sinais de alarme

- `as_human_warrior does not contain act_dual_...` seguido de ` -1 ` → o `action_sets.xml` não carregou; checar
  `soln_action_sets` no `project.mbproj` do módulo instalado.
- Nenhuma linha `[RF_DualWield]` → o behavior não foi registrado; não é problema de canal de log (SOTOR usa o mesmo
  `Debug.Print` e aparece).

---

## Rodada 2b — auditoria do código antigo (a pedido do autor)

O autor afirmou: *"no código antigo havia sim tudo o que se necessitava para usar a espada na segunda mão"*, o que
parecia contradizer o meu inventário de "35 linhas em 2 arquivos". Fui atrás. **O autor está certo, e o inventário
também estava — eu é que descrevi mal.** Fecho a divergência aqui.

### Onde procurei

| Local | Resultado |
|---|---|
| `RF_DualWield/` (projeto inteiro, incluindo `obj/`) | 2 `.cs` antes da reescrita: `SubModule.cs` (345 B) e `DualWieldCollisionPatch.cs` (666 B) |
| `RF_DualWield/_old_gpt_backup/` | os mesmos 2 |
| **`RealmsForgottenMain/DeployBackups/DualWieldOptionalAddon_20260803_214237/RealmsSource/`** | **`DualWieldCollisionPatch.cs` (863 B)** — a versão de antes da isolação de 03/08, quando o dual wield vivia dentro do módulo `RealmsForgotten` (namespace `RealmsForgotten.DualWield`) |
| `DeployBackups/DualWieldIsolation_*` e `DualWield_*` | nenhum `.cs`; só `.tpac`, `ModuleData` e `RealmsForgotten.csproj` |
| Solution inteira (`grep -r` por `dual_shield`, `DualWield`, `HeldInOffHand`, `TryToWieldWeaponInSlot`, `GetOffhandWieldedItemIndex` em `*.cs`) | fora do `RF_DualWield`, os únicos matches são de "wield" genérico (tocha de assentamento, tocador de corno, treino de homestead, magia SOTOR). **Nenhum toca dual wield** — confirmado com grep específico por `dual_shield`/`HeldInOffHand`, resultado vazio |
| DLL antiga compilada | **não sobrou nenhuma cópia.** As 4 cópias (`bin/Debug/net472`, `obj/Debug/net472`, `_Module/bin/Win64_Shipping_Client`, e a do jogo) foram todas sobrescritas pelo build de hoje 17:54. Nenhum `.bak`/`.old` nos DeployBackups. Não deu para decompilar e comparar |

### O que de fato existia

Diff da versão pré-isolação contra a que estava no módulo: **só o namespace e um comentário XML.** Zero diferença
de lógica.

```
5c5
< namespace RealmsForgotten.DualWield
---
> namespace RF_DualWield
7,10d6
<     /// <summary>
<     /// Lets a dual-wield animation use the left-hand weapon bone without the
<     /// engine downgrading the strike to an unarmed/blunt collision.
<     /// </summary>
```

Arquivado em `RF_DualWield/_old_gpt_backup/DualWieldCollisionPatch.cs.pre_isolation_RealmsForgotten_20260803`.

**Conclusão: nunca existiu código de wield.** Nem `TryToWieldWeaponInSlot`, nem `SetWieldedItemIndex`, nem
`OnAgentBuild`, nem `AgentBuildData`. O total histórico de C# do dual wield é: `PatchAll` + o postfix de colisão.

### Por que o autor está certo mesmo assim

Porque **o wield nunca precisou de código** — era o item C da checagem, e a resposta é "dados". O
`rf_dual_wield_items.xml` traz `HeldInOffHand="true"` na offhand blade, e o vanilla faz o resto sozinho
(`Equipment.GetInitialWeaponIndicesToEquip` → `Agent.WieldInitialWeapons` → `TryToWieldWeaponInSlot`, citados na
seção anterior). E o `rf_dual_wield_items.xml` de hoje é **byte-idêntico** ao de 03/08 (`diff` limpo contra as duas
cópias do backup, `RealmsSource` e `RealmsRuntime`).

Então "tudo o que se necessitava" existia de fato, e continua existindo — só não era C#:

| Peça | Onde vive | O que faz |
|---|---|---|
| `HeldInOffHand` | `rf_dual_wield_items.xml` `<Flags>` | manda o vanilla escolher a lâmina para a mão esquerda |
| `dual_shield` (base `hand_shield`) | `item_usage_sets.xml` | faz a lâmina de offhand ser tratada como item de mão esquerda |
| `dual_shield_thrust` + `require_left_hand_usage_root_set` | `item_usage_sets.xml` | libera o ataque dual quando a esquerda está ocupada |
| `1h_with_dual_shield` | `movement_sets` / `full_movement_sets` | locomoção com as duas lâminas |
| postfix `IsCollisionBoneDifferentThanWeaponAttachBone` | **o único C#** | faz o golpe da esquerda registrar dano |

**Nada a portar: a lógica antiga está 100% preservada na versão nova.** O postfix é o mesmo (mais dois guards), e
o resto sempre foi dados que eu não alterei. O que quebrou o wield na rodada 1 foi exclusivamente o
`RemoveEquippedWeapon` que eu introduzi — e que a rodada 2 já corrigiu.

Correção da minha descrição original: chamar aquelas 35 linhas de "inofensivas" foi impreciso. Elas eram
**suficientes** — o postfix é indispensável, e o peso do sistema está nos assets. O que eu afirmei corretamente é
que aquele C# não podia causar o CTD; isso segue verdade (o CTD era o `action_sets.xml` vazio).

### Descoberta lateral, e ela explica o `act_human_blow_horn`

O backup `DualWield_20260803_204346/ModuleData/project.mbproj` (o `project.mbproj` do **RealmsForgotten** de antes da
isolação) **tinha** a linha:

```xml
<file id="soln_action_sets" name="ModuleData/action_sets.xml" type="action_set" />
```

O `project.mbproj` do RealmsForgotten **hoje não tem**. Ou seja: a isolação de 03/08 tirou os XMLs do dual wield do
RealmsForgotten e, no caminho, aparou o `mbproj` dele — perdendo o registro de `action_sets.xml`. É exatamente por
isso que o binding de `act_human_blow_horn` (que está lá, no `action_sets.xml` de 209 bytes do RealmsForgotten) nunca
mais é carregado. Confirma o risco latente que já estava anotado no fim deste documento. **Continuo sem tocar** —
é módulo do Anian.

E note: aquele mesmo backup mostra que o `action_sets.xml` do RealmsForgotten **nunca teve os bindings do dual
wield** — 209 bytes, uma única action (`act_human_blow_horn`). Ou seja, os 82 bindings de animação nunca existiram
em nenhuma época do projeto. A tabela nova é a primeira vez que eles existem.

### ⚠️ Achado de infraestrutura, fora do escopo mas urgente

`df` do disco C: durante esta investigação:

```
Filesystem      Size  Used Avail Use% Mounted on
C:              953G  953G     0 100% /c
```

**Disco 100% cheio, 0 bytes livres.** Um `ls` chegou a falhar com `write error: No space left on device`. O build e
o deploy de hoje passaram (DLL de 20.480 bytes gravada às 18:47), mas nesse estado o jogo pode falhar ao gravar
save, log ou shader cache, e qualquer build futuro pode quebrar de formas confusas. Vale liberar espaço antes do
próximo teste — os `DeployBackups` do RF, cheios de `.tpac` duplicados, são candidatos óbvios.

---

# Rodada 3 — 2026-08-10, depois do 2º teste in-game (`rgl_log_23844.txt`)

## O que o 2º teste provou

A correção da rodada 2 funcionou **exatamente como desenhada**. A validação rodou, no hook certo, e logou:

```
7373: [19:11:40.000] [RF_DualWield] AVISO: dual wield DESLIGADO nesta missao: 'as_human_warrior' nao tem
      clipe de animacao para 'act_dual_ready_thrust_1h'. ...
7549: (strip da offhand por seguranca)
```

Zero crash, guard funcionando, ordem de hook resolvida. O que restou foi o problema real, agora isolado.

## A hipótese do coordenador estava errada — e eu tenho a prova

A suspeita era: *"ids duplicados de `action_set` são ignorados pelo loader; o primeiro vence; por isso nada entra"*.
**Não é isso.** Merge por id funciona, e funciona por design documentado no próprio jogo.

### 1. Como o engine realmente mescla `action_sets` de vários módulos

`action_sets.xml` é XML **nativo** (declarado em `project.mbproj`, não em `SubModule.xml`), então passa por
`XmlResource.GetMergedXmlForNative` → `CreateMergedXmlFile` → `MergeTwoXmls`
(`Vanilla_1.3.x/MEGA_016.md:4614-4700`):

```csharp
public static XmlDocument CreateMergedXmlFile(List<Tuple<string,string>> toBeMerged, List<string> xsltList, bool skipValidation)
{
    XmlDocument xmlDocument = CreateDocumentFromXmlFile(toBeMerged[0].Item1, toBeMerged[0].Item2, skipValidation);
    for (int i = 1; i < toBeMerged.Count; i++)
    {
        if (xsltList[i] != "")            // XSLT do módulo i, aplicado ao doc ACUMULADO
            xmlDocument = ApplyXslt(xsltList[i], xmlDocument);
        if (toBeMerged[i].Item1 != "")
            xmlDocument = MergeTwoXmls(xmlDocument, xmlDocument2, toBeMerged[i].Item2, keepDuplicates: false);
    }
    return xmlDocument;
}
```

E o `MergeTwoXmls`/`MergeElements` (mesmo arquivo, `:4514-4570`) decide por **XSD**:

- se `xsdPath == ""` → `xDocument.Root.Add(xDocument2.Root.Elements())`, isto é, **append cego, duplicatas mantidas**;
- se existe XSD → agrupa por nome de elemento e casa pelos `UniqueAttributes` do schema; **se a chave casa, MESCLA;
  se não casa, adiciona**.

O XSD existe: `$BASE/XmlSchemas/soln_action_sets.xsd` declara
`<xs:unique name="action_set_unique_attribute"><xs:selector xpath="action_set"/><xs:field xpath="@id"/></xs:unique>`
e, dentro de `action_set`, `action` é única por `@type`.

**Resposta à pergunta 1: MESCLA.** Dois módulos com `<action_set id="as_human_warrior">` viram um só, e as `<action>`
se juntam pelo `@type` (mesmo `@type` = o último sobrescreve; `@type` novo = entra). Não é ignorado, não é substituído.

### 2. O RF_Magic realmente mescla em id vanilla — a analogia era válida

`Modules/RF_Magic/ModuleData/project.mbproj` declara
`<file id="soln_action_sets" name="ModuleData/sotor_action_sets.xml" type="action_set" />`, e o arquivo inteiro é:

```xml
<action_sets>
    <action_set id="as_human_warrior" skeleton="human_skeleton" movement_system="bipedal">
        <action type="act_spellcasting_idle" animation="spellcasting_stance_idle" />
        <action type="act_raisefromground" animation="spellcasting_raisefromground" />
    </action_set>
</action_sets>
```

Sem XSLT, sem `base_set`, sem action set novo: **merge puro por id em `as_human_warrior` vanilla**, e a magia
funciona in-game. A "prova" citada na Fase A não era falsa analogia.

### 3. E o RF_DualWield tem AINDA um segundo caminho, que eu testei fora do jogo

O módulo também traz `ModuleData/action_sets.xslt`, com o predicado
`action_set[not(@base_set) and @skeleton='human_skeleton' and @movement_system='bipedal' and action[@type='act_troop_cavalry_sword']]`.
Rodei a transformação de verdade (`XslCompiledTransform`, o mesmo tipo que o `ApplyXslt` usa) contra o
`action_sets.xml` do Native:

```
as_human_warrior action count BEFORE xslt: 4699
as_human_warrior action count AFTER  xslt: 4781      (+82, exatamente os nossos)
  FOUND act_dual_ready_thrust_1h            -> animation=ready_dual_thrust_1h
  FOUND act_dual_quick_release_thrust_1h    -> animation=quick_release_dual_thrust_1h
  FOUND act_run_forward_1h_with_d_shld      -> animation=dual_run_forward_1h_with_hand_shield
action_sets que receberam os bindings: 1  -> as_human_warrior     (nenhum outro set foi tocado)
total de action_sets: 102 antes, 102 depois  (nada duplicado, nada perdido)
```

Ou seja: **o binding entrava**. Por dois caminhos independentes. O XML nunca foi o problema.

## A causa raiz: os assets de animação nunca foram carregados

`MBActionSet.CheckActionAnimationClipExists` devolve `false` por **duas** razões diferentes, e o meu log não as
separava: (A) não existe binding, ou (B) existe binding mas o **clipe** não está carregado. Era (B).

### A evidência no próprio log do autor

O engine loga uma linha por módulo ao carregar pacotes de asset. No `rgl_log_23844.txt`, as **12** linhas são:

```
151: Bannerlord.MBOptionScreen/AssetPackages     269: RF_Races/AssetPackages
154: Native/AssetPackages                        272: RealmsForgotten/Assets
243: SandBox/AssetPackages                       278: RF_Core_II/AssetPackages
246: NavalDLC/AssetPackages                      287: RF_Core_III/Assets
264: RF_Map/Assets                               329: RF_Extension/AssetPackages
                                                 349: Bannerlord.CCsBanners/AssetPackages
                                                 490: RF_Magic/Assets
```

**`RF_DualWield` não aparece.** Nem `Assets`, nem `AssetPackages`, nem nada — apesar de ter 28 `.tpac` em
`Assets/`. Os únicos outros módulos ativos ausentes dessa lista (Harmony, ButterLib, UIExtenderEx, SandBoxCore,
CustomBattle, StoryMode) **não têm pasta de asset nenhuma**. RF_DualWield era o único módulo com `Assets/` que o
engine ignorou por completo.

### Por que: o `.tpac` de animação é só metadata; o payload está no `RuntimeDataCache`

`Assets/ready_dual_thrust_1h_anm.tpac` tem **481 bytes**. As strings dele são só o nome da animação
(`ready_dual_thrust_1h`), o grupo de combate (`1h_dual_others`), as flags (`cyclic`, `enforce_root_rotation`, …) — e
um GUID binário. Nada de keyframe. O payload mora em `RuntimeDataCache/<GUID>.rdc`.

Casei os GUIDs por força bruta (16 bytes, little e big endian) contra o `RuntimeDataCache` do UniqueCombat, de onde
os assets vieram:

```
24 dos 28 .tpac (todos os *_anm.tpac) referenciam 1 .rdc cada — 24/24, sem sobra
fecho transitivo: +0  (nenhum .rdc referencia outro)
tamanhos: 4 KB a 95 KB por arquivo, 513 KB no total  <- keyframes de verdade
os 4 *_geo.tpac não referenciam .rdc (contêm dados inline; clipes 'human_skeleton_notused|...')
```

**`RF_DualWield` não tinha pasta `RuntimeDataCache` nenhuma.** Todo módulo cujos `Assets/` soltos o engine carregou
(RF_Map, RealmsForgotten, RF_Core_III, RF_Magic) tem `RuntimeDataCache`; UniqueCombat e RFMonsters, que também usam
`Assets/` solto sem `AssetPackages`, também têm. Sem os `.rdc`, nenhum clipe de animação existe — e o guard, que
está correto, desligou o dual wield.

Ou seja: **rota (c) da lista do coordenador, mas um nível abaixo**. O mapeamento do `gen_action_sets.ps1` está certo
(`act_dual_ready_thrust_1h` → `ready_dual_thrust_1h`), o nome do clipe dentro do `.tpac` está certo, o arquivo
`.tpac` está no lugar certo — faltava o payload que o `.tpac` aponta.

## O que mudou

1. **`_Module/RuntimeDataCache/` criada com os 24 `.rdc`** exigidos, copiados do `RuntimeDataCache` do UniqueCombat
   (hash conferido, 0 divergências). Só os 24 do fecho transitivo — os outros 13 `.rdc` do UniqueCombat são de
   animações que este módulo não usa (assassin, night king, drunk, mead cup) e não foram trazidos.
2. **`.csproj`**: `_Module\RuntimeDataCache\*.rdc` entrou no `RFModuleAsset`, com comentário explicando que não é
   cache descartável. Sem isso o próximo deploy limpo repetiria o bug.
3. **`.csproj`**: `MakeDir` de uma `AssetPackages` **vazia** no módulo instalado. A evidência não separa se o
   gatilho que o engine usa é a ausência de `RuntimeDataCache` ou de `AssetPackages` (RF_DualWield não tinha as
   duas). Uma `AssetPackages` vazia é comprovadamente inofensiva: o RF_Magic tem a dele vazia e o log mostra
   `Loading packages $BASE/Modules/RF_Magic/Assets...`, então ela **não** desvia o carregamento para si. Cobrir as
   duas hipóteses custa um diretório vazio e economiza um teste inteiro do autor.
4. **`DualWieldActions.Diagnose`** (novo): o aviso agora separa as duas causas, para nunca mais custar um teste.
   - binding ausente → `GetActionAnimationName` volta vazio → problema de XML;
   - clipe ausente → binding aponta um nome mas `GetAnimationIndexOfAction` volta `-1` → problema de asset,
     e o log já diz qual `.tpac`/`.rdc` conferir.

Nenhum XML mudou nesta rodada. Nenhum outro módulo foi alterado (o UniqueCombat foi só **lido**). Build **0 erros /
0 warnings**; DLL deployada com hash idêntico ao build; `Assets` 28 `.tpac`, `RuntimeDataCache` 24 `.rdc`,
`ModuleData` 8 XMLs + 2 XSLTs.

## O que o autor testa agora

Mesmo roteiro: inventário → as duas lâminas no `BattleEquipment` → batalha. No `rgl_log` mais recente:

1. **Deve aparecer** uma linha nova de carregamento de asset, que nunca existiu antes:
   `Loading packages $BASE/Modules/RF_DualWield/Assets...`
2. **Deve aparecer** a validação passando:
   `[RF_DualWield] validacao OK (82 actions com clipe em 'as_human_warrior'). Dual wield ativo.`
3. E a lâmina na mão esquerda, com as animações de dual wield.

Se aparecer também `wield da offhand forcado por codigo`, o fallback entrou — funciona, mas me avise.

**Se ainda falhar**, o log agora diz sozinho para onde ir:
- `[causa: CLIPE AUSENTE ...]` → o asset ainda não carrega; o próximo passo é `RuntimeDataCache` completo (os 37
  `.rdc` do UniqueCombat) e/ou trazer os `*_geo.tpac` que faltam do UniqueCombat.
- `[causa: BINDING AUSENTE ...]` → aí sim seria XML, e o alvo passa a ser a rota (b): action set próprio
  (`as_rf_dual_warrior` com `base_set="as_human_warrior"` — herança que o XSD nem declara mas o Native usa em 6
  lugares) trocado por código no `AgentBuildData`.
- Se o aviso citar um action set **racial** (`as_human_...` de outro nome), é o caso já previsto: os sets do
  RF_Races não herdam `as_human_warrior`.

## Nota sobre a redundância XML + XSLT

O módulo hoje injeta os 82 bindings por **dois** caminhos: o `action_sets.xml` (merge por id) e o
`action_sets.xslt`. Provei que os dois funcionam e que são idempotentes entre si (o XSLT injeta em
`as_human_warrior`, depois o merge por `@type` sobrescreve os mesmos 82 — não duplica). **Deixei os dois**, de
propósito: mexer nisso agora acrescentaria uma variável ao teste que precisa validar o `RuntimeDataCache`. Quando o
dual wield estiver confirmado in-game, o XSLT é o candidato a sair — ele custa uma transformação de um XML de 956 KB
e o predicado é heurístico (casa qualquer `action_set` humano bípede com `act_troop_cavalry_sword`; hoje só
`as_human_warrior`, verificado, mas nada garante isso em versão futura).

## Nota de limpeza

`Modules/RF_DualWield/ModuleData/action_sets.xml.pre_actionset_fix_20260805.bak` está no módulo **instalado** (não
na fonte). Não tem extensão `.xml`, então o engine não o lê e não faz mal — mas é lixo de um deploy antigo.

---

# Rodada 4 — 2026-08-10: o dual wield FUNCIONA in-game

4º teste do autor: **funciona**. A peça que faltava foi encontrada e aplicada pelo coordenador:
**a pasta `AssetSources`**.

## O marcador real é `AssetSources`, não `RuntimeDataCache` nem `AssetPackages`

A rodada 3 acertou metade: sem os `.rdc` do `RuntimeDataCache` não existe payload de animação. Mas
errou o *gatilho* de o engine sequer olhar para `Assets/`. O critério é a presença de
`AssetSources`. Conferi a regra nos 11 módulos relevantes desta instalação, cruzando com a linha
que cada um produz no log:

```
modulo            AssetSources  Assets  AssetPackages(arqs)   o que o log mostra
RF_Map                 sim       sim        sim (5)           .../RF_Map/Assets
RealmsForgotten        sim       sim        sim (5)           .../RealmsForgotten/Assets
RF_Core_III            sim       sim        sim (4)           .../RF_Core_III/Assets
RF_Magic               sim       sim        sim (0)           .../RF_Magic/Assets
UniqueCombat           sim       sim        nao               (inativo)
RFMonsters             sim       sim        nao               (inativo)
RF_Races               nao       nao        sim (1)           .../RF_Races/AssetPackages
RF_Extension           nao       nao        sim (10)          .../RF_Extension/AssetPackages
RF_Core_II             nao       nao        sim (4)           .../RF_Core_II/AssetPackages
Native                 nao       nao        sim (150)         .../Native/AssetPackages
```

A correlação é perfeita: **`AssetSources` presente ⇔ o engine carrega `Assets/`**. E note
RF_Map/RealmsForgotten/RF_Core_III: têm `AssetPackages` **com arquivos** e ainda assim o engine
escolhe `Assets/`. Ou seja, `AssetSources` vence até uma `AssetPackages` populada.

## Correção da minha nota da rodada 3

Na rodada 3 eu criei uma `AssetPackages` **vazia** como "seguro", por não conseguir separar se o
gatilho era `RuntimeDataCache` ou `AssetPackages`. **Era palpite errado**, e a frase "uma
AssetPackages vazia é comprovadamente inofensiva… ela não desvia o carregamento" estava certa por
acidente e pelo motivo errado: ela não desvia porque quem decide é `AssetSources`, não porque
`AssetPackages` seja neutra por natureza. Sem `AssetSources`, uma `AssetPackages` (mesmo vazia) é
exatamente a rota errada em que o módulo caía.

Tirei o `MakeDir` da `AssetPackages` do `.csproj` e troquei o comentário pelo motivo real. O
diretório vazio que já estava no módulo instalado **não pôde ser apagado** (o sandbox bloqueia
remoção dentro de `Program Files`), mas a tabela acima prova que ele é inerte: com `AssetSources`
presente, o engine vai para `Assets/` mesmo diante de uma `AssetPackages` cheia. Num deploy limpo
ele não é mais criado.

Forma final do módulo, idêntica à dos módulos que comprovadamente carregam `Assets/` soltos
(UniqueCombat, RFMonsters): **`Assets` + `AssetSources` + `RuntimeDataCache`**, sem `AssetPackages`.

## Placar das quatro rodadas

| rodada | causa | quem achou |
|---|---|---|
| 1 | CTD por `action_sets` sem binding (índice de animação -1) | reescrita: guard + tabela de 82 bindings |
| 2 | behavior registrado em `OnMissionBehaviorInitialize`, hook tarde demais | rodada 2 |
| 3 | `RuntimeDataCache` ausente: `.tpac` de animação sem payload | rodada 3 |
| 4 | `AssetSources` ausente: engine nem olhava para `Assets/` | coordenador |

---

# Rodada 5 — 2026-08-10: a defesa UP não bloqueava golpe vertical

Único defeito remanescente reportado pelo autor: as defesas down/left/right funcionam, a **up
não** — golpe superior/vertical passava.

## Inventário (o que os dados realmente dizem)

**1. O módulo não tem action de defesa nenhuma.** As 82 actions do `action_types.xml` do módulo se
distribuem em `ud_attack_right` (26), `ud_attack_left` (26), `ud_attack_down` (22) e 8 sem direção.
Zero `ud_defend_*`, zero `act_dual_defend_*`, zero parry próprio. Toda a defesa do dual wield vem
do vanilla — o módulo só declara os `actt_blocked_melee` (as animações de "meu ataque foi
bloqueado", que são do atacante, não do defensor).

**2. Nem o UniqueCombat tinha.** Diff dos `action_types.xml`: o nosso é subconjunto exato do dele
(94 → 82; as 12 de diferença são `assassin`, `coward`, `drunk`/`mead` e dois `act_dual2_*`, nada de
defesa). O `item_usage_sets.xml` do módulo é semanticamente **idêntico** ao do UniqueCombat —
mesmos 10 `guard`, mesma usage de thrust, mesmos `base_set`. Ou seja: **o defeito da defesa up não
é regressão da reescrita, é do desenho original**, e existia no mod de referência.

**3. Onde a defesa up realmente era resolvida.** O set da mão principal, `dual_shield_thrust`,
herda de `onehanded_shield_swing`. Listando as `defend usages` do vanilla:

```
onehanded_shield_swing   defend_up/down/left/right, 6 cada:
    3x require_left_hand_usage_root_set="shield"       -> act_defend_shield_up_1h_*
    3x require_left_hand_usage_root_set="hand_shield"  -> act_defend_hand_shield_up_1h_*
hand_shield (base da nossa offhand)  defend_up/down/left/right, 3 cada
                                                       -> act_defend_hand_shield_up_0h_*
onehanded_block_swing (1h SEM escudo) defend_up/down/left/right, 3 cada
                                                       -> act_defend_up_1h_*   <-- parry de ARMA
```

Os quatro lados são perfeitamente simétricos nos três sets — **não há lacuna de dados na direção
up**. Então a hipótese (a) do coordenador ("o usage dual não tem entrada de defend up") está
descartada por evidência, e a (c) também: os `combat_parameter` do módulo (`1h_dual_others`,
`onehanded_dual_thrust`, `onehanded_dual_right`, `..._balanced`) são todos de ataque — colisão de
defesa é `defend_with_1h` / `hand_shield_defence`, que são vanilla e intocados.

## A causa: pose de escudo de mão aplicada a uma lâmina

Nossa offhand é um **`OneHandedSword`** (`weapon_class="OneHandedSword"`, `weapon_length="95"`) com
`HeldInOffHand`, `WoodenParry` e `ForceAttachOffHandPrimaryItemBone`, e usage `dual_shield` cujo
`base_set` é `hand_shield`. Consequência: para o engine, a mão esquerda segura um **escudo de mão**,
e a defesa usada é `act_defend_hand_shield_up_1h_*` com a geometria de escudo:

```
defend_up     offhand_begin_hand_position="0,-0.1,0.1"    offhand_begin_arm_rotation="0,0.4"
defend_down   offhand_begin_hand_position="0,-0.1,-0.1"   offhand_begin_arm_rotation="0,-0.4"
defend_left   offhand_begin_hand_position="-0.18,0.05,-0.1" offhand_begin_arm_rotation="-0.4,0"
defend_right  offhand_begin_hand_position="0.18,0.05,-0.1"  offhand_begin_arm_rotation="0.4,0"
```

Nas três direções que funcionam a pose mantém o "escudo" **de pé, à frente do corpo** — e uma lâmina
nessa posição atravessa a trajetória de um golpe horizontal, então o capsule intercepta. Na direção
up a pose de escudo de mão levanta o antebraço com o escudo **deitado acima da cabeça**: um escudo é
um disco e cobre; uma lâmina fica praticamente alinhada com a trajetória vertical do golpe e não
cobre nada. É exatamente o padrão do sintoma — três direções boas, up furada — e é a hipótese (b) do
coordenador, com a causa precisada: não é janela/flag do clipe, é a **geometria** da pose.

## A correção: o parry de arma do vanilla, só na direção up

Rota (3) do coordenador — usar o fallback vanilla **correto** de defesa up de 1h, o mesmo que o jogo
usa sem escudo. É `onehanded_block_swing`:

```xml
<usage style="defend_up"
       defend_action="act_defend_up_1h_passive"
       defend_active_action="act_defend_up_1h_active"
       defend_parry_light_action="act_defend_up_1h_parry_light"
       begin_hand_position="0.3,0.25,0.4" begin_hand_rotation="-90,0" ... />
```

`begin_hand_position="0.3,0.25,0.4"` + `begin_hand_rotation="-90,0"` põe a lâmina da **mão
principal na horizontal acima da cabeça** — a pose que o jogo usa para aparar golpe vertical com 1h
sem escudo. Declarei as três variantes (a pé, montado, left stance) dentro do **nosso** set
derivado `dual_shield_thrust`, cada uma com `require_left_hand_usage_root_set="dual_shield"`, o
mesmo idioma de gating que o resto do arquivo já usa — então **só vale com dual wield**, e o
comportamento vanilla de qualquer outra arma fica intacto.

Verificado antes de escrever: as três actions existem no `action_types.xml` do Native
(`type="actt_defend_up_1h" usage_direction="ud_defend_up"`) e as seis (com as `_left_stance`) já
estão ligadas a animação em `as_human_warrior` do Native:

```
act_defend_up_1h_passive                 -> defend_up_1h_passive
act_defend_up_1h_active                  -> defend_up_1h_active
act_defend_up_1h_parry_light             -> defend_up_1h_parry_light
act_defend_up_1h_passive_left_stance     -> defend_up_1h_passive_left_stance
act_defend_up_1h_active_left_stance      -> defend_up_1h_active_left_stance
act_defend_up_1h_parry_light_left_stance -> defend_up_1h_parry_light_left_stance
```

Portanto esta correção **não precisa de asset novo, `.rdc` novo, `action_type` novo nem binding
novo** — nenhum risco de repetir a causa das rodadas 3 e 4. Um único arquivo mudou:
`_Module/ModuleData/item_usage_sets.xml` (+3 usages e o comentário que explica o porquê).

**Não toquei em `defend_down`/`left`/`right`** — o autor confirmou que funcionam. Uma variável por
teste. Também deixei os 10 `<guard>` como estavam: eles são a pose da mão da arma, mecanismo
separado das `defend usages`, e mexer neles somaria variável sem evidência.

Conferido no merge: o XSD `soln_item_usage_sets.xsd` declara unicidade só para `item_usage_set/@id`
(e `flag`, `idle`, `movement_set`) — **não** para `usage`. Então `usage` nunca é sobrescrita por
chave no merge entre módulos: as nossas entram por adição. E `dual_shield_thrust` não existe no
Native, então o set inteiro é adicionado sem colisão.

## O que o autor testa agora

Build **0 erros / 0 warnings**, `item_usage_sets.xml` deployado com hash conferido (3 `defend_up`
presentes no arquivo instalado), DLL inalterada e com hash idêntico, `Assets` 28 `.tpac`,
`AssetSources` 5 `.fbx`, `RuntimeDataCache` 24 `.rdc`.

Com as duas lâminas equipadas, segurar defesa **para cima** e receber:

1. **golpe vertical de arma 2h** (overhead de dois-mãos) — tem de bloquear;
2. **overhead de 1h** — tem de bloquear;
3. e confirmar que **down / left / right continuam bloqueando** (é o risco desta mudança: a
   defesa up nova não deve roubar as outras direções).

Se a up passar a bloquear mas a animação ficar estranha (a lâmina da mão principal sobe em vez da
offhand), é o esperado — é o parry de arma do vanilla. Ajuste cosmético fica para depois de a
mecânica estar certa.

Se a up **ainda** não bloquear, a próxima hipótese é que o engine prefira a `defend usage` herdada
do `base_set` à declarada no set derivado; nesse caso o caminho é suprimir a variante
`hand_shield` de `defend_up` do `onehanded_shield_swing` via o `item_usage_sets.xslt` que o módulo
já tem, em vez de adicionar por cima.
