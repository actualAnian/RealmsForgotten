using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RealmsForgotten.Raiding.Patches
{
    // ------------------------------------------------------------------
    //  REGRA DE OURO destes patches: fora de um saque em curso todos eles
    //  devolvem true (ou nao mexem em nada) e o jogo se comporta como vanilla.
    //  Alvos conferidos contra a 1.4.8 em 2026-08-27 — ATENCAO as duas
    //  correcoes de namespace vs o mod original:
    //    LeaveMissionLogic -> SandBox.Missions.MissionLogics (nao TaleWorlds.MountAndBlade)
    //    SandBoxMissions   -> SandBox (nao SandBox.Missions)
    // ------------------------------------------------------------------

    /// <summary>
    /// Destrava bater em civil. O vanilla cancela o dano quando atacante e vitima
    /// nao sao inimigos (e o que impede socar aldeoes numa visita normal); durante
    /// o saque isso precisa sair do caminho, e SO durante ele.
    /// </summary>
    [HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
    internal static class RFRaidAllowCivilianDamagePatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!RFRaidState.RaidInProgress)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// Passagens trancadas: ninguem entra numa casa (nem o jogador) no meio do
    /// saque — e assim que os alde\u00f5es sao obrigados a fugir pela rua em vez de
    /// sumirem por uma porta.
    /// ATENCAO: o RF_Homesteads tambem patcheia PassageUsePoint.OnUse. Dois
    /// prefixes convivem no Harmony (ambos rodam ate um devolver false), mas se
    /// aquele patch mudar, reavaliar aqui.
    /// </summary>
    [HarmonyPatch(typeof(PassageUsePoint), "OnUse")]
    internal static class RFRaidLockPassagesPatch
    {
        private static bool Prefix()
        {
            if (!RFRaidState.RaidInProgress)
            {
                return true;
            }
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=rf_raid_locked}The door is barred!").ToString()));
            return false;
        }
    }

    /// <summary>
    /// Segura o "sair da missao" do vanilla enquanto o saque corre: sem isto o
    /// jogo devolve o jogador ao menu do assentamento no meio da briga.
    /// </summary>
    [HarmonyPatch(typeof(LeaveMissionLogic), "OnMissionTick")]
    internal static class RFRaidBlockLeavePatch
    {
        private static bool Prefix()
        {
            return !RFRaidState.RaidInProgress;
        }
    }

    /// <summary>
    /// Bandeira negra: suprime a hostilidade do encounter UMA vez (a escolha do
    /// jogador no menu). Consumo one-shot para nunca virar um bloqueio global.
    /// </summary>
    [HarmonyPatch(typeof(BeHostileAction), nameof(BeHostileAction.ApplyEncounterHostileAction))]
    internal static class RFBlackBannerHostilityPatch
    {
        private static bool Prefix()
        {
            return !RFRaidState.ConsumeHostilitySuppression();
        }
    }

    /// <summary>Tropas do jogador entram de preto no ataque sob bandeira negra.</summary>
    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    internal static class RFBlackBannerAgentColorPatch
    {
        private const string BlackBannerCode =
            "19.116.116.1836.1836.768.788.1.0.-30.503.116.116.240.240.948.1097.1.0.0.503.116.116.240.240.844.1155.1.0.0";

        private static void Prefix(AgentBuildData agentBuildData)
        {
            if (!RFRaidState.BlackBannerActive || agentBuildData?.AgentTeam == null || !agentBuildData.AgentTeam.IsPlayerTeam)
            {
                return;
            }
            Banner banner = new Banner(BlackBannerCode);
            agentBuildData.Banner(banner);
            agentBuildData.ClothingColor1(banner.GetPrimaryColor());
            agentBuildData.ClothingColor2(banner.GetPrimaryColor());
        }
    }

    /// <summary>
    /// UI de ordens nas cenas de aldeia e cidade: o vanilla nao a monta ali, e sem
    /// ela as tropas que voce leva para o saque ficam sem comando. Vale sozinho,
    /// mesmo sem saque.
    /// </summary>
    [HarmonyPatch(typeof(SandBoxMissionViews), "OpenVillageMission")]
    internal static class RFRaidVillageOrderViewPatch
    {
        private static void Postfix(ref MissionView[] __result)
        {
            __result = RFRaidViewHelper.WithOrderUI(__result);
        }
    }

    [HarmonyPatch(typeof(SandBoxMissionViews), "OpenTownCenterMission")]
    internal static class RFRaidTownOrderViewPatch
    {
        private static void Postfix(ref MissionView[] __result)
        {
            __result = RFRaidViewHelper.WithOrderUI(__result);
        }
    }

    internal static class RFRaidViewHelper
    {
        public static MissionView[] WithOrderUI(MissionView[] views)
        {
            if (views == null || !RFRaidConfig.SceneRaidsEnabled || !RFRaidConfig.OrderUIEnabled)
            {
                return views;
            }
            List<MissionView> list = views.ToList();
            list.Add(ViewCreator.CreateMissionOrderUIHandler(null));
            list.Add(ViewCreator.CreateOrderTroopPlacerView(null));
            return list.ToArray();
        }
    }
}
