using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadNotableApparelQuest : QuestBase, IHomesteadNotableQuest
{
	public const int ApparelDurationDays = 14;

	private const int ApparelFailPenalty = 3;

	public const int ApparelRelationReward = 4;

	[SaveableField(1)]
	private ItemObject _item;

	[SaveableField(2)]
	private int _equipmentIndex;

	[SaveableField(3)]
	private int _slotBit;

	[SaveableField(4)]
	private string _slotLabelKey;

	private JournalLog? _progressLog;

	public ItemObject Item => _item;

	private TextObject SlotLabel => new TextObject("{=" + _slotLabelKey + "}" + _slotLabelKey);

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_apparel_quest_title}New Attire for {NOTABLE}");
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadNotableApparelQuest(string questId, Hero notable, ItemObject item, int equipmentIndex, int slotBit, string slotLabelKey)
		: base(questId, notable, CampaignTime.DaysFromNow(14f), 0)
	{
		_item = item;
		_equipmentIndex = equipmentIndex;
		_slotBit = slotBit;
		_slotLabelKey = slotLabelKey;
		AddObjectiveLog();
	}

	public bool PlayerHasItem()
	{
		if (_item != null)
		{
			return (MobileParty.MainParty?.ItemRoster?.GetItemNumber(_item)).GetValueOrDefault() > 0;
		}
		return false;
	}

	public void CompleteHandIn()
	{
		ItemRoster itemRoster = MobileParty.MainParty?.ItemRoster;
		if (itemRoster == null || _item == null || itemRoster.GetItemNumber(_item) < 1)
		{
			return;
		}
		itemRoster.AddToCounts(_item, -1);
		if (base.QuestGiver != null)
		{
			EquipmentIndex equipmentIndex = (EquipmentIndex)_equipmentIndex;
			EquipmentElement value = new EquipmentElement(_item);
			try
			{
				base.QuestGiver.CivilianEquipment[equipmentIndex] = value;
				base.QuestGiver.BattleEquipment[equipmentIndex] = value;
			}
			catch
			{
			}
		}
		if (base.QuestGiver != null)
		{
			HomesteadBehavior.Instance?.MarkApparelSlotAwarded(base.QuestGiver, _slotBit);
		}
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, 4, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_apparel_complete}{NOTABLE} is delighted with the new {SLOT} (+{REWARD} relation).");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("SLOT", SlotLabel);
		textObject.SetTextVariable("REWARD", 4);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.5f, 0.85f, 0.5f)));
		_progressLog?.UpdateCurrentProgress(1);
		CompleteQuestWithSuccess();
	}

	public void CancelApparel()
	{
		CompleteQuestWithCancel();
	}

	protected override void OnTimedOut()
	{
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, -3, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_apparel_failed}You failed to bring {NOTABLE} the {SLOT} they wanted (-{PENALTY} relation).");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("SLOT", SlotLabel);
		textObject.SetTextVariable("PENALTY", 3);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_apparel_quest_log}{NOTABLE} has asked you to bring them a fine {ITEM} to wear. Acquire it and return to hand it over.");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("ITEM", _item?.Name ?? new TextObject(string.Empty));
		TextObject textObject2 = new TextObject("{=homestead_apparel_quest_task}Bring {ITEM} to {NOTABLE}");
		textObject2.SetTextVariable("ITEM", _item?.Name ?? new TextObject(string.Empty));
		textObject2.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		_progressLog = AddDiscreteLog(textObject, textObject2, 0, 1);
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (base.JournalEntries.Count > 0)
		{
			_progressLog = base.JournalEntries[0];
		}
	}

	protected override void SetDialogs()
	{
	}

	protected override void RegisterEvents()
	{
	}
}
