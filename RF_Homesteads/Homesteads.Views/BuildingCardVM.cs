using System;
using Homesteads.Models;
using TaleWorlds.Library;

namespace Homesteads.Views;

public class BuildingCardVM : ViewModel
{
	public Action<BuildingCardVM> SelectRequested;

	private bool _isSelected;

	private readonly bool _isAffordable;

	public HomesteadScenePlaceable Placeable { get; }

	public bool IsLocked { get; }

	[DataSourceProperty]
	public string Name => Placeable.DisplayName;

	[DataSourceProperty]
	public string BuildPointsText => Placeable.BuildPointsRequired.ToString();

	[DataSourceProperty]
	public bool IsEnabled => !IsLocked;

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
				OnPropertyChangedWithValue(NameColorText, "NameColorText");
			}
		}
	}

	[DataSourceProperty]
	public string NameColorText
	{
		get
		{
			if (!IsLocked)
			{
				if (!_isSelected)
				{
					return "#EEEEEEFF";
				}
				return "#C7900AFF";
			}
			return "#777777FF";
		}
	}

	[DataSourceProperty]
	public string PointsColorText
	{
		get
		{
			if (!IsLocked)
			{
				if (!_isAffordable)
				{
					return "#D83B3BFF";
				}
				return "#3CAB52FF";
			}
			return "#777777FF";
		}
	}

	public BuildingCardVM(HomesteadScenePlaceable placeable, bool isAffordable, bool isLocked)
	{
		Placeable = placeable;
		_isAffordable = isAffordable;
		IsLocked = isLocked;
	}

	public void ExecuteSelect()
	{
		SelectRequested?.Invoke(this);
	}
}
