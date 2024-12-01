using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Helpers;

namespace RealmsForgotten.AiMade.Models
{
    public class ADODSpecialSettlementTroopsModel : VolunteerModel
    {
        private readonly Dictionary<string, List<string>> _settlementSpecificTroops = new();

        private const int RecruitMaxUpgradeTier = 3;

        public ADODSpecialSettlementTroopsModel()
        {
            InitializeSettlementSpecificTroops();
        }

        private void InitializeSettlementSpecificTroops()
        {
            // Define settlement and troop IDs without helpers
            AddSettlementTroop("town_FirstTree", "elvean_druid_militia");
           
        }

        private void AddSettlementTroop(string settlementId, params string[] troopIds)
        {
            // Ensure each settlement ID and troop ID exists
            if (Settlement.Find(settlementId) == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Settlement with ID '{settlementId}' does not exist."));
                return;
            }

            var validTroopIds = troopIds.Where(troopId => CharacterObject.Find(troopId) != null).ToList();
            if (!validTroopIds.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage($"No valid troops found for settlement '{settlementId}'."));
                return;
            }

            _settlementSpecificTroops[settlementId] = validTroopIds;
        }

        public override CharacterObject GetBasicVolunteer(Hero notable)
        {
            // Retrieve specific troops for the notable's settlement, if available
            if (notable.CurrentSettlement != null
                && _settlementSpecificTroops.TryGetValue(notable.CurrentSettlement.StringId, out var specificTroops))
            {
                return GetSpecificTroop(specificTroops) ?? notable.Culture?.BasicTroop;
            }

            // Default to the culture's basic troop or a generic "recruit" if no culture is found
            return notable.Culture?.BasicTroop ?? CharacterObject.All.FirstOrDefault(troop => troop.StringId == "recruit");
        }

        private CharacterObject GetSpecificTroop(List<string> specificTroops)
        {
            string troopId = specificTroops[MBRandom.RandomInt(specificTroops.Count)];
            return CharacterObject.Find(troopId);
        }

        public override float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            float baseProbability = CalculateBaseProbability(hero);
            return AdjustProbabilityBySettlement(baseProbability, hero, index, settlement);
        }

        private float CalculateBaseProbability(Hero hero)
        {
            int fiefCount = hero.CurrentSettlement.MapFaction.Fiefs.Sum(f => f.IsTown ? 3 : 1);
            return MathF.Clamp((float)(fiefCount / 46.0 * (fiefCount / 46.0)), 0.0f, 1f);
        }

        private float AdjustProbabilityBySettlement(float baseProbability, Hero hero, int index, Settlement settlement)
        {
            float probability = 0.95f * MathF.Pow(baseProbability, index + 1);
            var explainedNumber = new ExplainedNumber(probability, false);

            if (hero.Clan?.Kingdom != null && hero.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
                explainedNumber.AddFactor(0.2f);

            AddRidingPerkBonus(hero, settlement, index, explainedNumber);

            return explainedNumber.ResultNumber;
        }

        private void AddRidingPerkBonus(Hero hero, Settlement settlement, int index, ExplainedNumber explainedNumber)
        {
            Town town = settlement.Town ?? settlement.Village?.TradeBound?.Town;
            if (town == null) return;

            if (hero.VolunteerTypes[index]?.IsMounted == true
                && PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.CavalryTactics, town))
            {
                explainedNumber.AddFactor(DefaultPerks.Riding.CavalryTactics.PrimaryBonus * 0.01f);
            }
        }

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int relationScore = CalculateRelationScore(buyerHero, sellerHero, useValueAsRelation);
            int perkBonus = CalculatePerkBonus(buyerHero, sellerHero);

            return (int)MathF.Clamp(1 + relationScore + perkBonus, 0, 6);
        }

        private int CalculateRelationScore(Hero buyerHero, Hero sellerHero, int useValueAsRelation)
        {
            int relation = useValueAsRelation < -100 ? buyerHero.GetRelation(sellerHero) : useValueAsRelation;
            return relation switch
            {
                >= 100 => 7,
                >= 80 => 6,
                >= 60 => 5,
                >= 40 => 4,
                >= 20 => 3,
                >= 10 => 2,
                >= 5 => 1,
                >= 0 => 0,
                _ => -1
            };
        }

        private int CalculatePerkBonus(Hero buyerHero, Hero sellerHero)
        {
            int perkBonus = 0;

            if (sellerHero.IsGangLeader && buyerHero.MapFaction == sellerHero.MapFaction)
            {
                perkBonus += GetGovernorPerkBonus(sellerHero.CurrentSettlement);
            }
            perkBonus += GetOtherPerkBonuses(buyerHero, sellerHero);

            return perkBonus;
        }

        private int GetGovernorPerkBonus(Settlement settlement)
        {
            if (settlement == null) return 0;

            Hero governor = settlement.Town?.Governor ?? settlement.Village?.Bound?.Town?.Governor;
            return governor?.GetPerkValue(DefaultPerks.Roguery.OneOfTheFamily) == true
                ? (int)DefaultPerks.Roguery.OneOfTheFamily.SecondaryBonus
                : 0;
        }

        private int GetOtherPerkBonuses(Hero buyerHero, Hero sellerHero)
        {
            int bonus = 0;
            if (sellerHero.IsMerchant && buyerHero.GetPerkValue(DefaultPerks.Trade.ArtisanCommunity))
                bonus += (int)DefaultPerks.Trade.ArtisanCommunity.SecondaryBonus;
            if (sellerHero.Culture == buyerHero.Culture && buyerHero.GetPerkValue(DefaultPerks.Leadership.CombatTips))
                bonus += (int)DefaultPerks.Leadership.CombatTips.SecondaryBonus;
            if (sellerHero.IsRuralNotable && buyerHero.GetPerkValue(DefaultPerks.Charm.Firebrand))
                bonus += (int)DefaultPerks.Charm.Firebrand.SecondaryBonus;
            if (sellerHero.IsUrbanNotable && buyerHero.GetPerkValue(DefaultPerks.Charm.FlexibleEthics))
                bonus += (int)DefaultPerks.Charm.FlexibleEthics.SecondaryBonus;
            if (sellerHero.IsArtisan && buyerHero.GetPerkValue(DefaultPerks.Engineering.EngineeringGuilds))
                bonus += (int)DefaultPerks.Engineering.EngineeringGuilds.PrimaryBonus;

            return bonus;
        }

        public override bool CanHaveRecruits(Hero hero) => hero.IsNotable;

        // Defines the max recruit tier, aligned with the original code's constant
        public override int MaxVolunteerTier => RecruitMaxUpgradeTier;
    }
}
