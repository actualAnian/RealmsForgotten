using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_BattleAI;

internal static class BattleAICombatantHelper
{
    public static int GetCommanderTacticsSkill(Team? team)
    {
        return GetCharacterTacticsSkill(team?.GeneralAgent?.Character);
    }

    public static int GetCharacterTacticsSkill(object? character)
    {
        object? tacticsSkill = GetDefaultTacticsSkillObject();
        if (character == null || tacticsSkill == null)
        {
            return 0;
        }

        MethodInfo? getSkillValue = character.GetType().GetMethod("GetSkillValue", BindingFlags.Instance | BindingFlags.Public);
        if (getSkillValue == null)
        {
            return 0;
        }

        object? result = getSkillValue.Invoke(character, new[] { tacticsSkill });
        return result is int skillValue ? skillValue : 0;
    }

    public static bool IsBanditTeam(Team? team)
    {
        if (team == null || Mission.Current == null)
        {
            return false;
        }

        MissionCombatantsLogic? missionCombatants = Mission.Current.GetMissionBehavior<MissionCombatantsLogic>();
        if (missionCombatants != null)
        {
            foreach (object combatant in missionCombatants.GetAllCombatants())
            {
                object? sideValue = ReadProperty(combatant, "Side");
                if (sideValue?.ToString() != team.Side.ToString())
                {
                    continue;
                }

                if (IsBanditCombatant(combatant))
                {
                    return true;
                }
            }
        }

        return IsBanditCultureTeam(team);
    }

    public static string GetTeamCultureId(Team? team)
    {
        if (team?.GeneralAgent?.Character?.Culture?.StringId is { Length: > 0 } cultureId)
        {
            return cultureId;
        }

        object? basicCulture = ReadProperty(ReadProperty(team?.GeneralAgent?.Origin, "BattleCombatant"), "BasicCulture");
        return ReadProperty(basicCulture, "StringId") as string ?? string.Empty;
    }

    public static string GetCommanderIdentityKey(Team? team)
    {
        return GetCharacterIdentityKey(team?.GeneralAgent?.Character);
    }

    public static Hero? GetCommanderHero(Team? team)
    {
        return team?.GeneralAgent?.Character is CharacterObject characterObject
            ? characterObject.HeroObject
            : null;
    }

    public static string GetCharacterIdentityKey(object? character)
    {
        if (character == null)
        {
            return string.Empty;
        }

        if (ReadProperty(character, "StringId") as string is { Length: > 0 } stringId)
        {
            return stringId;
        }

        object? name = ReadProperty(character, "Name");
        if (name != null)
        {
            string text = name.ToString() ?? string.Empty;
            if (text.Length > 0)
            {
                return text;
            }
        }

        return character.GetType().FullName ?? string.Empty;
    }

    public static Hero? GetFormationCaptainHero(Formation? formation)
    {
        if (formation?.Captain?.Character is not CharacterObject characterObject)
        {
            return null;
        }

        Hero? hero = characterObject.HeroObject;
        if (hero == null)
        {
            return null;
        }

        if (hero.Occupation != Occupation.Wanderer)
        {
            return null;
        }

        if (hero.Clan != Clan.PlayerClan)
        {
            return null;
        }

        return hero;
    }

    public static Hero? GetAnyFormationCaptainHero(Formation? formation)
    {
        return formation?.Captain?.Character is CharacterObject characterObject
            ? characterObject.HeroObject
            : null;
    }

    public static bool IsPlayerClanCompanionPartyLeader(Hero? hero)
    {
        if (hero == null)
        {
            return false;
        }

        if (hero.Clan != Clan.PlayerClan)
        {
            return false;
        }

        if (hero.Occupation != Occupation.Wanderer)
        {
            return false;
        }

        return hero.PartyBelongedTo?.LeaderHero == hero;
    }

    public static int GetHeroSkill(Hero? hero, SkillObject skill)
    {
        if (hero == null)
        {
            return 0;
        }

        try
        {
            return hero.GetSkillValue(skill);
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsBanditCombatant(object combatant)
    {
        return HasBooleanProperty(combatant, "IsBandit")
            || IsBanditFaction(ReadProperty(combatant, "MapFaction"))
            || IsBanditFaction(ReadProperty(ReadProperty(combatant, "Party"), "MapFaction"))
            || IsBanditFaction(ReadProperty(ReadProperty(combatant, "MobileParty"), "MapFaction"))
            || IsBanditFaction(ReadProperty(ReadProperty(combatant, "LeaderParty"), "MapFaction"))
            || IsBanditCulture(ReadProperty(combatant, "Culture"))
            || IsBanditCulture(ReadProperty(combatant, "BasicCulture"));
    }

    private static bool IsBanditCultureTeam(Team team)
    {
        if (team.GeneralAgent?.Character?.Culture?.IsBandit == true)
        {
            return true;
        }

        foreach (Formation formation in team.FormationsIncludingEmpty)
        {
            if (formation?.Captain?.Character?.Culture?.IsBandit == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBanditFaction(object? faction)
    {
        return faction != null && HasBooleanProperty(faction, "IsBanditFaction");
    }

    private static bool IsBanditCulture(object? culture)
    {
        return culture != null && HasBooleanProperty(culture, "IsBandit");
    }

    private static bool HasBooleanProperty(object target, string propertyName)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.PropertyType == typeof(bool) && property.GetValue(target) as bool? == true;
    }

    private static object? ReadProperty(object? target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(target);
    }

    private static object? GetDefaultTacticsSkillObject()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            object? tacticsSkill = GetTacticsSkillFromType(assembly, "TaleWorlds.Core.DefaultSkills");
            if (tacticsSkill != null)
            {
                return tacticsSkill;
            }

            tacticsSkill = GetTacticsSkillFromType(assembly, "TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultSkills");
            if (tacticsSkill != null)
            {
                return tacticsSkill;
            }
        }

        return null;
    }

    private static object? GetTacticsSkillFromType(Assembly assembly, string typeName)
    {
        Type? defaultSkillsType = assembly.GetType(typeName);
        if (defaultSkillsType == null)
        {
            return null;
        }

        PropertyInfo? tacticsProperty = defaultSkillsType.GetProperty("Tactics", BindingFlags.Static | BindingFlags.Public);
        if (tacticsProperty != null)
        {
            return tacticsProperty.GetValue(null);
        }

        FieldInfo? tacticsField = defaultSkillsType.GetField("Tactics", BindingFlags.Static | BindingFlags.Public);
        if (tacticsField != null)
        {
            return tacticsField.GetValue(null);
        }

        return null;
    }
}
