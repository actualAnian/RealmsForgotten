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

            if (!PlayerCareerExtension.HasAnyCareer())
                PlayerCareerExtension.AddCareer(RFCareers.Mercenary);
            _currentCareerVM = new CareerObjectVM(PlayerCareerExtension.GetCareer());

            _canClose = true;
        }

        private void ExecuteDone()
        {
            _currentCareerVM.GiveActivePerkBonuses();
            _closeAction();
        }
        private void ExecuteCancel()
        {
            _currentCareerVM.RefundPerks();
            _closeAction();
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
