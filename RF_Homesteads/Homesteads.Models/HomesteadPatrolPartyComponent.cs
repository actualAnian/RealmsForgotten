using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadPatrolPartyComponent : PartyComponent
{
	[SaveableField(1)]
	private Homestead homestead;

	public override Hero PartyOwner => Hero.MainHero;

	public override TextObject Name => new TextObject(homestead.Name?.ToString() + " Patrol");

	public override Settlement HomeSettlement => homestead.MobileParty?.HomeSettlement;

	public override Hero Leader => null;

	public override Banner GetDefaultComponentBanner()
	{
		return homestead.GetDefaultComponentBanner();
	}

	public HomesteadPatrolPartyComponent(Homestead homestead)
	{
		this.homestead = homestead;
	}

	protected override void OnFinalize()
	{
		HomesteadBehavior.Instance?.PatrolMobileParties.Remove(base.MobileParty);
		homestead.OnPatrolDestroyed();
	}
}
