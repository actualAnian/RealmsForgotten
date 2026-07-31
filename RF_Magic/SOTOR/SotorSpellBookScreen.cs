using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SOTOR;

[GameStateScreen(typeof(SotorSpellBookState))]
public class SotorSpellBookScreen : ScreenBase, IGameStateListener
{
	private GauntletLayer _gauntletLayer;

	private SotorSpellBookVM _dataSource;

	private readonly SotorSpellBookState _state;

	public SotorSpellBookScreen(SotorSpellBookState state)
	{
		_state = state;
		_state.RegisterListener(this);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		LoadingWindow.DisableGlobalLoadingWindow();
		if (_gauntletLayer != null && (_gauntletLayer.Input.IsHotKeyReleased("Exit") || Input.IsKeyReleased(InputKey.Escape)))
		{
			CloseScreen();
		}
	}

	void IGameStateListener.OnActivate()
	{
		OnActivate();
		LoadSotorSprites();
		_dataSource = new SotorSpellBookVM(CloseScreen);
		_gauntletLayer = new GauntletLayer("GauntletLayer", 1, shouldClear: true);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_gauntletLayer.LoadMovie("SotorSpellBook", _dataSource);
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		ScreenManager.TrySetFocus(_gauntletLayer);
	}

	void IGameStateListener.OnDeactivate()
	{
		OnDeactivate();
		if (_gauntletLayer != null)
		{
			RemoveLayer(_gauntletLayer);
			_gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
	}

	void IGameStateListener.OnFinalize()
	{
		_dataSource?.OnFinalize();
		_gauntletLayer = null;
		_dataSource = null;
	}

	void IGameStateListener.OnInitialize()
	{
		OnInitialize();
	}

	private void CloseScreen()
	{
		if (Game.Current?.GameStateManager != null)
		{
			Game.Current.GameStateManager.PopState();
		}
	}

	private static void LoadSotorSprites()
	{
		EnsureCategoryLoaded("ui_sotor");
		EnsureCategoryLoaded("ui_sotor_perks");
	}

	private static void EnsureCategoryLoaded(string name)
	{
		SpriteData spriteData = UIResourceManager.SpriteData;
		bool flag = spriteData?.SpriteCategories != null && spriteData.SpriteCategories.ContainsKey(name);
		SotorLog.Info($"[TABTEST] EnsureCategoryLoaded('{name}') registered={flag}");
		if (flag)
		{
			SpriteCategory spriteCategory = spriteData.SpriteCategories[name];
			if (!spriteCategory.IsLoaded)
			{
				spriteCategory.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
			}
			SotorLog.Info($"[TABTEST] '{name}' loaded={spriteCategory.IsLoaded}");
		}
	}
}
