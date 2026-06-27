using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal enum BattleAITerrainPreference
{
    DefensiveHighGround,
    ReserveRear,
    ForwardScreen
}

internal readonly struct BattleAITerrainAssessment
{
    public BattleAITerrainAssessment(float ownHeight, float enemyHeight)
    {
        OwnHeight = ownHeight;
        EnemyHeight = enemyHeight;
        ElevationDelta = ownHeight - enemyHeight;
    }

    public float OwnHeight { get; }

    public float EnemyHeight { get; }

    public float ElevationDelta { get; }

    public bool HasUphillAdvantage => ElevationDelta >= 2.5f;

    public bool HasStrongUphillAdvantage => ElevationDelta >= 5f;

    public bool HasDownhillDisadvantage => ElevationDelta <= -2.5f;

    public string Describe()
    {
        if (HasStrongUphillAdvantage)
        {
            return $"uphill+{ElevationDelta:F1}";
        }

        if (HasUphillAdvantage)
        {
            return $"high+{ElevationDelta:F1}";
        }

        if (HasDownhillDisadvantage)
        {
            return $"low{ElevationDelta:F1}";
        }

        return $"flat{ElevationDelta:F1}";
    }
}

internal static class BattleAITerrainAnalyzer
{
    private static readonly Vec2[] SampleDirections =
    {
        new Vec2(1f, 0f),
        new Vec2(-1f, 0f),
        new Vec2(0f, 1f),
        new Vec2(0f, -1f),
        new Vec2(0.7071f, 0.7071f),
        new Vec2(0.7071f, -0.7071f),
        new Vec2(-0.7071f, 0.7071f),
        new Vec2(-0.7071f, -0.7071f)
    };

    public static BattleAITerrainAssessment Analyze(Team team)
    {
        Mission? mission = Mission.Current;
        if (mission?.Scene == null)
        {
            return new BattleAITerrainAssessment(0f, 0f);
        }

        Formation? ownAnchor = FindAnchorFormation(team);
        Formation? enemyAnchor = FindEnemyAnchorFormation(team, ownAnchor);
        if (ownAnchor == null || enemyAnchor == null)
        {
            return new BattleAITerrainAssessment(0f, 0f);
        }

        float ownHeight = mission.Scene.GetTerrainHeight(ownAnchor.CachedMedianPosition.AsVec2);
        float enemyHeight = mission.Scene.GetTerrainHeight(enemyAnchor.CachedMedianPosition.AsVec2);
        return new BattleAITerrainAssessment(ownHeight, enemyHeight);
    }

    public static WorldPosition CreateTerrainAdjustedPosition(Formation referenceFormation, Vec2 desiredPosition, Vec2 enemyPosition, BattleAITerrainPreference preference, float searchRadius)
    {
        Mission? mission = Mission.Current;
        if (mission?.Scene == null)
        {
            WorldPosition fallback = referenceFormation.CachedMedianPosition;
            fallback.SetVec2(desiredPosition);
            return fallback;
        }

        Vec2 bestPosition = FindBestNearbyPosition(mission.Scene, desiredPosition, enemyPosition, preference, searchRadius);
        float height = mission.Scene.GetTerrainHeight(bestPosition);
        mission.Scene.GetHeightAtPoint(bestPosition, BodyFlags.None, ref height);
        return new WorldPosition(position: new Vec3(bestPosition, height), scene: mission.Scene, navMesh: UIntPtr.Zero, hasValidZ: false);
    }

    public static Vec2 FindBestNearbyPosition(Scene scene, Vec2 desiredPosition, Vec2 enemyPosition, BattleAITerrainPreference preference, float searchRadius)
    {
        float baseHeight = scene.GetTerrainHeight(desiredPosition);
        Vec2 directionToEnemy = enemyPosition.IsValid ? (enemyPosition - desiredPosition).Normalized() : Vec2.Zero;
        float bestScore = ScoreCandidate(desiredPosition, desiredPosition, baseHeight, baseHeight, directionToEnemy, preference);
        Vec2 bestPosition = desiredPosition;

        foreach (float radiusScale in new[] { 0.5f, 1f })
        {
            float radius = searchRadius * radiusScale;
            foreach (Vec2 sampleDirection in SampleDirections)
            {
                Vec2 candidate = desiredPosition + sampleDirection * radius;
                float candidateHeight = scene.GetTerrainHeight(candidate);
                float score = ScoreCandidate(candidate, desiredPosition, candidateHeight, baseHeight, directionToEnemy, preference);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate;
                }
            }
        }

        return bestPosition;
    }

    private static float ScoreCandidate(Vec2 candidate, Vec2 desiredPosition, float candidateHeight, float baseHeight, Vec2 directionToEnemy, BattleAITerrainPreference preference)
    {
        Vec2 displacement = candidate - desiredPosition;
        float distancePenalty = displacement.Length * 0.08f;
        float forwardProjection = directionToEnemy.IsValid ? Vec2.DotProduct(displacement, directionToEnemy) : 0f;
        float heightGain = candidateHeight - baseHeight;

        return preference switch
        {
            BattleAITerrainPreference.DefensiveHighGround => (heightGain * 1.8f) - distancePenalty - (forwardProjection * 0.08f),
            BattleAITerrainPreference.ReserveRear => (heightGain * 1.4f) - distancePenalty - (forwardProjection * 0.12f),
            BattleAITerrainPreference.ForwardScreen => (heightGain * 1.25f) - distancePenalty + (forwardProjection * 0.06f),
            _ => heightGain - distancePenalty
        };
    }

    private static Formation? FindEnemyAnchorFormation(Team team, Formation? ownAnchor)
    {
        Mission? mission = Mission.Current;
        if (mission == null)
        {
            return null;
        }

        Formation? bestEnemyAnchor = null;
        float bestDistance = float.MaxValue;
        foreach (Team otherTeam in mission.Teams)
        {
            if (otherTeam == null || !otherTeam.IsEnemyOf(team))
            {
                continue;
            }

            Formation? candidate = FindAnchorFormation(otherTeam);
            if (candidate == null)
            {
                continue;
            }

            if (ownAnchor == null)
            {
                return candidate;
            }

            float distance = ownAnchor.CachedMedianPosition.AsVec2.Distance(candidate.CachedMedianPosition.AsVec2);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestEnemyAnchor = candidate;
            }
        }

        return bestEnemyAnchor;
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
}
