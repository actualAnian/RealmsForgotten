using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using RealmsForgotten.AiMade.Enlistement;
using RealmsForgotten.UI;

namespace RealmsForgotten.Career.VievModels
{
    [ViewModelExtension(typeof(CharacterDeveloperVM))]
    public class CharacterDeveloperVMExtension : BaseViewModelExtension
    {
        private bool _hasCareer = false;

        public CharacterDeveloperVMExtension(ViewModel vm) : base(vm)
        {
            HasCareer = true;
            //HasCareer = Hero.MainHero.HasAnyCareer();
        }

        private void ExecuteNavigateToCareers()
        {
            try
            {
                var state = Game.Current.GameStateManager.CreateState<CareerScreenGameState>();
                Game.Current.GameStateManager.PushState(state);
            }
            catch (Exception) { }
        }
        [DataSourceProperty]
        public bool HasCareer
        {
            get
            {
                return _hasCareer;
            }
            set
            {
                if (value != _hasCareer)
                {
                    _hasCareer = value;
                    _vm.OnPropertyChangedWithValue(value, "HasCareer");
                }
            }
        }
    }
}
