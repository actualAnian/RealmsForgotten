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
        
        WeaponDesignElement weaponDesignElement = args[0] as WeaponDesignElement;

        int price = 0;
        foreach ((CraftingMaterials, int) tuple in weaponDesignElement.CraftingPiece.MaterialsUsed)
        {
            if (KardrathiumButtonToggleVM.Irons.Contains(tuple.Item1) && tuple.Item2 > 0)
            {
                price = tuple.Item2;
            }
        }

        if (price <= 0)
        {
            return;
        }
        
        propertyBasedTooltipVM.AddProperty(() => new TextObject("{=kardrathium}Kardrathium(?)").ToString(), () => price.ToString());
    }
}