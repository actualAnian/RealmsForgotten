using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RealmsForgotten.Smithing.Mixins;
using RealmsForgotten.Smithing.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Smithing.Patches;

[HarmonyPatch(typeof(TooltipRefresherCollection), "RefreshCraftingPartTooltip")]
public static class RefreshCraftingPartTooltip
{
    public static void Postfix(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
    {
        if (WeaponDesignMixin.Instance?.KardrathiumButtonToggle == null || CraftingMixin.Instance == null)
            return;

        int price = WeaponDesignMixin.Instance.KardrathiumButtonToggle.GetCurrentKardrathiumPrice();

        propertyBasedTooltipVM.AddProperty(() => new TextObject("{=kardrathium}Kardrathium(?)").ToString(), () => price.ToString());
    }
}