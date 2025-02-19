using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;
        private CareerObjectVM _currentCareerVM;
        bool _canClose = false;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;

            PlayerCareerExtension.AddCareer(RFCareers.Mercenary);
            _currentCareerVM = new CareerObjectVM(PlayerCareerExtension.GetCareer());

            Game.Current.AfterTick = (Action<float>)Delegate.Combine(Game.Current.AfterTick, new Action<float>(this.OnTick));
            _canClose = true;
        }

        private void OnTick(float obj)
        {
            if (Input.IsKeyPressed(InputKey.Escape) && _canClose)
            {
                Game.Current.AfterTick = (Action<float>)Delegate.Remove(Game.Current.AfterTick, new Action<float>(this.OnTick));
                _canClose = false;
                ExecuteCancel();
            }
        }

        private void ExecuteDone()
        {
            _closeAction();
        }
        private void ExecuteCancel()
        {
            _currentCareerVM.RefundPerks();
            _closeAction();
        }
        private void ExecuteBuyPerk()
        {
            _currentCareerVM.BuyPerk();
        }
        private void ExecuteReset()
        {
            _currentCareerVM.RefundPerks();
        }

        [DataSourceProperty]
        public CareerObjectVM CurrentCareer
        {
            get
            {
                return _currentCareerVM;
            }
            set
            {
                if (value != _currentCareerVM)
                {
                    _currentCareerVM = value;
                    OnPropertyChangedWithValue(value, "CurrentCareer");
                }
            }
        }
    }
}
