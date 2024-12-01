using System.Linq;
using System.Reflection;
using HarmonyLib;
using RealmsForgotten.Smithing.Mixins;
using RealmsForgotten.Smithing.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Smithing.Patches;

[HarmonyPatch(typeof(Crafting), "GenerateItem")]
public static class CraftingPatches
{
    private static PropertyInfo ItemWeight = AccessTools.Property(typeof(ItemObject), "Weight");
    public static void Postfix(
        WeaponDesign weaponDesignTemplate,
        TextObject name,
        BasicCultureObject culture,
        ItemModifierGroup itemModifierGroup,
        ref ItemObject itemObject)
    {
        if (!CraftingMixin.IsMainActionExecuting || WeaponDesignMixin.Instance?.KardrathiumButtonToggle?.UseKardrathium == false)
            return;
        
        ItemWeight.SetValue(itemObject, RefreshStatsPatch.KardrathiumCurrentWeaponStats[CraftingTemplate.CraftingStatTypes.Weight]);
    }
}

[HarmonyPatch(typeof(CraftingCampaignBehavior), "SpendMaterials")]
public static class SpendMaterialsPatch
{
    public static bool Prefix(WeaponDesign weaponDesign)
    {
        if (WeaponDesignMixin.Instance?.KardrathiumButtonToggle == null || PartyBase.MainParty?.ItemRoster == null || WeaponDesignMixin.Instance.KardrathiumButtonToggle.UseKardrathium == false)
            return true;
        
        ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
        int[] costsForWeaponDesign = Campaign.Current.Models.SmithingModel.GetSmithingCostsForWeaponDesign(weaponDesign);
        for (int craftingMaterial = 8; craftingMaterial >= 0; --craftingMaterial)
        {
            if (KardrathiumButtonToggleVM.Irons.Contains((CraftingMaterials)craftingMaterial))
            {
                costsForWeaponDesign[craftingMaterial] = 0;
            }
            if (costsForWeaponDesign[craftingMaterial] != 0)
                itemRoster.AddToCounts(Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem((CraftingMaterials) craftingMaterial), costsForWeaponDesign[craftingMaterial]);
        }
        
        int cost = WeaponDesignMixin.Instance.KardrathiumButtonToggle.GetCurrentKardrathiumPrice();
        itemRoster.AddToCounts(RFItems.Kardrathium, -cost);
        
        return false;
    }
}

public static class CalculateStatsPatch
{
    public static void Postfix(ref float ____currentWeaponHandling, 
        ref float ____currentWeaponThrustDamage, 
        ref float ____currentWeaponSwingDamage, 
        ref float ____currentWeaponWeight)
    {
        if (CraftingMixin.IsMainActionExecuting == false || WeaponDesignMixin.Instance == null || WeaponDesignMixin.Instance?.KardrathiumButtonToggle?.UseKardrathium == false)
            return;
        
        
        ____currentWeaponHandling = RefreshStatsPatch.KardrathiumCurrentWeaponStats[CraftingTemplate.CraftingStatTypes.Handling];
        ____currentWeaponThrustDamage = RefreshStatsPatch.KardrathiumCurrentWeaponStats[CraftingTemplate.CraftingStatTypes.ThrustDamage];
        ____currentWeaponSwingDamage = RefreshStatsPatch.KardrathiumCurrentWeaponStats[CraftingTemplate.CraftingStatTypes.SwingDamage];
        ____currentWeaponWeight = RefreshStatsPatch.KardrathiumCurrentWeaponStats[CraftingTemplate.CraftingStatTypes.Weight];
    }
}