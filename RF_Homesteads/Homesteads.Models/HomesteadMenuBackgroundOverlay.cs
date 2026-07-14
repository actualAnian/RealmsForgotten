using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace Homesteads.Models;

internal sealed class HomesteadMenuBackgroundOverlay
{
	private sealed class PassiveGauntletLayer : GauntletLayer
	{
		public PassiveGauntletLayer(int localOrder, bool shouldClear = false)
			: base("HomesteadMenuBackground", localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return false;
		}
	}

	private sealed class EmptyViewModel : ViewModel
	{
	}

	private const string LayerName = "HomesteadMenuBackground";

	private const string SpriteCategoryName = "ui_homesteads";

	private static readonly EmptyViewModel EmptyDataSource = new EmptyViewModel();

	private static HomesteadMenuBackgroundOverlay? _instance;

	private GauntletLayer? _layer;

	private ScreenBase? _attachedScreen;

	private SpriteCategory? _loadedCategory;

	private HomesteadMenuBackgroundOverlay()
	{
	}

	public static void UpdateForMenu(string menuId)
	{
		if (IsHomesteadMenu(menuId))
		{
			if (_instance == null)
			{
				_instance = new HomesteadMenuBackgroundOverlay();
			}
			_instance.Show();
		}
		else
		{
			_instance?.Hide();
		}
	}

	private static bool IsHomesteadMenu(string menuId)
	{
		if (!(menuId == "homestead_menu_main") && !(menuId == "homestead_menu_manage_main"))
		{
			return menuId == "homestead_menu_wait_waiting";
		}
		return true;
	}

	private void Show()
	{
		ScreenBase topScreen = ScreenManager.TopScreen;
		if (topScreen != null && _attachedScreen != topScreen)
		{
			Hide();
			EnsureSpriteCategoryLoaded();
			_layer = new PassiveGauntletLayer(0);
			_layer.LoadMovie("HomesteadMenuBackground", EmptyDataSource);
			topScreen.AddLayer(_layer);
			_attachedScreen = topScreen;
		}
	}

	private void Hide()
	{
		if (_attachedScreen != null && _layer != null && _attachedScreen.HasLayer(_layer))
		{
			_attachedScreen.RemoveLayer(_layer);
		}
		_layer = null;
		_attachedScreen = null;
	}

	private void EnsureSpriteCategoryLoaded()
	{
		if (_loadedCategory == null)
		{
			SpriteCategory spriteCategory = UIResourceManager.SpriteData.SpriteCategories["ui_homesteads"];
			spriteCategory.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
			_loadedCategory = spriteCategory;
		}
	}
}
