using System.Collections.Generic;
using RealmsForgotten.AiMade;
using RealmsForgotten.Behaviors;
using RealmsForgotten.Quest;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten;

internal class SaveDefiner : SaveableTypeDefiner
{
    public SaveDefiner() : base(2_87656_493) { }
    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(MercenaryData), 1);
        AddEnumDefinition(typeof(DestinationTypes), 2, null);
        AddClassDefinition(typeof(TownSlaveData), 3, null);
    }
    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(Dictionary<Settlement, MercenaryData>)); 
        ConstructContainerDefinition(typeof(Dictionary<string, TownSlaveData>));
        ConstructContainerDefinition(typeof(Dictionary<Settlement, CampaignTime>));

    }
}