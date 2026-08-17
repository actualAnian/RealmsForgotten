using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

namespace SOTOR.MagicAccessories;

// [RF-RUNAS R9] Cor por tier no BOX da linha do inventario — AGORA pelo mecanismo NATIVO.
//
// Historia: a primeira tentativa (R5) injetava um Widget nosso dentro da tuple do inventario e
// DERRUBAVA o jogo (crash nativo ao abrir o inventario, sem excecao gerenciada — provado por
// A/B: com o widget desligado a tela abre). Aquele caminho esta morto e nao volta.
//
// Como o JOGO faz: nao existe sprite vermelho separado para "nao pode usar". E o MESMO sprite com
// fatores de cor na camada do brush (SandBox/GUI/Brushes/Inventory.xml):
//
//   <Brush Name="Inventory.Tuple.CantUseInSet.Left">
//     <BrushLayer Name="Default" Sprite="Inventory\tuple_left" HueFactor="-40" SaturationFactor="10" />
//
// E quem escolhe o brush e InventoryItemTupleWidget.UpdateEquipmentTypeState() (decompilado do
// 1.4.8 instalado):
//
//   if (IsEquipable && !CanCharacterUseItem)      MainContainer.Brush = CharacterCantUseBrush;
//   else if (naoPodeNesteSet)                     MainContainer.Brush = CantUseInSetBrush;
//   else if (!Brush.IsCloneRelated(DefaultBrush)) MainContainer.Brush = DefaultBrush;
//
// Entao o tier vira um CLONE do DefaultBrush daquele widget com Hue/Saturation/Value deslocados —
// exatamente o idioma do vermelho, so com outros numeros. Tres consequencias boas:
//   1. Zero widget novo na arvore: nada para dar crash de layout/render.
//   2. Nada de mutar brush compartilhado (o veneno classico): Clone() da um objeto proprio, e o
//      cache abaixo garante 1 clone por (brush base x tier), nao um por linha da lista.
//   3. O VERMELHO DO JOGO CONTINUA VENCENDO: nosso clone nao e "clone related" aos brushes de
//      CantUse, logo quando o jogo decide vermelho ele sobrescreve nossa cor — e a informacao do
//      jogo (nao pode usar) nunca fica escondida por cosmetica nossa.
internal static class RuneTierBrushes
{
	// Bronze / prata / dourado como DESLOCAMENTOS sobre o brush base da linha, no mesmo espirito
	// do vermelho vanilla (que usa HueFactor -40 / SaturationFactor +10). Sao os 9 numeros a
	// ajustar se o autor quiser outra paleta.
	private const float LesserHue = -6f;
	private const float LesserSaturation = 30f;
	private const float LesserValue = -6f;

	private const float GreaterHue = 0f;
	private const float GreaterSaturation = -70f;
	private const float GreaterValue = 14f;

	private const float AncientHue = 10f;
	private const float AncientSaturation = 55f;
	private const float AncientValue = 12f;

	private static readonly Dictionary<Brush, Brush[]> _byBaseBrush = new Dictionary<Brush, Brush[]>();
	private static readonly HashSet<Brush> _ours = new HashSet<Brush>();

	public static bool IsOurs(Brush brush)
	{
		return brush != null && _ours.Contains(brush);
	}

	public static Brush Get(Brush baseBrush, MagicRuneTier tier)
	{
		if (baseBrush == null)
		{
			return null;
		}

		if (!_byBaseBrush.TryGetValue(baseBrush, out Brush[] perTier))
		{
			// A lista da esquerda e a da direita usam brushes base diferentes
			// (Inventory.Tuple.Left / .Right), por isso o cache e por brush base.
			perTier = new Brush[3];
			_byBaseBrush[baseBrush] = perTier;
		}

		int index = tier == MagicRuneTier.Ancient ? 2 : (tier == MagicRuneTier.Greater ? 1 : 0);
		if (perTier[index] == null)
		{
			Brush clone = baseBrush.Clone();
			float hue = index == 2 ? AncientHue : (index == 1 ? GreaterHue : LesserHue);
			float saturation = index == 2 ? AncientSaturation : (index == 1 ? GreaterSaturation : LesserSaturation);
			float value = index == 2 ? AncientValue : (index == 1 ? GreaterValue : LesserValue);

			// Todos os estilos (Default/Hovered/Pressed/Selected) recebem o mesmo deslocamento,
			// senao a cor do tier desapareceria ao passar o mouse ou selecionar a linha.
			foreach (Style style in clone.Styles)
			{
				StyleLayer layer = style?.DefaultLayer;
				if (layer == null)
				{
					continue;
				}
				layer.HueFactor += hue;
				layer.SaturationFactor += saturation;
				layer.ValueFactor += value;
			}

			perTier[index] = clone;
			_ours.Add(clone);
		}

		return perTier[index];
	}
}

