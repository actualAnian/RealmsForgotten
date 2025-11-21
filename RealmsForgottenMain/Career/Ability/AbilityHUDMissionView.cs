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
        private AbilityHudVM? _abilityHudVM;
        private GauntletLayer? _abilityLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _abilityHudVM = new AbilityHudVM();
            _abilityLayer = new GauntletLayer("AbilityHUD", 100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHudVM);
            MissionScreen.AddLayer(_abilityLayer);
            var career = PlayerCareerExtension.GetCareer();
            _hasCareerAbility = career != null && career.Ability.IsEnabled;
            _isInitialized = _hasCareerAbility;
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
                                        !ScreenManager.GetMouseVisibility() &&
                                        Globals.IsCurrentMainAgentPlayerHero();
                if (canHudBeVisible)
                {
                    if (_hasCareerAbility)
                    {
                        _abilityHudVM?.RefreshValues();
                    }
                    return;
                }
                if (_abilityHudVM != null) _abilityHudVM.IsVisible = false;
            }
        }
    }
}
