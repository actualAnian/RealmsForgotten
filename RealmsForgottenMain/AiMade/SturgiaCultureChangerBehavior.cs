using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade
{
    public class SturgiaCultureChangerBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, float> _ownershipDurations = new Dictionary<string, float>(); // Settlement IDs to durations
        private const float DaysToChangeCulture = 30f;
        private const string TargetCultureId = "sturgia"; // ID for Sturgia culture

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlementTick);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        private void OnDailySettlementTick(Settlement settlement)
        {
            if (settlement == null || settlement.OwnerClan == null)
                return;

            string settlementId = settlement.StringId;

            if (!_ownershipDurations.ContainsKey(settlementId))
            {
                _ownershipDurations[settlementId] = 0f;
            }

            // Increment the ownership duration
            _ownershipDurations[settlementId] += 1f;

            // Check if the settlement is eligible for culture change
            if (_ownershipDurations[settlementId] >= DaysToChangeCulture &&
                settlement.OwnerClan.Culture.StringId == TargetCultureId &&
                settlement.Culture.StringId != TargetCultureId)
            {
                UpdateSettlementCulture(settlement, settlement.OwnerClan.Culture);
                _ownershipDurations.Remove(settlementId);
            }
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            foreach (var settlement in Settlement.All)
            {
                if (_ownershipDurations.ContainsKey(settlement.StringId) &&
                    _ownershipDurations[settlement.StringId] >= DaysToChangeCulture &&
                    settlement.OwnerClan != null &&
                    settlement.Culture.StringId != TargetCultureId &&
                    settlement.OwnerClan.Culture.StringId == TargetCultureId)
                {
                    UpdateSettlementCulture(settlement, settlement.OwnerClan.Culture);
                    _ownershipDurations.Remove(settlement.StringId);
                }
            }
        }

        private void UpdateSettlementCulture(Settlement settlement, CultureObject newCulture)
        {
            if (settlement == null || newCulture == null) return;

            // Update settlement culture
            settlement.Culture = newCulture;

            // Update notables and their troops
            UpdateNotables(settlement, newCulture);

            // Update bound villages
            if (settlement.BoundVillages != null)
            {
                foreach (var village in settlement.BoundVillages)
                {
                    UpdateSettlementCulture(village.Settlement, newCulture);
                }
            }

            // Notify player
            InformationManager.DisplayMessage(new InformationMessage(
                $"The culture of {settlement.Name} has been changed to {newCulture.Name}."));
        }

        private void UpdateNotables(Settlement settlement, CultureObject newCulture)
        {
            foreach (var notable in settlement.Notables)
            {
                if (notable.CanHaveRecruits)
                {
                    // Update the notable's culture
                    notable.Culture = newCulture;

                    // Update the volunteer types array
                    if (notable.VolunteerTypes != null && notable.VolunteerTypes.Length > 0)
                    {
                        notable.VolunteerTypes[0] = newCulture.BasicTroop;
                        if (notable.VolunteerTypes.Length > 1)
                        {
                            notable.VolunteerTypes[1] = newCulture.EliteBasicTroop;
                        }
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_ownershipDurations", ref _ownershipDurations);

            // Ensure the dictionary is not null after deserialization
            if (_ownershipDurations == null)
            {
                _ownershipDurations = new Dictionary<string, float>();
            }
        }
    }
}