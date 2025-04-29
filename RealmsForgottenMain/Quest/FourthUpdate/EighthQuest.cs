using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class EighthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog _meetPriestessLog;

        [SaveableField(1)]
        private JournalLog _findSacredObjectLog;

        [SaveableField(2)]
        private JournalLog _returnSacredObjectLog;

        private bool _conversationTriggered = false;

        private bool PlayerHasSacredObject()
        {
            var sacredObject = MBObjectManager.Instance.GetObject<ItemObject>("sacred_object");
            return sacredObject != null && MobileParty.MainParty.ItemRoster.GetItemNumber(sacredObject) > 0;
        }
        public EighthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold)
        {
            InitializeQuestOnCreation();
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickCheck);
        }

        protected override void OnStartQuest()
        {
            _meetPriestessLog = AddDiscreteLog(
                new TextObject("The Priestess Beckons"),
                new TextObject("Return to the First Tree and speak with the High Priestess about the deformed beings."),
                0, 1
            );

            InformationManager.ShowInquiry(new InquiryData(
                "🌳 The Priestess Awaits",
                "Strange deformations spread across the land. The First Tree Priestess summons you.",
                true, false, "Continue", null, null, null
            ));
        }

        protected override void OnTimedOut()
        {
            CompleteQuestWithFail();
            InformationManager.DisplayMessage(new InformationMessage("You failed to uncover the source of the corruption."));
        }

        protected override void OnFinalize()
        {
            // Clean up if needed
        }

        public override TextObject Title => new TextObject("Eighth Quest: Call of the First Tree");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => false;

        private void HourlyTickCheck()
        {
            if (_meetPriestessLog != null && !_conversationTriggered)
            {
                Hero priestess = Hero.FindFirst(hero => hero.StringId == "elvean_first_tree_druid_quest");

                if (priestess != null && MobileParty.MainParty.CurrentSettlement != null &&
                    MobileParty.MainParty.CurrentSettlement.StringId == "town_FirstTree")
                {
                    StartPriestessConversation(priestess);
                    _conversationTriggered = true;
                }
            }
        }

        private void StartPriestessConversation(Hero priestess)
        {
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessDialogFlow, this);
            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(CharacterObject.PlayerCharacter),
                new ConversationCharacterData(priestess.CharacterObject)
            );
        }

        private DialogFlow PriestessDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
            .NpcLine(new TextObject("For months, I have been collecting reports of a strange aberration occurring in various parts of Aeurth. Groups of deformed beings have been reported assaulting villagers and caravans. It seems as if some kind of plague has taken hold of them; some say they appear possessed by an evil force. We need you to find out where they are coming from."))
            .Condition(() => _meetPriestessLog?.CurrentProgress == 0
                && CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == "elvean_first_tree_druid_quest")
            .PlayerLine(new TextObject("What do you advice?"))
            .NpcLine(new TextObject("The latest reports have come from the eastern borders of the Nasorian Kingdom. I suggest you take advantage of this fresh information and start your research over there."))
            .Consequence(() =>
            {
                _meetPriestessLog.UpdateCurrentProgress(1);

                // 🌟 NOW start the second objective: find the sacred object
                _findSacredObjectLog = AddDiscreteLog(
                    new TextObject("Find the origin of those aberrations."),
                    new TextObject("The Priestess has told you about the the possible location to start your search. Travel there and find any clues."),
                    0, 1
                );

                InformationManager.DisplayMessage(new InformationMessage("New objective: Find the Sacred Object!"));
            })
            .CloseDialog();

        private DialogFlow PriestessReturnDialogFlow => DialogFlow.CreateDialogFlow("start", 125)
    .NpcLine(new TextObject("You have found it... The path ahead grows darker still. But for now, you have done enough."))
    .Condition(() =>
        _returnSacredObjectLog != null &&
        _returnSacredObjectLog.CurrentProgress == 1 &&
        CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == "elvean_first_tree_priestess" &&
        PlayerHasSacredObject()
    )
    .PlayerLine(new TextObject("I await your guidance for what comes next."))
    .Consequence(() =>
    {
        _returnSacredObjectLog.UpdateCurrentProgress(2);
        CompleteQuestWithSuccess();

        InformationManager.ShowInquiry(new InquiryData(
            "🌿 To Be Continued...",
            "The Priestess smiles faintly. 'Your destiny has only begun to unfold...'\n\n(To Be Continued in a future update!)",
            true, false,
            "Close", null, null, null
        ));
    })
    .CloseDialog();

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessDialogFlow, this);
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessReturnDialogFlow, this);
        }

        protected override void HourlyTick()
        {
            CheckIfPlayerHasSacredItem();
        }

        private void CheckIfPlayerHasSacredItem()
        {
            ItemObject sacredObject = MBObjectManager.Instance.GetObject<ItemObject>("sacred_object");
            if (sacredObject == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Sacred object item not found!", Colors.Red));
                return;
            }

            int itemCount = MobileParty.MainParty.ItemRoster.GetItemNumber(sacredObject);

            if (itemCount > 0)
            {
                if (_findSacredObjectLog != null && _findSacredObjectLog.CurrentProgress == 0)
                {
                    _findSacredObjectLog.UpdateCurrentProgress(1);

                    // 🆕 Create new log for returning the object
                    _returnSacredObjectLog = AddDiscreteLog(
                        new TextObject("Return the Sacred Object"),
                        new TextObject("Return the Sacred Object to the Priestess at the First Tree."),
                        0, 1
                    );

                    InformationManager.ShowInquiry(new InquiryData(
                        "Sacred Object Found!",
                        "You have discovered an strange object. Return it to the Priestess.",
                        true, false,
                        "Continue", null, null, null
                    ));
                    _returnSacredObjectLog.UpdateCurrentProgress(1);

                }
            }
        }

    }
}