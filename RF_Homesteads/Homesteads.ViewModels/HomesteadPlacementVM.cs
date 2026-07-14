using System;
using Homesteads.Models;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Homesteads.ViewModels;

public class HomesteadPlacementVM : ViewModel
{
	private string _estimatedTravelTimeText = "";

	private bool _isVisible;

	private Homestead _homestead;

	[DataSourceProperty]
	public string RelocatingTitleText
	{
		get
		{
			if (_homestead == null)
			{
				return new TextObject("{=homestead_relocating_title_generic}Homestead is relocating...").ToString();
			}
			return new TextObject("{=homestead_relocating_title_named}Homestead of {HOMESTEAD_NAME} is relocating...").SetTextVariable("HOMESTEAD_NAME", _homestead.Name).ToString();
		}
	}

	[DataSourceProperty]
	public string PlacementControlsText => new TextObject("{=homestead_placement_controls}Left Click to Place | Right Click to Cancel").ToString();

	[DataSourceProperty]
	public string PlacementWarningText => new TextObject("{=homestead_placement_warning}Warning: Results may vary depending on pathing and terrain").ToString();

	[DataSourceProperty]
	public string PlacementHaltedText => new TextObject("{=homestead_placement_halted}Note: All production is halted while the homestead is relocating").ToString();

	[DataSourceProperty]
	public string EstimatedTravelTimeText
	{
		get
		{
			return _estimatedTravelTimeText;
		}
		set
		{
			if (value != _estimatedTravelTimeText)
			{
				_estimatedTravelTimeText = value;
				OnPropertyChangedWithValue(value, "EstimatedTravelTimeText");
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

	public HomesteadPlacementVM(Homestead? homestead)
	{
		_homestead = homestead;
		IsVisible = true;
		RefreshValues();
	}

	public void SetHomestead(Homestead homestead)
	{
		_homestead = homestead;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}

	public void UpdateTargetPosition(Vec2 targetPosition)
	{
		if (_homestead == null)
		{
			EstimatedTravelTimeText = "Unavailable";
			return;
		}
		float num = _homestead.MobileParty.GetPosition2D.Distance(targetPosition);
		float num2 = 1f;
		float num3 = num / num2;
		if (num3 < 1f)
		{
			EstimatedTravelTimeText = "Estimated Travel Time: Less than an hour";
		}
		else if (num3 > 24f)
		{
			int num4 = (int)(num3 / 24f);
			int num5 = (int)(num3 % 24f);
			EstimatedTravelTimeText = $"Estimated Travel Time: {num4} Days, {num5} Hours";
		}
		else
		{
			EstimatedTravelTimeText = $"Estimated Travel Time: {Math.Round(num3, 1)} Hours";
		}
	}
}
