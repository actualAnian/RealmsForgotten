# Análise de compatibilidade — mod `UniqueCombat` vs Bannerlord v1.4.8 (com War Sails / NavalDLC)

Data: 2026-08-10
Escopo: **somente leitura**. Nada dentro da pasta do jogo foi alterado.
Fonte do módulo: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\UniqueCombat`
Decompilado do mod (temporário): `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\.tmp_uniquecombat_decompiled\UniqueCombat\*.cs`
Método de verificação: reflexão (ReflectionOnlyLoad) direto nos assemblies **reais da 1.4.8** + dump de IL do mod (`ilspycmd -il`) para extrair a tabela exata de type/member refs. O decompilado do workspace (`Vanilla_1.3.x/MEGA_*.md`) é 1.3.x, então **não** foi usado como fonte de verdade.

---

## 1. O que o mod é / faz

`UniqueCombat` (AssemblyTitle interno: **"Dragon"**, Copyright 2022) é um **protótipo pessoal de "combate/cenas únicas"**, não um mod acabado. Ele tem duas metades bem distintas:

- **Metade C# (24 KB de DLL, 718 linhas, 16 tipos)** — um `MissionBehavior` global (`HorseKickMissionBehavior`) que injeta 7 tipos de `AgentComponent` customizados em agentes da missão e reage a **teclas hardcoded de debug**: `P` = transforma todos os NPCs de IA em brigões de taverna (troca de time aleatória a cada 10 s), `L` = animação de bebedeira + ragdoll do jogador após 8 s, **botão do meio do mouse = "assassinato" instantâneo de 2000 de dano com câmera cinematográfica** no NPC mais próximo (até 3 m). Além disso: coice de cavalo com AoE de knockdown quando o cavalo é atingido, componente "covarde" (todo NPC atingido foge), "shamer/prisoner shame" (arrasta heróis prisioneiros e 15 NPCs numa cena de humilhação em missões amigáveis), um bêbado briguento de taverna com diálogo próprio ("What are you looking at? Bitch.") e 5 patches Harmony. Vários componentes cospem `InformationManager.DisplayMessage` de debug ("Drunk", "DrunkMan", "Drunk Attack: N", nome da cena).
- **Metade XML/assets (ModuleData + Assets)** — o **dual wield** (dual sword/shield/polearm: `action_types`, `item_usage_sets`, `movement_sets`, `full_movement_sets`, `item_holsters`, `crafting_pieces/templates`, `weapon_descriptions`, `items`) + animações `.tpac`. **Esta metade já foi portada para o módulo `RF_DualWield`** do RF (mesmos ids, mesmos assets, versões divergentes — ver §7).

Ou seja: em termos de valor para o RF, o C# é um caderno de experimentos com hotkeys de debug; o conteúdo aproveitável (dual wield) já está no `RF_DualWield`.

### Para qual versão foi compilado

- `SubModule.xml` declara `<Version value="e1.5.2"/>` — **valor fictício**, não existe e1.5.2; e não há nenhum `DependentVersion` declarado nos `DependedModule`.
- DLL datada de **31/05/2023**, `net472`, `AssemblyVersion 1.0.0.0`, referencia `0Harmony 2.2.2.0`.
- Pela **assinatura real das APIs que ela chama**, foi compilada contra assemblies da era **e1.1.x / e1.2.0** (meados de 2023). Provas: `TavernEmployeesCampaignBehavior` ainda em `TaleWorlds.CampaignSystem.CampaignBehaviors`, `ActionIndexCache` ainda **classe** (hoje é struct), `AgentComponent.OnTickAsAI` ainda existia, `Agent.ControllerType` como enum aninhado, `Blow.Position` (hoje `GlobalPosition`), ctor de `LocationCharacter` com 12 parâmetros, `AgentComponentExtensions.Retreat(Agent)` com 1 parâmetro.
- Referências de assembly: todas `TaleWorlds.* 1.0.0.0` — Bannerlord não usa strong naming, então **versão de assembly não é problema**; o problema é assinatura de membro.

---

## 2. BLOQUEADOR ZERO — a DLL nem está no lugar certo

```
Modules\UniqueCombat\bin\
└── Win64_Shipping_wEditor\        ← ÚNICA pasta existente
    ├── UniqueCombat.dll
    └── UniqueCombat.pdb
