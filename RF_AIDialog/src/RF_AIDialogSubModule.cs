using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AIDialog
{
    /// <summary>
    /// Entry point do mod RF_AIDialog.
    ///
    /// Responsabilidades:
    ///   1. Registrar todos os CampaignBehaviors na campanha.
    ///   2. A cada frame (OnApplicationTick):
    ///      a) Verificar mensagens pendentes vindas das tasks de background.
    ///      b) Detectar F8 para abrir o World Chronicle.
    ///
    /// InformationManager.DisplayMessage/ShowInquiry DEVEM ser chamados da thread principal.
    /// </summary>
    public class RF_AIDialogSubModule : MBSubModuleBase
    {
        private AIDialogBehavior? _dialogBehavior;
        private bool _chronicleKeyWasDown = false;

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
            // ── AI response notification ──────────────────────────────────
            if (_dialogBehavior != null && _dialogBehavior.ResponseJustArrived)
            {
                _dialogBehavior.ResponseJustArrived = false;

                var cm = Campaign.Current?.ConversationManager;
                if (cm != null && cm.IsConversationInProgress)
                {
                    string npcName = _dialogBehavior.CurrentNp