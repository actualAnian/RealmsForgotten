using RF_warsystem.Logic;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RF_warsystem.Models;

public sealed class RFWarSystemDiplomacyModel : DiplomacyModel
{
    private readonly DiplomacyModel _baseModel;

    public RFWarSystemDiplomacyModel(DiplomacyModel baseModel)
    {
        _baseModel = baseModel;
    }

    public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
    {
        float baseScore = _baseModel.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out reason, includeReason);
        if (baseScore <= -999999f)
        {
            return baseScore;
        }

        return RFWarStrategicHeuristics.AdjustWarScore(_baseModel, baseScore, factionDeclaresWar, factionDeclaredWar, evaluatingClan);
    }

    public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
    {
        float baseScore = _baseModel.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
        return RFWarStrategicHeuristics.AdjustPeaceScore(_baseModel, baseScore, factionDeclaresPeace, factionDeclaredPeace);
    }

    public override float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
    {
        float baseScore = _baseModel.GetScoreOfDeclaringPeaceForClan(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out reason, includeReason);
        return RFWarStrategicHeuristics.AdjustPeaceScore(_baseModel, baseScore, factionDeclaresPeace, factionDeclaredPeace);
    }

    public override int MaxRelationLimit => _baseModel.MaxRelationLimit;
    public override int MinRelationLimit => _baseModel.MinRelationLimit;
    public override int MaxNeutralRelationLimit => _baseModel.MaxNeutralRelationLimit;
    public override int MinNeutralRelationLimit => _baseModel.MinNeutralRelationLimit;
    public override int MinimumRelationWithConversationCharacterToJoinKingdom => _baseModel.MinimumRelationWithConversationCharacterToJoinKingdom;
    public override int GiftingTownRelationshipBonus => _baseModel.GiftingTownRelationshipBonus;
    public override int GiftingCastleRelationshipBonus => _baseModel.GiftingCastleRelationshipBonus;
    public override float WarDeclarationScorePenaltyAgainstTradePartners => _baseModel.WarDeclarationScorePenaltyAgainstTradePartners;

    public override bool CanSettlementBeGifted(Settlement settlement) => _baseModel.CanSettlementBeGifted(settlement);
    public override float DenarsToInfluence() => _baseModel.DenarsToInfluence();
    public override IEnumerable<BarterGroup> GetBarterGroups() => _baseModel.GetBarterGroups();
    public override int GetBaseRelation(Hero h1, Hero h2) => _baseModel.GetBaseRelation(h1, h2);
    public override int GetCharmExperienceFromRelationGain(Hero hero, float val, ChangeRelationAction.ChangeRelationDetail detail) => _baseModel.GetCharmExperienceFromRelationGain(hero, val, detail);
    public override float GetClanStrength(Clan clan) => _baseModel.GetClanStrength(clan);
    public override int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays) => _baseModel.GetDailyTributeToPay(factionToPay, factionToReceive, out tributeDurationInDays);
    // Null guards: vanilla's CalculatePartyInfluenceCost passes a null
    // party.LeaderHero straight into GetRelation when an army contains a
    // momentarily leaderless party (leader died/captured before the army
    // reacts), and DefaultDiplomacyModel dereferences hero.Clan without
    // checking. 0 = "no relation", the same value vanilla returns when an
    // effective hero cannot be resolved.
    public override int GetEffectiveRelation(Hero h1, Hero h2)
        => h1 == null || h2 == null ? 0 : _baseModel.GetEffectiveRelation(h1, h2);
    public override float GetHeroCommandingStrengthForClan(Hero hero) => _baseModel.GetHeroCommandingStrengthForClan(hero);
    public override void GetHeroesForEffectiveRelation(Hero h1, Hero h2, out Hero e1, out Hero e2)
    {
        if (h1 == null || h2 == null)
        {
            e1 = h1;
            e2 = h2;
            return;
        }

        _baseModel.GetHeroesForEffectiveRelation(h1, h2, out e1, out e2);
    }
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
    public override float GetScoreOfClanToJoinKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfClanToJoinKingdom(c, k);
    public override float GetScoreOfClanToLeaveKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfClanToLeaveKingdom(c, k);
    public override float GetScoreOfKingdomToGetClan(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToGetClan(k, c);
    public override float GetScoreOfKingdomToHireMercenary(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToHireMercenary(k, c);
    public override float GetScoreOfKingdomToSackClan(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToSackClan(k, c);
    public override float GetScoreOfKingdomToSackMercenary(Kingdom k, Clan c) => _baseModel.GetScoreOfKingdomToSackMercenary(k, c);
    public override float GetScoreOfLettingPartyGo(MobileParty p, MobileParty target) => _baseModel.GetScoreOfLettingPartyGo(p, target);
    public override float GetScoreOfMercenaryToJoinKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfMercenaryToJoinKingdom(c, k);
    public override float GetScoreOfMercenaryToLeaveKingdom(Clan c, Kingdom k) => _baseModel.GetScoreOfMercenaryToLeaveKingdom(c, k);
    public override float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom k) => _baseModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(k);
    public override float GetValueOfHeroForFaction(Hero h, IFaction f, bool marriage) => _baseModel.GetValueOfHeroForFaction(h, f, marriage);
    public override float GetValueOfSettlementsForFaction(IFaction faction) => _baseModel.GetValueOfSettlementsForFaction(faction);
    public override ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, bool includeDescriptions = false) => _baseModel.GetWarProgressScore(factionDeclaresWar, factionDeclaredWar, includeDescriptions);
    public override DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2) => _baseModel.GetShallowDiplomaticStance(faction1, faction2);
    public override DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2) => _baseModel.GetDefaultDiplomaticStance(faction1, faction2);
    public override bool IsAtConstantWar(IFaction faction1, IFaction faction2) => _baseModel.IsAtConstantWar(faction1, faction2);
    public override bool IsClanEligibleToBecomeRuler(Clan c) => _baseModel.IsClanEligibleToBecomeRuler(c);
    public override bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace) => _baseModel.IsPeaceSuitable(factionDeclaresPeace, factionDeclaredPeace);
    public override float GetDecisionMakingThreshold(IFaction consideringFaction) => _baseModel.GetDecisionMakingThreshold(consideringFaction);
}
