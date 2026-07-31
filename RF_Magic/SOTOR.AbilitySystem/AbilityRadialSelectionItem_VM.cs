using System;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

public class AbilityRadialSelectionItem_VM : ViewModel
{
	private readonly Ability _ability;

	private readonly Action<Ability> _onSelected;

	private string _spriteName;

	[DataSourceProperty]
	public string SpriteName
	{
		get
		{
			return _spriteName;
		}
		set
		{
			if (!(value == _spriteName))
			{
				_spriteName = value;
				OnPropertyChangedWithValue(value, "SpriteName");
			}
		}
	}

	public AbilityRadialSelectionItem_VM(Ability ability, Action<Ability> onSelected)
	{
		_ability = ability;
		_onSelected = onSelected;
		_spriteName = ability.Template.SpriteName;
	}

	public void OnSelected()
	{
		_onSelected?.Invoke(_ability);
	}
}
