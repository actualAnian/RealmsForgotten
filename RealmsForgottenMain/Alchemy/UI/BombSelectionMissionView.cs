using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.Alchemy.UI
{
    [DefaultView]
    public class BombSelectionMissionView : MissionView
    {
        private BombSelectorVM _bombSelectorVM;
        private GauntletLayer _gauntletLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _bombSelectorVM = new BombSelectorVM();
            _gauntletLayer = new GauntletLayer("BombSelectionLayer", 120);
            _gauntletLayer.LoadMovie("BombSelection", _bombSelectorVM);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnMissionTick(float dt)
        {
            if (_bombSelectorVM == null)
                return;

            if (!_bombSelectorVM.IsVisible && Mission.InputManager.IsKeyPressed(InputKey.H))
            {
                _bombSelectorVM.IsVisible = true;
            }
            else if (_bombSelectorVM.IsVisible && Mission.InputManager.IsKeyPressed(InputKey.Escape))
            {
                _bombSelectorVM.IsVisible = false;
            }
        }
    }
}
