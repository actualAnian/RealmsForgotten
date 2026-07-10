using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RealmsForgotten.Alchemy.UI
{
    [DefaultView]
    public class BombSelectionMissionView : MissionView
    {
        private BombSelectorVM? _bombSelectorVM;
        private GauntletLayer? _gauntletLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _bombSelectorVM = new BombSelectorVM();
            _gauntletLayer = new GauntletLayer("BombSelectionLayer", 120);
            _gauntletLayer.LoadMovie("BombSelection", _bombSelectorVM);
            MissionScreen.AddLayer(_gauntletLayer);
        }
        private void HandleVisibility()
        {
            if (_bombSelectorVM != null && 
                !_bombSelectorVM.IsVisible &&
                Mission.InputManager.IsKeyPressed(InputKey.H))
            {
                _bombSelectorVM.IsVisible = true;
                return;
            }

            if (_bombSelectorVM != null &&
                _bombSelectorVM.IsVisible &&
                Mission.InputManager.IsKeyPressed(InputKey.Escape))
            {
                _bombSelectorVM.IsVisible = false;
            }
        }
        private void SynchronizeBombs()
        {
            if (_bombSelectorVM == null) return;
            var playerBombs = PlayerBombManager.Instance.PlayerBombs;

            foreach (var bomb in playerBombs)
            {
                var definition = bomb.Key;
                var amount = bomb.Value;
                var amountString = amount.ToString();
                var vmBomb = _bombSelectorVM.Bombs.FirstOrDefault(b => b.StringId == definition.Item.StringId);

                if (vmBomb == null)
                {
                    _bombSelectorVM.AddBombItem(new(definition.Item.StringId, definition.BaseDescription, amountString, definition.spriteStringId, _bombSelectorVM.OnBombItemSelected));
                    continue;
                }
                if (vmBomb.Amount != amountString)
                    _bombSelectorVM.DecrementBombAmount(vmBomb);
            }

            var validBombIds = playerBombs.Select(b => b.Key.Item.StringId).ToHashSet();
            var bombsToRemove = _bombSelectorVM.Bombs.Where(b => !validBombIds.Contains(b.StringId)).ToList();

            for (int i = bombsToRemove.Count - 1; i >= 0; i--)
            {
                BombItemVM? bomb = bombsToRemove[i];
                _bombSelectorVM?.Bombs.Remove(bomb);
            }
        }
        public override void OnMissionTick(float dt)
        {
            if (_bombSelectorVM == null)
                return;

            HandleVisibility();
            SynchronizeBombs();
        }
    }
}
