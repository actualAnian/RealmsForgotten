using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public class AlignmentMomentumBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)] private int _goodMomentum = 0;
        [SaveableField(1)] private int _evilMomentum = 0;
        [SaveableField(2)] private bool _warEnded = false;
        private readonly Dictionary<string, string> _neutralToSideMap = new()
        {
            { "wulf", "Evil" },
            { "empire_w", "Evil" },
            { "south_realm", "Good" },
            { "khuzait", "Evil" },
             { "empire", "Good" },
             { "vlandia", "Good" },
        };

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnBattleEnded);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementCaptured);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckWarIntegrity);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_goodMomentum", ref _goodMomentum);
            dataStore.SyncData("_evilMomentum", ref _evilMomentum);
            dataStore.SyncData("_warEnded", ref _warEnded);
        }

        private void CheckWarIntegrity()
        {
            if (!AlignmentWarBehavior.IsActive) return;

            foreach (var good in Kingdom.All.Where(k => k.Culture.IsGoodCulture()))
            {
                foreach (var evil in Kingdom.All.Where(k => k.Culture.IsEvilCulture()))
                {
                    if (!FactionManager.IsAtWarAgainstFaction(good, evil))
                    {
                        FactionManager.DeclareWar(good, evil);
                        InformationManager.DisplayMessage(new InformationMessage($"⛔ Peace invalidated: {good.Name} vs {evil.Name} war reinstated."));
                    }
                }
            }

            CheckNeutralAllies();
        }

        private void OnBattleEnded(MapEvent mapEvent)
        {
            if (_warEnded || mapEvent == null || mapEvent.BattleState == BattleState.None)
                return;

            var attacker = mapEvent.AttackerSide.LeaderParty?.MapFaction as Kingdom;
            var defender = mapEvent.DefenderSide.LeaderParty?.MapFaction as Kingdom;

            if (attacker == null || defender == null) return;

            if (mapEvent.BattleState == BattleState.AttackerVictory)
                ApplyMomentum(attacker, defender);
            else if (mapEvent.BattleState == BattleState.DefenderVictory)
                ApplyMomentum(defender, attacker);
        }

        private void OnSettlementCaptured(Settlement settlement, bool isPlayerInvolved, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (_warEnded || settlement == null || newOwner == null || oldOwner == null)
                return;

            var newFaction = newOwner.Clan?.Kingdom;
            var oldFaction = oldOwner.Clan?.Kingdom;

            if (newFaction == null || oldFaction == null) return;

            if (newFaction.IsGood() && oldFaction.IsEvil())
                _goodMomentum += 10;
            else if (newFaction.IsEvil() && oldFaction.IsGood())
                _evilMomentum += 10;

            InformationManager.DisplayMessage(new InformationMessage($"[Momentum] Good: {_goodMomentum} | Evil: {_evilMomentum}", Colors.Gray));
            CheckWarOutcome();
        }

        private void ApplyMomentum(IFaction winner, IFaction loser)
        {
            if (winner.IsGood() && loser.IsEvil())
                _goodMomentum += 5;
            else if (winner.IsEvil() && loser.IsGood())
                _evilMomentum += 5;

            InformationManager.DisplayMessage(new InformationMessage($"[Momentum] Good: {_goodMomentum} | Evil: {_evilMomentum}", Colors.Gray));
            CheckWarOutcome();
        }

        private void CheckWarOutcome()
        {
            CheckNeutralAllies();

            if (_goodMomentum >= 5000)
                EndWar("✨ The forces of good have triumphed over evil!");
            else if (_evilMomentum >= 5000)
                EndWar("☠️ Evil has overwhelmed the world!");
        }

        private void CheckNeutralAllies()
        {
            var alignmentWar = Campaign.Current.GetCampaignBehavior<AlignmentWarBehavior>();

            if (_goodMomentum >= 2000 && _goodMomentum > _evilMomentum)
            {
                foreach (var kv in _neutralToSideMap.Where(kvp => kvp.Value == "Good"))
                {
                    Kingdom neutral = Kingdom.All.FirstOrDefault(k => k.StringId == kv.Key);
                    if (neutral != null && !neutral.Culture.IsGoodCulture())
                    {
                        neutral.Culture.SetGoodCulture(true);
                        InformationManager.DisplayMessage(new InformationMessage($"✨ {neutral.Name} joins the good-aligned war effort.", Colors.Blue));

                        // Ensure the faction is added to the alignment war
                        alignmentWar?.AddKingdomToSide(neutral, isGood: true);
                    }
                }
            }
            else if (_evilMomentum >= 2000 && _evilMomentum > _goodMomentum)
            {
                foreach (var kv in _neutralToSideMap.Where(kvp => kvp.Value == "Evil"))
                {
                    Kingdom neutral = Kingdom.All.FirstOrDefault(k => k.StringId == kv.Key);
                    if (neutral != null && !neutral.Culture.IsEvilCulture())
                    {
                        neutral.Culture.SetEvilCulture(true);
                        InformationManager.DisplayMessage(new InformationMessage($"☠️ {neutral.Name} has joined the evil-aligned war effort.", Colors.Red));

                        // Ensure the faction is added to the alignment war
                        alignmentWar?.AddKingdomToSide(neutral, isGood: false);
                    }
                }
            }
        }


        private void EndWar(string message)
        {
            if (_warEnded) return;
            _warEnded = true;
            AlignmentWarBehavior.IsActive = false;

            var warBehavior = Campaign.Current.GetCampaignBehavior<AlignmentWarBehavior>();
            warBehavior.EndWar();

            var declarePeace = typeof(FactionManager).Assembly
                .GetType("TaleWorlds.CampaignSystem.Actions.DeclarePeaceAction")
                ?.GetMethod("Apply", new[] { typeof(IFaction), typeof(IFaction) });

            foreach (var good in warBehavior.GetGoodKingdoms())
            {
                foreach (var evil in warBehavior.GetEvilKingdoms())
                {
                    if (FactionManager.IsAtWarAgainstFaction(good, evil))
                        declarePeace?.Invoke(null, new object[] { good, evil });
                }
            }

            InformationManager.DisplayMessage(new InformationMessage(message, Color.FromUint(0xFFFFD700)));
        }
    }
}