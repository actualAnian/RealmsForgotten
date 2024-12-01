using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career;

public static class QuestCompletionExtensions
{
    public static void RaiseQuestCompleted(this QuestBase quest)
    {
        CustomCampaignEvents.RaiseQuestCompletedEvent(quest);
    }
}