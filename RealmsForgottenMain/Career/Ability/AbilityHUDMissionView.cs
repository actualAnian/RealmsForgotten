using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.Career.Ability
{
    [DefaultView]
    class AbilityHUDMissionView : MissionView
    {
        private bool _hasCareerAbility;
        private bool _isInitialized;
        private AbilityHudVM? _abilityHUD_VM;
        private GauntletLayer? _abilityLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _abilityHUD_VM = new AbilityHudVM();
            _abilityLayer = new GauntletLayer(100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
            MissionScreen.AddLayer(_abilityLayer);
            _hasCareerAbility = PlayerCareerExtension.GetCareer().Ability.IsEnabled;
            _isInitialized = true;
        }
        public override void OnMissionTick(float dt)
        {
            if (_isInitialized)
            {
                bool canHudBeVisible = Agent.Main != null &&
                                        Agent.Main.State == AgentState.Active &&
                                        (Mission.Current.Mode == MissionMode.Battle ||
                                        Mission.Current.Mode == MissionMode.Stealth) &&
                                        MissionScreen.CustomCamera == null &&
                                        !MissionScreen.IsViewingCharacter() &&
                                        !MissionScreen.IsPhotoModeEnabled &&
                                        !ScreenManager.GetMouseVisibility();
                if (canHudBeVisible)
                {
                    if (_hasCareerAbility)
                    {
                        _abilityHUD_VM?.RefreshValues();
                    }
                    return;
                }
                if (_abilityHUD_VM != null) _abilityHUD_VM.IsVisible = false;
            }
        }
    }
}
