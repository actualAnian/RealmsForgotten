using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace SOTOR.AbilitySystem;

public class AbilityRadialSelectionItemWidget : ButtonWidget
{
	public AbilityRadialSelectionItemWidget(UIContext context)
		: base(context)
	{
		EnsureState("Selected");
		EnsureState("Default");
		EnsureState("Pressed");
		EnsureState("Hovered");
		EnsureState("Disabled");
	}

	private void EnsureState(string state)
	{
		if (!ContainsState(state))
		{
			AddState(state);
		}
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		base.boolPropertyChanged += OnBoolPropertyChanged;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.boolPropertyChanged -= OnBoolPropertyChanged;
		base.OnDisconnectedFromRoot();
	}

	private void OnBoolPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
	{
		if (!(propertyName != "IsSelected"))
		{
			if (value)
			{
				SetState("Selected");
				EventFired("OnSelected");
			}
			else
			{
				SetState("Default");
			}
		}
	}
}
