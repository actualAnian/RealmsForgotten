using System.Collections.Generic;
using System.Linq;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using RealmsForgotten.Smithing.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Smithing.Mixins;

[HarmonyPatch(typeof(WeaponDesignVM), "RefreshStats")]
public static class RefreshStatsPatch
{
    public static readonly Dictionary<CraftingTemplate.CraftingStatTypes, float> KardrathiumMultipliers = new()
    {
        { CraftingTemplate.CraftingStatTypes.MissileDamage, 1.5f },
        { CraftingTemplate.CraftingStatTypes.SwingDamage, 1.5f },
        { CraftingTemplate.CraftingStatTypes.ThrustDamage, 1.5f },
        { CraftingTemplate.CraftingStatTypes.Handling, 1.2f },
        { CraftingTemplate.CraftingStatTypes.Weight, 0.5f }
    };
    
    public static readonly Dictionary<CraftingTemplate.CraftingStatTypes, float> KardrathiumCurrentWeaponStats = new()
    {
        { CraftingTemplate.CraftingStatTypes.MissileDamage, 0f },
        { CraftingTemplate.CraftingStatTypes.SwingDamage, 0f },
        { CraftingTemplate.CraftingStatTypes.ThrustDamage, 0f },
        { CraftingTemplate.CraftingStatTypes.Handling, 0f },
        { CraftingTemplate.CraftingStatTypes.Weight, 0f }
    };
    
    public static void Postfix(WeaponDesignVM __instance)
    {
        if (CraftingMixin.IsMainActionExecuting || WeaponDesignMixin.Instance == null || WeaponDesignMixin.Instance?.WeaponDesignVM != __instance || WeaponDesignMixin.Instance?.KardrathiumButtonToggle?.UseKardrathium == false)
            return;

        foreach (var propertyItem in __instance.PrimaryPropertyList)
        {
            if (!KardrathiumMultipliers.TryGetValue(propertyItem.Type, out float multiplier)) 
                continue;
            
            propertyItem.PropertyValue *= multiplier;
            KardrathiumCurrentWeaponStats[propertyItem.Type] = propertyItem.PropertyValue;
            propertyItem.RefreshValues();
        }
    }
}

[ViewModelMixin]
public class WeaponDesignMixin : BaseViewModelMixin<WeaponDesignVM>
{
    public static WeaponDesignMixin? Instance { get; private set; }

    public WeaponDesignVM WeaponDesignVM;
    
    public WeaponDesignMixin(WeaponDesignVM vm) : base(vm)
    {
        Instance = this;
        WeaponDesignVM = vm;
        KardrathiumButtonToggle = new KardrathiumButtonToggleVM(this)
        {
            UseKardrathium = false
        };
    }
    
    private KardrathiumButtonToggleVM _kardrathiumButtonToggle;
    
    [DataSourceProperty]
    public KardrathiumButtonToggleVM KardrathiumButtonToggle
    {
        get => _kardrathiumButtonToggle;
        set
        {
            if (value == _kardrathiumButtonToggle) 
                return;
            
            _kardrathiumButtonToggle = value;
            OnPropertyChangedWithValue(value);
        }
    }
}