using System;
using RF_BattleAI.FieldBattle.Behaviors;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIFormationCaptainTuner
{
    private static Mission? _mission;
    private static float _nextUpdateTime;

    public static void Tick()
    {
        Mission? mission = Mission.Current;
        if (!ReferenceEquals(_mission, mission))
        {
            _mission = mission;
            _nextUpdateTime = 0f;
        }

        if (mission == null || mission.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
        {
            return;
        }

        float currentTime = mission.CurrentTime;
        if (currentTime < _nextUpdateTime)
        {
            return;
        }

        _nextUpdateTime = currentTime + 1f;

        foreach (Team team in mission.Teams)
        {
            if (team.TeamAI == null)
            {
                continue;
            }

            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                ApplyToFormation(formation);
            }
        }
    }

    private static void ApplyToFormation(Formation? formation)
    {
        if (formation?.AI == null || formation.CountOfUnits <= 0)
        {
            return;
        }

        if (!formation.IsAIControlled)
        {
            return;
        }

        SergeantProfile? profile = BattleAISergeantDoctrineAdvisor.AnalyzeAnyFormationCaptain(formation);
        if (profile == null)
        {
            return;
        }

        float intensity = GetIntensity(profile.TacticsSkill);
        switch (profile.FormationClass)
        {
            case "infantry":
                ApplyInfantryBias(formation, profile, intensity);
                break;
            case "ranged":
                ApplyRangedBias(formation, profile, intensity);
                break;
            case "cavalry":
                ApplyCavalryBias(formation, profile, intensity);
                break;
            case "horse_archer":
                ApplyHorseArcherBias(formation, profile, intensity);
                break;
        }
    }

    private static void ApplyInfantryBias(Formation formation, SergeantProfile profile, float intensity)
    {
        switch (profile.Role)
        {
            case "tactician":
                RaiseWeight<BehaviorDefend>(formation, 0.95f + (0.17f * intensity));
                RaiseWeight<BehaviorMaintainReserve>(formation, 0.85f + (0.2f * intensity));
                break;
            case "raider":
                RaiseWeight<BehaviorAdvance>(formation, 0.95f + (0.14f * intensity));
                RaiseWeight<BehaviorTacticalCharge>(formation, 0.95f + (0.18f * intensity));
                break;
            default:
                RaiseWeight<BehaviorAdvance>(formation, 0.92f + (0.12f * intensity));
                RaiseWeight<BehaviorTacticalCharge>(formation, 0.95f + (0.14f * intensity));
                break;
        }
    }

    private static void ApplyRangedBias(Formation formation, SergeantProfile profile, float intensity)
    {
        switch (profile.Role)
        {
            case "archer_captain":
                RaiseWeight<BehaviorScreenedSkirmish>(formation, 0.92f + (0.16f * intensity));
                RaiseWeight<BehaviorSkirmish>(formation, 0.72f + (0.12f * intensity));
                break;
            case "tactician":
                RaiseWeight<BehaviorDefend>(formation, 0.88f + (0.16f * intensity));
                RaiseWeight<BehaviorScreenedSkirmish>(formation, 0.82f + (0.12f * intensity));
                break;
            default:
                RaiseWeight<BehaviorScreenedSkirmish>(formation, 0.84f + (0.12f * intensity));
                break;
        }
    }

    private static void ApplyCavalryBias(Formation formation, SergeantProfile profile, float intensity)
    {
        switch (profile.Role)
        {
            case "cavalry_shock":
                RaiseWeight<BehaviorFlank>(formation, 0.95f + (0.18f * intensity));
                RaiseWeight<BehaviorTacticalCharge>(formation, 0.98f + (0.2f * intensity));
                break;
            case "tactician":
                RaiseWeight<BehaviorProtectFlank>(formation, 0.92f + (0.16f * intensity));
                RaiseWeight<BehaviorFlank>(formation, 0.82f + (0.1f * intensity));
                break;
            case "raider":
                RaiseWeight<BehaviorFlank>(formation, 0.98f + (0.18f * intensity));
                RaiseWeight<BehaviorTacticalCharge>(formation, 0.9f + (0.14f * intensity));
                break;
            default:
                RaiseWeight<BehaviorProtectFlank>(formation, 0.88f + (0.14f * intensity));
                RaiseWeight<BehaviorFlank>(formation, 0.88f + (0.14f * intensity));
                break;
        }
    }

    private static void ApplyHorseArcherBias(Formation formation, SergeantProfile profile, float intensity)
    {
        switch (profile.Role)
        {
            case "horse_archer_raider":
                RaiseWeight<BehaviorMountedSkirmish>(formation, 0.95f + (0.16f * intensity));
                RaiseWeight<BehaviorHorseArcherSkirmish>(formation, 0.98f + (0.18f * intensity));
                break;
            case "raider":
                RaiseWeight<BehaviorMountedSkirmish>(formation, 0.92f + (0.16f * intensity));
                RaiseWeight<BehaviorHorseArcherSkirmish>(formation, 0.95f + (0.16f * intensity));
                break;
            default:
                RaiseWeight<BehaviorMountedSkirmish>(formation, 0.88f + (0.12f * intensity));
                RaiseWeight<BehaviorHorseArcherSkirmish>(formation, 0.9f + (0.14f * intensity));
                break;
        }
    }

    private static float GetIntensity(int tacticsSkill)
    {
        if (tacticsSkill >= 160)
        {
            return 1f;
        }

        if (tacticsSkill <= 40)
        {
            return 0.35f;
        }

        return 0.35f + ((tacticsSkill - 40) / 120f * 0.65f);
    }

    private static void RaiseWeight<T>(Formation formation, float desiredWeight) where T : BehaviorComponent
    {
        BehaviorComponent? behavior = formation.AI?.GetBehavior<T>();
        if (behavior == null || behavior.WeightFactor >= desiredWeight)
        {
            return;
        }

        formation.AI!.SetBehaviorWeight<T>(desiredWeight);
    }
}
