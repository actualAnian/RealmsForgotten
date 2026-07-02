using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace RealmsForgotten.Alchemy.UI
{
    public class BombSelectionItemWidget : ButtonWidget
    {
        public BombSelectionItemWidget(UIContext context) : base(context)
        {
            if (!ContainsState("Selected")) AddState("Selected");
            if (!ContainsState("Default")) AddState("Default");
            if (!ContainsState("Pressed")) AddState("Pressed");
            if (!ContainsState("Hovered")) AddState("Hovered");
            if (!ContainsState("Disabled")) AddState("Disabled");
        }

        protected override void OnConnectedToRoot()
        {
            base.OnConnectedToRoot();
            boolPropertyChanged += OnBoolPropertyChanged;
        }

        protected override void OnDisconnectedFromRoot()
        {
            base.OnDisconnectedFromRoot();
            boolPropertyChanged -= OnBoolPropertyChanged;
        }

        private void OnBoolPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
        {
            InformationManager.DisplayMessage(new("selected!"));
            if (propertyName == "IsSelected")
            {
                if (value)
                {
                    SetState("Selected");
                    EventFired("OnSelected", Array.Empty<object>());
                }
                else
                {
                    SetState("Default");
                }
            }
        }
    }
}
