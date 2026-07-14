using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadAngryVillagerPartyComponent : PartyComponent
{
	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private bool _clanEnforcement;

	public Homestead Homestead => _homestead;

	public override Hero? PartyOwner => null;

	public override TextObject Name => new TextObject(_clanEnforcement ? "{=homestead_lords_men}The Lord's Men" : "{=homestead_angry_villagers}Angry Villagers");

	public override Settlement? HomeSettlement => null;

	public override Hero? Leader => null;

	public override Banner GetDefaultComponentBanner()
	{
		return null;
	}

	public HomesteadAngryVillagerPartyComponent(Homestead homestead, bool clanEnforcement = false)
	{
		_homestead = homestead;
		_clanEnforcement = clanEnforcement;
	}

	protected override void OnFinalize()
	{
	}
}
