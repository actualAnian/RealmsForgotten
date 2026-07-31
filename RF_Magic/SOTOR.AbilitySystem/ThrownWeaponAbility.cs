using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SOTOR.AbilitySystem;

public class ThrownWeaponAbility : Spell
{
	public const string AmberJavelinItemId = "sotor_amber_javelin";

	public override bool IsThrownWeapon => true;

	public ThrownWeaponAbility(AbilityTemplate template)
		: base(template)
	{
	}

	public override bool TryCast(Agent casterAgent, SotorTarget preferredTarget, out TextObject failureReason)
	{
		failureReason = null;
		if (casterAgent == null || Mission.Current == null)
		{
			failureReason = new TextObject("{=sotor_cast_no_context}No mission context.");
			return false;
		}
		if (IsDisabled(casterAgent, out failureReason))
		{
			SotorLog.Info("TryCast " + base.StringID + " (thrown): blocked — " + (failureReason?.ToString() ?? "disabled") + ".");
			return false;
		}
		try
		{
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("sotor_amber_javelin");
			if (itemObject == null)
			{
				failureReason = new TextObject("{=sotor_cast_no_item}Amber javelin item missing.");
				SotorLog.Warn("TryCast " + base.StringID + " (thrown): ItemObject 'sotor_amber_javelin' not found.");
				LogAmberItemDiagnostics();
				return false;
			}
			if ((itemObject.ItemFlags & ItemFlags.QuickFadeOut) == 0)
			{
				itemObject.SetItemFlagsForCosmetics(itemObject.ItemFlags | ItemFlags.QuickFadeOut);
			}
			EquipmentIndex primaryWieldedItemIndex = casterAgent.GetPrimaryWieldedItemIndex();
			MissionWeapon weapon = new MissionWeapon(itemObject, null, null, 1);
			casterAgent.EquipWeaponToExtraSlotAndWield(ref weapon);
			SotorThrownJavelinMissionLogic.Instance?.OnAmberJavelinReadied(casterAgent, this, primaryWieldedItemIndex);
			return true;
		}
		catch (Exception ex)
		{
			failureReason = new TextObject("{=sotor_cast_thrown_failed}Amber javelin ready failed.");
			SotorLog.Error("TryCast " + base.StringID + " (thrown) EXCEPTION: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			return false;
		}
	}

	public void GrantThrowSpellcraftXp(Hero hero)
	{
		if (hero != null)
		{
			GrantSpellcraftXp(hero);
		}
	}

	private static void LogAmberItemDiagnostics()
	{
		try
		{
			MBObjectManager instance = MBObjectManager.Instance;
			ItemObject itemObject = instance.GetObject<ItemObject>("sotor_amber_javelin");
			CraftingTemplate craftingTemplate = instance.GetObject<CraftingTemplate>("sotor_amber_javelin_template");
			SotorLog.Info("[AMBERDIAG] item 'sotor_amber_javelin'=" + ((itemObject != null) ? "OK" : "NULL") + " ; template 'sotor_amber_javelin_template'=" + ((craftingTemplate != null) ? "OK" : "NULL") + ".");
			string[] array = new string[4] { "sotor_amber_spear_blade", "sotor_amber_spear_handle", "sotor_amber_spear_pommel", "default_polearm_guard" };
			foreach (string text in array)
			{
				CraftingPiece craftingPiece = instance.GetObject<CraftingPiece>(text);
				SotorLog.Info("[AMBERDIAG] piece '" + text + "'=" + ((craftingPiece != null) ? ("OK (IsValid=" + craftingPiece.IsValid + ")") : "NULL") + ".");
			}
			ItemObject itemObject2 = instance.GetObject<ItemObject>("eastern_javelin_3_t4");
			CraftingPiece craftingPiece2 = instance.GetObject<CraftingPiece>("spear_blade_27");
			CraftingTemplate craftingTemplate2 = instance.GetObject<CraftingTemplate>("Javelin");
			SotorLog.Info("[AMBERDIAG] native jereed=" + ((itemObject2 != null) ? "OK" : "NULL") + " ; native spear_blade_27=" + ((craftingPiece2 != null) ? "OK" : "NULL") + " ; native Javelin template=" + ((craftingTemplate2 != null) ? "OK" : "NULL") + ".");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("[AMBERDIAG] diagnostics failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
