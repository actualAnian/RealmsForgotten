using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Conversation;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class RuinsQuest : QuestBase
    {
        [SaveableField(1)] private JournalLog _ruinsLog;
        [SaveableField(2)] private Village _targetVillage;
        [SaveableField(3)] private Hideout _targetHideout;
        [SaveableField(4)] private bool _questResolved;

        private const string QuestIdConst = "rf_ruins_quest";

        public RuinsQuest(Village targetVillage, Hideout targetHideout)
            : base(QuestIdConst, Hero.MainHero, CampaignTime.DaysFromNow(20), 800)
        {
            _targetVillage = targetVillage;
            _targetHideout = targetHideout;
            _questResolved = false;

            InitializeLogs();
            RegisterEvents();
            StartQuest();
        }

        public RuinsQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold) { }

        public override TextObject Title => new TextObject("{=rf_ruins_title}The Lost Relic of the Ruins");
        public override string SpecialQuestType => "RfRuins";
        public override bool IsRemainingTimeHidden => false;

        private void InitializeLogs()
        {
            TextObject logText = new TextObject("{=rf_ruins_log}The villagers of {VILLAGE} claim a sacred relic has been stolen. Bandits took it to their hideout nearby. Track it down and recover it.");
            logText.SetTextVariable("VILLAGE", _targetVillage.Name);
            _ruinsLog = AddLog(logText);
            AddTrackedObject(_targetVillage.Settlement);
            AddTrackedObject(_targetHideout.Settlement);
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (_questResolved) return;

            // Check if battle was at the target hideout
            if (mapEvent.MapEventSettlement != null && mapEvent.MapEventSettlement.Hideout == _targetHideout)
            {
                if (mapEvent.WinningSide == mapEvent.PlayerSide)
                {
                    // Player won the hideout fight → trigger bandit NPC dialogue
                    Campaign.Current.ConversationManager.AddDialogFlow(
                        RuinsQuestBehavior.GetBanditDialog(this), this);

                    var bandit = CharacterObject.Find("bandit_artifact_thief");
                    if (bandit != null)
                    {
                        CampaignMapConversation.OpenConversation(
                            new ConversationCharacterData(bandit),
                            new ConversationCharacterData(Hero.MainHero.CharacterObject));
                    }

                    _questResolved = true;
                }
            }
        }

        public void OnDecision_FreeBandit()
        {
            AddLog(new TextObject("{=rf_ruins_free}You accepted the bandit's bribe and only returned the relic."));
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000);

            if (_targetVillage?.Settlement != null)
            {
                foreach (var notable in _targetVillage.Settlement.Notables)
                {
                    ChangeRelationAction.ApplyPlayerRelation(notable, -5);
                }
            }

            CompleteQuestWithSuccess();
        }

        public void OnDecision_CaptureBandit()
        {
            AddLog(new TextObject("{=rf_ruins_capture}You captured the thief and returned both him and the relic to the villagers."));
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
            Hero.MainHero.Clan.AddRenown(10);

            if (_targetVillage?.Settlement != null)
            {
                foreach (var notable in _targetVillage.Settlement.Notables)
                {
                    ChangeRelationAction.ApplyPlayerRelation(notable, 5);
                }
            }

            CompleteQuestWithSuccess();
        }

        protected override void InitializeQuestOnGameLoad()
        {
            RegisterEvents();
        }

        protected override void SetDialogs() { }

        protected override void HourlyTick()
        {
            // Não usado nesta quest
        }

        public Settlement TargetSettlement => _targetVillage?.Settlement;
        public Hideout TargetHideout => _targetHideout;
        public bool IsActiveAndNotResolved => IsOngoing && !_questResolved;
    }
}