using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public class AlignmentDiplomacyModel : DiplomacyModel
    {
        private readonly DiplomacyModel _baseModel;

        public AlignmentDiplomacyModel(DiplomacyModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan)
        {
            if (AreCulturesOpposed(kingdom.Culture, mercenaryClan.Culture))
                return float.MinValue;

            return _baseModel.GetScoreOfKingdomToHireMercenary(kingdom, mercenaryClan);
        }

        public override float GetScoreOfKingdomToGetClan(Kingdom kingdom, Clan clan)
        {
            if (AreCulturesOpposed(kingdom.Culture, clan.Culture))
                return float.MinValue;

            return _baseModel.GetScoreOfKingdomToGetClan(kingdom, clan);
        }

        public override float GetScoreOfClanToJoinKingdom(Clan clan, Kingdom kingdom)
        {
            if (AreCulturesOpposed(clan.Culture, kingdom.Culture))
                return float.MinValue;

            return _baseModel.GetScoreOfClanToJoinKingdom(clan, kingdom);
        }

       
        private bool AreCulturesOpposed(CultureObject c1, CultureObject c2)
        {
            return (c1.IsGoodCulture() && c2.IsEvilCulture()) || (c1.IsEvilCulture() && c2.IsGoodCulture());
        }

        // Delegate everything else
        public override int MaxRelationLimit => _baseModel.MaxRelationLimit;
        public override int MinRelationLimit => _baseModel.MinRelationLimit;
        public override int MaxNeutralRelationLimit => _baseModel.MaxNeutralRelationLimit;
        public override int MinNeutralRelationLimit => _baseModel.MinNeutralRelationLimit;
        public override int MinimumRelationWithConversationCharacterToJoinKingdom => _baseModel.MinimumRelationWithConversationCharacterToJoinKingdom;
        public override int GiftingTownRelationshipBonus => _baseModel.GiftingTownRelationshipBonus;
        public override int GiftingCastleRelationshipBonus => _baseModel.GiftingCastleRelationshipBonus;

        public override float WarDeclarationScorePenaltyAgainstAllies => _baseModel.WarDeclarationScorePenaltyAgainstAllies;

        public override float WarDeclarationScoreBonusAgainstEnemiesOfAllies => _baseModel.WarDeclarationScoreBonusAgainstEnemiesOfAllies;

        public override bool CanSettlementBeGifted(Settlement settlement) => _baseModel.CanSettlementBeGifted(settlement);
        public override float DenarsToInfluence() => _baseModel.DenarsToInfluence();
        public override IEnumerable<BarterGroup> GetBarterGroups() => _baseModel.GetBarterGroups();
        public override int GetBaseRelation(Hero h1, Hero h2) => _baseModel.GetBaseRelation(h1, h2);
        public override int GetCharmExperienceFromRelationGain(Hero hero, float val, ChangeRelationAction.ChangeRelationDetail detail) => _baseModel.GetCharmExperienceFromRelationGain(hero, val, detail);
        public override float GetClanStrength(Clan clan) => _baseModel.GetClanStrength(clan);
        public override int GetEffectiveRelation(Hero h1, Hero h2) => _baseModel.GetEffectiveRelation(h1, h2);
        public override float GetHeroCommandingStrengthForClan(Hero hero) => _baseModel.GetHeroCommandingStrengthForClan(hero);
        public override void GetHeroesForEffectiveRelation(Hero h1, Hero h2, out Hero e1, out Hero e2) => _baseModel.GetHeroesForEffectiveRelation(h1, h2, out e1, out e2);
        public override float GetHeroGoverningStrengthForClan(Hero hero) => _baseModel.GetHeroGoverningStrengthForClan(hero);
        public override float GetHourlyInfluenceAwardForBeingArmyMember(MobileParty p) => _baseModel.GetHourlyInfluenceAwardForBeingArmyMember(p);
        public override float GetHourlyInfluenceAwardForBesiegingEnemyFortification(MobileParty p) => _baseModel.GetHourlyInfluenceAwardForBesiegingEnemyFortification(p);
        public override float GetHourlyInfluenceAwardForRaidingEnemyVillage(MobileParty p) => _baseModel.GetHourlyInfluenceAwardForRaidingEnemyVillage(p);
        public override int GetInfluenceAwardForSettlementCapturer(Settlement s) => _baseModel.GetInfluenceAwardForSettlementCapturer(s);
        public override int GetInfluenceCostOfAbandoningArmy() => _baseModel.GetInfluenceCostOfAbandoningArmy();
        public override int GetInfluenceCostOfAnnexation(Clan c) => _baseModel.GetInfluenceCostOfAnnexation(c);
        public override int GetInfluenceCostOfChangingLeaderOfArmy() => _baseModel.GetInfluenceCostOfChangingLeaderOfArmy();
        public override int GetInfluenceCostOfDisbandingArmy() => _baseModel.GetInfluenceCostOfDisbandingArmy();
        public override int GetInfluenceCostOfExpellingClan(Clan c) => _baseModel.GetInfluenceCostOfExpellingClan(c);
        public override int GetInfluenceCostOfPolicyProposalAndDisavowal(Clan c) => _baseModel.GetInfluenceCostOfPolicyProposalAndDisavowal(c);
        public override int GetInfluenceCostOfProposingPeace(Clan c) => _baseModel.GetInfluenceCostOfProposingPeace(c);
        public override int GetInfluenceCostOfProposingWar(Clan c) => _baseModel.GetInfluenceCostOfProposingWar(c);
        public override int GetInfluenceCostOfSupportingClan() => _baseModel.GetInfluenceCostOfSupportingClan();
        public override int GetInfluenceValueOfSupportingClan() => _baseModel.GetInfluenceValueOfSupportingClan();
        public override uint GetNotificationColor(ChatNotificationType n) => _baseModel.GetNotificationColor(n);
        public override int GetRelationChangeAfterClanLeaderIsDead(Hero h1, Hero h2) => _baseModel.GetRelationChangeAfterClanLeaderIsDead(h1, h2);
        public override int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(Hero h, bool votedAgainst) => _baseModel.GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(h, votedAgainst);
        public override int GetRelationCostOfDisbandingArmy(bool isLeader) => _baseModel.GetRelationCostOfDisbandingArmy(isLeader);
        public override int GetRelationCostOfExpellingClanFromKingdom() => _baseModel.GetRelationCostOfExpellingClanFromKingdom();
        public override float GetRelationIncreaseFactor(Hero h1, Hero h2, float val) => _baseModel.GetRelationIncreaseFactor(h1, h2, val);
        public override int GetRelationValueOfSupportingClan() => _baseModel.GetRelationValueOfSupportingClan();
        public override float GetScoreOfDeclaringPeace(IFaction a, IFaction b)
        {
            // 🔒 Block peace as long as the global alignment war flag is active
            if (AlignmentWarBehavior.IsActive)
            {
                //reason = new TextObject("⚔️ Alignment war is active. Peace is forbidden.");
                return float.MinValue;
            }

            // (Optional) fallback culture logic if you want both checks
            if ((a.Culture?.IsGoodCulture() == true && b.Culture?.IsEvilCulture() == true) ||
                (a.Culture?.IsEvilCulture() == true && b.Culture?.IsGoodCulture() == true))
            {
                //reason = new TextObject("⚔️ Alignment war: Peace is forbidden between good and evil.");
                return float.MinValue;
            }

            return _baseModel.GetScoreOfDeclaringPeace(a, b);
        }
        public override float GetScoreOfClanToLeaveKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfClanToLeaveKingdom(c, k);
        public override float GetScoreOfKingdomToSackClan(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToSackClan(k, c);
        public override float GetScoreOfKingdomToSackMercenary(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToSackMercenary(k, c);
        public override float GetScoreOfLettingPartyGo(MobileParty p, MobileParty target) => _baseModel.GetScoreOfLettingPartyGo(p, target);
        public override float GetScoreOfMercenaryToJoinKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfMercenaryToJoinKingdom(c, k);
        public override float GetScoreOfMercenaryToLeaveKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfMercenaryToLeaveKingdom(c, k);
        public override float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom k) => _baseModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(k);
        public override float GetValueOfHeroForFaction(Hero h, IFaction f, bool marriage) => _baseModel.GetValueOfHeroForFaction(h, f, marriage);
        public override bool IsClanEligibleToBecomeRuler(Clan c) => _baseModel.IsClanEligibleToBecomeRuler(c);
        public override bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace) => _baseModel.IsPeaceSuitable(factionDeclaresPeace, factionDeclaredPeace);
        public override float GetValueOfSettlementsForFaction(IFaction faction) => _baseModel.GetValueOfSettlementsForFaction(faction);
        public override DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2) => _baseModel.GetShallowDiplomaticStance(faction1 , faction2);
        public override DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2) => _baseModel.GetDefaultDiplomaticStance(faction1, faction2);
        public override bool IsAtConstantWar(IFaction faction1, IFaction faction2) => _baseModel.IsAtConstantWar(faction1, faction2);
        public override float GetDecisionMakingThreshold(IFaction consideringFaction) => _baseModel.GetDecisionMakingThreshold(consideringFaction);

        public override float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
        {
            return _baseModel.GetScoreOfDeclaringPeaceForClan(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out reason, includeReason);
        }

        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
        {
            return _baseModel.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out reason, includeReason);
        }

        public override ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
        {
            return _baseModel.GetWarProgressScore(factionDeclaresWar, factionDeclaredWar);
        }

        public override int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays)
        {
            return _baseModel.GetDailyTributeToPay(factionToPay, factionToReceive, out tributeDurationInDays);
        }
    }
}