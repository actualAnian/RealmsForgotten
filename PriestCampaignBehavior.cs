using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using Bannerlord.Module1.Religions;

public class PriestCampaignBehavior : CampaignBehaviorBase
{
    private CeremonyQuestBehavior _ceremonyQuestBehavior;
    private ProcessionEscortQuestBehavior _processionEscortQuestBehavior;

    public PriestCampaignBehavior(CeremonyQuestBehavior ceremonyQuestBehavior, ProcessionEscortQuestBehavior processionEscortQuestBehavior)
    {
        _ceremonyQuestBehavior = ceremonyQuestBehavior;
        _processionEscortQuestBehavior = processionEscortQuestBehavior;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_ceremonyQuestBehavior", ref _ceremonyQuestBehavior);
        dataStore.SyncData("_processionEscortQuestBehavior", ref _processionEscortQuestBehavior);
        InformationManager.DisplayMessage(new InformationMessage($"SyncData called for {this.GetType().Name}"));
    }

    private void OnSessionLaunched(CampaignGameStarter campaignStarter)
    {
        EnsurePriestInSettlement();
        AddTownMenuOption(campaignStarter);
        AddDialogues(campaignStarter);
    }

    private void AddTownMenuOption(CampaignGameStarter campaignStarter)
    {
        campaignStarter.AddGameMenuOption("town", "town_visit_priest", "Visit the Priest",
            gameMenuOption => true,
            gameMenuOption => StartPriestConversation(),
            false,
            5
        );
    }

    private void AddDialogues(CampaignGameStarter campaignStarter)
    {
        campaignStarter.AddDialogLine("priest_start", "start", "priest_talk", "Greetings my son, how can I assist you?",
            () => CharacterObject.OneToOneConversationCharacter == Hero.OneToOneConversationHero?.CharacterObject,
            null);

        campaignStarter.AddPlayerLine("priest_response", "priest_talk", "priest_missions", "I am looking for pious work. Do you need any assistance?",
            () => true,
            null);

        campaignStarter.AddDialogLine("priest_missions", "priest_missions", "priest_missions_options", "Yes, I do need some assistance. Would you like to help with a religious ceremony or escort a procession?",
            () => true,
            null);

        campaignStarter.AddPlayerLine("priest_mission_ceremony", "priest_missions_options", "close_window", "I would like to help with the religious ceremony.",
            () => true,
            () => _ceremonyQuestBehavior.TriggerReligiousCeremonyFromDialogue());

        campaignStarter.AddPlayerLine("priest_mission_procession", "priest_missions_options", "priest_procession_accepted", "There is a procession leading to {TARGET_TOWN} that needs protection.",
            () => _ceremonyQuestBehavior.CeremonyCompleted,
            () => _processionEscortQuestBehavior.TriggerEscortProcessionDirectlyFromDialogue());

        campaignStarter.AddDialogLine("priest_procession_accepted", "priest_procession_accepted", "close_window", "Thank you. Please ensure their safe passage.",
            () => true,
            null);

        campaignStarter.AddPlayerLine("priest_mission_decline", "priest_missions_options", "close_window", "Not at the moment, thank you.",
            () => true,
            null);
    }

    private void EnsurePriestInSettlement()
    {
        var settlement = Settlement.CurrentSettlement;
        if (settlement == null)
        {
            InformationManager.DisplayMessage(new InformationMessage("Current settlement is null."));
            return;
        }

        var priest = MBObjectManager.Instance.GetObject<CharacterObject>("keep_monastery_monk");
        if (priest == null)
        {
            InformationManager.DisplayMessage(new InformationMessage("Priest not found."));
            return;
        }

        var priestHero = Hero.FindFirst(h => h.CharacterObject == priest);
        if (priestHero == null)
        {
            priestHero = HeroCreator.CreateSpecialHero(priest, settlement);
            priestHero.StringId = "keep_monastery_monk";
            priestHero.SetName(new TextObject("{=keep_monastery_monk}Monk"), new TextObject("Monk"));
            AddHeroToSettlement(priestHero, settlement);
        }
    }

    private void StartPriestConversation()
    {
        var priestHero = Hero.FindFirst(h => h.StringId == "keep_monastery_monk");
        if (priestHero == null)
        {
            InformationManager.DisplayMessage(new InformationMessage("Priest hero not found."));
            return;
        }

        CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(priestHero.CharacterObject));
    }

    private void AddHeroToSettlement(Hero hero, Settlement settlement)
    {
        if (settlement != null && hero != null)
        {
            hero.ChangeState(Hero.CharacterStates.Active);
            settlement.HeroesWithoutParty.Add(hero);
        }
    }
}






