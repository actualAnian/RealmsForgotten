using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_PartyVisuals
{
    /// <summary>
    /// Entry point. Registers the campaign behavior and applies the Harmony patch, both scoped
    /// to a real Campaign in OnGameStart (never at module load). We patch ONLY
    /// MobilePartyVisualManager.OnVisualTick — a per-frame sync hook. We deliberately do NOT
    /// patch MobilePartyVisual.AddMobileIconComponents: patching that at load corrupts agent
    /// poses game-wide (the "folded characters" bug documented in RF_Settlers).
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "rf.partyvisuals";
        private static Harmony _harmony;

        /// <summary>
        /// O modulo virou projeto dentro do RealmsForgotten (2026-08-18) mas a pasta
        /// Modules/RF_PartyVisuals antiga continua no disco. Se as duas estiverem ligadas no
        /// launcher, o jogo instancia esta classe duas vezes: o guarda estatico garante que o
        /// behavior e o patch entram uma unica vez.
        /// </summary>
        private static Game _initializedGame;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Debug.Print("[RF_PartyVisuals] SubModule loaded.");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            try
            {
                if (game.GameType is Campaign && gameStarter is CampaignGameStarter starter)
                {
                    if (_initializedGame == game)
                    {
                        Debug.Print("[RF_PartyVisuals] Ja inicializado nesta partida (modulo carregado duas vezes?) — ignorando.");
                        return;
                    }
                    _initializedGame = game;

                    starter.AddBehavior(new PartyVisualsBehavior());

                    if (_harmony == null)
                    {
                        _harmony = new Harmony(HarmonyId);
                        _harmony.PatchAll(typeof(SubModule).Assembly);
                        Debug.Print("[RF_PartyVisuals] Harmony patches applied.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_PartyVisuals] OnGameStart error: " + e.Message);
            }
        }
    }
}
