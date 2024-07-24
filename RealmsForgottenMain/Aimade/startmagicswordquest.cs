using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade;

public class RetrieveSwordQuestBehavior : CampaignBehaviorBase
{
    private ItemObject _targetSword;
    private Hero _questGiver;
    private QuestStage _currentStage = QuestStage.NotStarted;

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        AddDialogues(campaignGameStarter);
    }

    

    private void AddDialogues(CampaignGameStarter campaignGameStarter)
    {
        var dialogFlow = DialogFlow.CreateDialogFlow("start", 125)
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_1"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_2"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_3"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_4"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_5"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_6"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_7"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_8"))
            .PlayerLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_9"))
            .NpcLine(GameTexts.FindText("rf_fourth_quest_monk_dialog_10"))
            // Continue adding the lines in the correct sequence
            .CloseDialog(); // Ends the dialogue flow
    }

    private void OnTick(float dt)
    {
        if (_currentStage == QuestStage.Searching && PlayerIsInLocation("ice_tower"))
        {
            _currentStage = QuestStage.FoundSword;
            InformationManager.DisplayMessage(new InformationMessage("You have found the sacred sword! Return it to the high maester."));
            GiveItemToMainHero(_targetSword);
        }
    }

    private bool PlayerIsInLocation(string locationId)
    {
        return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.StringId.Equals(locationId, StringComparison.OrdinalIgnoreCase);
    }

    private void GiveItemToMainHero(ItemObject item)
    {
        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, 1);
    }
    public override void SyncData(IDataStore dataStore)
    {
        // Implement if there's any data that needs syncing
    }
}

public enum QuestStage
{
    NotStarted,
    Searching,
    FoundSword,
    Returning,
    Completed
}