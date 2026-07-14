using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadSettlementCharterQuest : QuestBase
{
	public const int VassalRelation = 40;

	public const int OutsiderRelation = 60;

	public const float DeadlineDays = 365f;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private Kingdom _kingdom;

	[SaveableField(3)]
	private Settlement _settlement;

	public Homestead Homestead => _homestead;

	public Kingdom Kingdom => _kingdom;

	public Settlement Settlement => _settlement;

	public Hero? Ruler => _kingdom?.Leader;

	public override TextObject Title => new TextObject("{=homestead_charter_title}A Settlement Charter");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public bool PlayerIsRuler
	{
		get
		{
			if (_kingdom != null)
			{
				return _kingdom.RulingClan == Clan.PlayerClan;
			}
			return false;
		}
	}

	public bool PlayerIsVassal
	{
		get
		{
			if (_kingdom != null && Clan.PlayerClan?.Kingdom == _kingdom)
			{
				return !PlayerIsRuler;
			}
			return false;
		}
	}

	public int RequiredRelation
	{
		get
		{
			if (!PlayerIsVassal)
			{
				return 60;
			}
			return 40;
		}
	}

	public bool IsApprovalReady
	{
		get
		{
			if (!PlayerIsRuler)
			{
				if (Ruler != null && Ruler.IsAlive)
				{
					return Ruler.GetRelationWithPlayer() >= (float)RequiredRelation;
				}
				return false;
			}
			return true;
		}
	}

	public HomesteadSettlementCharterQuest(string questId, Kingdom kingdom, Settlement settlement, Homestead homestead)
		: base(questId, kingdom?.Leader, CampaignTime.DaysFromNow(365f), 0)
	{
		_homestead = homestead;
		_kingdom = kingdom;
		_settlement = settlement;
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

	protected override void HourlyTick()
	{
		RetargetIfNeeded();
	}

	private void AddStartLog()
	{
		TextObject textObject = new TextObject("{=homestead_charter_start}Your homestead is ready to become a settlement, but raising a town and castle requires a royal charter. Win the leave of {RULER}, who rules {KINGDOM} — the realm holding {SETTLEMENT}. {THRESHOLD}");
		textObject.SetTextVariable("RULER", Ruler?.Name ?? new TextObject(""));
		textObject.SetTextVariable("KINGDOM", _kingdom?.Name ?? new TextObject(""));
		textObject.SetTextVariable("SETTLEMENT", _settlement?.Name ?? new TextObject(""));
		textObject.SetTextVariable("THRESHOLD", new TextObject(PlayerIsVassal ? "{=homestead_charter_thresh_vassal}As their vassal you must reach 40 relation with them, then ask for the charter." : "{=homestead_charter_thresh_outsider}As an outsider you must reach 60 relation with them, then ask for the charter."));
		AddLog(textObject);
	}

	public void GrantApproval()
	{
		try
		{
			if (_homestead != null)
			{
				_homestead.OnSettlementCharterGranted();
				TextObject textObject = new TextObject("{=homestead_charter_success}{RULER} grants your homestead a charter to raise a settlement. You may now build it into a town and castle of your own.");
				textObject.SetTextVariable("RULER", Ruler?.Name ?? new TextObject(""));
				AddLog(textObject);
				CompleteQuestWithSuccess();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementCharterQuest", "GrantApproval failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	protected override void OnTimedOut()
	{
		try
		{
			TextObject textObject = new TextObject("{=homestead_charter_timeout}You did not secure a settlement charter from {RULER} in time. Your homestead waits, ready, until you try again.");
			textObject.SetTextVariable("RULER", Ruler?.Name ?? new TextObject(""));
			AddLog(textObject);
			CompleteQuestWithFail();
		}
		catch
		{
		}
	}

	public void AbandonForRelocation()
	{
		try
		{
			AddLog(new TextObject("{=homestead_charter_relocated}Your homestead has moved on; its petition for a charter here is set aside, as a different realm now holds the nearest lands."));
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
			if (_settlement != null)
			{
				Kingdom kingdom = _settlement.OwnerClan?.Kingdom;
				if (kingdom != null && kingdom != _kingdom)
				{
					_kingdom = kingdom;
					TextObject textObject = new TextObject("{=homestead_charter_retarget}{SETTLEMENT} now answers to {KINGDOM}. You must seek your charter from {RULER} instead.");
					textObject.SetTextVariable("SETTLEMENT", _settlement.Name);
					textObject.SetTextVariable("KINGDOM", _kingdom.Name);
					textObject.SetTextVariable("RULER", Ruler?.Name ?? new TextObject(""));
					AddLog(textObject);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementCharterQuest", "RetargetIfNeeded failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
