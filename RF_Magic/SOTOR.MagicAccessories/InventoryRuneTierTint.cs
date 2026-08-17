using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SOTOR.MagicAccessories;

// [RF-RUNAS R5] Cor por tier no BOX da linha do inventario (bronze / prata / dourado).
//
// Por que NAO ha rota nativa por dados: `ItemQuality` existe em TaleWorlds.Core mas
// nao e referenciado por TaleWorlds.MountAndBlade.GauntletUI.Widgets (conferido na DLL
// 1.4.8) — a lista do inventario nao coloriza por qualidade/modifier. Nao existe
// modifier_group que tinja a linha.
//
// Por que NAO tingimos o brush da tuple: InventoryItemTupleWidget.UpdateEquipmentTypeState()
// reatribui `MainContainer.Brush` (Default/CantUseInSet/CharacterCantUse) e BrushWidget.OnRender
// desenha via BrushRenderer IGNORANDO `Widget.Color`. Tingir exigiria mutar o Brush — e o Brush
// vem de `DefaultBrush="!Inventory.Tuple"`, um objeto COMPARTILHADO por todas as tuples:
// mutar tingiria a lista inteira (mesma classe de erro de mutar ItemObject compartilhado).
//
// Solucao: um Widget SIMPLES nosso, injetado no box. `Widget.OnRender` faz
// `simpleMaterial.Color = Color` (conferido em TaleWorlds.GauntletUI.BaseTypes.Widget) e a cor
// NAO propaga para os filhos — ou seja, tinge so o proprio retangulo de fundo e nunca o
// tableau/arte da runa. A cor vem de um mixin no SPItemVM, portanto e sempre a cor DAQUELE
// item: quando a lista virtualizada recicla a tuple, o DataSource troca e o binding recalcula.
// Nao existe estado de widget para "vazar".
public sealed class InventoryRuneTierTint
{
	public const uint White = 0xFFFFFFFFu;

	// Bronze / prata / dourado.
	public const uint LesserBronze = 0xFFCD7F32u;
	public const uint GreaterSilver = 0xFFD8D8DCu;
	public const uint AncientGold = 0xFFFFD24Au;

	public static uint ColorFor(MagicRuneTier tier)
	{
		switch (tier)
		{
			case MagicRuneTier.Greater:
				return GreaterSilver;
			case MagicRuneTier.Ancient:
				return AncientGold;
			default:
				return LesserBronze;
		}
	}
}

[ViewModelMixin("RefreshValues")]
public sealed class InventoryRuneTierTintMixin : BaseViewModelMixin<SPItemVM>
{
	public InventoryRuneTierTintMixin(SPItemVM vm)
		: base(vm)
	{
	}

	// Propriedades COMPUTADAS de proposito: nada de cache que possa sobreviver a uma
	// troca de item na mesma instancia de SPItemVM.
	[DataSourceProperty]
	public bool RfIsRuneItem => TryGetTier(out _);

	[DataSourceProperty]
	public uint RfRuneTierColor =>
		TryGetTier(out MagicRuneTier tier) ? InventoryRuneTierTint.ColorFor(tier) : InventoryRuneTierTint.White;

	public override void OnRefresh()
	{
		OnPropertyChanged(nameof(RfIsRuneItem));
		OnPropertyChanged(nameof(RfRuneTierColor));
	}

	private bool TryGetTier(out MagicRuneTier tier)
	{
		tier = MagicRuneTier.Lesser;
		SPItemVM vm = ViewModel;
		ItemObject item = vm?.ItemRosterElement.EquipmentElement.Item;
		if (item == null)
		{
			return false;
		}
		if (!MagicRuneRegistry.TryGet(item.StringId, out MagicRuneData rune))
		{
			return false;
		}
		tier = rune.Tier;
		return true;
	}
}

