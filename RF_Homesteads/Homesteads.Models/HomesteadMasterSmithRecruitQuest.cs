using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadMasterSmithRecruitQuest : QuestBase
{
	public const int OrdersRequired = 10;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private bool _pitchHeard;

	[SaveableField(3)]
	private int _ordersCompleted;

	[SaveableField(4)]
	private Settlement? _pitchSettlement;

	public Homestead Homestead => _homestead;

	public bool PitchHeard => _pitchHeard;

	public int OrdersCompleted => _ordersCompleted;

	public bool OrdersDone => _ordersCompleted >= 10;

	public Settlement? PitchSettlement => _pitchSettlement;

	public override TextObject Title => new TextObject("{=homestead_smith_quest_title}A Smith for the Homestead");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadMasterSmithRecruitQuest(string questId, Hero questGiver, Homestead homestead)
		: base(questId, questGiver, CampaignTime.DaysFromNow(120f), 0)
	{
		_homestead = homestead;
		_pitchHeard = false;
		_ordersCompleted = 0;
		AddLog(new TextObject("{=homestead_smith_quest_start}Your homestead's smithy stands ready, but it needs a master to work it. Seek out a Blacksmith in any town and persuade them to take up the hammer at your homestead."));
	}

	protected override void InitializeQuestOnGameLoad()
	{
	}

	protected override void HourlyTick()
	{
	}

	protected override void RegisterEvents()
	{
	}

	protected override void SetDialogs()
	{
	}

	protected override void OnTimedOut()
	{
		try
		{
			AddLog(new TextObject("{=homestead_smith_quest_expired}The search for a Master Smith was abandoned — too much time has passed."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void MarkPitchHeard(Settlement? pitchSettlement)
	{
		if (_pitchHeard)
		{
			return;
		}
		_pitchHeard = true;
		_pitchSettlement = pitchSettlement;
		try
		{
			AddLog(BuildProgressLog());
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadMasterSmithRecruitQuest", "MarkPitchHeard (AddLog) threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void RegisterOrderCompleted()
	{
		if (!_pitchHeard || OrdersDone)
		{
			return;
		}
		_ordersCompleted++;
		try
		{
			if (OrdersDone)
			{
				TextObject textObject = new TextObject("{=homestead_smith_quest_orders_done}You have proven your dedication to the craft. Return to the blacksmith in {SETTLEMENT} and offer them a place at your homestead.");
				textObject.SetTextVariable("SETTLEMENT", _pitchSettlement?.Name ?? new TextObject("{=homestead_smith_quest_town_fallback}the town where you made your offer"));
				AddLog(textObject);
			}
			else
			{
				AddLog(BuildProgressLog());
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadMasterSmithRecruitQuest", "RegisterOrderCompleted (AddLog) threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private TextObject BuildProgressLog()
	{
		TextObject textObject = new TextObject("{=homestead_smith_quest_progress}Prove your dedication to the craft: complete {DONE} of {TOTAL} smithing orders in any town, then return to the blacksmith in {SETTLEMENT}. ({DONE}/{TOTAL})");
		textObject.SetTextVariable("DONE", _ordersCompleted);
		textObject.SetTextVariable("TOTAL", 10);
		textObject.SetTextVariable("SETTLEMENT", _pitchSettlement?.Name ?? new TextObject("{=homestead_smith_quest_town_fallback}the town where you made your offer"));
		return textObject;
	}

	public void CompleteRecruitment(BodyProperties? templateBody = null, int templateAge = -1)
	{
		try
		{
			AddLog(new TextObject("{=homestead_smith_quest_success}The Blacksmith has accepted your offer and will serve as Master Smith at your homestead's forge."));
			_homestead.MasterSmithRecruited = true;
			_homestead.TryEnsureMasterSmithHero(templateBody, templateAge);
			CompleteQuestWithSuccess();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadMasterSmithRecruitQuest", "CompleteRecruitment threw " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	public void CancelRecruitment()
	{
		try
		{
			AddLog(new TextObject("{=homestead_smith_quest_cancelled}The search for a Master Smith has been abandoned."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}
}
