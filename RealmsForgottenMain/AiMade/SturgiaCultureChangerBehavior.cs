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
using TaleWorlds.CampaignSystem.Roster;

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
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            // Ownership changed hands — the counter must measure continuous
            // Sturgian POSSESSION, not the settlement's age. Reset it.
            if (settlement != null)
            {
                _ownershipDurations.Remove(settlement.StringId);
            }
        }

        private void OnDailySettlementTick(Settlement settlement)
        {
            if (settlement == null || settlement.OwnerClan == null)
                return;

            string settlementId = settlement.StringId;

            // Only accrue days while the owner is actually Sturgian — a
            // non-Sturgian holder makes no progress toward conversion (and its
            // counter is cleared so it starts fresh if a Sturgian later takes it).
            if (settlement.OwnerClan.Culture?.StringId != TargetCultureId)
            {
                _ownershipDurations.Remove(settlementId);
                return;
            }

            if (!_ownershipDurations.ContainsKey(settlementId))
            {
                _ownershipDurations[settlementId] = 0f;
            }

            _ownershipDurations[settlementId] += 1f;

            if (_ownershipDurations[settlementId] >= DaysToChangeCulture &&
                settlement.Culture?.StringId != TargetCultureId)
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

            // Update settlement troop spawning (garrison & militia)
            UpdateSettlementTroops(settlement, newCulture);

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
                $"The culture of {settlement.Name} has been changed to {newCulture.Name}. Troops and notables now recruit from {newCulture.Name} culture."));
        }

        private void UpdateNotables(Settlement settlement, CultureObject newCulture)
        {
            foreach (var notable in settlement.Notables)
            {
                if (notable.CanHaveRecruits)
                {
                    // Update notable's culture
                    notable.Culture = newCulture;

                    // Update all volunteer troop slots with variety
                    UpdateNotableVolunteerTroops(notable, newCulture);
                }
            }
        }

        private void UpdateNotableVolunteerTroops(Hero notable, CultureObject newCulture)
        {
            if (notable.VolunteerTypes == null)
            {
                notable.VolunteerTypes = new CharacterObject[6];
            }

            // Determine troop types based on notable occupation
            switch (notable.CharacterObject.Occupation)
            {
                case Occupation.Headman:
                case Occupation.RuralNotable:
                    // Headmen provide basic infantry/militia
                    PopulateInfantryTroops(notable, newCulture);
                    break;

                case Occupation.Merchant:
                case Occupation.Artisan:
                    // Merchants provide more varied troops
                    PopulateMerchantTroops(notable, newCulture);
                    break;

                case Occupation.GangLeader:
                case Occupation.Preacher:
                    // Gang leaders/Preachers provide specialized troops
                    PopulateSpecializedTroops(notable, newCulture);
                    break;

                default:
                    // Default: basic troops
                    notable.VolunteerTypes[0] = newCulture.BasicTroop;
                    if (notable.VolunteerTypes.Length > 1)
                    {
                        notable.VolunteerTypes[1] = newCulture.EliteBasicTroop;
                    }
                    break;
            }
        }

        private void PopulateInfantryTroops(Hero notable, CultureObject culture)
        {
            // Headmen primarily offer infantry progression
            var cultureTroops = GetCultureTroopTree(culture, false, false); // Not mounted, not ranged

            for (int i = 0; i < Math.Min(notable.VolunteerTypes.Length, cultureTroops.Count); i++)
            {
                notable.VolunteerTypes[i] = cultureTroops[i];
            }
        }

        private void PopulateMerchantTroops(Hero notable, CultureObject culture)
        {
            // Merchants offer a mix: infantry + some ranged/mounted
            var infantry = GetCultureTroopTree(culture, false, false);
            var ranged = GetCultureTroopTree(culture, false, true);
            var mounted = GetCultureTroopTree(culture, true, false);

            int slot = 0;

            // Add 2-3 infantry
            for (int i = 0; i < Math.Min(2, infantry.Count) && slot < notable.VolunteerTypes.Length; i++, slot++)
            {
                notable.VolunteerTypes[slot] = infantry[i];
            }

            // Add 1-2 ranged
            for (int i = 0; i < Math.Min(2, ranged.Count) && slot < notable.VolunteerTypes.Length; i++, slot++)
            {
                notable.VolunteerTypes[slot] = ranged[i];
            }

            // Add 1 mounted if available
            if (mounted.Any() && slot < notable.VolunteerTypes.Length)
            {
                notable.VolunteerTypes[slot] = mounted[0];
            }
        }

        private void PopulateSpecializedTroops(Hero notable, CultureObject culture)
        {
            // Gang leaders/Preachers offer higher tier troops
            var allTroops = GetCultureTroopTree(culture, false, false);

            // Start from tier 2+ troops
            var higherTierTroops = allTroops.Where(t => t.Tier >= 2).ToList();

            if (higherTierTroops.Any())
            {
                for (int i = 0; i < Math.Min(notable.VolunteerTypes.Length, higherTierTroops.Count); i++)
                {
                    notable.VolunteerTypes[i] = higherTierTroops[i];
                }
            }
            else
            {
                // Fallback to regular troops
                PopulateInfantryTroops(notable, culture);
            }
        }

        private List<CharacterObject> GetCultureTroopTree(CultureObject culture, bool mountedOnly, bool rangedOnly)
        {
            var troops = new List<CharacterObject>();

            // Get all culture troops sorted by tier
            var cultureTroops = CharacterObject.All
                .Where(t => t.Culture == culture &&
                            !t.IsHero &&
                            t.Occupation == Occupation.Soldier)
                .OrderBy(t => t.Tier)
                .ToList();

            // Filter by type
            if (mountedOnly)
                cultureTroops = cultureTroops.Where(t => t.IsMounted).ToList();
            else if (rangedOnly)
                cultureTroops = cultureTroops.Where(t => t.IsRanged && !t.IsMounted).ToList();
            else
                cultureTroops = cultureTroops.Where(t => !t.IsMounted && !t.IsRanged).ToList();

            // Build progression tree (T1 → T2 → T3, etc.)
            for (int tier = 1; tier <= 6; tier++)
            {
                var troopOfTier = cultureTroops.FirstOrDefault(t => t.Tier == tier);
                if (troopOfTier != null)
                    troops.Add(troopOfTier);

                if (troops.Count >= 6) break; // Max 6 slots
            }

            // Fallback to basic troop if none found
            if (!troops.Any())
                troops.Add(culture.BasicTroop);

            return troops;
        }

        private void UpdateSettlementTroops(Settlement settlement, CultureObject newCulture)
        {
            if (settlement.Town != null)
            {
                // Update garrison troops
                if (settlement.Town.GarrisonParty != null)
                {
                    ConvertGarrisonTroops(settlement.Town.GarrisonParty.MemberRoster, newCulture);
                }

                // Update militia - access through Party property
                if (settlement.MilitiaPartyComponent?.Party != null)
                {
                    ConvertGarrisonTroops(settlement.MilitiaPartyComponent.Party.MemberRoster, newCulture);
                }
            }
            else if (settlement.IsVillage && settlement.Village != null)
            {
                // Update village militia - access through Party property
                if (settlement.MilitiaPartyComponent?.Party != null)
                {
                    ConvertGarrisonTroops(settlement.MilitiaPartyComponent.Party.MemberRoster, newCulture);
                }
            }
        }

        private void ConvertGarrisonTroops(TroopRoster roster, CultureObject newCulture)
        {
            if (roster == null || newCulture == null) return;

            // Create a list of troops to convert
            List<(CharacterObject oldTroop, int count, int wounded)> troopsToConvert = new List<(CharacterObject, int, int)>();

            for (int i = 0; i < roster.Count; i++)
            {
                var element = roster.GetElementCopyAtIndex(i);
                var troop = element.Character;

                // Skip heroes and troops already of target culture
                if (troop.IsHero || troop.Culture == newCulture)
                    continue;

                troopsToConvert.Add((troop, element.Number, element.WoundedNumber));
            }

            // Convert each troop to Sturgia equivalent
            foreach (var (oldTroop, count, wounded) in troopsToConvert)
            {
                CharacterObject newTroop = FindCultureEquivalent(oldTroop, newCulture);

                if (newTroop != null)
                {
                    // Remove old troops
                    roster.AddToCounts(oldTroop, -count, false, -wounded, 0, true, -1);

                    // Add new culture troops
                    roster.AddToCounts(newTroop, count, false, wounded, 0, true, -1);
                }
            }
        }

        private CharacterObject FindCultureEquivalent(CharacterObject oldTroop, CultureObject newCulture)
        {
            // Try to match by tier and role
            int tier = oldTroop.Tier;
            bool isMounted = oldTroop.IsMounted;
            bool isRanged = oldTroop.IsRanged;

            // Get all basic troops of the new culture
            var candidateTroops = CharacterObject.All
                .Where(t => t.Culture == newCulture &&
                            !t.IsHero &&
                            t.Tier == tier &&
                            t.Occupation == Occupation.Soldier)
                .ToList();

            if (!candidateTroops.Any())
            {
                // Fallback to basic troop if no exact match
                return newCulture.BasicTroop;
            }

            // Try to match mounted/ranged preference
            var exactMatch = candidateTroops.FirstOrDefault(t =>
                t.IsMounted == isMounted &&
                t.IsRanged == isRanged);

            if (exactMatch != null)
                return exactMatch;

            // If no exact match, try to at least match mounted status
            var mountedMatch = candidateTroops.FirstOrDefault(t => t.IsMounted == isMounted);
            if (mountedMatch != null)
                return mountedMatch;

            // Otherwise return first candidate of same tier
            return candidateTroops.First();
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