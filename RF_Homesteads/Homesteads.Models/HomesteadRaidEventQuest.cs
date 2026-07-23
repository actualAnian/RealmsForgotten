using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadRaidEventQuest : QuestBase
{
	public const int VictoryRelationReward = 5;

	private const int RaidDurationDays = 7;

	private static readonly string[][] TierCommonTroops = new string[4][]
	{
		new string[3] { "forest_bandits_bandit", "mountain_bandits_bandit", "looter" },
		new string[3] { "forest_bandits_raider", "mountain_bandits_raider", "desert_bandits_bandit" },
		new string[3] { "forest_bandits_chief", "mountain_bandits_chief", "desert_bandits_raider" },
		new string[3] { "forest_bandits_chief", "mountain_bandits_chief", "desert_bandits_chief" }
	};

	private static readonly string[] BossPool = new string[3] { "forest_bandits_boss", "mountain_bandits_boss", "desert_bandits_boss" };

	[SaveableField(1)]
	private MobileParty _raiderParty;

	[SaveableField(2)]
	private MobileParty _homesteadParty;

	[SaveableField(3)]
	private Homestead _homestead;

	[SaveableField(4)]
	private int _raiderCount;

	private bool _questResolved;

	private JournalLog? _progressLog;

	public Homestead Homestead => _homestead;

	public int RaiderCount => _raiderParty?.MemberRoster?.TotalManCount ?? _raiderCount;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_raid_quest_title}Angry Mob Approaching {HOMESTEAD}");
			textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadRaidEventQuest(string questId, Hero questGiver, Homestead homestead, int raiderCount, int tier, bool distantSpawn = false)
		: base(questId, questGiver, CampaignTime.DaysFromNow(7f), 0)
	{
		_homestead = homestead;
		_homesteadParty = homestead.MobileParty;
		_raiderCount = raiderCount;
		_raiderParty = SpawnRaiderParty(raiderCount, tier, homestead, distantSpawn);
		AddObjectiveLog();
	}

	private static MobileParty SpawnRaiderParty(int count, int tier, Homestead homestead, bool distantSpawn = false)
	{
		Vec2 getPosition2D = homestead.MobileParty.GetPosition2D;
		float num = MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f);
		// A hound master's dogs catch the raiders' scent early: the mob starts
		// farther out, which buys the player real time to ride home or prepare.
		float num2 = (distantSpawn ? (4.5f + MBRandom.RandomFloat * 1.5f) : (2f + MBRandom.RandomFloat));
		Vec2 pos = new Vec2(getPosition2D.X + (float)Math.Cos(num) * num2, getPosition2D.Y + (float)Math.Sin(num) * num2);
		CampaignVec2 position = new CampaignVec2(pos, isOnLand: true);
		MobileParty mobileParty = MobileParty.CreateParty("homestead_angry_mob_" + homestead.Name?.ToString() + "_" + CampaignTime.Now.ToMilliseconds, new HomesteadRaiderPartyComponent(homestead));
		mobileParty.InitializeMobilePartyAroundPosition(new TroopRoster(mobileParty.Party), new TroopRoster(mobileParty.Party), position, 1f);
		FillRaiderRoster(mobileParty.MemberRoster, count, tier);
		Clan clan = Clan.BanditFactions?.FirstOrDefault();
		if (clan != null)
		{
			mobileParty.ActualClan = clan;
		}
		mobileParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		mobileParty.SetMoveGoToPoint(new CampaignVec2(getPosition2D, isOnLand: true), MobileParty.NavigationType.Default);
		mobileParty.Aggressiveness = 1f;
		TraceLogger.Write("HomesteadRaidEventQuest", $"Spawned Angry Mob ({count} troops, tier {tier}) at ({pos.X:0.#},{pos.Y:0.#}) " + $"targeting {homestead.Name}.");
		return mobileParty;
	}

	private static void FillRaiderRoster(TroopRoster roster, int totalCount, int tier)
	{
		string[] array = TierCommonTroops[Math.Max(0, Math.Min(tier - 1, TierCommonTroops.Length - 1))];
		CharacterObject characterObject = CharacterObject.Find(BossPool[MBRandom.RandomInt(BossPool.Length)]);
		int num = totalCount;
		if (characterObject != null && totalCount > 0)
		{
			roster.AddToCounts(characterObject, 1);
			num = totalCount - 1;
		}
		int num2 = num / array.Length;
		int num3 = num - num2 * array.Length;
		for (int i = 0; i < array.Length; i++)
		{
			CharacterObject characterObject2 = CharacterObject.Find(array[i]) ?? CharacterObject.Find("looter");
			if (characterObject2 != null)
			{
				int num4 = num2 + ((i == 0) ? num3 : 0);
				if (num4 > 0)
				{
					roster.AddToCounts(characterObject2, num4);
				}
			}
		}
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_raid_quest_log}An angry mob of {COUNT} is moving toward your homestead of {HOMESTEAD}. Intercept them on the campaign map or defend the homestead when they arrive.");
		textObject.SetTextVariable("COUNT", _raiderCount);
		textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
		TextObject taskName = new TextObject("{=homestead_raid_quest_task}Defeat the Angry Mob");
		_progressLog = AddDiscreteLog(textObject, taskName, 0, 1);
		if (_raiderParty != null)
		{
			AddTrackedObject(_raiderParty);
		}
	}

	private void ApplyVictoryRewardAndComplete()
	{
		if (_questResolved)
		{
			return;
		}
		_questResolved = true;
		Hero mainHero = Hero.MainHero;
		List<Hero> list = new List<Hero>
		{
			_homestead?.HoundMasterHero,
			_homestead?.MarketLadyHero,
			_homestead?.AmbassadorHero
		};
		int num = 0;
		if (mainHero != null)
		{
			foreach (Hero item in list)
			{
				if (item != null && item.IsAlive)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, item, 5, showQuickNotification: false);
					num++;
				}
			}
		}
		TextObject textObject = new TextObject("{=homestead_raid_victory}The Angry Mob has been driven off! Your homestead notables are grateful (+{REWARD} relation each).");
		textObject.SetTextVariable("REWARD", 5);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.3f, 0.85f, 0.4f)));
		TraceLogger.Write("HomesteadRaidEventQuest", $"Raid victory: applied +{5} relation to {num} notables.");
		HomesteadChronicle.Record($"The homestead of {_homestead?.Name} drove off an angry mob of {_raiderCount} raiders.");
		_progressLog?.UpdateCurrentProgress(1);
		CompleteQuestWithSuccess();
	}

	private void DestroyRaiderPartyIfAlive()
	{
		if (_raiderParty != null && _raiderParty.IsActive)
		{
			try
			{
				DestroyPartyAction.Apply(null, _raiderParty);
			}
			catch
			{
			}
			_raiderParty = null;
		}
	}

	public void CancelRaid()
	{
		if (!_questResolved)
		{
			_questResolved = true;
			DestroyRaiderPartyIfAlive();
			CompleteQuestWithCancel();
		}
	}

	protected override void OnTimedOut()
	{
		DestroyRaiderPartyIfAlive();
		TextObject textObject = new TextObject("{=homestead_raid_dispersed}The Angry Mob approaching {HOMESTEAD} has dispersed.");
		textObject.SetTextVariable("HOMESTEAD", _homestead?.Name ?? new TextObject(string.Empty));
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase? destroyer)
	{
		if (!_questResolved)
		{
			if (party == _raiderParty)
			{
				_raiderParty = null;
				ApplyVictoryRewardAndComplete();
			}
			else if (party == _homesteadParty && !_questResolved)
			{
				_questResolved = true;
				DestroyRaiderPartyIfAlive();
				CompleteQuestWithFail(new TextObject("{=homestead_raid_lost}The homestead fell to the Angry Mob."));
			}
		}
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (attackerParty?.MobileParty == _raiderParty && defenderParty?.MobileParty == _homesteadParty)
		{
			TraceLogger.Write("HomesteadRaidEventQuest", "Angry Mob reached the homestead — battle starting, quest outcome tracked via MobilePartyDestroyed.");
		}
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (base.JournalEntries.Count > 0)
		{
			_progressLog = base.JournalEntries[0];
		}
		if (_raiderParty != null && _raiderParty.IsActive)
		{
			AddTrackedObject(_raiderParty);
		}
	}

	protected override void SetDialogs()
	{
	}
}
