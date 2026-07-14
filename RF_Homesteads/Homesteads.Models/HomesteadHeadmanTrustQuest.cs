using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadHeadmanTrustQuest : QuestBase
{
	public const int RequiredRelation = 40;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private Hero _headman;

	[SaveableField(3)]
	private Settlement _village;

	public const float DeadlineDays = 30f;

	public Homestead Homestead => _homestead;

	public Hero Headman => _headman;

	public Settlement Village => _village;

	public override TextObject Title => new TextObject("{=homestead_headman_trust_title}The Headman's Trust");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public bool IsApprovalReady
	{
		get
		{
			if (_headman != null && _headman.IsAlive)
			{
				return _headman.GetRelationWithPlayer() >= 40f;
			}
			return false;
		}
	}

	public int CurrentRelation
	{
		get
		{
			if (_headman == null)
			{
				return 0;
			}
			return (int)_headman.GetRelationWithPlayer();
		}
	}

	public HomesteadHeadmanTrustQuest(string questId, Hero headman, Settlement village, Homestead homestead)
		: base(questId, headman, CampaignTime.DaysFromNow(30f), 0)
	{
		_homestead = homestead;
		_headman = headman;
		_village = village;
		AddStartLog();
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

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (_homestead != null)
		{
			_homestead.AmbassadorAidingHeadman = false;
		}
	}

	public void ForceTimeout()
	{
		OnTimedOut();
	}

	protected override void OnTimedOut()
	{
		try
		{
			TextObject textObject = new TextObject("{=homestead_headman_trust_timeout}You failed to win the blessing of {HEADMAN} in time. The villagers of {VILLAGE}, resentful of your encroachment, march on your homestead.");
			textObject.SetTextVariable("HEADMAN", _headman?.Name ?? new TextObject(""));
			textObject.SetTextVariable("VILLAGE", _village?.Name ?? new TextObject(""));
			AddLog(textObject);
			HomesteadBehavior.Instance?.StartAngryVillagersRaid(_homestead, _headman);
			CompleteQuestWithFail();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadHeadmanTrustQuest", "OnTimedOut failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	protected override void HourlyTick()
	{
		RetargetIfNeeded();
	}

	private void AddStartLog()
	{
		TextObject textObject = new TextObject("{=homestead_headman_trust_start}Your homestead has grown enough to expand, but a larger, more permanent steading encroaches on the lands of {VILLAGE}. Win the trust of its headman, {HEADMAN} (reach {RELATION} relation), then ask for his blessing to grow. Doing his people's tasks — or sending your Ambassador to speak on your behalf — will warm him to you.");
		textObject.SetTextVariable("VILLAGE", _village?.Name ?? new TextObject("{=homestead_the_nearby_village}the nearby village"));
		textObject.SetTextVariable("HEADMAN", _headman?.Name ?? new TextObject("{=homestead_the_headman}the headman"));
		textObject.SetTextVariable("RELATION", 40);
		AddLog(textObject);
	}

	public void GrantApproval()
	{
		try
		{
			if (_homestead != null)
			{
				_homestead.OnHeadmanApprovalGranted();
				TextObject textObject = new TextObject("{=homestead_headman_trust_success}{HEADMAN} of {VILLAGE} gives his blessing for your homestead to grow. Construction may resume.");
				textObject.SetTextVariable("HEADMAN", _headman?.Name ?? new TextObject(""));
				textObject.SetTextVariable("VILLAGE", _village?.Name ?? new TextObject(""));
				AddLog(textObject);
				CompleteQuestWithSuccess();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadHeadmanTrustQuest", "GrantApproval failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void AbandonForRelocation()
	{
		try
		{
			AddLog(new TextObject("{=homestead_headman_trust_relocated}Your homestead has moved on, so its bid to expand here is set aside — a new village's headman now holds the lands you encroach upon."));
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void CancelForTeardown()
	{
		try
		{
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void RetargetIfNeeded()
	{
		try
		{
			if (_village != null && (_headman == null || !_headman.IsAlive || !_headman.IsHeadman || Homesteads.Models.Homestead.FindHeadmanOfVillage(_village) != _headman))
			{
				Hero hero = Homesteads.Models.Homestead.FindHeadmanOfVillage(_village);
				if (hero != null && hero != _headman)
				{
					_headman = hero;
					TextObject textObject = new TextObject("{=homestead_headman_trust_retarget}{VILLAGE} has a new headman, {HEADMAN}. You will need to earn his trust instead.");
					textObject.SetTextVariable("VILLAGE", _village.Name);
					textObject.SetTextVariable("HEADMAN", _headman.Name);
					AddLog(textObject);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadHeadmanTrustQuest", "RetargetIfNeeded failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
