using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_IsoCam
{
    /// <summary>
    /// Entry point. RF_IsoCam registers its MissionView in EVERY mission via
    /// OnMissionBehaviorInitialize (verified public virtual on MBSubModuleBase in 1.4.8),
    /// which runs for field battles, sieges, hideouts, arena AND settlement walkabout
    /// (towns / villages / castles). The view is inert until the player toggles it, and
    /// only actually drives the camera while there is a controllable Agent.Main.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Debug.Print("[RF_IsoCam] SubModule loaded.");
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            try
            {
                mission.AddMissionBehavior(new IsometricCameraMissionView());
                Debug.Print("[RF_IsoCam] IsometricCameraMissionView added to mission.");
            }
            catch (Exception e)
            {
                Debug.Print("[RF_IsoCam] Failed to add MissionView: " + e.Message);
            }
        }
    }
}
