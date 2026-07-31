using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

public class ProjectileCrosshair_VM : ViewModel
{
	private string _spriteName = "test_spell_crosshair";

	private bool _isVisible;

	[DataSourceProperty]
	public string SpriteName
	{
		get
		{
			return _spriteName;
		}
		set
		{
			if (value != _spriteName)
			{
				_spriteName = value;
				OnPropertyChangedWithValue(value, "SpriteName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}
}
