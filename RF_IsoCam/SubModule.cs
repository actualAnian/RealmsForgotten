using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

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
                // This hook fires inside Mission.AfterStart(), which runs AFTER the
                // MissionScreen has already snapshotted Mission.MissionBehaviors into its
                // ticking container (Handler.OnMissionAfterStarting). A view added here via
                // mission.AddMissionBehavior() is therefore never registered and never gets
                // OnMissionScreenTick. MissionScreen.AddMissionView() is the runtime-add API:
                // it adds the behavior AND registers it for ticking.
                IsometricCameraMissionView view = new IsometricCameraMissionView();
                MissionState state = MissionState.Current;
                if (state != null && state.CurrentMission == mission && state.Handler is MissionScreen screen)
                {
                    screen.AddMissionView(view);
                    Debug.Print("[RF_IsoCam] IsometricCameraMissionView registered via MissionScreen.AddMissionView.");
                }
                else
                {
                    mission.AddMissionBehavior(view);
                    Debug.Print("[RF_IsoCam] WARNING: MissionScreen unavailable, view added unregistered (camera inactive).");
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_IsoCam] Failed to add MissionView: " + e.Message);
            }
        }
    }
}
