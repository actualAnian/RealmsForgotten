using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadRecruiterComponent : PartyComponent
{
	private const float ArrivalDistance = 3f;

	[SaveableField(1)]
	private Homestead homeHomestead;

	[SaveableField(2)]
	private Settlement targetVillage;

	[SaveableField(3)]
	private CharacterObject? escortTroop;

	[SaveableField(4)]
	private HomesteadRecruiterState state;

	[SaveableField(5)]
	private int recruitsCollected;

	[SaveableField(6)]
	private int goldSpent;

	[SaveableField(7)]
	private bool isRetiredOrDestroyed;

	[SaveableField(8)]
	private string displayName = "Homestead Recruit Group";

	public Homestead HomeHomestead => homeHomestead;

	public Settlement TargetVillage => targetVillage;

	public int RecruitsCollected => recruitsCollected;

	public int GoldSpent => goldSpent;

	public bool IsRetiredOrDestroyed => isRetiredOrDestroyed;

	private HomesteadRecruiterState State
	{
		get
		{
			if (!Enum.IsDefined(typeof(HomesteadRecruiterState), state))
			{
				return HomesteadRecruiterState.ReturningHome;
			}
			return state;
		}
		set
		{
			state = value;
		}
	}

	public override Hero PartyOwner => homeHomestead?.Leader ?? Hero.MainHero;

	public override Hero Leader => null;

	public override TextObject Name => new TextObject(displayName ?? "Homestead Recruit Group");

	public override Settlement HomeSettlement
	{
		get
		{
			object obj = homeHomestead?.HomeSettlement;
			if (obj == null)
			{
				obj = targetVillage;
				if (obj == null)
				{
					Hero mainHero = Hero.MainHero;
					if (mainHero == null)
					{
						return null;
					}
					obj = mainHero.HomeSettlement;
				}
			}
			return (Settlement)obj;
		}
	}

	public override bool AvoidHostileActions => true;

	public HomesteadRecruiterComponent(Homestead homeHomestead, Settlement targetVillage, int initialRecruits, int initialGoldSpent)
	{
		this.homeHomestead = homeHomestead;
		this.targetVillage = targetVillage;
		recruitsCollected = Math.Max(0, initialRecruits);
		goldSpent = Math.Max(0, initialGoldSpent);
		State = HomesteadRecruiterState.ReturningHome;
		displayName = Utils.GetLocalizedString("{=homestead_recruits_from}Recruits from {VILLAGE_NAME}", ("VILLAGE_NAME", targetVillage?.Name?.ToString() ?? Utils.GetLocalizedString("{=homestead_village}Village")));
	}

	public override Banner GetDefaultComponentBanner()
	{
		return homeHomestead?.Leader?.ClanBanner ?? Hero.MainHero?.ClanBanner ?? Banner.CreateOneColoredEmptyBanner(0);
	}

	protected override void OnFinalize()
	{
		isRetiredOrDestroyed = true;
	}

	internal static HomesteadRecruiterComponent? GetFor(MobileParty party)
	{
		return party?.PartyComponent as HomesteadRecruiterComponent;
	}

	internal static bool HasActiveRecruiterFor(Homestead homestead)
	{
		if (homestead == null || Campaign.Current == null)
		{
			return false;
		}
		foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
		{
			if (mobileParty?.PartyComponent is HomesteadRecruiterComponent recruiter && IsActiveRecruiterFor(homestead, mobileParty, recruiter))
			{
				return true;
			}
		}
		return false;
	}

	internal static int GetActiveRecruiterSummaryFor(Homestead homestead, HashSet<Settlement> sourceVillagesInFlight)
	{
		sourceVillagesInFlight?.Clear();
		if (homestead == null || Campaign.Current == null)
		{
			return 0;
		}
		int num = 0;
		foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
		{
			if (mobileParty?.PartyComponent is HomesteadRecruiterComponent homesteadRecruiterComponent && IsActiveRecruiterFor(homestead, mobileParty, homesteadRecruiterComponent))
			{
				num += Math.Max(0, homesteadRecruiterComponent.recruitsCollected);
				if (homesteadRecruiterComponent.targetVillage != null)
				{
					sourceVillagesInFlight?.Add(homesteadRecruiterComponent.targetVillage);
				}
			}
		}
		return num;
	}

	private static bool IsActiveRecruiterFor(Homestead homestead, MobileParty party, HomesteadRecruiterComponent recruiter)
	{
		if (homestead != null && party != null && recruiter != null && recruiter.homeHomestead == homestead && !recruiter.isRetiredOrDestroyed && party.IsActive)
		{
			return !party.IsDisbanding;
		}
		return false;
	}

	internal static bool TryCreateAndDispatch(Homestead homestead, Settlement targetVillage, int alreadyCollected, out MobileParty? recruiterParty, out int recruitedCount)
	{
		recruiterParty = null;
		recruitedCount = 0;
		if (homestead?.MobileParty == null || targetVillage == null)
		{
			return false;
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		var (num, num2) = homestead.RecruitVolunteersFromVillageToRoster(troopRoster, targetVillage, alreadyCollected);
		if (num <= 0)
		{
			return false;
		}
		string stringId = "homestead_recruiter_" + Guid.NewGuid().ToString("N").Substring(0, 8);
		HomesteadRecruiterComponent homesteadRecruiterComponent = new HomesteadRecruiterComponent(homestead, targetVillage, num, num2);
		recruiterParty = MobileParty.CreateParty(stringId, homesteadRecruiterComponent);
		TroopRoster troopRoster2 = new TroopRoster(recruiterParty.Party);
		troopRoster2.Add(troopRoster);
		recruiterParty.InitializeMobilePartyAroundPosition(troopRoster2, new TroopRoster(recruiterParty.Party), new CampaignVec2(targetVillage.GetPosition2D, isOnLand: true), 1f);
		recruiterParty.ActualClan = homestead.Leader?.Clan ?? Hero.MainHero?.Clan;
		recruiterParty.ShouldJoinPlayerBattles = false;
		recruiterParty.Party.SetCustomName(homesteadRecruiterComponent.Name);
		recruiterParty.Party.SetVisualAsDirty();
		homesteadRecruiterComponent.MoveTowardHome("creation");
		TraceLogger.Write("HomesteadRecruiterComponent", $"Created visible recruit group '{recruiterParty.StringId}' for homestead='{homestead.Name}' sourceVillage='{targetVillage.StringId}' recruits={num} cost={num2}.");
		recruitedCount = num;
		return true;
	}

	internal void HourlyTick()
	{
		if (State == HomesteadRecruiterState.Completed)
		{
			return;
		}
		if (base.MobileParty == null || base.MobileParty.IsDisbanding || !base.MobileParty.IsActive)
		{
			isRetiredOrDestroyed = true;
		}
		else
		{
			if (base.MobileParty.MapEvent != null)
			{
				return;
			}
			if (base.MobileParty.MemberRoster.TotalManCount <= 0)
			{
				TraceLogger.Write("HomesteadRecruiterComponent", "Recruiter party '" + base.MobileParty.StringId + "' has 0 members (wiped in combat?) — destroying ghost.");
				CompleteAndDestroy("all troops lost");
				return;
			}
			if (!IsHomeValid())
			{
				CompleteAndDestroy("home invalid");
				return;
			}
			switch (State)
			{
			case HomesteadRecruiterState.TravelingToVillage:
				TickTravelingToVillage();
				break;
			case HomesteadRecruiterState.ReturningHome:
				TickReturningHome();
				break;
			case HomesteadRecruiterState.WaitingForCapacity:
				TickWaitingForCapacity();
				break;
			}
		}
	}

	private void TickTravelingToVillage()
	{
		if (!homeHomestead.IsValidAutoRecruitVillage(targetVillage))
		{
			SetReturnMode("target village no longer valid");
		}
		else if (IsAtOrNearSettlement(targetVillage))
		{
			RecruitAtTargetVillage();
			SetReturnMode("recruitment complete");
		}
		else
		{
			DispatchToVillage("hourly travel");
		}
	}

	private void TickReturningHome()
	{
		if (IsNearHome())
		{
			int num = homeHomestead.DepositRecruiterTroops(base.MobileParty, escortTroop);
			recruitsCollected = Math.Max(0, recruitsCollected - num);
			if (recruitsCollected <= 0)
			{
				CompleteAndDestroy("deposited recruits");
				return;
			}
			State = HomesteadRecruiterState.WaitingForCapacity;
			base.MobileParty.SetMoveModeHold();
			TraceLogger.Write("HomesteadRecruiterComponent", $"Recruit group '{base.MobileParty.StringId}' is waiting at homestead='{homeHomestead.Name}' with {recruitsCollected} recruits because capacity is full.");
		}
		else
		{
			MoveTowardHome("hourly return");
		}
	}

	private void TickWaitingForCapacity()
	{
		int num = homeHomestead.DepositRecruiterTroops(base.MobileParty, escortTroop);
		recruitsCollected = Math.Max(0, recruitsCollected - num);
		if (recruitsCollected <= 0)
		{
			CompleteAndDestroy("deposited waiting recruits");
		}
		else
		{
			base.MobileParty.SetMoveModeHold();
		}
	}

	private void RecruitAtTargetVillage()
	{
		var (num, num2) = homeHomestead.RecruitVolunteersFromVillageToParty(base.MobileParty, targetVillage, recruitsCollected);
		recruitsCollected += num;
		goldSpent += num2;
		TraceLogger.Write("HomesteadRecruiterComponent", string.Format("Recruit group '{0}' collected {1} recruits from village='{2}' cost={3} totalCollected={4} totalSpent={5}.", base.MobileParty.StringId, num, targetVillage?.StringId ?? "null", num2, recruitsCollected, goldSpent));
	}

	private void DispatchToVillage(string reason)
	{
		if (targetVillage != null)
		{
			LeaveCurrentSettlementIfNeeded(reason);
			base.MobileParty.SetMoveGoToSettlement(targetVillage, MobileParty.NavigationType.Default, isTargetingThePort: false);
		}
	}

	private void SetReturnMode(string reason)
	{
		State = HomesteadRecruiterState.ReturningHome;
		MoveTowardHome(reason);
	}

	private void MoveTowardHome(string reason)
	{
		LeaveCurrentSettlementIfNeeded(reason);
		base.MobileParty.SetMoveGoToPoint(homeHomestead.MobileParty.Position, MobileParty.NavigationType.Default);
	}

	private void LeaveCurrentSettlementIfNeeded(string reason)
	{
		if (base.MobileParty?.CurrentSettlement == null)
		{
			return;
		}
		try
		{
			LeaveSettlementAction.ApplyForParty(base.MobileParty);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadRecruiterComponent", string.Format("LeaveSettlementAction failed for recruiter '{0}' during {1}: {2}", base.MobileParty?.StringId ?? "null", reason, arg));
		}
	}

	private bool IsAtOrNearSettlement(Settlement settlement)
	{
		if (settlement == null || base.MobileParty == null)
		{
			return false;
		}
		if (base.MobileParty.CurrentSettlement == settlement)
		{
			return true;
		}
		return base.MobileParty.GetPosition2D.Distance(settlement.GetPosition2D) <= 3f;
	}

	private bool IsNearHome()
	{
		if (base.MobileParty == null || homeHomestead?.MobileParty == null)
		{
			return false;
		}
		return base.MobileParty.GetPosition2D.Distance(homeHomestead.MobileParty.GetPosition2D) <= 3f;
	}

	private bool IsHomeValid()
	{
		if (homeHomestead != null && !homeHomestead.IsRetiredOrDestroyed && homeHomestead.MobileParty != null && homeHomestead.MobileParty.IsActive)
		{
			return !homeHomestead.MobileParty.IsDisbanding;
		}
		return false;
	}

	private void CompleteAndDestroy(string reason)
	{
		if (base.MobileParty == null)
		{
			isRetiredOrDestroyed = true;
			State = HomesteadRecruiterState.Completed;
			return;
		}
		TraceLogger.Write("HomesteadRecruiterComponent", $"Destroying recruit group '{base.MobileParty.StringId}' during {reason}; totalCollected={recruitsCollected} totalSpent={goldSpent}.");
		isRetiredOrDestroyed = true;
		State = HomesteadRecruiterState.Completed;
		try
		{
			DestroyPartyAction.Apply(null, base.MobileParty);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadRecruiterComponent", $"DestroyPartyAction failed for recruiter '{base.MobileParty.StringId}': {arg}");
		}
	}
}
