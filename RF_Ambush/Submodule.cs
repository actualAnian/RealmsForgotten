using System;
using RF_Ambush.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_Ambush;

/// <summary>
/// RF_Ambush — emboscada de verdade, sem contato previo (2026-08-03).
/// Design: RF_Warsails_AI/ESTUDO_TOTALAMBUSH.md.
///
///   Fase 1: tecla B no mapa arma a stance; Scouting vs Scouting decide quem ve
///           quem; saltar abre a batalha com o inimigo em coluna de marcha.
///   Fase 2: parties hostis invisiveis com batedor melhor podem nos emboscar;
///           Scouting alto vira aviso ("sinais de emboscada") em vez de surpresa.
///
/// A missao e anexada via OnMissionStartedEvent quando ha gatilho armado para a
/// party inimiga DESTA batalha (id conferido; expira sozinho) — nada de Harmony.
/// </summary>
public sealed class Submodule : MBSubModuleBase
{
    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);
        if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter starter)
        {
            starter.AddBehavior(new AmbushStanceBehavior());
            starter.AddBehavior(new EnemyAmbushBehavior());
            starter.AddBehavior(new AmbushMissionAttacher());
        }
    }
}

/// <summary>
/// Anexa o behavior de emboscada a missao certa. Behavior separado (e nao codigo
/// no Submodule) para poder assinar CampaignEvents.
/// </summary>
public sealed class AmbushMissionAttacher : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnMissionStarted(IMission imission)
    {
        if (imission is not Mission mission)
        {
            return;
        }
        try
        {
            MobileParty? enemyParty = ResolveEnemyParty();
            if (!AmbushState.TryConsume(enemyParty, out AmbushSide side))
            {
                return;
            }
            if (side == AmbushSide.PlayerAmbushes)
            {
                mission.AddMissionBehavior(new PlayerAmbushMissionBehavior());
            }
            else
            {
                mission.AddMissionBehavior(new EnemyAmbushMissionBehavior());
            }
        }
        catch (Exception)
        {
            AmbushState.Disarm();
        }
    }

    /// <summary>Party inimiga do map event atual, para conferir com o gatilho armado.</summary>
    private static MobileParty? ResolveEnemyParty()
    {
        var mapEvent = MobileParty.MainParty?.MapEvent;
        if (mapEvent == null)
        {
            return null;
        }
        try
        {
            PartyBase? leader = mapEvent.GetLeaderParty(mapEvent.PlayerSide == BattleSideEnum.Attacker
                ? BattleSideEnum.Defender
                : BattleSideEnum.Attacker);
            return leader?.MobileParty;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
