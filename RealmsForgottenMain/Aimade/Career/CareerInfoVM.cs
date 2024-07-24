using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class CareerInfoVM : ViewModel
    {
        private Action _onClose;

        public CareerInfoVM(Action onClose)
        {
            _onClose = onClose;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            CareerInfo = new TextObject("You are currently pursuing a career as a Knight. Serve your lord and manage your fief.");
        }

        private TextObject _careerInfo;
        public TextObject CareerInfo
        {
            get => _careerInfo;
            set
            {
                if (_careerInfo != value)
                {
                    _careerInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        private void ExecuteClose()
        {
            _onClose?.Invoke();
        }

        public void OnClose()
        {
            ExecuteClose();
        }
    }
}





