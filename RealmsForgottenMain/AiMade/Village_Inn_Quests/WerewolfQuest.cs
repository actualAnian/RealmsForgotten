using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.MountAndBlade;
using System;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
   public class WerewolfQuest : QuestBase
    {
        [SaveableField(1)] private JournalLog _werewolfLog;
        [SaveableField(2)] private Village _targetVillage;
        [SaveableField(3)] private bool _werewolfDefeated;
        [SaveableField(4)] private bool _playerJustAcceptedQuest;
        [SaveableField(5)] private MobileParty _werewolfParty;

        private const string QuestIdConst = "rf_werewolf_quest";

        public WerewolfQuest(Village targetVillage)
            : base(QuestIdConst, Hero.MainHero, CampaignTime.DaysFromNow(10), 500)
        {
            _targetVillage = targetVillage;
            _werewolfDefeated = false;
            _playerJustAcceptedQuest = true;

            InitializeLogs();
            RegisterEvents();
            StartQuest();
        }

        public WerewolfQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold) { }

        public override TextObject Title => new TextObject("{=rf_werewolf_title}The Werewolf in the Village");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => false;

        private void InitializeLogs()
        {
            TextObject logText = new TextObject("{=rf_werewolf_log_hint}Return to {VILLAGE} at night to confront the beast that terrorizes the villagers.");
            logText.SetTextVariable("VILLAGE", _targetVillage.Name);
            _werewolfLog = AddLog(logText);
            AddTrackedObject(_targetVillage.Settlement);
        }

        protected override void RegisterEvents()
        {
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
        }

        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            if (!IsOngoing || _werewolfDefeated)
                return;

            if (_playerJustAcceptedQuest)
                _playerJustAcceptedQuest = false;
        }

        // === Batalha estilo Poachers mas forçando só o player ===
        public void StartQuestBattle(Settlement village)
        {
            if (village == null || village.Village != _targetVillage)
                return;

            var werewolfChar = CharacterObject.All.FirstOrDefault(c => c.StringId == "werewolf");
            if (werewolfChar == null)
                return;

            PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            _werewolfParty = BanditPartyComponent.CreateBanditParty("rf_werewolf_party_" + village.StringId, Clan.BanditFactions.First(), null, false, looterTemplate, village.GatePosition); //@TODO

            _werewolfParty.InitializeMobilePartyAroundPosition(
                new TroopRoster(_werewolfParty.Party),
                new TroopRoster(_werewolfParty.Party),
                village.GatePosition, 1f);

            _werewolfParty.MemberRoster.Clear();
            _werewolfParty.MemberRoster.AddToCounts(werewolfChar, 5);
            _werewolfParty.Party.SetCustomName(new TextObject("Werewolf"));
            _werewolfParty.SetPartyUsedByQuest(true);
            _werewolfParty.Ai.DisableAi();

            var backupRoster = MobileParty.MainParty.MemberRoster.CloneRosterData();

            try
            {
                MobileParty.MainParty.MemberRoster.Clear();
                MobileParty.MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, true);

                PlayerEncounter.RestartPlayerEncounter(MobileParty.MainParty.Party, _werewolfParty.Party, false);
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();

                CampaignMission.OpenBattleMission(village.LocationComplex.GetScene("village_center", 1), false);

                CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

                AddLog(new TextObject("{=rf_werewolf_started}The duel against the werewolf has begun."));
            }
            finally
            {
                MobileParty.MainParty.MemberRoster.Clear();
                MobileParty.MainParty.MemberRoster.Add(backupRoster);
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (_werewolfParty == null || mapEvent == null)
                return;

            bool wasAttacker = mapEvent.AttackerSide.Parties.Any(p => p.Party == _werewolfParty.Party);
            bool wasDefender = mapEvent.DefenderSide.Parties.Any(p => p.Party == _werewolfParty.Party);

            if (wasAttacker || wasDefender)
            {
                var winningSide = mapEvent.WinningSide;
                bool werewolfWon =
                    (winningSide == BattleSideEnum.Attacker && wasAttacker) ||
                    (winningSide == BattleSideEnum.Defender && wasDefender);

                if (werewolfWon)
                    OnWerewolfFailed();
                else
                    OnWerewolfDefeated();

                DestroyPartyAction.Apply(null, _werewolfParty);
                _werewolfParty = null;
            }
        }

        public void OnWerewolfDefeated()
        {
            if (_werewolfDefeated) return;
            _werewolfDefeated = true;

            AddLog(new TextObject("{=rf_werewolf_complete}You have defeated the werewolf, bringing peace to the village."));
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);
            Hero.MainHero.Clan.AddRenown(5);
                     
            GainRenownAction.Apply(Hero.MainHero, 2);

            // Melhora relação com todos os notables da vila
            if (_targetVillage?.Settlement != null)
            {
                foreach (var notable in _targetVillage.Settlement.Notables)
                {
                    ChangeRelationAction.ApplyPlayerRelation(notable, 5); // +5 de relação
                }
            }
            CompleteQuestWithSuccess();
        }

        public void OnWerewolfFailed()
        {
            AddLog(new TextObject("{=rf_werewolf_fail}You were defeated by the beast. It continues to terrorize the night."));
            CompleteQuestWithFail();
        }

        protected override void InitializeQuestOnGameLoad()
        {
            RegisterEvents();
        }

        protected override void SetDialogs() { }
        protected override void HourlyTick() { }

        // === Novas propriedades expostas para o menu condicional ===
        public Settlement TargetSettlement => _targetVillage?.Settlement;
        public bool IsActiveAndNotResolved => IsOngoing && !_werewolfDefeated;
    }
}