using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using RealmsForgotten.UI;

namespace RealmsForgotten.Career.VievModels
{
    [ViewModelExtension(typeof(CharacterDeveloperVM))]
    public class CharacterDeveloperVMExtension : BaseViewModelExtension
    {
        private bool _hasCareer = false;

        public CharacterDeveloperVMExtension(ViewModel vm) : base(vm)
        {
            HasCareer = PlayerCareerExtension.GetCareer() != null;
        }

        private void ExecuteNavigateToCareers()
        {
            try
            {
                var state = Game.Current.GameStateManager.CreateState<CareerScreenGameState>();
                Game.Current.GameStateManager.PushState(state);
            }
            catch (Exception) { Game.Current.GameStateManager.PopState(); }
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
