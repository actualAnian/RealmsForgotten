using System.Collections.Generic;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten;

internal class SaveDefiner : SaveableTypeDefiner
{
    public SaveDefiner() : base(2_87656_493) { }
    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(MercenaryData), 1);
    }
    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(Dictionary<Settlement, MercenaryData>));
    }
}