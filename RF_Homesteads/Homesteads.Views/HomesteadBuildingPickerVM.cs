using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.MissionLogics;
using Homesteads.Models;
using TaleWorlds.Library;

namespace Homesteads.Views;

public class HomesteadBuildingPickerVM : ViewModel
{
	private HomesteadSceneEditingMissionLogic _owner;

	private List<(string CategoryKey, string DisplayName)> _categories = new List<(string, string)>();

	private int _categoryIndex;

	private int _maxTier;

	private BuildingCardVM _selectedCard;

	private string _categoryName = "";

	private string _prevCategoryName = "";

	private string _nextCategoryName = "";

	private bool _isControlsTab;

	private MBBindingList<TierGroupVM> _tierGroups = new MBBindingList<TierGroupVM>();

	private MBBindingList<BindingRowVM> _bindingRows = new MBBindingList<BindingRowVM>();

	private string _detailName = "";

	private string _detailDescription = "";

	private string _buildStatusText = "";

	private string _buildStatusColor = "#B9FF9FFF";

	private bool _hasSelection;

	private bool _canBuildSelection;

	public Action CloseRequested;

	[DataSourceProperty]
	public string CategoryName
	{
		get
		{
			return _categoryName;
		}
		set
		{
			if (value != _categoryName)
			{
				_categoryName = value;
				OnPropertyChangedWithValue(value, "CategoryName");
			}
		}
	}

	[DataSourceProperty]
	public string PrevCategoryName
	{
		get
		{
			return _prevCategoryName;
		}
		set
		{
			if (value != _prevCategoryName)
			{
				_prevCategoryName = value;
				OnPropertyChangedWithValue(value, "PrevCategoryName");
			}
		}
	}

