using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RF_warsystem.Models;

public sealed class RFWarSystemTargetScoreCalculatingModel : TargetScoreCalculatingModel
{
    private readonly TargetScoreCalculatingModel _baseModel;

    public RFWarSystemTargetScoreCalculatingModel(TargetScoreCalculatingModel baseModel)
    {
        _baseModel = baseModel;
    }

    public override float TravelingToAssignmentFactor => _baseModel.TravelingToAssignmentFactor;
    public override float BesiegingFactor => _baseModel.BesiegingFactor;
    public override float AssaultingTownFactor => _baseModel.AssaultingTownFactor;
    public override float RaidingFactor => _baseModel.RaidingFactor;
    public override float DefendingFactor => _baseModel.DefendingFactor;

    public override float GetDefensivePatrollingFactor(bool isNavalPatrolling)
    {
        return _baseModel.GetDefensivePatrollingFactor(isNavalPatrolling);
    }

    public override float GetOffensivePatrollingFactor(bool isNavalPatrolling)
    {
        return _baseModel.GetOffensivePatrollingFactor(isNavalPatrolling);
    }

    public override float CalculateDefensivePatrollingScoreForSettlement(Settlement settlement, bool isFromPort, MobileParty mobileParty)
    {
        return _baseModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isFromPort, mobileParty);
    }

    public override float CalculateOffensivePatrollingScoreForSettlement(Settlement settlement, bool isFromPort, MobileParty mobileParty)
    {
        return _baseModel.CalculateOffensivePatrollingScoreForSettlement(settlement, isFromPort, mobileParty);
    }

    public override float CurrentObjectiveValue(MobileParty mobileParty)
    {
        return _baseModel.CurrentObjectiveValue(mobileParty);
    }

    public override float GetTargetScoreForFaction(Settlement targetSettlement, Army.ArmyTypes missionType, MobileParty mobileParty, float ourStrength)
    {
        float baseScore = _baseModel.GetTargetScoreForFaction(targetSettlement, missionType, mobileParty, ourStrength);
        float adjustedScore = RFWarStrategicHeuristics.AdjustTargetScore(baseScore, targetSettlement, missionType, mobileParty);
        RFWarSystemTargetTraceCollector.Observe(mobileParty, missionType, targetSettlement, baseScore, adjustedScore);
        return adjustedScore;
    }
}
