using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;
        private CareerObjectVM _currentCareerVM;
        private bool _hasBattlePrayers;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;

            PlayerCareerExtension.AddCareer(RFCareers.Mercenary);
            _currentCareerVM = new CareerObjectVM(PlayerCareerExtension.GetCareer());
            HasBattlePrayers = false;
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

        private void OpenBattlePrayers()
        {
            //var state = Game.Current.GameStateManager.CreateState<BattlePrayerBookState>();
            //Game.Current.GameStateManager.PushState(state);
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

        [DataSourceProperty]
        public bool HasBattlePrayers
        {
            get
            {
                return _hasBattlePrayers;
            }
            set
            {
                if (value != _hasBattlePrayers)
                {
                    _hasBattlePrayers = value;
                    OnPropertyChangedWithValue(value, "HasBattlePrayers");
                }
            }
        }
    }
}
