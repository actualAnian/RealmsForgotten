using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using DeclareWarDetail          = TaleWorlds.CampaignSystem.Actions.DeclareWarAction.DeclareWarDetail;
using MakePeaceDetail           = TaleWorlds.CampaignSystem.Actions.MakePeaceAction.MakePeaceDetail;
using KillCharacterActionDetail = TaleWorlds.CampaignSystem.Actions.KillCharacterAction.KillCharacterActionDetail;

namespace RF_AIDialog
{
    /// <summary>
    /// Tracks the player's reputation across three axes that every NPC in Aeurth
    /// can plausibly know about through rumour, court gossip, and battlefield report:
    ///
    ///   HonorScore      (0-100) — keeps word, respects laws of war
    ///   MercyScore      (0-100) — treats enemies and prisoners with mercy vs cruelty
    ///   AggressionScore (0-100) — raids, wars, relentless expansion
    ///
    /// All scores start at 50 (neutral). They accumulate across the campaign.
    /// Injected into WorldContext so every NPC prompt receives a brief character
    /// summary of the player's known deeds.
    /// </summary>
    public class PlayerReputationStore : CampaignBehaviorBase
    {
        // ── Singleton ─────────────────────────────────────────────────────
        public static PlayerReputationStore? Instance { get; private set; }

        // ── Persisted scores ──────────────────────────────────────────────
        private float _honorScore      = 50f;
        private float _mercyScore      = 50f;
        private float _aggressionScore = 50f;

        public float HonorScore      => _honorScore;
        public float MercyScore      => _mercyScore;
        public float AggressionScore => _aggressionScore;

        // ── Lifecycle ─────────────────────────────────────────────────────

        public override void RegisterEvents()
        {
            Instance = this;

            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(
                this, OnHeroKilled);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(
                this, OnHeroPrisonerReleased);
            CampaignEvents.WarDeclared.AddNonSerializedListener(
                this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(
                this, OnMakePeace);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(
                this, OnRaidCompleted);
        }

        public override void SyncData(IDataStore dataStore)
        {
            Instance = this;
            dataStore.SyncData("PlayerRep_Honor",      ref _honorScore);
            dataStore.SyncData("PlayerRep_Mercy",      ref _mercyScore);
            dataStore.SyncData("PlayerRep_Aggression", ref _aggressionScore);
            if (!dataStore.IsLoading)
                WriteExternalSnapshot();
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterActionDetail detail,
            bool showNotification)
        {
            try
            {
                if (killer != Hero.MainHero) return;
                if (detail != KillCharacterActionDetail.Executed
                    && detail != KillCharacterActionDetail.ExecutionAfterMapEvent) return;
                if (!victim.IsLord && !victim.IsNotable) return;

                // Executing a lord or notable: dishonoring and cruel
                _honorScore      = Math.Max(0f, _honorScore      - 10f);
                _mercyScore      = Math.Max(0f, _mercyScore      -  8f);
                _aggressionScore = Math.Min(100f, _aggressionScore + 3f);
                WriteExternalSnapshot();
            }
            catch { }
        }

        private void OnHeroPrisonerReleased(
            Hero hero,
            PartyBase party,
            IFaction capturerFaction,
            EndCaptivityDetail detail,
            bool showNotification)
        {
            try
            {
                // Only count prisoners released from the player's own party by choice
                if (party?.MobileParty != MobileParty.MainParty) return;
                if (detail != EndCaptivityDetail.ReleasedByChoice) return;
                if (!hero.IsLord && !hero.IsNotable) return;

                // Generous release: builds mercy and honour
                _mercyScore = Math.Min(100f, _mercyScore + 4f);
                _honorScore = Math.Min(100f, _honorScore + 2f);
                WriteExternalSnapshot();
            }
            catch { }
        }

        private void OnWarDeclared(
            IFaction faction1,
            IFaction faction2,
            DeclareWarDetail detail)
        {
            try
            {
                var playerFaction = Hero.MainHero?.MapFaction;
                if (playerFaction == null) return;
                if (faction1 != playerFaction && faction2 != playerFaction) return;

                // Player's kingdom goes to war — raises aggression perception
                _aggressionScore = Math.Min(100f, _aggressionScore + 5f);
                WriteExternalSnapshot();
            }
            catch { }
        }

        private void OnMakePeace(
            IFaction faction1,
            IFaction faction2,
            MakePeaceDetail detail)
        {
            try
            {
                var playerFaction = Hero.MainHero?.MapFaction;
                if (playerFaction == null) return;
                if (faction1 != playerFaction && faction2 != playerFaction) return;

                // Choosing peace: honors the player's restraint
                _honorScore      = Math.Min(100f, _honorScore      + 3f);
                _aggressionScore = Math.Max(0f,   _aggressionScore - 3f);
                WriteExternalSnapshot();
            }
            catch { }
        }

        private void OnRaidCompleted(
            BattleSideEnum winnerSide,
            RaidEventComponent raidEventComponent)
        {
            try
            {
                if (winnerSide != BattleSideEnum.Attacker) return;
                if (raidEventComponent?.AttackerSide?.LeaderParty != PartyBase.MainParty) return;

                // Player raided a village and won — clear act of aggression
                _aggressionScore = Math.Min(100f, _aggressionScore + 6f);
                _mercyScore      = Math.Max(0f,   _mercyScore      - 2f);
                WriteExternalSnapshot();
            }
            catch { }
        }

        private void WriteExternalSnapshot()
        {
            try
            {
                AIMemoryStore.WritePlayerReputation(
                    _honorScore,
                    _mercyScore,
                    _aggressionScore,
                    BuildSummary(),
                    CurrentDay());
            }
            catch { }
        }

        private string BuildSummary()
        {
            string honor = _honorScore >= 65f ? "honorable" : _honorScore <= 35f ? "dishonorable" : "mixed in honor";
            string mercy = _mercyScore >= 65f ? "merciful" : _mercyScore <= 35f ? "cruel" : "pragmatic about mercy";
            string aggression = _aggressionScore >= 65f ? "aggressive" : _aggressionScore <= 35f ? "restrained" : "moderate in aggression";
            return $"The player is seen as {honor}, {mercy}, and {aggression}.";
        }

        private static int CurrentDay()
        {
            try
            {
                return (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow;
            }
            catch
            {
                return 0;
            }
        }
    }
}
