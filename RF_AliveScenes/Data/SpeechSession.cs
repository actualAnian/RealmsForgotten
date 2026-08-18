using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_AliveScenes.Data;

/// <summary>
/// Estado "ja falei isso" de UMA missao. O banco em si continua imutavel, entao sair da
/// cena e entrar em outra sempre comeca limpo (no mod original o indice dos dialogos
/// vazava de uma cena para a outra).
/// </summary>
public sealed class SpeechSession
{
    private readonly ConversationBank _bank = ConversationBank.Instance;
    private readonly HashSet<OneLiner> _usedOneLiners = new();
    private readonly HashSet<string> _usedDialogues = new();

    public bool IsEmpty => !_bank.IsLoaded;

    /// <summary>
    /// Sorteia uma fala valida para o ator/contexto. Varre a lista do ator a partir de um
    /// ponto aleatorio (sem embaralhar nada), devolve string vazia se nada servir.
    /// </summary>
    public string PickOneLiner(ActorType actor, in SpeechContext context)
    {
        IReadOnlyList<OneLiner> pool = _bank.OneLinersFor(actor);
        int count = pool.Count;
        if (count == 0)
        {
            return string.Empty;
        }

        int start = MBRandom.RandomInt(count);
        for (int i = 0; i < count; i++)
        {
            OneLiner candidate = pool[(start + i) % count];
            if (candidate.Frequency == SpeechFrequency.UNIQUE && _usedOneLiners.Contains(candidate))
            {
                continue;
            }
            if (!Matches(candidate.Conditions, context))
            {
                continue;
            }

            if (candidate.Frequency == SpeechFrequency.UNIQUE)
            {
                _usedOneLiners.Add(candidate);
            }
            return candidate.ToLocalizedRaw();
        }

        return string.Empty;
    }

    /// <summary>Sorteia um dialogo ainda nao usado para o ator. Pode devolver null.</summary>
    public DialogueDef PickDialogue(ActorType actor)
    {
        IReadOnlyList<DialogueDef> pool = _bank.DialoguesFor(actor);
        int count = pool.Count;
        if (count == 0)
        {
            return null;
        }

        int start = MBRandom.RandomInt(count);
        for (int i = 0; i < count; i++)
        {
            DialogueDef candidate = pool[(start + i) % count];
            if (_usedDialogues.Contains(candidate.Id))
            {
                continue;
            }

            if (candidate.Frequency == SpeechFrequency.UNIQUE)
            {
                _usedDialogues.Add(candidate.Id);
            }
            return candidate;
        }

        return null;
    }

    private static bool Matches(SpeechCondition[] conditions, in SpeechContext context)
    {
        if (conditions == null || conditions.Length == 0)
        {
            return true;
        }

        foreach (SpeechCondition condition in conditions)
        {
            if (!Evaluate(condition, context))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Regra herdada do mod original: em batalha as condicoes de cena pacifica nunca
    /// batem (e vice-versa), e quase toda condicao de "clima/moral" e suprimida enquanto
    /// ha inimigo perto — em combate so passam ANY e DURING_COMBAT.
    /// </summary>
    private static bool Evaluate(SpeechCondition condition, in SpeechContext c)
    {
        switch (condition)
        {
            case SpeechCondition.ANY:
                return true;

            case SpeechCondition.DURING_COMBAT:
                return c.IsBattle && c.DuringCombat;

            // --- so fora de batalha ---
            case SpeechCondition.PLAYER_HAVE_SWORD:
                return !c.IsBattle && c.HasSword;
            case SpeechCondition.PLAYER_HAVE_BOW:
                return !c.IsBattle && c.HasBow;
            case SpeechCondition.PLAYER_HAVE_HORSE:
                return !c.IsBattle && c.HasHorse;
            case SpeechCondition.PLAYER_HAVE_EXPENSIVE_ARMOR:
                return !c.IsBattle && c.HasExpensiveArmor;
            case SpeechCondition.PLAYER_HAVE_EXPENSIVE_SWORD:
                return !c.IsBattle && c.HasExpensiveSword;
            case SpeechCondition.POOR_SETTLEMENT:
                return !c.IsBattle && c.IsPoorSettlement;
            case SpeechCondition.RICH_SETTLEMENT:
                return !c.IsBattle && c.IsRichSettlement;
            case SpeechCondition.STAND:
                return !c.IsBattle && c.IsStanding;
            case SpeechCondition.FARMING:
                return !c.IsBattle && c.IsFarming;
            case SpeechCondition.IN_TAVERN:
                return !c.IsBattle && c.InTavern;
            case SpeechCondition.IN_KEEP:
                return !c.IsBattle && c.InKeep;
            case SpeechCondition.IN_PORT:
                return !c.IsBattle && c.InPort;

            // --- so em batalha, e nunca com inimigo em cima ---
            case SpeechCondition.OVERPOWERED:
                return c.IsBattle && c.Overpowered && !c.DuringCombat;
            case SpeechCondition.UNDERPOWERED:
                return c.IsBattle && c.Underpowered && !c.DuringCombat;
            case SpeechCondition.SIEGE_DEFENDER:
                return c.IsBattle && c.SiegeDefender && !c.DuringCombat;
            case SpeechCondition.SIEGE_ATTACKER:
                return c.IsBattle && c.SiegeAttacker && !c.DuringCombat;
            case SpeechCondition.AGAINST_LOOTERS:
                return c.IsBattle && c.AgainstLooters && !c.DuringCombat;
            case SpeechCondition.AGAINST_VILLAGERS:
                return c.IsBattle && c.AgainstVillagers && !c.DuringCombat;
            case SpeechCondition.AT_SEA_ANY:
                return c.IsBattle && c.AtSea && !c.DuringCombat;
            case SpeechCondition.AT_SEA_RAINY:
                return c.IsBattle && c.AtSea && c.Rainy && !c.DuringCombat;
            case SpeechCondition.AT_SEA_WINDY:
                return c.IsBattle && c.AtSea && c.Windy && !c.DuringCombat;

            // --- estacao vale nos dois casos, mas nao no meio da porrada ---
            case SpeechCondition.SEASON_SPRING:
                return c.Season == CampaignTime.Seasons.Spring && !c.DuringCombat;
            case SpeechCondition.SEASON_SUMMER:
                return c.Season == CampaignTime.Seasons.Summer && !c.DuringCombat;
            case SpeechCondition.SEASON_AUTUMN:
                return c.Season == CampaignTime.Seasons.Autumn && !c.DuringCombat;
            case SpeechCondition.SEASON_WINTER:
                return c.Season == CampaignTime.Seasons.Winter && !c.DuringCombat;

            default:
                return false;
        }
    }
}
