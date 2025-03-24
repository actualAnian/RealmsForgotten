using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class AbilityVM : ThreeStateObjectVM
    {
        private string spriteName;
        private CareerObjectVM careerObjectVM;
        private bool forUpgraded;
        public AbilityVM(string _spriteName, CareerObjectVM _careerObjectVM, bool _forUpgraded) 
        {
            spriteName = _spriteName;
            careerObjectVM = _careerObjectVM;
            forUpgraded = _forUpgraded;
        }
        public void BuyAbility()
        {
            if (buttonState == State.AvailableToTake)
            {
                SetState(State.Taken);
                careerObjectVM.UnlockAbility(this);
            }
        }

        private void SetAbilityDescription()
        {
            var career = PlayerCareerExtension.GetCareer()!;
            if (forUpgraded)
            {
                careerObjectVM.AbilityName = career.Ability.Name.ToString() + "+";
                careerObjectVM.AbilityDescription = career.Ability.DescriptionUpgraded;
            }
            else
            {
                careerObjectVM.AbilityName = career.Ability.Name.ToString();
                careerObjectVM.AbilityDescription = career.Ability.Description;
            }
            careerObjectVM.CurrentSpriteName = spriteName;
            careerObjectVM.RefreshValues();
        }

        public override void RefreshValues()
        {
            if (forUpgraded)
            {
                if (PlayerCareerExtension.GetCareer()!.Ability.IsUpgraded)
                {
                    SetState(State.Taken);
                    return;
                }
                if (careerObjectVM.IsGroupCompleted(1) && PlayerCareerExtension.PointsSystem!.HasAvailablePoints())
                    SetState(State.AvailableToTake);
                else SetState(State.UnavailableToTake);
            }
            else
            {
                if (PlayerCareerExtension.GetCareer()!.Ability.IsEnabled)
                {
                    SetState(State.Taken);
                    return;
                }
                if (careerObjectVM.IsGroupCompleted(0) && PlayerCareerExtension.PointsSystem!.HasAvailablePoints())
                    SetState(State.AvailableToTake);
                else SetState(State.UnavailableToTake);
            }

        }
        [DataSourceProperty]
        public string Sprite
        {
            get
            {
                return spriteName;
            }
            set
            {
                if (value != spriteName)
                {
                    spriteName = value;
                    OnPropertyChangedWithValue(value, "Sprite");
                }
            }
        }
    }
}
