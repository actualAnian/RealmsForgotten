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
        private int _countOfAbilities;
        private bool _hasCareerAbility;
        private bool _isInitialized;
        private AbilityHudVM _abilityHUD_VM;
        private GauntletLayer _abilityLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            //Mission.Current.OnMainAgentChanged += (o, s) => CheckMainAgent();

            _abilityHUD_VM = new AbilityHudVM();
            _abilityLayer = new GauntletLayer(100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
            MissionScreen.AddLayer(_abilityLayer);
            _isInitialized = true;
        }

        //private void CheckMainAgent()
        //{
        //    if (Agent.Main != null)
        //    {
        //        var component = Agent.Main.GetComponent<AbilityComponent>();
        //        if (component != null)
        //        {
        //            _countOfAbilities = component.KnownAbilitySystem.Count;
        //            var careerAbility = component.CareerAbility;
        //            if (careerAbility != null)
        //            {
        //                _careerAbilityHUD_VM.CareerAbility = careerAbility;
        //                _hasCareerAbility = true;
        //            }
        //            if (_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.FillAbilities(Agent.Main);
        //        }
        //    }
        //}
        public void DisplayErrorMessage(string message)
        {
            //if (_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.DisplayErrorMessage(message);
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
                    if (true)//_hasCareerAbility)
                    {
                        _abilityHUD_VM.RefreshValues();
                    }
                    return;
                }
                _abilityHUD_VM.IsVisible = false;
            }
        }
    }
}
