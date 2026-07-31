using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace SOTOR.CampaignBehaviors;

public class SotorGraveyardNightWatchPartyComponent : PartyComponent
{
	[CachedData]
	private TextObject _cachedName;

	private const int NightwatchFloor = 8;

	private const int NightwatchCap = 30;

	private const int VillageNightwatchFloor = 4;

	private const int VillageNightwatchCap = 15;

	[SaveableProperty(1)]
	public Settlement Settlement { get; private set; }

	public override Hero PartyOwner => Settlement.Owner ?? Settlement.MapFaction.Leader;

	public override Settlement HomeSettlement => Settlement;

	public override TextObject Name
	{
		get
		{
			if (_cachedName == null)
			{
				_cachedName = new TextObject("{=*}{SETTLEMENT_NAME}'s Nightwatch");
				_cachedName.SetTextVariable("SETTLEMENT_NAME", HomeSettlement.Name);
			}
			return _cachedName;
		}
	}

	public SotorGraveyardNightWatchPartyComponent()
	{
	}

	private SotorGraveyardNightWatchPartyComponent(Settlement settlement)
	{
		Settlement = settlement;
	}

	public static MobileParty CreateParty(Settlement settlement)
	{
		return MobileParty.CreateParty(settlement?.StringId + "_sotor_nightwatchparty_1", new SotorGraveyardNightWatchPartyComponent(settlement));
	}

	protected override void OnMobilePartySetOnCreation()
	{
		base.MobileParty.ActualClan = Settlement.OwnerClan;
		base.MobileParty.InitializeMobilePartyAtPosition(Settlement.Culture.MilitiaPartyTemplate, Settlement.GatePosition);
		base.MobileParty.MemberRoster.Clear();
		BuildNightwatchRoster();
		base.MobileParty.Party.SetVisualAsDirty();
		base.MobileParty.Ai.DisableAi();
		base.MobileParty.Aggressiveness = 0f;
	}

	private void BuildNightwatchRoster()
	{
		TroopRoster troopRoster = BuildNightwatchRoster(Settlement);
		base.MobileParty.MemberRoster.Add(troopRoster);
	}

	public static TroopRoster BuildNightwatchRoster(Settlement settlement)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		CultureObject culture = settlement.Culture;
		CharacterObject meleeMilitiaTroop = culture.MeleeMilitiaTroop;
		CharacterObject rangedMilitiaTroop = culture.RangedMilitiaTroop;
		CharacterObject elite = culture.MeleeEliteMilitiaTroop ?? meleeMilitiaTroop;
		CharacterObject elite2 = culture.RangedEliteMilitiaTroop ?? rangedMilitiaTroop;
		if (meleeMilitiaTroop == null && rangedMilitiaTroop == null)
		{
			return troopRoster;
		}
		float eliteRatio;
		int b;
		if (settlement.IsVillage)
		{
			b = MBRandom.RoundRandomized(((settlement.Village != null) ? settlement.Village.Hearth : 0f) / 70f) + MBRandom.RandomInt(-2, 3);
			b = MathF.Max(4, MathF.Min(15, b));
			eliteRatio = 0f;
		}
		else
		{
			b = MBRandom.RoundRandomized((settlement.IsTown ? settlement.Town.Militia : settlement.Militia) * 0.15f) + MBRandom.RandomInt(-2, 3);
			b = MathF.Max(8, MathF.Min(30, b));
			eliteRatio = MathF.Clamp(((settlement.IsTown ? settlement.Town.Prosperity : 0f) - 3000f) / 4000f, 0f, 0.6f);
		}
		int num = MathF.Round((float)b * 0.6f);
		int number = b - num;
		AddNightwatchTroops(troopRoster, meleeMilitiaTroop, elite, num, eliteRatio);
		AddNightwatchTroops(troopRoster, rangedMilitiaTroop, elite2, number, eliteRatio);
		return troopRoster;
	}

	private static void AddNightwatchTroops(TroopRoster roster, CharacterObject basic, CharacterObject elite, int number, float eliteRatio)
	{
		if (number <= 0)
		{
			return;
		}
		for (int i = 0; i < number; i++)
		{
			CharacterObject characterObject = ((elite != null && MBRandom.RandomFloat < eliteRatio) ? elite : basic);
			if (characterObject != null)
			{
				roster.AddToCounts(characterObject, 1);
			}
		}
	}

	public override Banner GetDefaultComponentBanner()
	{
		return PartyOwner?.ClanBanner;
	}
}
