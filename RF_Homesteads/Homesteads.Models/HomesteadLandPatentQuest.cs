using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadLandPatentQuest : QuestBase
{
	public const int RequiredRelation = 40;

	public const float DeadlineDays = 60f;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private Clan _clan;

	[SaveableField(3)]
	private Settlement _town;

	public Homestead Homestead => _homestead;

	public Clan OwningClan => _clan;

	public Settlement Town => _town;

	public override TextObject Title => new TextObject("{=homestead_land_patent_title}A Land Patent");

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public bool PlayerOwnsTown
	{
		get
		{
			if (_clan != null)
			{
				return _clan == Clan.PlayerClan;
			}
			return false;
		}
	}

	public int ClanRelation
	{
		get
		{
			if (_clan == null)
			{
				return 0;
			}
			int num = int.MinValue;
			if (_clan.Heroes != null)
			{
				foreach (Hero hero in _clan.Heroes)
				{
					if (hero != null && hero.IsAlive)
					{
						int num2 = (int)hero.GetRelationWithPlayer();
						if (num2 > num)
						{
							num = num2;
						}
					}
				}
			}
			if (num == int.MinValue)
			{
				num = ((_clan.Leader != null) ? ((int)_clan.Leader.GetRelationWithPlayer()) : 0);
			}
			return num;
		}
	}

	public bool IsApprovalReady
	{
		get
		{
			if (!PlayerOwnsTown)
			{
				return ClanRelation >= 40;
			}
			return true;
		}
	}

	public HomesteadLandPatentQuest(string questId, Clan owningClan, Settlement town, Homestead homestead)
		: base(questId, owningClan?.Leader, CampaignTime.DaysFromNow(60f), 0)
	{
		_homestead = homestead;
		_clan = owningClan;
		_town = town;
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
		TextObject textObject = new TextObject("{=homestead_land_patent_start}Your homestead is large enough to grow again, but a holding this permanent needs a land patent from {CLAN}, who rule {TOWN}. Win the favour of their house (reach {RELATION} relation), then ask any of their nobles to grant it. Doing their clan's work — or sending your Ambassador to speak for you — will warm them to you.");
		textObject.SetTextVariable("CLAN", _clan?.Name ?? new TextObject(""));
		textObject.SetTextVariable("TOWN", _town?.Name ?? new TextObject(""));
		textObject.SetTextVariable("RELATION", 40);
		AddLog(textObject);
	}

	public void GrantApproval()
	{
		try
		{
			if (_homestead != null)
			{
				_homestead.OnLandPatentGranted();
				TextObject textObject = new TextObject("{=homestead_land_patent_success}{CLAN} grant your homestead a land patent. It may grow once more.");
				textObject.SetTextVariable("CLAN", _clan?.Name ?? new TextObject(""));
				AddLog(textObject);
				CompleteQuestWithSuccess();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadLandPatentQuest", "GrantApproval failed: " + ex.GetType().Name + ": " + ex.Message);
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
			TextObject textObject = new TextObject("{=homestead_land_patent_timeout}You failed to secure a land patent from {CLAN} in time. They have dispatched men-at-arms to bring your unsanctioned steading to heel.");
			textObject.SetTextVariable("CLAN", _clan?.Name ?? new TextObject(""));
			AddLog(textObject);
			HomesteadBehavior.Instance?.StartClanEnforcementRaid(_homestead, _clan);
			CompleteQuestWithFail();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadLandPatentQuest", "OnTimedOut failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	public void AbandonForRelocation()
	{
		try
		{
			AddLog(new TextObject("{=homestead_land_patent_relocated}Your homestead has moved on; its bid for a land patent here is set aside, as a different town now holds sway over its lands."));
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
			if (_town != null)
			{
				Clan ownerClan = _town.OwnerClan;
				if (ownerClan != null && ownerClan != _clan)
				{
					_clan = ownerClan;
					TextObject textObject = new TextObject("{=homestead_land_patent_retarget}{TOWN} has changed hands. You must now win the favour of {CLAN} to secure your land patent.");
					textObject.SetTextVariable("TOWN", _town.Name);
					textObject.SetTextVariable("CLAN", _clan.Name);
					AddLog(textObject);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadLandPatentQuest", "RetargetIfNeeded failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
