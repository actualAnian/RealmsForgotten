using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SOTOR;

[ViewModelMixin("RefreshValues")]
public class CharacterDeveloperSpellBookMixin : BaseViewModelMixin<CharacterDeveloperVM>
{
	private bool _isSpellBookButtonVisible;

	[DataSourceProperty]
	public bool IsSpellBookButtonVisible
	{
		get
		{
			return _isSpellBookButtonVisible;
		}
		set
		{
			if (value != _isSpellBookButtonVisible)
			{
				_isSpellBookButtonVisible = value;
				OnPropertyChangedWithValue(value, "IsSpellBookButtonVisible");
			}
		}
	}

	public CharacterDeveloperSpellBookMixin(CharacterDeveloperVM vm)
		: base(vm)
	{
		RefreshSpellBookButtonVisibility();
	}

	[DataSourceMethod]
	public void ExecuteOpenSpellBook()
	{
		if (Campaign.Current != null && Game.Current != null)
		{
			SotorSpellBookState gameState = Game.Current.GameStateManager.CreateState<SotorSpellBookState>();
			Game.Current.GameStateManager.PushState(gameState);
		}
	}

	public override void OnRefresh()
	{
		RefreshSpellBookButtonVisibility();
	}

	private void RefreshSpellBookButtonVisibility()
	{
		IsSpellBookButtonVisible = Hero.MainHero != null;
	}
}
