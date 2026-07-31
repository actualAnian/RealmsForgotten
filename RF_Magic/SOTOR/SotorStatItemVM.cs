using TaleWorlds.Library;

namespace SOTOR;

public class SotorStatItemVM : ViewModel
{
	private string _label;

	private string _value;

	private string _iconSprite = "";

	[DataSourceProperty]
	public string Label
	{
		get
		{
			return _label;
		}
		set
		{
			if (!(value == _label))
			{
				_label = value;
				OnPropertyChangedWithValue(value, "Label");
			}
		}
	}

	[DataSourceProperty]
	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!(value == _value))
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	[DataSourceProperty]
	public string IconSprite
	{
		get
		{
			return _iconSprite;
		}
		set
		{
			if (!(value == _iconSprite))
			{
				_iconSprite = value;
				OnPropertyChangedWithValue(value, "IconSprite");
				OnPropertyChanged("HasIcon");
			}
		}
	}

	[DataSourceProperty]
	public bool HasIcon => !string.IsNullOrEmpty(_iconSprite);

	public SotorStatItemVM(string label, string value)
	{
		_label = label;
		_value = value;
	}

	public SotorStatItemVM(string label, string value, string iconSprite)
	{
		_label = label;
		_value = value;
		_iconSprite = iconSprite ?? "";
	}
}
