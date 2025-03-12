using TaleWorlds.Library;

namespace RealmsForgotten.Career.VievModels
{
    public class AbilityVM : ThreeStateObjectVM
    {
        private string spriteName;
        private CareerObjectVM careerObjectVM;
        public AbilityVM(string _spriteName, CareerObjectVM _careerObjectVM) 
        {
            spriteName = _spriteName;
            careerObjectVM = _careerObjectVM;
        }
        public void BuyAbility()
        {
            careerObjectVM.UnlockAbility(this); 
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
