using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadStableMasterRecruitQuest : QuestBase
{
	[SaveableField(1)]
	private Homestead _homestead;

	public Homestead Homestead => _homestead;

	public override TextObject Title => new TextObject("{=homestead_stable_quest_title}A Master for the Stable");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public bool HasRequiredHorses
	{
		get
		{
			if (_homestead != null && _homestead.HasStable)
			{
				return _homestead.HasStableFoundingHerd();
			}
			return false;
		}
	}

	public string HerdProgress => _homestead?.GetFoundingHerdProgress() ?? "";

	public HomesteadStableMasterRecruitQuest(string questId, Hero questGiver, Homestead homestead)
		: base(questId, questGiver, CampaignTime.DaysFromNow(120f), 0)
	{
		_homestead = homestead;
		AddLog(new TextObject("{=homestead_stable_quest_start}Your stable stands empty. Gather a founding herd — two each of pack animals, riding horses, and war horses, plus a noble mount — and bring them to your homestead, then tell your homestead's leader. Fine animals will draw a skilled stable master to your door."));
	}

	protected override void InitializeQuestOnGameLoad()
	{
	}

	protected override void RegisterEvents()
	{
	}

	protected override void SetDialogs()
	{
	}

	protected override void HourlyTick()
	{
	}

	public void CompleteByDelivery()
	{
		try
		{
			if (_homestead != null && !_homestead.StableMasterRecruited && HasRequiredHorses)
			{
				_homestead.ConsumeStableFoundingHerd();
				_homestead.StableMasterRecruited = true;
				_homestead.TryEnsureStableMasterHero();
				AddLog(new TextObject("{=homestead_stable_quest_success}Word of your fine animals has spread, and a skilled stable master has come to take charge of your homestead's stable."));
				MBInformationManager.AddQuickInformation(new TextObject("{=homestead_stable_master_arrived}A stable master has been drawn to your homestead."), 0, _homestead.StableMasterHero?.CharacterObject);
				CompleteQuestWithSuccess();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadStableMasterRecruitQuest", "CompleteByDelivery failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	protected override void OnTimedOut()
	{
		try
		{
			AddLog(new TextObject("{=homestead_stable_quest_expired}The search for a stable master was abandoned — too much time has passed."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void CancelRecruitment()
	{
		try
		{
			AddLog(new TextObject("{=homestead_stable_quest_cancelled}The search for a stable master has been abandoned."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}
}
