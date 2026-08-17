using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SOTOR.MagicAccessories;

[ViewModelMixin("RefreshValues")]
public sealed class InventoryMagicAccessorySlotsMixin : BaseViewModelMixin<SPInventoryVM>
{
	// [RF-RUNAS R2] Comparador por REFERENCIA explicito: o ViewModel da TaleWorlds nao
	// sobrescreve Equals/GetHashCode hoje, mas o dicionario e a unica ponte entre o patch
	// estatico de equip e a instancia do mixin — nao pode depender disso.
	private sealed class ReferenceComparer : IEqualityComparer<SPInventoryVM>
	{
		public static readonly ReferenceComparer Instance = new ReferenceComparer();

		public bool Equals(SPInventoryVM x, SPInventoryVM y) => ReferenceEquals(x, y);

		public int GetHashCode(SPInventoryVM obj) => RuntimeHelpers.GetHashCode(obj);
	}

	private static readonly Dictionary<SPInventoryVM, InventoryMagicAccessorySlotsMixin> _instances =
		new Dictionary<SPInventoryVM, InventoryMagicAccessorySlotsMixin>(ReferenceComparer.Instance);

	// [RF-RUNAS R2] Ultimo mixin vivo, para fallback quando o lookup por VM falha.
	private static WeakReference _lastMixin;

