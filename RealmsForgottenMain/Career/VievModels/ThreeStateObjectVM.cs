using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class ThreeStateObjectVM : ViewModel
    {
        protected State buttonState = State.UnavailableToTake;
        private bool _isTaken = false;
        private bool _isUnavailableToTake = true;
        private bool _isAvailableToTake = false;
        public enum State
        {
            Taken,
            AvailableToTake,
            UnavailableToTake
        }
        public void SetState(State newState)
        {
            switch (newState)
            {
                case State.Taken:
                    IsTaken = true;
                    IsAvailableToTake = false;
                    IsUnavailableToTake = false;
                    break;
                case State.AvailableToTake:
                    IsTaken = false;
                    IsAvailableToTake = true;
                    IsUnavailableToTake = false;
                    break;
                case State.UnavailableToTake:
                    IsTaken = false;
                    IsAvailableToTake = false;
                    IsUnavailableToTake = true;
                    break;
            }
            buttonState = newState;
        }

        [DataSourceProperty]
        public bool IsTaken
        {
            get
            {
                return _isTaken;
            }
            private set
            {
                if (value != _isTaken)
                {
                    _isTaken = value;
                    OnPropertyChangedWithValue(value, "IsTaken");
                }
            }
        }
        [DataSourceProperty]
        public bool IsAvailableToTake
        {
            get
            {
                return _isAvailableToTake;
            }
            private set
            {
                if (value != _isAvailableToTake)
                {
                    _isAvailableToTake = value;
                    OnPropertyChangedWithValue(value, "IsAvailableToTake");
                }
            }
        }
        [DataSourceProperty]
        public bool IsUnavailableToTake
        {
            get
            {
                return _isUnavailableToTake;
            }
            private set
            {
                if (value != _isUnavailableToTake)
                {
                    _isUnavailableToTake = value;
                    OnPropertyChangedWithValue(value, "IsUnavailableToTake");
                }
            }
        }
    }
}
