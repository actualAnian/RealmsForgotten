using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.TradePact
{
    public class TradePactCampaignBehavior : CampaignBehaviorBase
    {
        // Change to use value tuple (string, string) for consistency with SaveableTypeDefiner
        private HashSet<(string, string)> _activeTradePacts = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // The SyncData method should now correctly handle the HashSet<(string, string)>
            dataStore.SyncData("_activeTradePacts", ref _activeTradePacts);
        }

        public void AddTradePact(Kingdom k1, Kingdom k2)
        {
            // Create value tuples instead of System.Tuple
            var pact = (k1.StringId, k2.StringId);
            if (!_activeTradePacts.Contains(pact))
                _activeTradePacts.Add(pact);
        }

        public bool HasTradePact(Kingdom k1, Kingdom k2)
        {
            // Create value tuples for comparison
            var a = (k1.StringId, k2.StringId);
            var b = (k2.StringId, k1.StringId);
            return _activeTradePacts.Contains(a) || _activeTradePacts.Contains(b);
        }

        public void RemoveTradePact(Kingdom k1, Kingdom k2)
        {
            // Create value tuples for removal
            var pact1 = (k1.StringId, k2.StringId);
            var pact2 = (k2.StringId, k1.StringId);
            _activeTradePacts.Remove(pact1);
            _activeTradePacts.Remove(pact2);
        }

        private void OnDailyTick()
        {
            // Deconstruction works directly with value tuples
            foreach (var (idA, idB) in _activeTradePacts)
            {
                var k1 = Kingdom.All.FirstOrDefault(k => k.StringId == idA);
                var k2 = Kingdom.All.FirstOrDefault(k => k.StringId == idB);

                if (k1 is null || k2 is null)
                    continue;

                ApplyTradePactBenefits(k1, k2);
            }
        }

        private void ApplyTradePactBenefits(Kingdom k1, Kingdom k2)
        {
            foreach (var town in k1.Fiefs.Where(f => f.IsTown))
                town.Prosperity += 2f;
            foreach (var town in k2.Fiefs.Where(f => f.IsTown))
                k2.Fiefs.Where(f => f.IsTown).ToList().ForEach(town => town.Prosperity += 2f); // Added ToList() for clarity, though not strictly necessary here.
        }
    }
}