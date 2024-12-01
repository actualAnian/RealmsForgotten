using System;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class CampaignMapScreenButtonVM : ViewModel
    {
        private readonly Action _onButtonPressed;

        public CampaignMapScreenButtonVM(Action onButtonPressed)
        {
            _onButtonPressed = onButtonPressed;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
        }

        // Command for the button click
        public void OnCareerInfoButtonPressed()
        {
            InformationManager.DisplayMessage(new InformationMessage("Career Info Button Pressed"));
            _onButtonPressed?.Invoke();
        }
    }
}


