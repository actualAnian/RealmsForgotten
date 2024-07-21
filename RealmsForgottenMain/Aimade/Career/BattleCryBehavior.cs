using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Career
{
    public class BattleCryMissionBehavior : MissionBehavior
    {
        private const float DamageAbsorption = 0.5f; // 50% damage absorption
        private float battleCryCooldown = 30f; // Cooldown duration in seconds
        private BattleCryStateBehavior battleCryState;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            if (battleCryState == null)
            {
                battleCryState = Campaign.Current.GetCampaignBehavior<BattleCryStateBehavior>();
            }

            if (battleCryState.CanUseBattleCry && Input.IsKeyPressed(InputKey.B)) // Assuming 'B' key is used for Battle Cry
            {
                UseBattleCry();
            }

            // Cooldown management
            if (!battleCryState.CanUseBattleCry && Mission.Current.CurrentTime - battleCryState.LastBattleCryTime >= battleCryCooldown)
            {
                battleCryState.CanUseBattleCry = true;
            }
        }

        private void UseBattleCry()
        {
            battleCryState.CanUseBattleCry = false;
            battleCryState.LastBattleCryTime = Mission.Current.CurrentTime;
            var playerHero = Hero.MainHero.CharacterObject;

            foreach (Agent agent in Mission.Current.AllAgents)
            {
                if (agent.Team == Agent.Main.Team && agent != Agent.Main)
                {
                    AdjustMorale(agent, 20); // Adjust morale by 20
                }
            }

            InformationManager.DisplayMessage(new InformationMessage($"{playerHero.Name} used Battle Cry! Allies' morale boosted!"));
        }

        private void AdjustMorale(Agent agent, float moraleBoost)
        {
            agent.SetMorale(agent.GetMorale() + moraleBoost); // Adjust this line with the correct method to boost morale
        }

        protected override void OnEndMission()
        {
            battleCryState.CanUseBattleCry = true;
        }
    }
}
