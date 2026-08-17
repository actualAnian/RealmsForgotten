# Diagnóstico e correção — Sistema de RUNAS (RF_Magic / SOTOR.MagicAccessories)

Data: 2026-08-10 · Jogo: Bannerlord 1.4.8 (build 119303, War Sails) · Módulo: `RF_Magic` (DLL `SOTOR.dll`)

---

## (a) Mapa do sistema

### Onde vive

Tudo no projeto **`RF_Warsails_AI/RF_Magic`** (assembly `SOTOR.dll`, deploy em
`Modules/RF_Magic/bin/Win64_Shipping_Client/SOTOR.dll`). **Nada do sistema de runas toca
código do Anian** — verificado por grep, nenhum arquivo do RF_Magic tem marca de autoria dele.

| Arquivo | Papel |
|---|---|
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneData.cs` | POCO + enums `MagicRuneTarget` (flags), `MagicRuneTier`, `MagicRuneEffect` (24 efeitos) |
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneRegistry.cs` | Carrega `rf_magic_runes.xml`; `CanApply(rune, item)` mapeia `ItemObject.ItemTypeEnum` → `MagicRuneTarget` |
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneService.cs` | Fachada: `TryAssign`, `TryClear`, `GetBonuses`, `TryGetForSlot/ForWeapon/ForWieldedWeapon`, `HasEffect` |
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneSocketVM.cs` | ViewModel de 1 socket (imagem, hint, `ExecuteClick`) |
| `RF_Magic/SOTOR.MagicAccessories/InventoryMagicAccessorySlotsMixin.cs` | **Coração da UI**: mixin do `SPInventoryVM`, 4 sockets + anel/colar, patches Harmony de equip/tooltip |
| `RF_Magic/SOTOR.MagicAccessories/InventoryMagicAccessorySlotsExtension.cs` | `PrefabExtension` que injeta o XML dos sockets em `Inventory` → `descendant::Widget[@Id='RightEquipmentList']` |
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneCombatPatches.cs` / `MagicRuneMissionLogic.cs` | Efeitos em batalha (registrado em `SOTOR/SubModule.cs:170`) |
| `RF_Magic/SOTOR.MagicAccessories/MagicRuneLoadoutRegistry.cs` | Runas fixas de NPC/tropa (`rf_magic_rune_loadouts.xml`) |
| `RF_Magic/SOTOR.Extensions.ExtendedInfoSystem/HeroExtendedInfo.cs` | **Persistência** |

### XMLs

- `RF_Magic/ModuleData/rf_magic_runes.xml` — 72 runas (definição de efeito/alvo/valores). Log confirma: `loaded 72 runes`.
- `RF_Magic/ModuleData/rf_magic_rune_loadouts.xml` — 2 loadouts de tropa.
- `RealmsForgottenMain/_Module/ModuleData/rfitems/rf_magic_rune_items.xml` — 72 `ItemObject` (`Type="Goods"`, `item_category="jewelry"`, `Civilian="true"`). **Não há crafting**: as runas são itens de mercadoria comum.

### Desenho do fluxo

Não existe "criar socket". **Todo item de arma/escudo/arco/munição equipado nos 4 slots de
arma já tem um socket implícito** — os 4 botões injetados à direita do boneco (visíveis só
no modo Batalha, `IsVisible="@IsBattleMode"`) são os sockets dos `EquipmentIndex.Weapon0..Weapon3`.

Fluxo pretendido de equipar (é o que o próprio hint diz):
1. Jogador **seleciona** a runa no inventário (clique na linha).
2. Jogador **clica no socket** correspondente à arma.
3. `MagicRuneSocketVM.ExecuteClick` → `InventoryMagicAccessorySlotsMixin.ExecuteRuneSocket(slot)`.
4. Valida (modo batalha, arma no slot, runa compatível, runa está no inventário do jogador).
5. `MagicRuneService.TryAssign` → `HeroExtendedInfo.SetWeaponRune(slot, runeId, targetItemId)`.
6. A runa é **consumida**: `TransferCommand.Transfer(1, PlayerInventory, None, ...)`.
7. `RefreshSlots()` redesenha o socket.

Fluxo alternativo (arrastar/duplo-clique): prefixo Harmony em `SPInventoryVM.ProcessEquipItem`
→ `EquipRuneAutomatically` acha o primeiro slot compatível. Um postfix em
`SPItemVM.get_IsEquipableItem` força runas (que são `Goods`) a contarem como equipáveis.

### Onde salva

`HeroExtendedInfo` (classe saveable id **1** em `SOTOR.SaveGameSystem/SotorSaveableTypeDefiner.cs:17`),
campos `[SaveableField(10..17)]`: `_weaponRuneItemId0..3` + `_weaponRuneTargetItemId0..3`.
O par (runa, id da arma alvo) é gravado para que a runa fique **inativa** se a arma daquele
slot mudar. Não há behavior de campanha próprio — pega carona no `ExtendedInfoManager`.
**Não há conflito de saveBaseId**: os ids novos são campos de uma classe já definida.

---

## (b) Causa do erro — a seleção do inventário morre antes do clique no socket

O sistema lia o item selecionado assim (`InventoryMagicAccessorySlotsMixin.cs`, linhas
originais 307 e 356):

```csharp
SPItemVM selectedItem = GetPrivate<SPItemVM>("_selectedItem");
```

`SPInventoryVM._selectedItem` **não é a seleção do jogador — é o item sob o mouse**, e ele é
zerado assim que o ponteiro sai da linha do item. Duas evidências no jogo instalado (1.4.8):

1. **Prefab do inventário** — `Modules/SandBox/GUI/Prefabs/Inventory/Inventory.xml:35`
   (`InventoryScreenWidget` raiz):
   ```
   Command.ItemHoverBegin="ProcessItemTooltip"  Command.ItemHoverEnd="ResetSelectedItem"
   ```
   e `SPInventoryVM.ProcessItemTooltip` é justamente quem faz `_selectedItem = item as SPItemVM;`,
   enquanto `ResetSelectedItem()` faz `_selectedItem = null;`
   (vanilla decompilado: `Vanilla_1.3.x/MEGA_009.md:87335` e `:87347`).
   `InventoryScreenWidget.ItemWidgetHoverEnd` dispara `EventFired("ItemHoverEnd")` sempre que
   o mouse deixa a linha (`MEGA_014.md:12540-12546`).

   → Ao mover o mouse do inventário até o socket, `_selectedItem` **já é null**.

2. **Pior: a seleção "de verdade" também é apagada.** O jogo tem seleção persistente
   (`SPItemVM.IsSelected`, via `SPInventoryVM.ExecuteSelectItem`, `MEGA_009.md:88183`), mas
   `InventoryScreenWidget.OnUpdate` (`MEGA_014.md:12371-12379`) faz:
   ```csharp
   bool flag = _latestMouseDownWidget is InventoryItemButtonWidget || ...parents...;
   if (_latestMouseDownWidget == null || (!flag && !flag2 && !ItemPreviewWidget.IsVisible))
       EventFired("OnEmptyClick");     // -> ExecuteClearSelectedItem() -> IsSelected = false
   ```
   Nossos sockets são `ButtonWidget` comuns injetados em `RightEquipmentList` — **não** são
   `InventoryItemButtonWidget` nem filhos de um. Logo, **o mouse-down no socket dispara
   `OnEmptyClick` e limpa a seleção um frame antes do nosso `Command.Click` rodar.**

Consequência: `ExecuteRuneSocket` sempre chegava com `selectedItem == null` e caía no fim
do método (`InventoryMagicAccessorySlotsMixin.cs:381` original):

```
"Select a compatible rune in your inventory, then click this socket."   (amarelo)
```

ou, se já havia runa naquele socket, **desequipava** em vez de equipar. Exatamente o sintoma
relatado: *"dá erro e não equipa nos sockets criados"*.

### Confirmação por log

- `C:\ProgramData\Mount and Blade II Bannerlord\logs\rgl_log_9104.txt` (sessão 20:36→20:47):
  registries carregam OK (`MagicRuneRegistry: loaded 72 runes`, linha 1247) e há dezenas de
  `Render Requested: rf_rune_*_lesser` (linhas 7734-7798) — ou seja, o autor **estava** com as
  runas no inventário mexendo na tela.
- **Nenhuma exceção** no caminho de runas: `rgl_log_errors_*.txt` estão vazios e o log SOTOR
  (`Documents\Mount and Blade II Bannerlord\Logs\SOTOR\session_2026-08-10_20-36-47.log`) só
  tem 1 erro, alheio (`SotorGraveyardMountPatch` falha ao patchear `Mission::SpawnTroop`).
  → Não era crash nem NRE: era **beco sem saída lógico + mensagem de erro na tela**.
- O mesmo bug afetava **anel e colar** (`ExecuteAccessorySlot` usava o mesmo `_selectedItem`).

---

## (c) O que foi corrigido

Um único arquivo: `RF_Warsails_AI/RF_Magic/SOTOR.MagicAccessories/InventoryMagicAccessorySlotsMixin.cs`.
**O desenho foi mantido** (selecionar runa → clicar no socket); só a fonte da seleção mudou.

1. **Seleção persistente própria** — novo campo `_stickySelectedItem` no mixin.
2. **Novo patch Harmony** `InventoryMagicSelectionMemoryPatch`: postfix em
   `SPInventoryVM.ExecuteSelectItem(ItemVM)`. Grava a seleção quando `item != null` e
   **ignora as limpezas** (`item == null`), que são exatamente o `OnEmptyClick` disparado
   pelo mouse-down no socket. Assim a seleção sobrevive até o clique.
3. **Novo helper `GetSelectedInventoryItem()`**: tenta primeiro o `_selectedItem` do VM
   (caso o mouse ainda esteja sobre o item), depois o sticky. Substitui as duas leituras
   diretas de `_selectedItem` em `ExecuteRuneSocket` **e** `ExecuteAccessorySlot`
   (o bug era idêntico para anel/colar).
4. **Limpeza do sticky** após equipar com sucesso (runa e acessório) e em `OnFinalize`.
5. **Diagnóstico**: `SotorLog.Info` a cada clique de socket (slot, arma alvo, item
   selecionado, lado do inventário), `SotorLog.Warn` quando `TryAssign` recusa e
   `SotorLog.Info` quando a runa é encaixada. Se ainda falhar in-game, o log SOTOR agora
   diz em qual gate parou.

Nada mais foi redesenhado: `TransferCommand.Transfer(..., InventorySide.None, ...)` foi
verificado no vanilla (`MEGA_010.md:131110-131165`) e **é válido** — remove do
`PlayerInventory`, não adiciona em lado nenhum e emite `TransferCommandResult` correto para
o `AfterTransfer` do VM; o cancelamento é coberto pelo `_rostersBackup` do `InventoryLogic`
mais o `OnInventoryReset` do mixin.

### Build e deploy

```
dotnet build RF_Warsails_AI/RF_Magic/RF_Magic.csproj
→ 0 Error(s), 31 Warning(s) (todas pré-existentes: MSB3246 de DLLs nativas + 2 CS9191)
```
Deploy pelo PostBuild do próprio csproj → `Modules/RF_Magic/bin/Win64_Shipping_Client/SOTOR.dll`
(21:01 de 2026-08-10). **Nada commitado nem enviado.**

---

## (d) Roteiro de teste in-game

1. **Obter runas** — as runas são mercadoria (`is_merchandise`), ids `rf_rune_*_lesser|greater|ancient`.
   Comprar num mercado ou usar console/cheat para inserir p.ex. `rf_rune_keen_edge_lesser`
   (Melee) e `rf_rune_huntsman_lesser` (Bow) no inventário.
2. **Socket existe?** Abrir Inventário → aba **Batalha** (os sockets só aparecem com
   `IsBattleMode`) → 4 quadradinhos "R" à direita, ao lado dos slots de arma 1-4.
   Passar o mouse: o hint mostra `Rune socket N / Item: <arma> / Rune: Empty`.
3. **Equipar (fluxo principal)** — ter uma espada no slot de arma 1. **Clicar uma vez na
   linha da runa** no inventário (a linha fica destacada) e **depois clicar no socket 1**.
   Esperado: a imagem da runa aparece no socket, a runa **desaparece do inventário**
   (foi consumida) e o log SOTOR registra `Rune '...' socketed on '...' (slot 0)`.
4. **Incompatibilidade** — selecionar a runa de arco e clicar no socket da espada:
   deve dar *"That rune is not compatible with this item."* (esperado, não é bug).
5. **Desequipar** — clicar no socket ocupado **sem** runa selecionada: a runa volta ao
   inventário e o socket esvazia.
6. **Cancelar** — encaixar uma runa e sair do inventário com **Cancelar**: a runa deve voltar
   ao inventário e o socket voltar ao estado anterior.
7. **Anel/colar** — mesmo teste com os slots RING/NECKLACE (a correção vale para eles também).
8. **Efeito em batalha** — com a runa encaixada, entrar em batalha e conferir o efeito
   (ex.: `keen_edge` = +dano/velocidade de ataque corpo-a-corpo). Runas de arma só valem
   para a arma daquele slot.
9. **Save/load** — salvar com a runa encaixada, sair para o menu, carregar: o socket deve
   continuar preenchido. Depois **trocar a arma daquele slot** e reabrir o inventário: o hint
   deve dizer *"Inactive: the weapon in this slot changed."* e o bônus deve deixar de contar.
10. **Verificar o log** ao fim: `Documents\Mount and Blade II Bannerlord\Logs\SOTOR\latest.txt`
    aponta a sessão; procurar linhas `RuneSocket click` / `Rune ... socketed`.

---

## RODADA 2 — "Rune sockets are unavailable" no botão Equip vanilla

### Fluxo confirmado pelo autor

Ele clicou na runa, o **botão "Equip" vanilla apareceu** (o postfix em `get_IsEquipableItem`
funciona: a runa `Goods` é tratada como equipável) e clicou nele. Cadeia:

```
ItemVM.ExecuteEquipItem() → delegate estático ItemVM.ProcessEquipItem
  → SPInventoryVM.ProcessEquipItem  [nosso HarmonyPrefix]
    → InventoryMagicAccessorySlotsMixin.TryHandleVanillaEquip(__instance, item)
      → _instances.TryGetValue(inventoryVm, …)  ← FALHOU
        → ShowMessage("Rune sockets are unavailable.", vermelho)
