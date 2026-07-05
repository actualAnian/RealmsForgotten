using System.Linq;
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
                _bombSelectorVM.IsVisible = true;
            else if (_bombSelectorVM.IsVisible && Mission.InputManager.IsKeyPressed(InputKey.Escape))
                _bombSelectorVM.IsVisible = false;

            foreach(var bomb in PlayerBombManager.Instance.PlayerBombs)
            {
                var definiton = bomb.Key;
                var amount = bomb.Value;
                var vmBomb = _bombSelectorVM.Bombs.FirstOrDefault(b => b.StringId == definiton.Item.StringId);
                if (vmBomb == null)
                {
                    _bombSelectorVM.AddBombItem(new(definiton.Item.StringId, definiton.BaseDescription, amount.ToString(), definiton.spriteStringId, _bombSelectorVM.OnBombItemSelected));
                    continue;
                }
                if (vmBomb.Amount != amount.ToString())
                    _bombSelectorVM.DecrementBombAmount(vmBomb);
            }
        }
    }
}
