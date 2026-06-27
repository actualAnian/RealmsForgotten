using System;
using System.Collections.Generic;
using RF_BattleAI.FieldBattle.Tactics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAIDoctrineSelector
{
    private static readonly BattleAIDoctrine[] Doctrines = CreateDoctrines();

    public static BattleAIDoctrineSelection? SelectDoctrine(Team team, out string diagnostics)
    {
        bool isBandit = BattleAICombatantHelper.IsBanditTeam(team);
        SergeantDoctrineAdvice commanderAdvice = BattleAISergeantDoctrineAdvisor.AnalyzeCommander(team);
        int tacticsSkill = commanderAdvice.GetEffectiveTacticsSkill(BattleAICombatantHelper.GetCommanderTacticsSkill(team));
        BattleAITerrainAssessment terrain = BattleAITerrainAnalyzer.Analyze(team);
        string cultureId = BattleAICombatantHelper.GetTeamCultureId(team);

        BattleAIDoctrineSelection? bestSelection = null;
        List<string> evaluations = new();
        foreach (BattleAIDoctrine doctrine in Doctrines)
        {
            DoctrineEvaluation evaluation = EvaluateDoctrine(doctrine, team, tacticsSkill, isBandit, terrain, cultureId, commanderAdvice, manualRequest: false);
            evaluations.Add($"{doctrine.Id}={evaluation.Status} score={evaluation.Score:F2} [{evaluation.Reason}]");
            if (!evaluation.IsSelectable)
            {
                continue;
            }

            if (bestSelection == null || evaluation.Score > bestSelection.Score)
            {
                bestSelection = new BattleAIDoctrineSelection(
                    doctrine.Id,
                    tacticsSkill,
                    evaluation.Score,
                    doctrine.Create(team));
            }
        }

        diagnostics = $"context[{DescribeTeamContext(team, tacticsSkill, isBandit, terrain, cultureId, commanderAdvice)}] | {string.Join(" || ", evaluations)}";
        return bestSelection;
    }

    public static BattleAIDoctrineSelection? SelectDoctrineById(Team team, string doctrineId, bool manualRequest, out string diagnostics)
    {
        bool isBandit = BattleAICombatantHelper.IsBanditTeam(team);
        SergeantDoctrineAdvice commanderAdvice = BattleAISergeantDoctrineAdvisor.AnalyzeCommander(team);
        int tacticsSkill = commanderAdvice.GetEffectiveTacticsSkill(BattleAICombatantHelper.GetCommanderTacticsSkill(team));
        BattleAITerrainAssessment terrain = BattleAITerrainAnalyzer.Analyze(team);
        string cultureId = BattleAICombatantHelper.GetTeamCultureId(team);

        foreach (BattleAIDoctrine doctrine in Doctrines)
        {
            if (!string.Equals(doctrine.Id, doctrineId, StringComparison.Ordinal))
            {
                continue;
            }

            DoctrineEvaluation evaluation = EvaluateDoctrine(doctrine, team, tacticsSkill, isBandit, terrain, cultureId, commanderAdvice, manualRequest);
            diagnostics = $"context[{DescribeTeamContext(team, tacticsSkill, isBandit, terrain, cultureId, commanderAdvice)}] | requested={doctrineId} | {evaluation.Status} | score={evaluation.Score:F2} | {evaluation.Reason}";
            if (!evaluation.IsSelectable)
            {
                return null;
            }

            return new BattleAIDoctrineSelection(
                doctrine.Id,
                tacticsSkill,
                evaluation.Score > 0f ? evaluation.Score : 0.01f,
                doctrine.Create(team));
        }

        diagnostics = $"context[{DescribeTeamContext(team, tacticsSkill, isBandit, terrain, cultureId, commanderAdvice)}] | requested={doctrineId} | not_found";
        return null;
    }

    private static BattleAIDoctrine[] CreateDoctrines()
    {
        return new[]
        {
            new BattleAIDoctrine(
                nameof(TacticBanditAdaptiveSkirmish),
                minTacticsSkill: 0,
                isEligible: static (_, isBandit) => isBandit,
                score: static (team, tacticsSkill, _) =>
                {
                    float score = 0.75f;
                    score += GetTacticsBonus(tacticsSkill, 120f);
                    score += team.QuerySystem.RangedRatio + team.QuerySystem.RangedCavalryRatio > 0.15f ? 0.15f : 0f;
                    score += team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio > 0.1f ? 0.1f : 0f;
                    score += team.QuerySystem.RemainingPowerRatio < 1f ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticBanditAdaptiveSkirmish(team)),

            new BattleAIDoctrine(
                nameof(TacticCannaeEnvelopment),
                minTacticsSkill: 175,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.4f
                    && CountInfantryFormations(team) >= 2
                    && team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio > 0.14f,
                score: static (team, tacticsSkill, _) =>
                {
                    float cavalryRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
                    float score = 0.2f;
                    score += GetTacticsBonus(tacticsSkill, 105f);
                    score += CountInfantryFormations(team) >= 2 ? 0.3f : 0f;
                    score += cavalryRatio > 0.14f ? 0.2f : 0f;
                    score += team.QuerySystem.InfantryRatio > 0.4f ? 0.15f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticCannaeEnvelopment(team)),

            new BattleAIDoctrine(
                nameof(TacticRefusedFlank),
                minTacticsSkill: 140,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.4f
                    && CountInfantryFormations(team) >= 2,
                score: static (team, tacticsSkill, _) =>
                {
                    float cavalryRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
                    float score = 0.18f;
                    score += GetTacticsBonus(tacticsSkill, 115f);
                    score += team.QuerySystem.InfantryRatio > 0.4f ? 0.25f : 0f;
                    score += CountInfantryFormations(team) >= 2 ? 0.2f : 0f;
                    score += cavalryRatio > 0.08f ? 0.1f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.15f : 0f;
                    return score;
                },
                create: static team => new TacticRefusedFlank(team)),

            new BattleAIDoctrine(
                nameof(TacticFeignedRetreat),
                minTacticsSkill: 160,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && (team.QuerySystem.RangedCavalryRatio > 0.08f
                        || team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio > 0.22f),
                score: static (team, tacticsSkill, _) =>
                {
                    float mountedMissileRatio = team.QuerySystem.RangedCavalryRatio;
                    float cavalryRatio = team.QuerySystem.CavalryRatio + mountedMissileRatio;
                    float score = 0.22f;
                    score += GetTacticsBonus(tacticsSkill, 110f);
                    score += mountedMissileRatio > 0.08f ? 0.3f : 0f;
                    score += cavalryRatio > 0.22f ? 0.2f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.12f ? 0.1f : 0f;
                    score += team.Side == BattleSideEnum.Attacker ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticFeignedRetreat(team)),

            new BattleAIDoctrine(
                nameof(TacticBaitAndPounce),
                minTacticsSkill: 125,
                isEligible: static (team, isBandit) =>
                {
                    BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
                    return !isBandit
                        && composition.InfantryRatio > 0.3f
                        && composition.RangedRatio > 0.08f
                        && composition.MountedRatio > 0.12f;
                },
                score: static (team, tacticsSkill, _) =>
                {
                    BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
                    float powerRatio = team.QuerySystem.RemainingPowerRatio;
                    float score = 0.16f;
                    score += GetTacticsBonus(tacticsSkill, 135f);
                    score += composition.InfantryRatio > 0.3f ? 0.18f : 0f;
                    score += composition.RangedRatio > 0.08f ? 0.14f : 0f;
                    score += composition.MountedRatio > 0.12f ? 0.2f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.18f : 0f;
                    score += powerRatio <= 1.15f ? 0.12f : 0f;
                    score += powerRatio < 0.9f ? 0.08f : 0f;
                    score += composition.EnemyMountedRatio > 0.24f ? 0.06f : 0f;
                    score += powerRatio > 1.2f ? -0.08f : 0f;
                    return score;
                },
                create: static team => new TacticBaitAndPounce(team)),

            new BattleAIDoctrine(
                nameof(TacticAntiCavalryBrace),
                minTacticsSkill: 95,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.35f
                    && CountInfantryUnits(team) >= 30
                    && team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio > 0.22f,
                score: static (team, tacticsSkill, _) =>
                {
                    float enemyMountedRatio = team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio;
                    float score = 0.16f;
                    score += GetTacticsBonus(tacticsSkill, 160f);
                    score += team.QuerySystem.InfantryRatio > 0.35f ? 0.22f : 0f;
                    score += CountInfantryUnits(team) >= 45 ? 0.12f : 0.06f;
                    score += enemyMountedRatio > 0.22f ? 0.34f : 0f;
                    score += enemyMountedRatio > 0.32f ? 0.14f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.08f ? 0.08f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.16f : 0f;
                    score += team.QuerySystem.RemainingPowerRatio < 0.95f ? 0.08f : 0f;
                    return score;
                },
                create: static team => new TacticAntiCavalryBrace(team)),

            new BattleAIDoctrine(
                nameof(TacticObliqueOrder),
                minTacticsSkill: 130,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.35f
                    && CountInfantryFormations(team) >= 2,
                score: static (team, tacticsSkill, _) =>
                {
                    float cavalryRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
                    float score = 0.18f;
                    score += GetTacticsBonus(tacticsSkill, 120f);
                    score += team.QuerySystem.InfantryRatio > 0.35f ? 0.25f : 0f;
                    score += cavalryRatio > 0.1f ? 0.15f : 0f;
                    score += CountInfantryFormations(team) >= 2 ? 0.2f : 0f;
                    score += team.Side == BattleSideEnum.Attacker ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticObliqueOrder(team)),

            new BattleAIDoctrine(
                nameof(TacticHammerAndAnvil),
                minTacticsSkill: 110,
                isEligible: static (team, isBandit) =>
                {
                    BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
                    return !isBandit
                        && composition.InfantryRatio > 0.28f
                        && composition.MountedRatio > 0.12f;
                },
                score: static (team, tacticsSkill, _) =>
                {
                    BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
                    float cavalryRatio = composition.MountedRatio;
                    float enemyMountedRatio = composition.EnemyMountedRatio;
                    float powerRatio = team.QuerySystem.RemainingPowerRatio;

                    float score = 0.2f;
                    score += GetTacticsBonus(tacticsSkill, 130f);
                    score += composition.InfantryRatio > 0.28f ? 0.2f : 0f;
                    score += cavalryRatio > 0.12f ? 0.25f : 0f;
                    score += cavalryRatio > 0.2f ? 0.12f : 0f;
                    score += composition.RangedRatio > 0.1f ? 0.1f : 0f;
                    score += powerRatio >= 0.9f ? 0.08f : 0f;
                    score += powerRatio > 1.05f ? 0.08f : 0f;
                    score += powerRatio < 0.95f ? -0.12f : 0f;
                    score += enemyMountedRatio > 0.26f ? -0.08f : 0f;
                    score += team.Side == BattleSideEnum.Attacker ? 0.08f : 0f;
                    return score;
                },
                create: static team => new TacticHammerAndAnvil(team)),

            new BattleAIDoctrine(
                nameof(TacticShieldwallAdvance),
                minTacticsSkill: 80,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.45f
                    && team.QuerySystem.RangedRatio > 0.08f,
                score: static (team, tacticsSkill, _) =>
                {
                    float cavalryRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
                    float enemyMountedRatio = team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio;

                    float score = 0.2f;
                    score += GetTacticsBonus(tacticsSkill, 160f);
                    score += team.QuerySystem.InfantryRatio > 0.45f ? 0.35f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.12f ? 0.15f : 0f;
                    score += cavalryRatio < 0.25f ? 0.15f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.22f ? -0.12f : 0f;
                    score += enemyMountedRatio > 0.24f ? -0.06f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticShieldwallAdvance(team)),

            new BattleAIDoctrine(
                nameof(TacticInfantryWaves),
                minTacticsSkill: 90,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.InfantryRatio > 0.5f
                    && CountInfantryFormations(team) >= 1
                    && CountInfantryUnits(team) >= 35,
                score: static (team, tacticsSkill, _) =>
                {
                    float mountedRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
                    int infantryFormations = CountInfantryFormations(team);
                    int infantryUnits = CountInfantryUnits(team);
                    float score = 0.14f;
                    score += GetTacticsBonus(tacticsSkill, 170f);
                    score += team.QuerySystem.InfantryRatio > 0.5f ? 0.32f : 0f;
                    score += infantryFormations >= 2 ? 0.28f : 0.12f;
                    score += infantryUnits >= 60 ? 0.12f : 0.06f;
                    score += team.QuerySystem.RangedRatio > 0.08f ? 0.08f : 0f;
                    score += mountedRatio < 0.18f ? 0.12f : 0f;
                    score += team.QuerySystem.RemainingPowerRatio >= 0.85f ? 0.08f : 0f;
                    score += team.Side == BattleSideEnum.Attacker ? 0.08f : 0f;
                    return score;
                },
                create: static team => new TacticInfantryWaves(team)),

            new BattleAIDoctrine(
                nameof(TacticMissileScreen),
                minTacticsSkill: 105,
                isEligible: static (team, isBandit) =>
                    !isBandit
                    && team.QuerySystem.RangedRatio > 0.22f
                    && team.QuerySystem.InfantryRatio > 0.24f
                    && CountRangedUnits(team) >= 25,
                score: static (team, tacticsSkill, _) =>
                {
                    BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
                    int rangedUnits = CountRangedUnits(team);
                    float score = 0.14f;
                    score += GetTacticsBonus(tacticsSkill, 150f);
                    score += composition.RangedRatio > 0.22f ? 0.34f : 0f;
                    score += composition.InfantryRatio > 0.24f ? 0.18f : 0f;
                    score += rangedUnits >= 45 ? 0.12f : 0.06f;
                    score += composition.MountedRatio < 0.24f ? 0.1f : 0f;
                    score += team.Side == BattleSideEnum.Defender ? 0.12f : 0f;
                    score += composition.EnemyMountedRatio < 0.24f ? 0.08f : 0f;
                    score += composition.EnemyMountedRatio > 0.26f ? -0.14f : 0f;
                    score += composition.RangedRatio > 0.3f ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticMissileScreen(team)),

            new BattleAIDoctrine(
                nameof(TacticElasticDefense),
                minTacticsSkill: 60,
                isEligible: static (_, isBandit) => !isBandit,
                score: static (team, tacticsSkill, _) =>
                {
                    float powerRatio = team.QuerySystem.RemainingPowerRatio;
                    float enemyMountedRatio = team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio;

                    float score = 0.05f;
                    score += GetTacticsBonus(tacticsSkill, 220f);
                    score += team.Side == BattleSideEnum.Defender ? 0.18f : 0f;
                    score += team.QuerySystem.InfantryRatio > 0.3f ? 0.15f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.15f ? 0.12f : 0f;
                    score += enemyMountedRatio > 0.2f ? 0.12f : 0f;
                    score += powerRatio < 0.95f ? 0.25f : 0f;
                    score += powerRatio < 0.8f ? 0.15f : 0f;
                    score += team.QuerySystem.InfantryRatio > 0.5f && CountInfantryUnits(team) >= 35 ? -0.08f : 0f;
                    score += team.QuerySystem.RangedRatio > 0.22f && CountRangedUnits(team) >= 25 ? -0.08f : 0f;
                    return score;
                },
                create: static team => new TacticElasticDefense(team)),

            new BattleAIDoctrine(
                nameof(TacticReserveCounterattack),
                minTacticsSkill: 100,
                isEligible: static (team, isBandit) => !isBandit && CountInfantryFormations(team) >= 2,
                score: static (team, tacticsSkill, _) =>
                {
                    float cavalryRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;

                    float score = 0.15f;
                    score += GetTacticsBonus(tacticsSkill, 140f);
                    score += team.Side == BattleSideEnum.Defender ? 0.25f : 0f;
                    score += CountInfantryFormations(team) >= 2 ? 0.45f : 0f;
                    score += cavalryRatio > 0.12f ? 0.15f : 0f;
                    score += team.QuerySystem.InfantryRatio > 0.35f ? 0.1f : 0f;
                    return score;
                },
                create: static team => new TacticReserveCounterattack(team))
        };
    }

    private static float GetTacticsBonus(int tacticsSkill, float scale)
    {
        if (tacticsSkill <= 0)
        {
            return 0f;
        }

        return tacticsSkill / scale;
    }

    private static int CountInfantryFormations(Team team)
    {
        int count = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits > 0 && formation.QuerySystem.IsInfantryFormation)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountInfantryUnits(Team team)
    {
        int count = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits > 0 && formation.QuerySystem.IsInfantryFormation)
            {
                count += formation.CountOfUnits;
            }
        }

        return count;
    }

    private static int CountRangedUnits(Team team)
    {
        int count = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits > 0 && formation.QuerySystem.IsRangedFormation)
            {
                count += formation.CountOfUnits;
            }
        }

        return count;
    }

    private static bool IsManualDoctrineViable(string doctrineId, Team team, bool isBandit)
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
        float infantryRatio = composition.InfantryRatio;
        float rangedRatio = composition.RangedRatio;
        float mountedRatio = composition.MountedRatio;
        float enemyMountedRatio = team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio;
        int infantryFormations = composition.InfantryFormationCount;
        int infantryUnits = CountInfantryUnits(team);
        int rangedUnits = CountRangedUnits(team);

        switch (doctrineId)
        {
            case nameof(TacticBanditAdaptiveSkirmish):
                return isBandit;
            case nameof(TacticCannaeEnvelopment):
                return !isBandit
                    && infantryRatio > 0.28f
                    && infantryFormations >= 1
                    && mountedRatio > 0.1f;
            case nameof(TacticRefusedFlank):
                return !isBandit
                    && infantryRatio > 0.28f
                    && infantryFormations >= 1;
            case nameof(TacticFeignedRetreat):
                return !isBandit
                    && (team.QuerySystem.RangedCavalryRatio > 0.05f || mountedRatio > 0.16f);
            case nameof(TacticBaitAndPounce):
                return !isBandit
                    && infantryRatio > 0.2f
                    && rangedRatio > 0.06f
                    && mountedRatio > 0.08f;
            case nameof(TacticAntiCavalryBrace):
                return !isBandit
                    && infantryRatio > 0.28f
                    && infantryUnits >= 20
                    && enemyMountedRatio > 0.16f;
            case nameof(TacticObliqueOrder):
                return !isBandit
                    && infantryRatio > 0.24f
                    && infantryFormations >= 1;
            case nameof(TacticHammerAndAnvil):
                return !isBandit
                    && (infantryRatio + rangedRatio) > 0.18f
                    && mountedRatio > 0.08f
                    && composition.HasMountedFormation;
            case nameof(TacticShieldwallAdvance):
                return !isBandit
                    && infantryRatio > 0.3f
                    && infantryUnits >= 20;
            case nameof(TacticInfantryWaves):
                return !isBandit
                    && infantryRatio > 0.4f
                    && infantryUnits >= 24;
            case nameof(TacticMissileScreen):
                return !isBandit
                    && rangedRatio > 0.16f
                    && infantryRatio > 0.12f
                    && rangedUnits >= 18;
            case nameof(TacticElasticDefense):
                return !isBandit && CountInfantryFormations(team) >= 1;
            case nameof(TacticReserveCounterattack):
                return !isBandit
                    && infantryRatio > 0.22f
                    && infantryFormations >= 1;
            default:
                return false;
        }
    }

    private static bool HasMountedFormation(Team team)
    {
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits <= 0)
            {
                continue;
            }

            if (formation.QuerySystem.IsCavalryFormation || formation.QuerySystem.IsRangedCavalryFormation)
            {
                return true;
            }
        }

        return false;
    }

    private static DoctrineEvaluation EvaluateDoctrine(BattleAIDoctrine doctrine, Team team, int tacticsSkill, bool isBandit, BattleAITerrainAssessment terrain, string cultureId, SergeantDoctrineAdvice commanderAdvice, bool manualRequest)
    {
        if (tacticsSkill < doctrine.MinTacticsSkill)
        {
            return new DoctrineEvaluation(false, 0f, "blocked", $"tactics={tacticsSkill} below required={doctrine.MinTacticsSkill}");
        }

        bool eligible = doctrine.IsEligible(team, isBandit);
        bool manualViable = manualRequest && IsManualDoctrineViable(doctrine.Id, team, isBandit);
        if (!eligible && !manualViable)
        {
            return new DoctrineEvaluation(false, 0f, "blocked", DescribeEligibilityFailure(doctrine.Id, team, isBandit));
        }

        float score = doctrine.Score(team, tacticsSkill, isBandit);
        score = ApplyTerrainBias(doctrine.Id, team, score, terrain);
        score = ApplyOpeningBias(doctrine.Id, team, score, cultureId);
        score = ApplyCommanderBias(doctrine.Id, team, score, cultureId);
        score = commanderAdvice.ApplyDoctrineBias(doctrine.Id, score);
        AdaptiveMemoryBias memoryBias = BattleAIAdaptiveMemory.EvaluateDoctrineBias(team, doctrine.Id);
        score += memoryBias.ScoreBonus;
        if (score <= 0f && !manualRequest)
        {
            return new DoctrineEvaluation(false, score, "blocked", "score <= 0");
        }

        string status = eligible ? "eligible" : "manual_override";
        string summary = DescribeTeamSummary(team, terrain);
        string reasonCore = eligible ? summary : $"manual viability passed | {summary}";
        string reason = $"{reasonCore} commander[{DescribeCommanderBias(team, cultureId, doctrine.Id)}] commanderProfile[{commanderAdvice.Describe(doctrine.Id)}] memory[{memoryBias.Description}]";
        return new DoctrineEvaluation(true, score, status, reason);
    }

    private static float ApplyTerrainBias(string doctrineId, Team team, float score, BattleAITerrainAssessment terrain)
    {
        if (terrain.HasStrongUphillAdvantage)
        {
            return doctrineId switch
            {
                nameof(TacticShieldwallAdvance) => score + 0.2f,
                nameof(TacticElasticDefense) => score + 0.18f,
                nameof(TacticReserveCounterattack) => score + 0.16f,
                nameof(TacticRefusedFlank) => score + 0.08f,
                nameof(TacticHammerAndAnvil) => score - 0.08f,
                nameof(TacticFeignedRetreat) => score - 0.06f,
                _ => score
            };
        }

        if (terrain.HasUphillAdvantage)
        {
            return doctrineId switch
            {
                nameof(TacticShieldwallAdvance) => score + 0.12f,
                nameof(TacticElasticDefense) => score + 0.1f,
                nameof(TacticReserveCounterattack) => score + 0.08f,
                nameof(TacticHammerAndAnvil) => score - 0.04f,
                _ => score
            };
        }

        if (terrain.HasDownhillDisadvantage)
        {
            float mountedRatio = team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
            return doctrineId switch
            {
                nameof(TacticHammerAndAnvil) when mountedRatio > 0.12f => score + 0.08f,
                nameof(TacticObliqueOrder) => score + 0.06f,
                nameof(TacticFeignedRetreat) => score + 0.05f,
                nameof(TacticShieldwallAdvance) => score - 0.1f,
                nameof(TacticElasticDefense) => score - 0.05f,
                _ => score
            };
        }

        return score;
    }

    private static float ApplyOpeningBias(string doctrineId, Team team, float score, string cultureId)
    {
        if (!IsOpeningWindowActive())
        {
            return score;
        }

        string openingDoctrineId = GetOpeningDoctrineId(team, cultureId) ?? string.Empty;
        if (openingDoctrineId.Length == 0)
        {
            return score;
        }

        if (string.Equals(openingDoctrineId, doctrineId, StringComparison.Ordinal))
        {
            return score + 0.22f;
        }

        if (IsOpeningCompatible(openingDoctrineId, doctrineId))
        {
            return score + 0.08f;
        }

        return score;
    }

    private static bool IsOpeningWindowActive()
    {
        return Mission.Current?.CurrentTime <= 45f;
    }

    private static string? GetOpeningDoctrineId(Team team, string cultureId)
    {
        if (BattleAICombatantHelper.IsBanditTeam(team))
        {
            return nameof(TacticBanditAdaptiveSkirmish);
        }

        string normalizedCultureId = cultureId.ToLowerInvariant();
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
        float mountedRatio = composition.MountedRatio;
        float rangedRatio = composition.RangedRatio;
        float horseArcherRatio = composition.RangedCavalryRatio;

        if (normalizedCultureId.Contains("empire"))
        {
            return mountedRatio > 0.12f ? nameof(TacticHammerAndAnvil) : nameof(TacticElasticDefense);
        }

        if (normalizedCultureId.Contains("sturgia") || normalizedCultureId.Contains("nord"))
        {
            return nameof(TacticShieldwallAdvance);
        }

        if (normalizedCultureId.Contains("vlandia"))
        {
            return nameof(TacticHammerAndAnvil);
        }

        if (normalizedCultureId.Contains("khuzait"))
        {
            return horseArcherRatio > 0.08f ? nameof(TacticFeignedRetreat) : nameof(TacticHammerAndAnvil);
        }

        if (normalizedCultureId.Contains("battania"))
        {
            return rangedRatio > 0.16f ? nameof(TacticObliqueOrder) : nameof(TacticShieldwallAdvance);
        }

        if (normalizedCultureId.Contains("aserai") || normalizedCultureId.Contains("darshi"))
        {
            return horseArcherRatio > 0.08f ? nameof(TacticFeignedRetreat) : nameof(TacticObliqueOrder);
        }

        if (team.Side == BattleSideEnum.Defender)
        {
            return nameof(TacticElasticDefense);
        }

        return mountedRatio > 0.12f ? nameof(TacticHammerAndAnvil) : nameof(TacticShieldwallAdvance);
    }

    private static bool IsOpeningCompatible(string openingDoctrineId, string doctrineId)
    {
        if (string.Equals(openingDoctrineId, doctrineId, StringComparison.Ordinal))
        {
            return true;
        }

        return openingDoctrineId switch
        {
            nameof(TacticHammerAndAnvil) => doctrineId is nameof(TacticReserveCounterattack) or nameof(TacticObliqueOrder) or nameof(TacticCannaeEnvelopment),
            nameof(TacticElasticDefense) => doctrineId is nameof(TacticShieldwallAdvance) or nameof(TacticReserveCounterattack) or nameof(TacticRefusedFlank) or nameof(TacticInfantryWaves) or nameof(TacticBaitAndPounce) or nameof(TacticMissileScreen) or nameof(TacticAntiCavalryBrace),
            nameof(TacticShieldwallAdvance) => doctrineId is nameof(TacticElasticDefense) or nameof(TacticReserveCounterattack) or nameof(TacticRefusedFlank) or nameof(TacticInfantryWaves) or nameof(TacticMissileScreen),
            nameof(TacticFeignedRetreat) => doctrineId is nameof(TacticObliqueOrder) or nameof(TacticHammerAndAnvil) or nameof(TacticBaitAndPounce),
            nameof(TacticObliqueOrder) => doctrineId is nameof(TacticHammerAndAnvil) or nameof(TacticRefusedFlank),
            nameof(TacticBanditAdaptiveSkirmish) => doctrineId is nameof(TacticElasticDefense) or nameof(TacticFeignedRetreat),
            _ => false
        };
    }

    private static float ApplyCommanderBias(string doctrineId, Team team, float score, string cultureId)
    {
        string[] preferences = GetCommanderPreferredDoctrines(team, cultureId);
        if (preferences.Length == 0)
        {
            return score;
        }

        if (string.Equals(preferences[0], doctrineId, StringComparison.Ordinal))
        {
            return score + 0.12f;
        }

        if (preferences.Length > 1 && string.Equals(preferences[1], doctrineId, StringComparison.Ordinal))
        {
            return score + 0.06f;
        }

        if (preferences.Length > 2 && string.Equals(preferences[2], doctrineId, StringComparison.Ordinal))
        {
            return score + 0.03f;
        }

        return score;
    }

    private static string DescribeCommanderBias(Team team, string cultureId, string doctrineId)
    {
        string[] preferences = GetCommanderPreferredDoctrines(team, cultureId);
        if (preferences.Length == 0)
        {
            return "none";
        }

        string rank = string.Equals(preferences[0], doctrineId, StringComparison.Ordinal)
            ? "primary"
            : preferences.Length > 1 && string.Equals(preferences[1], doctrineId, StringComparison.Ordinal)
                ? "secondary"
                : preferences.Length > 2 && string.Equals(preferences[2], doctrineId, StringComparison.Ordinal)
                    ? "tertiary"
                    : "none";
        return $"{rank}:{string.Join(">", preferences)}";
    }

    private static string[] GetCommanderPreferredDoctrines(Team team, string cultureId)
    {
        string[] pool = GetCommanderPreferencePool(team, cultureId);
        if (pool.Length == 0)
        {
            return Array.Empty<string>();
        }

        uint hash = ComputeStableHash(BattleAICombatantHelper.GetCommanderIdentityKey(team));
        int primaryIndex = (int)(hash % (uint)pool.Length);
        int secondaryIndex = (primaryIndex + 1 + (int)((hash / (uint)pool.Length) % (uint)Math.Max(1, pool.Length - 1))) % pool.Length;
        if (secondaryIndex == primaryIndex)
        {
            secondaryIndex = (primaryIndex + 1) % pool.Length;
        }

        int tertiaryIndex = (secondaryIndex + 1 + (int)((hash / 17u) % (uint)Math.Max(1, pool.Length - 1))) % pool.Length;
        while (tertiaryIndex == primaryIndex || tertiaryIndex == secondaryIndex)
        {
            tertiaryIndex = (tertiaryIndex + 1) % pool.Length;
        }

        if (pool.Length == 1)
        {
            return new[] { pool[primaryIndex] };
        }

        if (pool.Length == 2)
        {
            return new[] { pool[primaryIndex], pool[secondaryIndex] };
        }

        return new[] { pool[primaryIndex], pool[secondaryIndex], pool[tertiaryIndex] };
    }

    private static string[] GetCommanderPreferencePool(Team team, string cultureId)
    {
        string normalizedCultureId = cultureId.ToLowerInvariant();
        if (normalizedCultureId.Contains("empire"))
        {
            return new[] { nameof(TacticHammerAndAnvil), nameof(TacticReserveCounterattack), nameof(TacticCannaeEnvelopment), nameof(TacticShieldwallAdvance) };
        }

        if (normalizedCultureId.Contains("sturgia") || normalizedCultureId.Contains("nord"))
        {
            return new[] { nameof(TacticShieldwallAdvance), nameof(TacticAntiCavalryBrace), nameof(TacticElasticDefense), nameof(TacticInfantryWaves) };
        }

        if (normalizedCultureId.Contains("vlandia"))
        {
            return new[] { nameof(TacticHammerAndAnvil), nameof(TacticShieldwallAdvance), nameof(TacticObliqueOrder), nameof(TacticReserveCounterattack) };
        }

        if (normalizedCultureId.Contains("khuzait"))
        {
            return new[] { nameof(TacticFeignedRetreat), nameof(TacticHammerAndAnvil), nameof(TacticBaitAndPounce), nameof(TacticElasticDefense) };
        }

        if (normalizedCultureId.Contains("battania"))
        {
            return new[] { nameof(TacticObliqueOrder), nameof(TacticMissileScreen), nameof(TacticRefusedFlank), nameof(TacticElasticDefense) };
        }

        if (normalizedCultureId.Contains("aserai") || normalizedCultureId.Contains("darshi"))
        {
            return new[] { nameof(TacticObliqueOrder), nameof(TacticFeignedRetreat), nameof(TacticBaitAndPounce), nameof(TacticHammerAndAnvil) };
        }

        if (BattleAICombatantHelper.IsBanditTeam(team: team))
        {
            return new[] { nameof(TacticBanditAdaptiveSkirmish), nameof(TacticElasticDefense), nameof(TacticFeignedRetreat) };
        }

        return new[] { nameof(TacticElasticDefense), nameof(TacticShieldwallAdvance), nameof(TacticHammerAndAnvil), nameof(TacticReserveCounterattack) };
    }

    private static uint ComputeStableHash(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0u;
        }

        uint hash = 2166136261u;
        foreach (char c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }

    private static string DescribeEligibilityFailure(string doctrineId, Team team, bool isBandit)
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
        float infantryRatio = composition.InfantryRatio;
        float rangedRatio = composition.RangedRatio;
        float cavalryRatio = composition.MountedRatio;
        int infantryFormations = composition.InfantryFormationCount;
        int infantryUnits = CountInfantryUnits(team);
        int rangedUnits = CountRangedUnits(team);
        bool hasMounted = composition.HasMountedFormation;

        return doctrineId switch
        {
            nameof(TacticBanditAdaptiveSkirmish) => isBandit ? "bandit requirements unexpectedly failed" : "team is not bandit",
            nameof(TacticCannaeEnvelopment) => $"needs non-bandit, inf>0.40, 2 infantry formations, cav>0.14 | got inf={infantryRatio:F2} infForms={infantryFormations} cav={cavalryRatio:F2} bandit={isBandit}",
            nameof(TacticRefusedFlank) => $"needs non-bandit, inf>0.40, 2 infantry formations | got inf={infantryRatio:F2} infForms={infantryFormations} bandit={isBandit}",
            nameof(TacticFeignedRetreat) => $"needs non-bandit and horse-archers>0.08 or cav>0.22 | got cav={cavalryRatio:F2} horseArch={team.QuerySystem.RangedCavalryRatio:F2} bandit={isBandit}",
            nameof(TacticBaitAndPounce) => $"needs non-bandit, inf>0.30, arch>0.08, mounted>0.12 | got inf={infantryRatio:F2} arch={rangedRatio:F2} mounted={cavalryRatio:F2} bandit={isBandit}",
            nameof(TacticAntiCavalryBrace) => $"needs non-bandit, inf>0.35, infantry units>=30, enemy mounted>0.22 | got inf={infantryRatio:F2} infUnits={infantryUnits} enemyMounted={team.QuerySystem.EnemyCavalryRatio + team.QuerySystem.EnemyRangedCavalryRatio:F2} bandit={isBandit}",
            nameof(TacticObliqueOrder) => $"needs non-bandit, inf>0.35, 2 infantry formations | got inf={infantryRatio:F2} infForms={infantryFormations} bandit={isBandit}",
            nameof(TacticHammerAndAnvil) => $"needs non-bandit, inf>0.28, cav>0.12 | got inf={infantryRatio:F2} cav={cavalryRatio:F2} mountedFormation={hasMounted} bandit={isBandit}",
            nameof(TacticShieldwallAdvance) => $"needs non-bandit, inf>0.45, arch>0.08 | got inf={infantryRatio:F2} arch={rangedRatio:F2} bandit={isBandit}",
            nameof(TacticInfantryWaves) => $"needs non-bandit, inf>0.50, infantry units>=35 | got inf={infantryRatio:F2} infUnits={infantryUnits} infForms={infantryFormations} bandit={isBandit}",
            nameof(TacticMissileScreen) => $"needs non-bandit, arch>0.22, inf>0.24, ranged units>=25 | got arch={rangedRatio:F2} rangedUnits={rangedUnits} inf={infantryRatio:F2} bandit={isBandit}",
            nameof(TacticElasticDefense) => isBandit ? "bandit teams do not use ElasticDefense" : "unexpected eligibility failure",
            nameof(TacticReserveCounterattack) => $"needs non-bandit and 2 infantry formations | got infForms={infantryFormations} bandit={isBandit}",
            _ => DescribeTeamSummary(team, BattleAITerrainAnalyzer.Analyze(team))
        };
    }

    private static string DescribeTeamContext(Team team, int tacticsSkill, bool isBandit, BattleAITerrainAssessment terrain, string cultureId, SergeantDoctrineAdvice commanderAdvice)
    {
        string openingDoctrineId = GetOpeningDoctrineId(team, cultureId) ?? "none";
        return $"team={(team.IsPlayerTeam ? "player" : "enemy")} side={team.Side} bandit={isBandit} tactics={tacticsSkill} culture={cultureId} opening={openingDoctrineId} commanderProfile={commanderAdvice.DescribeSummary()} {DescribeTeamSummary(team, terrain)}";
    }

    private static string DescribeTeamSummary(Team team, BattleAITerrainAssessment terrain)
    {
        BattleAIFormationComposition composition = BattleAIFormationCompositionHelper.FromTeam(team);
        return $"power={team.QuerySystem.RemainingPowerRatio:F2} inf={composition.InfantryRatio:F2} arch={composition.RangedRatio:F2} cav={composition.CavalryRatio:F2} horseArch={composition.RangedCavalryRatio:F2} enemyCav={composition.EnemyMountedRatio:F2} infForms={composition.InfantryFormationCount} mounted={composition.HasMountedFormation} terrain={terrain.Describe()}";
    }
}

