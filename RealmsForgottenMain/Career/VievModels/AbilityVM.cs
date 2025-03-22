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
            if (forUpgraded)
            {
                careerObjectVM.AbilityName = PlayerCareerExtension.GetCareer().Ability.Name.ToString() + "+";
                careerObjectVM.AbilityDescription = PlayerCareerExtension.GetCareer().Ability.DescriptionUpgraded;
            }
            else
            {
                careerObjectVM.AbilityName = PlayerCareerExtension.GetCareer().Ability.Name.ToString();
                careerObjectVM.AbilityDescription = PlayerCareerExtension.GetCareer().Ability.Description;
            }
            careerObjectVM.CurrentSpriteName = spriteName;
            careerObjectVM.RefreshValues();
        }

        public override void RefreshValues()
        {
            if (forUpgraded)
            {
                if (PlayerCareerExtension.GetCareer().Ability.IsUpgraded)
                {
                    SetState(State.Taken);
                    return;
                }
                if (careerObjectVM.DoubleGroupTier2.GetChoices().Count > 0 && (careerObjectVM.DoubleGroupTier2.GetChoices().Last().IsTaken && PlayerCareerExtension.PointsSystem.HasAvailablePoints()))
                    SetState(State.AvailableToTake);
            }
            else
            {
                if (PlayerCareerExtension.GetCareer().Ability.IsEnabled)
                {
                    SetState(State.Taken);
                    return;
                }
                if (!IsTaken && careerObjectVM.DoubleGroupTier1.GetChoices().Count > 0 && (careerObjectVM.DoubleGroupTier1.GetChoices().Last().IsTaken && PlayerCareerExtension.PointsSystem.HasAvailablePoints()))
                    SetState(State.AvailableToTake);

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
