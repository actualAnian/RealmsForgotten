using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadRaiderPartyComponent : PartyComponent
{
	[SaveableField(1)]
	private Homestead _homestead;

	public Homestead Homestead => _homestead;

	public override Hero? PartyOwner => null;

	public override TextObject Name => new TextObject("{=homestead_angry_mob}Angry Mob");

	public override Settlement? HomeSettlement => null;

	public override Hero? Leader => null;

	public override Banner GetDefaultComponentBanner()
	{
		return null;
	}

	public HomesteadRaiderPartyComponent(Homestead homestead)
	{
		_homestead = homestead;
	}

	protected override void OnFinalize()
	{
	}
}
