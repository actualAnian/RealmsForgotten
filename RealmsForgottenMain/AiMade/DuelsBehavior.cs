using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade;

public class DuelCampaignBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        AddDuelOptions(starter);
    }

    private void AddDuelOptions(CampaignGameStarter starter)
    {
        // Add dialogues or interactions that lead to a duel
        starter.AddPlayerLine("challenge_to_duel", "lord_talk", "lord_pretalk",
            "I challenge you to a duel!", CanChallengeToDuel, StartDuel, 100);
        starter.AddDialogLine("duel_accept_line", "lord_pretalk", "close_window",
            "Very well, prepare yourself!", null, null);
    }

    private bool CanChallengeToDuel()
    {
        return Hero.OneToOneConversationHero != null && !Hero.MainHero.IsWounded;
    }

    private void StartDuel()
    {
        CharacterObject opponent = Hero.OneToOneConversationHero.CharacterObject;
        bool onHorse = false; // Example setting, adjust as needed
        bool isFriendly = false; // Example setting, adjust as needed
        bool isInsideSettlement = false; // Example setting, adjust as needed
        string sceneId = "scene_village_arena"; // Example scene ID, adjust as needed

        DuelMission.OpenDuelMission(sceneId, opponent, onHorse, isFriendly, isInsideSettlement);
    }

    public override void SyncData(IDataStore dataStore) { }
}