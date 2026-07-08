using System.Collections.Generic;
using System.Linq;
using Helpers;
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

namespace RealmsForgotten.WorldState.Refugees
{
    /// <summary>
    /// World-state system: completed village raids displace part of the
    /// population as wandering refugee bands. They drift between settlements,
    /// merge when they meet, can be recruited by the player, dissolve into a
    /// town after enough time — or, if they grow large enough with nowhere to
    /// go, turn to banditry.
    /// </summary>
    public class RefugeeCampaignBehavior : CampaignBehaviorBase
    {
        private const float RefugeesPerHearthRatio = 0.25f;
        private const int MinPartySize = 8;
        private const int MaxPartySize = 45;
        private const int MaxRefugeeParties = 15;
        private const float MergeDistance = 6f;
        private const int BanditThreshold = 40;
        private const float MinDaysBeforeBanditry = 14f;
        private const float AbsorbAfterDays = 21f;
        private const float TownAbsorbDistance = 20f;
        private const float WanderMinDistance = 15f;
        private const float WanderMaxDistance = 60f;
        private const int GoldCostPerRefugee = 15;
        private const float MoralePenaltyPerRefugee = 0.25f;
        private const float MoralePenaltyCap = 15f;
        private const float MaxUntrainedSpeedPenalty = 0.35f;

        private static RefugeeCampaignBehavior _instance;

        private Clan _refugeeClan;
        private int _spawnCounter;
        private int _untrainedRefugees;

        public RefugeeCampaignBehavior()
        {
            _instance = this;
        }

        /// <summary>
        /// Speed factor applied by RFPartySpeedCalculatingModel: the party is
        /// slowed in proportion to how much of it is untrained refugees —
        /// a handful barely registers, a column of civilians crawls.
        /// </summary>
        public static float GetUntrainedSpeedFactor(MobileParty party)
        {
            if (_instance == null || _instance._untrainedRefugees <= 0 || party != MobileParty.MainParty)
            {
                return 0f;
            }

            int partySize = party.MemberRoster?.TotalManCount ?? 0;
            if (partySize <= 0)
            {
                return 0f;
            }

            float ratio = MathF.Min(1f, _instance._untrainedRefugees / (float)partySize);
            return -MaxUntrainedSpeedPenalty * ratio;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, OnPlayerUpgradedTroops);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfRefugeeClan", ref _refugeeClan);
            dataStore.SyncData("_rfRefugeeSpawnCounter", ref _spawnCounter);
            dataStore.SyncData("_rfUntrainedRefugees", ref _untrainedRefugees);
        }

        // ── Untrained recruits: the army stays disorganized while refugee
        //    villagers remain in the ranks; upgrading them clears the debt. ──

