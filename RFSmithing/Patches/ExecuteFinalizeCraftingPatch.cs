using HarmonyLib;
using RealmsForgotten.Smithing.Mixins;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

namespace RealmsForgotten.Smithing.Patches;

[HarmonyPatch(typeof(CraftingVM), "ExecuteCancel")]
public static class ExecuteFinalizeCraftingPatch
{
    public static void Prefix()
    {
        if (WeaponDesignMixin.Instance?.KardrathiumButtonToggle != null)
        {
            WeaponDesignMixin.Instance.KardrathiumButtonToggle.UseKardrathium = false;
        }
    }
}