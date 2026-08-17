using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldContextCooldownState
    {
        [SaveableField(1)] public string DefinitionId = string.Empty;
        [SaveableField(2)] public CampaignTime LastTriggeredAt;
        [SaveableField(3)] public float CooldownDays;
    }
}
