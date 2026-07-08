using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

/// <summary>
/// Melee-cavalry arm of the mounted onslaught: charge in WAVES instead of
/// grinding in a ball. Cycle: gather at a staging point on the OPEN side of
/// the target (offset to this formation's flank so two cavalry groups stage
/// on different lanes) → committed charge through the enemy → return to
/// staging and re-form → repeat. The BattleAICavalryStabilizer still rescues
/// any charge that bogs down mid-cycle; this executor supplies the geometry
/// that keeps the cycle out of the corner the enemy is pinned against.
/// </summary>
public sealed class BehaviorCavalryWaveCharge : BehaviorComponent
{
    private bool _charging;
    private float _nextPhaseTime;

    /// <summary>Formation to break (set by the tactic each tick).</summary>
    public Formation? TargetEnemyFormation { get; set; }

    /// <summary>Unit vector from the target toward open ground.</summary>
    public Vec2 OpenSideDirection { get; set; }

    /// <summary>+1 stages right of the open bearing, -1 left — two cavalry
    /// wings attack along different lanes.</summary>
    public float LaneSign { get; set; } = 1f;

    public float StagingDistance { get; set; } = 100f;

    public float ChargeDuration { get; set; } = 9f;

    public float RegroupMinimumDuration { get; set; } = 3f;

    public float StagingArrivalDistance { get; set; } = 25f;

    public BehaviorCavalryWaveCharge(Formation formation)
        : base(formation)
    {
    }

    protected override float GetAiWeight()
    {
        if (base.Formation.CountOfUnits <= 0)
        {
            return 0f;
        }

        return 1f;
    }

    public override TextObject GetBehaviorString()
    {
        return new TextObject("{=!}Cavalry wave charge");
    }

    protected override void CalculateCurrentOrder()
    {
        Mission? mission = Mission.Current;
        Formation? enemy = TargetEnemyFormation != null && TargetEnemyFormation.CountOfUnits > 0
            ? TargetEnemyFormation
            : base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation?.Formation
                ?? base.Formation.CachedClosestEnemyFormation?.Formation;
        if (enemy == null || mission == null)
        {
            base.CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        float now = mission.CurrentTime;
        Vec2 targetPosition = enemy.CachedMedianPosition.AsVec2;
        Vec2 openDirection = OpenSideDirection.IsValid && OpenSideDirection.LengthSquared > 0.01f
            ? OpenSideDirection.Normalized()
            : (base.Formation.CachedMedianPosition.AsVec2 - targetPosition).Normalized();
        if (!targetPosition.IsValid || !openDirection.IsValid)
        {
            base.CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        if (_charging)
        {
            if (now >= _nextPhaseTime)
            {
                // Wave spent: break off and re-form before the next one.
                _charging = false;
                _nextPhaseTime = now + RegroupMinimumDuration;
            }
            else
            {
                base.CurrentOrder = MovementOrder.MovementOrderChargeToTarget(enemy);
                return;
            }
        }

        // Staging lane: offset from the open bearing toward this wing's side,
        // rotated back inside the map if the corner pushes it out.
        Vec2 stagingPoint = targetPosition
            + Vec2.FromRotation(openDirection.RotationInRadians + LaneSign * 0.7f) * StagingDistance;
        for (int attempt = 1; attempt <= 5 && !mission.IsPositionInsideBoundaries(stagingPoint); attempt++)
        {
            stagingPoint = targetPosition
                + Vec2.FromRotation(openDirection.RotationInRadians + LaneSign * (0.7f - attempt * 0.3f)) * StagingDistance;
        }

        Vec2 myPosition = base.Formation.CachedMedianPosition.AsVec2;
        if (myPosition.IsValid
            && myPosition.Distance(stagingPoint) <= StagingArrivalDistance
            && now >= _nextPhaseTime)
        {
            // Formed up on the lane: launch the next wave.
            _charging = true;
            _nextPhaseTime = now + ChargeDuration;
            base.CurrentOrder = MovementOrder.MovementOrderChargeToTarget(enemy);
            return;
        }

        WorldPosition worldPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            stagingPoint,
            targetPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 8f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(worldPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
