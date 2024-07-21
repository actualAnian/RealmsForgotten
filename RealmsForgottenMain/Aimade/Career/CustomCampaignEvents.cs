using System;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public static class CustomCampaignEvents
    {
        public static event Action<QuestBase> OnQuestCompleted;

        public static void RaiseQuestCompletedEvent(QuestBase quest)
        {
            OnQuestCompleted?.Invoke(quest);
        }
    }
}
