using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Career
{
    public class DivineShieldMissionBehavior : MissionBehavior
    {
        private const float DamageAbsorption = 0.5f; // 50% damage absorption
        private float divineShieldCooldown = 60f; // Cooldown duration in seconds
        private float shieldDuration = 10f; // Shield duration in seconds
        private DivineShieldStateBehavior divineShieldState;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            if (divineShieldState == null)
            {
                divineShieldState = Campaign.Current.GetCampaignBehavior<DivineShieldStateBehavior>();
            }

            if (divineShieldState.CanUseDivineShield && Input.IsKeyPressed(InputKey.D)) // Assuming 'D' key is used for Divine Shield
            {
                ActivateDivineShield();
            }

            // Cooldown management
            if (!divineShieldState.CanUseDivineShield && Mission.Current.CurrentTime - divineShieldState.LastDivineShieldTime >= divineShieldCooldown)
            {
                divineShieldState.CanUseDivineShield = true;
            }

            // Check if shield duration has ended
            if (divineShieldState.IsShieldActive && Mission.Current.CurrentTime >= divineShieldState.ShieldEndTime)
            {
                DeactivateShield();
            }
        }

        public void ActivateDivineShield()
        {
            divineShieldState.CanUseDivineShield = false;
            divineShieldState.IsShieldActive = true;
            divineShieldState.LastDivineShieldTime = Mission.Current.CurrentTime;
            divineShieldState.ShieldEndTime = Mission.Current.CurrentTime + shieldDuration;

            InformationManager.DisplayMessage(new InformationMessage("Divine Shield activated! 50% damage absorption for 10 seconds."));
        }

        private void DeactivateShield()
        {
            divineShieldState.IsShieldActive = false;
            InformationManager.DisplayMessage(new InformationMessage("Divine Shield expired."));
        }

        public bool IsShieldActive()
        {
            return divineShieldState.IsShieldActive;
        }

        public float GetDamageAbsorption()
        {
            return divineShieldState.IsShieldActive ? DamageAbsorption : 0f;
        }

        protected override void OnEndMission()
        {
            divineShieldState.CanUseDivineShield = true;
            divineShieldState.IsShieldActive = false;
        }
    }
}