// Reaplica a cor do tier depois de CADA decisao de brush do widget nativo. Os tres ganchos
// cobrem os momentos em que o brush pode ter sido (re)definido:
//   UpdateEquipmentTypeState — o proprio metodo que troca o brush (estado de equipavel/uso);
//   OnConnectedToRoot        — a linha entrou na arvore com os bindings ja resolvidos (1a pintura);
//   RefreshState             — hover/press/select.
// Tudo em try/catch: nenhuma cosmetica de runa pode derrubar a tela de inventario (de novo).
[HarmonyPatch(typeof(InventoryItemTupleWidget), "UpdateEquipmentTypeState")]
public static class RuneTupleBrushEquipmentStatePatch
{
	public static void Postfix(InventoryItemTupleWidget __instance)
	{
		RuneTupleBrushApplier.Apply(__instance);
	}
}

[HarmonyPatch(typeof(InventoryItemTupleWidget), "OnConnectedToRoot")]
public static class RuneTupleBrushConnectedPatch
{
	public static void Postfix(InventoryItemTupleWidget __instance)
	{
		RuneTupleBrushApplier.Apply(__instance);
	}
}

[HarmonyPatch(typeof(InventoryItemTupleWidget), "RefreshState")]
public static class RuneTupleBrushRefreshStatePatch
{
	public static void Postfix(InventoryItemTupleWidget __instance)
	{
		RuneTupleBrushApplier.Apply(__instance);
	}
}

// [RF-RUNAS R9b] O gancho que faltava — e a causa do bug "a cor aparece conforme eu arrasto o
// mouse". `UpdateEquipmentTypeState` comeca com `if (base.ScreenWidget == null) return;`, e na
// primeira pintura da linha o ScreenWidget AINDA E NULO: o jogo sai sem definir brush, e portanto
// o nosso postfix nao tinha o que tingir. Depois disso o metodo so roda de novo se alguma
// propriedade mudar — e quem mudava era o HOVER (RefreshState). Resultado: a lista abria sem cor e
// cada linha ganhava a cor no instante em que o mouse passava por cima.
//
// O ImageId do tableau e definido quando a linha recebe o ITEM (uma vez por item, depois dos
// bindings), com a tela ja montada — e o momento certo para pintar. Vale para qualquer
// ImageIdentifierWidget do jogo, por isso o filtro sobe a arvore procurando a tuple do inventario
// e desiste em poucos niveis.
[HarmonyPatch(typeof(ImageIdentifierWidget), "ImageId", MethodType.Setter)]
public static class RuneTupleBrushImageIdPatch
{
	public static void Postfix(ImageIdentifierWidget __instance)
	{
		try
		{
			Widget parent = __instance?.ParentWidget;
			for (int depth = 0; depth < 5 && parent != null; depth++)
			{
				if (parent is InventoryItemTupleWidget tuple)
				{
					RuneTupleBrushApplier.Apply(tuple);
					return;
				}
				parent = parent.ParentWidget;
			}
		}
		catch
		{
			// Nao logar: este setter roda para toda imagem de UI do jogo.
		}
	}
}

internal static class RuneTupleBrushApplier
{
	private static bool _loggedOnce;

	internal static void Apply(InventoryItemTupleWidget widget)
	{
		try
		{
			if (widget?.MainContainer == null)
			{
				return;
			}

			Brush current = widget.MainContainer.Brush;

			// Primeira pintura: o jogo ainda nao atribuiu brush nenhum (UpdateEquipmentTypeState
			// saiu cedo porque ScreenWidget era nulo). Usamos o DefaultBrush como base para o tier.
			if (current == null)
			{
				current = widget.DefaultBrush;
			}

			// O jogo esta marcando "nao pode usar"? Entao a cor dele manda; saimos sem tocar.
			if (current != null &&
				((widget.CharacterCantUseBrush != null && current.IsCloneRelated(widget.CharacterCantUseBrush)) ||
				 (widget.CantUseInSetBrush != null && current.IsCloneRelated(widget.CantUseInSetBrush))))
			{
				return;
			}

			// O ImageId do tableau do item E o StringId do ItemObject — e assim que sabemos qual
			// item esta nesta linha sem depender do ViewModel.
			string itemId = widget.ItemImageIdentifier?.ImageId;

			if (!string.IsNullOrEmpty(itemId) && MagicRuneRegistry.TryGet(itemId, out MagicRuneData rune))
			{
				Brush tinted = RuneTierBrushes.Get(widget.DefaultBrush, rune.Tier);
				if (tinted != null && !ReferenceEquals(current, tinted))
				{
					widget.MainContainer.Brush = tinted;
					if (!_loggedOnce)
					{
						_loggedOnce = true;
						SotorLog.Info("RuneTupleBrush: cor de tier aplicada pelo brush nativo (1o caso: " + itemId + ", tier=" + rune.Tier + ").");
					}
				}
				return;
			}

			// Linha que NAO e runa mas esta com um clone nosso (widget reaproveitado): devolve o
			// brush padrao, senao a cor vazaria para outro item.
			if (RuneTierBrushes.IsOurs(current))
			{
				widget.MainContainer.Brush = widget.DefaultBrush;
			}
		}
		catch (Exception e)
		{
			SotorLog.Error("RuneTupleBrush.Apply: " + e.GetType().Name + ": " + e.Message);
		}
	}
}