        private void OnPlayerUpgradedTroops(CharacterObject upgradeFrom, CharacterObject upgradeTo, int number)
        {
            if (_untrainedRefugees <= 0 || upgradeFrom?.Occupation != Occupation.Villager)
            {
                return;
            }

            _untrainedRefugees = MBMath.ClampInt(_untrainedRefugees - number, 0, int.MaxValue);
            if (_untrainedRefugees == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=rf_refugee_trained}The refugees have found their footing as soldiers — the ranks are orderly again.").ToString(),
                    Color.FromUint(0xFFC8B87Au)));
            }
        }

        private int CountVillagersInMainParty()
        {
            int count = 0;
            foreach (TroopRosterElement element in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (element.Character?.Occupation == Occupation.Villager)
                {
                    count += element.Number;
                }
            }

            return count;
        }

        private static IEnumerable<MobileParty> RefugeeParties()
        {
            return MobileParty.All.Where(p => p?.PartyComponent is RefugeePartyComponent && p.IsActive);
        }

        // ── Spawning ─────────────────────────────────────────────────────────

        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
        {
            // The event fires when the raid MapEvent finalizes for ANY reason —
            // carried to the end (attacker victory) or interrupted (defender
            // relief, attacker withdrawal). Refugees are displaced either way,
            // in proportion to how far the raid got: RaidDamage accumulates
            // 0→1 while the village burns, so a raid broken off early frees a
            // handful of families and a full sack empties the village.
            Settlement settlement = raidEvent?.MapEventSettlement;
            if (settlement?.IsVillage != true || RefugeeParties().Count() >= MaxRefugeeParties)
            {
                return;
            }

            CharacterObject villager = settlement.Culture?.Villager
                ?? settlement.Village?.Bound?.Culture?.Villager;
            if (villager == null)
            {
                return;
            }

            float raidProgress = MBMath.ClampFloat(raidEvent.RaidDamage, 0f, 1f);
            float hearths = settlement.Village?.Hearth ?? 0f;
            int size = (int)(hearths * RefugeesPerHearthRatio * raidProgress * MBRandom.RandomFloatRanged(0.7f, 1.3f));
            if (size < MinPartySize)
            {
                // Raid barely started before being broken off — nobody fled yet.
                return;
            }

            size = MBMath.ClampInt(size, MinPartySize, MaxPartySize);

            Clan clan = GetOrCreateRefugeeClan(settlement);
            if (clan == null)
            {
                return;
            }

            TroopRoster members = TroopRoster.CreateDummyTroopRoster();
            members.AddToCounts(villager, size);

            _spawnCounter++;
            MobileParty party = MobileParty.CreateParty(
                $"rf_refugees_{settlement.StringId}_{_spawnCounter}",
                new RefugeePartyComponent(settlement));
            party.ActualClan = clan;
            party.InitializeMobilePartyAroundPosition(
                members, TroopRoster.CreateDummyTroopRoster(), settlement.GatePosition, 1f);
            // They fled with SOMETHING in the carts: without food the party
            // starves and silently wastes away within days.
            party.ItemRoster.AddToCounts(DefaultItems.Grain, MathF.Max(5, size / 2));
            // Set off at once: refugees do not linger at the smoking ruin.
            SetWanderTarget(party);

            TextObject message = raidProgress >= 0.99f
                ? new TextObject("{=rf_refugee_fled}Refugees flee the razed village of {VILLAGE}.")
                : new TextObject("{=rf_refugee_fled_partial}Refugees flee the burning village of {VILLAGE}.");
            message.SetTextVariable("VILLAGE", settlement.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFC8B87Au)));
        }

        private Clan GetOrCreateRefugeeClan(Settlement seedSettlement)
        {
            if (_refugeeClan != null && !_refugeeClan.IsEliminated)
            {
                return _refugeeClan;
            }

            _refugeeClan = Clan.All.FirstOrDefault(c => c.StringId == "rf_displaced_folk" && !c.IsEliminated);
            if (_refugeeClan != null)
            {
                EnsureClanLeader(_refugeeClan, seedSettlement);
                return _refugeeClan;
            }

            // Same runtime-creation recipe vanilla uses for rebel clans.
            Clan clan = Clan.CreateClan("rf_displaced_folk");
            TextObject name = new("{=rf_displaced_folk}Displaced Villagers");
            clan.ChangeClanName(name, name);
            clan.Culture = seedSettlement.Culture;
            uint primary = seedSettlement.MapFaction?.Banner?.GetPrimaryColor() ?? 0xFF6E5B3Cu;
            uint secondary = seedSettlement.MapFaction?.Banner?.GetFirstIconColor() ?? 0xFFD8C9A3u;
            clan.Banner = Banner.CreateOneColoredBannerWithOneIcon(secondary, primary, -1);
            clan.SetInitialHomeSettlement(seedSettlement);
            _refugeeClan = clan;
            EnsureClanLeader(clan, seedSettlement);
            return clan;
        }

        /// <summary>
        /// Vanilla assumes every non-bandit clan has a living leader — e.g.
        /// ClanVariablesCampaignBehavior.MakeClanFinancialEvaluation dereferences
        /// clan.Leader.Gold on the daily tick and crashes otherwise. Keep a
        /// symbolic "elder" hero at the head of the refugee clan at all times.
        /// </summary>
        private void EnsureClanLeader(Clan clan, Settlement seedSettlement)
        {
            if (clan == null || (clan.Leader != null && clan.Leader.IsAlive))
            {
                return;
            }

            CharacterObject template = seedSettlement?.Culture?.Villager
                ?? clan.Culture?.Villager
                ?? CharacterObject.All.FirstOrDefault(c => c.Occupation == Occupation.Villager);
            if (template == null)
            {
                return;
            }

            Settlement home = seedSettlement?.Village?.Bound ?? seedSettlement ?? clan.HomeSettlement;
            Hero elder = HeroCreator.CreateSpecialHero(template, home, clan, null, 52);
            TextObject elderName = new("{=rf_refugee_elder}Elder of the Displaced");
            elder.SetName(elderName, elderName);
            elder.SetNewOccupation(Occupation.Special);
            elder.ChangeState(Hero.CharacterStates.Active);
            clan.SetLeader(elder);

            if (home != null)
            {
                EnterSettlementAction.ApplyForCharacterOnly(elder, home);
            }
        }

        // ── Life cycle ───────────────────────────────────────────────────────

        private void OnHourlyTick()
        {
            if (_untrainedRefugees > 0)
            {
                // Deaths/dismissals also settle the debt: never owe more than the
                // villagers actually still in the ranks. The speed penalty itself
                // is applied proportionally by RFPartySpeedCalculatingModel via
                // GetUntrainedSpeedFactor.
                _untrainedRefugees = MBMath.ClampInt(_untrainedRefugees, 0, CountVillagersInMainParty());
            }

            List<MobileParty> parties = RefugeeParties().Where(p => p.MapEvent == null && p.CurrentSettlement == null).ToList();
            for (int i = 0; i < parties.Count; i++)
            {
                for (int j = i + 1; j < parties.Count; j++)
                {
                    if (!parties[i].IsActive || !parties[j].IsActive)
                    {
                        continue;
                    }

                    if (parties[i].Position.Distance(parties[j].Position) > MergeDistance)
                    {
                        continue;
                    }

                    MobileParty bigger = parties[i].MemberRoster.TotalManCount >= parties[j].MemberRoster.TotalManCount ? parties[i] : parties[j];
                    MobileParty smaller = bigger == parties[i] ? parties[j] : parties[i];
                    bigger.MemberRoster.Add(smaller.MemberRoster);
                    DestroyPartyAction.Apply(null, smaller);
                    // Banditry is decided on the daily tick: a large band must
                    // also have been on the road long enough to grow desperate.
                }
            }
        }

        private void OnDailyTick()
        {
            // Self-heal: if the elder somehow died or was removed, replace him
            // before vanilla's daily clan ticks dereference clan.Leader.
            if (_refugeeClan != null && !_refugeeClan.IsEliminated)
            {
                EnsureClanLeader(_refugeeClan, _refugeeClan.HomeSettlement);
            }

            foreach (MobileParty party in RefugeeParties().ToList())
            {
                var component = (RefugeePartyComponent)party.PartyComponent;

                // Desperation: a band that grew large and has wandered rootless
                // for weeks finally turns to banditry.
                if (party.MemberRoster.TotalManCount >= BanditThreshold
                    && component.CreatedTime.ElapsedDaysUntilNow >= MinDaysBeforeBanditry)
                {
                    TurnToBanditry(party);
                    continue;
                }

                if (component.CreatedTime.ElapsedDaysUntilNow >= AbsorbAfterDays)
                {
                    Settlement town = Settlement.All.FirstOrDefault(s =>
                        s.IsTown && s.GatePosition.Distance(party.Position) <= TownAbsorbDistance);
                    if (town != null)
                    {
                        TextObject message = new("{=rf_refugee_settled}The refugees of {VILLAGE} have settled in {TOWN}.");
                        message.SetTextVariable("VILLAGE", component.HomeVillage?.Name ?? new TextObject("{=rf_refugee_unknown_home}a razed village"));
                        message.SetTextVariable("TOWN", town.Name);
                        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFC8B87Au)));
                        DestroyPartyAction.Apply(null, party);
                        continue;
                    }
                }

                // Scraping by: the exact idiom vanilla uses to keep its own
                // scripted refugee parties alive — enough grain not to starve,
                // never enough to be comfortable.
                if (party.Party.IsStarving)
                {
                    party.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                }

                // Always on the move: every day the band sets out toward another
                // settlement — refugees searching for a home, never camping in
                // place at the ruin they fled.
                if (party.MapEvent == null && party.CurrentSettlement == null)
                {
                    SetWanderTarget(party);
                }
            }
        }

        private static void SetWanderTarget(MobileParty party)
        {
            Settlement target = Settlement.All
                .Where(s => (s.IsTown || s.IsVillage)
                    && s.GatePosition.Distance(party.Position) >= WanderMinDistance
                    && s.GatePosition.Distance(party.Position) <= WanderMaxDistance)
                .GetRandomElementInefficiently()
                ?? Settlement.All
                    .Where(s => s.IsTown || s.IsVillage)
                    .GetRandomElementInefficiently();
            if (target != null)
            {
                party.SetMoveGoToPoint(target.GatePosition, MobileParty.NavigationType.Default);
            }
        }

        private void TurnToBanditry(MobileParty refugeeParty)
        {
            Clan looterClan = Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters")
                ?? Clan.BanditFactions.FirstOrDefault(c => c.StringId == "cs_looters")
                ?? Clan.BanditFactions.FirstOrDefault();
            if (looterClan?.DefaultPartyTemplate == null)
            {
                return;
            }

            int size = refugeeParty.MemberRoster.TotalManCount;
            var position = refugeeParty.Position;
            Settlement nearestSettlement = SettlementHelper.FindNearestSettlementToPoint(refugeeParty.Position, s => s.IsTown || s.IsVillage)
                ?? Settlement.All.FirstOrDefault();

            DestroyPartyAction.Apply(null, refugeeParty);

            _spawnCounter++;
            MobileParty banditParty = BanditPartyComponent.CreateLooterParty(
                $"rf_refugee_looters_{_spawnCounter}", looterClan, nearestSettlement,
                isBossParty: false, looterClan.DefaultPartyTemplate, position);
            banditParty.InitializeMobilePartyAroundPosition(looterClan.DefaultPartyTemplate, position, 1f);

            CharacterObject looterTroop = looterClan.Culture?.BasicTroop;
            if (looterTroop != null)
            {
                int deficit = size - banditParty.MemberRoster.TotalManCount;
                if (deficit > 0)
                {
                    banditParty.MemberRoster.AddToCounts(looterTroop, deficit);
                }
            }

            banditParty.SetMovePatrolAroundPoint(position, MobileParty.NavigationType.Default);

            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=rf_refugee_banditry}A desperate mass of refugees has turned to banditry.").ToString(),
                Color.FromUint(0xFFC87A5Au)));
        }

        // ── Player interaction ───────────────────────────────────────────────

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Saves created before the leader fix may carry a leaderless clan;
            // heal it before any vanilla daily tick dereferences clan.Leader.
            if (_refugeeClan != null && !_refugeeClan.IsEliminated)
            {
                EnsureClanLeader(_refugeeClan, _refugeeClan.HomeSettlement);
            }

            starter.AddDialogLine(
                "rf_refugee_greeting", "start", "rf_refugee_hub",
                "{=rf_refugee_greet}Please, we want no trouble. Our village was put to the torch — we carry nothing but our children and the clothes on our backs. Take us with you, {?PLAYER.GENDER}my lady{?}my lord{\\?}: we will carry arms for you, for coin to feed our families and bread from your stores. {RF_REFUGEE_COST} denars for all {RF_REFUGEE_COUNT} of us.",
                IsRefugeeConversation, null);

            starter.AddPlayerLine(
                "rf_refugee_recruit", "rf_refugee_hub", "rf_refugee_join",
                "{=rf_refugee_offer}Very well. You march with me now — earn your bread.",
                CanAffordRefugees, null);

            starter.AddPlayerLine(
                "rf_refugee_no_gold", "rf_refugee_hub", "close_window",
                "{=rf_refugee_poor}I cannot afford so many mouths. Safe travels.",
                () => !CanAffordRefugees(), null);

            starter.AddPlayerLine(
                "rf_refugee_farewell", "rf_refugee_hub", "close_window",
                "{=rf_refugee_leave}I have no place for you. Safe travels.",
                CanAffordRefugees, null);

            starter.AddDialogLine(
                "rf_refugee_join_accept", "rf_refugee_join", "close_window",
                "{=rf_refugee_accept}We will not fail you, {?PLAYER.GENDER}my lady{?}my lord{\\?}. May we never see smoke on the horizon again.",
                null, RecruitRefugeesConsequence);
        }

        private static bool IsRefugeeConversation()
        {
            if (MobileParty.ConversationParty?.PartyComponent is not RefugeePartyComponent)
            {
                return false;
            }

            int count = MobileParty.ConversationParty.MemberRoster.TotalManCount;
            MBTextManager.SetTextVariable("RF_REFUGEE_COUNT", count);
            MBTextManager.SetTextVariable("RF_REFUGEE_COST", count * GoldCostPerRefugee);
            return true;
        }

        private static bool CanAffordRefugees()
        {
            MobileParty refugees = MobileParty.ConversationParty;
            if (refugees?.PartyComponent is not RefugeePartyComponent)
            {
                return false;
            }

            return Hero.MainHero.Gold >= refugees.MemberRoster.TotalManCount * GoldCostPerRefugee;
        }

        private void RecruitRefugeesConsequence()
        {
            MobileParty refugees = MobileParty.ConversationParty;
            if (refugees?.PartyComponent is not RefugeePartyComponent)
            {
                return;
            }

            int count = refugees.MemberRoster.TotalManCount;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, count * GoldCostPerRefugee, true);
            MobileParty.MainParty.MemberRoster.Add(refugees.MemberRoster);

            // An untrained mass folding into the ranks unsettles the army: the
            // party is slowed in proportion to the share of refugees in it
            // (see GetUntrainedSpeedFactor) until they are upgraded into real
            // troops, die or are dismissed; morale dips once and recovers
            // naturally.
            _untrainedRefugees += count;
            MobileParty.MainParty.RecentEventsMorale -= MathF.Min(MoralePenaltyCap, count * MoralePenaltyPerRefugee);

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }

            DestroyPartyAction.Apply(null, refugees);
        }
    }
}
