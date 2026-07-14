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

public class HomesteadPackageDeliveryQuest : QuestBase, IHomesteadNotableQuest
{
	public const int DeliveryDurationDays = 14;

	private const int DeliveryFailPenalty = 3;

	public const int DeliveryRelationReward = 5;

	[SaveableField(1)]
	private ItemObject _package;

	[SaveableField(2)]
	private Hero _recipient;

	[SaveableField(3)]
	private Settlement? _recipientSettlement;

	private JournalLog? _progressLog;

	public Hero Recipient => _recipient;

	public ItemObject Package => _package;

	public Settlement? RecipientSettlement => _recipientSettlement;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_pkg_quest_title}Delivery: {ITEM} for {RECIPIENT}");
			textObject.SetTextVariable("ITEM", _package?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadPackageDeliveryQuest(string questId, Hero questGiver, ItemObject package, Hero recipient)
		: base(questId, questGiver, CampaignTime.DaysFromNow(14f), 0)
	{
		_package = package;
		_recipient = recipient;
		_recipientSettlement = recipient.HomeSettlement;
		MobileParty.MainParty?.ItemRoster?.AddToCounts(_package, 1);
		AddObjectiveLog();
	}

	public bool PlayerHasPackage()
	{
		if (_package != null)
		{
			return (MobileParty.MainParty?.ItemRoster?.GetItemNumber(_package)).GetValueOrDefault() > 0;
		}
		return true;
	}

	private void TakePackageFromPlayer()
	{
		ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
		if (itemRoster != null && _package != null && itemRoster.GetItemNumber(_package) > 0)
		{
			itemRoster.AddToCounts(_package, -1);
		}
	}

	public void CompleteDelivery()
	{
		TakePackageFromPlayer();
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, 5, showQuickNotification: false);
		}
		if (mainHero != null && _recipient != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, _recipient, 5, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_pkg_complete}Delivery complete! {PKG_SENDER} will be pleased (+{REWARD} relation). {RECIPIENT} is also grateful (+5 relation).");
		textObject.SetTextVariable("PKG_SENDER", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("REWARD", 5);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.5f, 0.85f, 0.5f)));
		_progressLog?.UpdateCurrentProgress(1);
		CompleteQuestWithSuccess();
	}

	public void CancelDelivery()
	{
		TakePackageFromPlayer();
		CompleteQuestWithCancel();
	}

	protected override void OnTimedOut()
	{
		TakePackageFromPlayer();
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, -3, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_pkg_failed}You failed to deliver {ITEM} to {RECIPIENT} in time (-{PENALTY} relation).");
		textObject.SetTextVariable("ITEM", _package?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("PENALTY", 3);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_pkg_quest_log}{SENDER} has asked you to deliver {ITEM} to {RECIPIENT} in {SETTLEMENT}. The package has been added to your inventory. Find {RECIPIENT} and speak with them to hand it over.");
		textObject.SetTextVariable("SENDER", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("ITEM", _package?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("SETTLEMENT", _recipientSettlement?.Name ?? new TextObject(string.Empty));
		TextObject textObject2 = new TextObject("{=homestead_pkg_quest_task}Deliver {ITEM} to {RECIPIENT} in {SETTLEMENT}");
		textObject2.SetTextVariable("ITEM", _package?.Name ?? new TextObject(string.Empty));
		textObject2.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
		textObject2.SetTextVariable("SETTLEMENT", _recipientSettlement?.Name ?? new TextObject(string.Empty));
		_progressLog = AddDiscreteLog(textObject, textObject2, 0, 1);
	}

	protected override void SetDialogs()
	{
	}

	protected override void RegisterEvents()
	{
		if (_recipientSettlement != null)
		{
			AddTrackedObject(_recipientSettlement);
		}
		if (_recipient != null)
		{
			AddTrackedObject(_recipient);
		}
		CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnTargetVillageRaided);
	}

	private void OnTargetVillageRaided(Village village)
	{
		if (village?.Settlement != null && _recipientSettlement != null && village.Settlement == _recipientSettlement)
		{
			TakePackageFromPlayer();
			TextObject textObject = new TextObject("{=homestead_pkg_raided}{SETTLEMENT} has been raided, so your delivery of {ITEM} to {RECIPIENT} is cancelled. No one holds it against you.");
			textObject.SetTextVariable("SETTLEMENT", _recipientSettlement?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("ITEM", _package?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("RECIPIENT", _recipient?.Name ?? new TextObject(string.Empty));
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.75f, 0.4f)));
			CompleteQuestWithCancel();
		}
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (base.JournalEntries.Count > 0)
		{
			_progressLog = base.JournalEntries[0];
		}
		if (_recipientSettlement != null)
		{
			AddTrackedObject(_recipientSettlement);
		}
		if (_recipient != null)
		{
			AddTrackedObject(_recipient);
		}
	}
}
