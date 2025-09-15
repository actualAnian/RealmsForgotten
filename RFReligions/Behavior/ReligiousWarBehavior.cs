using RealmsForgotten.RFReligions.Core;
using RealmsForgotten.RFReligions.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Behavior
{
    public class ReligiousWarBehavior : CampaignBehaviorBase
    {
        // --- Configuration ---
        private const float WeeklyWarDeclarationChance = 0.05f;
        private const int TensionThresholdForWar = 150;
        private const int TensionIncreasePerWeek = 3;
        private const int TensionDecayPerWeek = 4;
        private const int WarCooldownWeeks = 40;

        // Using your existing cooldown tracker
        private Dictionary<long, int> _cooldownTracker = new Dictionary<long, int>();
        // NEW: A dictionary to track the hidden 'Religious Tension' score.
        private Dictionary<long, int> _tensionTracker = new Dictionary<long, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("RF_ReligiousWarCooldowns", ref _cooldownTracker);
            // NEW: Saving the tension tracker.
            dataStore.SyncData("RF_ReligiousTensionTracker", ref _tensionTracker);
        }

        private void OnWeeklyTick()
        {
            if (ReligionBehavior.Instance == null)
                return;

            // Initialize dictionaries if they are null (for new games or loaded saves from before this system)
            if (_cooldownTracker == null) _cooldownTracker = new Dictionary<long, int>();
            if (_tensionTracker == null) _tensionTracker = new Dictionary<long, int>();

            var kingdoms = Campaign.Current.Kingdoms.Where(k => !k.IsEliminated).ToList();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    var kingdom1 = kingdoms[i];
                    var kingdom2 = kingdoms[j];

                    if (kingdom1.IsMinorFaction || kingdom2.IsMinorFaction || kingdom1.Leader == null || kingdom2.Leader == null)
                        continue;

                    long pairHash = GetKingdomPairHash(kingdom1, kingdom2);

                    // --- Cooldown Logic (from your working code) ---
                    if (_cooldownTracker.TryGetValue(pairHash, out int weeksLeft) && weeksLeft > 0)
                    {
                        _cooldownTracker[pairHash] = weeksLeft - 1;
                        // While on cooldown, tension also decays.
                        if (_tensionTracker.ContainsKey(pairHash))
                        {
                            _tensionTracker[pairHash] = Math.Max(0, _tensionTracker[pairHash] - TensionDecayPerWeek);
                        }
                        continue;
                    }

                    // --- NEW: Tension Logic ---
                    UpdateAndCheckTension(kingdom1, kingdom2, pairHash);
                }
            }
        }

        private void UpdateAndCheckTension(Kingdom kingdom1, Kingdom kingdom2, long pairHash)
        {
            if (!_tensionTracker.ContainsKey(pairHash))
            {
                _tensionTracker[pairHash] = 0;
            }

            // If factions are at war, tension should decay
            if (kingdom1.IsAtWarWith(kingdom2))
            {
                _tensionTracker[pairHash] = Math.Max(0, _tensionTracker[pairHash] - TensionDecayPerWeek);
                return;
            }

            // Check for religious intolerance
            if (ReligionBehavior.Instance._heroes.TryGetValue(kingdom1.Leader, out var religionModel1) &&
                ReligionBehavior.Instance._heroes.TryGetValue(kingdom2.Leader, out var religionModel2) &&
                AreReligionsIntolerant(religionModel1.Religion, religionModel2.Religion))
            {
                // Increase tension if intolerant and at peace
                _tensionTracker[pairHash] += TensionIncreasePerWeek;
            }
            else
            {
                // Decay tension if not intolerant
                _tensionTracker[pairHash] = Math.Max(0, _tensionTracker[pairHash] - TensionDecayPerWeek);
            }

            // Check if high tension triggers a war
            if (_tensionTracker[pairHash] >= TensionThresholdForWar && MBRandom.RandomFloat < WeeklyWarDeclarationChance)
            {
                DeclareWarAction.ApplyByKingdomDecision(kingdom1, kingdom2);

                var message = new TextObject("⚔️ Religious tension between {KINGDOM1} and {KINGDOM2} has erupted into a Holy War!", null);
                message.SetTextVariable("KINGDOM1", kingdom1.Name);
                message.SetTextVariable("KINGDOM2", kingdom2.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Red));

                // War declared: reset tension and set the cooldown
                _tensionTracker[pairHash] = 0;
                _cooldownTracker[pairHash] = WarCooldownWeeks;
            }
        }

        // REMOVED: The TryTriggerReligiousWar method is no longer needed. Its logic is now inside OnWeeklyTick.
        // The direct relationship penalty is gone.

        // Using your method signature to prevent namespace errors, but with restored logic for 'All'.
        private bool AreReligionsIntolerant(RFReligions.Core.RFReligions religion1, RFReligions.Core.RFReligions religion2)
        {
            if (religion1 == religion2)
                return false;

            bool religion1Tolerates2 = false;
            if (ReligionLogicHelper.TolerableReligions.TryGetValue(religion1, out var toleratedBy1))
            {
                if (toleratedBy1 == RFReligions.Core.RFReligions.All || toleratedBy1 == religion2)
                {
                    religion1Tolerates2 = true;
                }
            }

            bool religion2Tolerates1 = false;
            if (ReligionLogicHelper.TolerableReligions.TryGetValue(religion2, out var toleratedBy2))
            {
                if (toleratedBy2 == RFReligions.Core.RFReligions.All || toleratedBy2 == religion1)
                {
                    religion2Tolerates1 = true;
                }
            }

            return !(religion1Tolerates2 || religion2Tolerates1);
        }

        // Kept your working hash method
        private long GetKingdomPairHash(Kingdom k1, Kingdom k2)
        {
            int id1 = k1.StringId.GetHashCode();
            int id2 = k2.StringId.GetHashCode();
            return id1 < id2 ? ((long)id1 << 32) + id2 : ((long)id2 << 32) + id1;
        }
    }
}