// Injeta o retangulo de tint como PRIMEIRO filho de `MainControls`, que e um Widget simples
// (pai com HeightSizePolicy Fixed), portanto os filhos se sobrepoem sem layout de lista.
// Deliberadamente NAO em `Main`: `Main` e um BrushListPanel (ListPanel) e um filho novo
// entraria no stack layout, empurrando o conteudo da linha para o lado.
//
// XPath aponta para `/Children` e o tipo e `Child` com Index=0 porque, no
// PrefabComponent do UIExtenderEx (conferido no binario): `Child` faz
// InsertAsChild(node, novo, Index) inserindo dentro de node.ChildNodes, enquanto
// `Prepend`/`Append` inserem como IRMAO (node.ParentNode.InsertBefore/InsertAfter).
// Primeiro filho => desenhado antes dos irmaos => fica ATRAS do nome, contagem, preco e icone.
// [RF-RUNAS R8 — ISOLACAO DO CRASH 2026-08-11 09:40]
// O atributo [PrefabExtension] abaixo esta COMENTADO de proposito: sem ele o UIExtenderEx
// nao registra esta extensao e NADA e injetado na tuple do inventario. E o interruptor do
// teste A/B do crash nativo ao abrir o inventario (log morre 1s depois de HandleActivate,
// sem excecao gerenciada). Com o tint desligado e os meshes reais mantidos:
//   inventario abre  => a causa era o tint (este widget) -> corrigir/derrubar o tint
//   inventario crasha => a causa e o mesh/tableau        -> reverter mesh e investigar asset
// PARA RELIGAR: descomentar a linha do atributo (e o RFSocketTierTint em
// InventoryMagicAccessorySlotsExtension.cs, removido no mesmo commit de isolacao).
// [PrefabExtension("InventoryItemTuple", "descendant::Widget[@Id='MainControls']/Children")]
internal sealed class InventoryRuneTierTintExtension : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _document = new XmlDocument();

	public override InsertType Type => InsertType.Child;

	public override int Index => 0;

	public InventoryRuneTierTintExtension()
	{
		_document.LoadXml(@"
<Widget Id=""RFRuneTierTint"" DoNotAcceptEvents=""true"" IsDisabled=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Sprite=""BlankWhiteSquare_9"" AlphaFactor=""0.30"" Color=""@RfRuneTierColor"" IsVisible=""@RfIsRuneItem"" />");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}
}

// [RF-RUNAS R7] INATIVO — mantido apenas como referencia/fallback.
// A arte REAL das runas existe: o autor importou os 24 meshes em
// RealmsForgotten/Assets/magic/{runes_game_geo,lesser_runes/lesser_runes_game_geo}.tpac,
// entao o caminho ativo voltou a ser o tableau do item (ImageIdentifier). Nada consome esta
// tabela hoje; ela fica documentada caso algum dia se queira um icone 2D de UI.
//
// [RF-RUNAS R6] Icone de runa por familia usando SPRITE de UI (rota A), nao mesh/tableau.
//
// Por que sprite: o RF_Magic JA tem infraestrutura de sprite custom funcionando —
// `GUI/RF_MagicSpriteData.xml` declara as SpriteCategories com <AlwaysLoad/>,
// `GUI/SpriteParts/Config.xml` repete a declaracao, e os PNG ficam soltos em
// `GUI/SpriteParts/ui_sotor/` (140 arquivos). Sendo AlwaysLoad, esses sprites estao
// disponiveis em QUALQUER tela, inclusive o inventario. Zero tpac, zero editor.
// `Sprite="@Prop"` e bindavel (56 usos nos prefabs vanilla: @SpriteName, @IconPath...).
//
// IMPORTANTE — de onde vem a arte: NAO existe arte de runa por familia no projeto.
// `RF_Magic_runas/` contem 3 direcoes de design do CIRCULO DE CONJURACAO (marcador de mira
// no chao, 1024x1024 — o proprio _preview.html diz "Runa de conjuracao — 3 direcoes"), o que
// e outra feature. Entao mapeamos cada familia para um sprite JA EXISTENTE de `ui_sotor`
// (todos 256x256, conferidos) por tema. Nenhuma arte foi inventada; trocar qualquer entrada
// e mudar uma string desta tabela.
public static class MagicRuneIcons
{
	public static string SpriteFor(MagicRuneEffect effect)
	{
		switch (effect)
		{
			// Corpo-a-corpo
			case MagicRuneEffect.Sundering: return "plagueofrust_icon";
			case MagicRuneEffect.Impact: return "wind_blast_icon";
			case MagicRuneEffect.KeenEdge: return "quicksilversword_icon";
			case MagicRuneEffect.Piercing: return "deadlyshards_icon";
			case MagicRuneEffect.Executioner: return "taste_of_death_icon";
			case MagicRuneEffect.Vampiric: return "drainlife_icon";
			// Distancia
			case MagicRuneEffect.Precision: return "gleamingarrow_icon";
			case MagicRuneEffect.Wind: return "chillwind_icon";
			case MagicRuneEffect.Windlass: return "enchant_weapon_icon";
			case MagicRuneEffect.FarSight: return "radiantgaze_icon";
			case MagicRuneEffect.Huntsman: return "beastunleashed_icon";
			case MagicRuneEffect.Returning: return "bironastimewarp_icon";
			// Elemental
			case MagicRuneEffect.Flame: return "fireball_icon";
			case MagicRuneEffect.Frost: return "iceshardblizzard_icon";
			case MagicRuneEffect.Storm: return "chainlightning_icon";
			case MagicRuneEffect.Explosive: return "cinderblast_icon";
			// Defensivo
			case MagicRuneEffect.Bulwark: return "shield_of_saphery_icon";
			case MagicRuneEffect.Reprisal: return "shield_of_thorns_icon";
			case MagicRuneEffect.Mirror: return "resistanceaura_icon";
			case MagicRuneEffect.Lightness: return "phasprotection_icon";
			// Arcano
			case MagicRuneEffect.Arcane: return "harmonicconvergence_icon";
			case MagicRuneEffect.Focus: return "traits_magic_icon";
			case MagicRuneEffect.Reservoir: return "regrowth_icon";
			case MagicRuneEffect.Echo: return "finaltransmutation_icon";
			default: return null;
		}
	}

	public static string SpriteFor(string runeItemId)
	{
		return MagicRuneRegistry.TryGet(runeItemId, out MagicRuneData rune) ? SpriteFor(rune.Effect) : null;
	}
}
