using System.Collections.Generic;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Homesteads.Models;

public class HomesteadVM : ViewModel
{
	private HomesteadScene homesteadScene;

	private bool _areStatsVisible;

	private bool _currentPlaceableBoxVisible;

	private bool _targetBoxVisible;

	private int _buildPointsLeft;

	private int _productivity;

	private int _space;

	private int _leisure;

	private string _placeableName;

	private string _placeableDesc;

	private string _currentCategoryName;

	private string _targetName;

	private string _targetHint;

	private string _availableMaterialsText;

	private string _heightModeText;

	private string _buildMenuPromptText;

	private bool _isBuildMenuPromptVisible;

	private bool _isHelpPanelVisible;

	private string _helpPanelText = "";

	[DataSourceProperty]
	public string BuildPointsLocalization => Utils.GetLocalizedString("{=homestead_gui_build_points}Build Points: ");

	[DataSourceProperty]
	public string ProductivityLocalization => Utils.GetLocalizedString("{=homestead_gui_productivity}Productivity: ");

	[DataSourceProperty]
	public string SpaceLocalization => Utils.GetLocalizedString("{=homestead_gui_space}Extra Space: ");

	[DataSourceProperty]
	public string LeisureLocalization => Utils.GetLocalizedString("{=homestead_gui_leisure}Leisure: ");

	[DataSourceProperty]
	public string CategoryLocalization => Utils.GetLocalizedString("{=homestead_gui_category}Category: ");

	[DataSourceProperty]
	public string CategoryHintLocalization
	{
		get
		{
			if (HomesteadsReloaded.Settings == null)
			{
				return Utils.GetLocalizedString("{=homestead_gui_category_hint_fallback}Press the category key to switch category. Press the cycle keys to cycle placeables.");
			}
			return Utils.GetLocalizedString("{=homestead_gui_category_hint_dynamic}Press {CATEGORY_KEY} to switch category. Press {LEFT_KEY} and {RIGHT_KEY} to cycle placeables.", ("CATEGORY_KEY", HomesteadsReloaded.Settings.GetSwitchBuilderModeCategoryKeyLabel()), ("LEFT_KEY", HomesteadsReloaded.Settings.GetCycleLeftKeyLabel()), ("RIGHT_KEY", HomesteadsReloaded.Settings.GetCycleRightKeyLabel()));
		}
	}