	private static readonly MethodInfo AfterTransferMethod = typeof(SPInventoryVM).GetMethod(
		"AfterTransfer", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly PropertyInfo ActiveEquipmentProperty = typeof(SPInventoryVM).GetProperty(
		"ActiveEquipment", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

	private sealed class InitialAccessoryState
	{
		public string RingItemId { get; }
		public string NecklaceItemId { get; }
		public string[] RuneItemIds { get; } = new string[MagicRuneService.WeaponSlotCount];
		public string[] RuneTargetItemIds { get; } = new string[MagicRuneService.WeaponSlotCount];

		public InitialAccessoryState(Hero hero)
		{
			RingItemId = MagicAccessoryService.GetEquippedItemId(hero, MagicAccessorySlot.Ring);
			NecklaceItemId = MagicAccessoryService.GetEquippedItemId(hero, MagicAccessorySlot.Necklace);
			for (int slotIndex = 0; slotIndex < MagicRuneService.WeaponSlotCount; slotIndex++)
			{
				RuneItemIds[slotIndex] = MagicRuneService.GetRuneItemId(hero, slotIndex);
				RuneTargetItemIds[slotIndex] = MagicRuneService.GetTargetItemId(hero, slotIndex);
			}
		}
	}

	private readonly Dictionary<Hero, InitialAccessoryState> _initialStates =
		new Dictionary<Hero, InitialAccessoryState>();
	private readonly List<ItemObject> _returnedRuneItems = new List<ItemObject>();
	private readonly List<ItemObject> _returnedAccessoryItems = new List<ItemObject>();

	// [RF-RUNAS] A selecao "oficial" do inventario NAO sobrevive ate o clique nos
	// nossos botoes:
	//   1) SPInventoryVM._selectedItem e preenchido no HOVER (ProcessItemTooltip) e
	//      zerado assim que o mouse sai da linha do item — o prefab Inventory.xml liga
	//      Command.ItemHoverEnd="ResetSelectedItem" no InventoryScreenWidget;
	//   2) SPItemVM.IsSelected (clique na linha) tambem e limpo antes do nosso clique:
	//      InventoryScreenWidget.OnUpdate dispara "OnEmptyClick" -> ExecuteClearSelectedItem
	//      sempre que o mouse-down cai em um widget que nao e InventoryItemButtonWidget,
	//      que e exatamente o caso dos sockets/slots injetados por este mixin.
	// Por isso guardamos a ultima selecao real do jogador aqui e ignoramos as limpezas.
	private SPItemVM _stickySelectedItem;
	private int _selectedRuneSocketIndex = -1;

	// [RF-RUNAS R3] NAO usar BaseViewModelMixin.GetPrivate — ele esta QUEBRADO nesta versao
	// do UIExtenderEx. Decompilado (Bannerlord.UIExtenderEx.dll instalada):
	//
	//   private readonly WeakReference<TViewModel> _vm;
	//   protected TValue? GetPrivate<TValue>(string name) => _vm.PrivateValue<TValue>(name);
	//
	// e `PrivateValue` e uma extension de `object` que faz `o.GetType().GetProperties()
	// .Concat(GetFields())` e, se o nome nao existir, devolve `default(T)` SEM erro
	// (ReflectionHelpers.PrivateValue). Como `o` e o **WeakReference<SPInventoryVM>** e nao
	// o ViewModel, nenhum campo do VM e encontrado: `GetPrivate` sempre devolveu null,
	// silenciosamente, desde o construtor. Era isso que produzia "logic=NULL" no log.
	//
	// Porta oficial (verificada no binario 1.4.8): InventoryState.InventoryLogic e uma
	// propriedade PUBLICA (TaleWorlds.CampaignSystem.GameState.InventoryState) e e o mesmo
	// objeto que o SPInventoryVM recebe no construtor. Reflexao no campo privado fica so
	// como ultimo recurso, com o nome conferido na DLL instalada.
	private InventoryLogic Logic
	{
		get
		{
			InventoryLogic fromState = (GameStateManager.Current?.ActiveState as InventoryState)?.InventoryLogic;
			if (fromState != null)
			{
				return fromState;
			}
			SPInventoryVM vm = ViewModel;
			return vm != null ? InventoryLogicField?.GetValue(vm) as InventoryLogic : null;
		}
	}

	// Nomes confirmados na DLL 1.4.8 instalada (TaleWorlds.CampaignSystem.ViewModelCollection.dll,
	// SPInventoryVM): `private InventoryLogic _inventoryLogic;` e `private SPItemVM _selectedItem;`.
	// NUNCA tirar nomes de membro privado do dump Vanilla_1.3.x.
	private static readonly FieldInfo InventoryLogicField = typeof(SPInventoryVM).GetField(
		"_inventoryLogic", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo SelectedItemField = typeof(SPInventoryVM).GetField(
		"_selectedItem", BindingFlags.Instance | BindingFlags.NonPublic);

	private InventoryLogic _subscribedLogic;
	private ItemImageIdentifierVM _magicRingImage;
	private ItemImageIdentifierVM _magicNecklaceImage;
	private HintViewModel _magicRingHint;
	private HintViewModel _magicNecklaceHint;
	private bool _magicRingSlotOccupied;
	private bool _magicNecklaceSlotOccupied;
	private readonly MagicRuneSocketVM _magicRuneSlot1;
	private readonly MagicRuneSocketVM _magicRuneSlot2;
	private readonly MagicRuneSocketVM _magicRuneSlot3;
	private readonly MagicRuneSocketVM _magicRuneSlot4;

	[DataSourceProperty]
	public MagicRuneSocketVM MagicRuneSlot1 => _magicRuneSlot1;

	[DataSourceProperty]
	public MagicRuneSocketVM MagicRuneSlot2 => _magicRuneSlot2;

	[DataSourceProperty]
	public MagicRuneSocketVM MagicRuneSlot3 => _magicRuneSlot3;

	[DataSourceProperty]
	public MagicRuneSocketVM MagicRuneSlot4 => _magicRuneSlot4;

	[DataSourceProperty]
	public ItemImageIdentifierVM MagicRingImage
	{
		get => _magicRingImage;
		private set
		{
			if (value != _magicRingImage)
			{
				_magicRingImage = value;
				OnPropertyChanged(nameof(MagicRingImage));
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM MagicNecklaceImage
	{
		get => _magicNecklaceImage;
		private set
		{
			if (value != _magicNecklaceImage)
			{
				_magicNecklaceImage = value;
				OnPropertyChanged(nameof(MagicNecklaceImage));
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MagicRingHint
	{
		get => _magicRingHint;
		private set
		{
			if (value != _magicRingHint)
			{
				_magicRingHint = value;
				OnPropertyChanged(nameof(MagicRingHint));
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MagicNecklaceHint
	{
		get => _magicNecklaceHint;
		private set
		{
			if (value != _magicNecklaceHint)
			{
				_magicNecklaceHint = value;
				OnPropertyChanged(nameof(MagicNecklaceHint));
			}
		}
	}

	[DataSourceProperty]
	public bool MagicRingSlotOccupied
	{
		get => _magicRingSlotOccupied;
		private set
		{
			if (value != _magicRingSlotOccupied)
			{
				_magicRingSlotOccupied = value;
				OnPropertyChangedWithValue(value, nameof(MagicRingSlotOccupied));
				OnPropertyChangedWithValue(!value, nameof(MagicRingSlotEmpty));
			}
		}
	}

	[DataSourceProperty]
	public bool MagicRingSlotEmpty => !MagicRingSlotOccupied;

	[DataSourceProperty]
	public bool MagicNecklaceSlotOccupied
	{
		get => _magicNecklaceSlotOccupied;
		private set
		{
			if (value != _magicNecklaceSlotOccupied)
			{
				_magicNecklaceSlotOccupied = value;
				OnPropertyChangedWithValue(value, nameof(MagicNecklaceSlotOccupied));
				OnPropertyChangedWithValue(!value, nameof(MagicNecklaceSlotEmpty));
			}
		}
	}

	[DataSourceProperty]
	public bool MagicNecklaceSlotEmpty => !MagicNecklaceSlotOccupied;

	public InventoryMagicAccessorySlotsMixin(SPInventoryVM vm)
		: base(vm)
	{
		_instances[vm] = this;
		_lastMixin = new WeakReference(this);
		_magicRuneSlot1 = new MagicRuneSocketVM(0, ExecuteRuneSocket, ProcessRuneSocketTooltip,
			ResetRuneSocketTooltip, DiscardRuneFromSocket, UnequipRuneFromSocket);
		_magicRuneSlot2 = new MagicRuneSocketVM(1, ExecuteRuneSocket, ProcessRuneSocketTooltip,
			ResetRuneSocketTooltip, DiscardRuneFromSocket, UnequipRuneFromSocket);
		_magicRuneSlot3 = new MagicRuneSocketVM(2, ExecuteRuneSocket, ProcessRuneSocketTooltip,
			ResetRuneSocketTooltip, DiscardRuneFromSocket, UnequipRuneFromSocket);
		_magicRuneSlot4 = new MagicRuneSocketVM(3, ExecuteRuneSocket, ProcessRuneSocketTooltip,
			ResetRuneSocketTooltip, DiscardRuneFromSocket, UnequipRuneFromSocket);
		_subscribedLogic = Logic;
		if (_subscribedLogic != null)
		{
			_subscribedLogic.AfterReset += OnInventoryReset;
		}
		if (InventoryLogicField == null || SelectedItemField == null || ActiveEquipmentProperty == null ||
			AfterTransferMethod == null)
		{
			// Canario de rename: se a TaleWorlds mudar um nome, isso aparece UMA vez e
			// explicito, em vez de virar null silencioso.
			SotorLog.Error("MagicAccessories: membro de SPInventoryVM nao encontrado por reflexao — " +
				$"_inventoryLogic={(InventoryLogicField != null ? "ok" : "MISSING")}, " +
				$"_selectedItem={(SelectedItemField != null ? "ok" : "MISSING")}, " +
				$"ActiveEquipment={(ActiveEquipmentProperty != null ? "ok" : "MISSING")}, " +
				$"AfterTransfer={(AfterTransferMethod != null ? "ok" : "MISSING")}.");
		}
		SotorLog.Info($"MagicAccessories mixin created for SPInventoryVM#{RuntimeHelpers.GetHashCode(vm)} " +
			$"(logic={(_subscribedLogic != null ? "ok" : "NULL")}, instances={_instances.Count}).");
		// O construtor roda DENTRO do ctor do SPInventoryVM (transpiler do UIExtenderEx):
		// nesse instante partes do VM ainda podem estar cruas. Um throw aqui destruiria
		// a tela de inventario inteira, entao o refresh inicial nunca pode ser fatal.
		try
		{
			RefreshSlots();
		}
		catch (Exception ex)
		{
			SotorLog.Warn($"MagicAccessories mixin: refresh inicial falhou ({ex.GetType().Name}: {ex.Message}); " +
				"os slots serao preenchidos no proximo RefreshValues.");
		}
	}

	[DataSourceMethod]
	public void ExecuteMagicRingSlot()
	{
		ExecuteAccessorySlot(MagicAccessorySlot.Ring);
	}

	[DataSourceMethod]
	public void ExecuteMagicNecklaceSlot()
	{
		ExecuteAccessorySlot(MagicAccessorySlot.Necklace);
	}

	public override void OnRefresh()
	{
		RefreshSlots();
	}

	public override void OnFinalize()
	{
		if (ViewModel != null &&
			_instances.TryGetValue(ViewModel, out InventoryMagicAccessorySlotsMixin instance) &&
			ReferenceEquals(instance, this))
		{
			_instances.Remove(ViewModel);
		}
		if (ReferenceEquals(_lastMixin?.Target, this))
		{
			_lastMixin = null;
		}
		if (_subscribedLogic != null)
		{
			_subscribedLogic.AfterReset -= OnInventoryReset;
			_subscribedLogic = null;
		}
		_initialStates.Clear();
		_returnedRuneItems.Clear();
		_stickySelectedItem = null;
		SotorLog.Info($"MagicAccessories mixin finalized for SPInventoryVM#" +
			$"{(ViewModel != null ? RuntimeHelpers.GetHashCode(ViewModel).ToString() : "null")} (instances={_instances.Count}).");
	}

	// [RF-RUNAS R2] Resolve (ou RECRIA) o mixin de um SPInventoryVM.
	//
	// Por que recriar: a unica ponte entre o patch estatico de `ProcessEquipItem` e o
	// estado do mixin era `_instances`, e o mixin so entra la se o UIExtenderEx tiver
	// conseguido instancia-lo. Isso acontece dentro de um transpiler no fim de CADA
	// construtor declarado de SPInventoryVM (Bannerlord.UIExtenderEx
	// ViewModelWithMixinPatch.Constructor -> ViewModelComponent.InitializeMixinsForVMInstance),
	// com casamento EXATO de tipo e com o mixin previamente habilitado. Se qualquer elo
	// dessa cadeia falhar — ou se OnFinalize tiver removido a entrada de uma tela que
	// continua viva — o botao "Equip" da runa morria em "Rune sockets are unavailable".
	// Criar sob demanda cura o caso: o construtor se registra em `_instances` e toda a
	// logica (assign, consumo, refresh, cancelamento) passa a funcionar. As propriedades
	// de UI podem nao estar ligadas nesse cenario, mas os sockets do prefab continuam
	// lendo o estado no proximo RefreshValues.
	private static InventoryMagicAccessorySlotsMixin ResolveMixin(SPInventoryVM inventoryVm, string caller)
	{
		if (inventoryVm == null)
		{
			SotorLog.Warn($"ResolveMixin({caller}): SPInventoryVM nulo.");
			return null;
		}
		if (_instances.TryGetValue(inventoryVm, out InventoryMagicAccessorySlotsMixin mixin))
		{
			return mixin;
		}

		InventoryMagicAccessorySlotsMixin fallback = _lastMixin?.Target as InventoryMagicAccessorySlotsMixin;
		if (fallback != null && ReferenceEquals(fallback.ViewModel, inventoryVm))
		{
			SotorLog.Warn($"ResolveMixin({caller}): entrada ausente em _instances, usando _lastMixin " +
				$"(mesmo VM#{RuntimeHelpers.GetHashCode(inventoryVm)}). Reinserindo.");
			_instances[inventoryVm] = fallback;
			return fallback;
		}

		SotorLog.Warn($"ResolveMixin({caller}): nenhum mixin para SPInventoryVM#{RuntimeHelpers.GetHashCode(inventoryVm)} " +
			$"(instances={_instances.Count}, lastMixinVM=" +
			$"{(fallback?.ViewModel != null ? RuntimeHelpers.GetHashCode(fallback.ViewModel).ToString() : "none")}). " +
			"Criando sob demanda.");
		try
		{
			return new InventoryMagicAccessorySlotsMixin(inventoryVm);
		}
		catch (Exception ex)
		{
			// O ctor se registra em _instances antes de qualquer trabalho pesado, entao
			// mesmo um throw tardio pode ter deixado uma instancia utilizavel.
			SotorLog.Error($"ResolveMixin({caller}): falha ao criar mixin sob demanda: {ex.GetType().Name}: {ex.Message}");
			return _instances.TryGetValue(inventoryVm, out InventoryMagicAccessorySlotsMixin partial) ? partial : null;
		}
	}

	// [RF-RUNAS] Chamado pelo postfix de SPInventoryVM.ExecuteSelectItem. Guardamos
	// somente selecoes reais (item != null) e IGNORAMOS as limpezas (item == null),
	// que sao justamente o que o OnEmptyClick do InventoryScreenWidget provoca quando
	// o jogador aperta o mouse em cima de um socket.
	internal static void RememberSelection(SPInventoryVM inventoryVm, ItemVM itemVm)
	{
		if (inventoryVm == null || !(itemVm is SPItemVM item) ||
			item.ItemRosterElement.EquipmentElement.Item == null)
		{
			return;
		}
		// Selecao e evento de alta frequencia: aqui NAO criamos mixin sob demanda,
		// so aproveitamos o que existir.
		if (_instances.TryGetValue(inventoryVm, out InventoryMagicAccessorySlotsMixin mixin) ||
			((mixin = _lastMixin?.Target as InventoryMagicAccessorySlotsMixin) != null &&
				ReferenceEquals(mixin.ViewModel, inventoryVm)))
		{
			mixin._stickySelectedItem = item;
			mixin.SelectRuneSocket(-1);
		}
	}

	private SPItemVM GetSelectedInventoryItem()
	{
		SPInventoryVM vm = ViewModel;
		SPItemVM hovered = vm != null ? SelectedItemField?.GetValue(vm) as SPItemVM : null;
		if (hovered?.ItemRosterElement.EquipmentElement.Item != null)
		{
			return hovered;
		}
		if (_stickySelectedItem?.ItemRosterElement.EquipmentElement.Item != null)
		{
			return _stickySelectedItem;
		}
		return null;
	}

	internal static bool TryHandleVanillaEquip(SPInventoryVM inventoryVm, ItemVM itemVm)
	{
		if (inventoryVm == null || !(itemVm is SPItemVM selectedItem))
		{
			return false;
		}

		ItemObject selectedObject = selectedItem.ItemRosterElement.EquipmentElement.Item;
		if (selectedObject == null || !MagicRuneRegistry.TryGet(selectedObject.StringId, out MagicRuneData rune))
		{
			return false;
		}

		SotorLog.Info($"Vanilla equip intercepted for rune '{selectedObject.StringId}' " +
			$"(SPInventoryVM#{RuntimeHelpers.GetHashCode(inventoryVm)}, side={selectedItem.InventorySide}).");

		InventoryMagicAccessorySlotsMixin mixin = ResolveMixin(inventoryVm, "TryHandleVanillaEquip");
		if (mixin == null)
		{
			SotorLog.Error("Vanilla equip aborted: nao foi possivel obter nem criar o mixin para esta tela.");
			ShowMessage("Rune sockets are unavailable (no inventory mixin).", Colors.Red);
			return true;
		}

		mixin.EquipRuneAutomatically(selectedItem, rune);
		return true;
	}

	private bool EquipRuneAutomatically(SPItemVM selectedItem, MagicRuneData rune)
	{
		Hero hero = GetCurrentHero();
		if (hero == null)
		{
			SotorLog.Error("EquipRuneAutomatically: hero nulo (CharacterList/SelectedItem/Hero e Hero.MainHero indisponiveis).");
			ShowMessage("Rune sockets are unavailable (no hero selected).", Colors.Red);
			return false;
		}
		if (Logic == null)
		{
			SotorLog.Error($"EquipRuneAutomatically: SPInventoryVM._inventoryLogic nulo " +
				$"(VM#{RuntimeHelpers.GetHashCode(ViewModel)} — tela ja finalizada?).");
			ShowMessage("Rune sockets are unavailable (inventory closing).", Colors.Red);
			return false;
		}
		if (!ViewModel.IsBattleMode)
		{
			SotorLog.Info("EquipRuneAutomatically recusado: nao esta na aba Batalha.");
			ShowMessage("Switch to the Battle tab to socket runes.", Colors.Yellow);
			return false;
		}
		if (!IsRuneUsableFromSide(selectedItem.InventorySide, out string sideRefusal))
		{
			SotorLog.Info($"EquipRuneAutomatically recusado: runa esta em {selectedItem.InventorySide}.");
			ShowMessage(sideRefusal, Colors.Yellow);
			return false;
		}

		int firstCompatibleSlot = -1;
		int selectedSlot = -1;
		Equipment activeEquipment = GetActiveEquipment(hero);
		for (int slotIndex = 0; slotIndex < MagicRuneService.WeaponSlotCount; slotIndex++)
		{
			ItemObject targetItem = activeEquipment?[(EquipmentIndex)slotIndex].Item;
			if (!MagicRuneRegistry.CanApply(rune, targetItem))
			{
				continue;
			}

			if (firstCompatibleSlot < 0)
			{
				firstCompatibleSlot = slotIndex;
			}
			if (string.IsNullOrEmpty(MagicRuneService.GetRuneItemId(hero, slotIndex)))
			{
				selectedSlot = slotIndex;
				break;
			}
		}

		if (selectedSlot < 0)
		{
			selectedSlot = firstCompatibleSlot;
		}
		if (selectedSlot < 0)
		{
			SotorLog.Info($"EquipRuneAutomatically: nenhum slot de arma compativel com '{rune.ItemId}' " +
				$"(targets={rune.Targets}). Armas: " +
				$"[{string.Join(", ", GetWeaponSlotIds(activeEquipment))}].");
			ShowMessage("Equip a weapon compatible with this rune first.", Colors.Yellow);
			return false;
		}

		ItemObject selectedTarget = activeEquipment?[(EquipmentIndex)selectedSlot].Item;
		return EquipSelectedRune(hero, selectedSlot, selectedTarget, selectedItem);
	}

	// [RF-RUNAS R3] O log do autor mostrou a runa em `side=OtherInventory`. Encaixar uma
	// runa a CONSOME do roster de origem (TransferCommand com from=PlayerInventory), entao
	// aceitar `OtherInventory` seria pegar de graca item de mercador/pilhagem/estoque alheio.
	// Semantica confirmada no binario 1.4.8: a lista da DIREITA do inventario do jogador e
	// construida com InventorySide.PlayerInventory; `OtherInventory` e sempre o roster do
	// outro lado. O gate continua, mas a recusa agora diz o que fazer.
	private static bool IsRuneUsableFromSide(InventoryLogic.InventorySide side, out string refusal)
	{
		refusal = null;
		if (side == InventoryLogic.InventorySide.PlayerInventory)
		{
			return true;
		}
		refusal = side == InventoryLogic.InventorySide.OtherInventory
			? "That rune is not yours yet - move it into your own inventory first."
			: "Runes must be socketed from your inventory, not from an equipment slot.";
		return false;
	}

	private static IEnumerable<string> GetWeaponSlotIds(Equipment equipment)
	{
		for (int i = 0; i < MagicRuneService.WeaponSlotCount; i++)
		{
			yield return equipment?[(EquipmentIndex)i].Item?.StringId ?? "empty";
		}
	}

	private void ExecuteAccessorySlot(MagicAccessorySlot slot)
	{
		Hero hero = GetCurrentHero();
		if (hero == null)
		{
			SotorLog.Error("ExecuteAccessorySlot: hero nulo.");
			ShowMessage("Magic accessory slots are unavailable (no hero selected).", Colors.Red);
			return;
		}
		if (Logic == null)
		{
			SotorLog.Error($"ExecuteAccessorySlot: _inventoryLogic nulo (VM#{RuntimeHelpers.GetHashCode(ViewModel)}).");
			ShowMessage("Magic accessory slots are unavailable (inventory closing).", Colors.Red);
			return;
		}

		SPItemVM selectedItem = GetSelectedInventoryItem();
		ItemObject selectedObject = selectedItem?.ItemRosterElement.EquipmentElement.Item;
		if (selectedObject != null && MagicAccessoryRegistry.TryGet(selectedObject.StringId, out MagicAccessoryData selectedAccessory))
		{
			if (selectedAccessory.Slot != slot)
			{
				ShowMessage("That accessory belongs in the other magic slot.", Colors.Red);
				return;
			}
			if (selectedItem.InventorySide != InventoryLogic.InventorySide.PlayerInventory)
			{
				ShowMessage("Move the accessory to your inventory before equipping it.", Colors.Red);
				return;
			}

			EquipSelectedAccessory(hero, slot, selectedItem);
			return;
		}

		if (!string.IsNullOrEmpty(MagicAccessoryService.GetEquippedItemId(hero, slot)))
		{
			UnequipAccessory(hero, slot);
			return;
		}

		ShowMessage("Select a compatible accessory in your inventory, then click this slot.", Colors.Yellow);
	}

	private void ExecuteRuneSocket(int slotIndex)
	{
		Hero hero = GetCurrentHero();
		SotorLog.Info($"RuneSocket click slot={slotIndex} on SPInventoryVM#{GetHashCode()} " +
			$"(hero={hero?.Name?.ToString() ?? "null"}, logic={(Logic != null ? "ok" : "NULL")}, " +
			$"battleMode={ViewModel?.IsBattleMode.ToString() ?? "?"}).");
		if (hero == null)
		{
			SotorLog.Error("ExecuteRuneSocket: hero nulo.");
			ShowMessage("Rune sockets are unavailable (no hero selected).", Colors.Red);
			return;
		}
		if (Logic == null)
		{
			SotorLog.Error($"ExecuteRuneSocket: _inventoryLogic nulo (mixin#{GetHashCode()}).");
			ShowMessage("Rune sockets are unavailable (inventory closing).", Colors.Red);
			return;
		}
		if (!ViewModel.IsBattleMode)
		{
			ShowMessage("Switch to the Battle tab to socket runes.", Colors.Yellow);
			return;
		}

		ItemObject targetItem = GetActiveEquipment(hero)?[(EquipmentIndex)slotIndex].Item;
		if (targetItem == null)
		{
			ShowMessage("Equip an item in this weapon slot first.", Colors.Yellow);
			return;
		}

		SPItemVM selectedItem = GetSelectedInventoryItem();
		ItemObject selectedObject = selectedItem?.ItemRosterElement.EquipmentElement.Item;
		SotorLog.Info($"RuneSocket slot={slotIndex} target='{targetItem.StringId}' " +
			$"selected='{selectedObject?.StringId ?? "none"}' side={selectedItem?.InventorySide.ToString() ?? "none"}");
		if (selectedObject != null && MagicRuneRegistry.TryGet(selectedObject.StringId, out MagicRuneData selectedRune))
		{
			if (!IsRuneUsableFromSide(selectedItem.InventorySide, out string sideRefusal))
			{
				SotorLog.Info($"RuneSocket recusado: runa esta em {selectedItem.InventorySide}.");
				ShowMessage(sideRefusal, Colors.Yellow);
				return;
			}
			if (!MagicRuneRegistry.CanApply(selectedRune, targetItem))
			{
				ShowMessage("That rune is not compatible with this item.", Colors.Red);
				return;
			}

			EquipSelectedRune(hero, slotIndex, targetItem, selectedItem);
			return;
		}

		if (!string.IsNullOrEmpty(MagicRuneService.GetRuneItemId(hero, slotIndex)))
		{
			SelectRuneSocket(_selectedRuneSocketIndex == slotIndex ? -1 : slotIndex);
			return;
		}

		ShowMessage("Select a compatible rune in your inventory, then click this socket.", Colors.Yellow);
	}

	private bool EquipSelectedRune(Hero hero, int slotIndex, ItemObject targetItem, SPItemVM selectedItem)
	{
		ItemRosterElement selectedElement = selectedItem.ItemRosterElement;
		ItemObject selectedObject = selectedElement.EquipmentElement.Item;
		InventoryLogic logic = Logic;
		if (logic == null)
		{
			SotorLog.Error("EquipSelectedRune: _inventoryLogic nulo.");
			ShowMessage("Rune sockets are unavailable (inventory closing).", Colors.Red);
			return false;
		}
		if (!logic.CheckItemRosterHasElement(InventoryLogic.InventorySide.PlayerInventory, selectedElement, 1))
		{
			SotorLog.Warn($"EquipSelectedRune: runa '{selectedObject?.StringId}' nao esta mais no inventario do jogador.");
			ShowMessage("The selected rune is no longer in your inventory.", Colors.Red);
			return false;
		}
		Equipment activeEquipment = GetActiveEquipment(hero);
		if (activeEquipment == null || targetItem == null)
		{
			ShowMessage("This weapon slot is empty.", Colors.Red);
			return false;
		}

		RememberInitialState(hero);
		string previousRuneItemId = MagicRuneService.GetRuneItemId(hero, slotIndex);
		ItemObject previousRune = MagicRuneService.GetItemObject(previousRuneItemId);
		if (!MagicRuneService.TryAssign(hero, activeEquipment, slotIndex, selectedObject.StringId,
			targetItem.StringId, out string failure))
		{
			SotorLog.Warn($"RuneSocket assign failed slot={slotIndex} rune='{selectedObject.StringId}' " +
				$"target='{targetItem.StringId}' reason='{failure}'");
			ShowMessage(failure, Colors.Red);
			return false;
		}

		if (previousRune != null)
		{
			AddToPlayerInventory(previousRune, hero);
			_returnedRuneItems.Add(previousRune);
		}

		Transfer(selectedElement, InventoryLogic.InventorySide.PlayerInventory, InventoryLogic.InventorySide.None, hero);
		_stickySelectedItem = null;
		ViewModel?.ResetSelectedItem();
		ViewModel?.ExecuteRemoveZeroCounts();
		RefreshSlots();
		SelectRuneSocket(slotIndex);
		SotorLog.Info($"Rune '{selectedObject.StringId}' socketed on '{targetItem.StringId}' (slot {slotIndex}) for {hero.Name}.");
		return true;
	}

	private void UnequipRuneFromSocket(int slotIndex)
	{
		Hero hero = GetCurrentHero();
		if (hero == null)
		{
			ShowMessage("Rune sockets are unavailable (no hero selected).", Colors.Red);
			return;
		}

		UnequipRune(hero, slotIndex);
	}

	private void DiscardRuneFromSocket(int slotIndex)
	{
		Hero hero = GetCurrentHero();
		if (hero == null)
		{
			ShowMessage("Rune sockets are unavailable (no hero selected).", Colors.Red);
			return;
		}

		string runeItemId = MagicRuneService.GetRuneItemId(hero, slotIndex);
		ItemObject runeItem = MagicRuneService.GetItemObject(runeItemId);
		if (runeItem == null)
		{
			return;
		}

		RememberInitialState(hero);
		ItemRosterElement returnedElement = AddToPlayerInventory(runeItem, hero);
		_returnedRuneItems.Add(runeItem);
		Transfer(returnedElement, InventoryLogic.InventorySide.PlayerInventory,
			InventoryLogic.InventorySide.OtherInventory, hero);
		MagicRuneService.TryClear(hero, slotIndex);
		_stickySelectedItem = null;
		ViewModel?.ResetSelectedItem();
		ViewModel?.ExecuteRemoveZeroCounts();
		RefreshSlots();
		SelectRuneSocket(-1);
	}

	private void UnequipRune(Hero hero, int slotIndex)
	{
		string runeItemId = MagicRuneService.GetRuneItemId(hero, slotIndex);
		ItemObject runeItem = MagicRuneService.GetItemObject(runeItemId);
		RememberInitialState(hero);
		if (runeItem != null)
		{
			AddToPlayerInventory(runeItem, hero);
			_returnedRuneItems.Add(runeItem);
		}
		MagicRuneService.TryClear(hero, slotIndex);
		_stickySelectedItem = null;
		ViewModel?.ResetSelectedItem();
		ViewModel?.ExecuteRemoveZeroCounts();
		RefreshSlots();
		SelectRuneSocket(-1);
	}

	private void EquipSelectedAccessory(Hero hero, MagicAccessorySlot slot, SPItemVM selectedItem)
	{
		ItemRosterElement selectedElement = selectedItem.ItemRosterElement;
		ItemObject selectedObject = selectedElement.EquipmentElement.Item;
		string previousItemId = MagicAccessoryService.GetEquippedItemId(hero, slot);
		if (string.Equals(previousItemId, selectedObject.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		if (Logic == null || !Logic.CheckItemRosterHasElement(InventoryLogic.InventorySide.PlayerInventory, selectedElement, 1))
		{
			ShowMessage("The selected accessory is no longer in your inventory.", Colors.Red);
			return;
		}

		RememberInitialState(hero);
		ItemObject previousItem = MagicAccessoryService.GetItemObject(previousItemId);
		if (previousItem != null)
		{
			AddToPlayerInventory(previousItem, hero);
			_returnedAccessoryItems.Add(previousItem);
		}

		Transfer(selectedElement, InventoryLogic.InventorySide.PlayerInventory, InventoryLogic.InventorySide.None, hero);
		if (!MagicAccessoryService.TryAssign(hero, slot, selectedObject.StringId, out string failure))
		{
			ShowMessage(failure, Colors.Red);
			return;
		}

		_stickySelectedItem = null;
		ViewModel?.ResetSelectedItem();
		ViewModel?.ExecuteRemoveZeroCounts();
		RefreshSlots();
	}

	private void UnequipAccessory(Hero hero, MagicAccessorySlot slot)
	{
		string itemId = MagicAccessoryService.GetEquippedItemId(hero, slot);
		ItemObject item = MagicAccessoryService.GetItemObject(itemId);
		RememberInitialState(hero);
		if (item != null)
		{
			AddToPlayerInventory(item, hero);
			_returnedAccessoryItems.Add(item);
		}
		MagicAccessoryService.TryClear(hero, slot);
		_stickySelectedItem = null;
		ViewModel?.ResetSelectedItem();
		ViewModel?.ExecuteRemoveZeroCounts();
		RefreshSlots();
	}

	private ItemRosterElement AddToPlayerInventory(ItemObject item, Hero hero)
	{
		ItemRoster roster = Logic?.OwnerParty?.ItemRoster;
		if (roster == null)
		{
			return default;
		}

		int index = roster.AddToCounts(item, 1);
		ItemRosterElement updatedElement = roster.GetElementCopyAtIndex(index);
		if (AfterTransferMethod != null && ViewModel != null)
		{
			List<TransferCommandResult> results = new List<TransferCommandResult>
			{
				new TransferCommandResult(
					InventoryLogic.InventorySide.PlayerInventory,
					updatedElement,
					1,
					updatedElement.Amount,
					EquipmentIndex.None,
					hero.CharacterObject)
			};
			AfterTransferMethod.Invoke(ViewModel, new object[] { Logic, results });
		}
		return updatedElement;
	}

	private void Transfer(ItemRosterElement element, InventoryLogic.InventorySide from, InventoryLogic.InventorySide to, Hero hero)
	{
		TransferCommand command = TransferCommand.Transfer(
			1,
			from,
			to,
			element,
			EquipmentIndex.None,
			EquipmentIndex.None,
			hero.CharacterObject);
		Logic?.AddTransferCommand(command);
	}

	private void RememberInitialState(Hero hero)
	{
		if (!_initialStates.ContainsKey(hero))
		{
			_initialStates.Add(hero, new InitialAccessoryState(hero));
		}
	}

	private void OnInventoryReset(InventoryLogic inventoryLogic, bool fromCancel)
	{
		if (fromCancel)
		{
			RemoveReturnedItems(_returnedRuneItems);
			RemoveReturnedItems(_returnedAccessoryItems);
			foreach (KeyValuePair<Hero, InitialAccessoryState> pair in _initialStates)
			{
				Restore(pair.Key, MagicAccessorySlot.Ring, pair.Value.RingItemId);
				Restore(pair.Key, MagicAccessorySlot.Necklace, pair.Value.NecklaceItemId);
				Equipment equipment = GetActiveEquipment(pair.Key);
				for (int slotIndex = 0; slotIndex < MagicRuneService.WeaponSlotCount; slotIndex++)
				{
					RestoreRune(pair.Key, equipment, slotIndex, pair.Value.RuneItemIds[slotIndex], pair.Value.RuneTargetItemIds[slotIndex]);
				}
			}
		}
		_initialStates.Clear();
		_returnedRuneItems.Clear();
		_returnedAccessoryItems.Clear();
		RefreshSlots();
	}

	private void RemoveReturnedItems(List<ItemObject> items)
	{
		ItemRoster roster = Logic?.OwnerParty?.ItemRoster;
		if (roster == null || items.Count == 0)
		{
			return;
		}

		List<TransferCommandResult> results = new List<TransferCommandResult>();
		foreach (ItemObject item in items)
		{
			if (item == null || roster.GetItemNumber(item) <= 0)
			{
				continue;
			}

			int index = roster.AddToCounts(item, -1);
			ItemRosterElement updatedElement = index >= 0
				? roster.GetElementCopyAtIndex(index)
				: new ItemRosterElement(item, 0);
			results.Add(new TransferCommandResult(
				InventoryLogic.InventorySide.PlayerInventory,
				updatedElement,
				-1,
				updatedElement.Amount,
				EquipmentIndex.None,
				GetCurrentHero()?.CharacterObject));
		}

		if (results.Count > 0 && AfterTransferMethod != null && ViewModel != null)
		{
			AfterTransferMethod.Invoke(ViewModel, new object[] { Logic, results });
		}
	}

	private static void Restore(Hero hero, MagicAccessorySlot slot, string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			MagicAccessoryService.TryClear(hero, slot);
		}
		else
		{
			MagicAccessoryService.TryAssign(hero, slot, itemId, out _);
		}
	}

	private static void RestoreRune(Hero hero, Equipment equipment, int slotIndex, string runeItemId, string targetItemId)
	{
		if (string.IsNullOrEmpty(runeItemId))
		{
			MagicRuneService.TryClear(hero, slotIndex);
		}
		else
		{
			MagicRuneService.TryAssign(hero, equipment, slotIndex, runeItemId, targetItemId, out _);
		}
	}

	private Hero GetCurrentHero()
	{
		return ViewModel?.CharacterList?.SelectedItem?.Hero ?? Hero.MainHero;
	}

	private Equipment GetActiveEquipment(Hero hero)
	{
		if (hero == GetCurrentHero())
		{
			return ActiveEquipmentProperty?.GetValue(ViewModel) as Equipment ?? hero?.BattleEquipment;
		}
		return hero?.BattleEquipment;
	}

	internal static Hero GetTooltipHero()
	{
		foreach (InventoryMagicAccessorySlotsMixin mixin in _instances.Values)
		{
			Hero hero = mixin.GetCurrentHero();
			if (hero != null)
			{
				return hero;
			}
		}
		return Hero.MainHero;
	}

	private void ProcessRuneSocketTooltip(int slotIndex)
	{
		GetRuneSocket(slotIndex)?.RuneHint?.ExecuteBeginHint();
	}

	private void ResetRuneSocketTooltip(int slotIndex)
	{
		GetRuneSocket(slotIndex)?.RuneHint?.ExecuteEndHint();
	}

	private MagicRuneSocketVM GetRuneSocket(int slotIndex)
	{
		return slotIndex switch
		{
			0 => _magicRuneSlot1,
			1 => _magicRuneSlot2,
			2 => _magicRuneSlot3,
			3 => _magicRuneSlot4,
			_ => null
		};
	}

	private void SelectRuneSocket(int slotIndex)
	{
		_selectedRuneSocketIndex = slotIndex;
		_magicRuneSlot1.SetSelected(slotIndex == 0);
		_magicRuneSlot2.SetSelected(slotIndex == 1);
		_magicRuneSlot3.SetSelected(slotIndex == 2);
		_magicRuneSlot4.SetSelected(slotIndex == 3);
	}

	private void RefreshSlots()
	{
		Hero hero = GetCurrentHero();
		Equipment activeEquipment = GetActiveEquipment(hero);
		RefreshSlot(hero, MagicAccessorySlot.Ring, out ItemImageIdentifierVM ringImage,
			out HintViewModel ringHint, out bool ringOccupied);
		RefreshSlot(hero, MagicAccessorySlot.Necklace, out ItemImageIdentifierVM necklaceImage,
			out HintViewModel necklaceHint, out bool necklaceOccupied);

		MagicRingImage = ringImage;
		MagicRingHint = ringHint;
		MagicRingSlotOccupied = ringOccupied;
		MagicNecklaceImage = necklaceImage;
		MagicNecklaceHint = necklaceHint;
		MagicNecklaceSlotOccupied = necklaceOccupied;

		RefreshRuneSlot(hero, activeEquipment, 0, _magicRuneSlot1);
		RefreshRuneSlot(hero, activeEquipment, 1, _magicRuneSlot2);
		RefreshRuneSlot(hero, activeEquipment, 2, _magicRuneSlot3);
		RefreshRuneSlot(hero, activeEquipment, 3, _magicRuneSlot4);
		SelectRuneSocket(_selectedRuneSocketIndex);
	}

	private static void RefreshSlot(Hero hero, MagicAccessorySlot slot, out ItemImageIdentifierVM image,
		out HintViewModel hint, out bool occupied)
	{
		string itemId = MagicAccessoryService.GetEquippedItemId(hero, slot);
		ItemObject item = MagicAccessoryService.GetItemObject(itemId);
		// [RF-RUNAS R4] Nunca nulo (mesmo motivo do MagicRuneSocketVM.Refresh): com
		// DataSource="{MagicRingImage}" apontando para null, o binding de ImageId nao
		// religa quando o acessorio e equipado.
		image = new ItemImageIdentifierVM(item, string.Empty);
		occupied = item != null;

		string slotName = slot == MagicAccessorySlot.Ring ? "Ring" : "Necklace";
		string equippedName = item == null ? "Empty" : item.Name.ToString();
		string bonusText = MagicAccessoryRegistry.TryGet(itemId, out MagicAccessoryData accessory)
			? BuildBonusText(accessory.MaxWindsBonus, accessory.RechargeMultiplier, accessory.EffectivenessMultiplier,
				accessory.WindsCostMultiplier, accessory.CooldownMultiplier)
			: string.Empty;
		string bonuses = string.IsNullOrEmpty(bonusText) ? string.Empty : "\n" + bonusText;
		hint = new HintViewModel(new TextObject(
			slotName + " slot\nEquipped: " + equippedName + bonuses +
			"\nSelect a compatible item in your inventory and click to equip. Click with no compatible item selected to unequip."));
	}

	private static void RefreshRuneSlot(Hero hero, Equipment equipment, int slotIndex, MagicRuneSocketVM socket)
	{
		ItemObject targetItem = equipment?[(EquipmentIndex)slotIndex].Item;
		string targetItemId = MagicRuneService.GetTargetItemId(hero, slotIndex);
		string runeItemId = MagicRuneService.GetRuneItemId(hero, slotIndex);
		ItemObject runeItem = MagicRuneService.GetItemObject(runeItemId);
		bool targetMatches = runeItem == null || (targetItem != null &&
			string.Equals(targetItem.StringId, targetItemId, StringComparison.OrdinalIgnoreCase) &&
			(!MagicRuneRegistry.TryGet(runeItem.StringId, out MagicRuneData rune) ||
				MagicRuneRegistry.CanApply(rune, targetItem)));

		if (!string.IsNullOrEmpty(runeItemId) && runeItem == null)
		{
			// Estado salvo aponta para um item que o MBObjectManager nao conhece (xml de
			// itens removido/renomeado). Sem isso o socket ficaria vazio sem explicacao.
			SotorLog.Warn($"RefreshRuneSlot slot={slotIndex}: runa '{runeItemId}' salva no heroi " +
				"nao existe no MBObjectManager — icone impossivel de resolver.");
		}
		socket.Refresh(targetItem, runeItem, targetMatches);
		SotorLog.Debug($"RefreshRuneSlot slot={slotIndex} target='{targetItem?.StringId ?? "empty"}' " +
			$"rune='{runeItem?.StringId ?? "none"}' occupied={runeItem != null} matches={targetMatches}");
	}

	private static string BuildBonusText(float maxWindsBonus, float rechargeMultiplier,
		float effectivenessMultiplier, float windsCostMultiplier, float cooldownMultiplier)
	{
		List<string> bonuses = new List<string>();
		if (Math.Abs(maxWindsBonus) > 0.001f)
		{
			bonuses.Add("Max Mana " + FormatSigned(maxWindsBonus));
		}
		AddMultiplier(bonuses, "Recharge", rechargeMultiplier);
		AddMultiplier(bonuses, "Effectiveness", effectivenessMultiplier);
		AddMultiplier(bonuses, "Mana cost", windsCostMultiplier);
		AddMultiplier(bonuses, "Cooldown", cooldownMultiplier);
		return string.Join("\n", bonuses);
	}

	private static void AddMultiplier(List<string> bonuses, string label, float multiplier)
	{
		float percent = (multiplier - 1f) * 100f;
		if (Math.Abs(percent) <= 0.05f)
		{
			return;
		}
		bonuses.Add(label + " " + FormatSigned(percent) + "%");
	}

	private static string FormatSigned(float value)
	{
		return value >= 0f ? "+" + value.ToString("0.#") : value.ToString("0.#");
	}

	private static void ShowMessage(string message, Color color)
	{
		InformationManager.DisplayMessage(new InformationMessage(message, color));
	}
}

// [RF-RUNAS] Espelha a selecao real do jogador (clique na linha do inventario) para
// dentro do mixin. Sem isso, nenhum socket/slot magico ve o item selecionado: o
// InventoryScreenWidget limpa a selecao no mouse-down sobre widgets que nao sao
// InventoryItemButtonWidget (OnEmptyClick -> ExecuteClearSelectedItem) e o
// _selectedItem do VM morre no ItemHoverEnd.
[HarmonyPatch(typeof(SPInventoryVM), nameof(SPInventoryVM.ExecuteSelectItem))]
internal static class InventoryMagicSelectionMemoryPatch
{
	[HarmonyPostfix]
	private static void Postfix(SPInventoryVM __instance, ItemVM item)
	{
		InventoryMagicAccessorySlotsMixin.RememberSelection(__instance, item);
	}
}

[HarmonyPatch(typeof(SPInventoryVM), "ProcessEquipItem")]
internal static class InventoryMagicRuneEquipPatch
{
	[HarmonyPrefix]
	private static bool Prefix(SPInventoryVM __instance, ItemVM draggedItem)
	{
		return !InventoryMagicAccessorySlotsMixin.TryHandleVanillaEquip(__instance, draggedItem);
	}
}

[HarmonyPatch(typeof(SPItemVM), "get_IsEquipableItem")]
internal static class InventoryMagicRuneEquipablePatch
{
	[HarmonyPostfix]
	private static void Postfix(SPItemVM __instance, ref bool __result)
	{
		if (__result)
		{
			return;
		}

		ItemObject item = __instance?.ItemRosterElement.EquipmentElement.Item;
		if (item != null && MagicRuneRegistry.TryGet(item.StringId, out _))
		{
			__result = true;
		}
	}
}

[HarmonyPatch(typeof(ItemMenuVM), "RefreshItemTooltips")]
internal static class MagicRuneItemTooltipPatch
{
	[HarmonyPostfix]
	private static void Postfix(ItemMenuVM __instance, ItemVM item, ItemVM comparedItem, int alternativeUsageIndex)
	{
		Hero hero = InventoryMagicAccessorySlotsMixin.GetTooltipHero();
		MagicRuneData targetRune = GetRuneForTooltip(item, hero, out bool targetIsSocketed);
		MagicRuneData comparedRune = GetRuneForTooltip(comparedItem, hero, out bool comparedIsSocketed);
		if (targetRune == null && comparedRune == null)
		{
			return;
		}

		IReadOnlyList<KeyValuePair<string, string>> targetRows = MagicRuneService.GetTooltipRows(targetRune, targetIsSocketed);
		IReadOnlyList<KeyValuePair<string, string>> comparedRows = MagicRuneService.GetTooltipRows(comparedRune, comparedIsSocketed);
		int rowCount = Math.Max(targetRows.Count, comparedRows.Count);
		for (int i = 0; i < rowCount; i++)
		{
			KeyValuePair<string, string> targetRow = i < targetRows.Count
				? targetRows[i]
				: new KeyValuePair<string, string>(string.Empty, string.Empty);
			KeyValuePair<string, string> comparedRow = i < comparedRows.Count
				? comparedRows[i]
				: new KeyValuePair<string, string>(string.Empty, string.Empty);
			__instance.TargetItemProperties?.Add(CreateProperty(targetRow));
			__instance.ComparedItemProperties?.Add(CreateProperty(comparedRow));
		}
	}

	private static MagicRuneData GetRuneForTooltip(ItemVM item, Hero hero, out bool isSocketed)
	{
		isSocketed = false;
		ItemObject itemObject = item?.ItemRosterElement.EquipmentElement.Item;
		if (itemObject == null)
		{
			return null;
		}
		if (MagicRuneRegistry.TryGet(itemObject.StringId, out MagicRuneData rune))
		{
			return rune;
		}
		if (!(item is SPItemVM inventoryItem) || inventoryItem.InventorySide != InventoryLogic.InventorySide.BattleEquipment)
		{
			return null;
		}

		EquipmentIndex equipmentIndex = item.ItemType;
		if (hero == null || equipmentIndex < EquipmentIndex.Weapon0 || equipmentIndex > EquipmentIndex.Weapon3)
		{
			return null;
		}

		int slotIndex = (int)equipmentIndex;
		string runeItemId = MagicRuneService.GetRuneItemId(hero, slotIndex);
		string targetItemId = MagicRuneService.GetTargetItemId(hero, slotIndex);
		if (string.Equals(targetItemId, itemObject.StringId, StringComparison.OrdinalIgnoreCase) &&
			MagicRuneRegistry.TryGet(runeItemId, out rune) && MagicRuneRegistry.CanApply(rune, itemObject))
		{
			isSocketed = true;
			return rune;
		}
		return null;
	}

	private static ItemMenuTooltipPropertyVM CreateProperty(KeyValuePair<string, string> row)
	{
		return new ItemMenuTooltipPropertyVM(row.Key, row.Value, 0, false, null, null, false);
	}
}