	[DataSourceProperty]
	public string NextCategoryName
	{
		get
		{
			return _nextCategoryName;
		}
		set
		{
			if (value != _nextCategoryName)
			{
				_nextCategoryName = value;
				OnPropertyChangedWithValue(value, "NextCategoryName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsControlsTab
	{
		get
		{
			return _isControlsTab;
		}
		set
		{
			if (value != _isControlsTab)
			{
				_isControlsTab = value;
				OnPropertyChangedWithValue(value, "IsControlsTab");
				OnPropertyChangedWithValue(!value, "IsCategoryTab");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCategoryTab => !_isControlsTab;

	[DataSourceProperty]
	public MBBindingList<TierGroupVM> TierGroups
	{
		get
		{
			return _tierGroups;
		}
		set
		{
			if (value != _tierGroups)
			{
				_tierGroups = value;
				OnPropertyChangedWithValue(value, "TierGroups");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<BindingRowVM> BindingRows
	{
		get
		{
			return _bindingRows;
		}
		set
		{
			if (value != _bindingRows)
			{
				_bindingRows = value;
				OnPropertyChangedWithValue(value, "BindingRows");
			}
		}
	}

	[DataSourceProperty]
	public bool HasSelection
	{
		get
		{
			return _hasSelection;
		}
		set
		{
			if (value != _hasSelection)
			{
				_hasSelection = value;
				OnPropertyChangedWithValue(value, "HasSelection");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBuildSelection
	{
		get
		{
			return _canBuildSelection;
		}
		set
		{
			if (value != _canBuildSelection)
			{
				_canBuildSelection = value;
				OnPropertyChangedWithValue(value, "CanBuildSelection");
			}
		}
	}

	[DataSourceProperty]
	public string DetailName
	{
		get
		{
			return _detailName;
		}
		set
		{
			if (value != _detailName)
			{
				_detailName = value;
				OnPropertyChangedWithValue(value, "DetailName");
			}
		}
	}

	[DataSourceProperty]
	public string DetailDescription
	{
		get
		{
			return _detailDescription;
		}
		set
		{
			if (value != _detailDescription)
			{
				_detailDescription = value;
				OnPropertyChangedWithValue(value, "DetailDescription");
			}
		}
	}

	[DataSourceProperty]
	public string BuildStatusText
	{
		get
		{
			return _buildStatusText;
		}
		set
		{
			if (value != _buildStatusText)
			{
				_buildStatusText = value;
				OnPropertyChangedWithValue(value, "BuildStatusText");
			}
		}
	}

	[DataSourceProperty]
	public string BuildStatusColor
	{
		get
		{
			return _buildStatusColor;
		}
		set
		{
			if (value != _buildStatusColor)
			{
				_buildStatusColor = value;
				OnPropertyChangedWithValue(value, "BuildStatusColor");
			}
		}
	}

	[DataSourceProperty]
	public string ControlsFooterText => Utils.GetLocalizedString("{=homestead_picker_controls_footer}Rebind these in Mod Options (MCM) - Change Key Binds / Controller Binds.");

	[DataSourceProperty]
	public string ConfirmButtonText => Utils.GetLocalizedString("{=homestead_picker_confirm}Build This");

	[DataSourceProperty]
	public string CancelButtonText => Utils.GetLocalizedString("{=homestead_picker_cancel}Cancel");

	[DataSourceProperty]
	public string ColumnBuildingText => Utils.GetLocalizedString("{=homestead_picker_col_building}Building");

	[DataSourceProperty]
	public string ColumnPointsText => Utils.GetLocalizedString("{=homestead_picker_col_points}Build Pts");

	public void Initialize(HomesteadSceneEditingMissionLogic owner)
	{
		_owner = owner;
		_categories = owner.GetNonEmptyCategoriesForPicker();
		_maxTier = owner.MaxBuildableTierForPicker;
		string currentCategory = owner.CurrentCategoryStringForPicker;
		_categoryIndex = _categories.FindIndex(((string CategoryKey, string DisplayName) c) => c.CategoryKey == currentCategory);
		if (_categoryIndex < 0)
		{
			_categoryIndex = 0;
		}
		RefreshCategory();
	}

	public void ExecuteNext()
	{
		if (_categories.Count != 0)
		{
			if (_isControlsTab)
			{
				_categoryIndex = 0;
				RefreshCategory();
			}
			else if (_categoryIndex == _categories.Count - 1)
			{
				ShowControlsTab();
			}
			else
			{
				_categoryIndex++;
				RefreshCategory();
			}
		}
	}

	public void ExecutePrev()
	{
		if (_categories.Count != 0)
		{
			if (_isControlsTab)
			{
				_categoryIndex = _categories.Count - 1;
				RefreshCategory();
			}
			else if (_categoryIndex == 0)
			{
				ShowControlsTab();
			}
			else
			{
				_categoryIndex--;
				RefreshCategory();
			}
		}
	}

	private void RefreshCategory()
	{
		IsControlsTab = false;
		if (_categories.Count == 0)
		{
			CategoryName = "";
			TierGroups.Clear();
			return;
		}
		(string CategoryKey, string DisplayName) tuple = _categories[_categoryIndex];
		string item = tuple.CategoryKey;
		string item2 = tuple.DisplayName;
		CategoryName = item2;
		UpdateCarouselLabels();
		TierGroups.Clear();
		_selectedCard = null;
		SetNoSelection();
		List<HomesteadScenePlaceable> allPlaceablesForPickerCategory = _owner.GetAllPlaceablesForPickerCategory(item);
		BuildingCardVM buildingCardVM = null;
		foreach (IGrouping<int, HomesteadScenePlaceable> item3 in from p in allPlaceablesForPickerCategory
			group p by p.TierRequired into g
			orderby g.Key
			select g)
		{
			TierGroupVM tierGroupVM = new TierGroupVM(TierHeader(item3.Key));
			foreach (HomesteadScenePlaceable item4 in item3)
			{
				bool flag = item4.TierRequired > _maxTier;
				BuildingCardVM buildingCardVM2 = new BuildingCardVM(item4, _owner.IsPlaceableAffordable(item4), flag);
				buildingCardVM2.SelectRequested = SelectCard;
				tierGroupVM.Cards.Add(buildingCardVM2);
				if (!flag && buildingCardVM == null)
				{
					buildingCardVM = buildingCardVM2;
				}
			}
			TierGroups.Add(tierGroupVM);
		}
		if (buildingCardVM != null)
		{
			SelectCard(buildingCardVM);
		}
	}

	private string TierHeader(int tier)
	{
		return Utils.GetLocalizedString("{=homestead_picker_tier}Tier {TIER}", ("TIER", tier.ToString()));
	}

	private void ShowControlsTab()
	{
		IsControlsTab = true;
		CategoryName = Utils.GetLocalizedString("{=homestead_picker_controls_tab}Controls");
		UpdateCarouselLabels();
		TierGroups.Clear();
		SetNoSelection();
		BuildBindingRows();
	}

	private void UpdateCarouselLabels()
	{
		if (_categories.Count == 0)
		{
			PrevCategoryName = "";
			NextCategoryName = "";
			return;
		}
		string localizedString = Utils.GetLocalizedString("{=homestead_picker_controls_tab}Controls");
		int count = _categories.Count;
		if (_isControlsTab)
		{
			PrevCategoryName = _categories[count - 1].DisplayName;
			NextCategoryName = _categories[0].DisplayName;
		}
		else
		{
			PrevCategoryName = ((_categoryIndex == 0) ? localizedString : _categories[_categoryIndex - 1].DisplayName);
			NextCategoryName = ((_categoryIndex == count - 1) ? localizedString : _categories[_categoryIndex + 1].DisplayName);
		}
	}

	private void SetNoSelection()
	{
		HasSelection = false;
		CanBuildSelection = false;
		DetailName = "";
		DetailDescription = "";
		BuildStatusText = "";
	}

	private void SelectCard(BuildingCardVM card)
	{
		if (card == null || card.IsLocked)
		{
			return;
		}
		if (card == _selectedCard && _canBuildSelection)
		{
			ExecuteConfirm();
			return;
		}
		foreach (TierGroupVM tierGroup in TierGroups)
		{
			foreach (BuildingCardVM card2 in tierGroup.Cards)
			{
				card2.IsSelected = false;
			}
		}
		card.IsSelected = true;
		_selectedCard = card;
		HasSelection = true;
		DetailName = card.Name;
		DetailDescription = card.Placeable.Description;
		string unaffordableReason = _owner.GetUnaffordableReason(card.Placeable);
		if (string.IsNullOrEmpty(unaffordableReason))
		{
			CanBuildSelection = true;
			BuildStatusText = Utils.GetLocalizedString("{=homestead_picker_can_build}Ready to build.");
			BuildStatusColor = "#3CAB52FF";
		}
		else
		{
			CanBuildSelection = false;
			BuildStatusText = Utils.GetLocalizedString("{=homestead_picker_need}Need: {MISSING}", ("MISSING", unaffordableReason));
			BuildStatusColor = "#D83B3BFF";
		}
	}

	private void BuildBindingRows()
	{
		BindingRows.Clear();
		MCMSettings settings = HomesteadsReloaded.Settings;
		if (settings != null)
		{
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_open}Open This Menu"), settings.GetOpenBuildMenuKeyLabel(), "D-Pad Down");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_mode}Cycle Edit Mode"), settings.GetEditModeKeyLabel(), "D-Pad Up");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_category}Switch Category"), settings.GetSwitchBuilderModeCategoryKeyLabel(), "D-Pad Down");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_cycle}Cycle Placeables"), settings.GetCycleLeftKeyLabel() + " / " + settings.GetCycleRightKeyLabel(), "D-Pad Left / Right");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_place}Place / Delete / Pick Up"), settings.GetPlaceKeyLabel(), "A");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_rotate}Rotate"), settings.GetRotateTurnLeftKeyLabel() + " / " + settings.GetRotateTurnRightKeyLabel(), "hold Y + D-Pad");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_reset}Reset Rotation"), settings.GetResetRotationKeyLabel(), "R3");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_height}Object Height"), "Scroll / " + settings.GetMoveUpKeyLabel(), "LT / RT");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_snap}Snap to Ground"), settings.GetSnapToGroundKeyLabel(), "X");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_lock}Toggle Height Lock"), settings.GetToggleHeightLockKeyLabel(), "X");
			Row(Utils.GetLocalizedString("{=homestead_picker_ctrl_camera}Camera Height"), "Space / Alt", "LB / RB");
		}
		void Row(string action, string kb, string ctl)
		{
			BindingRows.Add(new BindingRowVM(action, kb, ctl));
		}
	}

	public void ExecuteConfirm()
	{
		if (!_isControlsTab && _selectedCard != null && _canBuildSelection && _categories.Count != 0)
		{
			HomesteadScenePlaceable placeable = _selectedCard.Placeable;
			_owner.SetActivePlaceableByIdentity(_categories[_categoryIndex].CategoryKey, placeable.PrefabName, placeable.DisplayName, placeable.NpcAction);
			CloseRequested?.Invoke();
		}
	}

	public void ExecuteClose()
	{
		CloseRequested?.Invoke();
	}
}
