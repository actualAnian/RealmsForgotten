using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RealmsForgotten.Smithing.Mixins;
using RealmsForgotten.Smithing.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Smithing.ViewModels;

public static class CraftingExtension
{
    private static FieldInfo _crafting = AccessTools.Field(typeof(CraftingVM), "_crafting");

    public static Crafting GetCurrentCrafting(this CraftingVM craftingVm)
    {
        return (Crafting)_crafting.GetValue(craftingVm);
    }
}
public class KardrathiumButtonToggleVM : ViewModel
{
    public static readonly CraftingMaterials[] Irons =
    {
        CraftingMaterials.Iron2, CraftingMaterials.Iron3, CraftingMaterials.Iron4,
        CraftingMaterials.Iron5, CraftingMaterials.Iron6
    };
    private static MethodInfo RefreshStats = AccessTools.Method(typeof(WeaponDesignVM), "RefreshStats");
    
    private WeaponDesignMixin _weaponDesignMixin;
    public KardrathiumButtonToggleVM(WeaponDesignMixin weaponDesignMixin)
    {
        _weaponDesignMixin = weaponDesignMixin;
    }
    public int GetCurrentKardrathiumPrice()
    {
        var smithingModel = Campaign.Current.Models.SmithingModel as RFSmithingModel;
        int[] smithingCostsForWeaponDesign = smithingModel.GetSmithingCostsForWeaponDesign(CraftingMixin.Instance.CraftingVm.GetCurrentCrafting().CurrentWeaponDesign);
        List<int> foundIrons = new List<int>();
        for (int i = 0; i < smithingCostsForWeaponDesign.Length; i++)
        {
            if (Irons.Contains((CraftingMaterials)i))
                foundIrons.Add(smithingCostsForWeaponDesign[i]);
        }
        return -foundIrons.Min();
    }
    private static MethodInfo RefreshEnableMainAction = AccessTools.Method(typeof(CraftingVM), "RefreshEnableMainAction");
    private void OnKardrathiumToggle(bool newValue)
    {
        RefreshEnableMainAction.Invoke(CraftingMixin.Instance.CraftingVm, new object[] {});
        RefreshStats.Invoke(_weaponDesignMixin.WeaponDesignVM, new object[] { });
    }
    
    
    private bool _useKardrathium;
    
    [DataSourceProperty]
    public bool UseKardrathium
    {
        get => _useKardrathium;
        set
        {
            if (value == _useKardrathium)
                return;
            _useKardrathium = value;
            OnPropertyChangedWithValue(value);
            OnKardrathiumToggle(value);
        }
    }
}