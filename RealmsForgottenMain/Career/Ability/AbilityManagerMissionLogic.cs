using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Ability
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private readonly bool hasAbility;
        readonly ClassAbility? ability;
        //private AbilityHUDMissionView? _abilityView;

        public AbilityManagerMissionLogic() 
        {
            var career = PlayerCareerExtension.GetCareer();
            if (career != null)
            {
                hasAbility = true;
                ability = career.Ability;
            }
        }
        public override void OnMissionTick(float dt)
        {
            if (!hasAbility || Input.IsKeyDown(InputKey.Tab) || !Input.IsKeyDown(InputKey.E) || Agent.Main == null || !Globals.IsCurrentMainAgentPlayerHero())
                return;

            if (!ability!.IsDisabled(Agent.Main) && !ability.IsOnCooldown())
            {
                ability.TryActivate(Agent.Main);
            }
            else
            {
                //_abilityView.DisplayErrorMessage(failureReason.ToString());
            }
        }

        public override void EarlyStart()
        {
            //_abilityView = Mission.Current.GetMissionBehavior<AbilityHUDMissionView>();
        }
    }
}
