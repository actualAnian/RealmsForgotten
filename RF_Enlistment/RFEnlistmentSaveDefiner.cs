using TaleWorlds.SaveSystem;

namespace RF_Enlistment;

public sealed class RFEnlistmentSaveDefiner : SaveableTypeDefiner
{
    public RFEnlistmentSaveDefiner()
        : base(8_803_412)
    {
    }

    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(RFEnlistmentServiceRecord), 1);
        AddEnumDefinition(typeof(RFEnlistmentRank), 2);
        AddEnumDefinition(typeof(RFEnlistmentAssignment), 3);
    }
}
