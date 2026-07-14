using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadArmsMasterRecruitQuest : QuestBase
{
	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private bool _tournamentWon;

	[SaveableField(3)]
	private Settlement? _tournamentSettlement;

	public Homestead Homestead => _homestead;

	public bool TournamentWon => _tournamentWon;

	public Settlement? TournamentSettlement => _tournamentSettlement;

	public override TextObject Title => new TextObject("{=homestead_arms_master_quest_title}Find an Arms Master");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadArmsMasterRecruitQuest(string questId, Hero questGiver, Homestead homestead)
		: base(questId, questGiver, CampaignTime.DaysFromNow(90f), 0)
	{
		_homestead = homestead;
		_tournamentWon = false;
		_tournamentSettlement = null;
		AddLog(new TextObject("{=homestead_arms_master_quest_start}Your homestead requires an experienced warrior to train the guards. Win a tournament to prove your prowess and attract the attention of a skilled Arms Master."));
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
			AddLog(new TextObject("{=homestead_arms_master_quest_expired}The search for an Arms Master was abandoned — time has passed without a tournament victory."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void MarkTournamentWon(Town town)
	{
		if (_tournamentWon)
		{
			return;
		}
		_tournamentWon = true;
		_tournamentSettlement = town?.Settlement;
		try
		{
			TextObject textObject = new TextObject("{=homestead_arms_master_quest_tournament_won}Your victory has drawn the attention of a seasoned warrior. Return to {TOWN_NAME} and speak with the Tournament Master at the arena to offer them a place at your homestead.");
			textObject.SetTextVariable("TOWN_NAME", town?.Name ?? new TextObject("the city"));
			AddLog(textObject);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadArmsMasterRecruitQuest", "MarkTournamentWon (AddLog) threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void CompleteOnTournamentWin(BodyProperties? templateBody = null, int templateAge = -1)
	{
		try
		{
			AddLog(new TextObject("{=homestead_arms_master_quest_success}The Tournament Master has accepted your offer and will serve as Arms Master at your homestead's training grounds."));
			_homestead.CreateArmsMasterHero(templateBody, templateAge);
			CompleteQuestWithSuccess();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadArmsMasterRecruitQuest", "CompleteOnTournamentWin threw " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	public void CancelRecruitment()
	{
		try
		{
			AddLog(new TextObject("{=homestead_arms_master_quest_cancelled}The search for an Arms Master has been abandoned."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}
}
