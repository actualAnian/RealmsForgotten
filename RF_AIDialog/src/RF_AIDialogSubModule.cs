using System.Text;
using HarmonyLib;
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
            RFAIDebug.Log("OnSubModuleLoad: start");
            new Harmony("RF_AIDialog").PatchAll(typeof(RF_AIDialogSubModule).Assembly);
            RFAIDebug.Log("OnSubModuleLoad: OK");
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            if (game.GameType is Campaign &&
                starterObject is CampaignGameStarter campaignStarter)
            {
                // NPCContextStore must be registered first — everything reads from it
                campaignStarter.AddBehavior(new NPCContextStore());

                // QuestAtomEngine — tracks VISIT_SETTLEMENT, DEFEAT_PARTY, etc. against real game events
                campaignStarter.AddBehavior(new QuestAtomEngine());

                // WorldHistoryStore must be registered before WorldHistoryBehavior
                campaignStarter.AddBehavior(new WorldHistoryStore());
                campaignStarter.AddBehavior(new WorldHistoryBehavior());

                // NPC initiative — evaluates daily conditions for all lords/notables
                campaignStarter.AddBehavior(new NPCInitiativeBehavior());

                // Betrayal reactions — injects narrative when a clan defects
                campaignStarter.AddBehavior(new BetrayalReactionBehavior());

                // Player reputation — tracks honor, mercy, aggression across the campaign
                campaignStarter.AddBehavior(new PlayerReputationStore());

                // Settlement scars — records conquest history for each fortification
                campaignStarter.AddBehavior(new SettlementScarStore());

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
                    string npcName = _dialogBehavior.CurrentNpcName;
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            $"💬 {npcName} is ready to respond. Click '...' to hear the reply.",
                            Color.FromUint(0xFF_A0_D0_FFu)));
                }
            }

            // ── F8 — World Chronicle ──────────────────────────────────────
            try
            {
                bool keyDown = Input.IsKeyDown(InputKey.F8);

                // Rising edge only — fire once per press, not while held
                if (keyDown && !_chronicleKeyWasDown)
                {
                    // Only open on the campaign map, not mid-conversation or battle
                    bool inConversation = Campaign.Current?.ConversationManager
                                             ?.IsConversationInProgress ?? false;
                    if (Campaign.Current != null && !inConversation)
                        ShowChronicle();
                }

                _chronicleKeyWasDown = keyDown;
            }
            catch { }
        }

        // ── World Chronicle popup ─────────────────────────────────────────

        private static void ShowChronicle()
        {
            try
            {
                var store = WorldHistoryStore.Instance;
                string text;

                if (store == null)
                {
                    text = "The chronicle is not available.";
                }
                else
                {
                    // Show all recorded events (no day cap), newest last
                    var events = store.GetRecentEvents(maxCount: 40, maxDays: 0);

                    if (events.Count == 0)
                    {
                        text = "No significant events have been recorded yet.\n\n" +
                               "The chronicle will fill as wars are declared, settlements " +
                               "change hands, and kingdoms rise and fall.";
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        foreach (var e in events)
                            sb.AppendLine($"[Day {e.Day}]  {e.Description}");
                        text = sb.ToString().TrimEnd();
                    }
                }

                InformationManager.ShowInquiry(new InquiryData(
                    titleText:                "📜 World Chronicle",
                    text:                     text,
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown:    false,
                    affirmativeText:          "Close",
                    negativeText:             "",
                    affirmativeAction:        () => { },
                    negativeAction:           null));
            }
            catch { }
        }
    }
}