internal sealed class DoctrineEvaluation
{
    public DoctrineEvaluation(bool isSelectable, float score, string status, string reason)
    {
        IsSelectable = isSelectable;
        Score = score;
        Status = status;
        Reason = reason;
    }

    public bool IsSelectable { get; }

    public float Score { get; }

    public string Status { get; }

    public string Reason { get; }
}

internal sealed class BattleAIDoctrine
{
    public BattleAIDoctrine(
        string id,
        int minTacticsSkill,
        Func<Team, bool, bool> isEligible,
        Func<Team, int, bool, float> score,
        Func<Team, TacticComponent> create)
    {
        Id = id;
        MinTacticsSkill = minTacticsSkill;
        IsEligible = isEligible;
        Score = score;
        Create = create;
    }

    public string Id { get; }

    public int MinTacticsSkill { get; }

    public Func<Team, bool, bool> IsEligible { get; }

    public Func<Team, int, bool, float> Score { get; }

    public Func<Team, TacticComponent> Create { get; }
}

internal sealed class BattleAIDoctrineSelection
{
    public BattleAIDoctrineSelection(string doctrineId, int commanderTacticsSkill, float score, TacticComponent tactic)
    {
        DoctrineId = doctrineId;
        CommanderTacticsSkill = commanderTacticsSkill;
        Score = score;
        Tactic = tactic;
    }

    public string DoctrineId { get; }

    public int CommanderTacticsSkill { get; }

    public float Score { get; }

    public TacticComponent Tactic { get; }
}
