using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadAngryVillagersQuest : QuestBase
{
	public const int RelationPenaltyOnDefeat = 40;

	private const int MobDurationDays = 7;

	private const float ArrivalRadius = 2.5f;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private Hero _targetHero;

	[SaveableField(3)]
	private MobileParty _mobParty;

	[SaveableField(4)]
	private int _count;

	[SaveableField(5)]
	private bool _clanEnforcement;

	[SaveableField(6)]
	private Settlement? _originVillage;

	[SaveableField(7)]
	private bool _marching;

	private bool _resolved;

	public Homestead Homestead => _homestead;

	public Hero Target => _targetHero;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject(_clanEnforcement ? "{=homestead_enforcers_quest_title}The Lord's Men at {HOMESTEAD}" : "{=homestead_villagers_quest_title}Angry Villagers at {HOMESTEAD}");
			textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadAngryVillagersQuest(string questId, Hero targetHero, Homestead homestead, int count, CultureObject? troopCulture, bool clanEnforcement, Settlement? originVillage = null)
		: base(questId, targetHero, CampaignTime.DaysFromNow(7f), 0)
	{
		_homestead = homestead;
		_targetHero = targetHero;
		_count = count;
		_clanEnforcement = clanEnforcement;
		_originVillage = originVillage;
		_marching = originVillage != null;
		_mobParty = SpawnMob(homestead, count, troopCulture, clanEnforcement, originVillage);
		AddObjectiveLog();
	}

	private static MobileParty SpawnMob(Homestead homestead, int count, CultureObject? culture, bool clanEnforcement, Settlement? originVillage)
	{
		Vec2 pos;
		if (originVillage != null)
		{
			pos = originVillage.GatePosition.ToVec2();
		}
		else
		{
			Vec2 getPosition2D = homestead.MobileParty.GetPosition2D;
			float num = MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f);
			pos = new Vec2(getPosition2D.X + (float)Math.Cos(num) * 2.5f, getPosition2D.Y + (float)Math.Sin(num) * 2.5f);
		}
		CampaignVec2 position = new CampaignVec2(pos, isOnLand: true);
		MobileParty mobileParty = MobileParty.CreateParty((clanEnforcement ? "homestead_enforcers_" : "homestead_angry_villagers_") + homestead.Name?.ToString() + "_" + CampaignTime.Now.ToMilliseconds, new HomesteadAngryVillagerPartyComponent(homestead, clanEnforcement));
		mobileParty.InitializeMobilePartyAroundPosition(new TroopRoster(mobileParty.Party), new TroopRoster(mobileParty.Party), position, 1f);
		FillRoster(mobileParty.MemberRoster, count, culture, clanEnforcement);
		Clan clan = Clan.BanditFactions?.FirstOrDefault();
		if (clan != null)
		{
			mobileParty.ActualClan = clan;
		}
		mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		mobileParty.Aggressiveness = 0f;
		mobileParty.RecentEventsMorale = 100f;
		if (originVillage != null)
		{
			Vec2 getPosition2D2 = homestead.MobileParty.GetPosition2D;
			mobileParty.SetMoveGoToPoint(new CampaignVec2(getPosition2D2, isOnLand: true), MobileParty.NavigationType.Default);
			TraceLogger.Write("HomesteadAngryVillagersQuest", string.Format("Spawned {0} ({1}) ", clanEnforcement ? "clan enforcers" : "angry villagers", count) + $"at '{originVillage.Name}' marching toward '{homestead.Name}'.");
		}
		else
		{
			TraceLogger.Write("HomesteadAngryVillagersQuest", string.Format("Spawned {0} ({1}) ", clanEnforcement ? "clan enforcers" : "angry villagers", count) + $"camped at ({pos.X:0.#},{pos.Y:0.#}) by '{homestead.Name}'.");
		}
		return mobileParty;
	}

	private static void FillRoster(TroopRoster roster, int count, CultureObject? culture, bool clanEnforcement)
	{
		CharacterObject characterObject = ((!clanEnforcement) ? culture?.Villager : (culture?.MeleeMilitiaTroop ?? culture?.BasicTroop ?? culture?.Villager));
		if (characterObject == null)
		{
			characterObject = CharacterObject.Find("looter");
		}
		if (characterObject != null && count > 0)
		{
			roster.AddToCounts(characterObject, count);
		}
	}

	protected override void HourlyTick()
	{
		if (_mobParty == null || !_mobParty.IsActive)
		{
			return;
		}
		try
		{
			TroopRoster memberRoster = _mobParty.MemberRoster;
			for (int i = 0; i < memberRoster.Count; i++)
			{
				if (memberRoster.GetElementWoundedNumber(i) > 0)
				{
					memberRoster.SetElementWoundedNumber(i, 0);
				}
			}
			int totalManCount = memberRoster.TotalManCount;
			if (totalManCount < _count && totalManCount > 0)
			{
				CharacterObject characterObject = ((memberRoster.Count > 0) ? memberRoster.GetCharacterAtIndex(0) : null);
				if (characterObject != null)
				{
					memberRoster.AddToCounts(characterObject, _count - totalManCount);
				}
			}
		}
		catch
		{
		}
		try
		{
			_mobParty.RecentEventsMorale = 100f;
		}
		catch
		{
		}
		if (!_marching || _homestead?.MobileParty == null)
		{
			return;
		}
		Vec2 getPosition2D = _mobParty.GetPosition2D;
		Vec2 getPosition2D2 = _homestead.MobileParty.GetPosition2D;
		if (getPosition2D.Distance(getPosition2D2) <= 2.5f)
		{
			_marching = false;
			_mobParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
			_mobParty.Aggressiveness = 0f;
			try
			{
				_mobParty.SetMoveModeHold();
			}
			catch
			{
			}
			TraceLogger.Write("HomesteadAngryVillagersQuest", $"Mob arrived at '{_homestead.Name}' -- switching to camped mode.");
		}
		else
		{
			_mobParty.SetMoveGoToPoint(new CampaignVec2(getPosition2D2, isOnLand: true), MobileParty.NavigationType.Default);
		}
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject((!_clanEnforcement) ? ((_originVillage != null) ? "{=homestead_villagers_quest_log_march}You let the headman's patience run out. A mob of {COUNT} angry villagers from {VILLAGE} is marching on {HOMESTEAD}. Intercept them, or drive them off when they arrive -- else they will sack your stores and disperse." : "{=homestead_villagers_quest_log}You let the headman's patience run out. A mob of {COUNT} angry villagers has gathered at {HOMESTEAD}. Drive them off, or they will sack your stores and disperse.") : ((_originVillage != null) ? "{=homestead_enforcers_quest_log_march}You let the deadline for a land patent lapse. {COUNT} men-at-arms sent by {CLAN} have set out from {VILLAGE} for {HOMESTEAD}. Intercept them on the road, or drive them off when they arrive -- else they will levy a fine from your stores and depart." : "{=homestead_enforcers_quest_log}You let the deadline for a land patent lapse. {COUNT} men-at-arms sent by {CLAN} have encamped at {HOMESTEAD}. Drive them off, or they will levy a fine from your stores and depart."));
		textObject.SetTextVariable("COUNT", _count);
		textObject.SetTextVariable("CLAN", _targetHero?.Clan?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("VILLAGE", _originVillage?.Name ?? new TextObject(string.Empty));
		AddLog(textObject);
		if (_mobParty != null)
		{
			AddTrackedObject(_mobParty);
		}
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase? destroyer)
	{
		if (!_resolved && party == _mobParty)
		{
			_resolved = true;
			_mobParty = null;
			if (_targetHero != null && _targetHero.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(_targetHero, -40, _clanEnforcement);
			}
			TextObject textObject = new TextObject(_clanEnforcement ? "{=homestead_enforcers_defeated}You drove off {CLAN}'s men, but cutting them down has soured the whole clan against you. You must regain their favour before your homestead can grow." : "{=homestead_villagers_defeated}You drove off the angry villagers, but {HEADMAN} is furious that you cut down his people. You must earn his trust anew before your homestead can grow.");
			textObject.SetTextVariable("CLAN", _targetHero?.Clan?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("HEADMAN", _targetHero?.Name ?? new TextObject(string.Empty));
			AddLog(textObject);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.4f, 0.3f)));
			CompleteQuestWithSuccess();
		}
	}

	protected override void OnTimedOut()
	{
		if (!_resolved)
		{
			_resolved = true;
			int num = 0;
			if (_homestead != null)
			{
				num = Math.Max(0, _homestead.GoldStored / 2);
				_homestead.GoldStored -= num;
			}
			DestroyMobIfAlive();
			TextObject textObject = new TextObject(_clanEnforcement ? "{=homestead_enforcers_sacked}{CLAN}'s men levied a fine from {HOMESTEAD}'s stores (took {GOLD} gold) and departed. Your homestead still cannot grow without their land patent." : "{=homestead_villagers_sacked}The angry villagers sacked {HOMESTEAD}'s stores (lost {GOLD} gold) and dispersed. Your homestead still cannot grow without the headman's blessing.");
			textObject.SetTextVariable("CLAN", _targetHero?.Clan?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("GOLD", num);
			AddLog(textObject);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
		}
	}

	private void DestroyMobIfAlive()
	{
		if (_mobParty != null && _mobParty.IsActive)
		{
			try
			{
				DestroyPartyAction.Apply(null, _mobParty);
			}
			catch
			{
			}
		}
		_mobParty = null;
	}

	public void CancelForTeardown()
	{
		if (!_resolved)
		{
			_resolved = true;
			DestroyMobIfAlive();
			CompleteQuestWithCancel();
		}
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (_mobParty != null && _mobParty.IsActive)
		{
			AddTrackedObject(_mobParty);
		}
	}

	protected override void SetDialogs()
	{
	}
}
