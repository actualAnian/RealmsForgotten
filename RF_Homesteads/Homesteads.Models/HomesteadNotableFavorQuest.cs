using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadNotableFavorQuest : QuestBase, IHomesteadNotableQuest
{
	public const int FavorDurationDays = 14;

	private const int FavorFailRelationPenalty = 3;

	[SaveableField(1)]
	private ItemObject _item;

	[SaveableField(2)]
	private int _requiredCount;

	[SaveableField(3)]
	private MobileParty _homesteadParty;

	[SaveableField(4)]
	private int _deliveredCount;

	private JournalLog? _progressLog;

	public ItemObject Item => _item;

	public int RequiredCount => _requiredCount;

	public int DeliveredCount => _deliveredCount;

	public int RemainingNeeded => Math.Max(0, _requiredCount - _deliveredCount);

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_favor_quest_title}A Favor for {NOTABLE}");
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadNotableFavorQuest(string questId, Hero questGiver, MobileParty homesteadParty, ItemObject item, int requiredCount)
		: base(questId, questGiver, CampaignTime.DaysFromNow(14f), 0)
	{
		_item = item;
		_requiredCount = requiredCount;
		_homesteadParty = homesteadParty;
		_deliveredCount = 0;
		AddObjectiveLog();
	}

	protected override void OnTimedOut()
	{
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, -3, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_favor_failed}You failed to deliver {ITEM} to {NOTABLE} in time (-{PENALTY} relation).");
		textObject.SetTextVariable("ITEM", _item?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("PENALTY", 3);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
	}

	private int CurrentPlayerCount()
	{
		if (_item == null)
		{
			return 0;
		}
		return (MobileParty.MainParty?.ItemRoster?.GetItemNumber(_item)).GetValueOrDefault();
	}

	public new bool IsReady()
	{
		if (CurrentPlayerCount() >= RemainingNeeded)
		{
			return RemainingNeeded > 0;
		}
		return false;
	}

	public bool HasPartialInInventory()
	{
		int num = CurrentPlayerCount();
		if (num > 0)
		{
			return num < RemainingNeeded;
		}
		return false;
	}

	public int PlayerCarryCount()
	{
		return Math.Min(CurrentPlayerCount(), RemainingNeeded);
	}

	public void PartialDeliver()
	{
		int num = PlayerCarryCount();
		if (num > 0)
		{
			ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
			if (itemRoster != null)
			{
				itemRoster.AddToCounts(_item, -num);
				_deliveredCount += num;
				RefreshProgress();
				TraceLogger.Write("HomesteadNotableFavorQuest", $"PartialDeliver: took {num}x{_item?.StringId} from player, delivered={_deliveredCount}/{_requiredCount}.");
			}
		}
	}

	public void TurnInAndComplete()
	{
		int remainingNeeded = RemainingNeeded;
		ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
		if (itemRoster != null && _item != null && itemRoster.GetItemNumber(_item) >= remainingNeeded)
		{
			itemRoster.AddToCounts(_item, -remainingNeeded);
			_deliveredCount = _requiredCount;
			CompleteQuestWithSuccess();
		}
	}

	public void CancelFavor()
	{
		CompleteQuestWithCancel();
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_favor_quest_log}{NOTABLE} of {HOMESTEAD} has asked you to bring {COUNT} {ITEM}. Carry them in your party inventory, then return to hand them over.");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("HOMESTEAD", _homesteadParty?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("COUNT", _requiredCount);
		textObject.SetTextVariable("ITEM", _item?.Name ?? new TextObject(string.Empty));
		TextObject textObject2 = new TextObject("{=homestead_favor_quest_task}Deliver {ITEM}");
		textObject2.SetTextVariable("ITEM", _item?.Name ?? new TextObject(string.Empty));
		_progressLog = AddDiscreteLog(textObject, textObject2, _deliveredCount, _requiredCount);
	}

	public void RefreshProgress()
	{
		_progressLog?.UpdateCurrentProgress(_deliveredCount);
	}

	protected override void InitializeQuestOnGameLoad()
	{
	}

	protected override void SetDialogs()
	{
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, RefreshProgress);
	}
}
