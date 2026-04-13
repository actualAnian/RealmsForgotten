using System.Collections.Generic;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class StrategicIntrigueTypeDefiner : SaveableTypeDefiner
{
    public StrategicIntrigueTypeDefiner()
        : base(StrategicIntrigueConstants.SaveBaseId)
    {
    }

    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(ClanIntrigueState), 1);
        AddClassDefinition(typeof(SecretPact), 2);
        AddClassDefinition(typeof(IntrigueOperation), 3);
        AddClassDefinition(typeof(KingdomIntrigueState), 4);
        AddClassDefinition(typeof(SecretAllianceCompact), 5);
    }

    protected override void DefineEnumTypes()
    {
        AddEnumDefinition(typeof(IntriguePactGoal), 11);
        AddEnumDefinition(typeof(IntrigueOperationType), 12);
        AddEnumDefinition(typeof(IntrigueOperationStatus), 13);
        AddEnumDefinition(typeof(IntrigueAllianceObjective), 14);
        AddEnumDefinition(typeof(IntrigueAllianceRewardType), 15);
    }

    protected override void DefineContainerDefinitions()
    {
        ConstructContainerDefinition(typeof(List<SecretPact>));
        ConstructContainerDefinition(typeof(List<SecretAllianceCompact>));
        ConstructContainerDefinition(typeof(List<IntrigueOperation>));
        ConstructContainerDefinition(typeof(Dictionary<Clan, ClanIntrigueState>));
        ConstructContainerDefinition(typeof(Dictionary<Kingdom, KingdomIntrigueState>));
    }
}
