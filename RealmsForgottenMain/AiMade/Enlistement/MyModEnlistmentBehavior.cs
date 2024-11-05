
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.MapEvents;
using System;

namespace RealmsForgotten.Behaviors
{
    public class MyModEnlistmentBehavior : CampaignBehaviorBase
    {
        private bool _isEnlisted;
        private Hero _enlistedHero;
        private MobileParty _enlistedParty;
        private float _lastEnlistmentCheckTime;

        // New fields for battle participation
        private string _playerRole = "Infantry"; // Default role
        private FormationClass _formationClass = FormationClass.Infantry; // Default formation

        public bool IsEnlisted => _isEnlisted;
        public MobileParty EnlistedParty => _enlistedParty;
        public Hero EnlistedHero => _enlistedHero;

        public override void RegisterEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            // Custom map event creation will be handled using MapEventManager directly.
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isEnlisted", ref _isEnlisted);
            dataStore.SyncData("_enlistedHero", ref _enlistedHero);
            dataStore.SyncData("_enlistedParty", ref _enlistedParty);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            MyModEnlistmentSettings settings = new MyModEnlistmentSettings();
        }

        private void OnTick(float dt)
        {
            if (IsEnlisted)
            {
                if (_lastEnlistmentCheckTime + 1.0f < Campaign.CurrentTime)
                {
                    _lastEnlistmentCheckTime = Campaign.CurrentTime;
                    CheckEnlistmentStatus();
                }
            }
        }

        private void OnMapEventStarted(MapEvent mapEvent)
        {
            if (IsEnlisted && (EnlistedParty.Party == mapEvent.AttackerSide.LeaderParty || EnlistedParty.Party == mapEvent.DefenderSide.LeaderParty))
            {
                // Add player to the correct formation based on their role
                AssignPlayerToFormation();
            }
        }

        private void OnBattleEnd(MapEvent mapEvent, PartyBase winnerParty, PartyBase loserParty)
        {
            if (IsEnlisted && (EnlistedParty.Party == winnerParty || EnlistedParty.Party == loserParty))
            {
                // Handle any logic after the battle ends (e.g., rewards, promotion checks)
                HandlePostBattleOutcome(mapEvent, winnerParty == EnlistedParty.Party);
            }
        }

        private void AssignPlayerToFormation()
        {
            if (Mission.Current != null && Mission.Current.PlayerTeam != null)
            {
                var playerTeam = Mission.Current.PlayerTeam;

                // Find or create the formation based on the player's role
                Formation playerFormation = playerTeam.GetFormation(_formationClass);

                if (playerFormation == null)
                {
                    // Create a new formation if it doesn't exist
                    playerFormation = new Formation(playerTeam, (int)_formationClass);
                }

                // Add player to the appropriate formation
                playerFormation.PlayerOwner = Agent.Main;
                Mission.Current.MainAgent.Controller = Agent.ControllerType.AI;

                // Set movement order using the correct method
                playerFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
        }

        private void HandlePostBattleOutcome(MapEvent mapEvent, bool playerWon)
        {
            if (playerWon)
            {
                // Reward logic, promotions, etc.
                InformationManager.DisplayMessage(new InformationMessage("You fought bravely and earned rewards!"));
            }
            else
            {
                // Handle loss logic
                InformationManager.DisplayMessage(new InformationMessage("Your party was defeated. Better luck next time!"));
            }
        }

        private void CheckEnlistmentStatus()
        {
            if (_enlistedParty == null || !_enlistedParty.IsActive)
            {
                DischargePlayer();
            }
        }

        private void DischargePlayer()
        {
            _isEnlisted = false;
            _enlistedHero = null;
            _enlistedParty = null;
            InformationManager.DisplayMessage(new InformationMessage("You have been discharged from the enlisted party."));
        }

        public bool EnlistPlayer(Hero targetHero)
        {
            if (targetHero == null || targetHero.PartyBelongedTo == null)
                return false;

            _isEnlisted = true;
            _enlistedHero = targetHero;
            _enlistedParty = targetHero.PartyBelongedTo;

            // Transfer the player to the lord's party
            MobileParty playerParty = MobileParty.MainParty;
            if (playerParty != null)
            {
                // Add the player to the lord's party as a non-leader troop
                _enlistedParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1);

                // Remove the player's party
                playerParty.RemoveParty();

                // Show enlistment message
                InformationManager.DisplayMessage(new InformationMessage($"You have enlisted in {targetHero.Name}'s party."));
            }

            return true;
        }

        public void SetPlayerRole(string role, FormationClass formationClass)
        {
            _playerRole = role;
            _formationClass = formationClass;
        }
    }
}