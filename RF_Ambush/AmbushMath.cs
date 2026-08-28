using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace RF_Ambush;

/// <summary>
/// A matematica de "quem ve quem" no mapa — Scouting contra Scouting, sem d100.
///
/// Na 1.4.7 o modelo vanilla so calcula visao DO JOGADOR (o antigo
/// GetPartySpottingDifficulty virou GetPartySpottingRatioForMainPartySeeingRange,
/// que recebe apenas o alvo e serve ao fog of war). Entao:
///
///   - alcance de quem procura: MapVisibilityModel.GetPartySpottingRange
///     (Scouting do EffectiveScout, perks, noite corta a base pela metade) —
///     este vale para QUALQUER party, e e a metade vanilla que reusamos;
///   - ocultacao do emboscado: conta NOSSA (terreno, tempo assentado, batedor,
///     tamanho), aplicada como divisor do alcance de quem procura.
///
/// Decisao pela convencao vanilla: visivel se (range/dificuldade)² / dist² >= 1.
/// A IA de campanha nem faz esse teste (e onisciente por raio) — por isso a
/// stance tambem usa IgnoreByOtherPartiesTill; esta conta decide quando a
/// stance QUEBRA, e o contra-jogo da fase 2.
/// </summary>
public static class AmbushMath
{
    /// <summary>Multiplicador de ocultacao do terreno onde a party oculta esta.</summary>
    public static float TerrainConcealment(MobileParty hidden)
    {
        try
        {
            TerrainType terrain = Campaign.Current.MapSceneWrapper
                .GetFaceTerrainType(hidden.CurrentNavigationFace);
            switch (terrain)
            {
                case TerrainType.Forest:
                    return 2.2f;   // mata fechada: o lar da emboscada
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                    return 1.6f;   // pedras e desniveis escondem bem
                case TerrainType.Steppe:
                case TerrainType.Plain:
                    return 1.0f;   // campo aberto nao esconde ninguem
                case TerrainType.Swamp:
                    return 1.5f;   // juncos e vegetacao alta escondem razoavel
                case TerrainType.Snow:
                    return 0.9f;   // contraste e rastros na neve entregam a posicao
                case TerrainType.Desert:
                case TerrainType.Dune:
                    return 0.85f;  // areia aberta e ATE pior que campo
                default:
                    return 1.0f;
            }
        }
        catch (Exception)
        {
            return 1.0f;
        }
    }

    /// <summary>
    /// Dificuldade TOTAL de enxergar a party oculta. 1.0 = party comum andando;
    /// maior = mais dificil. Terreno x stance x assentamento x batedor x tamanho.
    /// </summary>
    public static float ConcealmentDifficulty(MobileParty hidden)
    {
        float difficulty = TerrainConcealment(hidden);

        if (!AmbushState.StanceActive || hidden != MobileParty.MainParty)
        {
            return difficulty;
        }

        // Ocultacao cresce com o tempo assentado: 60% ao armar, 100% apos 3h.
        float settle = AmbushConfig.InitialConcealmentFraction;
        float hours = (float)AmbushState.StanceSince.ElapsedHoursUntilNow;
        if (hours > 0f)
        {
            settle += (1f - AmbushConfig.InitialConcealmentFraction)
                * Math.Min(1f, hours / AmbushConfig.FullConcealmentAfterHours);
        }

        // O batedor esconde a party: +1/300 por ponto de Scouting.
        float scoutBonus = 1f;
        Hero? scout = hidden.EffectiveScout;
        if (scout != null)
        {
            scoutBonus += scout.GetSkillValue(DefaultSkills.Scouting) * AmbushConfig.ConcealmentPerScoutSkill;
        }

        // Party grande esconde mal: divisor logaritmico acima da folga de 20 homens.
        float men = Math.Max(1, hidden.MemberRoster.TotalManCount);
        float sizePenalty = 1f;
        if (men > AmbushConfig.ConcealmentSizeGraceMen)
        {
            sizePenalty = 1f + (float)Math.Log10(men / (float)AmbushConfig.ConcealmentSizeGraceMen);
        }

        float stance = AmbushConfig.StanceConcealmentMultiplier * settle * scoutBonus / sizePenalty;
        return difficulty * Math.Max(1f, stance);
    }

    /// <summary>Alcance de avistamento de uma party qualquer (Scouting do batedor, perks, noite).</summary>
    public static float SpottingRange(MobileParty spotter)
    {
        try
        {
            return Campaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(spotter).ResultNumber;
        }
        catch (Exception)
        {
            return 12f;
        }
    }

    /// <summary>
    /// Fracao de visibilidade de <paramref name="target"/> para <paramref name="spotter"/>:
    /// &gt;= 1 significa avistado (mesma convencao do fog of war vanilla).
    /// </summary>
    public static float VisibilityFraction(MobileParty spotter, MobileParty target)
    {
        if (spotter == null || target == null)
        {
            return 1f;
        }

        float dist = spotter.Position.Distance(target.Position);
        if (dist < 0.01f)
        {
            return float.MaxValue;
        }

        float effective = SpottingRange(spotter) / Math.Max(0.01f, ConcealmentDifficulty(target));
        return effective * effective / (dist * dist);
    }

    /// <summary>O inimigo avistou a party oculta?</summary>
    public static bool IsSpottedBy(MobileParty spotter, MobileParty hidden)
    {
        return VisibilityFraction(spotter, hidden) >= 1f;
    }

    /// <summary>
    /// Contra-jogo da fase 2: quanto o JOGADOR esta perto de avistar a party
    /// oculta. Aqui da para reusar a metade player-centric que sobrou no vanilla
    /// (o ratio por alvo do fog of war) por cima do nosso terreno.
    /// </summary>
    public static float PlayerSightFraction(MobileParty target)
    {
        MobileParty main = MobileParty.MainParty;
        if (main == null || target == null)
        {
            return 0f;
        }

        float dist = main.Position.Distance(target.Position);
        if (dist < 0.01f)
        {
            return float.MaxValue;
        }

        float ratio = 1f;
        try
        {
            ratio = Campaign.Current.Models.MapVisibilityModel
                .GetPartySpottingRatioForMainPartySeeingRange(target);
        }
        catch (Exception)
        {
        }

        float effective = SpottingRange(main) * Math.Max(0.05f, ratio)
            / Math.Max(0.5f, TerrainConcealment(target));
        return effective * effective / (dist * dist);
    }
}
