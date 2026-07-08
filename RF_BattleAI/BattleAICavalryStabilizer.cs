using System;
using System.Collections.Generic;
using RF_BattleAI.FieldBattle.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAICavalryStabilizer
{
    private sealed class TeamState
    {
        public float ActivatedAt;
        public float MinimumHoldSeconds;
        public float ChargeDistanceThreshold;
        public int InitialAnchorCount;
        public int InitialEnemyCavalryCount;
        public bool ChargeCommitted;
        public float ChargeCommittedAt;
    }

    private sealed class RegroupState
    {
        public float StartedAt;
        public FormationAI.BehaviorSide Side;
    }

    private const float TickInterval = 0.5f;
    private const float OverrideWeight = 2.25f;
    private const float RegroupOverrideWeight = 2.55f;
    private const float AttackerWingForwardOffset = 48f;
    private const float AttackerWingLateralDistance = 102f;
    private const float DefenderReserveRearOffset = 22f;
    private const float DefenderReserveLateralDistance = 42f;
    private const float RegroupRearOffset = 48f;
    private const float RegroupLateralDistance = 112f;
    private const float RegroupReadyRadius = 24f;
    private const float ChargeImpactGraceSeconds = 2.5f;
    private const float MinimumRegroupSeconds = 2f;
    private const float MaximumRegroupSeconds = 7f;
    private const float RegroupCooldownSeconds = 7f;
    private const float StuckRegroupRetryCooldownSeconds = 2.5f;
    private const float BoggedDownSpeed = 1.2f;
    private const float BehindEnemyRegroupLateralFactor = 1.4f;
    private const float RegroupEnemyDistance = 30f;
    private const float RegroupForwardOverrun = 26f;
    private const float LocalPowerLossRatio = 0.85f;
    private const float LocalUnitLossRatio = 0.68f;
    private const float InfantryTrapPowerRatio = 1.45f;
    private const float InfantryTrapUnitRatio = 1.55f;
    private const float ReadyRadius = 30f;
    private const float MinLateralDistance = 54f;
    private const float MaxForwardDepth = 18f;
    private const float CloseCombatDistance = 26f;
    private const float MinimumArmyDistance = 34f;
    private const float FrontlineEngagementDistance = 18f;
    private const float AnchorCasualtyTriggerRatio = 0.55f;
    private const float EnemyCavalryDepletionRatio = 0.62f;
    private const float EnemyCavalryThreatDistance = 95f;

    private static readonly Dictionary<Formation, string> ActiveOverrides = new();
    private static readonly Dictionary<Formation, RegroupState> ActiveRegroups = new();
    private static readonly Dictionary<Formation, float> RegroupCooldowns = new();
    private static readonly Dictionary<Team, TeamState> TeamStates = new();

    private static Mission? _mission;
    private static float _nextTickTime;

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_mission, mission))
        {
            Reset();
            _mission = mission;
        }

        if (mission == null || mission.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
        {
            return;
        }

        if (mission.CurrentTime < _nextTickTime)
        {
            return;
        }

        _nextTickTime = mission.CurrentTime + TickInterval;

        foreach (Team team in mission.Teams)
        {
            ProcessTeam(mission, team);
        }
    }

    private static void ProcessTeam(Mission mission, Team team)
    {
        if (!team.HasTeamAi)
        {
            ReleaseTeamOverrides(team, "no-team-ai");
            TeamStates.Remove(team);
            return;
        }

        Formation? anchor = FindAnchorFormation(team);
        if (anchor == null)
        {
            ReleaseTeamOverrides(team, "no-anchor");
            TeamStates.Remove(team);
            return;
        }

        Formation? enemyFormation = anchor.CachedClosestEnemyFormation?.Formation;
        if (enemyFormation == null || enemyFormation.CountOfUnits <= 0)
        {
            ReleaseTeamOverrides(team, "no-enemy");
            TeamStates.Remove(team);
            return;
        }

        TeamState state = GetOrCreateState(mission, team, anchor);
        if (!state.ChargeCommitted && ShouldCommitCharge(mission, team, anchor, enemyFormation, state, out string commitReason))
        {
            state.ChargeCommitted = true;
            state.ChargeCommittedAt = mission.CurrentTime;
            BattleAIRuntimeTracer.NoteEvent($"cavalry_commit | team={DescribeTeam(team)} | reason={commitReason}");
            ReleaseTeamOverrides(team, "commit");
        }

        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (!IsEligibleCavalry(formation))
            {
                EndRegroup(formation, mission.CurrentTime, "not-eligible", applyCooldown: false);
                ReleaseOverride(formation, "not-eligible");
                continue;
            }

            FormationAI.BehaviorSide side = ResolveFlankSide(formation, anchor, enemyFormation);

            if (state.ChargeCommitted)
            {
                if (ShouldMaintainRegroup(mission, formation, anchor, enemyFormation, out string regroupReason))
                {
                    ApplyRegroupOverride(formation, anchor, enemyFormation, side, regroupReason);
                    continue;
                }

                if (ShouldStartRegroup(mission, formation, anchor, enemyFormation, side, state, out string regroupStartReason))
                {
                    ActiveRegroups[formation] = new RegroupState
                    {
                        StartedAt = mission.CurrentTime,
                        Side = side
                    };
                    BattleAIRuntimeTracer.NoteEvent(
                        $"cavalry_regroup start | team={DescribeTeam(formation.Team)} | F{(int)formation.FormationIndex} | side={side} | {regroupStartReason}");
                    ApplyRegroupOverride(formation, anchor, enemyFormation, side, regroupStartReason);
                    continue;
                }

                // If the regroup timed out while still far from the rally point,
                // the formation is probably wedged in the melee — retry soon
                // instead of leaving it unassisted for the full cooldown.
                float regroupCooldown = RegroupCooldownSeconds;
                if (ActiveRegroups.TryGetValue(formation, out RegroupState? endedRegroup))
                {
                    Vec2 rallyPoint = GetRegroupPoint(formation, anchor, enemyFormation, endedRegroup.Side);
                    if (formation.CachedMedianPosition.AsVec2.Distance(rallyPoint) > RegroupReadyRadius * 2f)
                    {
                        regroupCooldown = StuckRegroupRetryCooldownSeconds;
                    }
                }

                EndRegroup(formation, mission.CurrentTime, "complete", applyCooldown: true, cooldownSeconds: regroupCooldown);
                ReleaseOverride(formation, "committed");
                continue;
            }

            if (formation.AI?.ActiveBehavior is BehaviorCavalryReposition)
            {
                ActiveOverrides.Remove(formation);
                continue;
            }

            EndRegroup(formation, mission.CurrentTime, "pre-commit", applyCooldown: false);
            if (ShouldStabilize(formation, team, anchor, enemyFormation, side, out string reason))
            {
                ApplyOverride(formation, team, anchor, enemyFormation, side, state, reason);
            }
            else
            {
                ReleaseOverride(formation, reason);
            }
        }
    }

    private static TeamState GetOrCreateState(Mission mission, Team team, Formation anchor)
    {
        if (TeamStates.TryGetValue(team, out TeamState? state))
        {
            return state;
        }

        float holdSeconds = team.Side == BattleSideEnum.Attacker ? 4.75f : 3.25f;
        float chargeDistance = team.Side == BattleSideEnum.Attacker ? 92f : 100f;
        state = new TeamState
        {
            ActivatedAt = mission.CurrentTime,
            MinimumHoldSeconds = holdSeconds,
            ChargeDistanceThreshold = chargeDistance,
            InitialAnchorCount = anchor.CountOfUnits,
            InitialEnemyCavalryCount = CountEnemyCavalry(team),
            ChargeCommitted = false
        };

        TeamStates[team] = state;
        BattleAIRuntimeTracer.NoteEvent(
            $"cavalry_stage | team={DescribeTeam(team)} | hold={holdSeconds:F1} | chargeDistance={chargeDistance:F1} | enemyCav={state.InitialEnemyCavalryCount}");
        return state;
    }

    private static bool ShouldCommitCharge(Mission mission, Team team, Formation anchor, Formation enemyFormation, TeamState state, out string reason)
    {
        reason = "hold";

        if (mission.CurrentTime - state.ActivatedAt < state.MinimumHoldSeconds)
        {
            return false;
        }

        float anchorEnemyDistance = anchor.CachedMedianPosition.AsVec2.Distance(enemyFormation.CachedMedianPosition.AsVec2);
        if (anchorEnemyDistance <= state.ChargeDistanceThreshold)
        {
            reason = $"distance={anchorEnemyDistance:F1}";
            return true;
        }

        if (state.InitialAnchorCount > 0 && anchor.CountOfUnits <= (int)(state.InitialAnchorCount * AnchorCasualtyTriggerRatio))
        {
            reason = $"anchor-losses={anchor.CountOfUnits}/{state.InitialAnchorCount}";
            return true;
        }

        if (state.InitialEnemyCavalryCount >= 6)
        {
            int currentEnemyCavalry = CountEnemyCavalry(team);
            if (currentEnemyCavalry <= (int)(state.InitialEnemyCavalryCount * EnemyCavalryDepletionRatio))
            {
                reason = $"enemy-cav-depleted={currentEnemyCavalry}/{state.InitialEnemyCavalryCount}";
                return true;
            }
        }

        Formation? nearestEnemyCavalry = FindNearestEnemyCavalry(team, anchor);
        if (nearestEnemyCavalry != null)
        {
            float enemyCavalryDistance = nearestEnemyCavalry.CachedMedianPosition.AsVec2.Distance(anchor.CachedMedianPosition.AsVec2);
            if (enemyCavalryDistance <= EnemyCavalryThreatDistance)
            {
                reason = $"enemy-cav-threat={enemyCavalryDistance:F1}";
                return true;
            }
        }

        if (IsInfantryFrontlineEngaged(anchor))
        {
            reason = "frontline-engaged";
            return true;
        }

        return false;
    }

    private static bool ShouldStabilize(Formation cavalry, Team team, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide side, out string reason)
    {
        reason = "stable";

        BehaviorComponent? activeBehavior = cavalry.AI?.ActiveBehavior;
        if (activeBehavior is not BehaviorFlank
            && activeBehavior is not BehaviorProtectFlank
            && activeBehavior is not BehaviorTacticalCharge
            && activeBehavior is not BehaviorCharge)
        {
            reason = "other-behavior";
            return false;
        }

        Vec2 cavalryPosition = cavalry.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        float cavalryEnemyDistance = cavalryPosition.Distance(enemyPosition);
        float anchorEnemyDistance = anchor.CachedMedianPosition.AsVec2.Distance(enemyPosition);
        if (cavalryEnemyDistance <= CloseCombatDistance || anchorEnemyDistance <= MinimumArmyDistance)
        {
            reason = "commit-range";
            return false;
        }

        Vec2 targetPoint = team.Side == BattleSideEnum.Attacker
            ? BehaviorCavalryReposition.ComputeWingPoint(anchor, enemyFormation, side, AttackerWingForwardOffset, AttackerWingLateralDistance)
            : BehaviorCavalryReposition.ComputeAssemblyPoint(anchor, enemyFormation, side, DefenderReserveLateralDistance, DefenderReserveRearOffset);

        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchor.CachedMedianPosition.AsVec2).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            reason = "invalid-direction";
            return false;
        }

        Vec2 lateralDirection = side == FormationAI.BehaviorSide.Left
            ? new Vec2(-directionToEnemy.y, directionToEnemy.x)
            : new Vec2(directionToEnemy.y, -directionToEnemy.x);
        Vec2 offset = cavalryPosition - anchor.CachedMedianPosition.AsVec2;
        float lateralDistance = MathF.Abs(Vec2.DotProduct(offset, lateralDirection));
        float forwardDepth = Vec2.DotProduct(offset, directionToEnemy);
        float distanceToTarget = cavalryPosition.Distance(targetPoint);

        bool tooCentral = lateralDistance < MinLateralDistance;
        bool tooFarForward = team.Side == BattleSideEnum.Attacker && forwardDepth > MaxForwardDepth;
        bool notInStage = distanceToTarget > ReadyRadius;

        if (tooCentral || tooFarForward || notInStage)
        {
            reason = $"reposition lateral={lateralDistance:F1} forward={forwardDepth:F1} stage={distanceToTarget:F1} active={activeBehavior.GetType().Name}";
            return true;
        }

        return false;
    }

    private static bool ShouldStartRegroup(
        Mission mission,
        Formation cavalry,
        Formation anchor,
        Formation enemyFormation,
        FormationAI.BehaviorSide side,
        TeamState state,
        out string reason)
    {
        reason = "stable";

        if (mission.CurrentTime - state.ChargeCommittedAt < ChargeImpactGraceSeconds)
        {
            reason = "grace";
            return false;
        }

        if (ActiveRegroups.ContainsKey(cavalry))
        {
            reason = "already-active";
            return false;
        }

        if (RegroupCooldowns.TryGetValue(cavalry, out float cooldownUntil) && mission.CurrentTime < cooldownUntil)
        {
            reason = "cooldown";
            return false;
        }

        Formation? localEnemy = cavalry.CachedClosestEnemyFormation?.Formation ?? enemyFormation;
        if (localEnemy == null || localEnemy.CountOfUnits <= 0)
        {
            reason = "no-local-enemy";
            return false;
        }

        float localEnemyDistance = cavalry.CachedMedianPosition.AsVec2.Distance(localEnemy.CachedMedianPosition.AsVec2);
        if (localEnemyDistance > RegroupEnemyDistance)
        {
            reason = $"enemy-distance={localEnemyDistance:F1}";
            return false;
        }

        if (!localEnemy.QuerySystem.IsInfantryFormation && !localEnemy.QuerySystem.IsRangedFormation)
        {
            reason = "enemy-mobile";
            return false;
        }

        float cavalryPower = Math.Max(1f, cavalry.QuerySystem.FormationPower);
        float enemyPower = Math.Max(1f, localEnemy.QuerySystem.FormationPower);
        bool localPowerDisadvantage = cavalryPower <= enemyPower * LocalPowerLossRatio;
        bool localUnitDisadvantage = cavalry.CountOfUnits <= localEnemy.CountOfUnits * LocalUnitLossRatio;
        bool underRangedPressure = IsFormationUnderRangedPressure(cavalry);
        bool overextended = IsOverextended(cavalry, anchor, localEnemy, side);
        bool trappedByHeavyInfantry = localEnemy.QuerySystem.IsInfantryFormation
            && enemyPower >= cavalryPower * InfantryTrapPowerRatio
            && localEnemy.CountOfUnits >= cavalry.CountOfUnits * InfantryTrapUnitRatio;
        // Grinding at power parity inside an infantry mass never trips the
        // disadvantage triggers — detect it by the horses barely moving while
        // in melee contact with infantry.
        bool boggedDown = localEnemy.QuerySystem.IsInfantryFormation
            && localEnemyDistance <= CloseCombatDistance
            && cavalry.CachedMovementSpeed < BoggedDownSpeed;

        if (!localPowerDisadvantage && !localUnitDisadvantage && !underRangedPressure && !overextended && !trappedByHeavyInfantry && !boggedDown)
        {
            reason = "no-trigger";
            return false;
        }

        reason =
            $"enemy={DescribeFormationType(localEnemy)} dist={localEnemyDistance:F1} power={cavalryPower:F1}/{enemyPower:F1} units={cavalry.CountOfUnits}/{localEnemy.CountOfUnits} ranged={underRangedPressure} overextended={overextended} infantryTrap={trappedByHeavyInfantry} bogged={boggedDown}";
        return true;
    }

    private static bool ShouldMaintainRegroup(Mission mission, Formation cavalry, Formation anchor, Formation enemyFormation, out string reason)
    {
        reason = "none";
        if (!ActiveRegroups.TryGetValue(cavalry, out RegroupState? regroupState))
        {
            return false;
        }

        Vec2 regroupPoint = GetRegroupPoint(cavalry, anchor, enemyFormation, regroupState.Side);
        float distanceToRegroupPoint = cavalry.CachedMedianPosition.AsVec2.Distance(regroupPoint);
        float elapsed = mission.CurrentTime - regroupState.StartedAt;

        if (elapsed < MinimumRegroupSeconds)
        {
            reason = $"elapsed={elapsed:F1} target={distanceToRegroupPoint:F1}";
            return true;
        }

        if (distanceToRegroupPoint > RegroupReadyRadius && elapsed < MaximumRegroupSeconds)
        {
            reason = $"closing target={distanceToRegroupPoint:F1} elapsed={elapsed:F1}";
            return true;
        }

        return false;
    }

    private static void ApplyOverride(Formation cavalry, Team team, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide side, TeamState state, string reason)
    {
        if (cavalry.AI == null)
        {
            return;
        }

        BehaviorCavalryReposition? behavior = cavalry.AI.GetBehavior<BehaviorCavalryReposition>();
        if (behavior == null)
        {
            return;
        }

        behavior.AnchorFormation = anchor;
        behavior.FlankSide = side;
        behavior.OuterArcLateralDistance = 0f;
        behavior.OuterArcRearOffset = 0f;

        if (team.Side == BattleSideEnum.Attacker)
        {
            behavior.ForwardOffset = AttackerWingForwardOffset;
            behavior.LateralDistance = AttackerWingLateralDistance;
            behavior.RearOffset = 0f;
        }
        else
        {
            behavior.ForwardOffset = 0f;
            behavior.LateralDistance = DefenderReserveLateralDistance;
            behavior.RearOffset = DefenderReserveRearOffset;
        }

        cavalry.AI.SetBehaviorWeight<BehaviorCavalryReposition>(OverrideWeight);

        float elapsed = (Mission.Current?.CurrentTime ?? state.ActivatedAt) - state.ActivatedAt;
        string fullReason = $"{reason} holdLeft={Math.Max(0f, state.MinimumHoldSeconds - elapsed):F1}";
        if (!ActiveOverrides.TryGetValue(cavalry, out string? currentReason) || !string.Equals(currentReason, fullReason, StringComparison.Ordinal))
        {
            ActiveOverrides[cavalry] = fullReason;
            BattleAIRuntimeTracer.NoteEvent(
                $"cavalry_override on | team={DescribeTeam(cavalry.Team)} | F{(int)cavalry.FormationIndex} | side={side} | {fullReason}");
        }
    }

    private static void ApplyRegroupOverride(Formation cavalry, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide side, string reason)
    {
        if (cavalry.AI == null)
        {
            return;
        }

        BehaviorCavalryReposition? behavior = cavalry.AI.GetBehavior<BehaviorCavalryReposition>();
        if (behavior == null)
        {
            return;
        }

        behavior.AnchorFormation = anchor;
        behavior.FlankSide = side;
        behavior.ForwardOffset = 0f;
        behavior.LateralDistance = GetRegroupLateralDistance(cavalry, anchor, enemyFormation);
        behavior.RearOffset = RegroupRearOffset;
        behavior.OuterArcLateralDistance = 0f;
        behavior.OuterArcRearOffset = 0f;
        cavalry.AI.SetBehaviorWeight<BehaviorCavalryReposition>(RegroupOverrideWeight);

        string fullReason = $"regroup {reason}";
        if (!ActiveOverrides.TryGetValue(cavalry, out string? currentReason) || !string.Equals(currentReason, fullReason, StringComparison.Ordinal))
        {
            ActiveOverrides[cavalry] = fullReason;
            BattleAIRuntimeTracer.NoteEvent(
                $"cavalry_override on | team={DescribeTeam(cavalry.Team)} | F{(int)cavalry.FormationIndex} | side={side} | {fullReason}");
        }
    }

    private static void ReleaseOverride(Formation? cavalry, string reason)
    {
        if (cavalry?.AI == null)
        {
            return;
        }

        BehaviorCavalryReposition? behavior = cavalry.AI.GetBehavior<BehaviorCavalryReposition>();
        if (behavior == null)
        {
            return;
        }

        behavior.ForwardOffset = 0f;
        cavalry.AI.SetBehaviorWeight<BehaviorCavalryReposition>(0f);
        if (ActiveOverrides.Remove(cavalry))
        {
            BattleAIRuntimeTracer.NoteEvent(
                $"cavalry_override off | team={DescribeTeam(cavalry.Team)} | F{(int)cavalry.FormationIndex} | reason={reason}");
        }
    }

    private static void EndRegroup(Formation? cavalry, float currentTime, string reason, bool applyCooldown = true, float cooldownSeconds = RegroupCooldownSeconds)
    {
        if (cavalry == null || !ActiveRegroups.Remove(cavalry))
        {
            return;
        }

        if (applyCooldown)
        {
            RegroupCooldowns[cavalry] = currentTime + cooldownSeconds;
        }
        else
        {
            RegroupCooldowns.Remove(cavalry);
        }

        BattleAIRuntimeTracer.NoteEvent(
            $"cavalry_regroup end | team={DescribeTeam(cavalry.Team)} | F{(int)cavalry.FormationIndex} | reason={reason}");
    }

    private static void ReleaseTeamOverrides(Team team, string reason)
    {
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            ReleaseOverride(formation, reason);
        }
    }

    private static Formation? FindAnchorFormation(Team team)
    {
        Formation? mainInfantry = null;
        Formation? bestInfantry = null;
        float bestInfantryPower = 0f;
        Formation? bestArchers = null;
        float bestArcherPower = 0f;

        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits <= 0 || !formation.IsAIControlled)
            {
                continue;
            }

            if (formation.QuerySystem.IsInfantryFormation && formation.AI != null && formation.AI.IsMainFormation)
            {
                mainInfantry = formation;
            }

            float power = formation.QuerySystem.FormationPower;
            if (formation.QuerySystem.IsInfantryFormation && power > bestInfantryPower)
            {
                bestInfantry = formation;
                bestInfantryPower = power;
                continue;
            }

            if (formation.QuerySystem.IsRangedFormation && power > bestArcherPower)
            {
                bestArchers = formation;
                bestArcherPower = power;
            }
        }

        return mainInfantry ?? bestInfantry ?? bestArchers;
    }

    private static FormationAI.BehaviorSide ResolveFlankSide(Formation cavalry, Formation anchor, Formation enemyFormation)
    {
        FormationAI? formationAi = cavalry.AI;
        if (formationAi?.Side == FormationAI.BehaviorSide.Left || formationAi?.Side == FormationAI.BehaviorSide.Right)
        {
            return formationAi.Side;
        }

        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            return FormationAI.BehaviorSide.Left;
        }

        Vec2 lateralDirection = new(-directionToEnemy.y, directionToEnemy.x);
        float lateralDot = Vec2.DotProduct(cavalry.CachedMedianPosition.AsVec2 - anchorPosition, lateralDirection);
        return lateralDot >= 0f ? FormationAI.BehaviorSide.Left : FormationAI.BehaviorSide.Right;
    }

    private static Formation? FindNearestEnemyCavalry(Team team, Formation anchor)
    {
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return null;
        }

        Formation? best = null;
        float bestDistance = float.MaxValue;
        foreach (Team otherTeam in mission.Teams)
        {
            if (otherTeam == null || !otherTeam.IsEnemyOf(team))
            {
                continue;
            }

            foreach (Formation formation in otherTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0 || !formation.QuerySystem.IsCavalryFormation)
                {
                    continue;
                }

                float distance = formation.CachedMedianPosition.AsVec2.Distance(anchor.CachedMedianPosition.AsVec2);
                if (distance < bestDistance)
                {
                    best = formation;
                    bestDistance = distance;
                }
            }
        }

        return best;
    }

    private static int CountEnemyCavalry(Team team)
    {
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Team otherTeam in mission.Teams)
        {
            if (otherTeam == null || !otherTeam.IsEnemyOf(team))
            {
                continue;
            }

            foreach (Formation formation in otherTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0 && (formation.QuerySystem.IsCavalryFormation || formation.QuerySystem.IsRangedCavalryFormation))
                {
                    count += formation.CountOfUnits;
                }
            }
        }

        return count;
    }

    private static bool IsInfantryFrontlineEngaged(Formation anchor)
    {
        if (!anchor.QuerySystem.IsInfantryFormation)
        {
            return false;
        }

        FormationQuerySystem? enemy = anchor.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
        if (enemy?.Formation == null)
        {
            return false;
        }

        return anchor.CachedMedianPosition.AsVec2.Distance(enemy.Formation.CachedMedianPosition.AsVec2) <= FrontlineEngagementDistance;
    }

    private static bool IsFormationUnderRangedPressure(Formation? formation)
    {
        return formation != null
            && formation.CountOfUnits > 0
            && (formation.QuerySystem.IsUnderRangedAttack || formation.QuerySystem.UnderRangedAttackRatio > 0.12f);
    }

    private static bool IsOverextended(Formation cavalry, Formation anchor, Formation localEnemy, FormationAI.BehaviorSide side)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = localEnemy.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            return false;
        }

        Vec2 lateralDirection = side == FormationAI.BehaviorSide.Left
            ? new Vec2(-directionToEnemy.y, directionToEnemy.x)
            : new Vec2(directionToEnemy.y, -directionToEnemy.x);
        Vec2 cavalryOffset = cavalry.CachedMedianPosition.AsVec2 - anchorPosition;
        float forwardDepth = Vec2.DotProduct(cavalryOffset, directionToEnemy);
        float lateralDistance = MathF.Abs(Vec2.DotProduct(cavalryOffset, lateralDirection));
        return forwardDepth >= RegroupForwardOverrun && lateralDistance <= RegroupLateralDistance * 0.9f;
    }

    private static float GetRegroupLateralDistance(Formation cavalry, Formation anchor, Formation enemyFormation)
    {
        Vec2 anchorPosition = anchor.CachedMedianPosition.AsVec2;
        Vec2 enemyPosition = enemyFormation.CachedMedianPosition.AsVec2;
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - anchorPosition).Normalized() : anchor.Direction;
        if (!directionToEnemy.IsValid)
        {
            return RegroupLateralDistance;
        }

        // Cavalry that overran past the enemy line must swing wide on the way
        // back — a straight path to the rally point behind the anchor runs
        // through the infantry mass it just escaped from.
        float enemyDepth = anchorPosition.Distance(enemyPosition);
        float cavalryDepth = Vec2.DotProduct(cavalry.CachedMedianPosition.AsVec2 - anchorPosition, directionToEnemy);
        return cavalryDepth > enemyDepth
            ? RegroupLateralDistance * BehindEnemyRegroupLateralFactor
            : RegroupLateralDistance;
    }

    private static Vec2 GetRegroupPoint(Formation cavalry, Formation anchor, Formation enemyFormation, FormationAI.BehaviorSide side)
    {
        return BehaviorCavalryReposition.ComputeAssemblyPoint(anchor, enemyFormation, side, GetRegroupLateralDistance(cavalry, anchor, enemyFormation), RegroupRearOffset);
    }

    private static bool IsEligibleCavalry(Formation formation)
    {
        return formation.AI != null
            && formation.CountOfUnits > 0
            && formation.IsAIControlled
            && formation.QuerySystem.IsCavalryFormation
            && !formation.QuerySystem.IsRangedCavalryFormation;
    }

    private static string DescribeFormationType(Formation formation)
    {
        if (formation.QuerySystem.IsInfantryFormation)
        {
            return "Inf";
        }

        if (formation.QuerySystem.IsRangedFormation)
        {
            return "Arch";
        }

        if (formation.QuerySystem.IsRangedCavalryFormation)
        {
            return "HorseArch";
        }

        if (formation.QuerySystem.IsCavalryFormation)
        {
            return "Cav";
        }

        return "Other";
    }

    private static string DescribeTeam(Team? team)
    {
        if (team == null)
        {
            return "?";
        }

        return team.Side == BattleSideEnum.Attacker ? "Attacker" : "Defender";
    }

    private static void Reset()
    {
        foreach (KeyValuePair<Formation, string> entry in ActiveOverrides)
        {
            Formation formation = entry.Key;
            BehaviorCavalryReposition? behavior = formation.AI?.GetBehavior<BehaviorCavalryReposition>();
            if (behavior != null)
            {
                behavior.ForwardOffset = 0f;
                FormationAI? formationAi = formation.AI;
                if (formationAi != null)
                {
                    formationAi.SetBehaviorWeight<BehaviorCavalryReposition>(0f);
                }
            }
        }

        ActiveOverrides.Clear();
        ActiveRegroups.Clear();
        RegroupCooldowns.Clear();
        TeamStates.Clear();
        _mission = null;
        _nextTickTime = 0f;
    }
}