	[DataSourceProperty]
	public bool AreStatsVisible
	{
		get
		{
			return _areStatsVisible;
		}
		set
		{
			if (value != _areStatsVisible)
			{
				_areStatsVisible = value;
				OnPropertyChangedWithValue(value, "AreStatsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHelpPanelVisible
	{
		get
		{
			return _isHelpPanelVisible;
		}
		set
		{
			if (value != _isHelpPanelVisible)
			{
				_isHelpPanelVisible = value;
				OnPropertyChangedWithValue(value, "IsHelpPanelVisible");
			}
		}
	}

	[DataSourceProperty]
	public string HelpPanelText
	{
		get
		{
			return _helpPanelText;
		}
		set
		{
			if (value != _helpPanelText)
			{
				_helpPanelText = value;
				OnPropertyChangedWithValue(value, "HelpPanelText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBuildMenuPromptVisible
	{
		get
		{
			return _isBuildMenuPromptVisible;
		}
		set
		{
			if (value != _isBuildMenuPromptVisible)
			{
				_isBuildMenuPromptVisible = value;
				OnPropertyChangedWithValue(value, "IsBuildMenuPromptVisible");
				OnPropertyChanged("BuildMenuPromptText");
			}
		}
	}

	[DataSourceProperty]
	public string BuildMenuPromptText
	{
		get
		{
			string text = HomesteadsReloaded.Settings?.GetOpenBuildMenuKeyLabel() ?? "Tilde";
			switch (text)
			{
			case "Tilde":
				text = "Tilde (`)";
				break;
			case "‘":
			case "'":
			case "’":
			case "`":
				text = "Tilde (`)";
				break;
			}
			return Utils.GetLocalizedString("{=homestead_gui_open_build_menu}Press {KEY} to bring up the build selector.", ("KEY", text));
		}
	}

	[DataSourceProperty]
	public bool CurrentPlaceableBoxVisible
	{
		get
		{
			return _currentPlaceableBoxVisible;
		}
		set
		{
			if (value != _currentPlaceableBoxVisible)
			{
				_currentPlaceableBoxVisible = value;
				OnPropertyChangedWithValue(value, "CurrentPlaceableBoxVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool TargetBoxVisible
	{
		get
		{
			return _targetBoxVisible;
		}
		set
		{
			if (value != _targetBoxVisible)
			{
				_targetBoxVisible = value;
				OnPropertyChangedWithValue(value, "TargetBoxVisible");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentPlaceableName
	{
		get
		{
			return _placeableName;
		}
		set
		{
			if (value != _placeableName)
			{
				_placeableName = value;
				OnPropertyChangedWithValue(value, "CurrentPlaceableName");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentPlaceableDesc
	{
		get
		{
			return _placeableDesc;
		}
		set
		{
			if (value != _placeableDesc)
			{
				_placeableDesc = value;
				OnPropertyChangedWithValue(value, "CurrentPlaceableDesc");
			}
		}
	}

	[DataSourceProperty]
	public string TargetName
	{
		get
		{
			return _targetName;
		}
		set
		{
			if (value != _targetName)
			{
				_targetName = value;
				OnPropertyChangedWithValue(value, "TargetName");
			}
		}
	}

	[DataSourceProperty]
	public string TargetHint
	{
		get
		{
			return _targetHint;
		}
		set
		{
			if (value != _targetHint)
			{
				_targetHint = value;
				OnPropertyChangedWithValue(value, "TargetHint");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentCategoryName
	{
		get
		{
			return _currentCategoryName;
		}
		set
		{
			if (value != _currentCategoryName)
			{
				_currentCategoryName = value;
				OnPropertyChangedWithValue(value, "CurrentCategoryName");
			}
		}
	}

	[DataSourceProperty]
	public int BuildPointsLeft
	{
		get
		{
			return _buildPointsLeft;
		}
		set
		{
			if (value != _buildPointsLeft)
			{
				_buildPointsLeft = value;
				OnPropertyChangedWithValue(value, "BuildPointsLeft");
			}
		}
	}

	[DataSourceProperty]
	public int TotalProductivity
	{
		get
		{
			return _productivity;
		}
		set
		{
			if (value != _productivity)
			{
				_productivity = value;
				OnPropertyChangedWithValue(value, "TotalProductivity");
			}
		}
	}

	[DataSourceProperty]
	public int ExtraSpace
	{
		get
		{
			return _space;
		}
		set
		{
			if (value != _space)
			{
				_space = value;
				OnPropertyChangedWithValue(value, "ExtraSpace");
			}
		}
	}

	[DataSourceProperty]
	public int TotalLeisure
	{
		get
		{
			return _leisure;
		}
		set
		{
			if (value != _leisure)
			{
				_leisure = value;
				OnPropertyChangedWithValue(value, "TotalLeisure");
			}
		}
	}

	[DataSourceProperty]
	public string HeightModeText
	{
		get
		{
			return _heightModeText;
		}
		set
		{
			if (value != _heightModeText)
			{
				_heightModeText = value;
				OnPropertyChangedWithValue(value, "HeightModeText");
			}
		}
	}

	[DataSourceProperty]
	public string AvailableMaterialsLocalization => Utils.GetLocalizedString("{=homestead_gui_materials}Building Materials:");

	[DataSourceProperty]
	public string AvailableMaterialsText
	{
		get
		{
			return _availableMaterialsText;
		}
		set
		{
			if (value != _availableMaterialsText)
			{
				_availableMaterialsText = value;
				OnPropertyChangedWithValue(value, "AvailableMaterialsText");
			}
		}
	}

	public HomesteadVM(HomesteadScene homesteadScene)
	{
		this.homesteadScene = homesteadScene;
		_areStatsVisible = false;
		_currentPlaceableBoxVisible = false;
		_isBuildMenuPromptVisible = false;
		_buildPointsLeft = homesteadScene.BuildPointsLeftToUse;
		_productivity = homesteadScene.TotalProductivity;
		_space = homesteadScene.TotalSpace;
		_leisure = homesteadScene.TotalLeisure;
		_placeableName = "";
		_placeableDesc = "";
		_currentCategoryName = "";
		_targetName = "";
		_targetHint = "";
		_availableMaterialsText = "";
		_heightModeText = Utils.GetLocalizedString("{=homestead_gui_height_follow}Height: Follow Terrain");
		UpdateAvailableMaterials(homesteadScene.Homestead);
	}

	public void UpdateAvailableMaterials(Homestead homestead)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (ItemRosterElement item in homestead.Stash)
		{
			if (item.EquipmentElement.Item != null && HomesteadBehavior.IsBuildingMaterial(item.EquipmentElement.Item, homestead))
			{
				string key = item.EquipmentElement.Item.Name.ToString();
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = 0;
				}
				dictionary[key] += item.Amount;
			}
		}
		if (dictionary.Count == 0)
		{
			AvailableMaterialsText = Utils.GetLocalizedString("{=homestead_menu_info_none}  (none)");
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			stringBuilder.Append($"{item2.Key}: {item2.Value}\n");
		}
		AvailableMaterialsText = stringBuilder.ToString().TrimEnd(new char[1] { '\n' });
	}
}
