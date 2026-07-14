using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadApprenticeQuest : QuestBase
{
	public const int ApprenticeDurationDays = 365;

	public const int ApprenticeRequiredSkillPoints = 200;

	private const int ApprenticeFailPenalty = 4;

	private const int ApprenticeRewardRelation = 6;

	private const int XpPerVictory = 2500;

	private const int StartGearTier = 2;

	private const int MaxGearTier = 6;

	private const int PointsPerGearTier = 50;

	[SaveableField(1)]
	private Hero _apprentice;

	[SaveableField(2)]
	private Homestead _homestead;

	[SaveableField(3)]
	private int _baselineSkillTotal;

	[SaveableField(4)]
	private int _legacyRequiredLevels;

	[SaveableField(5)]
	private Dictionary<string, int> _skillXp = new Dictionary<string, int>();

	[SaveableField(6)]
	private int _lastGearTier;

	[SaveableField(7)]
	private bool _readyNotified;

	[SaveableField(8)]
	private bool _farewellTriggered;

	private JournalLog? _progressLog;

	private static SkillObject[] AllSkills => new SkillObject[18]
	{
		DefaultSkills.OneHanded,
		DefaultSkills.TwoHanded,
		DefaultSkills.Polearm,
		DefaultSkills.Bow,
		DefaultSkills.Crossbow,
		DefaultSkills.Throwing,
		DefaultSkills.Riding,
		DefaultSkills.Athletics,
		DefaultSkills.Crafting,
		DefaultSkills.Scouting,
		DefaultSkills.Tactics,
		DefaultSkills.Roguery,
		DefaultSkills.Charm,
		DefaultSkills.Leadership,
		DefaultSkills.Trade,
		DefaultSkills.Steward,
		DefaultSkills.Medicine,
		DefaultSkills.Engineering
	};

	private static SkillObject[] TrainableSkills => new SkillObject[9]
	{
		DefaultSkills.OneHanded,
		DefaultSkills.TwoHanded,
		DefaultSkills.Polearm,
		DefaultSkills.Bow,
		DefaultSkills.Throwing,
		DefaultSkills.Riding,
		DefaultSkills.Athletics,
		DefaultSkills.Tactics,
		DefaultSkills.Leadership
	};

	public Hero Apprentice => _apprentice;

	public Homestead Homestead => _homestead;

	public int SkillPointsGained
	{
		get
		{
			if (_apprentice == null)
			{
				return 0;
			}
			return Math.Max(0, TotalSkillPoints(_apprentice) - _baselineSkillTotal);
		}
	}

	public bool IsTrainingComplete => SkillPointsGained >= 200;

	public bool IsApprenticePrisoner
	{
		get
		{
			if (base.IsOngoing && _apprentice != null && _apprentice.IsAlive)
			{
				return _apprentice.IsPrisoner;
			}
			return false;
		}
	}

	public TextObject TopSkillName => TopSkill().Name;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=homestead_appr_quest_title}Training {APPRENTICE} for {NOTABLE}");
			textObject.SetTextVariable("APPRENTICE", _apprentice?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => false;

	public override string SpecialQuestType => "HomesteadsQuest";

	public HomesteadApprenticeQuest(string questId, Hero notable, Homestead homestead, Hero apprentice)
		: base(questId, notable, CampaignTime.DaysFromNow(365f), 0)
	{
		_homestead = homestead;
		_apprentice = apprentice;
		_baselineSkillTotal = TotalSkillPoints(apprentice);
		_skillXp = new Dictionary<string, int>();
		_lastGearTier = 2;
		AddObjectiveLog();
	}

	public static Hero? FindGraduatingApprenticeInMainParty()
	{
		try
		{
			if (Campaign.Current?.QuestManager?.Quests == null)
			{
				return null;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is HomesteadApprenticeQuest homesteadApprenticeQuest && quest.IsOngoing && homesteadApprenticeQuest.IsTrainingComplete && homesteadApprenticeQuest.Apprentice?.PartyBelongedTo == MobileParty.MainParty)
				{
					return homesteadApprenticeQuest.Apprentice;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "FindGraduatingApprenticeInMainParty threw " + ex.GetType().Name + ": " + ex.Message);
		}
		return null;
	}

	private static int TotalSkillPoints(Hero h)
	{
		if (h == null)
		{
			return 0;
		}
		int num = 0;
		SkillObject[] allSkills = AllSkills;
		foreach (SkillObject skill in allSkills)
		{
			num += h.GetSkillValue(skill);
		}
		return num;
	}

	public static CharacterObject? GetCultureTroopAtTier(CultureObject? culture, int tier)
	{
		CharacterObject characterObject = culture?.BasicTroop;
		if (characterObject == null)
		{
			return null;
		}
		int num = Math.Max(0, tier - 1);
		for (int i = 0; i < num; i++)
		{
			CharacterObject[] upgradeTargets = characterObject.UpgradeTargets;
			if (upgradeTargets == null || upgradeTargets.Length == 0)
			{
				break;
			}
			characterObject = upgradeTargets[0];
		}
		return characterObject;
	}

	public static void EquipApprentice(Hero hero, int tier)
	{
		if (hero != null)
		{
			CharacterObject cultureTroopAtTier = GetCultureTroopAtTier(hero.Culture, tier);
			Equipment equipment = cultureTroopAtTier?.RandomBattleEquipment ?? cultureTroopAtTier?.FirstBattleEquipment;
			if (equipment != null)
			{
				EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, equipment);
			}
		}
	}

	private void OnPlayerBattleEnd(MapEvent mapEvent)
	{
		try
		{
			if (_apprentice != null && _apprentice.IsAlive && _apprentice.PartyBelongedTo == MobileParty.MainParty && mapEvent != null && mapEvent.WinningSide == mapEvent.PlayerSide)
			{
				GrantTrainingXp(2500);
				AfterProgress();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OnPlayerBattleEnd threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (_apprentice == null || !_apprentice.IsAlive)
			{
				return;
			}
			MobileParty mobileParty = _homestead?.MobileParty;
			if (mobileParty == null || _apprentice.PartyBelongedTo != mobileParty)
			{
				return;
			}
			HomesteadBehavior instance = HomesteadBehavior.Instance;
			if (instance == null || !instance.HasArmsMasterMasteryUnlocked)
			{
				return;
			}
			BattleSideEnum battleSideEnum = BattleSideEnum.NumSides;
			BattleSideEnum[] array = new BattleSideEnum[2]
			{
				BattleSideEnum.Attacker,
				BattleSideEnum.Defender
			};
			foreach (BattleSideEnum battleSideEnum2 in array)
			{
				foreach (MapEventParty item in mapEvent.PartiesOnSide(battleSideEnum2))
				{
					if (item?.Party?.MobileParty == mobileParty)
					{
						battleSideEnum = battleSideEnum2;
						break;
					}
				}
				if (battleSideEnum != BattleSideEnum.NumSides)
				{
					break;
				}
			}
			if (battleSideEnum != BattleSideEnum.NumSides && mapEvent.WinningSide == battleSideEnum)
			{
				GrantTrainingXp(2500);
				AfterProgress();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OnMapEventEnded threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void OnDailyTick()
	{
		try
		{
			if (_apprentice != null && _apprentice.IsAlive)
			{
				AfterProgress();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OnDailyTick threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void GrantTrainingXp(int totalXp)
	{
		if (_homestead?.ArmsMasterHero != null && _homestead.ArmsMasterHero.IsAlive)
		{
			totalXp = (int)Math.Round((float)totalXp * 1.25f);
		}
		EnsureApprenticeCanLearn();
		List<SkillObject> list = TrainableSkills.OrderBy((SkillObject _) => MBRandom.RandomFloat).Take(2).ToList();
		foreach (SkillObject item in list)
		{
			int num = totalXp / list.Count;
			if (_apprentice.HeroDeveloper != null)
			{
				_apprentice.HeroDeveloper.AddSkillXp(item, num, isAffectedByFocusFactor: false);
			}
			else
			{
				_apprentice.AddSkillXp(item, num);
			}
			_skillXp.TryGetValue(item.StringId, out var value);
			_skillXp[item.StringId] = value + num;
		}
	}

	private void EnsureApprenticeCanLearn()
	{
		HeroDeveloper hd = _apprentice?.HeroDeveloper;
		if (hd == null)
		{
			return;
		}
		try
		{
			EnsureAttr(DefaultCharacterAttributes.Vigor, 5);
			EnsureAttr(DefaultCharacterAttributes.Control, 5);
			EnsureAttr(DefaultCharacterAttributes.Endurance, 5);
			EnsureAttr(DefaultCharacterAttributes.Cunning, 4);
			EnsureAttr(DefaultCharacterAttributes.Social, 4);
			SkillObject[] trainableSkills = TrainableSkills;
			foreach (SkillObject skill in trainableSkills)
			{
				if (hd.GetFocus(skill) < 3)
				{
					hd.AddFocus(skill, 3 - hd.GetFocus(skill), checkUnspentFocusPoints: false);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "EnsureApprenticeCanLearn threw " + ex.GetType().Name + ": " + ex.Message);
		}
		void EnsureAttr(CharacterAttribute attr, int target)
		{
			int attributeValue = _apprentice.GetAttributeValue(attr);
			if (attributeValue < target)
			{
				hd.AddAttribute(attr, target - attributeValue, checkUnspentPoints: false);
			}
		}
	}

	private void AfterProgress()
	{
		int num = Math.Min(6, 2 + SkillPointsGained / 50);
		if (num > _lastGearTier)
		{
			_lastGearTier = num;
			EquipApprentice(_apprentice, num);
			TextObject textObject = new TextObject("{=homestead_appr_gear_up}{APPRENTICE} has earned better equipment.");
			textObject.SetTextVariable("APPRENTICE", _apprentice.Name);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.6f, 0.8f, 1f)));
		}
		RefreshLog();
		if (IsTrainingComplete && !_readyNotified)
		{
			_readyNotified = true;
			TextObject textObject2 = new TextObject("{=homestead_appr_ready_notice}{APPRENTICE} has grown into a capable fighter. They wish to speak with you before parting ways with the party.");
			textObject2.SetTextVariable("APPRENTICE", _apprentice.Name);
			InformationManager.DisplayMessage(new InformationMessage(textObject2.ToString(), new Color(0.5f, 0.8f, 1f)));
		}
		if (IsTrainingComplete && _readyNotified && !_farewellTriggered)
		{
			TryOpenFarewellOnMap();
		}
	}

	private SkillObject TopSkill()
	{
		if (_skillXp == null || _skillXp.Count == 0)
		{
			return DefaultSkills.OneHanded;
		}
		string topId = _skillXp.OrderByDescending<KeyValuePair<string, int>, int>((KeyValuePair<string, int> kv) => kv.Value).First().Key;
		return TrainableSkills.FirstOrDefault((SkillObject s) => s.StringId == topId) ?? DefaultSkills.OneHanded;
	}

	public void Graduate()
	{
		if (IsTrainingComplete)
		{
			if (_apprentice != null && _homestead?.MobileParty != null)
			{
				AddHeroToPartyAction.Apply(_apprentice, _homestead.MobileParty, showNotification: false);
				_homestead.AddResidentHero(_apprentice);
				_homestead.GrantApprenticeGraduationBonus(_apprentice);
			}
			else
			{
				RemoveApprenticeFromParty();
			}
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && base.QuestGiver != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, 6, showQuickNotification: false);
			}
			TextObject textObject = new TextObject("{=homestead_appr_complete}{APPRENTICE} has completed their training (grew most in {SKILL}) and will settle at the homestead. {NOTABLE} is grateful (+{REWARD} relation).");
			textObject.SetTextVariable("APPRENTICE", _apprentice?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("SKILL", TopSkill().Name);
			textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
			textObject.SetTextVariable("REWARD", 6);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.5f, 0.85f, 0.5f)));
			_progressLog?.UpdateCurrentProgress(200);
			CompleteQuestWithSuccess();
		}
	}

	public void RescueApprentice()
	{
		if (_apprentice != null && base.IsOngoing && _apprentice.IsPrisoner)
		{
			try
			{
				EndCaptivityAction.ApplyByReleasedByChoice(_apprentice, base.QuestGiver);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadApprenticeQuest", "RescueApprentice: EndCaptivityAction threw " + ex.GetType().Name + ": " + ex.Message);
			}
			if (_apprentice.IsAlive)
			{
				AddHeroToPartyAction.Apply(_apprentice, MobileParty.MainParty, showNotification: false);
				_apprentice.CharacterObject?.SetTransferableInPartyScreen(isTransferable: false);
				TextObject textObject = new TextObject("{=homestead_appr_rescued}{APPR_NAME} has been tracked down and released — they have returned to your party.");
				textObject.SetTextVariable("APPR_NAME", _apprentice.Name);
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.5f, 0.85f, 1f)));
				TraceLogger.Write("HomesteadApprenticeQuest", $"RescueApprentice: '{_apprentice.Name}' freed from captivity and returned to main party.");
			}
		}
	}

	public void CancelApprentice()
	{
		TraceLogger.Write("HomesteadApprenticeQuest", $"CancelApprentice called (giver='{base.QuestGiver?.Name}'). Stack: {Environment.StackTrace}");
		DisposeApprentice();
		CompleteQuestWithCancel();
	}

	protected override void OnTimedOut()
	{
		TraceLogger.Write("HomesteadApprenticeQuest", $"OnTimedOut fired — dueTime reached. (Should be ~{365} days after accept.)");
		DisposeApprentice();
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && base.QuestGiver != null)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, base.QuestGiver, -4, showQuickNotification: false);
		}
		TextObject textObject = new TextObject("{=homestead_appr_failed}{APPRENTICE} never got the training {NOTABLE} hoped for (-{PENALTY} relation).");
		textObject.SetTextVariable("APPRENTICE", _apprentice?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("PENALTY", 4);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.85f, 0.5f, 0.3f)));
	}

	private void RemoveApprenticeFromParty()
	{
		try
		{
			if (_apprentice?.CharacterObject != null)
			{
				MobileParty partyBelongedTo = _apprentice.PartyBelongedTo;
				if (partyBelongedTo?.MemberRoster != null)
				{
					partyBelongedTo.MemberRoster.RemoveTroop(_apprentice.CharacterObject);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "RemoveApprenticeFromParty threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void DisposeApprentice()
	{
		RemoveApprenticeFromParty();
		try
		{
			if (_apprentice != null && _apprentice.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(_apprentice, showNotification: false, isForced: false);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "DisposeApprentice threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void AddObjectiveLog()
	{
		TextObject textObject = new TextObject("{=homestead_appr_quest_log}{NOTABLE} has asked you to take their apprentice {APPRENTICE} into your party and season them in battle. Win battles with {APPRENTICE} along until they have gained {POINTS} skill points, then speak with {APPRENTICE} directly — they will have something to say before settling at the homestead.");
		textObject.SetTextVariable("NOTABLE", base.QuestGiver?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("APPRENTICE", _apprentice?.Name ?? new TextObject(string.Empty));
		textObject.SetTextVariable("POINTS", 200);
		TextObject textObject2 = new TextObject("{=homestead_appr_quest_task}Train {APPRENTICE}");
		textObject2.SetTextVariable("APPRENTICE", _apprentice?.Name ?? new TextObject(string.Empty));
		_progressLog = AddDiscreteLog(textObject, textObject2, 0, 200);
	}

	private void RefreshLog()
	{
		_progressLog?.UpdateCurrentProgress(Math.Min(SkillPointsGained, 200));
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (base.JournalEntries.Count > 0)
		{
			_progressLog = base.JournalEntries[0];
		}
		_apprentice?.CharacterObject?.SetTransferableInPartyScreen(isTransferable: false);
	}

	protected override void SetDialogs()
	{
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnHeroGainedSkill);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
	}

	private void OnHourlyTick()
	{
		try
		{
			if (IsTrainingComplete && _readyNotified && !_farewellTriggered)
			{
				TryOpenFarewellOnMap();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OnHourlyTick threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TryOpenFarewellOnMap()
	{
		if (IsTrainingComplete && _readyNotified && !_farewellTriggered && _apprentice != null && _apprentice.IsAlive && _apprentice.PartyBelongedTo == MobileParty.MainParty && Mission.Current == null && PlayerEncounter.Current == null)
		{
			_farewellTriggered = true;
			OpenMapFarewellConversation();
		}
	}

	private void OpenMapFarewellConversation()
	{
		try
		{
			if (Campaign.Current?.ConversationManager != null && _apprentice?.CharacterObject != null)
			{
				ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, MobileParty.MainParty?.Party);
				ConversationCharacterData conversationPartnerData = new ConversationCharacterData(_apprentice.CharacterObject, MobileParty.MainParty?.Party);
				Campaign.Current.ConversationManager.OpenMapConversation(playerCharacterData, conversationPartnerData);
				TraceLogger.Write("HomesteadApprenticeQuest", $"Opened world-map farewell conversation with '{_apprentice.Name}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OpenMapFarewellConversation threw " + ex.GetType().Name + ": " + ex.Message);
			_farewellTriggered = false;
		}
	}

	private void OnHeroGainedSkill(Hero hero, SkillObject skill, int change, bool shouldNotify)
	{
		try
		{
			if (hero == _apprentice)
			{
				AfterProgress();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadApprenticeQuest", "OnHeroGainedSkill threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
