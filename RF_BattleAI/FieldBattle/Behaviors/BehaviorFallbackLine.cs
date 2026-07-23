using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI.FieldBattle.Behaviors;

public sealed class BehaviorFallbackLine : BehaviorComponent
{
    public Formation? AnchorFormation { get; set; }

    public float FallbackDistance { get; set; } = 18f;

    public FormationAI.BehaviorSide FlankSide { get; set; } = FormationAI.BehaviorSide.Middle;

    public float LateralOffset { get; set; }

    // How close the enemy must get to the held line before we take another
    // step back. Without holding a line, the fallback target is recomputed
    // from our own moving median every tick and the withdrawal becomes an
    // endless treadmill the enemy can never catch.
    public float StepTriggerDistance { get; set; } = 30f;

    /// <summary>Enemy distance beyond which no withdrawal happens — the line
    /// simply holds where the army stands. Falling back only makes sense under
    /// approach pressure.</summary>
    public float EngageProximity { get; set; } = 70f;

    private Vec2 _heldFallbackPosition = Vec2.Invalid;

    public BehaviorFallbackLine(Formation formation)
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
        return new TextObject("{=!}Fallback line");
    }

    protected override void CalculateCurrentOrder()
    {
        Formation anchor = AnchorFormation ?? base.Formation;
        if (anchor.CountOfUnits <= 0)
        {
            base.CurrentOrder = MovementOrder.MovementOrderStop;
            return;
        }

        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = anchor.CachedClosestEnemyFormation?.Formation.CachedMedianPosition.AsVec2
            ?? base.Formation.Team.QuerySystem.AverageEnemyPosition;

        // No withdrawal before contact: while the enemy is still far away there is
        // nothing to fall back FROM — hold the line where the army currently
        // stands. Without this, applying a fallback-using doctrine mid-map made
        // the formations march backwards toward their deployment zone (tester
        // report: "troops run back to the start of the map").
        if (enemyPosition.IsValid && anchorPosition.IsValid
            && enemyPosition.DistanceSquared(anchorPosition) > EngageProximity * EngageProximity)
        {
            if (!_heldFallbackPosition.IsValid)
            {
                _heldFallbackPosition = anchorPosition;
            }
            WorldPosition holdPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
                base.Formation,
                _heldFallbackPosition,
                enemyPosition,
                BattleAITerrainPreference.DefensiveHighGround,
                searchRadius: 16f);
            base.CurrentOrder = MovementOrder.MovementOrderMove(holdPosition);
            return;
        }

        // Stepped withdrawal: keep ordering the formation to the line it already
        // holds; only pick a new line once the enemy has closed in on this one.
        if (_heldFallbackPosition.IsValid && enemyPosition.IsValid
            && enemyPosition.DistanceSquared(_heldFallbackPosition) > StepTriggerDistance * StepTriggerDistance)
        {
            return;
        }

        Vec2 fallbackDirection = anchorPosition.IsValid && enemyPosition.IsValid
            ? (anchorPosition - enemyPosition).Normalized()
            : (-base.Formation.Direction).Normalized();

        if (!fallbackDirection.IsValid)
        {
            fallbackDirection = (-base.Formation.Direction).Normalized();
        }

        Vec2 fallbackPosition = anchorPosition + fallbackDirection * FallbackDistance;
        if (LateralOffset != 0f && FlankSide != FormationAI.BehaviorSide.Middle)
        {
            Vec2 lateralDirection = FlankSide == FormationAI.BehaviorSide.Left
                ? new Vec2(-fallbackDirection.y, fallbackDirection.x)
                : new Vec2(fallbackDirection.y, -fallbackDirection.x);

            if (!lateralDirection.IsValid)
            {
                lateralDirection = FlankSide == FormationAI.BehaviorSide.Left ? new Vec2(-1f, 0f) : new Vec2(1f, 0f);
            }

            fallbackPosition += lateralDirection * LateralOffset;
        }

        _heldFallbackPosition = fallbackPosition;

        WorldPosition targetPosition = BattleAITerrainAnalyzer.CreateTerrainAdjustedPosition(
            base.Formation,
            fallbackPosition,
            enemyPosition,
            BattleAITerrainPreference.DefensiveHighGround,
            searchRadius: 16f);
        base.CurrentOrder = MovementOrder.MovementOrderMove(targetPosition);
    }

    public override void TickOccasionally()
    {
        CalculateCurrentOrder();
        base.Formation.SetMovementOrder(base.CurrentOrder);
    }
}
