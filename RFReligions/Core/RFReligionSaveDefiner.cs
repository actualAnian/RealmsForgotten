using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFReligions.Core;

public class RFReligionSaveDefiner : SaveableTypeDefiner
{
    public RFReligionSaveDefiner() : base(199235856_7)
    {
    }


    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(SettlementReligionModel), 1);
        AddClassDefinition(typeof(HeroReligionModel), 2);
    }


    protected override void DefineEnumTypes()
    {
        AddEnumDefinition(typeof(RFReligions), 555121);
    }


    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(Dictionary<RFReligions, float>));
        ConstructContainerDefinition(typeof(Dictionary<Settlement, SettlementReligionModel>));
        ConstructContainerDefinition(typeof(Dictionary<Hero, HeroReligionModel>));
    }
}