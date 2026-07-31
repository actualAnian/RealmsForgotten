using System;
using System.Collections.Generic;
using Helpers;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using SandBox;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.ObjectSystem;

namespace SOTOR.CampaignBehaviors;

public class SotorGraveyardBehavior : CampaignBehaviorBase
{
	public const string GraveyardScene = "sotor_graveyard_01_atmo_w_night";

	private const string RaisedTroopId = "sotor_skeleton";

	private const int MaxDefenderTroops = 5;

	private CharacterObject _skeleton;

	private CampaignTime _startWaitTime = CampaignTime.Zero;

	private Settlement _currentSettlement;

	private MobileParty _watchParty;

	private bool _isMissionStarted;

	private HashSet<CharacterObject> _selectedDefenders;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
		CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void Initialize(CampaignGameStarter starter)
	{
		starter.AddGameMenuOption("town", "sotor_graveyard", new TextObject("Go to the graveyard").ToString(), GraveyardAccessCondition, delegate
		{
			GameMenu.SwitchToMenu("sotor_graveyard");
		}, isLeave: false, 4);
		starter.AddGameMenuOption("village", "sotor_graveyard", new TextObject("Go to the graveyard").ToString(), GraveyardAccessCondition, delegate
		{
			GameMenu.SwitchToMenu("sotor_graveyard");
		}, isLeave: false, 4);
		starter.AddGameMenu("sotor_graveyard", "{SOTOR_GRAVEYARD_INTRODUCTION}", delegate(MenuCallbackArgs args)
		{
			args.MenuTitle = new TextObject("Graveyard");
			TextObject textObject = new TextObject("You have arrived at {SETTLEMENT_NAME}'s graveyard. Graves, tombstones and family crypts litter the peaceful hillside.");
			textObject.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement?.Name ?? new TextObject("the town"));
			MBTextManager.SetTextVariable("SOTOR_GRAVEYARD_INTRODUCTION", textObject);
		}, GameMenu.MenuOverlayType.SettlementWithCharacters);
		starter.AddGameMenuOption("sotor_graveyard", "sotor_raise_dead_attempt", new TextObject("Raise dead from the corpses in the ground (wait 8 hours).").ToString(), RaiseDeadAttemptCondition, delegate
		{
			GameMenu.SwitchToMenu("sotor_raising_dead");
		});
		starter.AddGameMenuOption("sotor_graveyard", "sotor_graveyard_leave", new TextObject("Leave").ToString(), delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate
		{
			GameMenu.SwitchToMenu(ParentSettlementMenu());
		}, isLeave: true);
		starter.AddWaitGameMenu("sotor_raising_dead", new TextObject("The common folk's graves are ripe for the taking. You spend the night hours dragging corpses from the ground and binding them to your will.").ToString(), delegate(MenuCallbackArgs args)
		{
			_startWaitTime = CampaignTime.Now;
			PlayerEncounter.Current.IsPlayerWaiting = true;
			args.MenuContext.GameMenu.StartWait();
		}, null, RaisingDeadConsequence, RaisingDeadTick, GameMenu.MenuAndOptionType.WaitMenuShowProgressAndHoursOption, GameMenu.MenuOverlayType.SettlementWithCharacters, 8f);
		starter.AddGameMenuOption("sotor_raising_dead", "sotor_raising_dead_leave", new TextObject("Leave").ToString(), delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}, delegate(MenuCallbackArgs args)
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
			SwitchToMenuIfThereIsAnInterrupt(args.MenuContext.GameMenu.StringId);
		}, isLeave: true);
		starter.AddGameMenu("sotor_graveyard_interrupt", "{SOTOR_GRAVEYARD_INTERRUPT}", delegate(MenuCallbackArgs args)
		{
			args.MenuTitle = new TextObject("Caught in the act");
			MBTextManager.SetTextVariable("SOTOR_GRAVEYARD_INTERRUPT", new TextObject("The local nightwatch is onto you. Face the consequences of your vile actions."));
			CalculateAndApplyCrimeRatingChange();
		}, GameMenu.MenuOverlayType.SettlementWithCharacters);
		starter.AddGameMenuOption("sotor_graveyard_interrupt", "sotor_interrupt_battle", new TextObject("Defend yourself").ToString(), delegate(MenuCallbackArgs args)
		{
			if (!Hero.MainHero.IsWounded)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
				return true;
			}
			return false;
		}, delegate(MenuCallbackArgs args)
		{
			OpenCompanionSelection(args);
		});
		starter.AddGameMenuOption("sotor_graveyard_interrupt", "sotor_interrupt_surrender", new TextObject("Surrender").ToString(), delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
			return true;
		}, delegate
		{
			PlayerEncounter.Current.IsPlayerWaiting = false;
			PlayerEncounter.Finish(forcePlayerOutFromSettlement: false);
			Settlement settlement = PrisonSettlementFor(Settlement.CurrentSettlement);
			if (settlement != null)
			{
				TakePrisonerAction.Apply(settlement.Party, Hero.MainHero);
			}
		}, isLeave: true);
		_skeleton = MBObjectManager.Instance.GetObject<CharacterObject>("sotor_skeleton");
	}

	private bool GraveyardAccessCondition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
		if (!SotorSettings.EnableSkeletonArmies)
		{
			return false;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null)
		{
			return false;
		}
		if (!currentSettlement.IsTown && !currentSettlement.IsVillage)
		{
			return false;
		}
		bool disableOption = false;
		TextObject disabledText = new TextObject("The graveyard's massive iron gates are closed shut.");
		bool flag = currentSettlement.IsVillage || Campaign.Current.Models.SettlementAccessModel.CanMainHeroAccessLocation(currentSettlement, "center", out disableOption, out disabledText);
		if (flag)
		{
			flag = GetBestRaiser() != null && !currentSettlement.IsUnderSiege;
		}
		return MenuHelper.SetOptionProperties(args, flag, disableOption, disabledText);
	}

	private bool RaiseDeadAttemptCondition(MenuCallbackArgs args)
	{
		if (GetBestRaiser() != null && !Settlement.CurrentSettlement.IsUnderSiege)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return true;
		}
		return false;
	}

	private void RaisingDeadConsequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Current.IsPlayerWaiting = false;
		args.MenuContext.GameMenu.EndWait();
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
		GameMenu.SwitchToMenu("sotor_graveyard");
	}

	private void RaisingDeadTick(MenuCallbackArgs args, CampaignTime dt)
	{
		if (Settlement.CurrentSettlement.IsUnderSiege)
		{
			InterruptWaitSiege(args);
			return;
		}
		float progress = args.MenuContext.GameMenu.Progress;
		int num = (int)_startWaitTime.ElapsedHoursUntilNow;
		if (num <= 0)
		{
			return;
		}
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu((float)num * 0.125f);
		if (args.MenuContext.GameMenu.Progress != progress)
		{
			if (_skeleton != null && MobileParty.MainParty.MemberRoster.TotalManCount <= MobileParty.MainParty.Party.PartySizeLimit)
			{
				Hero hero = GetBestRaiser() ?? Hero.MainHero;
				int num2 = Math.Max(1, (int)SotorSpellcraftHelper.GetCastingLevel(hero));
				int count = (Settlement.CurrentSettlement.IsVillage ? 1 : 2) * num2;
				MobileParty.MainParty.MemberRoster.AddToCounts(_skeleton, count);
				NotifyRaised(hero, count);
			}
			if (MBRandom.RandomFloatRanged(0f, 1f) > 0.95f && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
			{
				InterruptWait(args);
			}
		}
	}

	private void OpenCompanionSelection(MenuCallbackArgs args)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1);
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			CharacterObject character = item.Character;
			if (character != null && !character.IsPlayerCharacter && character.IsHero)
			{
				troopRoster.AddToCounts(character, 1);
			}
		}
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		troopRoster2.AddToCounts(CharacterObject.PlayerCharacter, 1);
		int num = 0;
		foreach (TroopRosterElement item2 in troopRoster.GetTroopRoster())
		{
			CharacterObject character2 = item2.Character;
			if (character2 != null && !character2.IsPlayerCharacter)
			{
				if (1 + num >= 5)
				{
					break;
				}
				troopRoster2.AddToCounts(character2, 1);
				num++;
			}
		}
		args.MenuContext.OpenTroopSelection(troopRoster, troopRoster2, (CharacterObject c) => !c.IsPlayerCharacter && c.IsHero, OnCompanionSelectionDone, 5);
	}

	private void OnCompanionSelectionDone(TroopRoster selected)
	{
		_selectedDefenders = new HashSet<CharacterObject>();
		_selectedDefenders.Add(CharacterObject.PlayerCharacter);
		foreach (TroopRosterElement item in selected.GetTroopRoster())
		{
			if (item.Character != null && item.Character.IsHero)
			{
				_selectedDefenders.Add(item.Character);
			}
		}
		SetupBattle();
	}

	private void SetupBattle()
	{
		PlayerEncounter.Current.IsPlayerWaiting = false;
		_watchParty = SotorGraveyardNightWatchPartyComponent.CreateParty(Settlement.CurrentSettlement);
		_currentSettlement = Settlement.CurrentSettlement;
		PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, _watchParty.Party, true);
		if (PlayerEncounter.Battle == null)
		{
			PlayerEncounter.StartBattle();
			PlayerEncounter.Update();
		}
		_isMissionStarted = true;
		OpenGraveyardBattleMission();
	}

	private Mission OpenGraveyardBattleMission()
	{
		MissionInitializerRecord rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord("sotor_graveyard_01_atmo_w_night", "", false, DecalAtlasGroup.All);
		rec.PlayingInCampaignMode = false;
		rec.AtmosphereOnCampaign = AtmosphereInfo.GetInvalidAtmosphereInfo();
		MapEvent mapEvent = MobileParty.MainParty.MapEvent;
		int count;
		FlattenedTroopRoster defenderPriors = BuildDefenderPriorTroops(out count);
		SotorGraveyardFightMissionController.DefenderSpawnCap = count;
		Hero leaderHero = mapEvent.AttackerSide.LeaderParty.LeaderHero;
		Hero leaderHero2 = mapEvent.DefenderSide.LeaderParty.LeaderHero;
		TextObject attackerLeaderName = leaderHero?.Name;
		TextObject defenderLeaderName = leaderHero2?.Name;
		return MissionState.OpenNew("Battle", rec, delegate
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Expected O, but got Unknown
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Expected O, but got Unknown
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Expected O, but got Unknown
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Expected O, but got Unknown
			IMissionTroopSupplier[] array = new IMissionTroopSupplier[2]
			{
				new PartyGroupTroopSupplier(mapEvent, BattleSideEnum.Defender, defenderPriors),
				new PartyGroupTroopSupplier(mapEvent, BattleSideEnum.Attacker)
			};
			return new List<MissionBehavior>
			{
				(MissionBehavior)new DefaultBattleMissionAgentSpawnLogic(array, BattleSideEnum.Defender, Mission.BattleSizeType.Battle), // [RF-A] 1.4.7: classe concreta renomeada
				new BattlePowerCalculationLogic(),
				new BattleSpawnLogic("battle_set"),
				new SotorGraveyardFightMissionController(),
				(MissionBehavior)new CampaignMissionComponent(),
				(MissionBehavior)new BattleAgentLogic(),
				(MissionBehavior)new MountAgentLogic(),
				new MissionOptionsComponent(),
				new BattleEndLogic(),
				new MissionCombatantsLogic(mapEvent.InvolvedParties, PartyBase.MainParty, mapEvent.GetLeaderParty(BattleSideEnum.Defender), mapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant: false),
				new BattleObserverMissionLogic(),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler(),
				new BattleMissionAgentInteractionLogic(),
				new AgentMoraleInteractionLogic(),
				new AssignPlayerRoleInTeamMissionController(isPlayerGeneral: true, isPlayerSergeant: false, isPlayerInArmy: false),
				new BannerBearerLogic(),
				(MissionBehavior)new SandboxGeneralsAndCaptainsAssignmentLogic(attackerLeaderName, defenderLeaderName, (TextObject)null, (TextObject)null, false),
				new EquipmentControllerLeaveLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new HighlightsController(),
				new BattleHighlightsController(),
				new BattleDeploymentMissionController(isPlayerAttacker: false),
				new BattleDeploymentHandler(isPlayerAttacker: false)
			}.ToArray();
		});
	}

	private FlattenedTroopRoster BuildDefenderPriorTroops(out int count)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		troopRoster.AddToCounts(CharacterObject.PlayerCharacter, 1);
		count = 1;
		if (_selectedDefenders != null)
		{
			foreach (CharacterObject selectedDefender in _selectedDefenders)
			{
				if (selectedDefender != null && !selectedDefender.IsPlayerCharacter && selectedDefender.IsHero)
				{
					troopRoster.AddToCounts(selectedDefender, 1);
					count++;
				}
			}
		}
		return troopRoster.ToFlattenedRoster();
	}

	private void OnBattleEnded(MapEvent mapevent)
	{
		if (_isMissionStarted && mapevent.WinningSide == mapevent.PlayerSide)
		{
			_isMissionStarted = false;
			_watchParty = null;
			_currentSettlement = null;
		}
	}

	private void HourlyPartyTick(MobileParty party)
	{
		if (party != _watchParty || !_isMissionStarted)
		{
			return;
		}
		_isMissionStarted = false;
		Settlement settlement = _currentSettlement ?? Town.AllTowns.GetRandomElementInefficiently().Settlement;
		if (Hero.MainHero.IsPrisoner && _watchParty.PrisonRoster.Contains(CharacterObject.PlayerCharacter))
		{
			Settlement settlement2 = PrisonSettlementFor(settlement);
			if (settlement2 != null)
			{
				TransferPrisonerAction.Apply(CharacterObject.PlayerCharacter, _watchParty.Party, settlement2.Party);
			}
			else
			{
				EndCaptivityAction.ApplyByEscape(Hero.MainHero);
			}
		}
		DestroyPartyAction.ApplyForDisbanding(_watchParty, settlement);
		_watchParty = null;
		_currentSettlement = null;
	}

	private void NotifyRaised(Hero raiser, int count)
	{
		if (count <= 0)
		{
			return;
		}
		TextObject textObject = new TextObject("You drag {COUNT} more from the earth to serve you.");
		textObject.SetTextVariable("COUNT", count);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null || _skeleton == null)
		{
			return;
		}
		using (SotorGraveyardRecruitTextPatch.Scope())
		{
			CampaignEventDispatcher.Instance.OnTroopRecruited(raiser, currentSettlement, null, _skeleton, count);
		}
	}

	private void SwitchToMenuIfThereIsAnInterrupt(string currentMenuId)
	{
		string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
		if (genericStateMenu != currentMenuId)
		{
			if (!string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.SwitchToMenu(genericStateMenu);
			}
			else
			{
				GameMenu.ExitToLast();
			}
		}
	}

	private static string ParentSettlementMenu()
	{
		if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsVillage)
		{
			return "town";
		}
		return "village";
	}

	private static Settlement PrisonSettlementFor(Settlement settlement)
	{
		if (settlement == null)
		{
			return null;
		}
		if (settlement.IsTown || settlement.IsCastle)
		{
			return settlement;
		}
		if (settlement.IsVillage)
		{
			Settlement settlement2 = settlement.Village?.Bound;
			if (settlement2 != null && (settlement2.IsTown || settlement2.IsCastle))
			{
				return settlement2;
			}
			return null;
		}
		return null;
	}

	private void InterruptWait(MenuCallbackArgs args)
	{
		PlayerEncounter.Current.IsPlayerWaiting = false;
		args.MenuContext.GameMenu.EndWait();
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
		GameMenu.SwitchToMenu("sotor_graveyard_interrupt");
	}

	private void InterruptWaitSiege(MenuCallbackArgs args)
	{
		PlayerEncounter.Current.IsPlayerWaiting = false;
		args.MenuContext.GameMenu.EndWait();
		args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(0f);
		GameMenu.SwitchToMenu(ParentSettlementMenu());
	}

	private void CalculateAndApplyCrimeRatingChange()
	{
		float num = 0f;
		float mainHeroCrimeRating = Settlement.CurrentSettlement.MapFaction.MainHeroCrimeRating;
		num = ((mainHeroCrimeRating < 30f) ? (30f - mainHeroCrimeRating + 5f) : ((!(mainHeroCrimeRating < 65f)) ? 20f : Math.Min(20f, 65f - mainHeroCrimeRating - 5f)));
		if (num > 0f)
		{
			ChangeCrimeRatingAction.Apply(Settlement.CurrentSettlement.MapFaction, num);
		}
	}

	private static Hero GetBestRaiser()
	{
		MobileParty mobileParty = Hero.MainHero?.PartyBelongedTo;
		if (mobileParty == null)
		{
			return null;
		}
		Hero result = null;
		int num = -1;
		foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
		{
			Hero hero = item.Character?.HeroObject;
			if (hero != null && CanRaiseDead(hero))
			{
				int num2 = SpellcraftOf(hero);
				if (num2 > num)
				{
					num = num2;
					result = hero;
				}
			}
		}
		return result;
	}

	private static bool CanRaiseDead(Hero hero)
	{
		HeroExtendedInfo heroExtendedInfo = hero?.GetExtendedInfo();
		if (heroExtendedInfo == null || !heroExtendedInfo.HasLore("LoreOfNecromancy"))
		{
			return false;
		}
		if (!heroExtendedInfo.HasSpell("SummonSkeleton"))
		{
			return heroExtendedInfo.HasSpell("GraveCall");
		}
		return true;
	}

	private static int SpellcraftOf(Hero hero)
	{
		SkillObject spellcraft = SotorSkills.Spellcraft;
		if (spellcraft == null)
		{
			return 0;
		}
		return hero.GetSkillValue(spellcraft);
	}
}
