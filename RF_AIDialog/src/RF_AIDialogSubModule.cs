using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AIDialog
{
    /// <summary>
    /// Entry point do mod RF_AIDialog.
    ///
    /// Responsabilidades:
    ///   1. Registrar AIDialogBehavior (dialog hook) e OllamaTestBehavior (POC) na campanha.
    ///   2. A cada frame (OnApplicationTick), verificar mensagens pendentes
    ///      vindas das tasks de background e exibi-las na tela.
    ///
    /// InformationManager.DisplayMessage DEVE ser chamado da thread principal.
    /// </summary>
    public class RF_AIDialogSubModule : MBSubModuleBase
    {
        private AIDialogBehavior? _dialogBehavior;

        // ── Ciclo de vida ─────────────────────────────────────────────────

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is Campaign &&
                starterObject is CampaignGameStarter campaignStarter)
            {
                // NPCContextStore must be registered first — everything reads from it
                campaignStarter.AddBehavior(new NPCContextStore());

                // WorldHistoryStore must be registered before WorldHistoryBehavior
                campaignStarter.AddBehavior(new WorldHistoryStore());
                campaignStarter.AddBehavior(new WorldHistoryBehavior());

                // NPC initiative — evaluates daily conditions for all lords/notables
                campaignStarter.AddBehavior(new NPCInitiativeBehavior());

                _dialogBehavior = new AIDialogBehavior();
                campaignStarter.AddBehavior(_dialogBehavior);
                _dialogBehavior.AddDialogs(campaignStarter);
            }
        }

        // ── Tick principal (toda frame, thread principal) ─────────────────

        protected override void OnApplicationTick(float dt)
        {
            if (_dialogBehavior == null || !_dialogBehavior.ResponseJustArrived)
                return;

            _dialogBehavior.ResponseJustArrived = false;

            // Só notifica se o jogador ainda está num diálogo
            var cm = Campaign.Current?.ConversationManager;
            if (cm == null || !cm.IsConversationInProgress)
                return;

            // Notificação no HUD — igual ao padrão do AI Influence
            // O texto do botão já mudou para "(Resposta pronta — clique aqui)"
            // via ConditionUpdateWaitText, mas a UI só re-lê quando o jogador
          