using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class RuinsQuestBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore) { }

        public static DialogFlow GetBanditDialog(RuinsQuest quest)
        {
            return DialogFlow.CreateDialogFlow("start")
                .NpcLine(new TextObject("{=rf_bandit_intro}Spare me! I have the relic, take it. Perhaps some gold too, if you let me go..."))
                .Condition(() => Hero.OneToOneConversationHero != null &&
                                 Hero.OneToOneConversationHero.CharacterObject.StringId == "bandit_artifact_thief")
                .BeginPlayerOptions()
                    .PlayerOption(new TextObject("{=rf_bandit_free}Very well. Keep your life, and I'll take the relic and your bribe."))
                        .Consequence(() => quest.OnDecision_FreeBandit())
                    .CloseDialog()
                    .PlayerOption(new TextObject("{=rf_bandit_capture}No mercy. You will face justice for your crimes."))
                        .Consequence(() => quest.OnDecision_CaptureBandit())
                    .CloseDialog()
                .EndPlayerOptions();
        }
    }
}
