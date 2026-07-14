using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadBuildingRequestQuest : QuestBase, IHomesteadNotableQuest
{
	public const int BuildingDurationDays = 21;

	private const int BuildingFailPenalty = 3;

	public const int BuildingRelationReward = 4;

	[SaveableField(1)]
	private Homestead _homestead;

	[SaveableField(2)]
	private string _prefabName;

	[SaveableField(3)]
	private string _displayName;

	[SaveableField(4)]
	private int _baselineCount;

	private JournalLog? _progressLog;

	public Homestead Homestead => _homestead;

	public string PrefabName => _prefabName;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_build_quest_title}A Building for {NOTABLE}");
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadBuildingRequestQuest(string questId, Hero notable, Homestead homestead, string prefabName, string displayName, int baselineCount)
		: base(questId, notable, CampaignTime.DaysFromNow(21f), 0)
	{
		_homestead = homestead;
		_prefabName = prefabName;
		_displayName = displayName;
		_baselineCount = baselineCount;
		AddObjectiveLog();
	}

	public bool IsTargetBuilt()
	{
		if (_homestead == null)
		{
			return false;
		}
		int num = _homestead.CountBuiltPlaceable(_prefabName);
		TraceLogger.Write("HomesteadBuildingRequestQuest", $"IsTargetBuilt: '{_prefabName}' count={num} baseline={_baselineCount}.");
		return num > _baselineCount;
	}

	public void CompleteIfBuilt()
	{
		if (IsTargetBuilt())
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && base.QuestGiver != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, 4, showQuickNotification: false);
			}
			TextObject textObject = new TextObject("{=homestead_build_complete}{NOTABLE} is pleased with the new {BUILDING} (+{REWARD} relation).");
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("BUILDING", _displayName);
			textObject.SetTextVariable("REWARD", 4);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.5f, 0.85f, 0.5f)));
			_progressLog?.UpdateCurrentProgress(1);
			CompleteQuestWithSuccess();
		}
	}

	public void CancelBuilding()
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
		TextObject textObject = new TextObject("{=homestead_build_failed}You never built the {BUILDING} {NOTABLE} asked for (-{PENALTY} relation).");
		textObject.SetTextVariable("BUILDING", _displayName);
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("PENALTY", 3);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_build_quest_log}{NOTABLE} has asked you to build a {BUILDING} in the homestead. Construct it in build mode to satisfy the request.");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("BUILDING", _displayName);
		TextObject textObject2 = new TextObject("{=homestead_build_quest_task}Build a {BUILDING}");
		textObject2.SetTextVariable("BUILDING", _displayName);
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
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, CompleteIfBuilt);
	}
}
