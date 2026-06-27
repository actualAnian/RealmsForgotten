using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal readonly struct BattleAIFormationComposition
{
    public BattleAIFormationComposition(
        float infantryRatio,
        float rangedRatio,
        float cavalryRatio,
        float rangedCavalryRatio,
        float enemyMountedRatio,
        int infantryFormationCount,
        bool hasMountedFormation)
    {
        InfantryRatio = infantryRatio;
        RangedRatio = rangedRatio;
        CavalryRatio = cavalryRatio;
        RangedCavalryRatio = rangedCavalryRatio;
        EnemyMountedRatio = enemyMountedRatio;
        InfantryFormationCount = infantryFormationCount;
        HasMountedFormation = hasMountedFormation;
    }

    public float InfantryRatio { get; }

    public float RangedRatio { get; }

    public float CavalryRatio { get; }

    public float RangedCavalryRatio { get; }

    public float MountedRatio => CavalryRatio + RangedCavalryRatio;

    public float EnemyMountedRatio { get; }

    public int InfantryFormationCount { get; }

    public bool HasMountedFormation { get; }
}

internal static class BattleAIFormationCompositionHelper
{
    public static BattleAIFormationComposition FromTeam(Team team)
    {
        int infantryUnits = 0;
        int rangedUnits = 0;
        int cavalryUnits = 0;
        int rangedCavalryUnits = 0;
        int infantryFormationCount = 0;
        bool hasMountedFormation = false;

        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            int unitCount = formation.CountOfUnits;
            if (unitCount <= 0)
            {
                continue;
            }

            if (formation.QuerySystem.IsInfantryFormation)
            {
                infantryUnits += unitCount;
                infantryFormationCount++;
                continue;
            }

            if (formation.QuerySystem.IsRangedFormation)
            {
                rangedUnits += unitCount;
                continue;
            }

            if (formation.QuerySystem.IsCavalryFormation)
            {
                cavalryUnits += unitCount;
                hasMountedFormation = true;
                continue;
            }

            if (formation.QuerySystem.IsRangedCavalryFormation)
            {
                rangedCavalryUnits += unitCount;
                hasMountedFormation = true;
            }
        }

        int totalUnits = infantryUnits + rangedUnits + cavalryUnits + rangedCavalryUnits;
        float total = totalUnits > 0 ? totalUnits : 1f;

        int enemyMountedUnits = 0;
        int enemyTotalUnits = 0;
        Mission? mission = Mission.Current;
        if (mission != null)
        {
            foreach (Team missionTeam in mission.Teams)
            {
                if (missionTeam.Side == team.Side)
                {
                    continue;
                }

                foreach (Formation formation in missionTeam.FormationsIncludingEmpty)
                {
                    int unitCount = formation.CountOfUnits;
                    if (unitCount <= 0)
                    {
                        continue;
                    }

                    enemyTotalUnits += unitCount;
                    if (formation.QuerySystem.IsCavalryFormation || formation.QuerySystem.IsRangedCavalryFormation)
                    {
                        enemyMountedUnits += unitCount;
                    }
                }
            }
        }

        float enemyMountedRatio = enemyTotalUnits > 0 ? enemyMountedUnits / (float)enemyTotalUnits : 0f;

        return new BattleAIFormationComposition(
            infantryUnits / total,
            rangedUnits / total,
            cavalryUnits / total,
            rangedCavalryUnits / total,
            enemyMountedRatio,
            infantryFormationCount,
            hasMountedFormation);
    }
}
