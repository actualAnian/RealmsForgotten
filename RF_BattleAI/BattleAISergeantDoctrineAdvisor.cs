using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAISergeantDoctrineAdvisor
{
    public static SergeantDoctrineAdvice AnalyzeCommander(Team? team)
    {
        Hero? hero = BattleAICombatantHelper.GetCommanderHero(team);
        if (!BattleAICombatantHelper.IsPlayerClanCompanionPartyLeader(hero))
        {
            return SergeantDoctrineAdvice.None;
        }

        Formation? anchorFormation = GetLargestFormation(team);
        if (hero == null || anchorFormation == null)
        {
            return SergeantDoctrineAdvice.None;
        }

        return new SergeantDoctrineAdvice(new[]
        {
            CreateProfile(hero, anchorFormation, anchorFormation.CountOfUnits, 1f)
        });
    }

    public static SergeantDoctrineAdvice Analyze(Team? team)
    {
        if (team == null || !team.IsPlayerTeam)
        {
            return SergeantDoctrineAdvice.None;
        }

        List<SergeantProfile> profiles = new();
        int totalUnits = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits > 0)
            {
                totalUnits += formation.CountOfUnits;
            }
        }

        if (totalUnits <= 0)
        {
            return SergeantDoctrineAdvice.None;
        }

        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits <= 0)
            {
                continue;
            }

            Hero? hero = BattleAICombatantHelper.GetFormationCaptainHero(formation);
            if (hero == null)
            {
                continue;
            }

            profiles.Add(CreateProfile(hero, formation, totalUnits));
        }

        return profiles.Count == 0 ? SergeantDoctrineAdvice.None : new SergeantDoctrineAdvice(profiles);
    }

    public static SergeantProfile? AnalyzeAnyFormationCaptain(Formation? formation)
    {
        if (formation == null || formation.CountOfUnits <= 0)
        {
            return null;
        }

        Hero? hero = BattleAICombatantHelper.GetAnyFormationCaptainHero(formation);
        if (hero == null)
        {
            return null;
        }

        return CreateProfile(hero, formation, formation.CountOfUnits, 1f);
    }

    private static SergeantProfile CreateProfile(Hero hero, Formation formation, int totalUnits)
    {
        float unitShare = formation.CountOfUnits / (float)Math.Max(1, totalUnits);
        float influence = (float)Math.Min(0.8, 0.35 + unitShare);
        return CreateProfile(hero, formation, totalUnits, influence);
    }

    private static SergeantProfile CreateProfile(Hero hero, Formation formation, int totalUnits, float influence)
    {
        string role = InferRole(hero, formation);
        string[] preferences = GetRolePreferences(role, formation);
        int tacticsSkill = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Tactics);

        return new SergeantProfile(
            hero.Name?.ToString() ?? hero.StringId,
            role,
            tacticsSkill,
            preferences,
            influence,
            DescribeFormationClass(formation));
    }

    private static string InferRole(Hero hero, Formation formation)
    {
        int tactics = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Tactics);
        int leadership = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Leadership);
        int steward = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Steward);
        int scouting = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Scouting);
        int roguery = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Roguery);
        int riding = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Riding);
        int polearm = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Polearm);
        int oneHanded = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.OneHanded);
        int twoHanded = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.TwoHanded);
        int athletics = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Athletics);
        int bow = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Bow);
        int crossbow = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Crossbow);
        int throwing = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Throwing);
        int engineering = BattleAICombatantHelper.GetHeroSkill(hero, DefaultSkills.Engineering);

        int infantryScore = Math.Max(oneHanded, twoHanded) + polearm / 2 + athletics + (formation.QuerySystem.IsInfantryFormation ? 40 : 0);
        int archerScore = Math.Max(bow, Math.Max(crossbow, throwing)) + tactics / 3 + (formation.QuerySystem.IsRangedFormation ? 45 : 0);
        int cavalryScore = riding + polearm + tactics / 2 + (formation.QuerySystem.IsCavalryFormation ? 50 : 0);
        int horseArcherScore = riding + Math.Max(bow, throwing) + scouting / 2 + roguery / 2 + (formation.QuerySystem.IsRangedCavalryFormation ? 55 : 0);
        int tacticianScore = tactics + leadership + steward / 2 + engineering / 2;
        int raiderScore = scouting + roguery + riding / 2 + tactics / 3 + (formation.QuerySystem.IsRangedCavalryFormation ? 25 : 0);

        string bestRole = "line_infantry";
        int bestScore = infantryScore;

        Consider("archer_captain", archerScore);
        Consider("cavalry_shock", cavalryScore);
        Consider("horse_archer_raider", horseArcherScore);
        Consider("tactician", tacticianScore);
        Consider("raider", raiderScore);

        return bestRole;

        void Consider(string role, int score)
        {
            if (score > bestScore)
            {
                bestScore = score;
                bestRole = role;
            }
        }
    }

    private static string[] GetRolePreferences(string role, Formation formation)
    {
        return role switch
        {
            "archer_captain" => new[] { nameof(FieldBattle.Tactics.TacticMissileScreen), nameof(FieldBattle.Tactics.TacticElasticDefense), nameof(FieldBattle.Tactics.TacticObliqueOrder) },
            "cavalry_shock" => new[] { nameof(FieldBattle.Tactics.TacticHammerAndAnvil), nameof(FieldBattle.Tactics.TacticReserveCounterattack), nameof(FieldBattle.Tactics.TacticObliqueOrder) },
            "horse_archer_raider" => new[] { nameof(FieldBattle.Tactics.TacticFeignedRetreat), nameof(FieldBattle.Tactics.TacticBaitAndPounce), nameof(FieldBattle.Tactics.TacticHammerAndAnvil) },
            "tactician" => new[] { nameof(FieldBattle.Tactics.TacticReserveCounterattack), nameof(FieldBattle.Tactics.TacticElasticDefense), nameof(FieldBattle.Tactics.TacticRefusedFlank) },
            "raider" => new[] { nameof(FieldBattle.Tactics.TacticBaitAndPounce), nameof(FieldBattle.Tactics.TacticFeignedRetreat), nameof(FieldBattle.Tactics.TacticObliqueOrder) },
            _ when formation.QuerySystem.IsInfantryFormation => new[] { nameof(FieldBattle.Tactics.TacticShieldwallAdvance), nameof(FieldBattle.Tactics.TacticInfantryWaves), nameof(FieldBattle.Tactics.TacticAntiCavalryBrace) },
            _ => new[] { nameof(FieldBattle.Tactics.TacticElasticDefense), nameof(FieldBattle.Tactics.TacticShieldwallAdvance), nameof(FieldBattle.Tactics.TacticReserveCounterattack) }
        };
    }

    private static string DescribeFormationClass(Formation formation)
    {
        if (formation.QuerySystem.IsRangedCavalryFormation)
        {
            return "horse_archer";
        }

        if (formation.QuerySystem.IsCavalryFormation)
        {
            return "cavalry";
        }

        if (formation.QuerySystem.IsRangedFormation)
        {
            return "ranged";
        }

        if (formation.QuerySystem.IsInfantryFormation)
        {
            return "infantry";
        }

        return "other";
    }

    private static Formation? GetLargestFormation(Team? team)
    {
        if (team == null)
        {
            return null;
        }

        Formation? best = null;
        int bestCount = 0;
        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation.CountOfUnits <= bestCount)
            {
                continue;
            }

            best = formation;
            bestCount = formation.CountOfUnits;
        }

        return best;
    }
}