```

Não existe `bin\Win64_Shipping_Client\`. O cliente normal do jogo (`Win64_Shipping_Client`) procura `bin\Win64_Shipping_Client\UniqueCombat.dll` e **não vai encontrar** → falha de carregamento do SubModule no startup. Isto é anterior a qualquer questão de API: **como está, o mod não carrega o código nem numa versão compatível.** Só funcionaria num build com editor (`_wEditor`).

Estado atual no launcher: `UniqueCombat` está **`IsSelected = false`** (desmarcado) e é o penúltimo da ordem de carregamento. Ou seja, hoje ele não está ativo — nada está quebrando por causa dele.

---

## 3. Superfície de risco — Harmony patches

5 patches, todos aplicados por `Harmony("Nwetta1.core").PatchAll()` em `OnSubModuleLoad` (`Main.cs:18-19`). **Nenhum transpiler** — só 3 postfix e 2 prefix. Isso é a boa notícia.

| # | Alvo (como o mod pede) | Tipo | Existe na 1.4.8? | Arquivo:linha |
|---|---|---|---|---|
| 1 | `TaleWorlds.CampaignSystem.CharacterObject.Deserialize(MBObjectManager, XmlNode)` | Postfix | **OK** (assinatura idêntica) | `CharacterObjectPatch.cs:8` |
| 2 | `TaleWorlds.MountAndBlade.MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(in AttackCollisionData, int)` | Postfix | **OK** (nomes de parâmetro batem: `collisionData`, `weaponAttachBoneIndex`) | `IsCollisionBoneDifferentThanWeaponAttachBonePatch.cs:6` |
| 3 | `SandBox.Missions.MissionLogics.MissionFightHandler.IsAgentAggressive(Agent agent)` | Prefix | **OK** | `MissionFightHandlerPatch.cs:7-8` |
| 4 | `TaleWorlds.MountAndBlade.Mission.RegisterBlow` | Prefix | **OK** — na 1.4.8 é `RegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon, ref CombatLogData combatLogData)`. O prefix pede só `attacker`, `victim`, `ref b` — nomes existem, e Harmony aceita `ref` sobre parâmetro por valor. **Nota:** `realHitEntity` mudou de `GameEntity` → `WeakGameEntity`, mas o patch não toca nele | `Mission_RegisterBlow_Patch.cs:7` |
| 5 | `TaleWorlds.CampaignSystem.CampaignBehaviors.TavernEmployeesCampaignBehavior.LocationCharactersAreReadyToSpawn` | Postfix | **QUEBRADO — tipo movido de assembly.** Na 1.4.8 é `SandBox.CampaignBehaviors.TavernEmployeesCampaignBehavior` (em `SandBox.dll`). O namespace antigo **não existe mais**. O método `LocationCharactersAreReadyToSpawn(Dictionary<string,int> unusedUsablePointCount)` continua idêntico no tipo novo | `TavernEmployeesCampaignBehaviorPatch.cs:14,17` |

**Consequência do #5:** o `typeof(...)` está no atributo `[HarmonyPatch]` e o tipo também é parâmetro do `Postfix`. Quando `PatchAll()` varrer os tipos do assembly, resolver esse typeref lança `TypeLoadException` **dentro de `OnSubModuleLoad`** → falha de carregamento do módulo (não é um erro silencioso, derruba o startup).

**Reflexão por string:** nenhuma. Não há `AccessTools.Method/Field/TypeByName`, nem `GetMethod("...")`, nem `typeof(X).GetField(...)`. Todos os alvos são `typeof(...)` + nome literal no atributo. Isso reduz muito o risco.

---

## 4. Tabela de alvos e correções — APIs TaleWorlds usadas diretamente

Legenda de status: **OK** / **MOVIDO** / **RENOMEADO** / **ASSINATURA MUDOU** / **CLASSE→STRUCT** / **REMOVIDO**.
Linhas referem-se ao decompilado em `.tmp_uniquecombat_decompiled\UniqueCombat\`.

### 4.1 Quebras obrigatórias (impedem compilar e/ou explodem em runtime)

| Alvo usado pelo mod | Status | 1.4.8 real / correção | Arquivo:linha |
|---|---|---|---|
| `TaleWorlds.CampaignSystem.CampaignBehaviors.TavernEmployeesCampaignBehavior` | **MOVIDO** | `SandBox.CampaignBehaviors.TavernEmployeesCampaignBehavior` (assembly `SandBox`). Trocar o `using`/`typeof` e adicionar referência a SandBox (já existe) | `TavernEmployeesCampaignBehaviorPatch.cs:5,14,17` |
| `AgentComponent.OnTickAsAI(float dt)` | **REMOVIDO** | Não existe mais em `AgentComponent` **nem** em `SandBox.CampaignAgentComponent`. Os hooks disponíveis hoje são `OnTick(float dt)` e `OnTickParallel(float dt)`. Correção: `override void OnTick(float dt)` + guarda `if (!Agent.IsAIControlled) return;` | `ExtraDrunkComponent.cs:21-23` |
| `CampaignAgentComponent.OnTickAsAI(float dt)` | **REMOVIDO** | idem acima (chamada `base` também morre) | `ExtraCrazyComponent.cs:18-20`, `ExtraDrunkPedestrianComponent.cs:14-16`, `ExtraPrisonerShameComponent.cs:15-17`, `ExtraShamerComponent.cs:15-17` |
| `AgentComponent.OnHit(Agent, int, in MissionWeapon)` — 3 params | **ASSINATURA MUDOU** | 1.4.8: `OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)` — 5 params. Adicionar os 2 parâmetros novos (ou migrar para `MissionBehavior.OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon, in Blow, in AttackCollisionData)` no `MissionLogic`) | `ExtraCowardComponent.cs:9-11`, `ExtraHorseComponent.cs:12-14` |
| `ActionIndexCache` como **classe de referência** (array `ActionIndexCache[]`, passagem por valor de referência, `null`) | **CLASSE→STRUCT** | Na 1.4.8 `TaleWorlds.MountAndBlade.ActionIndexCache` é **struct** (`IsValueType=True`). Todo o `fireActions` e os casts `(ActionIndexCache)((object[])(object)fireActions)[n]` precisam ser reescritos como array de struct e passagem por `ref` | `HorseKickMissionBehavior.cs:27-34,127,163,164`, `ExtraCowardComponent.cs:12`, `ExtraHorseComponent.cs:15`, `ExtraCrazyComponent.cs:41-42` |
| `ActionIndexCache.Name` (propriedade) | **RENOMEADO** | 1.4.8 tem o método **`GetName()`**; não existe `get_Name()` | `HorseKickMissionBehavior.cs:123`, `ExtraDrunkComponent.cs:26` |
| `ActionIndexCache.act_none` (propriedade) | **ASSINATURA MUDOU** | Hoje é **campo estático** (`[F] ActionIndexCache act_none`), não propriedade → `get_act_none()` não existe | `ExtraCrazyComponent.cs:41-42` |
| `Agent.SetActionChannel(int, ActionIndexCache /*classe*/, bool, ulong, ...)` | **ASSINATURA MUDOU** | 1.4.8: `bool SetActionChannel(int channelNo, ref ActionIndexCache actionIndexCache, bool ignorePriority = …, AnimFlags additionalFlags = …, float …, int actionShift = …, bool forceFaceMorphRestart = …)`. Agora é `ref` de struct e o 4º param é o enum `TaleWorlds.MountAndBlade.AnimFlags` (UInt64) | `HorseKickMissionBehavior.cs:127,163,164`, `ExtraCowardComponent.cs:12`, `ExtraHorseComponent.cs:15`, `ExtraCrazyComponent.cs:41-42` |
| `Agent.GetCurrentAction(int)` tratado como retorno de referência (e ponteiro `ActionIndexCache*`) | **CLASSE→STRUCT** | 1.4.8 retorna a struct `ActionIndexCache` por valor. Usar `agent.GetCurrentAction(1).GetName()` | `HorseKickMissionBehavior.cs:122-123`, `ExtraDrunkComponent.cs:26` |
| `Agent.ControllerType` (enum aninhado) | **MOVIDO/RENOMEADO** | Não existe mais como tipo aninhado de `Agent`. Hoje: **`TaleWorlds.Core.AgentControllerType`**, e `AgentBuildData.Controller(AgentControllerType controller)`. O `(ControllerType)1` do mod deve virar `AgentControllerType.AI` | `HorseKickMissionBehavior.cs:89` |
| `Blow.Position` (campo) | **RENOMEADO** | 1.4.8: o campo é **`GlobalPosition`**. Não existe `Blow.Position` | `HorseKickMissionBehavior.cs:152-153,159`, `ExtraHorseComponent.cs:29-30,36` |
| `AgentComponentExtensions.Retreat(Agent)` — 1 param | **ASSINATURA MUDOU** | 1.4.8: `Retreat(Agent agent, bool useCachingSystem = …)`. O parâmetro opcional é resolvido em tempo de compilação, então o memberref de 1 param **não existe** no assembly novo → `MissingMethodException`. Recompilar resolve | `ExtraCowardComponent.cs:13` |
| `LocationCharacter` ctor com **12** argumentos | **ASSINATURA MUDOU** | 1.4.8 tem **14** parâmetros (os 7 últimos opcionais, incluindo dois novos: `AfterAgentCreatedDelegate afterAgentCreated`, `bool forceSpawnOnSpecialTargetTag`). Mesmo caso dos opcionais: recompilar resolve | `TavernEmployeesCampaignBehaviorPatch.cs:46` |

### 4.2 Verificado e OK na 1.4.8 (nenhuma ação necessária)

| Alvo | Observação |
|---|---|
| `CharacterObject.Deserialize(MBObjectManager, XmlNode)` | idêntico |
| `MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(in AttackCollisionData, int)` | idêntico |
| `MissionFightHandler.IsAgentAggressive(Agent)` | idêntico, ainda em `SandBox.Missions.MissionLogics` |
| `Mission.RegisterBlow` | existe; nomes dos params do prefix batem |
| `AttackCollisionData.GetAttackCollisionDataForDebugPurpose(...)` | **37 parâmetros, ordem e tipos idênticos** aos do mod. Sobreviveu intacto — surpreendente, mas confirmado |
| `Blow` (ctor `(int ownerId)`, `DamageType`, `BoneIndex`, `BaseMagnitude`, `InflictedDamage`, `SwingDirection`, `Direction`, `BlowFlag`, `WeaponRecord`) | todos OK — **só `Position`** quebrou |
| `BlowWeaponRecord.FillAsMeleeBlow(ItemObject, WeaponComponentData, int, sbyte)` | idêntico |
| `BlowFlags.CanDismount` / `.KnockDown` | OK |
| `MonsterExtensions.FillAnimationSystemData(Monster, MBActionSet, float, bool)` | idêntico (extension) |
| `MBGlobals.GetActionSetWithSuffix(Monster, bool, string)` | idêntico |
| `ActionSetCode.GenerateActionSetNameWithSuffix(Monster, bool, string)` | idêntico |
| `TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(int, string)` | idêntico |
| `Agent.SetActionSet(ref AnimationSystemData)` | idêntico |
| `Agent.SetAgentFlags(TaleWorlds.Core.AgentFlag)`, `SetWatchState(Agent.WatchState)`, `SetScriptedFlags(Agent.AIScriptedFrameFlags)`, `SetLookAgent`, `SetTeam(Team,bool)`, `SetMovementDirection(ref Vec2)`, `GetMovementDirection()`, `TeleportToPosition(Vec3)`, `GetEyeGlobalHeight()`, `RegisterBlow(Blow, ref AttackCollisionData)`, `DisableScriptedMovement()`, `DisableScriptedCombatMovement()`, `AddComponent`/`RemoveComponent`/`GetComponent<T>`, `State`, `Velocity`, `Monster`, `Character`, `Frame`, `Position`, `LookDirection`, `MountAgent`, `SpawnEquipment`, `WieldedWeapon` | todos OK |
| `Agent.UsageDirection`, `Agent.WatchState`, `Agent.AIScriptedFrameFlags` (aninhados) | continuam existindo — **só `ControllerType` sumiu** |
| `MBAgentVisuals.GetEntity()` → `TaleWorlds.Engine.GameEntity` + `GameEntity.ActivateRagdoll()` | OK (não virou `WeakGameEntity` aqui) |
| `TaleWorlds.Core.Timer(float,float,bool)` + `Check(float)` | OK. O mod já referencia `TaleWorlds.Core.Timer` (não `TaleWorlds.Library.Timer`, que não existe mais) |
| `TaleWorlds.Core.MissionMode` / `AgentFlag` / `AgentState` | OK (já em `TaleWorlds.Core` no memberref do mod) |
| `Mission.SetMissionMode(MissionMode,bool)`, `SpawnAgent(AgentBuildData,bool)`, `AddMissionBehavior`, `IsFriendlyMission`, `Agents`, `MainAgent`, `Scene`, `CurrentTime`, `Mode`, `AttackerTeam`, `DefenderTeam` | todos OK |
| `AgentBuildData.Team/TroopOrigin/InitialPosition/InitialDirection/NoHorses/NoWeapons/NoArmor/ClothingColor1/ClothingColor2` | todos OK (só `Controller` quebrou pelo tipo do enum) |
| `SimpleAgentOrigin(BasicCharacterObject,int,Banner,UniqueTroopDescriptor)` | idêntico (4 params) |
| `AgeModel.GetAgeLimitForLocation(CharacterObject, out int, out int, string)` | idêntico (4 params, o mod já passa 4) |
| `Location.AddLocationCharacters(CreateLocationCharacterDelegate, CultureObject, CharacterRelations, int)` | idêntico |
| `ICampaignMission.Location` / `CampaignMission.Current` | OK (o mod chama via `ICampaignMission.get_Location`) |
| `PlayerEncounter.LocationEncounter.Settlement` | OK |
| `CampaignAgentComponent.CreateAgentNavigator()`, `AgentNavigator.AddBehaviorGroup<DailyBehaviorGroup>()`, `AgentBehaviorGroup.AddBehavior<EscortAgentBehavior>()`, `EscortAgentBehavior.Initialize(Agent, Agent, OnTargetReachedDelegate)`, `IsEscortFinished()`, `AgentBehavior.IsActive` | todos OK, mesmos namespaces `SandBox.Missions.AgentBehaviors` |
| `CampaignGameStarter.AddDialogLine(8 params)` / `AddPlayerLine(9 params)` | existem com essa aridade |
| `Campaign.Current.ConversationManager.ListenerAgent` | OK (`TaleWorlds.CampaignSystem.Conversation.ConversationManager`) |
| `MissionScreen` (base de `CustomCutsceneCamera`) | não é abstrata, sem membros abstratos → tipo carrega. Código morto, mas inofensivo |
| `MissionScreen.CustomCamera` / `SetCameraLockState(bool)` / `CombatCamera`, `Camera.CreateCamera/FillParametersFrom/LookAt`, `ScreenManager.TopScreen` | todos OK |
| `Input.IsKeyPressed(InputKey)`, `Scene.GetName()`, `InformationManager.DisplayMessage`, `Color.FromUint`, Vec2/Vec3/Mat3/MatrixFrame | todos OK |
| `MBObjectManager.GetObject<T>(string)`, `MBObjectBase.StringId`, `MobileParty.PrisonRoster.ToFlattenedRoster()`, `Team.Color/Color2`, `BasicCharacterObject.GetStepSize()/FaceDirtAmount/Race/IsHero/IsSoldier`, `Monster.MonsterUsage/HeadLookDirectionBoneIndex`, `Equipment.GetEquipmentFromSlot`, `MissionWeapon.CurrentUsageItem`, `WeaponComponentData.ItemUsage` | todos OK |

### 4.3 Resumo numérico das quebras

| Categoria | Qtd. de alvos distintos |
|---|---|
| Tipo movido de assembly/namespace | 2 (`TavernEmployeesCampaignBehavior`, `Agent.ControllerType`→`AgentControllerType`) |
| Membro removido | 2 (`AgentComponent.OnTickAsAI`, `CampaignAgentComponent.OnTickAsAI`) — atinge **5 componentes** |
| Membro renomeado | 2 (`Blow.Position`→`GlobalPosition`, `ActionIndexCache.Name`→`GetName()`) |
| Assinatura mudou | 5 (`AgentComponent.OnHit`, `Agent.SetActionChannel`, `Retreat`, `LocationCharacter.ctor`, `ActionIndexCache.act_none` propriedade→campo) |
| Mudança de forma de tipo (classe→struct) | 1 (`ActionIndexCache`) — contamina `fireActions`, `GetCurrentAction`, `SetActionChannel` |
| Harmony patches quebrados | **1 de 5** |
| Transpilers | **0** |
| Lookups por string / reflexão manual | **0** |

---

## 5. Referências de assembly da DLL

`UniqueCombat.dll` (net472, IL v4.0.30319) referencia:

```
mscorlib 4.0.0.0                      System.Xml 4.0.0.0     System.Core 4.0.0.0
0Harmony 2.2.2.0                      (instalado: Bannerlord.Harmony traz 0Harmony 2.4.2.0)
TaleWorlds.MountAndBlade 1.0.0.0      TaleWorlds.CampaignSystem 1.0.0.0
TaleWorlds.Core 1.0.0.0               TaleWorlds.MountAndBlade.View 1.0.0.0
TaleWorlds.Engine 1.0.0.0             TaleWorlds.Library 1.0.0.0
SandBox 1.0.0.0                       TaleWorlds.ObjectSystem 1.0.0.0
TaleWorlds.DotNet 1.0.0.0             TaleWorlds.InputSystem 1.0.0.0
TaleWorlds.ScreenSystem 1.0.0.0
```

- Todos os `TaleWorlds.*` são `1.0.0.0` (a TaleWorlds nunca versiona) e nada é strong-named → **mismatch de versão não é problema**, como esperado.
- `0Harmony 2.2.2.0` compilado vs **2.4.2.0** instalado: também não é problema (Harmony não é strong-named e a API usada — `new Harmony(id)` + `PatchAll()` + atributos — é estável entre 2.2 e 2.4).
- O `SubModule.xml` **não declara dependência de `Bannerlord.Harmony`**. Hoje funciona por sorte (Harmony é o 1º da ordem de carregamento), mas o correto é adicionar `<DependedModule Id="Bannerlord.Harmony"/>`.
- `<DependedModule Id="Sandbox"/>` está **correto** — o Id real do módulo vanilla é literalmente `Sandbox` (com b minúsculo), verificado no `SubModule.xml` do jogo.

---

## 6. XMLs do ModuleData

Estrutura geral: **aditiva**, baixo risco de crash. Verificações feitas:

| Arquivo | Conteúdo | Veredito |
|---|---|---|
| `action_sets.xml` | 2 action sets novos (`as_human_shamer`, `as_human_belligerent_drunk`), ambos `base_set="as_human_warrior"` | **OK** — `as_human_warrior` existe no `Native/ModuleData/action_sets.xml` da 1.4.8 |
| `action_types.xml` | 94 `<action>` novos (dual wield + assassin) | **OK** (aditivo). Ver §7: 82 desses ids também estão no `RF_DualWield` |
| `item_usage_sets.xml` | 3 sets novos com `base_set="onehanded_shield_swing"` e `"hand_shield"` | **OK** — ambos os base sets existem no Native 1.4.8 |
| `item_holsters.xml` | 1 holster `dual_back`, `base_set="thorax_back_near_left"` | **OK** — base existe no Native 1.4.8 |
| `movement_sets.xml` / `full_movement_sets.xml` | 12 movement sets + 2 full sets novos (dual/drunk) | **OK** (aditivo) |
| `combat_parameters.xml` | **sobrescreve 23 `<def>` vanilla + 4 `<combat_parameter>`** (limites de rotação de overswing montado/escada etc.) | **ATENÇÃO** — altera combate globalmente e **colide com `RF_DualWield` e `RF_Extension`, que também trazem `combat_parameters.xml`** (arquivos diferentes entre si). Quem carrega depois vence |
| `crafting_pieces.xml`, `crafting_templates.xml`, `weapon_descriptions.xml`, `items.xml` | peças/templates `DualPolearm`/dual + itens `dual_pitchfork*`, `mead_weapon` | **OK** (aditivo), mas duplica o que já existe em `RF_DualWield/ModuleData/rf_dual_wield_items.xml` |
| `bandits.xml` + `spnpccharacters.xml` | **ambos definem `NPCCharacter id="belligerent_drunk"`** (bandits com `Culture.looters`, spnpccharacters com 7 variantes por cultura) | **ATENÇÃO** — id duplicado em dois XMLs carregados na mesma sessão de Campaign; o último a carregar sobrescreve. Provoca aviso/override e pode fazer o lookup por cultura do código falhar (`Main.BelligerentDrunks[culture]` lança `KeyNotFoundException` se a cultura não tiver entrada) |
| `partyTemplates.xml`, `spclans.xml` | facção `belligerent_drunks` (bandido) + party template | **OK**, mas cria uma facção de bandidos nova no mapa — efeito colateral de campanha, não bug |

Nenhuma tag/atributo dos XMLs usa elemento que tenha mudado na 1.4.8. **Crash por XML: improvável.**

---

## 7. Sobreposição com `RF_DualWield` (importante para a decisão)

`Modules\RF_DualWield` é claramente **um port da metade XML/assets do UniqueCombat**: mesmos ids de action_types (82 em comum), mesmos `item_usage_sets`, `movement_sets`, `full_movement_sets`, `item_holsters`, `combat_parameters`, e um subconjunto dos mesmos `.tpac`. Todos os arquivos **diferem em conteúdo** (o do UniqueCombat é maior/mais antigo; o `RF_DualWield` tem até um `action_sets.xml.pre_actionset_fix_20260805.bak`, ou seja, já recebeu correções em agosto/2026).

**Implicação:** ativar `UniqueCombat` junto com `RF_DualWield` significa **duas definições concorrentes dos mesmos ids** de animação/usage/movement + dois `combat_parameters` conflitantes. Isso é pior que o problema de API: é regressão silenciosa no dual wield já corrigido do RF.

---

## 8. Veredito

### (a) Roda na 1.4.8 como está?

**Não. Duas vezes não.**

1. A DLL não existe em `bin\Win64_Shipping_Client\` → o SubModule falha no carregamento antes de qualquer coisa.
2. Mesmo que se copiasse a DLL para o lugar certo: `PatchAll()` em `OnSubModuleLoad` tentaria resolver `TaleWorlds.CampaignSystem.CampaignBehaviors.TavernEmployeesCampaignBehavior`, que **não existe na 1.4.8** → `TypeLoadException` no startup. E se o patch fosse removido, `HorseKickMissionBehavior` ainda quebraria no carregamento do tipo por causa do `static ActionIndexCache[] fireActions` (classe→struct) e depois em cada `SetActionChannel`/`Blow.Position`/`Retreat`/`OnTickAsAI`.

**Não há caminho de "só copiar a DLL".** Precisa de recompilação com correções de código.

### (b) Obrigatório vs cosmético

**Obrigatório (sem isso não roda):**
1. Produzir `bin\Win64_Shipping_Client\UniqueCombat.dll` (recompilar contra a 1.4.8).
2. `TavernEmployeesCampaignBehavior` → `SandBox.CampaignBehaviors`.
3. `OnTickAsAI` → `OnTick` (+ guarda de IA) nos 5 componentes.
4. `AgentComponent.OnHit` → 5 parâmetros (2 componentes).
5. `ActionIndexCache` como struct: `fireActions`, `GetCurrentAction(...)`, `SetActionChannel(... ref ...)`, `act_none` (campo), `GetName()`.
6. `Agent.ControllerType` → `TaleWorlds.Core.AgentControllerType.AI`.
7. `Blow.Position` → `Blow.GlobalPosition` (3 pontos).
8. Recompilar resolve automaticamente os opcionais (`Retreat`, `LocationCharacter` ctor).

**Cosmético / higiene (não bloqueia, mas deveria ser feito antes de considerar usar):**
- Remover as hotkeys de debug `P` / `L` / **botão do meio = 2000 de dano instantâneo** (isso é cheat, não feature).
- Remover os `DisplayMessage` de debug ("Drunk", "DrunkMan", "Drunk Attack: N", nome da cena).
- `Mission_RegisterBlow_Patch`: `new Random()` por chamada (dentro do hot path de todo golpe da missão — alocação + seed correlacionada) → usar `MBRandom`. E o efeito em si (25% de knockdown em **todo** golpe de herói, em toda missão, inclusive combate naval de War Sails) **colide de frente com o RBM**, que já está instalado e reescreve o cálculo de golpes.
- `ExtraCowardComponent` é adicionado a **todo** NPC de IA em `OnDeploymentFinished` → todo NPC atingido foge. Isso destrói qualquer batalha; é claramente código de teste.
- Adicionar `<DependedModule Id="Bannerlord.Harmony"/>`; corrigir `<Version>` (hoje "e1.5.2", fictício).
- Resolver o id duplicado `belligerent_drunk` entre `bandits.xml` e `spnpccharacters.xml`.
- Decidir o conflito de `combat_parameters.xml` com `RF_DualWield` e `RF_Extension`.

### (c) Esforço e roteiro

**Esforço do update puro de API: PEQUENO (2–4 h).** São 718 linhas, zero transpilers, zero reflexão por string, 1 patch quebrado de 5, e as quebras são todas mecânicas (rename/assinatura/struct). A parte mais chata é a conversão de `ActionIndexCache` de classe para struct, que toca 6 arquivos.

**Esforço para virar algo utilizável no RF: MÉDIO-GRANDE**, porque o problema não é compatibilidade — é que o mod é um protótipo com hotkeys de cheat, spam de debug, e um `ExtraCowardComponent` que quebra o combate. E a parte boa (dual wield) **já está no `RF_DualWield`**.

**Roteiro sugerido, em ordem:**

0. **Decidir escopo primeiro.** Recomendação: **não ressuscitar o módulo inteiro.** Ele está desmarcado no launcher e o dual wield já foi portado. Fazer *cherry-pick* das 2–3 ideias que interessam (coice de cavalo com AoE; bêbado briguento de taverna com diálogo; cena de humilhação de prisioneiro) para dentro de um módulo RF existente, escrevendo o código já contra a 1.4.8. Isso é mais rápido e mais seguro que consertar o protótipo.
1. Se ainda assim for consertar como módulo: criar `.csproj` novo apontando para os assemblies 1.4.8 do jogo (`bin\Win64_Shipping_Client` + `Modules\SandBox\bin\Win64_Shipping_Client`), com output para `bin\Win64_Shipping_Client`.
2. Corrigir os 8 itens obrigatórios da lista (b) — compilar até zerar os erros; o compilador vai apontar exatamente os pontos da tabela §4.1.
3. Antes de testar em jogo: **desativar/remover** as hotkeys de debug, o spam de mensagens e o `ExtraCowardComponent` global. Aplicar a **regra do canário** (habilitar um componente por vez, com log próprio, para saber qual quebra).
4. Não ativar junto com `RF_DualWield` sem antes reconciliar os XMLs duplicados (action_types / item_usage_sets / movement_sets / full_movement_sets / item_holsters / combat_parameters). Escolher **uma** fonte de verdade — provavelmente `RF_DualWield`, que é a versão mais nova — e apagar os equivalentes do UniqueCombat.
5. Só depois: testar `Mission_RegisterBlow_Patch` com RBM ativo, e testar especificamente uma **batalha naval de War Sails** (o patch é global e atinge combate de navio).

---

## 9. Riscos que a análise estática NÃO cobre

- **Nenhum transpiler**, então o principal risco de "IL não bate" está ausente. Isso é a melhor notícia do relatório.
- **Ordem de patches / conflito com RBM**: `Mission.RegisterBlow` é patchado por muitos mods. RBM (instalado) mexe pesado em cálculo de dano. Prefix sem prioridade declarada = ordem indefinida. Estático não prevê o resultado.
- **`AgentComponent.OnTick` vs `OnTickAsAI`**: a migração muda *semântica*, não só assinatura. `OnTickAsAI` só rodava para agentes de IA; `OnTick` roda para todos, e `OnTickParallel` roda em thread paralela. Chamar `SetTeam`/`SetActionChannel`/`InformationManager` do tick errado pode dar corrupção ou crash em thread. Só teste em jogo diz.
- **Animações**: os `.tpac` foram exportados em 2023. A 1.4.8 + War Sails adicionaram actions novas (`act_ship_*`, `act_usage_row_*`, `act_conversation_naval_*`). Se algum `base_set` herdado mudou de conteúdo, animações podem ficar em T-pose ou faltando — invisível na análise estática.
- **`Main.BelligerentDrunks[culture]`** é um `Dictionary` indexado direto, populado por um Postfix em `Deserialize`. Se a cultura do settlement não tiver um `belligerent_drunk` (e com o id duplicado entre dois XMLs isso é plausível), lança `KeyNotFoundException` **dentro de um Postfix de spawn de taverna** → crash ao entrar em taverna. Estático só levanta a suspeita.
- **`ScreenManager.TopScreen` cast direto para `MissionScreen`** (`HorseKickMissionBehavior.cs:167,180`): se qualquer overlay/UI estiver no topo (RTSCamera, DynamicCombatCamera, UIExtenderEx — todos instalados), o cast falha → `InvalidCastException` no meio do tick.
- **`camTimer` sem null-check** em `HorseKickMissionBehavior.cs:178` (`assassin && camTimer.Check(...)`): se `assassin` virar true por outro caminho, NRE.
- **Build `_wEditor` vs `_Client`**: a DLL existente foi compilada num contexto de editor. Se o código depender de algo só presente no build com editor (não vi nada, mas o `.pdb` e a pasta sugerem fluxo de editor), só o teste no cliente confirma.

---

## Apêndices — artefatos gerados

- Decompilado C# do mod: `C:\Users\gupol\Documents\Bannerlord_Warsails_AI\.tmp_uniquecombat_decompiled\`
- Dump de IL + tabelas extraídas (scratchpad da sessão): `mod.il`, `memberrefs.txt`, `calls_full.txt`, `probe_out.txt`, `probe2_out.txt`, `probe3_out.txt`