```

Evidência do log da sessão 21:15 (`session_2026-08-10_21-15-55.log`): registries carregam
(72 runas) e **nenhuma** linha da instrumentação de clique de socket — coerente, porque
`TryHandleVanillaEquip` não tinha nenhum `SotorLog` e as três mensagens genéricas idênticas
("Rune sockets are unavailable.") deixavam o diagnóstico cego.

### Por que `_instances` podia estar vazio — o mecanismo real do UIExtenderEx

Decompilei o UIExtenderEx instalado (`ilspycmd`) para parar de supor:

- `UIExtenderRuntime.Register` → `ViewModelComponent.RegisterViewModelMixin(type, RefreshMethodName, HandleDerived)`.
- `RegisterViewModelMixin` → `ViewModelWithMixinPatch.Patch(harmony, SPInventoryVM, "RefreshValues")`,
  que **transpila todos os construtores declarados** do VM inserindo, antes de cada `ret`,
  uma chamada a `ViewModelWithMixinPatch.Constructor` → `ViewModelComponent.InitializeMixinsForVMInstance(vm)`.
- `InitializeMixinsForVMInstance` só instancia o mixin se:
  1. `Mixins.TryGetValue(instance.GetType(), …)` — **casamento EXATO de tipo** (`HandleDerived == false`);
  2. `_mixinTypeEnabled[mixinType] == true` (depende de `Enable()` ter rodado antes da tela abrir);
  3. `Activator.CreateInstance(mixinType, instance)` não lançar.
- `ViewModelWithMixinPatch.Patch` só aplica o patch de construtor se
  `ViewModelInitializations.TryAdd(viewModelType, null)` — e esse dicionário é **estático e
  compartilhado entre todos os runtimes/módulos**.
- `Finalize` (transpilado em `SPInventoryVM.OnFinalize`) chama o `OnFinalize` do mixin, que
  **removia a entrada de `_instances`**.

Ou seja: o mixin nasce dentro do construtor do VM e a entrada no dicionário era o **único**
elo entre o patch estático de equip e todo o estado do sistema. Qualquer falha nessa cadeia
— ou um `OnFinalize` numa tela que continua viva — derrubava o equip inteiro.

Descartado por verificação direta:
- `TaleWorlds.Library.ViewModel` e `PropertyOwnerObject` **não** sobrescrevem
  `Equals`/`GetHashCode` (`MEGA_012.md:18344`, `MEGA_011.md:133500`) → o lookup era por
  referência, como se esperava.
- **Nenhum outro módulo carregado referencia `SPInventoryVM`** (grep binário em
  `RF_Map`, `RF_Races`, `RealmsForgotten`, `RF_Core_II/III`, `RF_Extension`, `CCsBanners`,
  `RF_DualWield`, `NavalDLC`): só `RF_Magic/SOTOR.dll`. Logo não há tipo derivado quebrando
  o casamento exato.

### Segunda falha, independente e provada por leitura do vanilla

`SPInventoryVM.OnFinalize()` termina com **`_inventoryLogic = null;`**
(`Vanilla_1.3.x/MEGA_009.md:86947`). O mixin **cacheava** `_inventoryLogic` no construtor:

```csharp
_inventoryLogic = GetPrivate<InventoryLogic>("_inventoryLogic");   // uma única vez, no ctor
```

Como o ctor do mixin roda **dentro** do ctor do VM (transpiler), qualquer divergência de
ordem ou qualquer reaproveitamento de VM após `OnFinalize` deixava esse cache nulo — e aí
**todos** os caminhos caíam nas mesmas mensagens genéricas, inclusive
`ExecuteRuneSocket` (linha 384 original), que abortava **antes** da instrumentação da
Rodada 1. Isso explica sozinho "mensagem vermelha + zero linhas de log".

### O que foi corrigido na Rodada 2

Mesmo arquivo (`InventoryMagicAccessorySlotsMixin.cs`):

1. **`ResolveMixin(vm, caller)`** substitui o `_instances.TryGetValue` cru no caminho do
   Equip vanilla. Três degraus, todos logados:
   `_instances` → `_lastMixin` (WeakReference ao último mixin vivo, aceito só se
   `ReferenceEquals(mixin.ViewModel, vm)`, e reinserido no dicionário) →
   **criação sob demanda** (`new InventoryMagicAccessorySlotsMixin(vm)`, que se auto-registra).
   O log imprime o hash dos **dois** VMs quando há divergência.
   → O botão Equip agora completa o socketing mesmo se o UIExtenderEx não tiver instanciado
   o mixin. **Não** degradei para mensagem instrutiva, como pedido.
2. **`_inventoryLogic` deixou de ser cacheado**: virou a propriedade
   `Logic => GetPrivate<InventoryLogic>("_inventoryLogic")`, lida do VM vivo em toda
   operação. O campo remanescente (`_subscribedLogic`) existe só para casar
   `AfterReset += / -=`.
3. **`_instances` com `IEqualityComparer` de referência explícito** (`RuntimeHelpers.GetHashCode`),
   para o dicionário nunca depender de a TaleWorlds não sobrescrever `GetHashCode`.
4. **Construtor do mixin blindado**: `RefreshSlots()` inicial em `try/catch`. Um throw ali
   destruiria a tela de inventário inteira (o ctor roda dentro do ctor do VM). O `catch` de
   `ResolveMixin` ainda tenta recuperar a instância parcial que o ctor já registrou.
5. **Mensagens específicas** (fim da mensagem genérica única):
   `"…unavailable (no inventory mixin)."`, `"…unavailable (no hero selected)."`,
   `"…unavailable (inventory closing)."`, `"Switch to the Battle tab to socket runes."`
6. **Instrumentação de todos os caminhos**: criação e finalização do mixin (com hash do VM,
   estado do logic e tamanho de `_instances`), interceptação do equip vanilla, cada clique de
   socket (hash do VM, herói, logic, modo batalha), qual gate recusou, ids das 4 armas
   quando nenhuma é compatível, e sucesso do socketing.

Build: **0 erros** (31 warnings pré-existentes). Redeploy pelo PostBuild em
`Modules/RF_Magic/bin/Win64_Shipping_Client/SOTOR.dll`. Nada commitado.

### O que o autor testa agora (os DOIS fluxos)

**Fluxo A — botão Equip vanilla** (o que ele usou):
1. Inventário → **aba Batalha**, com uma arma compatível no slot 1 (ex.: espada para runa Melee).
2. Clicar na runa → aparece o botão **Equip** → clicar em Equip.
3. Esperado: a runa vai para o primeiro socket compatível vazio, some do inventário, e o log
   registra `Vanilla equip intercepted…` seguido de `Rune '…' socketed on '…'`.

**Fluxo B — clique no socket**:
1. Mesma tela. Clicar **uma vez** na linha da runa (destaca).
2. Clicar no quadradinho "R" do socket da arma.
3. Esperado: mesmo resultado; log com `RuneSocket click slot=…` e `RuneSocket slot=… selected='rf_rune_…'`.

Depois, em qualquer um dos dois: incompatibilidade (runa de arco em espada → recusa),
desequipar (clicar no socket sem seleção), **Cancelar** (deve reverter), anel/colar, efeito
em batalha, e salvar/carregar + trocar a arma do slot (hint deve virar *"Inactive: the weapon
in this slot changed."*).

**Se ainda falhar**, o log agora responde qual elo quebrou — mandar
`Documents\Mount and Blade II Bannerlord\Logs\SOTOR\` (a sessão apontada por `latest.txt`),
filtrando por `MagicAccessories`, `ResolveMixin`, `RuneSocket` e `Vanilla equip`.

---

## RODADA 3 — causa raiz REAL: `BaseViewModelMixin.GetPrivate` do UIExtenderEx está quebrado

### O log do autor (session_2026-08-10_21-44-18.log)

```
21:48:46 [Info]  MagicAccessories mixin created for SPInventoryVM#52014770 (logic=NULL, instances=1).
21:50:33 [Info]  Vanilla equip intercepted for rune 'rf_rune_piercing_greater' (SPInventoryVM#52014770, side=OtherInventory).
21:50:33 [Error] EquipRuneAutomatically: SPInventoryVM._inventoryLogic nulo (VM#52014770 — tela ja finalizada?).
```

O mixin **nasce** com `logic=NULL`, a VM é a **mesma** do início ao fim, e a leitura "viva"
por reflexão continuou nula. Ou seja: nem instância stale, nem finalização. A reflexão
simplesmente nunca funcionou.

### O campo mudou de nome na 1.4.8? **NÃO — e nunca mudou.**

Resposta direta à pergunta do relatório. Decompilei a DLL **instalada** com `ilspycmd`:

`<jogo>\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.ViewModelCollection.dll`,
tipo `TaleWorlds.CampaignSystem.ViewModelCollection.Inventory.SPInventoryVM`:

```csharp
private InventoryLogic _inventoryLogic;   // linha 113 do decompilado 1.4.8
private CharacterObject _currentCharacter;
private SPItemVM _selectedItem;           // linha 117
```

Os nomes estão **corretos e idênticos** ao 1.3.x. A hipótese do rename está descartada.

### A causa raiz verdadeira

O culpado é o helper do próprio UIExtenderEx. Decompilando
`Modules\Bannerlord.UIExtenderEx\bin\Win64_Shipping_Client\Bannerlord.UIExtenderEx.dll`:

```csharp
// Bannerlord.UIExtenderEx.ViewModels.BaseViewModelMixin<TViewModel>
private readonly WeakReference<TViewModel> _vm;

protected TValue? GetPrivate<TValue>(string name)
{
    return _vm.PrivateValue<TValue>(name);   // <<< 'this' da extension é o WeakReference!
}
```

```csharp
// Bannerlord.UIExtenderEx.Extensions.ReflectionHelpers
public static T? PrivateValue<T>(this object? o, string fieldPropertyName)
{
    if (o == null) return default(T);
    var members = _fieldPropertyCache.GetOrAdd(o.GetType(),
        x => x.GetProperties().OfType<MemberInfo>().Concat(x.GetFields()).ToDictionary(m => m.Name, r => r));
    if (!members.TryGetValue(fieldPropertyName, out var value))
        return default(T);        // <<< NOME NÃO ENCONTRADO => null, SEM ERRO, SEM LOG
    ...
}
```

`PrivateValue` é uma extension de **`object`**, e é chamada em **`_vm`** — o
`WeakReference<SPInventoryVM>` — **não** no ViewModel. Logo `o.GetType()` é
`WeakReference<SPInventoryVM>`, cujos membros são `IsAlive`/`Target`/`TrackResurrection`.
Nem `_inventoryLogic` nem `_selectedItem` existem lá → **`default(T)` = null, silenciosamente,
sempre, desde o construtor**. Isso explica exatamente `logic=NULL` no ctor e nas 3 leituras
seguintes.

Corolário incômodo: `GetPrivate<SPItemVM>("_selectedItem")` **também sempre devolveu null**.
O ramo de hover do `GetSelectedInventoryItem()` era código morto desde o início — a análise
da Rodada 1 sobre `ItemHoverEnd`/`OnEmptyClick` continua correta e a correção do sticky
continua necessária (ela não usa `GetPrivate`), mas o hover nunca chegou nem a ser lido.

### O que foi corrigido na Rodada 3

1. **`GetPrivate` proibido neste sistema.** Não há mais nenhuma chamada a ele.
2. **`Logic` usa a porta oficial**, verificada no binário 1.4.8:
   `InventoryState.InventoryLogic` é propriedade **pública** de
   `TaleWorlds.CampaignSystem.GameState.InventoryState`, e é o mesmo objeto que o
   `SPInventoryVM` recebe no construtor (o próprio VM usa
   `InventoryScreenHelper.GetActiveInventoryState()` no ctor). Leio via
   `GameStateManager.Current?.ActiveState as InventoryState` para **evitar o
   `Debug.FailedAssert`** que `GetActiveInventoryState()` dispara quando o estado ativo não é
   o de inventário.
   *Último recurso*: `FieldInfo` estático sobre `typeof(SPInventoryVM)` com o nome
   `_inventoryLogic` **conferido na DLL instalada**, aplicado à instância real do ViewModel.
3. **`_selectedItem`** passou a ser lido pelo mesmo `FieldInfo` estático verificado.
4. **Canário de rename**: se qualquer um dos 4 membros refletidos (`_inventoryLogic`,
   `_selectedItem`, `ActiveEquipment`, `AfterTransfer`) não for encontrado, sai **um
   `[Error]` explícito** no log em vez de null silencioso. Foi a ausência disso que custou
   duas rodadas.
5. **`side=OtherInventory`** — investigado e o gate foi **mantido de propósito**. Semântica
   confirmada no binário 1.4.8: a lista da direita do inventário do jogador é construída com
   `InventorySide.PlayerInventory`; `OtherInventory` é sempre o roster do outro lado
   (mercador, pilhagem, estoque alheio). Encaixar uma runa a **consome** do roster de origem
   (`TransferCommand` com `from = PlayerInventory`), então aceitar `OtherInventory` seria
   pegar item de mercador **de graça** — exploit. O que mudou é a clareza: novo helper
   `IsRuneUsableFromSide` com mensagens específicas e log do lado:
   - `OtherInventory` → *"That rune is not yours yet - move it into your own inventory first."*
   - lados de equipamento → *"Runes must be socketed from your inventory, not from an equipment slot."*

   **Isto explica o teste do autor**: a runa `rf_rune_piercing_greater` dele estava do lado
   "outro" (`side=OtherInventory`). Mesmo com o `InventoryLogic` consertado, aquele clique
   específico teria sido recusado — corretamente. Ele precisa **transferir a runa para o
   inventário dele** antes.

### Build e deploy

`dotnet build RF_Magic.csproj` → **0 Error(s)**, nenhum warning CS (os warnings restantes são
MSB3246 de DLLs nativas, pré-existentes). DLL compilada em
`RF_Magic\bin\Debug\net472\SOTOR.dll` (21:59), com as strings novas conferidas no binário.

⚠ **Deploy PENDENTE**: o `TaleWorlds.MountAndBlade.Launcher` (PID 14100) e o `Watchdog`
(PID 3024) estavam abertos e mantinham `Modules\RF_Magic\...\SOTOR.dll` travado
(`MSB3027/MSB3021`). Não matei os processos do autor. Para concluir: **fechar o launcher** e
rodar `dotnet build RF_Warsails_AI\RF_Magic\RF_Magic.csproj` (o PostBuild copia sozinho).
A DLL no módulo é ainda a das 21:42 (Rodada 2).

### O que o autor testa (Rodada 3)

Pré-requisito novo e importante: **a runa tem de estar no inventário DELE** (lista da direita),
não na lista do mercador/pilhagem. Se ela estiver à esquerda, transferir primeiro.

1. Fechar o launcher, rebuildar (deploy), abrir o jogo.
2. Inventário → aba **Batalha**, arma compatível no slot 1, **runa no lado do jogador**.
3. **Fluxo A**: clicar na runa → botão **Equip**.
   **Fluxo B**: clicar uma vez na runa → clicar no socket "R".
4. Log esperado agora: `mixin created ... (logic=ok, ...)` — se ainda vier `logic=NULL`, o
   canário de rename dirá qual membro sumiu.
5. Se a runa estiver do lado errado, a mensagem agora é explícita
   (*"That rune is not yours yet…"*) em vez de genérica.

---

## RODADA 4 — o ícone da runa não aparecia no socket

Equip funcionando desde a Rodada 3. Relato: *"o ícone da runa não aparece nos slots quando
equipadas; elas desaparecem em vez de aparecerem nos slots"* — a runa é consumida do
inventário (estado gravado OK) mas o socket continua vazio.

### Como o socket renderiza — e os dois erros

O prefab injetado **já tinha** um `ImageIdentifierWidget` ligado, então não faltava binding.
Faltava que ele estivesse **certo**. Duas causas, ambas verificadas no binário/prefab 1.4.8:

**Erro 1 — `IsVisible` no contexto errado.** O widget era:
```xml
<ImageIdentifierWidget DataSource="{Image}" ... ImageId="@Id" ... IsVisible="@Occupied" />
```
Quando um widget declara `DataSource`, **todos** os `@Prop` dele passam a resolver contra esse
novo data source. Aqui o data source é o `ItemImageIdentifierVM`, e o decompilado da 1.4.8
(`TaleWorlds.Core.ViewModelCollection.dll`, `ImageIdentifierVM`) mostra que ele expõe como
`[DataSourceProperty]` **apenas** `Id`, `AdditionalArgs` e `TextureProviderName`. Não existe
`Occupied`. Binding morto. O mesmo erro estava nos slots de anel e colar
(`IsVisible="@MagicRingSlotOccupied"` / `@MagicNecklaceSlotOccupied`).

**Erro 2 — e era inútil de qualquer forma.** `ImageIdentifierWidget`
(`TaleWorlds.MountAndBlade.GauntletUI.Widgets.dll`, 1.4.8):
```csharp
private void RefreshVisibility()
{
    if (HideWhenNull) base.IsVisible = !string.IsNullOrEmpty(ImageId);
    else              base.IsVisible = true;
}
```
chamado nos setters de `ImageId`, `AdditionalArgs`, `IsBig` e `HideWhenNull`. **O widget manda
no próprio `IsVisible`** — qualquer `IsVisible` do XML é sobrescrito. O mecanismo correto para
esconder quando vazio é `HideWhenNull="true"` (usado em 15 lugares nos prefabs vanilla).

**Erro 3 — a causa real de o ícone nunca aparecer: `Image = null`.**
```csharp
Image = runeItem == null ? null : new ItemImageIdentifierVM(runeItem, string.Empty);
```
Com o socket vazio, `Image` era **null** → `DataSource="{Image}"` ficava sem data source. Ao
encaixar a runa, `Image` passava a não-nulo, mas o binding de `ImageId` não voltava a ser
alimentado. O vanilla nunca faz isso: slots de equipamento vazios também carregam um
`ItemImageIdentifierVM`, só com `Id` vazio — confirmado em
`ItemImageIdentifier(ItemObject item, string bannerCode)`:
```csharp
base.Id = item?.StringId ?? "";
base.TextureProviderName = "ItemImageTextureProvider";
```
Ou seja: `new ItemImageIdentifierVM(null)` é o estado "vazio" legítimo, e é o `HideWhenNull`
que o esconde.

### O que foi corrigido na Rodada 4

1. **Prefab** (`InventoryMagicAccessorySlotsExtension.cs`) — nos **6** `ImageIdentifierWidget`
   (4 sockets + anel + colar): removido `IsVisible="@…"`, adicionado `HideWhenNull="true"`.
   Passa a ser exatamente o idioma vanilla de item→imagem do 1.4.8 instalado
   (`SandBox/GUI/Prefabs/Inventory/InventoryItemTuple.xml`, `ImageIdentifierWidget` sem
   `LoadingIconWidget`). Deliberadamente **sem** `Standard.CircleLoadingWidget`, para não
   depender de resolução de prefab externo dentro do XML injetado — se essa resolução
   falhasse, os sockets inteiros deixariam de ser criados.
   XML validado por parser após o patch.
2. **`MagicRuneSocketVM.Refresh`** — `Image` nunca mais é nulo:
   `Image = new ItemImageIdentifierVM(runeItem, string.Empty)` (runeItem pode ser null).
   `Occupied` continua vindo de `runeItem != null` e ainda controla o "R" do placeholder
   (esse `IsVisible="@Empty"` está no contexto certo: o `TextWidget` herda o DataSource do
   `ButtonWidget`, que é o socket VM).
3. **`RefreshSlot`** (anel/colar) — mesma correção; `occupied` passou a ser um `out bool`
   derivado do **item**, não de "a imagem é nula".
4. **Log novo**: `RefreshRuneSlot slot=N target=… rune=… occupied=… matches=…` (nível Debug) e
   um `[Warn]` explícito se o herói tem um id de runa salvo que o `MBObjectManager` não conhece
   (xml de itens renomeado/removido) — antes esse caso daria socket vazio sem explicação.

### Caminhos de refresh conferidos (os quatro que importam)

| Situação | Onde chama `RefreshSlots()` | OK |
|---|---|---|
| Equip via socket | `EquipSelectedRune` (fim) | ✔ |
| Equip via botão vanilla | `EquipRuneAutomatically` → `EquipSelectedRune` | ✔ |
| Desequipar | `UnequipRune` / `UnequipAccessory` (fim) | ✔ |
| Abrir inventário / carregar save | `OnRefresh()` a cada `RefreshValues` | ✔ |
| Cancelar | `OnInventoryReset` (fim) | ✔ |

### Build e deploy

`dotnet build RF_Magic.csproj` → **0 Error(s)**; só os 2 warnings CS9191 pré-existentes.
**Deploy CONCLUÍDO**: o launcher já estava fechado, o PostBuild copiou —
`Modules\RF_Magic\bin\Win64_Shipping_Client\SOTOR.dll` às **22:26**, com as strings novas
(`HideWhenNull`, `RefreshRuneSlot slot=`) conferidas no binário implantado.

### O que o autor testa (Rodada 4)

1. Equipar (qualquer um dos dois fluxos) → **o ícone da runa aparece no socket**.
2. Desequipar → o ícone some e o "R" volta.
3. Fechar e reabrir o inventário → o ícone persiste (via `OnRefresh`).
4. Salvar / carregar → o ícone persiste.
5. Bônus: anel e colar agora também devem mostrar ícone (tinham o mesmo bug).

---

## RODADA 5 — arte por família + cor por tier

> ⛔ **A conclusão da Entrega 1 abaixo está ERRADA — ver Rodada 7.** Os 24 meshes **existem**;
> minha varredura tinha um furo de escopo. A Entrega 2 (cor por tier) permanece válida.

### Entrega 1 — reuso de meshes por família: **NÃO APLICADA (bloqueada por achado)** ← *conclusão incorreta, corrigida na Rodada 7*

O mapeamento foi calculado e aplicado, depois **revertido**. Motivo: os 24 meshes "customizados"
de runa **não existem em nenhum asset package instalado**. Trocar os 48 `silver_ore` por eles
transformaria 48 ícones que hoje funcionam em 48 itens **invisíveis** — exatamente o risco que
o próprio pedido mandava checar.

**Como verifiquei** (3 varreduras independentes, com controle positivo):

| Varredura | Escopo | Resultado |
|---|---|---|
| 1 — Python, string match em tpac | **259 `.tpac`, todos os 16 módulos** | 24 meshes de runa: **nenhum encontrado**. `silver_ore`: encontrado em `Native` ✔ |
| 2 — grep, módulos vanilla | Native, SandBox, NavalDLC, CCsBanners, BannerEditor | 24: nenhum. Controle `bd_sturgia_statue` (usado pelos nossos acessórios): encontrado em `Native` ✔ |
| 3 — grep, todo nome contendo "rune" | RF_Core_II, RealmsForgotten, RF_Extension, Native | Nos pacotes RF: **nenhum nome de mesh com "rune"**. Em Native só fragmentos aleatórios (`_acegilnrune`, `dkrunegow`) que não são nomes de mesh |

Os dois controles positivos provam que o método **acha** nome de mesh dentro de `.tpac`. Portanto
a ausência é real, não falso-negativo.

**Consequência que o autor precisa saber (bug pré-existente, não causado por nós):** os **24
itens de runa que já apontam para esses meshes hoje** (todos os `_lesser` com arte própria, mais
`impact_greater`, `piercing_greater`, `flame_greater`, `frost_greater`, `bulwark_greater`,
`arcane_greater`, `reservoir_greater`, `executioner_greater`, `vampiric_greater`) estão
apontando para arte inexistente. A arte foi autorada em `RF_Magic_runas/` como **SVG/PNG**
(memória do projeto: "Runas geradas por script, SVG vetorial 1024²") mas **nunca foi importada
como mesh para um `.tpac`**. O comentário no topo do XML ("the new set is Lesser, the original
elemental set is Greater") descreve um **plano**, não o estado atual dos assets.

Duas saídas, e a escolha é do autor (não decidi por ele):
- **(A)** importar os 24 meshes para um AssetPackage (ex.: `RF_Magic/AssetPackages`, hoje vazio)
  e então aplicar a tabela abaixo — é o caminho que ele pediu;
- **(B)** se a arte não vai ser importada agora, o oposto: apontar os 24 para `silver_ore`
  também, para pararem de ficar invisíveis.

**Tabela família→mesh (a especificação, pronta para aplicar depois da importação):**

| família | lesser | greater | ancient |
|---|---|---|---|
| sundering | rune_sundering | rune_sundering | rune_sundering |
| impact | rune_impact | iceberg_rune | iceberg_rune |
| keen_edge | rune_keen_edge | rune_keen_edge | rune_keen_edge |
| piercing | rune_forge | karthradium_rune | karthradium_rune |
| executioner | dark_rune | dark_rune | dark_rune |
| vampiric | necromancer_rune | necromancer_rune | necromancer_rune |
| **precision** | rune_far_sight | rune_far_sight | rune_far_sight |
| **wind** | rune_storm | rune_storm | rune_storm |
| windlass | rune_windlass | rune_windlass | rune_windlass |
| far_sight | rune_far_sight | rune_far_sight | rune_far_sight |
| huntsman | rune_huntsman | rune_huntsman | rune_huntsman |
| returning | rune_returning | rune_returning | rune_returning |
| flame | fire_rune | fire_rune | fire_rune |
| frost | ice_rune | ice_rune | ice_rune |
| storm | rune_storm | rune_storm | rune_storm |
| explosive | rune_explosive | rune_explosive | rune_explosive |
| bulwark | earth_rune | earth_rune | earth_rune |
| reprisal | rune_reprisal | rune_reprisal | rune_reprisal |
| mirror | rune_mirror | rune_mirror | rune_mirror |
| lightness | rune_lightness | rune_lightness | rune_lightness |
| arcane | cosmic_rune | cosmic_rune | cosmic_rune |
| focus | rune_focus | rune_focus | rune_focus |
| reservoir | life_rune | life_rune | life_rune |
| echo | rune_metamorfosis | rune_metamorfosis | rune_metamorfosis |

Regra usada: cada família fica com a **sua** arte nos 3 tiers; onde existiam duas artes
(impact, piercing) o `ancient` herda a mais ornamentada (a do greater). As duas famílias sem
arte nenhuma **emprestam** por tema, documentado:
- **precision** (reduz dispersão de arco/besta) ← `rune_far_sight`: é a arte da família de
  precisão à distância, exatamente a sugestão do pedido;
- **wind** (velocidade de projétil, `Bow|Thrown|Ammunition`) ← `rune_storm`: o único motivo
  "aéreo" do conjunto (tempestade/vento).

O XML `rf_magic_rune_items.xml` está **byte-equivalente ao original** (48 `silver_ore`,
XML validado). Nenhum item não-runa foi tocado — na verdade esse arquivo só contém runas.

### Entrega 2 — cor por tier no box do inventário: **APLICADA**

**Não existe rota nativa por dados** — verificado: `ItemQuality` existe em `TaleWorlds.Core`
mas **não é referenciado** por `TaleWorlds.MountAndBlade.GauntletUI.Widgets`; a lista do
inventário não coloriza por qualidade/`ItemModifier`, e não há `modifier_group` que tinja a linha.

**Não dá para tingir o brush da tuple**, por dois motivos verificados no binário 1.4.8:
1. `InventoryItemTupleWidget.UpdateEquipmentTypeState()` **reatribui** `MainContainer.Brush`
   (Default / CantUseInSet / CharacterCantUse);
2. `BrushWidget.OnRender` desenha via `BrushRenderer` e **ignora `Widget.Color`**.
   Tingir exigiria mutar o `Brush` — que vem de `DefaultBrush="!Inventory.Tuple"`, um objeto
   **compartilhado por todas as tuples**: mutar tingiria a lista inteira (mesma classe de erro
   de mutar um `ItemObject` compartilhado).

**Solução aplicada** (`RF_Magic/SOTOR.MagicAccessories/InventoryRuneTierTint.cs`, novo):
- Um `Widget` **simples** nosso, injetado como primeiro filho de `MainControls` na tuple.
  `Widget.OnRender` faz `simpleMaterial.Color = Color` e a cor **não propaga para os filhos** —
  logo tinge só o retângulo de fundo do box e **nunca** o tableau/arte da runa, como o autor pediu.
- Sprite `BlankWhiteSquare_9` (existe em `NativeSpriteData.xml`, usado 453× nos prefabs vanilla),
  `AlphaFactor="0.30"`.
- Injetado em `MainControls` e **não** em `Main`: `Main` é um `BrushListPanel` (ListPanel) e um
  filho novo entraria no stack layout, empurrando o conteúdo da linha para o lado.
- `InsertType.Child` + `Index=0` + XPath terminando em `/Children`, porque no `PrefabComponent`
  do UIExtenderEx `Child` faz `InsertAsChild(node, novo, Index)` dentro de `node.ChildNodes`,
  enquanto `Prepend`/`Append` inserem como **irmão** (`InsertBefore`/`InsertAfter`).

**Reciclagem resolvida pela arquitetura, não por limpeza manual**: a cor vem de um
`[ViewModelMixin]` no **`SPItemVM`** (`RfIsRuneItem`, `RfRuneTierColor`), com propriedades
**computadas** (sem cache). Como o valor pertence ao VM daquele item, quando a lista
virtualizada reaproveita a tuple o `DataSource` troca e o binding recalcula — não existe estado
de widget para vazar para um item não-runa.

Paleta: `lesser` = bronze `0xFFCD7F32`, `greater` = prata `0xFFD8D8DC`, `ancient` = dourado
`0xFFFFD24A` (`uint` ARGB, formato de `Color.FromUint`, o mesmo que os bindings `Color="@Color"`
do vanilla usam).

**Bônus pedido**: os nossos 4 sockets também ganharam o fundo tingido por tier
(`MagicRuneSocketVM.TierColor` + `Widget` de tint por socket, `AlphaFactor="0.45"`), com a
mesma paleta. O ícone da runa continua intocado.

### Build e deploy

`dotnet build RF_Magic.csproj` → **0 Error(s)**. Deploy **concluído** (jogo fechado):
`Modules\RF_Magic\bin\Win64_Shipping_Client\SOTOR.dll` às **08:54**, com
`RFRuneTierTint`, `RfRuneTierColor`, `RFSocketTierTint`, `InventoryItemTuple`, `MainControls` e
`BlankWhiteSquare_9` conferidos no binário implantado. Os dois XML injetados foram validados por
parser antes do build.

### O que o autor testa (Rodada 5)

1. Abrir o inventário com runas de tiers diferentes na lista: o **box** de cada linha de runa
   deve ter fundo bronze (lesser) / prata (greater) / dourado (ancient). Itens não-runa ficam
   inalterados.
2. **Rolar a lista para cima e para baixo várias vezes** com runas e não-runas misturadas — é o
   teste da reciclagem: nenhuma linha não-runa pode ficar tingida.
3. Trocar de personagem e de aba (Batalha/Civil) e conferir que o tint acompanha o item certo.
4. Encaixar runas de tiers diferentes: o fundo do socket deve pegar a cor do tier; o ícone
   continua na cor natural.
5. Se 0.30 de alpha estiver forte ou fraco demais, é um número só em
   `InventoryRuneTierTint.cs` (`AlphaFactor`) — e a paleta são 3 constantes no mesmo arquivo.
6. **Arte**: nada mudou nos meshes. Os 48 `silver_ore` continuam ore e os 24 com mesh próprio
   continuam sem arte importada — decidir entre (A) importar os meshes ou (B) apontar tudo para
   `silver_ore`.

---

## RODADA 6 — ícone de runa de verdade, via sprite de UI (Rota A)

### Achado que corrige a premissa: `RF_Magic_runas/` NÃO é arte de ícone de runa

Inventário completo da pasta (9 arquivos):

| arquivo | o que é |
|---|---|
| `runa_A_ordem.svg`, `runa_B_organica.svg`, `runa_C_sombria.svg` | 3 **direções de design** do círculo de conjuração, 1024×1024 |
| `rf_targeting_runes_A_1024.png` | textura do **marcador de mira no chão** |
| `rf_runes_A_v1_icc.png` / `v2_rgbvisivel` / `v3_semalpha` | variações de export do mesmo atlas 1024² |
| `_preview.html`, `_preview_atlas_A.png` | preview |

O próprio `_preview.html` diz, textualmente: **"Runa de conjuração — 3 direções"** e *"traço branco
sobre transparente (aqui com brilho azul só para simular o jogo, que tinge por código)"*. Abrindo
os SVG: são círculos concêntricos + heptagramas + ticks radiais — **círculo mágico de mira**, a
feature do `AbilityCrosshair`/`circular_targeting_rune`, não ícone de item.

Ou seja: **não existe arte de runa por família em nenhum lugar do projeto** — nem como mesh
(Rodada 5), nem como PNG. `RF_Magic_icons_para_trocar/` (≈130 PNG) são ícones de **feitiço** e
símbolos de escola; nenhum nome corresponde às 24 famílias.

Como o pedido foi "use os assets de runas que já estão lá" e a instrução era **não inventar
arte**, usei os assets de magia que **de fato existem e já estão registrados**, mapeados por tema.

### Rota escolhida: **A (sprite de UI)** — e ela já estava provada dentro do próprio RF_Magic

Não precisei descobrir nada por tentativa: o módulo **já tem** a infraestrutura funcionando
(é o que faz a arte do grimório aparecer):

- `RF_Magic/GUI/RF_MagicSpriteData.xml` — declara as `SpriteCategory` com **`<AlwaysLoad />`**
  (`ui_rf_scroll`, `ui_sotor` 4096×4096, `ui_sotor_perks`) e lista **159 `<SpritePart>`**;
- `RF_Magic/GUI/SpriteParts/Config.xml` — repete as categorias com `<AlwaysLoad/>`;
- os PNG ficam soltos em `GUI/SpriteParts/ui_sotor/` (**140 arquivos**).

Três consequências verificadas no binário/prefabs 1.4.8:
1. `AlwaysLoad` ⇒ a categoria está carregada em **qualquer** tela, inclusive o inventário;
2. `Sprite="@Prop"` **é bindável** — 56 usos nos prefabs vanilla (`@SpriteName`, `@IconPath`,
   `@Sprite`, `@FileName`…);
3. os 24 sprites que escolhi estão **todos declarados** em `<SpriteParts>` (24/24 conferidos) e
   são **todos 256×256** (quadrados, conferido por leitura do header PNG).

**Rota B (mesh/tpac) não foi necessária** e nenhum passo manual no editor é exigido.

### Tabela família → sprite (a decisão de arte, toda revisável)

Cada entrada é **uma string** em `MagicRuneIcons.SpriteFor(MagicRuneEffect)`
(`RF_Magic/SOTOR.MagicAccessories/InventoryRuneTierTint.cs`). O mapeamento usa o enum
`MagicRuneEffect`, que tem exatamente as 24 famílias.

| família | sprite | família | sprite |
|---|---|---|---|
| sundering | plagueofrust_icon | flame | fireball_icon |
| impact | wind_blast_icon | frost | iceshardblizzard_icon |
| keen_edge | quicksilversword_icon | storm | chainlightning_icon |
| piercing | deadlyshards_icon | explosive | cinderblast_icon |
| executioner | taste_of_death_icon | bulwark | shield_of_saphery_icon |
| vampiric | drainlife_icon | reprisal | shield_of_thorns_icon |
| precision | gleamingarrow_icon | mirror | resistanceaura_icon |
| wind | chillwind_icon | lightness | phasprotection_icon |
| windlass | enchant_weapon_icon | arcane | harmonicconvergence_icon |
| far_sight | radiantgaze_icon | focus | traits_magic_icon |
| huntsman | beastunleashed_icon | reservoir | regrowth_icon |
| returning | bironastimewarp_icon | echo | finaltransmutation_icon |

Tiers continuam distinguidos pela **cor do box** da Rodada 5 (bronze/prata/dourado), como pedido.

### O que foi implementado

**Nos nossos sockets** (prefab é nosso, caminho limpo): `MagicRuneSocketVM` ganhou
`IconSprite` + `HasIconSprite`; o prefab ganhou um `Widget` com `Sprite="@IconSprite"`
`IsVisible="@HasIconSprite"`. Quando há sprite, `Refresh` passa **item nulo** para o
`ItemImageIdentifierVM` — `ImageId` fica vazio e o `HideWhenNull="true"` da Rodada 4 esconde o
tableau sozinho. Não tentei esconder o `ImageIdentifierWidget` por binding: ele manda no próprio
`IsVisible` (lição da Rodada 4).

**Na lista do inventário**: nova `InventoryRuneIconExtension` injeta, como **último** filho de
`MainControls` (`InsertType.Child`, `Index=999` ⇒ acima do thumbnail), um host de 111×51 com
`MarginLeft="!Inventory.Tuple.ThumbnailMargin"` — a **constante do próprio prefab**, que vale 1
no lado do jogador e 41 no outro, então alinha nos dois lados. Dentro dele: um retângulo opaco
`#12151AFF` que **mascara** o thumbnail vanilla, e o sprite quadrado 47×47 centralizado.
A máscara é necessária porque o `InventoryImageIdentifierWidget` da tuple não é nosso e não pode
ser escondido por binding.
O sprite vem do mixin em `SPItemVM` (`RfRuneSprite`, `RfHasRuneSprite`) — propriedades
**computadas**, então a reciclagem da lista virtualizada continua segura pelo mesmo motivo da
Rodada 5: o valor pertence ao VM do item.

Nada foi mexido em `rf_magic_rune_items.xml`: o `mesh=` segue como estava (o tableau é
mascarado/suprimido, então o mesh só importa se algum dia a arte 3D entrar).

### Build e deploy

`dotnet build RF_Magic.csproj` → **0 Error(s)**. Deploy **concluído** (jogo fechado):
`SOTOR.dll` às **09:12**, com `fireball_icon`, `RFRuneIconHost`, `IconSprite` e `RfRuneSprite`
conferidos no binário implantado. Os 3 XML injetados foram validados por parser antes do build.

### O que o autor testa (Rodada 6)

1. Inventário com runas na lista: cada família mostra **seu** ícone (fogo→bola de fogo,
   gelo→estilhaços, etc.), com o fundo do box na cor do tier. Itens não-runa inalterados.
2. **Rolar muito** a lista misturando runas e não-runas — teste da reciclagem (nenhuma linha
   comum pode ganhar ícone ou máscara).
3. Conferir o alinhamento do ícone **nos dois lados** (inventário do jogador e do mercador): a
   margem usa a constante do prefab justamente para isso.
4. Encaixar runa: o socket mostra o ícone da família; desequipar → volta o "R".
5. Se algum ícone não combinar com a família, é **uma string** na tabela de
   `MagicRuneIcons.SpriteFor` — há 159 sprites disponíveis em `ui_sotor` para escolher.
6. Se a máscara `#12151AFF` não casar com o fundo da linha, é um atributo no XML da
   `InventoryRuneIconExtension`.

### Lacunas de arte (para registro, nada inventado)

- **0 de 24** famílias têm arte própria de runa. Os 24 ícones acima são **empréstimos temáticos**
  de arte de feitiço já existente.
- Se o autor quiser identidade visual de "runa" de verdade, o caminho é autorar 24 PNG (256×256,
  alpha) e soltá-los em `GUI/SpriteParts/ui_sotor/` + adicionar um `<SpritePart>` para cada em
  `RF_MagicSpriteData.xml`; depois só trocar as 24 strings da tabela. **Nenhum código muda.**
- Os 3 SVG de `RF_Magic_runas/` seguem sendo do círculo de mira; não os usei como ícone porque
  são o mesmo desenho genérico para todas as famílias e, a 47×47, o traço fino de 1024² viraria
  borrão.

---

## RODADA 7 — os meshes existem; plano original aplicado; Rodada 6 revertida

### Mea culpa técnica: por que minha varredura falhou

Eu concluí, nas Rodadas 5 e 6, que os 24 meshes de runa não existiam. **Estava errado, e o autor
tinha razão: ele mesmo importou os assets.** A causa é banal e inteiramente minha:

> As três varreduras usaram o glob **`Modules/*/AssetPackages/*.tpac`**. Os assets de runa do
> autor não estão em `AssetPackages/` — estão em **`Modules/RealmsForgotten/Assets/magic/`** (e
> `Assets/magic/lesser_runes/`), uma árvore `Assets/` que meu padrão **nunca tocou**. Pior: eu
> tinha um controle positivo (`silver_ore` achado em `Native/AssetPackages`) e interpretei-o como
> "o método funciona", quando ele só provava que o método funciona **dentro do diretório que eu
> estava olhando**. Um controle positivo só valida o escopo em que roda. O certo era enumerar
> primeiro os diretórios de asset de cada módulo (`ls -d Modules/*/Asset*`) — que é exatamente o
> comando que, agora, mostrou `Assets`, `AssetPackages` e `RuntimeDataCache` lado a lado no
> RealmsForgotten. **Regra nova: nunca afirmar ausência de asset sem antes enumerar as pastas de
> asset existentes; e um controle positivo tem de rodar no MESMO escopo da afirmação negativa.**

### Inventário real dos meshes (string scan nas duas geometrias agregadas)

`RealmsForgotten/Assets/magic/runes_game_geo.tpac` (264.841 bytes) — **9 meshes**:
`cosmic_rune`, `dark_rune`, `earth_rune`, `fire_rune`, `ice_rune`, `iceberg_rune`,
`karthradium_rune`, `life_rune`, `necromancer_rune`

`RealmsForgotten/Assets/magic/lesser_runes/lesser_runes_game_geo.tpac` (535.790 bytes) —
**15 meshes**: `rune_explosive`, `rune_far_sight`, `rune_focus`, `rune_forge`, `rune_huntsman`,
`rune_impact`, `rune_keen_edge`, `rune_lightness`, `rune_metamorfosis`, `rune_mirror`,
`rune_reprisal`, `rune_returning`, `rune_storm`, `rune_sundering`, `rune_windlass`

**Total 24** — exatamente os 24 nomes que o XML já referenciava. Cruzei com os `_tex`/`_mtl`:
as 24 famílias têm geo + texturas (d/n/s) + material, 1:1, sem sobra nem falta.

**Resposta à pergunta em aberto: `precision` e `wind` realmente NÃO têm mesh próprio.** A lista
não estava truncada — são 24 meshes para 24 famílias, mas duas famílias têm **dois** meshes
(impact: `rune_impact` + `iceberg_rune`; piercing: `rune_forge` + `karthradium_rune`) e duas têm
**zero**. Também não têm textura nem material. Mantive o empréstimo temático documentado:
`precision ← rune_far_sight` (arte de precisão à distância) e `wind ← rune_storm` (único motivo
aéreo do conjunto).

### Entrega 1 — aplicada: 48 itens saíram de `silver_ore`

`rf_magic_rune_items.xml`: **48 itens alterados**, `silver_ore` restante = **0**, 24 meshes
distintos em uso, XML validado por parser. Só itens de runa (o arquivo contém apenas runas).

**Sanidade automática antes de gravar**: o script valida cada mesh do plano contra o conjunto
extraído das duas geo e, se algum faltasse, manteria `silver_one` naquele item e o listaria.
Resultado: `itens mantidos por mesh ausente: 0`, `todos existem nas geo? True`.

| família | lesser | greater | ancient |
|---|---|---|---|
| sundering | rune_sundering | rune_sundering | rune_sundering |
| impact | rune_impact | iceberg_rune | iceberg_rune |
| keen_edge | rune_keen_edge | rune_keen_edge | rune_keen_edge |
| piercing | rune_forge | karthradium_rune | karthradium_rune |
| executioner | dark_rune | dark_rune | dark_rune |
| vampiric | necromancer_rune | necromancer_rune | necromancer_rune |
| **precision** *(emprestado)* | rune_far_sight | rune_far_sight | rune_far_sight |
| **wind** *(emprestado)* | rune_storm | rune_storm | rune_storm |
| windlass | rune_windlass | rune_windlass | rune_windlass |
| far_sight | rune_far_sight | rune_far_sight | rune_far_sight |
| huntsman | rune_huntsman | rune_huntsman | rune_huntsman |
| returning | rune_returning | rune_returning | rune_returning |
| flame | fire_rune | fire_rune | fire_rune |
| frost | ice_rune | ice_rune | ice_rune |
| storm | rune_storm | rune_storm | rune_storm |
| explosive | rune_explosive | rune_explosive | rune_explosive |
| bulwark | earth_rune | earth_rune | earth_rune |
| reprisal | rune_reprisal | rune_reprisal | rune_reprisal |
| mirror | rune_mirror | rune_mirror | rune_mirror |
| lightness | rune_lightness | rune_lightness | rune_lightness |
| arcane | cosmic_rune | cosmic_rune | cosmic_rune |
| focus | rune_focus | rune_focus | rune_focus |
| reservoir | life_rune | life_rune | life_rune |
| echo | rune_metamorfosis | rune_metamorfosis | rune_metamorfosis |

### Entrega 3 — Rodada 6 revertida (arte real do autor > sprite emprestado)

- **Lista do inventário**: `InventoryRuneIconExtension` **removida** — fim da máscara opaca sobre
  o thumbnail e do ícone de feitiço. O `InventoryImageIdentifierWidget` vanilla volta a mostrar o
  tableau, que agora renderiza o mesh real da runa.
- **Mixin do `SPItemVM`**: `RfRuneSprite` / `RfHasRuneSprite` removidos (sem consumidor).
- **Sockets**: `IconSprite` / `HasIconSprite` removidos do `MagicRuneSocketVM` e os 4 widgets de
  sprite saíram do prefab. `Refresh` voltou a `new ItemImageIdentifierVM(runeItem, "")` — ou seja,
  o comportamento da Rodada 4, agora com mesh de verdade por trás.
- **`MagicRuneIcons`** ficou no código marcado **INATIVO**, como referência caso algum dia se
  queira um ícone 2D de UI. Nada o consome.
- **Mantido**: as cores por tier no box do inventário e no fundo dos sockets (Rodada 5, entrega 2)
  — aprovadas e ortogonais à arte.

Conferência no binário implantado: `RFRuneIconHost` ausente ✔, `RfRuneSprite` ausente ✔,
`RFRuneTierTint` presente ✔, `RFSocketTierTint` presente ✔. (A string `IconSprite` ainda aparece
na DLL, mas vem de `SOTOR/SotorStatItemVM.cs`, classe pré-existente do grimório, não do sistema
de runas — verificado por grep.)

### Build e deploy

`dotnet build RF_Magic.csproj` → **0 Error(s)**. Deploy **concluído** (jogo fechado): `SOTOR.dll`
às **09:22**. Os 2 XML injetados revalidados por parser; o prefab dos sockets ficou com 4 tints de
tier + 4 `ImageIdentifierWidget` e **zero** referência a sprite.

### O que o autor testa (Rodada 7)

1. Abrir o inventário: **cada família de runa mostra a arte 3D dela** (a que ele importou), em
   todos os 3 tiers. Nenhuma runa deve mostrar minério.
2. `precision` e `wind` mostram, respectivamente, a arte de `rune_far_sight` e `rune_storm` — é
   empréstimo consciente, porque essas duas famílias não têm mesh. Se ele quiser arte própria,
   importar 2 meshes e trocar 2 linhas da tabela.
3. Cor do box por tier continua: bronze / prata / dourado.
4. Encaixar runa: o socket mostra o tableau da arte real, com o fundo na cor do tier.
5. Se **alguma** runa aparecer sem ícone, é sinal de que aquele mesh não está no
   `RuntimeDataCache` do módulo — nesse caso o nome dela vira o dado do próximo passo.

---

## (e) Riscos remanescentes

1. **Não testado in-game** (eu não executo o jogo). Build limpo e o caminho lógico está
   fechado, mas a confirmação é do autor.
2. **Dependência de nome privado**: `ExecuteSelectItem` é público e existe na DLL 1.4.8.
   `_selectedItem`, `ActiveEquipment` e `AfterTransfer` continuam sendo lidos por reflexão
   (`_inventoryLogic` virou apenas último recurso, atrás da porta pública `InventoryState`).
   A partir da Rodada 3 isso **não é mais silencioso**: o canário no construtor emite
   `[Error]` nomeando o membro ausente.
2b. **REGRA NOVA, aprendida na Rodada 3**: nomes de membro privado deste sistema saem
   **exclusivamente** do binário instalado (`ilspycmd -t <tipo> <dll>`), nunca do dump
   `Vanilla_1.3.x/MEGA_*.md`. E qualquer helper de reflexão de terceiro (`GetPrivate` do
   UIExtenderEx é o caso) tem de ser lido antes de usar: esse devolve `default(T)` sem erro
   quando não acha o membro — e ele procura no `WeakReference`, não no ViewModel.
3. **`get_IsEquipableItem` (postfix)**: na prática **funciona** — o autor confirmou que o botão
   Equip aparece para runas. Ainda assim é um getter trivial de campo, candidato a *inlining*
   pelo JIT, e o postfix não dispara `OnPropertyChanged`. Se algum dia o botão Equip parar de
   aparecer sem mudança nossa, a correção robusta é um postfix no **construtor** de `SPItemVM`
   forçando `IsEquipableItem = true` para runas.
3b. **Causa-raiz da Rodada 2 não foi isolada em UMA das duas falhas**: a correção cobre as
   duas hipóteses vivas (instância ausente em `_instances` e `_inventoryLogic` cacheado nulo)
   porque ambas produzem exatamente o sintoma observado e nenhuma delas pode ser distinguida
   sem o log novo. A próxima sessão do autor decide: se aparecer
   `ResolveMixin(...): nenhum mixin ... Criando sob demanda`, era a instância; se não aparecer
   nada disso e o socketing funcionar, era o cache do `InventoryLogic`.
4. **Destaque visual perdido**: o `OnEmptyClick` do jogo ainda apaga o realce da linha
   selecionada no instante do clique no socket. Funcionalmente correto (usamos o sticky),
   mas visualmente o item "desselecciona". Cosmético.
4b. **Tableau de item é assíncrono**: o `ImageIdentifierWidget` pede o render
   (`Render Requested: rf_rune_…` aparece no rgl_log) e a textura chega alguns frames depois.
   Sem `LoadingIconWidget` (omitido de propósito, ver Rodada 4) o socket fica vazio nesse
   intervalo, sem spinner. Se incomodar, dá para adicionar o
   `Standard.CircleLoadingWidget` — mas isso passa a depender de o nome do prefab resolver
   dentro do XML injetado, e a falha ali derrubaria a criação dos sockets.
4e. ~~**Máscara do thumbnail na lista**~~ — **REMOVIDA na Rodada 7** (o overlay de sprite foi
   revertido; a lista voltou ao tableau vanilla). Texto original abaixo, para histórico:
   4e-hist. **Máscara do thumbnail na lista** (Rodada 6): o ícone de runa é desenhado **sobre** um
   retângulo opaco que cobre o thumbnail vanilla, porque o widget de imagem da tuple não é nosso
   e controla o próprio `IsVisible`. Se a TaleWorlds mudar a geometria do thumbnail (hoje
   111×51, `MarginLeft` pela constante `Inventory.Tuple.ThumbnailMargin`), a máscara sai de
   registro — apareceria como um retângulo desalinhado, não como crash.
4f. ~~**Ícones são empréstimo de arte de feitiço**~~ — **OBSOLETO na Rodada 7**: usa-se a arte
   real do autor. `MagicRuneIcons` segue no código, inativo. Texto original:
   4f-hist. **Ícones eram empréstimo de arte de feitiço** (Rodada 6): tematicamente coerentes, mas não são
   runas. Trocar é uma string por família; autorar 24 PNG resolve de vez, sem tocar código.
4c. ~~**Arte das runas — PIOR do que parecia**~~ — **RESOLVIDO na Rodada 7**: os 24 meshes
   existem em `RealmsForgotten/Assets/magic/`; os 72 itens agora usam o mesh da sua família.
   Restam só `precision` e `wind` sem mesh próprio (empréstimo temático documentado).
4d. **Tint da tuple depende de um mixin em `SPItemVM`**: o UIExtenderEx instancia um mixin por
   `SPItemVM`, e a lista do inventário cria centenas deles. O objeto é minúsculo (sem estado) e
   as propriedades são computadas, mas é o único ponto do sistema que escala com o tamanho do
   inventário. Se aparecer custo de abertura de inventário, é o primeiro suspeito.
5. **Runas escritas direto em `HeroExtendedInfo` fora do `InventoryLogic`**: se o jogador
   sair do inventário por um caminho que não emita `AfterReset(fromCancel: true)`, o estado
   fica aplicado. Comportamento herdado do desenho original (mesmo dos acessórios), não alterado.
6. **Duplicidade XML pré-existente** e sem relação: `rgl_log_9104.txt:5652` —
   `duplicate key sequence 'runefang_001_blade' for 'CraftingPiece_unique_attribute'`.
   É peça de crafting chamada "runefang", **não** faz parte deste sistema; vale corrigir em
   auditoria separada.
7. **Falha de patch alheia** que continua no log: `SotorGraveyardMountPatch` não consegue
   patchear `Mission::SpawnTroop` (assinatura de 15 args mudou no 1.4.8). Fora de escopo aqui.
