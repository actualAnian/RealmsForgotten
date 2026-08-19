using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Messengers
{
    internal enum MessengerHandleResult
    {
        WaitForNextTick,
        AwaitingUserInput,
        RemoveFromList,
    }

    /// <summary>
    /// Mensageiro pago: viaja pelo mapa por alguns dias, pode se perder, e na chegada
    /// oferece uma CONVERSA À DISTÂNCIA com o herói alvo — sem o jogador ir até ele.
    ///
    /// Portado do LOTRAOMMessengerManager (código MIT), adaptado ao RF: sem IoC, custo e
    /// dias em constantes, assinaturas verificadas nas DLLs 1.4.8
    /// (OpenConversationMission ganhou um bool; IMissionListener trocou
    /// OnInitialDeploymentPlanMade por OnDeploymentPlanMade(Team, bool)).
    ///
    /// Mecânica herdada e mantida: o Bannerlord não tem "conversar à distância", então a
    /// chegada sintetiza um PlayerEncounter contra a party do alvo, roda a missão de
    /// conversa e desfaz tudo — inclusive devolvendo a party do jogador à posição salva,
    /// já que entrar em settlement a move.
    /// </summary>
    public class RFMessengerManager : IMissionListener
    {
        public const int MessengerGoldCost = 100;
        public const int MessengerTravelDays = 3;
        private const bool EnableAccidents = true;
        // 0.05%/hora ~= 3,5% de perda numa viagem de 3 dias. O valor herdado do LOTRAOM
        // (0.2%/h ~= 13% por viagem) perdia 1 em 7 mensageiros — punitivo demais para 100
        // denares sem reembolso (feedback de teste, 2026-08-18).
        private const float MessengerAccidentChance = 0.0005f;

        private List<RFMessenger> _messengers = new List<RFMessenger>();

        private Mission _currentMission;
        private RFMessenger _activeMessenger;
        private Vec2 _originalPosition = Vec2.Invalid;
        private float _messengerSpeed;
        private bool _processingArrivedMessenger;

        private static readonly List<MissionMode> AllowedMissionModes = new List<MissionMode>
        {
            MissionMode.Conversation,
            MissionMode.Barter,
        };

        public void Initialize()
        {
            CalculateMessengerSpeed();
        }

        public void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfMessengers", ref _messengers);

            if (dataStore.IsLoading)
            {
                _messengers = _messengers ?? new List<RFMessenger>();
                _processingArrivedMessenger = false;
                CalculateMessengerSpeed();
            }
        }

        private void CalculateMessengerSpeed()
        {
            _messengerSpeed = Campaign.MapDiagonal / (24f * Math.Max(MessengerTravelDays, 1));
        }

        public void SendMessenger(Hero targetHero)
        {
            if (!CanSendMessenger(targetHero, out TextObject reason))
            {
                InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=rf_msgr_cannot_send}Cannot Send Messenger").ToString(),
                    reason.ToString(),
                    true, false,
                    GameTexts.FindText("str_ok").ToString(),
                    string.Empty,
                    null, null), false, false);
                return;
            }

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, MessengerGoldCost, false);
            _messengers.Add(new RFMessenger(targetHero, CampaignTime.Now));

            TextObject text = new TextObject("{=rf_msgr_sent_body}A messenger has been dispatched to {HERO_NAME} and should arrive within {DAYS} days.");
            text.SetTextVariable("HERO_NAME", targetHero.Name);
            text.SetTextVariable("DAYS", MessengerTravelDays);

            InformationManager.ShowInquiry(new InquiryData(
                new TextObject("{=rf_msgr_sent_title}Messenger Sent").ToString(),
                text.ToString(),
                true, false,
                GameTexts.FindText("str_ok").ToString(),
                string.Empty,
                null, null), false, false);
        }

        public bool CanSendMessenger(Hero targetHero, out TextObject reason)
        {
            reason = new TextObject("{=!}");

            if (targetHero == null)
            {
                reason = new TextObject("{=rf_msgr_invalid}Invalid target.");
                return false;
            }

            if (Hero.MainHero.Gold < MessengerGoldCost)
            {
                reason = new TextObject("{=rf_msgr_no_gold}Not enough gold! You need {COST} denars.");
                reason.SetTextVariable("COST", MessengerGoldCost);
                return false;
            }

            if (_messengers.Any(m => m.TargetHero == targetHero && !m.Arrived))
            {
                reason = new TextObject("{=rf_msgr_already}A messenger has already been dispatched to this person.");
                return false;
            }

            if (!IsTargetAvailable(targetHero))
            {
                reason = GetUnavailabilityReason(targetHero);
                return false;
            }

            return true;
        }

        public void UpdateMessengers()
        {
            if (_messengers == null || _messengers.Count == 0)
            {
                return;
            }

            CalculateMessengerSpeed();

            List<RFMessenger> toRemove = new List<RFMessenger>();

            foreach (RFMessenger messenger in _messengers.ToList())
            {
                if (messenger.TargetHero == null || messenger.TargetHero.IsDead)
                {
                    toRemove.Add(messenger);
                    continue;
                }

                if (messenger.Arrived)
                {
                    // Um de cada vez: dois diálogos/encounters simultâneos quebram o fluxo.
                    if (!_processingArrivedMessenger)
                    {
                        if (HandleArrivedMessenger(messenger) == MessengerHandleResult.RemoveFromList)
                        {
                            toRemove.Add(messenger);
                        }
                    }
                    continue;
                }

                UpdateMessengerPosition(messenger);

                // Quem acabou de chegar nao rola mais o dado da estrada.
                if (!messenger.Arrived && CheckMessengerAccident(messenger))
                {
                    toRemove.Add(messenger);
                }
            }

            foreach (RFMessenger messenger in toRemove)
            {
                _messengers.Remove(messenger);
            }
        }

        private void UpdateMessengerPosition(RFMessenger messenger)
        {
            if (messenger.DispatchTime.ElapsedDaysUntilNow >= MessengerTravelDays)
            {
                messenger.Arrived = true;
                return;
            }

            Vec2 targetPosition = messenger.TargetHero.GetMapPoint()?.Position.ToVec2() ?? Vec2.Invalid;
            if (targetPosition.IsValid)
            {
                Vec2 direction = targetPosition - messenger.CurrentPosition;
                if (direction.Length <= _messengerSpeed)
                {
                    messenger.Arrived = true;
                }
                else
                {
                    messenger.CurrentPosition += direction.Normalized() * _messengerSpeed;
                }
            }
        }

        private bool CheckMessengerAccident(RFMessenger messenger)
        {
            if (!EnableAccidents || MBRandom.RandomFloat >= MessengerAccidentChance)
            {
                return false;
            }

            TextObject text = new TextObject("{=rf_msgr_lost_body}Your messenger to {HERO_NAME} was ambushed on the road and never arrived.");
            text.SetTextVariable("HERO_NAME", messenger.TargetHero.Name);

            InformationManager.ShowInquiry(new InquiryData(
                new TextObject("{=rf_msgr_lost_title}Messenger Lost").ToString(),
                text.ToString(),
                true, false,
                GameTexts.FindText("str_ok").ToString(),
                string.Empty,
                null, null), false, false);
            return true;
        }

        private MessengerHandleResult HandleArrivedMessenger(RFMessenger messenger)
        {
            if (messenger.TargetHero.PartyBelongedTo == Hero.MainHero.PartyBelongedTo)
            {
                return MessengerHandleResult.RemoveFromList; // alvo entrou na nossa party no caminho
            }

            if (!IsPlayerAvailable() || !IsTargetAvailableNow(messenger.TargetHero))
            {
                return MessengerHandleResult.WaitForNextTick;
            }

            _processingArrivedMessenger = true;

            TextObject arrivalText = new TextObject("{=rf_msgr_arrived_body}Your messenger has reached {HERO_NAME}. Do you wish to speak with them?");
            arrivalText.SetTextVariable("HERO_NAME", messenger.TargetHero.Name);

            InformationManager.ShowInquiry(new InquiryData(
                    new TextObject("{=rf_msgr_arrived_title}Messenger Arrived").ToString(),
                    arrivalText.ToString(),
                    true, true,
                    new TextObject("{=rf_msgr_speak}Speak").ToString(),
                    new TextObject("{=rf_msgr_dismiss}Dismiss").ToString(),
                    () => StartMessengerConversation(messenger),
                    () => DismissMessenger(messenger)),
                true, false);

            return MessengerHandleResult.AwaitingUserInput;
        }

        private void DismissMessenger(RFMessenger messenger)
        {
            _messengers.Remove(messenger);
            _processingArrivedMessenger = false;
        }

        private void StartMessengerConversation(RFMessenger messenger)
        {
            if (_activeMessenger != null || _currentMission != null)
            {
                return;
            }

            try
            {
                _activeMessenger = messenger;
                Hero targetHero = messenger.TargetHero;
                PartyBase mainParty = PartyBase.MainParty;

                // Andarilho ainda não spawnado: materializa no assentamento natal.
                if (targetHero.IsWanderer && targetHero.HeroState == Hero.CharacterStates.NotSpawned)
                {
                    targetHero.ChangeState(Hero.CharacterStates.Active);
                    EnterSettlementAction.ApplyForCharacterOnly(targetHero, targetHero.BornSettlement);
                }

                Settlement currentSettlement = targetHero.CurrentSettlement;
                PartyBase targetParty = currentSettlement != null
                    ? currentSettlement.Party ?? targetHero.BornSettlement?.Party
                    : targetHero.PartyBelongedTo?.Party ?? targetHero.BornSettlement?.Party;

                PlayerEncounter.Start();
                PlayerEncounter.Current.SetupFields(mainParty, targetParty ?? mainParty);
                Campaign.Current.CurrentConversationContext = ConversationContext.Default;

                if (currentSettlement != null)
                {
                    _originalPosition = Hero.MainHero.GetMapPoint().Position.ToVec2();
                    PlayerEncounter.EnterSettlement();

                    Location targetLocation = LocationComplex.Current.GetLocationOfCharacter(targetHero);
                    Location playerLocation = LocationComplex.Current.GetLocationOfCharacter(Hero.MainHero);

                    CampaignEventDispatcher.Instance.OnPlayerStartTalkFromMenu(targetHero);
                    _currentMission = (Mission)PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(
                        targetLocation, playerLocation, targetHero.CharacterObject, null);
                }
                else
                {
                    _originalPosition = Vec2.Invalid;
                    _currentMission = (Mission)Campaign.Current.CampaignMissionManager.OpenConversationMission(
                        new ConversationCharacterData(Hero.MainHero.CharacterObject, mainParty, true, false, false, false, false, false),
                        new ConversationCharacterData(targetHero.CharacterObject, targetParty, true, false, false, false, false, false),
                        "", "", false);
                }

                _currentMission?.AddListener(this);
            }
            catch (Exception e)
            {
                Debug.Print("[RF_Messengers] Falha ao abrir a conversa do mensageiro: " + e);
                _activeMessenger = null;
                _processingArrivedMessenger = false;
                _messengers.Remove(messenger);
                try { PlayerEncounter.Finish(true); } catch { }
            }
        }

        private static TextObject GetUnavailabilityReason(Hero targetHero)
        {
            if (targetHero.IsDead)
            {
                TextObject dead = new TextObject("{=rf_msgr_dead}{HERO_NAME} is dead.");
                dead.SetTextVariable("HERO_NAME", targetHero.Name);
                return dead;
            }

            if (targetHero.IsPrisoner)
            {
                TextObject prisoner = new TextObject("{=rf_msgr_prisoner}{HERO_NAME} is imprisoned and cannot receive messengers.");
                prisoner.SetTextVariable("HERO_NAME", targetHero.Name);
                return prisoner;
            }

            if (targetHero.IsFugitive)
            {
                TextObject fugitive = new TextObject("{=rf_msgr_fugitive}{HERO_NAME} is a fugitive and cannot be found.");
                fugitive.SetTextVariable("HERO_NAME", targetHero.Name);
                return fugitive;
            }

            if (targetHero.IsChild)
            {
                TextObject child = new TextObject("{=rf_msgr_child}{HERO_NAME} is too young to receive messengers.");
                child.SetTextVariable("HERO_NAME", targetHero.Name);
                return child;
            }

            return new TextObject("{=rf_msgr_unavailable}This person cannot be reached by messenger at this time.");
        }

        // ------------------------------------------------------------ IMissionListener

        public void OnEndMission()
        {
            if (_activeMessenger != null)
            {
                _messengers.Remove(_activeMessenger);
                _activeMessenger = null;
            }

            _currentMission?.RemoveListener(this);
            _currentMission = null;
            _processingArrivedMessenger = false;

            // A limpeza precisa esperar a missão morrer de verdade: um tick de atraso.
            CampaignEvents.TickEvent.AddNonSerializedListener(this, CleanUpSettlementEncounter);
        }

        private void CleanUpSettlementEncounter(float dt)
        {
            try
            {
                PlayerEncounter.Finish(true);

                if (_originalPosition.IsValid && MobileParty.MainParty != null)
                {
                    MobileParty.MainParty.Position = new CampaignVec2(_originalPosition, false);
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_Messengers] Limpeza do encounter falhou: " + e.Message);
            }

            _originalPosition = Vec2.Invalid;
            CampaignEvents.TickEvent.ClearListeners(this);
        }

        public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            if (_currentMission != null &&
                !AllowedMissionModes.Contains(_currentMission.Mode) &&
                AllowedMissionModes.Contains(oldMissionMode))
            {
                _currentMission.EndMission();
            }
        }

        private static bool IsTargetAvailable(Hero hero)
        {
            return hero != null &&
                   hero.IsAlive &&
                   !hero.IsPrisoner &&
                   !hero.IsChild &&
                   !hero.IsHumanPlayerCharacter &&
                   (hero.IsActive || (hero.IsWanderer && hero.HeroState == Hero.CharacterStates.NotSpawned));
        }

        private static bool IsTargetAvailableNow(Hero hero)
        {
            return IsTargetAvailable(hero) && hero.PartyBelongedTo?.MapEvent == null;
        }

        private static bool IsPlayerAvailable()
        {
            return PartyBase.MainParty != null &&
                   PlayerEncounter.Current == null &&
                   Mission.Current == null &&
                   GameStateManager.Current.ActiveState is MapState mapState &&
                   !mapState.AtMenu;
        }

        // Métodos vazios exigidos pela interface (assinaturas 1.4.8).
        public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType) { }
        public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType) { }
        public void OnConversationCharacterChanged() { }
        public void OnResetMission() { }
        public void OnDeploymentPlanMade(Team team, bool isFirstPlan) { }
    }
}
