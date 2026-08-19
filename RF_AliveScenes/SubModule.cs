using System;
using RF_AliveScenes.Config;
using RF_AliveScenes.Missions;
using RF_AliveScenes.Runtime;
using RF_AliveScenes.UI;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RF_AliveScenes;

/// <summary>
/// RF_AliveScenes — fala ambiente com balao sobre a cabeca, em cidade, vila, taverna,
/// salao, batalha, cerco e batalha naval. Baseado no estudo do mod "Alive Scenes"
/// (RF_Warsails_AI/ESTUDO_ALIVESCENES.md), reescrito para o RF.
///
/// Sem Harmony: o behavior entra em OnBeforeMissionBehaviorInitialize e a view e
/// registrada em OnMissionBehaviorInitialize via MissionScreen.AddMissionView, o mesmo
/// caminho ja validado no RF_IsoCam. Isso cobre a batalha naval do War Sails sem
/// precisar conhecer o assembly do DLC.
/// </summary>
public class SubModule : MBSubModuleBase
{
    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Debug.Print("[RF_AliveScenes] SubModule carregado.");
    }

    public override void OnBeforeMissionBehaviorInitialize(Mission mission)
    {
        base.OnBeforeMissionBehaviorInitialize(mission);

        try
        {
            if (!AliveScenesSettings.Instance.Enabled || !ShouldRunHere(mission))
            {
                return;
            }

            BattleSetup battle = BuildBattleSetup();
            bool isSettlementScene = battle == null;

            if (isSettlementScene && !IsSupportedSettlementScene())
            {
                return;
            }

            mission.AddMissionBehavior(new AliveScenesMissionLogic(battle));

            if (isSettlementScene &&
                AliveScenesSettings.Instance.CrowdsEnabled &&
                PlayerEncounter.EncounterSettlement != null &&
                PlayerEncounter.EncounterSettlement.IsTown)
            {
                mission.AddMissionBehavior(new CrowdMissionLogic());
            }
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao preparar a missao: " + e.Message);
        }
    }

    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        base.OnMissionBehaviorInitialize(mission);

        try
        {
            if (mission.GetMissionBehavior<AliveScenesMissionLogic>() == null)
            {
                return;
            }

            if (mission.GetMissionBehavior<AliveScenesBubbleView>() != null)
            {
                return; // view ja registrada nesta missao; nunca duplicar
            }

            MissionState state = MissionState.Current;
            if (state != null && state.CurrentMission == mission && state.Handler is MissionScreen screen)
            {
                screen.AddMissionView(new AliveScenesBubbleView());
            }
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao registrar a view: " + e.Message);
        }
    }

    /// <summary>Cenas onde a fala ambiente atrapalharia (herdado do mod original).</summary>
    private static bool ShouldRunHere(Mission mission)
    {
        if (mission.Scene == null)
        {
            return false;
        }
        if (mission.HasMissionBehavior<CustomBattleAgentLogic>())
        {
            return false;
        }
        if (mission.HasMissionBehavior<ConversationMissionLogic>())
        {
            return false;
        }
        if (mission.HasMissionBehavior<AlleyFightMissionHandler>())
        {
            return false;
        }
        if (mission.HasMissionBehavior<DisguiseMissionLogic>())
        {
            return false;
        }

        string scene = mission.SceneName ?? string.Empty;
        if (scene.Contains("_battle_site_") ||
            scene.Contains("fb_pit_fight_") ||
            scene.Contains("fb_escape") ||
            scene.Contains("safehouse_"))
        {
            return false;
        }

        if (Campaign.Current == null || MobileParty.MainParty == null)
        {
            return false;
        }

        MapEvent mapEvent = MobileParty.MainParty.MapEvent;
        if (mapEvent != null && mapEvent.IsRaid)
        {
            return false;
        }

        return true;
    }

    private static bool IsSupportedSettlementScene()
    {
        Settlement settlement = PlayerEncounter.EncounterSettlement;
        if (settlement == null || settlement.IsHideout)
        {
            return false;
        }
        return settlement.IsTown || settlement.IsVillage;
    }

    /// <summary>
    /// Devolve o retrato da batalha, ou null quando nao e batalha (cena de assentamento).
    /// </summary>
    private static BattleSetup BuildBattleSetup()
    {
        MapEvent mapEvent = MobileParty.MainParty?.MapEvent;
        if (mapEvent == null)
        {
            return null;
        }

        bool isBattle = mapEvent.IsFieldBattle || mapEvent.IsSallyOut || mapEvent.IsSiegeAmbush ||
                        mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsNavalMapEvent;
        if (!isBattle)
        {
            return null;
        }

        BattleSetup setup = new BattleSetup
        {
            IsSiege = mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside || mapEvent.IsSallyOut,
            AtSea = mapEvent.IsNavalMapEvent
        };

        bool playerAttacks = false;
        ReadSide(mapEvent.AttackerSide, setup, ref playerAttacks, true, out string attackerFaction);
        ReadSide(mapEvent.DefenderSide, setup, ref playerAttacks, false, out string defenderFaction);

        setup.AttackerFactionName = attackerFaction;
        setup.DefenderFactionName = defenderFaction;

        float attackerStrength = SafeInverse(mapEvent.AttackerSide.StrengthRatio);
        float defenderStrength = SafeInverse(mapEvent.DefenderSide.StrengthRatio);

        if (playerAttacks)
        {
            setup.PlayerSideOverpowered = attackerStrength > defenderStrength;
            setup.PlayerSideUnderpowered = attackerStrength < defenderStrength;
        }
        else
        {
            setup.PlayerSideOverpowered = attackerStrength < defenderStrength;
            setup.PlayerSideUnderpowered = attackerStrength > defenderStrength;
        }

        return setup;
    }

    private static void ReadSide(MapEventSide side, BattleSetup setup, ref bool playerAttacks, bool isAttackerSide, out string factionName)
    {
        factionName = string.Empty;

        foreach (MapEventParty party in side.Parties)
        {
            if (string.IsNullOrEmpty(factionName) && party.Party?.MapFaction != null)
            {
                factionName = party.Party.MapFaction.Name.ToString();
            }

            if (party.Party == MobileParty.MainParty.Party)
            {
                playerAttacks = isAttackerSide;
            }

            MobileParty mobileParty = party.Party?.MobileParty;
            if (mobileParty == null)
            {
                continue;
            }
            if (mobileParty.IsVillager)
            {
                setup.AgainstVillagers = true;
            }
            if (mobileParty.IsBandit)
            {
                setup.AgainstLooters = true;
            }
        }
    }

    private static float SafeInverse(float ratio) => Math.Abs(ratio) < 0.0001f ? 0f : 1f / ratio;
}
