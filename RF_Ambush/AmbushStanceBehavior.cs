using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace RF_Ambush;

/// <summary>
/// FASE 1 — o jogador embosca (ver ESTUDO_TOTALAMBUSH.md).
///
/// A critica do autor ao TotalAmbush: la a emboscada so existia como opcao do
/// menu de ENCOUNTER — ou seja, depois do contato, que e o contrario de
/// emboscar. Aqui a emboscada e uma STANCE no mapa, sem contato:
///
///   tecla B (fora de encounter) -> menu de espera "armar emboscada"
///   -> a party fica invisivel para a IA (IgnoreByOtherPartiesTill, o mecanismo
///      vanilla de "nao sou alvo" — a IA e onisciente por raio e nao consulta
///      dificuldade de avistamento, entao SO a matematica nao bastaria)
///   -> a cada hora, cada hostil proximo tenta nos AVISTAR de verdade
///      (AmbushMath: Scouting dele vs ocultacao nossa, formula do fog of war)
///   -> avistou: stance quebra, a IA volta a nos ver, ele reage normalmente
///   -> nao avistou e entrou no alcance de bote: "Saltar a emboscada / Deixar
///      passar". O DEIXAR PASSAR e o coracao da mecanica — emboscador escolhe
///      a presa.
///   -> saltar: arma a missao de emboscada e inicia o encounter como atacante.
/// </summary>
public sealed class AmbushStanceBehavior : CampaignBehaviorBase
{
    private const string MenuId = "rf_ambush_wait";

    /// <summary>Cooldown persistido (horas de campanha, CampaignTime.ToHours).</summary>
    private double _cooldownUntilHours;

    /// <summary>Parties que o jogador mandou deixar passar: id -> hora de campanha em que expira.</summary>
    private readonly Dictionary<string, double> _letPassUntil = new Dictionary<string, double>();

