using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.KingdomObjectives;

public static class KingdomObjectiveService
{
    /// <summary>0..100 wealth flowing from resource zones (gold/silver mines)
    /// owned by the kingdom's clans. Installed VIA REFLECTION by
    /// RF_ResourceZones on session launch — the zones system is an optional
    /// add-on, so no assembly reference exists. Null = zones absent.</summary>
    public static Func<Kingdom, float>? ZoneWealthScoreProvider;

    /// <summary>0..100 share of the map's mountain mines (iron/gold/silver
    /// zones) under the kingdom's control. Same installation contract.</summary>
    public static Func<Kingdom, float>? MineControlScoreProvider;

    private static float GetProviderScore(Func<Kingdom, float>? provider, Kingdom kingdom)
    {
        if (provider == null || kingdom == null)
        {
            return 0f;
        }

        try
        {
            return Clamp01Score(provider(kingdom));
        }
        catch
        {
            return 0f;
        }
    }

    private static readonly HashSet<string> AseraiRealmIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "aserai",
        "aserai_a",
        "aserai_b",
        "aserai_c",
        "aserai_d",
        "aserai_e"
    };

    private static readonly HashSet<string> ImperialRealmIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "empire",
        "empire_w",
        "empire_s",
        "south_realm",
        "west_realm"
    };

    private static readonly HashSet<string> ImperialCultureIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "empire",
        "south_realm",
        "west_realm"
    };

    private static readonly HashSet<string> KhuzaitTargetRealmIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "sturgia",
        "empire",
        "empire_w",
        "empire_s",
        "south_realm",
        "west_realm"
    };

    public static KingdomObjectiveType ResolveObjective(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return KingdomObjectiveType.None;
        }

        return kingdom.StringId switch
        {
            "sturgia" => KingdomObjectiveType.CrushBattanianResistance,
            "vlandia" => KingdomObjectiveType.NobleWealthSupremacy,
            "battania" => KingdomObjectiveType.PreserveBattanianHomelands,
            "khuzait" => KingdomObjectiveType.ForgeBorderEmpire,
            "dwarf_kingdom" => KingdomObjectiveType.SecureMountainHolds,
            "urkhai_kingdom" => KingdomObjectiveType.DefileMountainHolds,
            "mage_kingdom" => KingdomObjectiveType.ArcaneFrontier,
            "grimwatch_kingdom" => KingdomObjectiveType.UnbreakableRealm,
            "wulf_kingdom" => KingdomObjectiveType.MartialGlory,
            "giant_kingdom" => KingdomObjectiveType.GuardianFrenzy,
            "katogai_kingdom" => KingdomObjectiveType.MercenaryCreed,
            "tharnmar_kingdom" => KingdomObjectiveType.MercenaryCreed,
            "valthorne_kingdom" => KingdomObjectiveType.WovenAlliances,
            "nord_colonies" => KingdomObjectiveType.ColonialExpansion,
            _ => ResolveByCulture(kingdom.Culture?.StringId)
        };
    }

    /// <summary>The settlement cultures a design actually covets, used to tell
    /// the war system WHERE the realm wants to march (RFWarExternalIntentApi
    /// .ReinforceGrandDesignIntent). Empty means "the design names no particular
    /// ground" — either it is not territorial at all, or, like the Nord colonies,
    /// any enemy land will do.</summary>
    public static IReadOnlyList<string> GetCovetedCultureIds(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance
                or KingdomObjectiveType.ArcaneFrontier
                or KingdomObjectiveType.PreserveBattanianHomelands => new[] { "battania" },
            KingdomObjectiveType.SecureMountainHolds => new[] { "urkhai" },
            KingdomObjectiveType.DefileMountainHolds => new[] { "dwarf" },
            KingdomObjectiveType.ForgeBorderEmpire => new[] { "sturgia", "empire", "south_realm", "west_realm" },
            KingdomObjectiveType.UniteAseraiRealms => new[] { "aserai" },
            KingdomObjectiveType.ClaimImperialLegitimacy => new[] { "empire", "south_realm", "west_realm" },
            _ => Array.Empty<string>()
        };
    }

    public static TextObject GetTitle(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            // Player-facing text says Elvean, never "Battanian": battania is the
            // engine slot this realm occupies, not the name of anyone in Aeurth.
            KingdomObjectiveType.CrushBattanianResistance => new TextObject("{=rf_ko_title_sturgia}Break Elvean Resistance"),
            KingdomObjectiveType.NobleWealthSupremacy => new TextObject("{=rf_ko_title_vlandia}Noble Wealth Supremacy"),
            KingdomObjectiveType.PreserveBattanianHomelands => new TextObject("{=rf_ko_title_battania}Preserve the Old Forest Realm"),
            KingdomObjectiveType.UniteAseraiRealms => new TextObject("{=rf_ko_title_aserai}Unite the Desert Realms"),
            KingdomObjectiveType.ClaimImperialLegitimacy => new TextObject("{=rf_ko_title_empire}Claim Sole Imperial Legitimacy"),
            KingdomObjectiveType.ForgeBorderEmpire => new TextObject("{=rf_ko_title_khuzait}Forge a Border Empire"),
            KingdomObjectiveType.ArcaneFrontier => new TextObject("{=rf_ko_title_mage}Seize the Arcane Frontier"),
            KingdomObjectiveType.SecureMountainHolds => new TextObject("{=rf_ko_title_dwarf}Secure the Mountain Holds"),
            KingdomObjectiveType.DefileMountainHolds => new TextObject("{=rf_ko_title_urkhai}Defile the Mountain Holds"),
            KingdomObjectiveType.MartialGlory => new TextObject("{=rf_ko_title_wulf}Win Unrivaled Martial Glory"),
            KingdomObjectiveType.UnbreakableRealm => new TextObject("{=rf_ko_title_grimwatch}Become the Unbreakable Realm"),
            KingdomObjectiveType.GuardianFrenzy => new TextObject("{=rf_ko_title_giant}Guard the Giants' Peace"),
            KingdomObjectiveType.MercenaryCreed => new TextObject("{=rf_ko_title_mercenary}Stand Ready, Owe Nothing"),
            KingdomObjectiveType.WovenAlliances => new TextObject("{=rf_ko_title_valthorne}Weave the Web of Allies"),
            KingdomObjectiveType.ColonialExpansion => new TextObject("{=rf_ko_title_nord}Expand the Colonies"),
            _ => new TextObject("{=rf_ko_title_none}No Grand Design")
        };
    }

    public static TextObject GetFantasy(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance => new TextObject("{=rf_ko_fantasy_sturgia}The court wants the Elveans broken and the old forests bent to Dreadrealm will."),
            KingdomObjectiveType.NobleWealthSupremacy => new TextObject("{=rf_ko_fantasy_vlandia}The realm seeks unmatched wealth, entrenched great houses, and a nobility too rich to challenge."),
            KingdomObjectiveType.PreserveBattanianHomelands => new TextObject("{=rf_ko_fantasy_battania}The Elvean clans mean to hold the old woods, repel invaders, and keep the forest realm alive."),
            KingdomObjectiveType.UniteAseraiRealms => new TextObject("{=rf_ko_fantasy_aserai}The desert crowns are meant to yield until one supremacy rules the sands."),
            KingdomObjectiveType.ClaimImperialLegitimacy => new TextObject("{=rf_ko_fantasy_empire}This court claims there can be only one lawful Empire, and all rival claimants must bend."),
            KingdomObjectiveType.ForgeBorderEmpire => new TextObject("{=rf_ko_fantasy_khuzait}The khanate seeks to swallow the realms on its frontier and turn raids into empire."),
            KingdomObjectiveType.ArcaneFrontier => new TextObject("{=rf_ko_fantasy_mage}The kingdom means to take Elvean lands and refashion them into an arcane frontier."),
            KingdomObjectiveType.SecureMountainHolds => new TextObject("{=rf_ko_fantasy_dwarf}The realm is bent on breaking Urkhai power and making the mountain holds eternal."),
            KingdomObjectiveType.DefileMountainHolds => new TextObject("{=rf_ko_fantasy_urkhai}The kingdom wants dwarf holds shattered and their mountain defenses profaned."),
            KingdomObjectiveType.MartialGlory => new TextObject("{=rf_ko_fantasy_wulf}The realm lives for renown in battle and wants the world to admit its warriors are supreme."),
            KingdomObjectiveType.UnbreakableRealm => new TextObject("{=rf_ko_fantasy_grimwatch}The kingdom aims to become the hardest realm on the map to crack, siege, or shame."),
            KingdomObjectiveType.GuardianFrenzy => new TextObject("{=rf_ko_fantasy_giant}The giants covet nothing beyond their own lands and would risk no war for gain — but wake their wrath, and they will not rest until the aggressor is utterly broken."),
            KingdomObjectiveType.MercenaryCreed => new TextObject("{=rf_ko_fantasy_mercenary}A realm of sellswords with no grand design: keep the coffers full, the walls manned, and owe allegiance to no crown."),
            KingdomObjectiveType.WovenAlliances => new TextObject("{=rf_ko_fantasy_valthorne}Cast off from the Realms, Valthorne means to grow strong through diplomacy — lending swords to friends so that it never stands alone against an enemy."),
            KingdomObjectiveType.ColonialExpansion => new TextObject("{=rf_ko_fantasy_nord}The colonies hunger for new land: conquer, settle, and push the frontier ever outward."),
            _ => new TextObject("{=rf_ko_fantasy_none}This realm has no settled grand design.")
        };
    }

    public static TextObject BuildBriefing(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return new TextObject("{=rf_ko_briefing_none}This realm has no clear grand design that anyone at court can rally around.");
        }

        TextObject text = new TextObject("{=rf_ko_briefing}{TITLE}. {FANTASY} Right now, {STATUS}");
        text.SetTextVariable("TITLE", GetTitle(state.ObjectiveType));
        text.SetTextVariable("FANTASY", GetFantasy(state.ObjectiveType));
        text.SetTextVariable("STATUS", GetProgressStatus(state.ObjectiveProgress, state.ObjectivePressure));
        return text;
    }

    public static TextObject BuildSupportedBriefing(Kingdom kingdom, KingdomIntrigueState state)
    {
        TextObject briefing = BuildBriefing(kingdom, state);
        if (state?.PlayerSupportsObjective != true)
        {
            return briefing;
        }

        TextObject text = new TextObject("{=rf_ko_supported_briefing}{BRIEFING} Your clan is counted among those backing this design.");
        text.SetTextVariable("BRIEFING", briefing);
        return text;
    }

    public static float EvaluateProgress(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return 0f;
        }

        return state.ObjectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance => EvaluateForestConquestObjective(kingdom, state, "battania"),
            KingdomObjectiveType.NobleWealthSupremacy => EvaluateVlandianWealthObjective(kingdom),
            KingdomObjectiveType.PreserveBattanianHomelands => EvaluateBattanianDefenseObjective(kingdom, state),
            KingdomObjectiveType.UniteAseraiRealms => EvaluateBlocUnificationObjective(kingdom, state, AseraiRealmIds, town => town.Settlement.Culture?.StringId == "aserai"),
            KingdomObjectiveType.ClaimImperialLegitimacy => EvaluateBlocUnificationObjective(kingdom, state, ImperialRealmIds, town => IsImperialCulture(town.Settlement.Culture?.StringId)),
            KingdomObjectiveType.ForgeBorderEmpire => EvaluateKhuzaitExpansionObjective(kingdom, state),
            KingdomObjectiveType.ArcaneFrontier => EvaluateForestConquestObjective(kingdom, state, "battania"),
            KingdomObjectiveType.SecureMountainHolds => EvaluateRivalStrongholdObjective(kingdom, state, "urkhai_kingdom", "urkhai"),
            KingdomObjectiveType.DefileMountainHolds => EvaluateRivalStrongholdObjective(kingdom, state, "dwarf_kingdom", "dwarf"),
            KingdomObjectiveType.MartialGlory => EvaluateMartialGloryObjective(kingdom, state),
            KingdomObjectiveType.UnbreakableRealm => EvaluateFortressObjective(kingdom),
            KingdomObjectiveType.GuardianFrenzy => EvaluateGuardianFrenzyObjective(kingdom, state),
            KingdomObjectiveType.MercenaryCreed => EvaluateMercenaryCreedObjective(kingdom),
            KingdomObjectiveType.WovenAlliances => EvaluateWovenAlliancesObjective(kingdom),
            KingdomObjectiveType.ColonialExpansion => EvaluateColonialExpansionObjective(kingdom, state),
            _ => 0f
        };
    }

    public static float EvaluatePressure(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return 0f;
        }

        float pressure = Math.Max(0f, 58f - state.ObjectiveProgress);
        pressure += Math.Max(0f, -state.ObjectiveMomentum) * 1.75f;

        if (IsAggressiveObjective(state.ObjectiveType) && !kingdom.FactionsAtWarWith.Any(x => x.IsKingdomFaction))
        {
            pressure += state.ObjectiveType == KingdomObjectiveType.MartialGlory ? 14f : 6f;
        }

        if (state.ObjectiveType == KingdomObjectiveType.UnbreakableRealm && kingdom.Fiefs.Count() <= 2)
        {
            pressure += 8f;
        }

        // Giant frenzy: once at war the realm demands total victory — the
        // pressure only releases when the aggressor is broken or peace is made.
        if (state.ObjectiveType == KingdomObjectiveType.GuardianFrenzy
            && kingdom.FactionsAtWarWith.Any(x => x.IsKingdomFaction))
        {
            pressure += 16f;
        }

        // Elvean preservation is DEFENSIVE: peace with the old woods intact is
        // success. Pressure only builds while invaders hold Battanian lands.
        if (state.ObjectiveType == KingdomObjectiveType.PreserveBattanianHomelands)
        {
            float lostHomeland = 100f - GetSettlementShare(kingdom, town => town.Settlement.Culture?.StringId == "battania");
            pressure += lostHomeland * 0.12f;
        }

        return Clamp01Score(pressure);
    }

    private static KingdomObjectiveType ResolveByCulture(string cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return KingdomObjectiveType.None;
        }

        if (AseraiRealmIds.Contains(cultureId) || cultureId.Equals("aserai", StringComparison.OrdinalIgnoreCase))
        {
            return KingdomObjectiveType.UniteAseraiRealms;
        }

        if (ImperialCultureIds.Contains(cultureId) || cultureId.StartsWith("empire", StringComparison.OrdinalIgnoreCase))
        {
            return KingdomObjectiveType.ClaimImperialLegitimacy;
        }

        return cultureId.ToLowerInvariant() switch
        {
            "sturgia" => KingdomObjectiveType.CrushBattanianResistance,
            "vlandia" => KingdomObjectiveType.NobleWealthSupremacy,
            "battania" => KingdomObjectiveType.PreserveBattanianHomelands,
            "khuzait" => KingdomObjectiveType.ForgeBorderEmpire,
            "mage" => KingdomObjectiveType.ArcaneFrontier,
            "dwarf" => KingdomObjectiveType.SecureMountainHolds,
            "urkhai" => KingdomObjectiveType.DefileMountainHolds,
            "wulf" => KingdomObjectiveType.MartialGlory,
            "grimwatch" => KingdomObjectiveType.UnbreakableRealm,
            "giant" => KingdomObjectiveType.GuardianFrenzy,
            "katogai" => KingdomObjectiveType.MercenaryCreed,
            "tharnmar" => KingdomObjectiveType.MercenaryCreed,
            "valthorne" => KingdomObjectiveType.WovenAlliances,
            "nord" => KingdomObjectiveType.ColonialExpansion,
            "nords" => KingdomObjectiveType.ColonialExpansion,
            _ => KingdomObjectiveType.None
        };
    }

    private static float EvaluateForestConquestObjective(Kingdom kingdom, KingdomIntrigueState state, string targetKingdomId)
    {
        float settlementControl = GetSettlementShare(kingdom, town => town.Settlement.Culture?.StringId == "battania");
        float rivalWeakness = GetRivalWeaknessScore(kingdom, targetKingdomId);
        return Clamp01Score((settlementControl * 0.58f) + (rivalWeakness * 0.24f) + (state.ObjectiveWarScore * 0.18f));
    }

    private static float EvaluateBattanianDefenseObjective(Kingdom kingdom, KingdomIntrigueState state)
    {
        float homelandControl = GetSettlementShare(kingdom, town => town.Settlement.Culture?.StringId == "battania");
        float invaderWeakness = AverageScores(
            GetRivalWeaknessScore(kingdom, "sturgia"),
            GetRivalWeaknessScore(kingdom, "mage_kingdom"));
        float unity = Clamp01Score(100f - state.CourtFragmentation);
        return Clamp01Score((homelandControl * 0.42f) + (invaderWeakness * 0.23f) + (unity * 0.35f));
    }

    private static float EvaluateVlandianWealthObjective(Kingdom kingdom)
    {
        // "Richest in the WORLD" is a rank, not a number: measure the realm's
        // clan wealth against the richest rival kingdom.
        float totalGold = kingdom.Clans.Sum(x => (float)x.Gold);
        float richestRivalGold = Kingdom.All
            .Where(x => x != kingdom && !x.IsEliminated)
            .Select(x => x.Clans.Sum(c => (float)c.Gold))
            .DefaultIfEmpty(1f)
            .Max();
        float rankScore = Clamp01Score(MapToRange(
            totalGold / Math.Max(1f, richestRivalGold), 0.35f, 1.25f, 5f, 100f));

        float averageProsperity = kingdom.Fiefs.Any()
            ? (float)kingdom.Fiefs.Average(x => x.Prosperity)
            : 0f;
        int houseCount = kingdom.Clans.Count(x => x != kingdom.RulingClan && x.Fiefs.Count() >= 2);
        float houseStrength = kingdom.Clans.Count > 1
            ? (float)houseCount / (kingdom.Clans.Count - 1)
            : 0f;

        float prosperityScore = Clamp01Score((averageProsperity / 7500f) * 100f);
        float houseScore = Clamp01Score(houseStrength * 100f);
        // Gold and silver zones feed the design directly — Nasoria has one more
        // reason to covet, upgrade and defend the mines.
        float zoneScore = GetProviderScore(ZoneWealthScoreProvider, kingdom);
        return Clamp01Score((rankScore * 0.35f) + (prosperityScore * 0.25f) + (houseScore * 0.2f) + (zoneScore * 0.2f));
    }

    private static float EvaluateBlocUnificationObjective(
        Kingdom kingdom,
        KingdomIntrigueState state,
        HashSet<string> blocIds,
        Func<Town, bool> settlementPredicate)
    {
        List<Kingdom> bloc = Kingdom.All.Where(x => x != null && blocIds.Contains(x.StringId)).ToList();
        int rivalCount = bloc.Count(x => x != kingdom && !x.IsEliminated);
        int totalRivals = Math.Max(1, bloc.Count - 1);
        float rivalReduction = Clamp01Score((1f - ((float)rivalCount / totalRivals)) * 100f);
        float controlShare = GetSettlementShare(kingdom, settlementPredicate);
        return Clamp01Score((controlShare * 0.62f) + (rivalReduction * 0.24f) + (state.ObjectiveWarScore * 0.14f));
    }

    private static float EvaluateKhuzaitExpansionObjective(Kingdom kingdom, KingdomIntrigueState state)
    {
        float targetHoldings = GetSettlementShare(kingdom, town =>
        {
            string cultureId = town.Settlement.Culture?.StringId;
            return cultureId == "sturgia" || IsImperialCulture(cultureId);
        });
        float rivalWeakness = AverageScores(
            GetRivalWeaknessScore(kingdom, "sturgia"),
            GetRivalWeaknessScore(kingdom, "empire"),
            GetRivalWeaknessScore(kingdom, "empire_w"),
            GetRivalWeaknessScore(kingdom, "empire_s"),
            GetRivalWeaknessScore(kingdom, "south_realm"),
            GetRivalWeaknessScore(kingdom, "west_realm"));
        return Clamp01Score((targetHoldings * 0.56f) + (rivalWeakness * 0.24f) + (state.ObjectiveWarScore * 0.2f));
    }

    private static float EvaluateRivalStrongholdObjective(Kingdom kingdom, KingdomIntrigueState state, string rivalKingdomId, string rivalCultureId)
    {
        float conquest = GetSettlementShare(kingdom, town => town.Settlement.Culture?.StringId == rivalCultureId);
        float rivalWeakness = GetRivalWeaknessScore(kingdom, rivalKingdomId);
        float homelandSecurity = EvaluateHomelandSecurity(kingdom, kingdom.Culture?.StringId);
        // The mountain war is fought over the MINES too: holding the map's
        // iron/gold/silver diggings counts toward mastery of the holds.
        float mineControl = GetProviderScore(MineControlScoreProvider, kingdom);
        return Clamp01Score((conquest * 0.4f) + (rivalWeakness * 0.25f) + (homelandSecurity * 0.1f) + (mineControl * 0.1f) + (state.ObjectiveWarScore * 0.15f));
    }

    private static float EvaluateMartialGloryObjective(Kingdom kingdom, KingdomIntrigueState state)
    {
        float warTempo = Clamp01Score(kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction) * 22f);
        float strengthScore = Clamp01Score((float)Math.Sqrt(kingdom.CurrentTotalStrength) * 0.14f);
        return Clamp01Score((state.ObjectiveWarScore * 0.62f) + (warTempo * 0.23f) + (strengthScore * 0.15f));
    }

    private static float EvaluateFortressObjective(Kingdom kingdom)
    {
        List<Town> fiefs = kingdom.Fiefs.ToList();
        if (fiefs.Count == 0)
        {
            return 0f;
        }

        float securityScore = Clamp01Score((float)fiefs.Average(x => x.Security));
        float loyaltyScore = Clamp01Score((float)fiefs.Where(x => x.IsTown).DefaultIfEmpty().Average(x => x?.Loyalty ?? 60f));
        float militiaScore = Clamp01Score((float)fiefs.Average(x => Math.Min(100f, x.Militia / 3.2f)));
        float garrisonScore = Clamp01Score((float)fiefs.Average(x => Math.Min(100f, (x.GarrisonParty?.Party.NumberOfHealthyMembers ?? 0) / 4.8f)));
        return Clamp01Score((securityScore * 0.24f) + (loyaltyScore * 0.14f) + (militiaScore * 0.28f) + (garrisonScore * 0.34f));
    }

    /// <summary>Giants: at peace, success is simply holding the homeland safe.
    /// At war (frenzy), success is measured by how thoroughly the aggressor is
    /// being broken — peace-of-mind metrics stop mattering.</summary>
    private static float EvaluateGuardianFrenzyObjective(Kingdom kingdom, KingdomIntrigueState state)
    {
        float homelandControl = GetSettlementShare(kingdom, town => town.Settlement.Culture?.StringId == "giant");
        float homelandSecurity = EvaluateHomelandSecurity(kingdom, "giant");

        List<Kingdom> enemies = kingdom.FactionsAtWarWith.OfType<Kingdom>().ToList();
        if (enemies.Count == 0)
        {
            return Clamp01Score((homelandControl * 0.45f) + (homelandSecurity * 0.35f) + 20f);
        }

        float enemyWeakness = AverageScores(enemies.Select(x => GetRivalWeaknessScore(kingdom, x.StringId)).ToArray());
        return Clamp01Score((enemyWeakness * 0.45f) + (state.ObjectiveWarScore * 0.3f) + (homelandControl * 0.25f));
    }

    /// <summary>Mercenary realms (Katogai, Tharnmar): full coffers and walls
    /// that hold — no grand design beyond readiness.</summary>
    private static float EvaluateMercenaryCreedObjective(Kingdom kingdom)
    {
        float readiness = EvaluateFortressObjective(kingdom);
        float coffers = Clamp01Score((float)Math.Sqrt(kingdom.Clans.Sum(x => (float)x.Gold)) * 0.07f);
        return Clamp01Score((readiness * 0.72f) + (coffers * 0.28f));
    }

    /// <summary>Valthorne: strength through diplomacy — the fewer enemies and
    /// the more co-belligerent friends, the stronger the web.</summary>
    private static float EvaluateWovenAlliancesObjective(Kingdom kingdom)
    {
        List<Kingdom> others = Kingdom.All.Where(x => x != kingdom && !x.IsEliminated).ToList();
        if (others.Count == 0)
        {
            return 0f;
        }

        List<Kingdom> enemies = kingdom.FactionsAtWarWith.OfType<Kingdom>().ToList();
        float peaceShare = Clamp01Score((1f - ((float)enemies.Count / others.Count)) * 100f);

        // "Military support": realms fighting the same enemies count as friends
        // the web has bound — the diplomatic promise made real.
        int friendsInArms = others.Count(other =>
            other.FactionsAtWarWith.OfType<Kingdom>().Any(sharedEnemy =>
                sharedEnemy != kingdom && enemies.Contains(sharedEnemy)));
        float friendScore = Clamp01Score(friendsInArms * 25f);

        float strengthScore = Clamp01Score((float)Math.Sqrt(kingdom.CurrentTotalStrength) * 0.12f);
        return Clamp01Score((peaceShare * 0.5f) + (friendScore * 0.28f) + (strengthScore * 0.22f));
    }

    /// <summary>Nord colonies: expansion is the design — every new fief is the
    /// colony pushing its frontier outward.</summary>
    private static float EvaluateColonialExpansionObjective(Kingdom kingdom, KingdomIntrigueState state)
    {
        float holdings = Clamp01Score(kingdom.Fiefs.Count() * 9f);
        float strengthScore = Clamp01Score((float)Math.Sqrt(kingdom.CurrentTotalStrength) * 0.12f);
        return Clamp01Score((holdings * 0.55f) + (state.ObjectiveWarScore * 0.25f) + (strengthScore * 0.2f));
    }

    private static float EvaluateHomelandSecurity(Kingdom kingdom, string cultureId)
    {
        if (string.IsNullOrWhiteSpace(cultureId))
        {
            return 0f;
        }

        List<Town> homelandFiefs = kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == cultureId).ToList();
        if (homelandFiefs.Count == 0)
        {
            homelandFiefs = kingdom.Fiefs.ToList();
        }

        if (homelandFiefs.Count == 0)
        {
            return 0f;
        }

        return Clamp01Score((float)homelandFiefs.Average(x => (x.Security * 0.55f) + (Math.Min(100f, x.Militia / 3.5f) * 0.45f)));
    }

    private static float GetSettlementShare(Kingdom kingdom, Func<Town, bool> predicate)
    {
        List<Town> matchingTowns = Town.AllTowns.Where(predicate).ToList();
        if (matchingTowns.Count == 0)
        {
            return 0f;
        }

        int owned = matchingTowns.Count(x => x.OwnerClan?.Kingdom == kingdom);
        return Clamp01Score(((float)owned / matchingTowns.Count) * 100f);
    }

    private static float GetRivalWeaknessScore(Kingdom observer, string rivalId)
    {
        if (observer == null || string.IsNullOrWhiteSpace(rivalId))
        {
            return 0f;
        }

        Kingdom rival = Kingdom.All.FirstOrDefault(x => x != null && x.StringId.Equals(rivalId, StringComparison.OrdinalIgnoreCase));
        if (rival == null || rival.IsEliminated || rival.Fiefs.Count() == 0)
        {
            return 100f;
        }

        float ratioScore = Clamp01Score(MapToRange(observer.CurrentTotalStrength / Math.Max(1f, rival.CurrentTotalStrength), 0.45f, 2f, 8f, 88f));
        if (!observer.IsAtWarWith(rival))
        {
            return ratioScore;
        }

        float warProgress = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(observer, rival).ResultNumber;
        float warScore = Clamp01Score(MapToRange(warProgress, -100f, 100f, 0f, 100f));
        return Clamp01Score((ratioScore * 0.4f) + (warScore * 0.6f));
    }

    private static bool IsImperialCulture(string cultureId)
    {
        return !string.IsNullOrWhiteSpace(cultureId)
            && (ImperialCultureIds.Contains(cultureId) || cultureId.StartsWith("empire", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAggressiveObjective(KingdomObjectiveType objectiveType)
    {
        // PreserveBattanianHomelands removed (author decision 2026-07-15): a
        // defensive design must not accumulate frustration during peace — its
        // pressure now comes from invaders holding Elvean lands instead.
        return objectiveType is KingdomObjectiveType.CrushBattanianResistance
            or KingdomObjectiveType.UniteAseraiRealms
            or KingdomObjectiveType.ClaimImperialLegitimacy
            or KingdomObjectiveType.ForgeBorderEmpire
            or KingdomObjectiveType.ArcaneFrontier
            or KingdomObjectiveType.SecureMountainHolds
            or KingdomObjectiveType.DefileMountainHolds
            or KingdomObjectiveType.MartialGlory
            or KingdomObjectiveType.ColonialExpansion;
        // GuardianFrenzy, MercenaryCreed and WovenAlliances are deliberately
        // NOT aggressive: peace is their success state, not a frustration.
    }

    private static TextObject GetProgressStatus(float progress, float pressure)
    {
        return progress switch
        {
            >= 90f => new TextObject("{=rf_ko_status_excellent}the realm believes that design is nearly achieved."),
            >= 72f => new TextObject("{=rf_ko_status_strong}the agenda is advancing with real confidence."),
            >= 52f => new TextObject("{=rf_ko_status_steady}the agenda is moving, though not without strain."),
            >= 32f => pressure >= 40f
                ? new TextObject("{=rf_ko_status_strained}the court keeps speaking of that design, but frustration is beginning to creep in.")
                : new TextObject("{=rf_ko_status_mixed}the agenda advances unevenly and still needs proof in the field."),
            _ => pressure >= 55f
                ? new TextObject("{=rf_ko_status_failing}the design is stalling badly, and people are beginning to doubt the court behind it.")
                : new TextObject("{=rf_ko_status_weak}the design exists more as a claim than an achievement right now.")
        };
    }

    private static float AverageScores(params float[] scores)
    {
        float[] validScores = scores.Where(x => x >= 0f).ToArray();
        return validScores.Length == 0 ? 0f : validScores.Average();
    }

    private static float MapToRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        if (value <= fromMin)
        {
            return toMin;
        }

        if (value >= fromMax)
        {
            return toMax;
        }

        float ratio = (value - fromMin) / (fromMax - fromMin);
        return toMin + ((toMax - toMin) * ratio);
    }

    private static float Clamp01Score(float value)
    {
        return Math.Max(0f, Math.Min(100f, value));
    }
}
