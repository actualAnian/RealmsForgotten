using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldPendingEvent
    {
        [SaveableField(1)] public string DefinitionId = string.Empty;
        [SaveableField(2)] public Settlement? Origin;
        [SaveableField(3)] public Settlement? Destination;
        [SaveableField(4)] public CampaignTime CreatedAt;
    }
}
