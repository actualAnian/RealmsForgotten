using Helpers;
using System;
using HarmonyLib;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// Lado de campanha do saque a pe: injeta o controller nas cenas de
    /// assentamento onde saquear faz sentido, e cuida do que acontece DEPOIS —
    /// cair no meio do saque significa acordar numa cela, nao um "game over".
    ///
    /// A fuga e a pericia de Roguery: um ladrao experiente escapa quase sempre; um
    /// novato serve dias ate a sorte virar.
    /// </summary>
    public class RFRaidCampaignBehavior : CampaignBehaviorBase
    {
        private const string CaptiveMenu = "rf_raid_taken_captive";
        private const string ReleasedMenu = "rf_raid_released";

        private bool _isCaptive;
        private int _daysServed;

        /// <summary>Marcado pelo controller quando o jogador cai durante o saque.</summary>
        internal static bool PendingCapture;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("rfRaidIsCaptive", ref _isCaptive);
            dataStore.SyncData("rfRaidDaysServed", ref _daysServed);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenu(CaptiveMenu, "{=rf_raid_captive_text}{RF_RAID_CAPTIVE_TEXT}",
                CaptiveMenuInit, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(CaptiveMenu, "rf_raid_captive_wait",
                "{=rf_raid_captive_wait}Bide your time",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                _ => GameMenu.ExitToLast(), isLeave: true);

            starter.AddGameMenu(ReleasedMenu, "{=rf_raid_released_text}{RF_RAID_RELEASED_TEXT}",
                ReleasedMenuInit, GameMenu.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            starter.AddGameMenuOption(ReleasedMenu, "rf_raid_released_continue",
                "{=rf_raid_continue}Continue",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return true; },
                _ =>
                {
                    MobileParty.MainParty.IsActive = true;
                    PartyBase.MainParty.UpdateVisibilityAndInspected(PartyBase.MainParty.Position, 0f);
                    GameMenu.ExitToLast();
                }, isLeave: true);

            AddStormOption(starter);
        }

        /// <summary>
        /// "Storm the village" — a ponte entre o botao de saque do mapa e o nosso
        /// saque a pe.
        ///
        /// O "Raid the village" vanilla resolve tudo em numeros (um MapEvent
        /// abstrato) e nunca abre cena, por isso o nosso sistema — que so existe
        /// dentro da missao — jamais era consultado. Esta opcao entra no MESMO
        /// submenu, ao lado dele, e faz o caminho oposto: abre a cena da aldeia
        /// com o saque ja declarado.
        /// </summary>
        private void AddStormOption(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("village_hostile_action", "rf_storm_village",
                "{=rf_storm_village}Storm the village",
                StormOnCondition, StormOnConsequence, false, 1);
        }

        private static bool StormOnCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;

            if (!RFRaidConfig.SceneRaidsEnabled)
            {
                return false;
            }

            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || !settlement.IsVillage)
            {
                return false;
            }

            // Mesma regra do saque vanilla: nao se saqueia a propria faccao.
            if (DiplomacyHelper.IsSameFactionAndNotEliminated(Hero.MainHero.MapFaction, settlement.MapFaction))
            {
                return false;
            }

            // Entrar a pe exige alguem para entrar: o jogador precisa estar vivo
            // e no comando da propria party.
            if (Hero.MainHero.IsPrisoner || MobileParty.MainParty == null)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=rf_storm_no_party}You are in no position to lead an attack.");
                return true;
            }

            return true;
        }

        private static void StormOnConsequence(MenuCallbackArgs args)
        {
            // O flag e consumido pelo RFRaidMissionController no primeiro tick em
            // que Agent.Main existir — ver RFRaidState.ConsumeArmedEntry.
            RFRaidState.ArmRaidOnEntry = true;

            try
            {
                VillageEncounter encounter = PlayerEncounter.LocationEncounter as VillageEncounter;
                if (encounter == null)
                {
                    RFRaidState.ArmRaidOnEntry = false;
                    Debug.Print("[RF_Raid] Storm: sem VillageEncounter ativo — a cena nao pode abrir.");
                    return;
                }

                // A mesma chamada que o vanilla usa em "Take a walk through the
                // lands" (PlayerTownVisitCampaignBehavior); a diferenca esta toda
                // no flag acima.
                encounter.CreateAndOpenMissionController(
                    LocationComplex.Current.GetLocationWithId("village_center"));
            }
            catch (Exception ex)
            {
                RFRaidState.ArmRaidOnEntry = false;
                Debug.Print("[RF_Raid] Storm falhou ao abrir a cena: " + ex.Message);
            }
        }

        private void CaptiveMenuInit(MenuCallbackArgs args)
        {
            args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale ? "wait_prisoner_female" : "wait_prisoner_male");
            _isCaptive = true;
            _daysServed = 0;

            Settlement settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;
            TextObject text = new TextObject("{=rf_raid_captive_desc}They dragged you out of the blood and threw you in a cell beneath {TOWN_NAME}. Sooner or later a door will be left unlocked.");
            text.SetTextVariable("TOWN_NAME", settlement != null ? settlement.Name : new TextObject("{=rf_raid_here}this place"));
            MBTextManager.SetTextVariable("RF_RAID_CAPTIVE_TEXT", text.ToString(), false);
        }

        private void ReleasedMenuInit(MenuCallbackArgs args)
        {
            args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale ? "wait_prisoner_female" : "wait_prisoner_male");
            Settlement settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;

            TextObject text = settlement != null && settlement.IsTown
                ? new TextObject("{=rf_raid_released_desc_town}An enemy of {FACTION} had you quietly released. No one says why, and you do not ask.")
                    .SetTextVariable("FACTION", settlement.MapFaction?.Name ?? new TextObject("{=rf_raid_them}them"))
                : new TextObject("{=rf_raid_released_desc}You slipped the lock, took a horse that was not yours, and rode until the walls were out of sight.");

            MBTextManager.SetTextVariable("RF_RAID_RELEASED_TEXT", text.ToString(), false);
        }

        private void OnDailyTick()
        {
            if (!_isCaptive)
            {
                return;
            }

            if (CanEscape(_daysServed))
            {
                _isCaptive = false;
                _daysServed = 0;
                Campaign.Current.SetTimeSpeed(0);
                GameMenu.SwitchToMenu(ReleasedMenu);
                return;
            }
            _daysServed++;
        }

        /// <summary>
        /// Roguery alto sai quase sempre; Roguery baixo depende do tempo servido
        /// somar a favor. Ninguem fica preso para sempre.
        /// </summary>
        private static bool CanEscape(int daysServed)
        {
            int roguery = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery);
            float roll = MBRandom.RandomFloat;
            if (roguery > 60)
            {
                return roll >= 0.2f;
            }
            float chance = Math.Max(1, roguery) / 60f + daysServed / 16f;
            return roll < chance;
        }

        internal static void TakePlayerCaptive()
        {
            PendingCapture = false;
            Campaign.Current?.GameMenuManager?.SetNextMenu(CaptiveMenu);
        }

        // ------------------------------------------------------------------
        //  injecao do controller nas cenas certas
        // ------------------------------------------------------------------

        /// <summary>
        /// Onde o saque a pe pode existir: visita pacifica a aldeia ou cidade.
        /// Castelo, esconderijo, cerco, batalha de campo e sortida ficam de fora —
        /// la o jogo ja tem as proprias regras e nos nao entramos no caminho.
        /// </summary>
        internal static bool CanRaidHere(Mission mission)
        {
            if (mission == null || !RFRaidConfig.SceneRaidsEnabled)
            {
                return false;
            }
            Settlement settlement = MobileParty.MainParty?.CurrentSettlement;
            if (settlement == null || settlement.IsCastle || settlement.IsHideout)
            {
                return false;
            }
            if (settlement.SiegeEvent != null)
            {
                return false;
            }
            return !mission.IsFieldBattle && !mission.IsSiegeBattle && !mission.IsSallyOutBattle;
        }

        private static void AttachController(Mission mission)
        {
            if (!CanRaidHere(mission))
            {
                return;
            }
            try
            {
                mission.AddMissionBehavior(new RFArsonMissionLogic());
                mission.AddMissionBehavior(new RFRaidMissionController());
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Raid] falha ao anexar o controller de saque: " + ex.Message);
            }
        }

        // ATENCAO 1.4.8: o tipo e SandBox.SandBoxMissions (o mod original citava
        // SandBox.Missions.SandBoxMissions, que nao existe nesta versao).
        [HarmonyPatch(typeof(SandBoxMissions), "OpenVillageMission",
            new[] { typeof(string), typeof(Location), typeof(CharacterObject), typeof(string) })]
        internal static class RFRaidVillageMissionPatch
        {
            private static void Postfix(ref Mission __result) => AttachController(__result);
        }

        [HarmonyPatch(typeof(SandBoxMissions), "OpenTownCenterMission",
            new[] { typeof(string), typeof(string), typeof(Location), typeof(CharacterObject), typeof(string) })]
        internal static class RFRaidTownMissionPatch
        {
            private static void Postfix(ref Mission __result) => AttachController(__result);
        }

        [HarmonyPatch(typeof(SandBoxMissions), "OpenIndoorMission",
            new[] { typeof(string), typeof(Location), typeof(CharacterObject), typeof(string) })]
        internal static class RFRaidIndoorMissionPatch
        {
            private static void Postfix(ref Mission __result) => AttachController(__result);
        }
    }
}
