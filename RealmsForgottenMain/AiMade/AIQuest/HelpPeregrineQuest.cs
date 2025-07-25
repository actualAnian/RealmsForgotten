using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace RealmsForgotten.AiMade
{
    public class HelpPeregrineQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog _escortObjective;

        [SaveableField(1)]
        private Hero _monkHero;

        [SaveableField(2)]
        private Settlement _destination;

        [SaveableField(3)]
        private CampaignTime _questStartTime;

        private bool _banditAttackSpawned = false;

        public HelpPeregrineQuest(string questId, Hero monkHero, CampaignTime duration, Settlement destination)
            : base(questId, monkHero, CampaignTime.Now + duration, 500)
        {
            _destination = destination;
            _monkHero = monkHero;

            InitializeQuestOnCreation();
        }

        protected override void OnStartQuest()
        {
            _questStartTime = CampaignTime.Now;

            if (_monkHero != null)
            {
                AddHeroToPartyAction.Apply(_monkHero, MobileParty.MainParty, true);

                if (!Clan.PlayerClan.Heroes.Contains(_monkHero))
                    Clan.PlayerClan.Heroes.Add(_monkHero);

                _escortObjective = AddDiscreteLog(
                    new TextObject("Help the Peregrine Monk"),
                    new TextObject($"Escort the monk safely to {_destination.Name}."),
                    0, 1
                );

                InformationManager.DisplayMessage(new InformationMessage($"✅ Monk '{_monkHero.Name}' joined your party.", Colors.Green));
            }
            else
            {
                CompleteQuestWithFail();
                InformationManager.DisplayMessage(new InformationMessage("❌ Monk hero not found!", Colors.Red));
            }
        }

        protected override void OnTimedOut()
        {
            FailEscort("⏳ You took too long, and the monk left.");
        }

        protected override void OnFinalize()
        {
            if (_monkHero != null && _monkHero.PartyBelongedTo == MobileParty.MainParty)
            {
                MobileParty.MainParty.MemberRoster.RemoveTroop(_monkHero.CharacterObject);
            }
        }

        protected override void HourlyTick()
        {
            if (_questStartTime == CampaignTime.Never)
                return;

            if (CampaignTime.Now.ToHours - _questStartTime.ToHours < 2f)
                return;

            if (_monkHero == null || _monkHero.PartyBelongedTo != MobileParty.MainParty)
            {
                FailEscort("❌ The monk is no longer in your party.");
                return;
            }

            float distanceToDestination = MobileParty.MainParty.Position2D.Distance(_destination.Position2D);

            if (!_banditAttackSpawned && distanceToDestination < 10f)
            {
                SpawnBanditParty();
                _banditAttackSpawned = true;
            }

            if (distanceToDestination < 5f)
            {
                _escortObjective.UpdateCurrentProgress(1);
                CompleteQuestWithSuccess();
                InformationManager.DisplayMessage(new InformationMessage("✅ You successfully escorted the monk to safety!", Colors.Green));
            }
        }

        private void SpawnBanditParty()
        {
            CharacterObject looter = CharacterObject.Find("looter");
            if (looter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter character not found.", Colors.Red));
                return;
            }

            Clan looterClan = Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters");
            if (looterClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter clan not found.", Colors.Red));
                return;
            }

            PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            if (looterTemplate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter party template not found.", Colors.Red));
                return;
            }

            Vec2 spawnPosition = MobileParty.MainParty.Position2D + new Vec2(MBRandom.RandomFloatRanged(2f, 4f), MBRandom.RandomFloatRanged(2f, 4f));

            MobileParty banditParty = BanditPartyComponent.CreateLooterParty("peregrine_bandit_party", looterClan, null, false);
            banditParty.InitializeMobilePartyAroundPosition(looterTemplate, spawnPosition, 1f);
            banditParty.MemberRoster.AddToCounts(looter, 20);
            banditParty.SetCustomName(new TextObject("Bandit Ambush"));
            banditParty.IsVisible = true;

            if (banditParty.Ai != null)
            {
                banditParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                banditParty.Ai.SetDoNotMakeNewDecisions(true);
            }

            InformationManager.DisplayMessage(new InformationMessage("⚔️ A bandit party has appeared near your location!", Colors.Red));
        }

        private void FailEscort(string reason)
        {
            CompleteQuestWithFail();
            InformationManager.DisplayMessage(new InformationMessage(reason, Colors.Red));
        }

        public override TextObject Title => new TextObject("{=HelpPeregrineQuestTitle}Help the Peregrine Monk");
        public override bool IsSpecialQuest => false;
        public override bool IsRemainingTimeHidden => false;
        protected override void SetDialogs() { }
        protected override void InitializeQuestOnGameLoad() { }
    }
}

