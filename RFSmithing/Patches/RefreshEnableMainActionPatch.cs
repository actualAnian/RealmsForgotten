using System.Linq;
using HarmonyLib;
using RealmsForgotten.Smithing.Mixins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace RealmsForgotten.Smithing.Patches;

[HarmonyPatch(typeof(CraftingVM), "RefreshEnableMainAction")]
public static class RefreshEnableMainActionPatch
{
    public static void Postfix()
    {
        if (CraftingMixin.Instance == null || WeaponDesignMixin.Instance?.KardrathiumButtonToggle == null || PartyBase.MainParty == null) 
            return;
        
        
        if (WeaponDesignMixin.Instance.KardrathiumButtonToggle.UseKardrathium)
        {
            //var kardrathiumMaterial = CraftingMixin.Instance.ExtraMaterials.FirstOrDefault(m => m.ResourceItemStringId == RFItems.Kardrathium.StringId);

            if (PartyBase.MainParty.ItemRoster.GetItemNumber(RFItems.Kardrathium) <
                WeaponDesignMixin.Instance.KardrathiumButtonToggle.GetCurrentKardrathiumPrice())
            {
                CraftingMixin.Instance.CraftingVm.IsMainActionEnabled = false;
                CraftingMixin.Instance.CraftingVm.MainActionHint = 
                    new BasicTooltipViewModel(() => new TextObject("{=not_enough_kardrathium}You don't have enough kardrathium for this weapon").ToString());
            }
        }
    }
}