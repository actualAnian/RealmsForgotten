using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RF_Ambush;

/// <summary>
/// FASE 2 — a IA embosca o jogador.
///
/// Sem simular "stance" para centenas de parties: uma party ja e uma emboscadora
/// em potencial quando o proprio fog of war diz que NOS NAO A VEMOS. Elegivel =
/// hostil + invisivel para o jogador + batedor melhor que o nosso + perto. Dai
/// uma chance por hora decide se ela salta.
///
/// CONTRA-JOGO OBRIGATORIO (decisao de design, ver ESTUDO_TOTALAMBUSH.md): antes
/// de qualquer salto, o nosso batedor tem a chance dele — se a nossa visibilidade
/// da party oculta esta QUASE la (fracao >= ForewarnVisibilityFraction), o jogador
/// recebe "sinais de emboscada a frente", XP de Scouting, e aquela party fica
/// queimada por 24h. Scouting alto transforma emboscada inimiga em informacao.
/// Sem isso, a fase 2 seria so frustracao — nunca soltar uma sem a outra.
/// </summary>
public sealed class EnemyAmbushBehavior : CampaignBehaviorBase
{
    /// <summary>Parties reveladas pelos sinais: id -> hora em que a queima expira.</summary>
    private readonly Dictionary<string, double> _forewarnedUntil = new Dictionary<string, double>();

    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnHourlyTick()
    {
        MobileParty main = MobileParty.MainParty;
        if (main == null || Campaign.Current == null)
        {
            return;
        }
        // Nao emboscar quem nao esta viajando normalmente.
        if (main.MapEvent != null || main.CurrentSettlement != null || main.IsCurrentlyAtSea
            || PlayerEncounter.Current != null || AmbushState.StanceActive)
        {
            return;
        }

        CleanupForewarned();

        LocatableSearchData<MobileParty> data =
            MobileParty.StartFindingLocatablesAroundPosition(main.Position.ToVec2(), AmbushConfig.ScanRange);
        for (MobileParty p = MobileParty.FindNextLocatable(ref data); p != null; p = MobileParty.FindNextLocatable(ref data))
        {
            if (!IsEligibleAmbusher(p, main))
            {
                continue;
            }

            // O contra-jogo vem PRIMEIRO: o nosso batedor quase os viu?
            float ourSight = AmbushMath.VisibilityFraction(main, p);
            if (ourSight >= AmbushConfig.ForewarnVisibilityFraction)
            {
                Forewarn(p, main);
                continue;
            }

            // Perto o bastante para o bote?
            if (p.Position.Distance(main.Position) > AmbushConfig.EnemyPounceRange)
            {
                continue;
            }

            if (MBRandom.RandomFloat > AmbushConfig.EnemyAmbushChancePerHour)
            {
                continue;
            }

            SpringOnPlayer(p);
            return;
        }
    }

    private bool IsEligibleAmbusher(MobileParty p, MobileParty main)
    {
        if (p == main || !p.IsActive || p.IsCurrentlyAtSea || p.MapEvent != null
            || p.MemberRoster.TotalHealthyCount <= 0)
        {
            return false;
        }
        IFaction? faction = p.MapFaction;
        if (faction == null || main.MapFaction == null || !faction.IsAtWarWith(main.MapFaction))
        {
            return false;
        }
        // So embosca quem nos NAO vemos — e a definicao de emboscada.
        if (p.IsVisible)
        {
            return false;
        }
        if (_forewarnedUntil.ContainsKey(p.StringId))
        {
            return false;
        }
        // Predador precisa de olhos melhores: Scouting efetivo maior que o nosso.
        int theirScouting = p.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting)
            ?? p.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
        int ourScouting = main.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting)
            ?? Hero.MainHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
        if (theirScouting <= ourScouting)
        {
            return false;
        }
        // Bandidos sao emboscadores natos; lordes precisam de vantagem real (+25).
        if (!p.IsBandit && theirScouting < ourScouting + 25)
        {
            return false;
        }
        return true;
    }

    private void Forewarn(MobileParty ambusher, MobileParty main)
    {
        _forewarnedUntil[ambusher.StringId] = CampaignTime.Now.ToHours + 24.0;
        main.EffectiveScout?.AddSkillXp(DefaultSkills.Scouting, AmbushConfig.ScoutXpOnForewarn);
        MBInformationManager.AddQuickInformation(
            new TextObject("Your scout notices signs of an ambush ahead — broken branches, fresh tracks. Someone is lying in wait."),
            0, null, null, "");
    }

    private static void SpringOnPlayer(MobileParty ambusher)
    {
        AmbushState.Arm(AmbushSide.EnemyAmbushes, ambusher);
        MBInformationManager.AddQuickInformation(
            new TextObject($"{ambusher.Name} springs from hiding — it's an ambush!"), 0, null, null, "");
        EncounterManager.StartPartyEncounter(ambusher.Party, PartyBase.MainParty);
    }

    private void CleanupForewarned()
    {
        double now = CampaignTime.Now.ToHours;
        var expired = new List<string>();
        foreach (KeyValuePair<string, double> kv in _forewarnedUntil)
        {
            if (kv.Value < now)
            {
                expired.Add(kv.Key);
            }
        }
        foreach (string key in expired)
        {
            _forewarnedUntil.Remove(key);
        }
    }
}
