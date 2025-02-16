using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career
{
    public abstract class CareerButtonBehaviorBase
    {

        public virtual bool isDialogStart { get; protected set; }

        public virtual void DeactivateDialog()
        {
            isDialogStart = false;
        }

        public delegate void OnCareerButtonClickedEvent(CharacterObject troopID, bool isPrisoner);

        public delegate bool OnShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner);

        public delegate bool OnShouldButtonBeActive(CharacterObject characterObject, out TextObject deactivateCondition, bool isPrsioner);

        protected CareerButtonBehaviorBase(CareerObject career)
        {
        }

        public virtual string CareerButtonIcon => "General\\Icons\\Coin@2x";

        public void Register()
        {
            SpecialbuttonEventManagerHandler.Instance.RegisterNewButton(this);
        }

        public abstract void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner = false);

        public abstract bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner = false);

        public abstract bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner = false);
    }
}