    private CampaignTime _lastHourlyWork = CampaignTime.Zero;

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnRealtimeTick);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("rfAmbushCooldownUntilHours", ref _cooldownUntilHours);
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        AmbushState.ResetAll();
        AddMenus(starter);
    }

    // ------------------------------------------------------------------
    //  entrada na stance: tecla B no mapa
    // ------------------------------------------------------------------

    private void OnRealtimeTick(float dt)
    {
        if (!Input.IsKeyPressed(AmbushConfig.StanceKey))
        {
            return;
        }
        if (AmbushState.StanceActive || Campaign.Current == null)
        {
            return;
        }
        // So no mapa de campanha, sem menu aberto, sem encounter, em terra.
        if (Campaign.Current.CurrentMenuContext != null
            || ScreenManager.TopScreen is not SandBox.View.Map.MapScreen)
        {
            return;
        }
        MobileParty main = MobileParty.MainParty;
        if (main == null || PlayerEncounter.Current != null || main.MapEvent != null
            || main.CurrentSettlement != null || main.IsCurrentlyAtSea
            || main.Army != null || main.AttachedTo != null)
        {
            return;
        }
        if (CampaignTime.Now.ToHours < _cooldownUntilHours)
        {
            MBInformationManager.AddQuickInformation(
                new TextObject("Your troops are still recovering from the last ambush."), 0, null, null, "");
            return;
        }

        EnterStance();
    }

    private void EnterStance()
    {
        AmbushState.EnterStance();
        MobileParty.MainParty.SetMoveModeHold();
        RefreshAiIgnore();
        GameMenu.ActivateGameMenu(MenuId);
    }

    // ------------------------------------------------------------------
    //  menus
    // ------------------------------------------------------------------

    private void AddMenus(CampaignGameStarter starter)
    {
        starter.AddWaitGameMenu(
            MenuId,
            "{=rf_ambush_wait_text}{RF_AMBUSH_STATUS}",
            WaitMenuInit,
            WaitMenuCondition,
            null,
            WaitMenuTick,
            GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

        // Saltar sobre a presa. So habilita com alvo no alcance de bote e sem nos ver.
        starter.AddGameMenuOption(MenuId, "rf_ambush_spring",
            "{=rf_ambush_spring}Spring the ambush on {RF_AMBUSH_TARGET}",
            SpringCondition, SpringConsequence, isLeave: false, index: 1);

        // Deixar passar: marca a party para nao reabrir o aviso por 12h.
        starter.AddGameMenuOption(MenuId, "rf_ambush_letpass",
            "{=rf_ambush_letpass}Let {RF_AMBUSH_TARGET} pass",
            LetPassCondition, LetPassConsequence, isLeave: false, index: 2);

        starter.AddGameMenuOption(MenuId, "rf_ambush_break",
            "{=rf_ambush_break}Break the ambush and move on",
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            _ => LeaveStance(), isLeave: true, index: 3);
    }

    private void WaitMenuInit(MenuCallbackArgs args)
    {
        args.MenuContext.GameMenu.StartWait();
        UpdateStatusText();
    }

    private bool WaitMenuCondition(MenuCallbackArgs args)
    {
        return true;
    }

    private void WaitMenuTick(MenuCallbackArgs args, CampaignTime dt)
    {
        // O trabalho pesado e horario (OnHourlyTick). Aqui so o texto e a
        // deteccao de bote, que precisa de reacao mais fina que 1h.
        ScanForPounceTarget();
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        string status;
        if (AmbushState.PounceTarget != null)
        {
            status = $"Your troops lie in wait. {AmbushState.PounceTarget.Name} is walking into your trap, unaware.";
        }
        else
        {
            float hours = (float)AmbushState.StanceSince.ElapsedHoursUntilNow;
            string settle = hours >= AmbushConfig.FullConcealmentAfterHours
                ? "Your position is fully concealed."
                : "Your troops are settling into cover.";
            status = $"Your party lies in ambush. {settle} (Scout: {ScoutLabel()})";
        }
        MBTextManager.SetTextVariable("RF_AMBUSH_STATUS", status, false);
        MBTextManager.SetTextVariable("RF_AMBUSH_TARGET",
            AmbushState.PounceTarget?.Name?.ToString() ?? "no one", false);
    }

    private static string ScoutLabel()
    {
        Hero? scout = MobileParty.MainParty.EffectiveScout;
        return scout == null ? "none" : $"{scout.Name}, Scouting {scout.GetSkillValue(DefaultSkills.Scouting)}";
    }

    // ------------------------------------------------------------------
    //  o tick que faz o sistema existir
    // ------------------------------------------------------------------

    private void OnHourlyTick()
    {
        if (!AmbushState.StanceActive)
        {
            return;
        }
        // Stance nao sobrevive a estados anomalos (fomos arrastados a um evento).
        MobileParty main = MobileParty.MainParty;
        if (main == null || main.MapEvent != null || main.CurrentSettlement != null)
        {
            AmbushState.ExitStance();
            return;
        }

        RefreshAiIgnore();
        CleanupLetPass();

        bool enemyNearby = false;
        foreach (MobileParty enemy in EnumerateHostilesInRange(AmbushConfig.ScanRange))
        {
            enemyNearby = true;
            if (AmbushMath.IsSpottedBy(enemy, main))
            {
                OnSpotted(enemy);
                return;
            }
        }

        // XP de Scouting: so quando ha inimigo por perto sem nos ver — furtividade
        // de verdade, nao acampamento em area morta.
        if (enemyNearby && main.EffectiveScout != null)
        {
            main.EffectiveScout.AddSkillXp(DefaultSkills.Scouting, AmbushConfig.ScoutXpPerStealthHour);
        }

        // Esperar cansa: moral cai depois de MoraleDecayAfterHours.
        float hours = (float)AmbushState.StanceSince.ElapsedHoursUntilNow;
        if (hours > AmbushConfig.MoraleDecayAfterHours)
        {
            main.RecentEventsMorale -= AmbushConfig.MoralePerHourBeyond;
        }
    }

    private void ScanForPounceTarget()
    {
        MobileParty main = MobileParty.MainParty;
        if (main == null || !AmbushState.StanceActive)
        {
            return;
        }
        MobileParty? best = null;
        float bestDist = float.MaxValue;
        foreach (MobileParty enemy in EnumerateHostilesInRange(AmbushConfig.PounceRange))
        {
            if (_letPassUntil.ContainsKey(enemy.StringId))
            {
                continue;
            }
            if (AmbushMath.IsSpottedBy(enemy, main))
            {
                OnSpotted(enemy);
                return;
            }
            float d = enemy.Position.Distance(main.Position);
            if (d < bestDist)
            {
                bestDist = d;
                best = enemy;
            }
        }
        AmbushState.PounceTarget = best;
    }

    private static IEnumerable<MobileParty> EnumerateHostilesInRange(float range)
    {
        MobileParty main = MobileParty.MainParty;
        LocatableSearchData<MobileParty> data =
            MobileParty.StartFindingLocatablesAroundPosition(main.Position.ToVec2(), range);
        for (MobileParty p = MobileParty.FindNextLocatable(ref data); p != null; p = MobileParty.FindNextLocatable(ref data))
        {
            if (p == main || !p.IsActive || p.IsCurrentlyAtSea || p.MemberRoster.TotalHealthyCount <= 0)
            {
                continue;
            }
            IFaction? faction = p.MapFaction;
            if (faction == null || main.MapFaction == null || !faction.IsAtWarWith(main.MapFaction))
            {
                continue;
            }
            yield return p;
        }
    }

    // ------------------------------------------------------------------
    //  desfechos
    // ------------------------------------------------------------------

    private bool SpringCondition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Mission;
        if (AmbushState.PounceTarget == null)
        {
            args.IsEnabled = false;
            args.Tooltip = new TextObject("No unaware enemy is within striking distance.");
        }
        return true;
    }

    private void SpringConsequence(MenuCallbackArgs args)
    {
        MobileParty? target = AmbushState.PounceTarget;
        MobileParty main = MobileParty.MainParty;
        if (target == null || !target.IsActive || target.MapEvent != null)
        {
            AmbushState.PounceTarget = null;
            return;
        }

        AmbushState.ExitStance();
        _cooldownUntilHours = CampaignTime.Now.ToHours + AmbushConfig.CooldownHours;
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, AmbushConfig.TacticsXpOnSpring);

        // Arma a MISSAO como emboscada contra ESTA party (id confere na abertura;
        // expira sozinho se o jogador desistir no encounter).
        AmbushState.Arm(AmbushSide.PlayerAmbushes, target);

        GameMenu.ExitToLast();
        EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
    }

    private bool LetPassCondition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
        args.IsEnabled = AmbushState.PounceTarget != null;
        return true;
    }

    private void LetPassConsequence(MenuCallbackArgs args)
    {
        MobileParty? target = AmbushState.PounceTarget;
        if (target != null)
        {
            _letPassUntil[target.StringId] = CampaignTime.Now.ToHours + 12.0;
            AmbushState.PounceTarget = null;
            UpdateStatusText();
        }
    }

    private void OnSpotted(MobileParty spotter)
    {
        AmbushState.ExitStance();
        // Sem refresh do ignore: a proxima decisao da IA volta a nos considerar.
        MBInformationManager.AddQuickInformation(
            new TextObject($"{spotter.Name}'s scouts have spotted your ambush!"), 0, null, null, "");
        if (Campaign.Current.CurrentMenuContext?.GameMenu?.StringId == MenuId)
        {
            GameMenu.ExitToLast();
        }
    }

    private void LeaveStance()
    {
        AmbushState.ExitStance();
    }

    // ------------------------------------------------------------------
    //  a alavanca vanilla que faz a IA "nao nos ver"
    // ------------------------------------------------------------------

    /// <summary>
    /// A IA de campanha nao consulta dificuldade de avistamento — ela enxerga por
    /// raio (AiEngagePartyBehavior itera hostis e so pula quem tem
    /// ShouldBeIgnored). IgnoreByOtherPartiesTill e o mecanismo vanilla dessa
    /// flag; renovamos por 2h a cada hora enquanto a stance vive, e paramos de
    /// renovar quando ela quebra.
    /// </summary>
    private static void RefreshAiIgnore()
    {
        MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(2f));
    }

    private void CleanupLetPass()
    {
        double now = CampaignTime.Now.ToHours;
        var expired = new List<string>();
        foreach (KeyValuePair<string, double> kv in _letPassUntil)
        {
            if (kv.Value < now)
            {
                expired.Add(kv.Key);
            }
        }
        foreach (string key in expired)
        {
            _letPassUntil.Remove(key);
        }
    }
}