internal sealed class SergeantDoctrineAdvice
{
    public static readonly SergeantDoctrineAdvice None = new(Array.Empty<SergeantProfile>());

    public SergeantDoctrineAdvice(IReadOnlyList<SergeantProfile> profiles)
    {
        Profiles = profiles;
    }

    public IReadOnlyList<SergeantProfile> Profiles { get; }

    public bool HasProfiles => Profiles.Count > 0;

    public int GetEffectiveTacticsSkill(int commanderSkill)
    {
        int effective = commanderSkill;
        foreach (SergeantProfile profile in Profiles)
        {
            if (profile.TacticsSkill > effective)
            {
                effective = profile.TacticsSkill;
            }
        }

        return effective;
    }

    public float ApplyDoctrineBias(string doctrineId, float score)
    {
        float bias = 0f;
        foreach (SergeantProfile profile in Profiles)
        {
            if (profile.Preferences.Length > 0 && string.Equals(profile.Preferences[0], doctrineId, StringComparison.Ordinal))
            {
                bias += 0.18f * profile.Influence;
                continue;
            }

            if (profile.Preferences.Length > 1 && string.Equals(profile.Preferences[1], doctrineId, StringComparison.Ordinal))
            {
                bias += 0.09f * profile.Influence;
                continue;
            }

            if (profile.Preferences.Length > 2 && string.Equals(profile.Preferences[2], doctrineId, StringComparison.Ordinal))
            {
                bias += 0.045f * profile.Influence;
            }
        }

        return score + (float)Math.Min(0.3, bias);
    }

    public string Describe(string doctrineId)
    {
        if (!HasProfiles)
        {
            return "none";
        }

        List<string> parts = new();
        foreach (SergeantProfile profile in Profiles)
        {
            string rank = profile.Preferences.Length > 0 && string.Equals(profile.Preferences[0], doctrineId, StringComparison.Ordinal)
                ? "primary"
                : profile.Preferences.Length > 1 && string.Equals(profile.Preferences[1], doctrineId, StringComparison.Ordinal)
                    ? "secondary"
                    : profile.Preferences.Length > 2 && string.Equals(profile.Preferences[2], doctrineId, StringComparison.Ordinal)
                        ? "tertiary"
                        : "none";
            parts.Add($"{profile.Name}:{profile.FormationClass}:{profile.Role}:{rank}");
        }

        return string.Join(" | ", parts);
    }

    public string DescribeSummary()
    {
        if (!HasProfiles)
        {
            return "none";
        }

        List<string> parts = new();
        foreach (SergeantProfile profile in Profiles)
        {
            parts.Add($"{profile.Name}:{profile.FormationClass}:{profile.Role}:t{profile.TacticsSkill}");
        }

        return string.Join(" | ", parts);
    }
}

internal sealed class SergeantProfile
{
    public SergeantProfile(string name, string role, int tacticsSkill, string[] preferences, float influence, string formationClass)
    {
        Name = name;
        Role = role;
        TacticsSkill = tacticsSkill;
        Preferences = preferences;
        Influence = influence;
        FormationClass = formationClass;
    }

    public string Name { get; }

    public string Role { get; }

    public int TacticsSkill { get; }

    public string[] Preferences { get; }

    public float Influence { get; }

    public string FormationClass { get; }
}
