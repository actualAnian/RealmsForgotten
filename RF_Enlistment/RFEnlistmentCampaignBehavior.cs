using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_Enlistment;

public sealed class RFEnlistmentCampaignBehavior : CampaignBehaviorBase
{
    private sealed class ArmyRhythmState
    {
        public string Phase = "Midday";
        public string Context = "field service";
        public bool QuietGarrison;
        public bool Marching;
        public bool ActiveCampaign;
        public bool SiegePressure;
        public bool NavalService;
        public bool RecoveryState;
        public bool LowSupplies;
        public bool HighScrutiny;
        public bool PreBattle;
        public int WoundedCount;
        public int RecruitCount;
    }

    private sealed class CommanderDecisionState
    {
        public string CommanderId = "-";
        public string PartyId = "-";
        public string Behavior = "-";
        public string TargetSettlementId = "-";
        public string TargetPartyId = "-";
        public string CurrentSettlementId = "-";
        public string BesiegedSettlementId = "-";
        public string ArmyLeaderId = "-";
        public string AttachedToId = "-";
        public string MapEventType = "-";
        public int ArmyPartyCount;
        public int WarCount;
        public int NearbySieges;
        public int StrengthBucket;
        public float NearestEnemyDistance = -1f;

        public string BuildSummary()
        {
            return $"commander={CommanderId} party={PartyId} behavior={Behavior} targetSettlement={TargetSettlementId} targetParty={TargetPartyId} currentSettlement={CurrentSettlementId} besieged={BesiegedSettlementId} armyLeader={ArmyLeaderId} armyCount={ArmyPartyCount} attachedTo={AttachedToId} mapEvent={MapEventType} wars={WarCount} nearbySieges={NearbySieges} nearestEnemy={NearestEnemyDistance:0.0} strength={StrengthBucket}";
        }
    }

    public static RFEnlistmentCampaignBehavior? Instance { get; private set; }

    private const string ServiceWaitMenuId = "rf_enlistment_service_wait";
    private const float DefaultContractDays = 365f;
    private const int RecruitPromotionXp = 100;
    private const int SoldierPromotionXp = 350;
    private const int VeteranPromotionXp = 800;
    private const int DutyMissionNone = 0;
    private const int DutyMissionReconSweep = 1;
    private const int DutyMissionRoadPatrol = 2;
    private const int DutyMissionMountedPursuit = 3;
    private const int DutyMissionSupplyDelivery = 4;
    private const int DutyMissionTrustedDispatch = 5;
    private const int DutyMissionServiceShift = 6;
    private const int DutyMissionBanditHunt = 7;
    private const int DutyMissionScoutRoute = 8;
    private const int DutyMissionForage = 9;
    private const int DutyMissionReliefDispatch = 10;
    private const int DutyMissionRecruitmentErrand = 11;
    private const int DutyMissionDeserterSweep = 12;
    private const int DutyMissionHideoutStrike = 13;
    private const int InteractiveDutyNone = 0;
    private const int InteractiveDutyNightPatrol = 1;
    private const int InteractiveDutyTreatWounded = 2;
    private const int InteractiveDutyTrainRecruits = 3;
    private const int InteractiveDutyEquipmentCheck = 4;
    private const int InteractiveDutyInspectDefenses = 5;
    private const int InteractiveDutyGateWatch = 6;
    private const int InteractiveDutyQuartermasterShortage = 7;
    private const int InteractiveDutyLeadPatrol = 8;
    private const int InteractiveDutyCoordinateSupply = 9;
    private const int InteractiveDutyCommandSquad = 10;
    private const int InteractiveDutyStrategicPlanning = 11;
    private const int IncidentNone = 0;
    private const int IncidentPayDelay = 1;
    private const int IncidentShortRations = 2;
    private const int IncidentCampDiscipline = 3;

    private RFEnlistmentServiceRecord _serviceRecord = new();
    private string _pendingEnlistmentCommanderId = string.Empty;
    private string _pendingEnlistmentCommanderName = string.Empty;
    private bool _pendingCommanderAttachment;
    private bool _createdCommanderArmyForService;
    private float _nextAttachmentRetryHour;
    private string _lastTraceSignature = string.Empty;
    private long _lastTraceWriteTicks;
    private RFEnlistmentBattleMeritReport _latestBattleMeritReport = new();
    private CommanderDecisionState? _lastCommanderDecisionState;

    public RFEnlistmentCampaignBehavior()
    {
        Instance = this;
    }

    public override void RegisterEvents()
    {
        Instance = this;
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
        CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
        CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
        CampaignEvents.OnPartyLeftArmyEvent.AddNonSerializedListener(this, OnPartyLeftArmy);
        CampaignEvents.OnMobilePartyJoinedToSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyJoinedToSiegeEvent);
        CampaignEvents.OnMobilePartyLeftSiegeEventEvent.AddNonSerializedListener(this, OnMobilePartyLeftSiegeEvent);
        CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
        CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("rf_enlistment_service_record", ref _serviceRecord);
        dataStore.SyncData("rf_enlistment_pending_commander_id", ref _pendingEnlistmentCommanderId);
        dataStore.SyncData("rf_enlistment_pending_commander_name", ref _pendingEnlistmentCommanderName);
        dataStore.SyncData("rf_enlistment_pending_commander_attachment", ref _pendingCommanderAttachment);
        dataStore.SyncData("rf_enlistment_created_commander_army_for_service", ref _createdCommanderArmyForService);
        dataStore.SyncData("rf_enlistment_next_attachment_retry_hour", ref _nextAttachmentRetryHour);
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        RepairLegacyCommanderServiceState();
        AddServiceWaitMenu(starter);

        RFEnlistmentSettings settings = RFEnlistmentSettings.Instance;
        starter.AddGameMenuOption(
            "town",
            "rf_enlistment_join_town",
            "{=rf_enlistment_join_option}Ask about military service",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && !_serviceRecord.IsEnlisted;
            },
            args => TryEnlistAtCurrentSettlement(),
            false,
            4);

        starter.AddGameMenuOption(
            "castle",
            "rf_enlistment_join_castle",
            "{=rf_enlistment_join_option}Ask about military service",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && !_serviceRecord.IsEnlisted;
            },
            args => TryEnlistAtCurrentSettlement(),
            false,
            4);

        starter.AddGameMenuOption(
            "town",
            "rf_enlistment_status_town",
            "{=rf_enlistment_status_option}Review your military status",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && (_serviceRecord.IsEnlisted || HasPendingEnlistmentPetition());
            },
            args => ShowMilitaryStatus(),
            false,
            4);

        starter.AddGameMenuOption(
            "castle",
            "rf_enlistment_status_castle",
            "{=rf_enlistment_status_option}Review your military status",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && (_serviceRecord.IsEnlisted || HasPendingEnlistmentPetition());
            },
            args => ShowMilitaryStatus(),
            false,
            4);

        starter.AddGameMenuOption(
            "town",
            "rf_enlistment_renew_town",
            "{=rf_enlistment_renew_option}Renew your contract",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && CanRenewContract();
            },
            args => RenewContract(),
            false,
            4);

        starter.AddGameMenuOption(
            "castle",
            "rf_enlistment_renew_castle",
            "{=rf_enlistment_renew_option}Renew your contract",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && CanRenewContract();
            },
            args => RenewContract(),
            false,
            4);

        starter.AddGameMenuOption(
            "town",
            "rf_enlistment_discharge_town",
            "{=rf_enlistment_discharge_option}Leave military service",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && _serviceRecord.IsEnlisted;
            },
            args => Discharge(),
            false,
            4);

        starter.AddGameMenuOption(
            "castle",
            "rf_enlistment_discharge_castle",
            "{=rf_enlistment_discharge_option}Leave military service",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && _serviceRecord.IsEnlisted;
            },
            args => Discharge(),
            false,
            4);

        starter.AddGameMenuOption(
            "town",
            "rf_enlistment_training_town",
            "{=rf_enlistment_training_option}Drill with the local troops",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && CanTrainToday();
            },
            args => TrainWithTroops(),
            false,
            4);

        starter.AddGameMenuOption(
            "castle",
            "rf_enlistment_training_castle",
            "{=rf_enlistment_training_option}Drill with the local troops",
            args =>
            {
                if (args == null)
                {
                    return false;
                }

                return settings.EnableRecruitmentStub && CanTrainToday();
            },
            args => TrainWithTroops(),
            false,
            4);

        AddLordDialogues(starter);
    }

    private void AddServiceWaitMenu(CampaignGameStarter starter)
    {
        starter.AddWaitGameMenu(
            ServiceWaitMenuId,
            "{=!}{RF_ENLISTMENT_WAIT_TEXT}",
            ServiceWaitMenuOnInit,
            ServiceWaitMenuOnCondition,
            null,
            ServiceWaitMenuOnTick,
            GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

        starter.AddGameMenuOption(
            ServiceWaitMenuId,
            "rf_enlistment_wait_status",
            "{=rf_enlistment_status_option}Review your military status",
            args => _serviceRecord.IsEnlisted,
            args => ShowMilitaryStatus(),
            false,
            0);

        starter.AddGameMenuOption(
            ServiceWaitMenuId,
            "rf_enlistment_wait_talk_to_commander",
            "{=rf_enlistment_talk_to_commander_option}Talk to commander",
            args => _serviceRecord.IsEnlisted && ResolveCommander()?.CharacterObject != null,
            args => TalkToCommanderFromServiceMenu(),
            false,
            1);

        starter.AddGameMenuOption(
            ServiceWaitMenuId,
            "rf_enlistment_wait_leave_service",
            "{=rf_enlistment_discharge_option}Leave military service",
            args => _serviceRecord.IsEnlisted,
            args => Discharge(),
            false,
            2);
    }

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (!_serviceRecord.IsEnlisted || party != MobileParty.MainParty)
        {
            if (_serviceRecord.IsEnlisted && party == MobileParty.MainParty)
            {
                TryCompleteSettlementDutyMission(settlement);
            }

            return;
        }

        TryCompleteSettlementDutyMission(settlement);
        RefreshCommanderContextState();
    }

    private void OnSettlementLeft(MobileParty party, Settlement settlement)
    {
        if (!_serviceRecord.IsEnlisted
            || party != MobileParty.MainParty
            || !_pendingCommanderAttachment
            || IsDetachedDutyMissionInProgress())
        {
            return;
        }

        TryAttachPlayerToCommanderDuty(showFeedback: true);
    }

    private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
    {
        if (!_serviceRecord.IsEnlisted
            || _serviceRecord.ActiveDutyMissionType == DutyMissionNone
            || destroyedParty == null
            || destroyedParty.StringId != _serviceRecord.ActiveDutyMissionTargetPartyId)
        {
            return;
        }

        bool playerInvolved = destroyerParty == PartyBase.MainParty
            || destroyerParty?.MobileParty == MobileParty.MainParty
            || (MobileParty.MainParty.Army != null && destroyerParty?.MobileParty?.Army == MobileParty.MainParty.Army);

        if (playerInvolved)
        {
            string resultText = _serviceRecord.ActiveDutyMissionType switch
            {
                DutyMissionMountedPursuit => "You rode down the hostile scouts before they could escape the district.",
                DutyMissionBanditHunt => "You broke the bandit threat before it could fall upon the column.",
                DutyMissionDeserterSweep => "You caught the deserters before they could scatter with arms and stories from the camp.",
                _ => "You found the hostile scouts and broke the ambush before it could form."
            };

            CompleteDutyMission(resultText);
            return;
        }

        FailDutyMission("The target was lost before you could finish the duty.");
    }

    private void OnPlayerBattleEnd(MapEvent mapEvent)
    {
        if (!_serviceRecord.IsEnlisted
            || mapEvent == null
            || !mapEvent.IsPlayerMapEvent
            || !IsBattleTypeCounted(mapEvent)
            || !IsCommanderPresentInBattle(mapEvent))
        {
            return;
        }

        bool playerWon = mapEvent.WinningSide == mapEvent.PlayerSide;
        bool isNavalBattle = mapEvent.IsNavalMapEvent;
        bool isBlockadeBattle = IsBlockadeBattle(mapEvent);
        int serviceXp = playerWon ? 40 : 15;
        float leadershipXp = playerWon ? 15f : 5f;
        float tacticsXp = playerWon ? 8f : 3f;

        _serviceRecord.FieldServiceCount++;
        if (playerWon)
        {
            _serviceRecord.BattleVictories++;
        }
        else
        {
            _serviceRecord.BattleDefeats++;
        }

        if (isNavalBattle)
        {
            _serviceRecord.NavalServiceCount++;
            serviceXp += playerWon ? 18 : 8;
            leadershipXp += playerWon ? 5f : 2f;
            tacticsXp += playerWon ? 6f : 3f;
            GrantAssignmentBattleXp(isNavalBattle: true, isSiegeLike: false, isVictory: playerWon);
        }
        else if (isBlockadeBattle)
        {
            serviceXp += playerWon ? 12 : 5;
            leadershipXp += playerWon ? 4f : 2f;
            tacticsXp += playerWon ? 5f : 2f;
            GrantAssignmentBattleXp(isNavalBattle: false, isSiegeLike: true, isVictory: playerWon);
        }
        else
        {
            GrantAssignmentBattleXp(isNavalBattle: false, isSiegeLike: mapEvent.IsSiegeAssault || mapEvent.IsSiegeAmbush, isVictory: playerWon);
        }

        _serviceRecord.ServiceXp += serviceXp;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, leadershipXp);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, tacticsXp);
        AdjustCommanderTrust(playerWon ? 2 : -1);
        if (playerWon)
        {
            AdjustCommanderReputation(field: isNavalBattle ? 0 : 2, command: 1, siege: isBlockadeBattle ? 1 : 0);
        }
        ApplyBattleMeritRewards(playerWon);
        TryPromote();

        string contextLabel = isNavalBattle
            ? "naval battle"
            : isBlockadeBattle
                ? "blockade battle"
                : "battle";

        ShowMessage(playerWon
            ? $"Your service in this {contextLabel} has been recorded."
            : $"Even in defeat, your service in this {contextLabel} has been noted.");
        RecordServiceIncident(
            playerWon ? "Battle Report" : "Battle Reverse",
            playerWon
                ? $"Your service in the {contextLabel} improved your standing with the command."
                : $"The {contextLabel} went badly, but your name remained on the rolls of those who stood.",
            popup: false);

        _pendingCommanderAttachment = true;
    }

    private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
    {
        if (!_serviceRecord.IsEnlisted || mapEvent == null)
        {
            return;
        }

        _latestBattleMeritReport = new RFEnlistmentBattleMeritReport();

        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (commanderParty == null || commanderParty.MapEvent != mapEvent)
        {
            return;
        }

        TrackCommanderDecisionState(forceLog: true, trigger: "map_event_started");
        TraceEnlistmentState("MapEventStarted", $"mapEvent={RFEnlistmentDebugExtensions.DescribeMapEvent(mapEvent)}");

        _pendingCommanderAttachment = true;

        if (PlayerEncounter.Current == null)
        {
            TryCreateCommanderBattleEncounter(mapEvent, commanderParty);
        }

        if (PlayerEncounter.Current != null && PlayerEncounter.EncounteredBattle == mapEvent)
        {
            TryJoinCommanderEncounterBattle(mapEvent, commanderParty);
        }
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (!_serviceRecord.IsEnlisted || mapEvent == null)
        {
            return;
        }

        if (TryResolveHideoutStrikeDuty(mapEvent))
        {
            return;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        if (commanderParty == null)
        {
            return;
        }

        bool commanderWasInBattle = IsCommanderPresentInBattle(mapEvent);
        bool playerWasInBattle = mapEvent.InvolvedParties.Any(x => x.MobileParty == MobileParty.MainParty);
        if (!commanderWasInBattle && !playerWasInBattle)
        {
            return;
        }

        TrackCommanderDecisionState(forceLog: true, trigger: "map_event_ended");
        TraceEnlistmentState("MapEventEnded", $"mapEvent={RFEnlistmentDebugExtensions.DescribeMapEvent(mapEvent)}");

        if (MobileParty.MainParty.MapEvent == null
            && commanderParty.MapEvent == null
            && (commanderWasInBattle || playerWasInBattle))
        {
            _pendingCommanderAttachment = true;
        }
    }

    private bool TryResolveHideoutStrikeDuty(MapEvent mapEvent)
    {
        if (_serviceRecord.ActiveDutyMissionType != DutyMissionHideoutStrike
            || !mapEvent.IsHideoutBattle
            || !mapEvent.IsPlayerMapEvent)
        {
            return false;
        }

        Settlement? targetSettlement = FindDutyMissionSettlement();
        if (targetSettlement == null || mapEvent.MapEventSettlement != targetSettlement)
        {
            return false;
        }

        if (mapEvent.WinningSide == mapEvent.PlayerSide)
        {
            CompleteDutyMission($"You stormed the raider hideout near {GetHideoutDutyReferenceName(targetSettlement)} and broke the raiders sheltering there.");
        }
        else
        {
            FailDutyMission($"You failed to clear the raider hideout near {GetHideoutDutyReferenceName(targetSettlement)}, and the raiders remain a threat.");
        }

        return true;
    }

    private void OnPartyJoinedArmy(MobileParty party)
    {
        if (!_serviceRecord.IsEnlisted || party != MobileParty.MainParty)
        {
            return;
        }

        TraceEnlistmentState("OnPartyJoinedArmy");

        _serviceRecord.InCommanderArmy = IsMainPartyInCommanderArmy();
        if (!_serviceRecord.InCommanderArmy)
        {
            TraceEnlistmentState("OnPartyJoinedArmyIgnored", "Joined an army, but not the commander army.");
            return;
        }

        _serviceRecord.ServiceXp += 20;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 5f);
        TryPromote();
        ShowMessage("You have joined your commander's army. Your service will now be judged on the march.");
    }

    private void OnPartyLeftArmy(MobileParty party, Army army)
    {
        if (!_serviceRecord.IsEnlisted || party != MobileParty.MainParty)
        {
            return;
        }

        TraceEnlistmentState("OnPartyLeftArmy", $"leftArmy={army.DescribeArmy()}");

        bool leftCommanderArmy = _serviceRecord.InCommanderArmy && army?.LeaderParty?.LeaderHero?.StringId == _serviceRecord.CommanderId;
        _serviceRecord.InCommanderArmy = false;

        if (leftCommanderArmy)
        {
            TraceEnlistmentState("OnPartyLeftArmyCommander", "Main party left the commander army.");
            _pendingCommanderAttachment = true;
            _nextAttachmentRetryHour = 0f;

            ShowMessage("You have left your commander's army.");
        }
    }

    private void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
    {
        if (!_serviceRecord.IsEnlisted || mobileParty != MobileParty.MainParty)
        {
            return;
        }

        _serviceRecord.InCommanderSiege = IsMainPartyInCommanderSiege();
        if (!_serviceRecord.InCommanderSiege)
        {
            return;
        }

        _serviceRecord.ServiceXp += 25;
        Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 8f);
        TryPromote();
        ShowMessage("You have entered a siege under your commander. Siege duty will now count toward your record.");
    }

    private void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
    {
        if (!_serviceRecord.IsEnlisted || mobileParty != MobileParty.MainParty)
        {
            return;
        }

        bool wasInCommanderSiege = _serviceRecord.InCommanderSiege;
        _serviceRecord.InCommanderSiege = false;

        if (wasInCommanderSiege)
        {
            _pendingCommanderAttachment = true;
            ShowMessage("You are no longer serving in your commander's active siege.");
        }
    }

    private void OnSiegeCompleted(Settlement settlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
    {
        if (!_serviceRecord.IsEnlisted || !_serviceRecord.InCommanderSiege)
        {
            return;
        }

        _serviceRecord.SiegeServiceCount++;
        bool isBlockadeResolution = battleType == MapEvent.BattleTypes.BlockadeBattle
            || battleType == MapEvent.BattleTypes.BlockadeSallyOutBattle;
        int serviceXp = isWin ? 60 : 25;
        int gold = isWin ? 75 : 30;

        if (isBlockadeResolution)
        {
            serviceXp += isWin ? 20 : 8;
            gold += isWin ? 25 : 10;
        }

        _serviceRecord.ServiceXp += serviceXp;
        if (isWin)
        {
            _serviceRecord.BattleVictories++;
        }
        else
        {
            _serviceRecord.BattleDefeats++;
        }
        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
        _serviceRecord.TotalWagesPaid += gold;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, isWin ? 18f : 8f);
        Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 12f);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, isBlockadeResolution ? 10f : 6f);
        GrantAssignmentBattleXp(isNavalBattle: false, isSiegeLike: true, isVictory: isWin);
        ApplySiegeAssignmentRewards(isWin, isBlockadeResolution);
        ApplyBattleMeritRewards(isWin);
        AdjustCommanderTrust(isWin ? 3 : -1);
        if (isWin)
        {
            AdjustCommanderReputation(command: 1, siege: 3);
        }

        _serviceRecord.InCommanderSiege = false;
        _serviceRecord.InCommanderBlockade = false;
        _pendingCommanderAttachment = true;
        TryPromote();

        string dutyLabel = isBlockadeResolution ? "blockade" : "siege";
        string roleSummary = GetSiegeRoleSummary(isBlockadeResolution);
        ShowMessage(isWin
            ? $"The {dutyLabel} has ended in victory. {roleSummary} Your commander records your service."
            : $"The {dutyLabel} has ended. {roleSummary} Even in hardship, your service has been recorded.");
        RecordServiceIncident(
            isWin ? "Siege Report" : "Siege Report",
            isWin
                ? $"The {dutyLabel} ended in victory and your siege service was marked well."
                : $"The {dutyLabel} ended without victory, but your service in the line was still noted.",
            popup: false);
    }

    private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
    {
        if (!_serviceRecord.IsEnlisted || winner?.HeroObject != Hero.MainHero || town == null)
        {
            return;
        }

        Hero? commander = ResolveCommander();
        int serviceXp = 45;
        int relationGain = 2;
        ApplyTournamentAssignmentRewards(ref serviceXp);
        _serviceRecord.ServiceXp += serviceXp;
        _serviceRecord.TournamentWins++;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);
        AdjustCommanderTrust(2);

        if (commander != null && town.OwnerClan == commander.Clan)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, relationGain, true, true);
        }

        TryPromote();
        ShowMessage($"Your tournament victory in {town.Name} has improved your standing in service as {GetAssignmentName(_serviceRecord.Assignment).ToString()}.");
    }

    private void OnDailyTick()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        RepairLegacyCommanderServiceState();
        RefreshCommanderContextState();
        if (!_serviceRecord.InCommanderArmy
            && !_serviceRecord.InCommanderSiege
            && !_serviceRecord.InCommanderNavalService
            && !_serviceRecord.InCommanderBlockade
            && ResolveCommander()?.PartyBelongedTo != null)
        {
            _pendingCommanderAttachment = true;
        }

        UpdateActiveDutyMission();
        _serviceRecord.DaysServed++;
        GrantDailyServiceProgress();
        TryTriggerServiceDutyEvent();
        TryTriggerServiceIncident();

        if (_serviceRecord.ContractEnd.IsPast && !_serviceRecord.ContractExpiredNoticeShown)
        {
            _serviceRecord.ContractExpiredNoticeShown = true;
            ShowMessage("Your enlistment contract has expired. You can renew it in any town or castle.");
        }

        // ResolveCommander uses Hero.FindFirst, which returns DEAD heroes too
        // (MEGA_010:241373) — so a fallen commander is not null and the service
        // would never close. Treat dead/removed the same as unavailable.
        Hero? currentCommander = ResolveCommander();
        if (currentCommander == null || currentCommander.IsDead)
        {
            ShowMessage("Your commanding lord is no longer available. Your service record has been closed.");
            ReleasePlayerFromCommanderDuty();
            _serviceRecord.Clear();
        }
    }

    private void OnHourlyTick()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        TrackCommanderDecisionState(forceLog: false, trigger: "hourly");
        EnsureDutyMissionTargetVisibility();
        TryAdvanceServiceShiftDuty();

        if (!_pendingCommanderAttachment || IsDetachedDutyMissionInProgress())
        {
            return;
        }

        RepairLegacyCommanderServiceState();
        TraceEnlistmentState("HourlyReattachAttempt");
        TryAttachPlayerToCommanderDuty(showFeedback: true);
    }

    private void OnTick(float dt)
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        EnsureDutyMissionTargetVisibility();

        if (IsDetachedDutyMissionInProgress())
        {
            return;
        }

        RepairLegacyCommanderServiceState();
        if (TryResolvePendingCommanderMenuEscape())
        {
            return;
        }

        if (TryResolvePendingCommanderEncounter())
        {
            return;
        }

        if (!_pendingCommanderAttachment && !NeedsCommanderAttachment())
        {
            return;
        }

        float now = (float)CampaignTime.Now.ToHours;
        if (now < _nextAttachmentRetryHour)
        {
            return;
        }

        bool attached = TryAttachPlayerToCommanderDuty(showFeedback: false);
        _nextAttachmentRetryHour = now + (attached ? 0.25f : 1f);
    }

    public bool TryResolvePendingCommanderMenuEscape()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return false;
        }

        if (IsDetachedDutyMissionInProgress())
        {
            return false;
        }

        string? menuId = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
        if (string.IsNullOrEmpty(menuId))
        {
            return false;
        }

        string? currentMenuId = menuId;
        if (currentMenuId == null || !IsCommanderServiceNativeMenu(currentMenuId))
        {
            return false;
        }

        TraceEnlistmentState("TryResolvePendingCommanderMenuEscape", $"menu={currentMenuId}");

        if (ShouldForceCommanderServiceMenu(currentMenuId))
        {
            ActivateCommanderServiceWaitMenu();
            TraceEnlistmentState("TryResolvePendingCommanderMenuEscapeOverride", $"menu={currentMenuId}");
            return true;
        }

        if (!_pendingCommanderAttachment && !NeedsCommanderAttachment())
        {
            return false;
        }

        if (TryResolvePendingCommanderEncounter())
        {
            return true;
        }

        if (currentMenuId == "army_wait_at_settlement")
        {
            return TryAttachPlayerToCommanderDuty(showFeedback: false);
        }

        return false;
    }

    public bool TryOverrideRequestedNativeMenu(ref string menuId)
    {
        if (string.IsNullOrEmpty(menuId) || !IsCommanderServiceNativeMenu(menuId))
        {
            return false;
        }

        if (!ShouldForceCommanderServiceMenu(menuId))
        {
            return false;
        }

        TraceEnlistmentState("MenuGuardOverride", $"nativeMenu={menuId}");
        menuId = ServiceWaitMenuId;
        return true;
    }

    private bool TryResolvePendingCommanderEncounter()
    {
        if (!_pendingCommanderAttachment || IsDetachedDutyMissionInProgress())
        {
            return false;
        }

        TraceEnlistmentState("TryResolvePendingCommanderEncounter");

        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (commanderParty == null)
        {
            TraceEnlistmentState("TryResolvePendingCommanderEncounterIgnored", "Commander party is missing.");
            return false;
        }

        if (commanderParty.MapEvent != null)
        {
            TryClearStaleEncounterForCommanderBattle(commanderParty);
        }

        if (PlayerEncounter.Current == null)
        {
            if (commanderParty.MapEvent == null)
            {
                return false;
            }

            TryCreateCommanderBattleEncounter(commanderParty.MapEvent, commanderParty);
            if (PlayerEncounter.Current == null || PlayerEncounter.EncounteredBattle != commanderParty.MapEvent)
            {
                return false;
            }
        }

        if (!IsCommanderRelatedParty(PlayerEncounter.EncounteredMobileParty, commanderParty))
        {
            TraceEnlistmentState("TryResolvePendingCommanderEncounterIgnored", "Encounter is not commander-related.");
            return false;
        }

        if (Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter)
        {
            TraceEnlistmentState("TryResolvePendingCommanderEncounterWaiting", "Still inside party conversation.");
            return false;
        }

        if (commanderParty.MapEvent != null && TryJoinCommanderEncounterBattle(commanderParty.MapEvent, commanderParty))
        {
            _pendingCommanderAttachment = false;
            TraceEnlistmentState("TryResolvePendingCommanderEncounterBattleJoin");
            return true;
        }

        PlayerEncounter.Finish();
        TraceEnlistmentState("TryResolvePendingCommanderEncounterFinish", "Finished encounter and retrying commander attach.");
        TryAttachPlayerToCommanderDuty(showFeedback: false);
        return true;
    }

    private bool TryClearStaleEncounterForCommanderBattle(MobileParty commanderParty)
    {
        if (commanderParty == null || commanderParty.MapEvent == null || PlayerEncounter.Current == null)
        {
            return false;
        }

        if (PlayerEncounter.EncounteredBattle == commanderParty.MapEvent)
        {
            return false;
        }

        if (IsCommanderRelatedParty(PlayerEncounter.EncounteredMobileParty, commanderParty))
        {
            return false;
        }

        if (Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter)
        {
            TraceEnlistmentState("TryClearStaleEncounterForCommanderBattleWaiting", "Stale encounter cannot be cleared during party conversation.");
            return false;
        }

        TraceEnlistmentState("TryClearStaleEncounterForCommanderBattle", "Finishing stale encounter before commander battle handoff.");
        PlayerEncounter.Finish();
        return true;
    }

    private void TryEnlistAtCurrentSettlement()
    {
        string? reason = GetEnlistmentBlockReason(GetSettlementCommander());
        if (reason != null)
        {
            ShowMessage(reason);
            return;
        }

        List<Hero> candidates = GetSettlementEnlistmentCandidates().ToList();
        if (candidates.Count == 0)
        {
            ShowMessage("There is no valid lord here to accept your service.");
            return;
        }

        if (candidates.Count == 1)
        {
            SubmitPetitionForCommander(candidates[0]);
            return;
        }

        ShowEnlistmentCommanderSelection(candidates);
    }

    private void SubmitPetitionForCommander(Hero commander)
    {
        _pendingEnlistmentCommanderId = commander.StringId;
        _pendingEnlistmentCommanderName = commander.Name?.ToString() ?? commander.StringId;

        TextObject message = new("{=rf_enlistment_petition_submitted}You have submitted a petition for service under {COMMANDER}. Find this lord and speak in person before taking the oath.");
        message.SetTextVariable("COMMANDER", commander.Name);
        ShowMessage(message.ToString());
    }

    private void ShowEnlistmentCommanderSelection(List<Hero> candidates)
    {
        List<InquiryElement> options = candidates
            .Select(hero => new InquiryElement(
                hero,
                BuildCommanderSelectionLabel(hero),
                null))
            .ToList();

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                "Choose a Commander",
                "Select which lord of this clan you want to petition for service.",
                options,
                true,
                1,
                1,
                GameTexts.FindText("str_done").ToString(),
                GameTexts.FindText("str_cancel").ToString(),
                selected =>
                {
                    Hero? commander = selected.FirstOrDefault()?.Identifier as Hero;
                    if (commander != null)
                    {
                        SubmitPetitionForCommander(commander);
                    }
                },
                null),
            pauseGameActiveState: true);
    }

    private static string BuildCommanderSelectionLabel(Hero hero)
    {
        MobileParty? party = hero.PartyBelongedTo;
        string status;
        if (party?.CurrentSettlement != null)
        {
            status = $"in {party.CurrentSettlement.Name}";
        }
        else if (party?.Army?.LeaderParty == party)
        {
            status = "army leader";
        }
        else if (party != null && party.IsActive)
        {
            status = "field commander";
        }
        else
        {
            status = "lord";
        }

        return $"{hero.Name} ({status})";
    }

    private void ShowMilitaryStatus()
    {
        if (_serviceRecord.IsEnlisted)
        {
            ShowServiceStatus();
            return;
        }

        if (HasPendingEnlistmentPetition())
        {
            ShowPetitionStatus();
        }
    }

    private void ServiceWaitMenuOnInit(MenuCallbackArgs args)
    {
        if (args?.MenuContext?.GameMenu == null)
        {
            return;
        }

        args.MenuContext.GameMenu.StartWait();
        UpdateServiceWaitMenuBackground(args);
        UpdateServiceWaitMenuText(args);
    }

    private bool ServiceWaitMenuOnCondition(MenuCallbackArgs args)
    {
        return _serviceRecord.IsEnlisted;
    }

    private void TalkToCommanderFromServiceMenu()
    {
        Hero? commander = ResolveCommander();
        if (commander?.CharacterObject == null)
        {
            ShowMessage("Your commander is not available to speak right now.");
            return;
        }

        ConversationCharacterData playerData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
        ConversationCharacterData commanderData = commander.PartyBelongedTo != null
            ? new ConversationCharacterData(commander.CharacterObject, commander.PartyBelongedTo.Party)
            : new ConversationCharacterData(commander.CharacterObject);

        CampaignMapConversation.OpenConversation(playerData, commanderData);
    }

    private void ServiceWaitMenuOnTick(MenuCallbackArgs args, CampaignTime dt)
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        if (commander == null || commander.IsDead || commanderParty == null)
        {
            // The commander (or their party) no longer exists. Simply returning
            // here left the player frozen in the wait menu with an orphaned
            // camera — close the service, restore the player's own party and
            // leave the menu.
            ShowMessage("Your commanding lord is no longer available. Your service record has been closed.");
            ReleasePlayerFromCommanderDuty();
            _serviceRecord.Clear();
            if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == ServiceWaitMenuId)
            {
                GameMenu.ExitToLast();
            }
            return;
        }

        if (!commanderParty.IsActive)
        {
            // Transient (e.g. commander temporarily inside a settlement) — wait.
            return;
        }

        if (commanderParty.MapEvent != null)
        {
            TryClearStaleEncounterForCommanderBattle(commanderParty);

            if (PlayerEncounter.Current == null)
            {
                TryCreateCommanderBattleEncounter(commanderParty.MapEvent, commanderParty);
            }
            else if (PlayerEncounter.EncounteredBattle == commanderParty.MapEvent)
            {
                TryJoinCommanderEncounterBattle(commanderParty.MapEvent, commanderParty);
            }

            return;
        }

        if (commanderParty.CurrentSettlement != null)
        {
            if (MobileParty.MainParty.CurrentSettlement != commanderParty.CurrentSettlement)
            {
                TryAttachPlayerToCommanderDuty(showFeedback: false);
            }

            RefreshCommanderContextState();
            UpdateServiceWaitMenuBackground(args);
            UpdateServiceWaitMenuText(args);
            return;
        }

        SyncPlayerPartyToCommanderServiceMode(commanderParty);
        UpdateServiceWaitMenuBackground(args);
        UpdateServiceWaitMenuText(args);
    }

    private void UpdateServiceWaitMenuBackground(MenuCallbackArgs args)
    {
        if (args?.MenuContext == null)
        {
            return;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        string? backgroundMeshName;
        bool isSeaServiceState = commanderParty?.IsCurrentlyAtSea == true
            || MobileParty.MainParty.IsCurrentlyAtSea
            || MobileParty.MainParty.AttachedTo?.IsCurrentlyAtSea == true;

        if (isSeaServiceState)
        {
            backgroundMeshName = "encounter_naval";
        }
        else
        {
            backgroundMeshName = commanderParty?.MapFaction?.Culture?.EncounterBackgroundMesh;

            if (string.IsNullOrWhiteSpace(backgroundMeshName))
            {
                backgroundMeshName = Hero.MainHero.MapFaction?.Culture?.EncounterBackgroundMesh;
            }

            if (string.IsNullOrWhiteSpace(backgroundMeshName))
            {
                backgroundMeshName = "wait_fallback";
            }
        }

        if (!string.IsNullOrWhiteSpace(backgroundMeshName))
        {
            args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
        }
    }

    private void UpdateServiceWaitMenuText(MenuCallbackArgs args)
    {
        if (args?.MenuContext?.GameMenu == null)
        {
            return;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        TextObject text = args.MenuContext.GameMenu.GetText();
        string commanderName = (commander?.Name ?? new TextObject(_serviceRecord.CommanderName)).ToString();
        string assignment = GetAssignmentName(_serviceRecord.Assignment).ToString();
        string duty = GetCommanderTravelStatusText(commanderParty);
        string trustLabel = GetCommanderTrustLabel();
        string activeDuty = GetActiveDutyMissionStatusText();

        text.SetTextVariable("RF_ENLISTMENT_WAIT_TEXT",
            $"You are traveling in {commanderName}'s service.\n\n" +
            $"Rank: {GetRankName(_serviceRecord.Rank)}\n" +
            $"Current duty: {assignment}\n" +
            $"Status: {duty}\n" +
            $"Active assignment: {activeDuty}\n" +
            $"Days served: {_serviceRecord.DaysServed}\n" +
            $"Service XP: {_serviceRecord.ServiceXp}\n" +
            $"Trust: {trustLabel} ({_serviceRecord.CommanderTrust})");
    }

    private string GetCommanderTravelStatusText(MobileParty? commanderParty)
    {
        if (commanderParty == null)
        {
            return "commander unavailable";
        }

        if (commanderParty.MapEvent != null)
        {
            return "commander engaged in battle";
        }

        if (commanderParty.CurrentSettlement != null)
        {
            return $"inside {commanderParty.CurrentSettlement.Name}";
        }

        TextObject? behaviorText = GetMobilePartyBehaviorText(commanderParty);
        string fallback = commanderParty.IsCurrentlyAtSea ? "sailing with the fleet" : "on campaign";
        return behaviorText?.ToString() ?? fallback;
    }

    private static TextObject? GetMobilePartyBehaviorText(MobileParty party)
    {
        return party.DefaultBehavior switch
        {
            AiBehavior.GoToSettlement => new TextObject("{=rf_enlistment_status_marching_settlement}marching toward a settlement"),
            AiBehavior.EngageParty => new TextObject("{=rf_enlistment_status_engaging_enemy}moving to engage the enemy"),
            AiBehavior.EscortParty => new TextObject("{=rf_enlistment_status_escorting}keeping close formation with an allied party"),
            AiBehavior.RaidSettlement => new TextObject("{=rf_enlistment_status_raiding}raiding hostile lands"),
            AiBehavior.BesiegeSettlement => new TextObject("{=rf_enlistment_status_besieging}conducting siege operations"),
            AiBehavior.PatrolAroundPoint => new TextObject("{=rf_enlistment_status_patrolling}patrolling the surrounding roads"),
            AiBehavior.DefendSettlement => new TextObject("{=rf_enlistment_status_defending}defending nearby holdings"),
            AiBehavior.Hold => new TextObject("{=rf_enlistment_status_holding}holding position"),
            _ => null
        };
    }

    private void ShowServiceStatus()
    {
        MarkCommanderAttachmentPendingFromConversation();

        Hero? commander = ResolveCommander();
        string commanderName = (commander?.Name ?? new TextObject(_serviceRecord.CommanderName)).ToString();
        string rankName = GetRankName(_serviceRecord.Rank).ToString();
        string assignmentName = GetAssignmentName(_serviceRecord.Assignment).ToString();
        string expectedDuty = GetAssignmentName(GetDutyAssignmentForCurrentOffer()).ToString();
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        string servicePanel =
            $"Commander: {commanderName}\n" +
            $"Commander style: {GetCommanderServiceStyle()}\n" +
            $"Rank: {rankName}\n" +
            $"Role: {assignmentName}\n" +
            $"Days served: {_serviceRecord.DaysServed}\n" +
            $"Contract remaining: {GetDaysRemaining()} days\n" +
            $"Equipment debt: {_serviceRecord.OutstandingEquipmentDebt}\n" +
            $"Promotion track: {GetRankProgressText()}\n" +
            $"Officer track: {GetOfficerTrackStatusText()}\n\n" +
            $"Current duty mission: {GetActiveDutyMissionStatusText()}\n" +
            $"Expected duty posture: {expectedDuty}\n" +
            $"Army rhythm: {GetArmyRhythmSummary(rhythmState)}\n" +
            $"Army pressures: {GetArmyPressureSummary(rhythmState)}\n" +
            $"Commander trust: {GetCommanderTrustLabel()}\n" +
            $"Commander memory: {GetCommanderMemorySummary()}\n" +
            $"Service record: {GetServiceRecordSummary()}\n" +
            $"Duty history: {GetDutyRecordSummary()}\n" +
            $"Battle merits earned: {_serviceRecord.BattleMeritCount}\n" +
            $"Last battle merit: {GetLastBattleMeritSummary()}\n" +
            $"Last merit note: {GetLastBattleMeritDetail()}\n" +
            $"Battle record: {GetBattleRecordSummary()}\n" +
            $"Service reputations: {GetReputationBreakdown()}\n" +
            $"Deferred pay: {_serviceRecord.DeferredWageAmount}\n" +
            $"Promotions earned: {_serviceRecord.PromotionCount}\n" +
            $"Highest trust reached: {_serviceRecord.HighestTrustReached}\n" +
            $"Companion support: {GetCompanionSupportSummary()}\n" +
            $"Commission status: {GetCommissionStatusText()}\n\n" +
            $"Army context: {(_serviceRecord.InCommanderArmy ? "with commander's army" : "independent")}\n" +
            $"Siege context: {GetCommanderDutyStatusLabel()}\n" +
            $"Latest report: {GetLatestIncidentSummary()}\n" +
            $"Career consequences: {GetCareerConsequencePreview()}";

        InformationManager.ShowInquiry(new InquiryData(
            "Service Record",
            servicePanel,
            true,
            false,
            "Close",
            string.Empty,
            null,
            null), true);
    }

    private void ShowPetitionStatus()
    {
        string commanderName = string.IsNullOrWhiteSpace(_pendingEnlistmentCommanderName)
            ? "Unknown commander"
            : _pendingEnlistmentCommanderName;

        string petitionPanel =
            $"Petition status: pending\n" +
            $"Commander: {commanderName}\n\n" +
            $"Your request to enter military service has been recorded, but you have not yet taken the oath.\n\n" +
            $"Next step: find this lord and speak in person to confirm whether you truly wish to enlist.";

        InformationManager.ShowInquiry(new InquiryData(
            "Enlistment Petition",
            petitionPanel,
            true,
            false,
            "Close",
            string.Empty,
            null,
            null), true);
    }

    private bool CanRenewContract()
    {
        return _serviceRecord.IsEnlisted && _serviceRecord.ContractEnd.IsPast;
    }

    private void RenewContract()
    {
        _serviceRecord.ContractEnd = CampaignTime.DaysFromNow(DefaultContractDays);
        _serviceRecord.ContractExpiredNoticeShown = false;
        MarkCommanderAttachmentPendingFromConversation();
        string quartermasterSupport = ApplyQuartermasterContractSupport();
        ShowMessage(string.IsNullOrEmpty(quartermasterSupport)
            ? $"Your contract has been renewed for {(int)DefaultContractDays} more days."
            : $"Your contract has been renewed for {(int)DefaultContractDays} more days. {quartermasterSupport}");
    }

    private void Discharge()
    {
        if (_serviceRecord.OutstandingEquipmentDebt > 0)
        {
            ShowMessage($"You still owe {_serviceRecord.OutstandingEquipmentDebt} gold for issued equipment.");
            return;
        }

        if (HasOutstandingIssuedEquipment())
        {
            if (_serviceRecord.Rank >= RFEnlistmentRank.Veteran)
            {
                ClearIssuedEquipmentRecord();
                ShowMessage("Your commander allows you to keep your issued gear as an earned reward.");
            }
            else
            {
                ShowMessage("Return your issued gear or pay for it before leaving service.");
                return;
            }
        }

        string commanderName = _serviceRecord.CommanderName;
        string consequenceText = ApplyServiceCareerConsequences(isCommission: false);
        Settlement? releaseSettlement = ResolveCommander()?.PartyBelongedTo?.CurrentSettlement;
        ClearDutyMission(destroyTargetParty: true);
        ReleasePlayerFromCommanderDuty();
        RestorePostServiceCampaignContext(releaseSettlement);
        _serviceRecord.Clear();
        ShowMessage(string.IsNullOrEmpty(consequenceText)
            ? $"You have left military service under {commanderName}."
            : $"You have left military service under {commanderName}. {consequenceText}");
    }

    public void RegisterBattleKill()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        int xp = Math.Max(1, RFEnlistmentSettings.Instance.XPGainedPerKill);
        int gold = Math.Max(0, RFEnlistmentSettings.Instance.GoldLootedPerKill);

        _serviceRecord.ServiceXp += xp;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, xp * 0.25f);

        if (gold > 0)
        {
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
            _serviceRecord.TotalWagesPaid += gold;
        }

        TryPromote();
    }

    public void UpdateBattleMeritReport(RFEnlistmentBattleMeritReport report)
    {
        _latestBattleMeritReport = report ?? new RFEnlistmentBattleMeritReport();
    }

    private void ApplyBattleMeritRewards(bool playerWon)
    {
        RFEnlistmentBattleMeritReport report = _latestBattleMeritReport;
        _latestBattleMeritReport = new RFEnlistmentBattleMeritReport();

        if (!report.Valid)
        {
            return;
        }

        int score = CalculateBattleMeritScore(report);
        int meritXp;
        int gold;
        int trustGain;
        string grade;

        if (score >= 80)
        {
            meritXp = 30;
            gold = 20;
            trustGain = 2;
            grade = "distinguished";
        }
        else if (score >= 60)
        {
            meritXp = 20;
            gold = 10;
            trustGain = 1;
            grade = "strong";
        }
        else if (score >= 40)
        {
            meritXp = 12;
            gold = 5;
            trustGain = playerWon ? 1 : 0;
            grade = "solid";
        }
        else
        {
            meritXp = 4;
            gold = 0;
            trustGain = 0;
            grade = "rough";
        }

        _serviceRecord.ServiceXp += meritXp;
        if (gold > 0)
        {
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
            _serviceRecord.TotalWagesPaid += gold;
        }

        if (trustGain != 0)
        {
            AdjustCommanderTrust(trustGain);
        }

        if (score >= 80)
        {
            AdjustCommanderReputation(field: 2, command: 1);
        }
        else if (score >= 60)
        {
            AdjustCommanderReputation(field: 1, command: 1);
        }
        else if (score >= 40)
        {
            AdjustCommanderReputation(field: 1);
        }

        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, meritXp * 0.3f);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, meritXp * 0.25f);

        string cohesion = DescribeMeritBand(report.CohesionRatio);
        string engagement = DescribeMeritBand(report.EngagementRatio);
        string discipline = DescribeMeritBand((report.CohesionRatio + report.CommanderRatio) * 0.5f);
        string roleRead = DescribeRoleMerit(report);
        string meritSummary = $"Merit {grade} ({score}). Kills {report.Kills}, cohesion {cohesion}, engagement {engagement}, role fit {roleRead}.";

        _serviceRecord.BattleMeritCount++;
        _serviceRecord.LastBattleMeritScore = score;
        _serviceRecord.LastBattleMeritGrade = grade;
        _serviceRecord.LastBattleMeritText = meritSummary;
        _serviceRecord.LastBattleMeritDay = CampaignTime.Now.GetDayOfYear;

        ShowMessage($"Battle merit: {grade}. Discipline {discipline}, line contact {engagement}, role fit {roleRead}. Reward: {meritXp} service XP{(gold > 0 ? $" and {gold} gold" : string.Empty)}.");
        RecordServiceIncident(
            "Battle Merit",
            meritSummary,
            popup: false);
    }

    private string GetLastBattleMeritSummary()
    {
        if (_serviceRecord.LastBattleMeritScore < 0 || string.IsNullOrWhiteSpace(_serviceRecord.LastBattleMeritGrade))
        {
            return "none recorded yet";
        }

        return $"{_serviceRecord.LastBattleMeritGrade} ({_serviceRecord.LastBattleMeritScore})";
    }

    private string GetLastBattleMeritDetail()
    {
        return string.IsNullOrWhiteSpace(_serviceRecord.LastBattleMeritText)
            ? "none recorded yet"
            : _serviceRecord.LastBattleMeritText;
    }

    private string GetBattleRecordSummary()
    {
        int totalBattles = _serviceRecord.BattleVictories + _serviceRecord.BattleDefeats;
        if (totalBattles <= 0)
        {
            return "no battles recorded yet";
        }

        return $"{_serviceRecord.BattleVictories} victories, {_serviceRecord.BattleDefeats} defeats";
    }

    private string GetDutyRecordSummary()
    {
        return $"{_serviceRecord.InteractiveDutyCount} interactive, {_serviceRecord.DetachedDutyCount} detached, {_serviceRecord.ServiceShiftCount} service shifts";
    }

    private string GetReputationBreakdown()
    {
        return $"field {_serviceRecord.FieldReputation}, logistics {_serviceRecord.LogisticsReputation}, command {_serviceRecord.CommandReputation}, siege {_serviceRecord.SiegeReputation}";
    }

    private string GetRankProgressText()
    {
        return _serviceRecord.Rank switch
        {
            RFEnlistmentRank.Recruit => $"toward Soldier: {_serviceRecord.ServiceXp}/{RecruitPromotionXp} XP, {_serviceRecord.DaysServed}/7 days",
            RFEnlistmentRank.Soldier => $"toward Veteran: {_serviceRecord.ServiceXp}/{SoldierPromotionXp} XP, successes {_serviceRecord.DutySuccesses}/2, battles or merits {(HasVeteranPromotionServiceProof() ? "met" : "needed")}",
            RFEnlistmentRank.Veteran => $"toward Sergeant: {_serviceRecord.ServiceXp}/{VeteranPromotionXp} XP, trust {_serviceRecord.CommanderTrust}/6, successes {_serviceRecord.DutySuccesses}/5, campaigns {GetPromotionServiceActionCount()}/3",
            _ => "top enlisted rank reached"
        };
    }

    private bool HasVeteranPromotionServiceProof()
    {
        return _serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount >= 1
            || _serviceRecord.BattleMeritCount >= 1;
    }

    private int GetPromotionServiceActionCount()
    {
        return _serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount;
    }

    private string GetOfficerTrackStatusText()
    {
        if (_serviceRecord.Rank < RFEnlistmentRank.Veteran)
        {
            return "ordinary line soldier duties";
        }

        if (_serviceRecord.Rank < RFEnlistmentRank.Sergeant)
        {
            return _serviceRecord.CommanderTrust >= 5
                ? "trusted veteran, beginning to receive officer-style duties"
                : "veteran rank earned, but more trust is needed for officer-style work";
        }

        return _serviceRecord.CommanderTrust >= 8
            ? "sergeant standing, clearly in the officer-track layer"
            : "sergeant rank held, but command trust is still growing";
    }

    private int CalculateBattleMeritScore(RFEnlistmentBattleMeritReport report)
    {
        float score = 0f;
        score += MathF.Min(report.Kills, 6) * 5f;
        score += report.SurvivalRatio * 25f;
        score += report.CohesionRatio * 15f;
        score += report.CommanderRatio * 10f;
        score += report.EngagementRatio * 10f;
        score += GetRoleMeritBonus(report);

        if (report.FellInBattle && report.SurvivalRatio < 0.35f)
        {
            score -= 10f;
        }

        return MBMath.ClampInt((int)MathF.Round(score), 0, 100);
    }

    private float GetRoleMeritBonus(RFEnlistmentBattleMeritReport report)
    {
        float averageDistance = report.AverageEnemyDistance;
        if (averageDistance < 0f)
        {
            return 0f;
        }

        return _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => averageDistance >= 18f && averageDistance <= 50f
                ? 10f
                : averageDistance >= 12f && averageDistance <= 60f ? 6f : 0f,
            RFEnlistmentAssignment.Cavalry => averageDistance >= 10f && averageDistance <= 28f
                ? 8f + report.CohesionRatio * 2f
                : report.CohesionRatio * 3f,
            RFEnlistmentAssignment.Support => report.CommanderRatio * 10f,
            _ => report.CohesionRatio * 5f + report.EngagementRatio * 5f
        };
    }

    private string DescribeRoleMerit(RFEnlistmentBattleMeritReport report)
    {
        float bonus = GetRoleMeritBonus(report);
        if (bonus >= 8f)
        {
            return "high";
        }

        if (bonus >= 4f)
        {
            return "fair";
        }

        return "low";
    }

    private static string DescribeMeritBand(float ratio)
    {
        if (ratio >= 0.75f)
        {
            return "high";
        }

        if (ratio >= 0.45f)
        {
            return "fair";
        }

        return "low";
    }

    private void GrantDailyServiceProgress()
    {
        if (_serviceRecord.ContractEnd.IsPast)
        {
            return;
        }

        int dailyWage = GetDailyWage();
        bool deferWage = ShouldDeferDailyWage();
        if (dailyWage > 0 && !deferWage)
        {
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, dailyWage, false);
            _serviceRecord.TotalWagesPaid += dailyWage;
        }
        else if (dailyWage > 0)
        {
            _serviceRecord.DeferredWageAmount += dailyWage;
            RecordServiceIncident(
                "Pay Delayed",
                $"The pay chest did not reach the line cleanly today. {dailyWage} gold has been marked as owed to you.",
                popup: false);
        }

        TryResolveDeferredWages();

        int serviceXpGain = GetDailyServiceXp();
        _serviceRecord.ServiceXp += serviceXpGain;

        GrantAssignmentXp();
        GrantContextDutyXp();
        GrantReputationDailyXp();
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, RFEnlistmentSettings.Instance.DailyLeadershipXp);

        TryPromote();
    }

    private void GrantContextDutyXp()
    {
        if (_serviceRecord.InCommanderArmy)
        {
            _serviceRecord.ServiceXp += 6;
            Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 2f);
            Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 2f);
        }

        if (_serviceRecord.InCommanderSiege)
        {
            int siegeXp = 8;
            Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 4f);
            ApplyEngineerContextSupport(ref siegeXp);
            _serviceRecord.ServiceXp += siegeXp;

            if (_serviceRecord.Assignment == RFEnlistmentAssignment.Support)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 3f);
            }
        }

        if (_serviceRecord.InCommanderNavalService)
        {
            int navalXp = 7;
            Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 3f);
            Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 2f);
            ApplyScoutContextSupport(ref navalXp);
            _serviceRecord.ServiceXp += navalXp;

            if (_serviceRecord.Assignment == RFEnlistmentAssignment.Cavalry)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Throwing, 2f);
            }
            else if (_serviceRecord.Assignment == RFEnlistmentAssignment.Archer)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 2f);
            }
            else if (_serviceRecord.Assignment == RFEnlistmentAssignment.Support)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 3f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 2f);
            }
            else
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 1.5f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 2f);
            }
        }

        if (_serviceRecord.InCommanderBlockade)
        {
            int blockadeXp = 5;
            Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 2f);
            Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 2f);
            ApplyEngineerContextSupport(ref blockadeXp);
            _serviceRecord.ServiceXp += blockadeXp;
        }
    }

    private void GrantReputationDailyXp()
    {
        if (_serviceRecord.InCommanderSiege || _serviceRecord.InCommanderBlockade)
        {
            AdjustCommanderReputation(siege: 1);
            return;
        }

        if (_serviceRecord.InCommanderArmy)
        {
            AdjustCommanderReputation(command: 1);
        }
    }

    private bool ShouldDeferDailyWage()
    {
        if (_serviceRecord.DeferredWageAmount >= 60)
        {
            return false;
        }

        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        if (!rhythmState.ActiveCampaign && !rhythmState.SiegePressure)
        {
            return false;
        }

        if (!rhythmState.LowSupplies && _serviceRecord.CommanderTrust > -3)
        {
            return false;
        }

        return MBRandom.RandomFloat < (rhythmState.SiegePressure ? 0.22f : 0.14f);
    }

    private void TryResolveDeferredWages()
    {
        if (_serviceRecord.DeferredWageAmount <= 0)
        {
            return;
        }

        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        if (!rhythmState.QuietGarrison && !rhythmState.RecoveryState && !CommanderNeedsSupplies())
        {
            return;
        }

        int amount = _serviceRecord.DeferredWageAmount;
        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, false);
        _serviceRecord.TotalWagesPaid += amount;
        _serviceRecord.DeferredWageAmount = 0;
        RecordServiceIncident(
            "Back Pay Issued",
            $"The quartermaster finally cleared the arrears and {amount} gold in delayed pay reached your hands.",
            popup: false);
    }

    private int GetDailyWage()
    {
        float baseWage = _serviceRecord.Rank switch
        {
            RFEnlistmentRank.Soldier => 8f,
            RFEnlistmentRank.Veteran => 14f,
            RFEnlistmentRank.Sergeant => 22f,
            _ => 5f
        };

        return Math.Max(1, (int)Math.Round(baseWage * RFEnlistmentSettings.Instance.WageMultiplier));
    }

    private int GetDailyServiceXp()
    {
        float baseXp = _serviceRecord.Rank switch
        {
            RFEnlistmentRank.Soldier => 12f,
            RFEnlistmentRank.Veteran => 16f,
            RFEnlistmentRank.Sergeant => 20f,
            _ => 10f
        };

        return Math.Max(1, (int)Math.Round(baseXp * RFEnlistmentSettings.Instance.DailyXPMultiplier));
    }

    private void GrantAssignmentXp()
    {
        float baseXp = 10f * RFEnlistmentSettings.Instance.LevelUpXPMultiplier;

        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, baseXp);
                Hero.MainHero.AddSkillXp(DefaultSkills.Crossbow, baseXp * 0.4f);
                break;
            case RFEnlistmentAssignment.Cavalry:
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, baseXp);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, baseXp * 0.5f);
                break;
            case RFEnlistmentAssignment.Support:
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, baseXp * 0.75f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, baseXp * 0.75f);
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, baseXp);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, baseXp * 0.5f);
                break;
        }
    }

    private void TryPromote()
    {
        if (_serviceRecord.Rank == RFEnlistmentRank.Recruit
            && _serviceRecord.DaysServed >= 7
            && _serviceRecord.ServiceXp >= RecruitPromotionXp)
        {
            ApplyPromotion(
                RFEnlistmentRank.Soldier,
                "You have proven you can stay in line, keep pace, and bear ordinary soldier duty.");
        }
        else if (_serviceRecord.Rank == RFEnlistmentRank.Soldier
            && _serviceRecord.DaysServed >= 25
            && _serviceRecord.ServiceXp >= SoldierPromotionXp
            && Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) >= 20
            && _serviceRecord.DutySuccesses >= 2
            && (_serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount >= 1 || _serviceRecord.BattleMeritCount >= 1))
        {
            ApplyPromotion(
                RFEnlistmentRank.Veteran,
                "You are no longer being treated as raw line strength. The command now regards you as a veteran hand.");
        }
        else if (_serviceRecord.Rank == RFEnlistmentRank.Veteran
            && _serviceRecord.DaysServed >= 60
            && _serviceRecord.ServiceXp >= VeteranPromotionXp
            && Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) >= 50
            && _serviceRecord.CommanderTrust >= 6
            && _serviceRecord.DutySuccesses >= 5
            && _serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount >= 3)
        {
            ApplyPromotion(
                RFEnlistmentRank.Sergeant,
                "Your commander is now willing to put men and responsibility under your word.");
        }
    }

    private void ApplyPromotion(RFEnlistmentRank newRank, string roleText)
    {
        RFEnlistmentRank oldRank = _serviceRecord.Rank;
        if (newRank <= oldRank)
        {
            return;
        }

        _serviceRecord.Rank = newRank;
        _serviceRecord.PromotionCount++;
        _serviceRecord.LastPromotionDay = (int)CampaignTime.Now.ToDays;

        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, newRank == RFEnlistmentRank.Sergeant ? 20f : 10f);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, newRank == RFEnlistmentRank.Sergeant ? 12f : 6f);

        Hero? commander = ResolveCommander();
        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, newRank == RFEnlistmentRank.Sergeant ? 2 : 1, true, true);
        }

        TextObject message = RFEnlistmentSettings.Instance.CustomText
            ? GetConfiguredText("E_RANK_UP_DIALOGUE", "You have been promoted to {RANK}.")
            : GetConfiguredText("rf_enlistment_promoted", "You have been promoted to {RANK}.");
        message.SetTextVariable("HERO", Hero.MainHero.Name);
        message.SetTextVariable("RANK", GetRankName(_serviceRecord.Rank));

        string promotionText = $"{message}\n\n{roleText}";
        RecordServiceIncident("Promotion", $"{GetRankName(newRank).ToString()} - {roleText}", popup: false);
        ShowDutyPopup("Promotion", promotionText);
    }

    private void AddLordDialogues(CampaignGameStarter starter)
    {
        starter.AddPlayerLine(
            "rf_enlistment_lord_join_start",
            "lord_start",
            "rf_enlistment_lord_join_answer",
            new TextObject("{=rf_enlistment_petition_dialog}I am here about my petition to enlist in your service.").ToString(),
            CanEnlistWithConversationLord,
            null);

        starter.AddDialogLine(
            "rf_enlistment_lord_join_answer_line",
            "rf_enlistment_lord_join_answer",
            "rf_enlistment_lord_join_confirm",
            GetConfiguredText("E_DIALOGUE_9", "If you mean to serve, then serve with discipline. Are you ready to take the oath?").ToString(),
            null,
            null);

        starter.AddPlayerLine(
            "rf_enlistment_lord_join_accept",
            "rf_enlistment_lord_join_confirm",
            "rf_enlistment_lord_join_oath_words",
            GetConfiguredText("E_DIALOGUE_10", "I am ready.").ToString(),
            null,
            null);

        starter.AddDialogLine(
            "rf_enlistment_lord_join_oath_words_line",
            "rf_enlistment_lord_join_oath_words",
            "rf_enlistment_lord_join_oath_accept",
            GetConfiguredText("rf_enlistment_oath_words", "Then hear your oath. You will obey your commander, hold your place in the line, and not abandon your brothers in battle. Do you swear it?").ToString(),
            null,
            null);

        starter.AddPlayerLine(
            "rf_enlistment_lord_join_oath_accept_line",
            "rf_enlistment_lord_join_oath_accept",
            "rf_enlistment_lord_join_oath_complete",
            GetConfiguredText("rf_enlistment_oath_accept", "I swear it.").ToString(),
            null,
            CompleteEnlistmentWithConversationLord);

        starter.AddDialogLine(
            "rf_enlistment_lord_join_oath_complete_line",
            "rf_enlistment_lord_join_oath_complete",
            "close_window",
            GetConfiguredText("rf_enlistment_oath_complete", "Then rise and take your place. Your service begins now. Do not shame this banner.").ToString(),
            null,
            FinalizeEnlistmentConversation);

        starter.AddPlayerLine(
            "rf_enlistment_lord_join_decline",
            "rf_enlistment_lord_join_confirm",
            "close_window",
            GetConfiguredText("E_DIALOGUE_14", "Not yet.").ToString(),
            null,
            null);

        starter.AddPlayerLine(
            "rf_enlistment_commander_status",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_26", "I want to review my service.").ToString(),
            IsSpeakingToCommander,
            ShowServiceStatus);

        starter.AddPlayerLine(
            "rf_enlistment_commander_assignment",
            "lord_start",
            "rf_enlistment_assignment_answer",
            GetConfiguredText("E_DIALOGUE_24", "I want a different duty assignment.").ToString(),
            IsSpeakingToCommander,
            null);

        starter.AddPlayerLine(
            "rf_enlistment_commander_recon",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_36", "I want a more dangerous scouting assignment.").ToString(),
            CanRequestDutyMission,
            StartRequestedDutyMission);

        starter.AddPlayerLine(
            "rf_enlistment_commander_report_recon",
            "lord_start",
            "close_window",
            "I have returned from the scouting assignment.",
            CanReportDutyMission,
            ReportDutyMissionToCommander);

        starter.AddPlayerLine(
            "rf_enlistment_commander_cancel_recon",
            "lord_start",
            "close_window",
            "Can we cancel the patrol mission?",
            CanCancelDutyMission,
            CancelDutyMissionByCommander);

        starter.AddDialogLine(
            "rf_enlistment_assignment_answer_line",
            "rf_enlistment_assignment_answer",
            "rf_enlistment_assignment_pick",
            GetConfiguredText("E_DIALOGUE_29", "Very well. Where do you think you will serve best?").ToString(),
            null,
            null);

        AddAssignmentLine(starter, "infantry", "Infantry.", RFEnlistmentAssignment.Infantry);
        AddAssignmentLine(starter, "archer", "Ranged line.", RFEnlistmentAssignment.Archer);
        AddAssignmentLine(starter, "cavalry", "Mounted duty.", RFEnlistmentAssignment.Cavalry);
        AddAssignmentLine(starter, "support", "Quartermaster and support duty.", RFEnlistmentAssignment.Support);

        starter.AddPlayerLine(
            "rf_enlistment_commander_armorer",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_111", "I need some equipment issued to me.").ToString(),
            CanIssueEquipment,
            IssueServiceEquipment);

        starter.AddPlayerLine(
            "rf_enlistment_commander_return_gear",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_118", "I need to return some gear.").ToString(),
            CanReturnIssuedEquipment,
            ReturnIssuedEquipment);

        starter.AddPlayerLine(
            "rf_enlistment_commander_buy_gear",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_135", "I want to pay for the gear and keep it.").ToString(),
            CanPayForIssuedEquipment,
            PayForIssuedEquipment);

        starter.AddPlayerLine(
            "rf_enlistment_commander_renew",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_62", "I want to renew my contract.").ToString(),
            CanRenewWithCommander,
            RenewContract);

        starter.AddPlayerLine(
            "rf_enlistment_commander_discharge",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_63", "I want to leave your service.").ToString(),
            IsSpeakingToCommander,
            Discharge);

        starter.AddPlayerLine(
            "rf_enlistment_commander_commission",
            "lord_start",
            "close_window",
            GetConfiguredText("E_DIALOGUE_105", "I believe I am ready for higher station.").ToString(),
            CanRequestCommissionRecommendation,
            RequestCommissionRecommendation);
    }

    private void AddAssignmentLine(CampaignGameStarter starter, string suffix, string text, RFEnlistmentAssignment assignment)
    {
        starter.AddPlayerLine(
            $"rf_enlistment_assignment_{suffix}",
            "rf_enlistment_assignment_pick",
            "close_window",
            text,
            () => IsSpeakingToCommander() && _serviceRecord.Assignment != assignment,
            () => ChangeAssignment(assignment));
    }

    private bool CanEnlistWithConversationLord()
    {
        Hero? hero = Hero.OneToOneConversationHero;
        return CanEnlistWithHero(hero)
            && HasPendingEnlistmentPetition()
            && hero?.StringId == _pendingEnlistmentCommanderId;
    }

    private void CompleteEnlistmentWithConversationLord()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (hero == null)
        {
            return;
        }

        _serviceRecord.Start(hero, ChooseDefaultAssignment(), DefaultContractDays);
        ClearPendingEnlistmentPetition();

        TextObject message = GetConfiguredText(
            "rf_enlistment_joined_dialog",
            "You have sworn service under {COMMANDER} as a {ASSIGNMENT}.");
        message.SetTextVariable("COMMANDER", hero.Name);
        message.SetTextVariable("ASSIGNMENT", GetAssignmentName(_serviceRecord.Assignment));
        ShowMessage(message.ToString());

        _pendingCommanderAttachment = !TryAttachPlayerToCommanderDuty(showFeedback: false);
    }

    private void FinalizeEnlistmentConversation()
    {
        TraceEnlistmentState("FinalizeEnlistmentConversationStart");
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        if (commanderParty?.MapEvent != null && TryJoinCommanderEncounterBattle(commanderParty.MapEvent, commanderParty))
        {
            TraceEnlistmentState("FinalizeEnlistmentConversationBattleJoin");
            return;
        }

        if (Campaign.Current.CurrentConversationContext != ConversationContext.PartyEncounter)
        {
            TraceEnlistmentState("FinalizeEnlistmentConversationIgnored", "Conversation context is not PartyEncounter.");
            return;
        }

        if (PlayerEncounter.Current == null || PlayerEncounter.EncounteredMobileParty == null)
        {
            TraceEnlistmentState("FinalizeEnlistmentConversationIgnored", "No active player encounter.");
            return;
        }

        if (commanderParty == null || !IsCommanderRelatedParty(PlayerEncounter.EncounteredMobileParty, commanderParty))
        {
            TraceEnlistmentState("FinalizeEnlistmentConversationIgnored", "Encountered party is not commander-related.");
            return;
        }

        _pendingCommanderAttachment = true;
        PlayerEncounter.Finish();
        TraceEnlistmentState("FinalizeEnlistmentConversationAttachAttempt");
        TryAttachPlayerToCommanderDuty(showFeedback: false);
    }

    private bool IsSpeakingToCommander()
    {
        return _serviceRecord.IsEnlisted && Hero.OneToOneConversationHero?.StringId == _serviceRecord.CommanderId;
    }

    private void MarkCommanderAttachmentPendingFromConversation()
    {
        if (_serviceRecord.IsEnlisted && Hero.OneToOneConversationHero?.StringId == _serviceRecord.CommanderId)
        {
            _pendingCommanderAttachment = true;
        }
    }

    private bool CanRenewWithCommander()
    {
        return IsSpeakingToCommander() && CanRenewContract();
    }

    private void ChangeAssignment(RFEnlistmentAssignment assignment)
    {
        _serviceRecord.Assignment = assignment;
        MarkCommanderAttachmentPendingFromConversation();

        TextObject message = new("{=rf_enlistment_assignment_changed}Your duty assignment is now {ASSIGNMENT}.");
        message.SetTextVariable("ASSIGNMENT", GetAssignmentName(assignment));
        ShowMessage(message.ToString());
    }

    private bool CanIssueEquipment()
    {
        return RFEnlistmentSettings.Instance.EnableArmorer
            && IsSpeakingToCommander()
            && !HasOutstandingIssuedEquipment()
            && _serviceRecord.OutstandingEquipmentDebt <= 0
            && _serviceRecord.HighestEquipmentRankIssued < (int)_serviceRecord.Rank;
    }

    private void IssueServiceEquipment()
    {
        MarkCommanderAttachmentPendingFromConversation();
        List<string> issuedItemIds = GetEquipmentIdsForCurrentService().ToList();
        int givenCount = 0;

        foreach (string itemId in issuedItemIds)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null)
            {
                continue;
            }

            PartyBase.MainParty.ItemRoster.AddToCounts(item, 1);
            givenCount++;
        }

        _serviceRecord.HighestEquipmentRankIssued = (int)_serviceRecord.Rank;
        _serviceRecord.IssuedEquipmentIds = string.Join("|", issuedItemIds);
        _serviceRecord.OutstandingEquipmentDebt = 0;

        if (givenCount <= 0)
        {
            ShowMessage("No valid service equipment could be issued right now.");
            return;
        }

        int payoffCost = GetEquipmentPayoffCost(issuedItemIds);
        string quartermasterSupport = ApplyQuartermasterEquipmentSupport(ref payoffCost);
        ShowMessage(string.IsNullOrEmpty(quartermasterSupport)
            ? $"Service equipment issued: {givenCount} items added to your inventory. Return them later or pay {payoffCost} gold to keep them."
            : $"Service equipment issued: {givenCount} items added to your inventory. Return them later or pay {payoffCost} gold to keep them. {quartermasterSupport}");
    }

    private string[] GetEquipmentIdsForCurrentService()
    {
        return _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => _serviceRecord.Rank switch
            {
                RFEnlistmentRank.Sergeant => new[] { "empire_warrior_padded_armor_b", "nasal_helmet_over_padded_coif", "guarded_padded_vambrace", "belted_leather_boots" },
                RFEnlistmentRank.Veteran => new[] { "imperial_padded_cloth", "open_padded_coif", "padded_vambrace", "wrapped_leather_boots" },
                _ => new[] { "short_padded_robe", "padded_cap", "leather_gloves", "wrapped_shoes" }
            },
            RFEnlistmentAssignment.Cavalry => _serviceRecord.Rank switch
            {
                RFEnlistmentRank.Sergeant => new[] { "empire_horseman_armor", "nasal_helmet_over_padded_coif", "guarded_padded_vambrace", "belted_leather_boots" },
                RFEnlistmentRank.Veteran => new[] { "empire_warrior_padded_armor_a", "imperial_padded_coif", "padded_vambrace", "wrapped_leather_boots" },
                _ => new[] { "padded_coat", "padded_cap", "leather_gloves", "wrapped_shoes" }
            },
            RFEnlistmentAssignment.Support => _serviceRecord.Rank switch
            {
                RFEnlistmentRank.Sergeant => new[] { "empire_warrior_padded_armor_g", "imperial_padded_coif", "guarded_padded_vambrace", "belted_leather_boots" },
                RFEnlistmentRank.Veteran => new[] { "padded_leather_shirt", "open_padded_coif", "padded_vambrace", "wrapped_leather_boots" },
                _ => new[] { "long_padded_robe", "padded_cap", "leather_gloves", "wrapped_shoes" }
            },
            _ => _serviceRecord.Rank switch
            {
                RFEnlistmentRank.Sergeant => new[] { "empire_warrior_padded_armor_a", "nasal_helmet_over_padded_coif", "guarded_padded_vambrace", "belted_leather_boots" },
                RFEnlistmentRank.Veteran => new[] { "imperial_padded_cloth", "imperial_padded_coif", "padded_vambrace", "wrapped_leather_boots" },
                _ => new[] { "padded_short_coat", "padded_cap", "leather_gloves", "wrapped_shoes" }
            }
        };
    }

    private bool CanTrainToday()
    {
        return _serviceRecord.IsEnlisted && _serviceRecord.LastTrainingDay != (int)CampaignTime.Now.ToDays;
    }

    private void TrainWithTroops()
    {
        MarkCommanderAttachmentPendingFromConversation();
        _serviceRecord.LastTrainingDay = (int)CampaignTime.Now.ToDays;

        float trainingXp = RFEnlistmentSettings.Instance.HourlyTrainingXp * 4f;
        int serviceXpGain = Math.Max(1, (int)trainingXp);
        string companionSupportText = ApplyCompanionTrainingSupport(ref trainingXp, ref serviceXpGain);
        _serviceRecord.ServiceXp += serviceXpGain;
        GrantAssignmentTrainingXp(trainingXp);
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, trainingXp * 0.5f);
        TryPromote();

        ShowMessage(string.IsNullOrEmpty(companionSupportText)
            ? "You drill with the troops and sharpen your service skills."
            : $"You drill with the troops and sharpen your service skills. {companionSupportText}");
    }

    private void TryTriggerServiceDutyEvent()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return;
        }

        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null
            || mainParty.MapEvent != null
            || HasActiveDutyMission()
            || _serviceRecord.DaysServed < 3)
        {
            return;
        }

        bool canStartInteractiveDuty = CanStartInteractiveDuty();
        bool canStartServiceShift = CanStartServiceShiftDuty();
        bool canStartFieldDuty = CanStartDutyMission();
        if (!canStartInteractiveDuty && !canStartServiceShift && !canStartFieldDuty)
        {
            return;
        }

        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.LastServiceEventDay == today)
        {
            return;
        }

        int daysSinceLastOffer = _serviceRecord.LastServiceEventDay < 0
            ? 99
            : today - _serviceRecord.LastServiceEventDay;
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        float trustBonus = _serviceRecord.CommanderTrust >= 12 ? 0.05f : _serviceRecord.CommanderTrust >= 6 ? 0.025f : 0f;
        float distrustBonus = _serviceRecord.CommanderTrust <= -5 ? 0.03f : 0f;
        float urgencyBonus = rhythmState.PreBattle || rhythmState.SiegePressure
            ? 0.05f
            : rhythmState.LowSupplies || rhythmState.RecoveryState
                ? 0.03f
                : rhythmState.ActiveCampaign
                    ? 0.02f
                    : 0f;
        float baseChance = canStartFieldDuty
            ? (rhythmState.QuietGarrison ? 0.05f : 0.08f)
            : (rhythmState.QuietGarrison ? 0.04f : 0.06f);
        if (_serviceRecord.LastTensionEventDay >= 0 && today - _serviceRecord.LastTensionEventDay < 2)
        {
            return;
        }

        int minimumCooldownDays = (rhythmState.PreBattle || rhythmState.SiegePressure || rhythmState.LowSupplies) ? 2 : 4;
        int guaranteedOfferDays = (rhythmState.PreBattle || rhythmState.SiegePressure || rhythmState.LowSupplies) ? 4 : 7;
        if (daysSinceLastOffer < minimumCooldownDays)
        {
            return;
        }

        float chance = MBMath.ClampFloat(baseChance + trustBonus + distrustBonus + urgencyBonus, 0f, 0.45f);
        bool guaranteedOffer = daysSinceLastOffer >= guaranteedOfferDays
            || ((canStartServiceShift || canStartInteractiveDuty) && daysSinceLastOffer >= minimumCooldownDays + 1 && (rhythmState.PreBattle || rhythmState.SiegePressure || rhythmState.RecoveryState || rhythmState.LowSupplies));
        if (!guaranteedOffer && MBRandom.RandomFloat > chance)
        {
            return;
        }

        _serviceRecord.LastServiceEventDay = today;

        TextObject title = new("{=rf_enlistment_service_event_title}Service Duty");
        TextObject text = GetServiceDutyEventText();

        InformationManager.ShowInquiry(new InquiryData(
            title.ToString(),
            text.ToString(),
            true,
            true,
            "Take the duty",
            "Decline",
            AcceptServiceDutyEvent,
            DeclineServiceDutyEvent), true);
    }

    private void TryTriggerServiceIncident()
    {
        if (!_serviceRecord.IsEnlisted
            || HasActiveDutyMission()
            || _serviceRecord.DaysServed < 7)
        {
            return;
        }

        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.LastTensionEventDay == today || _serviceRecord.LastServiceEventDay == today)
        {
            return;
        }

        if (_serviceRecord.LastTensionEventDay >= 0 && today - _serviceRecord.LastTensionEventDay < 3)
        {
            return;
        }

        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        int incidentType = GetPreferredIncidentType(rhythmState);
        float chance = incidentType switch
        {
            IncidentPayDelay => 0.22f,
            IncidentShortRations => 0.16f,
            IncidentCampDiscipline => 0.14f,
            _ => 0f
        };

        if (chance <= 0f || MBRandom.RandomFloat > chance)
        {
            return;
        }

        _serviceRecord.LastTensionEventDay = today;
        NoteIncidentShown(incidentType);

        switch (incidentType)
        {
            case IncidentPayDelay:
                ShowDeferredPayIncidentInquiry();
                return;
            case IncidentShortRations:
                ShowShortRationsIncidentInquiry();
                return;
            case IncidentCampDiscipline:
                ShowCampDisciplineIncidentInquiry();
                return;
            default:
                return;
        }
    }

    private TextObject GetServiceDutyEventText()
    {
        bool isNavalDuty = _serviceRecord.InCommanderNavalService || MobileParty.MainParty?.IsCurrentlyAtSea == true;
        bool isSettlementDuty = MobileParty.MainParty?.CurrentSettlement != null
            || ResolveCommander()?.PartyBelongedTo?.CurrentSettlement != null;
        string trustLead = GetCommanderDutyLeadText();
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        bool preferFieldDuty = ShouldPreferFieldDutyOffer(
            rhythmState,
            canStartFieldDuty: CanStartDutyMission(),
            canStartInteractiveDuty: CanStartInteractiveDuty());

        int interactiveDutyType = GetPreferredInteractiveDutyType();
        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyTreatWounded)
        {
            return new TextObject($"{trustLead} The wounded are piling up around the host, and the surgeons need a steady hand right now.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyNightPatrol)
        {
            return new TextObject($"{trustLead} Darkness has settled over the camp line. Your commander wants a reliable night patrol before anyone sleeps easy.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyTrainRecruits)
        {
            return new TextObject($"{trustLead} Fresh recruits are making a mess of the drill line. Your commander wants you to put them in order.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyEquipmentCheck)
        {
            return new TextObject($"{trustLead} The officers want weapons, harness, and shields inspected before something important goes wrong on the march.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyInspectDefenses)
        {
            return new TextObject($"{trustLead} The commander wants the defenses looked over now, before the enemy tests a weak point.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyGateWatch)
        {
            return new TextObject($"{trustLead} The settlement gates need a harder watch tonight, and your commander wants someone dependable standing over it.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyQuartermasterShortage)
        {
            return new TextObject($"{trustLead} The quartermaster line is strained and stores are not balancing cleanly. Your commander wants the shortage sorted before tempers rise.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyLeadPatrol)
        {
            return new TextObject($"{trustLead} Your commander is ready to trust you with a patrol of your own and wants to see how you handle men away from the main line.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyCoordinateSupply)
        {
            return new TextObject($"{trustLead} The commander needs someone reliable to coordinate stores, wagons, and issue order before the march loses shape.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyCommandSquad)
        {
            return new TextObject($"{trustLead} A picked squad is being handed to you for a live task. Your commander wants to see whether men actually move well under your word.");
        }

        if (!preferFieldDuty && interactiveDutyType == InteractiveDutyStrategicPlanning)
        {
            return new TextObject($"{trustLead} The commander wants another head over the battle board before the next move is fixed.");
        }

        if (CanStartReliefDispatchDuty())
        {
            return new TextObject($"{trustLead} The commander needs a runner to carry an urgent call for relief before the siege line closes.");
        }

        if (isNavalDuty)
        {
            string body = _serviceRecord.Assignment switch
            {
                RFEnlistmentAssignment.Archer => "Your commander orders extra watch duty at sea. Keep eyes on the horizon and be ready to answer sails with arrows.",
                RFEnlistmentAssignment.Cavalry => "Your commander needs a fast boarding detail. Be ready to cross first if an enemy ship closes.",
                RFEnlistmentAssignment.Support => "The fleet needs extra hands on supplies, rigging, and wounded care before the next contact.",
                _ => "Hard naval duty has opened. Your commander wants steady soldiers ready for boarding and deck fighting."
            };

            return new TextObject($"{trustLead} {body}");
        }

        if (isSettlementDuty)
        {
            string body = _serviceRecord.Assignment switch
            {
                RFEnlistmentAssignment.Archer => "The garrison needs a sharp watch on the walls and gates while your commander handles recruitment and reports.",
                RFEnlistmentAssignment.Cavalry => "Your commander needs mounted orderlies and gate patrols while the host regathers in settlement.",
                RFEnlistmentAssignment.Support => "Stores, wounded, and quartermaster books are in disorder. Extra service inside the settlement is needed at once.",
                _ => "A garrison watch has opened while your commander recruits and reorganizes. Reliable soldiers are needed on rounds and gate duty."
            };

            return new TextObject($"{trustLead} {body}");
        }

        int fieldDutyType = GetPreferredFieldDutyMissionType();
        string fieldBody = fieldDutyType switch
        {
            DutyMissionReliefDispatch => "The commander needs a relief dispatch carried through enemy pressure before the position collapses.",
            DutyMissionTrustedDispatch => "A sealed field dispatch must be carried to another command post before the line shifts again.",
            DutyMissionSupplyDelivery => CommanderNeedsSupplies()
                ? "The host is running short on food and stores. A supply errand has opened and must be handled quickly."
                : "The quartermasters need a reliable runner to carry supply orders and return with fresh stores.",
            DutyMissionForage => "Stores are running thin along the march. A forage detail is needed to bring food back from the nearby countryside.",
            DutyMissionRecruitmentErrand => "The commander wants fresh men gathered from nearby settlements before the next stage of the march.",
            DutyMissionScoutRoute => "The route ahead is uncertain. Your commander wants a proper scouting ride before the column commits to the road.",
            DutyMissionMountedPursuit => "Enemy riders have been sighted ahead of the column. A fast pursuit detail is needed at once.",
            DutyMissionBanditHunt => "Bandits are shadowing the march. Your commander wants them hunted down before they strike the baggage.",
            DutyMissionDeserterSweep => "Armed deserters have slipped away from the wider war. Your commander wants them run down before they turn raider.",
            DutyMissionHideoutStrike => "A nearby hideout is feeding raids against the host. Your commander wants it cleared, not merely watched.",
            DutyMissionRoadPatrol => "The roads around the host have become uncertain. A disciplined patrol is needed to secure the route ahead.",
            _ => "A forward scouting sweep has opened. Your commander wants eyes ahead of the march before the enemy can hide again."
        };

        return new TextObject($"{trustLead} {fieldBody}");
    }

    private void AcceptServiceDutyEvent()
    {
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        bool canStartFieldDuty = CanStartDutyMission();
        int interactiveDutyType = GetPreferredInteractiveDutyType();
        bool canStartInteractiveDuty = interactiveDutyType != InteractiveDutyNone;

        if (ShouldPreferFieldDutyOffer(rhythmState, canStartFieldDuty, canStartInteractiveDuty) && StartDutyMission())
        {
            return;
        }

        if (canStartInteractiveDuty)
        {
            StartInteractiveDuty(interactiveDutyType);
            return;
        }

        if (CanStartServiceShiftDuty())
        {
            StartServiceShiftDutyMission();
            return;
        }

        if (StartDutyMission())
        {
            return;
        }

        ShowDutyPopup("Service Duty", "There is no proper duty to hand out right now.");
    }

    private bool ShouldPreferFieldDutyOffer(ArmyRhythmState rhythmState, bool canStartFieldDuty, bool canStartInteractiveDuty)
    {
        if (!canStartFieldDuty)
        {
            return false;
        }

        if (!canStartInteractiveDuty)
        {
            return true;
        }

        if (rhythmState.SiegePressure || rhythmState.RecoveryState || rhythmState.QuietGarrison)
        {
            return false;
        }

        if (rhythmState.PreBattle || rhythmState.ActiveCampaign || rhythmState.LowSupplies)
        {
            return true;
        }

        if (_serviceRecord.Rank >= RFEnlistmentRank.Veteran)
        {
            return true;
        }

        return _serviceRecord.Rank >= RFEnlistmentRank.Soldier
            && _serviceRecord.Assignment != RFEnlistmentAssignment.Support
            && rhythmState.RecruitCount < 8;
    }

    private void ResolveServiceDutyEvent()
    {
        Hero? commander = ResolveCommander();
        bool isNavalDuty = _serviceRecord.InCommanderNavalService || MobileParty.MainParty?.IsCurrentlyAtSea == true;
        bool isBlockadeDuty = _serviceRecord.InCommanderBlockade;
        int serviceXp = 30;
        int gold = 40;
        float leadershipXp = 8f;
        string dutyLabel = isBlockadeDuty ? "blockade duty" : isNavalDuty ? "naval duty" : "duty";

        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, isNavalDuty ? 16f : 20f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, isNavalDuty ? 8f : 4f);
                serviceXp = isNavalDuty ? 38 : 35;
                gold = isNavalDuty ? 50 : 45;
                break;
            case RFEnlistmentAssignment.Cavalry:
                Hero.MainHero.AddSkillXp(isNavalDuty ? DefaultSkills.Athletics : DefaultSkills.Riding, 20f);
                Hero.MainHero.AddSkillXp(isNavalDuty ? DefaultSkills.OneHanded : DefaultSkills.Polearm, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, isNavalDuty ? 10f : 4f);
                serviceXp = isNavalDuty ? 40 : 35;
                gold = 50;
                break;
            case RFEnlistmentAssignment.Support:
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 20f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, isNavalDuty ? 12f : 5f);
                serviceXp = isNavalDuty ? 45 : 40;
                gold = isNavalDuty ? 42 : 35;
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, isNavalDuty ? 20f : 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, isNavalDuty ? 8f : 3f);
                break;
        }

        if (isBlockadeDuty)
        {
            serviceXp += 8;
            gold += 10;
            Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 6f);
        }

        ApplyCommanderTrustRewardBonus(ref serviceXp, ref gold);
        ApplyRoleSpecificCompanionDutySupport(isNavalDuty, isBlockadeDuty, ref serviceXp, ref gold);

        _serviceRecord.ServiceXp += serviceXp;
        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
        _serviceRecord.TotalWagesPaid += gold;
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, leadershipXp);
        commander?.Clan?.AddRenown(0f, false);

        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, 1, true, true);
        }

        _serviceRecord.ServiceShiftCount++;
        RegisterDutySuccess(1);
        TryPromote();
        ShowDutyPopup("Duty Recorded", $"{dutyLabel} completed.\n\nYou gained {serviceXp} service XP and {gold} gold.");
    }

    private void DeclineServiceDutyEvent()
    {
        Hero? commander = ResolveCommander();
        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, -1, true, true);
        }

        AdjustCommanderTrust(_serviceRecord.CommanderTrust <= -5 ? -1 : -2);
        ShowDutyPopup("Duty Declined", "You declined the extra duty. Your commander takes note.");
    }

    private bool CanRequestDutyMission()
    {
        return IsSpeakingToCommander()
            && _serviceRecord.CommanderTrust > -8
            && (_serviceRecord.Rank >= RFEnlistmentRank.Soldier || _serviceRecord.CommanderTrust >= 6)
            && !HasActiveDutyMission();
    }

    private bool CanReportDutyMission()
    {
        return IsSpeakingToCommander()
            && HasActiveDutyMission()
            && _serviceRecord.ActiveDutyMissionType != DutyMissionServiceShift
            && _serviceRecord.ActiveDutyMissionCompleted;
    }

    private bool CanCancelDutyMission()
    {
        return IsSpeakingToCommander()
            && HasActiveDutyMission()
            && _serviceRecord.ActiveDutyMissionType != DutyMissionServiceShift
            && !_serviceRecord.ActiveDutyMissionCompleted;
    }

    private void StartRequestedDutyMission()
    {
        MarkCommanderAttachmentPendingFromConversation();

        if (_serviceRecord.CommanderTrust <= -8)
        {
            ShowMessage("Your commander does not trust you with requested duties right now. Rebuild your standing through ordinary service.");
            return;
        }

        if (!CanStartDutyMission())
        {
            ShowMessage("There is no proper field duty to hand out right now.");
            return;
        }

        if (!StartDutyMission())
        {
            ShowMessage("There is no proper field duty to hand out right now.");
        }
    }

    private bool CanStartDutyMission()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        Settlement? anchorSettlement = GetMissionAnchorSettlement();
        if (HasActiveDutyMission()
            || mainParty == null
            || mainParty.MapEvent != null
            || mainParty.IsCurrentlyAtSea
            || mainParty.CurrentSettlement != null
            || commanderParty?.IsCurrentlyAtSea == true
            || _serviceRecord.InCommanderNavalService
            || _serviceRecord.InCommanderBlockade
            || anchorSettlement == null)
        {
            return false;
        }

        int missionType = GetPreferredFieldDutyMissionType();
        return missionType switch
        {
            DutyMissionReliefDispatch => FindTrustedDispatchSettlement(mainParty, anchorSettlement) != null,
            DutyMissionTrustedDispatch => FindTrustedDispatchSettlement(mainParty, anchorSettlement) != null,
            DutyMissionSupplyDelivery => FindNearbyDutySettlement(mainParty, anchorSettlement) != null,
            DutyMissionForage => FindNearbyForageSettlement(mainParty, anchorSettlement) != null,
            DutyMissionRecruitmentErrand => FindRecruitmentDutySettlement(mainParty, anchorSettlement) != null,
            DutyMissionScoutRoute => FindNearbyDutySettlement(mainParty, anchorSettlement) != null,
            DutyMissionRoadPatrol => FindNearbyDutySettlement(mainParty, anchorSettlement) != null,
            DutyMissionHideoutStrike => FindNearbyHideoutDutySettlement(mainParty, anchorSettlement) != null,
            DutyMissionMountedPursuit or DutyMissionReconSweep or DutyMissionBanditHunt or DutyMissionDeserterSweep => GetBanditClanForDutyMission() != null,
            _ => false
        };
    }

    private bool CanStartInteractiveDuty()
    {
        if (HasActiveDutyMission())
        {
            return false;
        }

        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || mainParty.MapEvent != null || mainParty.IsCurrentlyAtSea)
        {
            return false;
        }

        return GetPreferredInteractiveDutyType() != InteractiveDutyNone;
    }

    private bool CanStartServiceShiftDuty()
    {
        if (HasActiveDutyMission())
        {
            return false;
        }

        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || mainParty.MapEvent != null)
        {
            return false;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        bool isSettlementService = mainParty.CurrentSettlement != null
            || commanderParty?.CurrentSettlement != null;

        return _serviceRecord.InCommanderSiege
            || _serviceRecord.InCommanderNavalService
            || _serviceRecord.InCommanderBlockade
            || isSettlementService;
    }

    private bool CanStartReliefDispatchDuty()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        Settlement? anchorSettlement = GetMissionAnchorSettlement();
        if (!_serviceRecord.InCommanderSiege && !_serviceRecord.InCommanderBlockade)
        {
            return false;
        }

        return _serviceRecord.CommanderTrust >= 8
            && mainParty != null
            && !mainParty.IsCurrentlyAtSea
            && mainParty.CurrentSettlement == null
            && mainParty.MapEvent == null
            && anchorSettlement != null
            && FindTrustedDispatchSettlement(mainParty, anchorSettlement) != null;
    }

    private bool StartDutyMission()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        Settlement? anchorSettlement = GetMissionAnchorSettlement();

        if (anchorSettlement == null || mainParty == null)
        {
            return false;
        }

        int missionType = GetPreferredFieldDutyMissionType();
        NoteFieldDutyStarted(missionType);
        switch (missionType)
        {
            case DutyMissionReliefDispatch:
                if (!StartReliefDispatchDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionTrustedDispatch:
                if (!StartTrustedDispatchDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionSupplyDelivery:
                if (!StartSupplyDeliveryDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionForage:
                if (!StartForageDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionRecruitmentErrand:
                if (!StartRecruitmentErrandDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionScoutRoute:
                if (!StartScoutRouteDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionRoadPatrol:
                if (!StartRoadPatrolDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionMountedPursuit:
                if (!StartMountedPursuitDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionBanditHunt:
                if (!StartBanditHuntDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionDeserterSweep:
                if (!StartDeserterSweepDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            case DutyMissionHideoutStrike:
                if (!StartHideoutStrikeDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
            default:
                if (!StartReconSweepDutyMission(mainParty, anchorSettlement))
                {
                    return false;
                }

                BeginIndependentFieldDuty();
                return true;
        }
    }

    private void StartInteractiveDuty(int dutyType)
    {
        NoteInteractiveDutyStarted(dutyType);
        switch (dutyType)
        {
            case InteractiveDutyNightPatrol:
                ShowNightPatrolInquiry();
                return;
            case InteractiveDutyTreatWounded:
                ShowTreatWoundedInquiry();
                return;
            case InteractiveDutyTrainRecruits:
                ShowTrainRecruitsInquiry();
                return;
            case InteractiveDutyEquipmentCheck:
                ShowEquipmentCheckInquiry();
                return;
            case InteractiveDutyInspectDefenses:
                ShowInspectDefensesInquiry();
                return;
            case InteractiveDutyGateWatch:
                ShowGateWatchInquiry();
                return;
            case InteractiveDutyQuartermasterShortage:
                ShowQuartermasterShortageInquiry();
                return;
            case InteractiveDutyLeadPatrol:
                ShowLeadPatrolInquiry();
                return;
            case InteractiveDutyCoordinateSupply:
                ShowCoordinateSupplyInquiry();
                return;
            case InteractiveDutyCommandSquad:
                ShowCommandSquadInquiry();
                return;
            case InteractiveDutyStrategicPlanning:
                ShowStrategicPlanningInquiry();
                return;
            default:
                StartServiceShiftDutyMission();
                return;
        }
    }

    private void BeginIndependentFieldDuty()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null)
        {
            return;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty)
        {
            mainParty.SetMoveModeHold();
        }

        mainParty.AttachedTo = null;
        RestorePlayerPartyCampaignPresence();
        _pendingCommanderAttachment = false;
        _nextAttachmentRetryHour = 0f;
        _serviceRecord.InCommanderArmy = false;
        _serviceRecord.InCommanderSiege = false;
        _serviceRecord.InCommanderNavalService = false;
        _serviceRecord.InCommanderBlockade = false;

        if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == ServiceWaitMenuId)
        {
            GameMenu.ExitToLast();
        }

        TraceEnlistmentState("BeginIndependentFieldDuty");
    }

    private void StartServiceShiftDutyMission()
    {
        NoteFieldDutyStarted(DutyMissionServiceShift);
        _serviceRecord.ActiveDutyMissionType = DutyMissionServiceShift;
        _serviceRecord.ActiveDutyMissionExpiresDay = -1;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = string.Empty;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.HoursFromNow(4f);
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        bool isSettlementService = MobileParty.MainParty?.CurrentSettlement != null
            || ResolveCommander()?.PartyBelongedTo?.CurrentSettlement != null;
        string shiftLabel = _serviceRecord.InCommanderBlockade
            ? "blockade watch"
            : _serviceRecord.InCommanderNavalService
                ? "shipboard watch"
                : _serviceRecord.InCommanderSiege
                    ? "siege shift"
                    : isSettlementService
                        ? "garrison watch"
                        : "camp watch";

        ShowDutyPopup("Duty Accepted", BuildCommanderBriefing($"You have taken a {shiftLabel}.\n\nRemain in service for 4 hours and your commander will record the work."));
    }

    private bool StartTrustedDispatchDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? targetSettlement = FindTrustedDispatchSettlement(mainParty, anchorSettlement);
        if (targetSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionTrustedDispatch;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + 6;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = targetSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        Hero? targetHero = FindTrustedDispatchHero(mainParty, anchorSettlement);
        string targetText = targetHero?.CurrentSettlement == targetSettlement
            ? $"{targetHero.Name} at {targetSettlement.Name}"
            : targetSettlement.Name.ToString();

        string briefing = (_serviceRecord.InCommanderSiege || _serviceRecord.InCommanderBlockade)
            ? $"Your commander is sending an urgent appeal for relief to {targetText}.\n\nBreak away from the line, deliver the message within 6 days, then return if you can."
            : $"Your commander entrusts you with sealed orders for {targetText}.\n\nReach the destination within 6 days, then return for a private debrief.";

        ShowDutyPopup("Duty Accepted", BuildCommanderBriefing(briefing));
        return true;
    }

    private bool StartReliefDispatchDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? targetSettlement = FindTrustedDispatchSettlement(mainParty, anchorSettlement);
        if (targetSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionReliefDispatch;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + 4;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = targetSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        Hero? targetHero = FindTrustedDispatchHero(mainParty, anchorSettlement);
        string targetText = targetHero?.CurrentSettlement == targetSettlement
            ? $"{targetHero.Name} at {targetSettlement.Name}"
            : targetSettlement.Name.ToString();

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"Your commander sends you with a relief appeal for {targetText}.\n\nBreak through, deliver the request within 4 days, then return if the road still holds."));
        return true;
    }

    private bool StartReconSweepDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Clan? banditClan = GetBanditClanForDutyMission();
        if (banditClan == null)
        {
            return false;
        }

        CampaignVec2 spawnPosition = FindDutyMissionSpawnPosition(mainParty);
        string partyId = $"rf_enlistment_duty_{CampaignTime.Now.GetHashCode():x}_{MBRandom.RandomInt(1000, 9999)}";
        MobileParty targetParty = BanditPartyComponent.CreateLooterParty(
            partyId,
            banditClan,
            anchorSettlement,
            isBossParty: false,
            banditClan.DefaultPartyTemplate,
            spawnPosition);

        targetParty.SetMoveEngageParty(mainParty, mainParty.NavigationCapability);

        _serviceRecord.ActiveDutyMissionType = DutyMissionReconSweep;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 12 ? 6 : 5);
        _serviceRecord.ActiveDutyMissionTargetPartyId = targetParty.StringId;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = anchorSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        string targetName = anchorSettlement.Name.ToString();
        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"You have been sent on a recon sweep near {targetName}.\n\nA hostile band has been marked. Destroy it within {_serviceRecord.ActiveDutyMissionExpiresDay - (int)CampaignTime.Now.ToDays} days, then report back to your commander."));
        return true;
    }

    private bool StartMountedPursuitDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Clan? banditClan = GetBanditClanForDutyMission();
        if (banditClan == null)
        {
            return false;
        }

        CampaignVec2 spawnPosition = FindDutyMissionSpawnPosition(mainParty);
        string partyId = $"rf_enlistment_pursuit_{CampaignTime.Now.GetHashCode():x}_{MBRandom.RandomInt(1000, 9999)}";
        MobileParty targetParty = BanditPartyComponent.CreateLooterParty(
            partyId,
            banditClan,
            anchorSettlement,
            isBossParty: false,
            banditClan.DefaultPartyTemplate,
            spawnPosition);

        targetParty.SetMoveEngageParty(mainParty, mainParty.NavigationCapability);

        _serviceRecord.ActiveDutyMissionType = DutyMissionMountedPursuit;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 12 ? 5 : 4);
        _serviceRecord.ActiveDutyMissionTargetPartyId = targetParty.StringId;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = anchorSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"You have been sent on a mounted pursuit near {anchorSettlement.Name}.\n\nRide down the hostile scouts within 4 days, then report back."));
        return true;
    }

    private bool StartBanditHuntDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Clan? banditClan = GetBanditClanForDutyMission();
        if (banditClan == null)
        {
            return false;
        }

        CampaignVec2 spawnPosition = FindDutyMissionSpawnPosition(mainParty);
        string partyId = $"rf_enlistment_hunt_{CampaignTime.Now.GetHashCode():x}_{MBRandom.RandomInt(1000, 9999)}";
        MobileParty targetParty = BanditPartyComponent.CreateLooterParty(
            partyId,
            banditClan,
            anchorSettlement,
            isBossParty: false,
            banditClan.DefaultPartyTemplate,
            spawnPosition);

        targetParty.SetMoveEngageParty(mainParty, mainParty.NavigationCapability);

        _serviceRecord.ActiveDutyMissionType = DutyMissionBanditHunt;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 5 : 4);
        _serviceRecord.ActiveDutyMissionTargetPartyId = targetParty.StringId;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = anchorSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"Bandits have been marked near {anchorSettlement.Name}.\n\nHunt them down before they strike the column, then return to your commander within {_serviceRecord.ActiveDutyMissionExpiresDay - (int)CampaignTime.Now.ToDays} days."));
        return true;
    }

    private bool StartDeserterSweepDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Clan? banditClan = GetBanditClanForDutyMission();
        if (banditClan == null)
        {
            return false;
        }

        CampaignVec2 spawnPosition = FindDutyMissionSpawnPosition(mainParty);
        string partyId = $"rf_enlistment_deserters_{CampaignTime.Now.GetHashCode():x}_{MBRandom.RandomInt(1000, 9999)}";
        MobileParty targetParty = BanditPartyComponent.CreateLooterParty(
            partyId,
            banditClan,
            anchorSettlement,
            isBossParty: false,
            banditClan.DefaultPartyTemplate,
            spawnPosition);

        targetParty.SetMoveEngageParty(mainParty, mainParty.NavigationCapability);

        _serviceRecord.ActiveDutyMissionType = DutyMissionDeserterSweep;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 6 : 5);
        _serviceRecord.ActiveDutyMissionTargetPartyId = targetParty.StringId;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = anchorSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"A knot of armed deserters has been marked near {anchorSettlement.Name}.\n\nRun them down before they spread panic through the countryside, then return to your commander within {_serviceRecord.ActiveDutyMissionExpiresDay - (int)CampaignTime.Now.ToDays} days."));
        return true;
    }

    private bool StartHideoutStrikeDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? hideoutSettlement = FindNearbyHideoutDutySettlement(mainParty, anchorSettlement);
        if (hideoutSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionHideoutStrike;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 7 : 6);
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = hideoutSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;
        EnsureDutyMissionTargetVisibility();

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"A raider hideout near {GetHideoutDutyReferenceName(hideoutSettlement, anchorSettlement)} has been feeding attacks against the host.\n\nFind it, clear it, and return to your commander within {_serviceRecord.ActiveDutyMissionExpiresDay - (int)CampaignTime.Now.ToDays} days."));
        return true;
    }

    private bool StartRoadPatrolDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? patrolSettlement = FindNearbyDutySettlement(mainParty, anchorSettlement);
        if (patrolSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionRoadPatrol;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 5 : 4);
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = patrolSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"You have been ordered to secure the road toward {patrolSettlement.Name}.\n\nReach the area within 4 days, make certain the route is safe, then return to your commander with a report."));
        return true;
    }

    private bool StartScoutRouteDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? scoutSettlement = FindNearbyDutySettlement(mainParty, anchorSettlement);
        if (scoutSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionScoutRoute;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 5 : 4);
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = scoutSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"Ride ahead toward {scoutSettlement.Name} and judge the route before the host commits to it.\n\nReturn within 4 days with a clear report on the road."));
        return true;
    }

    private bool StartSupplyDeliveryDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? deliverySettlement = FindNearbyDutySettlement(mainParty, anchorSettlement);
        if (deliverySettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionSupplyDelivery;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + (_serviceRecord.CommanderTrust >= 10 ? 6 : 5);
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = deliverySettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = GetRequiredSupplyCount();

        string body = CommanderNeedsSupplies()
            ? $"The host is short on stores. Ride to {deliverySettlement.Name}, obtain {_serviceRecord.ActiveDutyMissionRequiredSupplyCount} food units, hand them over there, and return within 5 days."
            : $"You have been assigned to carry supply orders to {deliverySettlement.Name}. Arrive with {_serviceRecord.ActiveDutyMissionRequiredSupplyCount} food units within 5 days, hand them over, then return to your commander.";

        ShowDutyPopup("Duty Accepted", BuildCommanderBriefing(body));
        return true;
    }

    private bool StartForageDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? forageSettlement = FindNearbyForageSettlement(mainParty, anchorSettlement);
        if (forageSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionForage;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + 4;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = forageSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"You have been sent to forage near {forageSettlement.Name}.\n\nGather what food you can there, then return within 4 days with the result."));
        return true;
    }

    private bool StartRecruitmentErrandDutyMission(MobileParty mainParty, Settlement anchorSettlement)
    {
        Settlement? recruitmentSettlement = FindRecruitmentDutySettlement(mainParty, anchorSettlement);
        if (recruitmentSettlement == null)
        {
            return false;
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionRecruitmentErrand;
        _serviceRecord.ActiveDutyMissionExpiresDay = (int)CampaignTime.Now.ToDays + 5;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = recruitmentSettlement.StringId;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;

        ShowDutyPopup(
            "Duty Accepted",
            BuildCommanderBriefing($"Your commander has sent you toward {recruitmentSettlement.Name}.\n\nSpeak for the host, gather what fresh men and promises you can there, and return within 5 days with your report."));
        return true;
    }

    private void UpdateActiveDutyMission()
    {
        if (!HasActiveDutyMission())
        {
            return;
        }

        if (_serviceRecord.ActiveDutyMissionType == DutyMissionServiceShift)
        {
            return;
        }

        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.ActiveDutyMissionExpiresDay >= 0 && today > _serviceRecord.ActiveDutyMissionExpiresDay)
        {
            FailDutyMission("You ran out of time and the target slipped away.");
            return;
        }

        if (_serviceRecord.ActiveDutyMissionCompleted)
        {
            return;
        }

        if (_serviceRecord.ActiveDutyMissionType == DutyMissionScoutRoute
            && TryCompleteScoutRouteFromEnemyContact())
        {
            return;
        }

        if (_serviceRecord.ActiveDutyMissionType == DutyMissionRoadPatrol
            || _serviceRecord.ActiveDutyMissionType == DutyMissionScoutRoute
            || _serviceRecord.ActiveDutyMissionType == DutyMissionSupplyDelivery
            || _serviceRecord.ActiveDutyMissionType == DutyMissionForage
            || _serviceRecord.ActiveDutyMissionType == DutyMissionRecruitmentErrand
            || _serviceRecord.ActiveDutyMissionType == DutyMissionHideoutStrike
            || _serviceRecord.ActiveDutyMissionType == DutyMissionTrustedDispatch
            || _serviceRecord.ActiveDutyMissionType == DutyMissionReliefDispatch)
        {
            Settlement? targetSettlement = FindDutyMissionSettlement();
            if (targetSettlement == null)
            {
                FailDutyMission("The duty route could no longer be confirmed.");
            }

            return;
        }

        MobileParty? targetParty = FindActiveDutyTargetParty();
        if (targetParty == null || !targetParty.IsActive)
        {
            FailDutyMission(_serviceRecord.ActiveDutyMissionType == DutyMissionMountedPursuit
                ? "The mounted target slipped away before you could finish the pursuit."
                : "The scouting trail went cold before the duty could be finished.");
        }
    }

    private bool TryCompleteScoutRouteFromEnemyContact()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        MobileParty? spottedEnemy = FindScoutRouteSpottedEnemy(mainParty);
        if (mainParty == null || spottedEnemy == null)
        {
            return false;
        }

        Settlement? targetSettlement = FindDutyMissionSettlement();
        string routeText = targetSettlement != null
            ? $"while scouting toward {targetSettlement.Name}"
            : "while scouting ahead of the host";
        string enemyText = spottedEnemy.Army?.LeaderParty?.Name?.ToString()
            ?? spottedEnemy.Name?.ToString()
            ?? "an enemy force";

        CompleteDutyMission($"You sighted {enemyText} {routeText} and returned with a live report on enemy movement.");
        return true;
    }

    private void CompleteDutyMission(string resultText)
    {
        _serviceRecord.ActiveDutyMissionCompleted = true;
        _serviceRecord.ActiveDutyMissionOutcomeText = resultText;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        RecordServiceIncident(GetDutyMissionLabel(), resultText, popup: false);

        switch (_serviceRecord.ActiveDutyMissionType)
        {
            case DutyMissionReliefDispatch:
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                break;
            case DutyMissionTrustedDispatch:
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 4f);
                break;
            case DutyMissionSupplyDelivery:
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 5f);
                break;
            case DutyMissionRoadPatrol:
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 6f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 6f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 4f);
                break;
            case DutyMissionScoutRoute:
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 6f);
                break;
            case DutyMissionForage:
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 6f);
                break;
            case DutyMissionRecruitmentErrand:
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 6f);
                break;
            case DutyMissionMountedPursuit:
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 6f);
                break;
            case DutyMissionDeserterSweep:
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 8f);
                break;
            case DutyMissionHideoutStrike:
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 10f);
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 5f);
                break;
        }

        ShowDutyPopup("Duty Complete", $"{resultText}\n\nReturn to your commander to make your report.");
    }

    private void TryAdvanceServiceShiftDuty()
    {
        if (_serviceRecord.ActiveDutyMissionType != DutyMissionServiceShift
            || _serviceRecord.ActiveDutyMissionCompleted
            || _serviceRecord.ActiveDutyMissionDueTime == CampaignTime.Never
            || CampaignTime.Now < _serviceRecord.ActiveDutyMissionDueTime)
        {
            return;
        }

        ResolveServiceDutyEvent();
        ClearDutyMission(destroyTargetParty: false);
    }

    private void ReportDutyMissionToCommander()
    {
        MarkCommanderAttachmentPendingFromConversation();
        Hero? commander = ResolveCommander();
        int missionType = _serviceRecord.ActiveDutyMissionType;
        int serviceXp = 45;
        int gold = 50;

        ApplyDutyMissionReportRewards(missionType, ref serviceXp, ref gold);
        _serviceRecord.ServiceXp += serviceXp;
        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
        _serviceRecord.TotalWagesPaid += gold;

        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, 2, true, true);
        }

        _serviceRecord.DetachedDutyCount++;
        RegisterDutySuccess(2);
        string resultText = string.IsNullOrWhiteSpace(_serviceRecord.ActiveDutyMissionOutcomeText)
            ? GetDefaultDutyMissionCompletionText(missionType)
            : _serviceRecord.ActiveDutyMissionOutcomeText;

        ApplyDutyMissionReputation(missionType);
        RecordServiceIncident("Duty Report", resultText, popup: false);

        ClearDutyMission(destroyTargetParty: false);
        TryPromote();
        ShowDutyPopup("Duty Report", $"{resultText}\n\nYour commander accepts the report.\n\nYou gained {serviceXp} service XP and {gold} gold.");
    }

    private void CancelDutyMissionByCommander()
    {
        MarkCommanderAttachmentPendingFromConversation();
        Hero? commander = ResolveCommander();
        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, -1, true, true);
        }

        RegisterDutyFailure();
        RecordServiceIncident("Duty Cancelled", "Your commander cancelled the assignment and marked the failure against your record.", popup: false);
        ClearDutyMission(destroyTargetParty: true);
        ShowDutyPopup("Duty Cancelled", "Your commander cancels the patrol mission and notes the failure.");
    }

    private void FailDutyMission(string resultText)
    {
        Hero? commander = ResolveCommander();
        if (commander != null)
        {
            ChangeRelationAction.ApplyPlayerRelation(commander, -1, true, true);
        }

        RegisterDutyFailure();
        RecordServiceIncident("Duty Failed", resultText, popup: false);
        ClearDutyMission(destroyTargetParty: true);
        ShowDutyPopup("Duty Failed", resultText);
    }

    private bool HasActiveDutyMission()
    {
        return _serviceRecord.ActiveDutyMissionType != DutyMissionNone;
    }

    private bool IsDetachedDutyMissionInProgress()
    {
        return HasActiveDutyMission()
            && _serviceRecord.ActiveDutyMissionType != DutyMissionServiceShift
            && !_serviceRecord.ActiveDutyMissionCompleted;
    }

    private void EnsureDutyMissionTargetVisibility()
    {
        if (_serviceRecord.ActiveDutyMissionType != DutyMissionHideoutStrike)
        {
            return;
        }

        Settlement? targetSettlement = FindDutyMissionSettlement();
        if (targetSettlement == null)
        {
            return;
        }

        targetSettlement.IsVisible = true;
    }

    private MobileParty? FindActiveDutyTargetParty()
    {
        if (string.IsNullOrWhiteSpace(_serviceRecord.ActiveDutyMissionTargetPartyId))
        {
            return null;
        }

        return MobileParty.All.FirstOrDefault(party => party.StringId == _serviceRecord.ActiveDutyMissionTargetPartyId);
    }

    private void ClearDutyMission(bool destroyTargetParty)
    {
        MobileParty? targetParty = destroyTargetParty ? FindActiveDutyTargetParty() : null;
        if (destroyTargetParty && targetParty != null && targetParty.IsActive && targetParty.MapEvent == null)
        {
            DestroyPartyAction.Apply(null, targetParty);
        }

        _serviceRecord.ActiveDutyMissionType = DutyMissionNone;
        _serviceRecord.ActiveDutyMissionExpiresDay = -1;
        _serviceRecord.ActiveDutyMissionTargetPartyId = string.Empty;
        _serviceRecord.ActiveDutyMissionTargetSettlementId = string.Empty;
        _serviceRecord.ActiveDutyMissionCompleted = false;
        _serviceRecord.ActiveDutyMissionOutcomeText = string.Empty;
        _serviceRecord.ActiveDutyMissionDueTime = CampaignTime.Never;
        _serviceRecord.ActiveDutyMissionRequiredSupplyCount = 0;
    }

    private string GetActiveDutyMissionStatusText()
    {
        if (!HasActiveDutyMission())
        {
            return "none";
        }

        if (_serviceRecord.ActiveDutyMissionType == DutyMissionServiceShift)
        {
            float hoursLeft = Math.Max(0f, (float)(_serviceRecord.ActiveDutyMissionDueTime.ToHours - CampaignTime.Now.ToHours));
            return _serviceRecord.ActiveDutyMissionCompleted
                ? "service shift completed"
                : $"service shift in progress ({Math.Ceiling(hoursLeft)} hours left)";
        }

        Settlement? targetSettlement = FindDutyMissionSettlement();
        string targetName = _serviceRecord.ActiveDutyMissionType == DutyMissionHideoutStrike
            ? GetHideoutDutyReferenceName(targetSettlement)
            : targetSettlement?.Name?.ToString() ?? "unknown area";
        int daysLeft = Math.Max(0, _serviceRecord.ActiveDutyMissionExpiresDay - (int)CampaignTime.Now.ToDays);
        if (_serviceRecord.ActiveDutyMissionType == DutyMissionSupplyDelivery && !_serviceRecord.ActiveDutyMissionCompleted)
        {
            return $"{GetDutyMissionLabel()} to {targetName} ({CountFoodUnitsInMainParty()}/{_serviceRecord.ActiveDutyMissionRequiredSupplyCount} food, {daysLeft} days left)";
        }

        return _serviceRecord.ActiveDutyMissionCompleted
            ? $"{GetDutyMissionLabel()} near {targetName} completed, awaiting report"
            : $"{GetDutyMissionLabel()} near {targetName} ({daysLeft} days left)";
    }

    private Settlement? FindDutyMissionSettlement()
    {
        if (string.IsNullOrWhiteSpace(_serviceRecord.ActiveDutyMissionTargetSettlementId))
        {
            return null;
        }

        return Settlement.Find(_serviceRecord.ActiveDutyMissionTargetSettlementId);
    }

    private void TryCompleteSettlementDutyMission(Settlement settlement)
    {
        if (!HasActiveDutyMission()
            || _serviceRecord.ActiveDutyMissionCompleted
            || settlement == null)
        {
            return;
        }

        Settlement? targetSettlement = FindDutyMissionSettlement();
        if (targetSettlement == null || settlement != targetSettlement)
        {
            return;
        }

        if (_serviceRecord.ActiveDutyMissionType == DutyMissionRoadPatrol)
        {
            CompleteDutyMission($"You patrolled the road near {settlement.Name} and restored order in the area.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionScoutRoute)
        {
            CompleteDutyMission($"You scouted the route near {settlement.Name} and returned with a clear report for the column.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionReliefDispatch)
        {
            CompleteDutyMission($"You delivered the relief appeal at {settlement.Name} before the line could close.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionTrustedDispatch)
        {
            CompleteDutyMission((_serviceRecord.InCommanderSiege || _serviceRecord.InCommanderBlockade)
                ? $"You delivered the urgent appeal at {settlement.Name} and secured a formal promise of relief."
                : $"You delivered the sealed orders to {settlement.Name} and received the local commander's acknowledgement.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionSupplyDelivery)
        {
            int requiredFood = Math.Max(1, _serviceRecord.ActiveDutyMissionRequiredSupplyCount);
            int carriedFood = CountFoodUnitsInMainParty();
            if (carriedFood < requiredFood)
            {
                ShowDutyPopup(
                    "Supply Shortfall",
                    $"The officers at {settlement.Name} are ready to release stores, but you still need {requiredFood} food units in your party to complete the transfer.\n\nCurrent carried food: {carriedFood}.");
                return;
            }

            ConsumeFoodUnitsFromMainParty(requiredFood);
                CompleteDutyMission($"You delivered {requiredFood} food units at {settlement.Name} and secured what the host needed.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionForage)
        {
            int forageFood = 6 + MBRandom.RandomInt(4, 10);
            AddFoodUnitsToMainParty(forageFood);
            CompleteDutyMission($"You foraged {forageFood} food units around {settlement.Name} and packed them for the host.");
        }
        else if (_serviceRecord.ActiveDutyMissionType == DutyMissionRecruitmentErrand)
        {
            CompleteDutyMission($"You gathered willing recruits and levy promises around {settlement.Name}, then carried the count back to the host.");
        }
    }

    private void ApplyDutyMissionReportRewards(int missionType, ref int serviceXp, ref int gold)
    {
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);

        switch (missionType)
        {
            case DutyMissionReliefDispatch:
                serviceXp = 68;
                gold = 85;
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                break;
            case DutyMissionTrustedDispatch:
                serviceXp = 62;
                gold = 75;
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 14f);
                break;
            case DutyMissionSupplyDelivery:
                serviceXp = 50;
                gold = 45;
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 15f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 8f);
                break;
            case DutyMissionForage:
                serviceXp = 46;
                gold = 42;
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 8f);
                break;
            case DutyMissionRecruitmentErrand:
                serviceXp = 48;
                gold = 48;
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 8f);
                break;
            case DutyMissionBanditHunt:
                serviceXp = 54;
                gold = 60;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 12f);
                break;
            case DutyMissionRoadPatrol:
                serviceXp = 48;
                gold = 55;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 10f);
                break;
            case DutyMissionScoutRoute:
                serviceXp = 58;
                gold = 62;
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 10f);
                break;
            case DutyMissionMountedPursuit:
                serviceXp = 55;
                gold = 65;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 12f);
                break;
            case DutyMissionDeserterSweep:
                serviceXp = 60;
                gold = 68;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 12f);
                break;
            case DutyMissionHideoutStrike:
                serviceXp = 72;
                gold = 90;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 14f);
                break;
            default:
                serviceXp = 45;
                gold = 60;
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 15f);

                if (_serviceRecord.Assignment == RFEnlistmentAssignment.Archer)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 12f);
                }

                break;
        }

        ApplyCompanionDutySupport(missionType, ref serviceXp, ref gold);
    }

    private string GetDefaultDutyMissionCompletionText(int missionType)
    {
        return missionType switch
        {
            DutyMissionReliefDispatch => "You completed the relief dispatch assignment.",
            DutyMissionTrustedDispatch => "You completed the trusted dispatch assignment.",
            DutyMissionSupplyDelivery => "You completed the supply delivery assignment.",
            DutyMissionForage => "You completed the forage assignment.",
            DutyMissionRecruitmentErrand => "You completed the recruitment errand.",
            DutyMissionBanditHunt => "You completed the bandit hunt assignment.",
            DutyMissionDeserterSweep => "You completed the deserter sweep assignment.",
            DutyMissionHideoutStrike => "You completed the hideout strike assignment.",
            DutyMissionRoadPatrol => "You completed the road patrol assignment.",
            DutyMissionScoutRoute => "You completed the scout route assignment.",
            DutyMissionMountedPursuit => "You completed the mounted pursuit assignment.",
            _ => "You completed the scouting assignment."
        };
    }

    private void ApplyDutyMissionReputation(int missionType)
    {
        switch (missionType)
        {
            case DutyMissionSupplyDelivery:
            case DutyMissionForage:
            case DutyMissionRecruitmentErrand:
                AdjustCommanderReputation(logistics: 2);
                break;
            case DutyMissionReliefDispatch:
            case DutyMissionTrustedDispatch:
                AdjustCommanderReputation(command: 1, siege: 2);
                break;
            case DutyMissionRoadPatrol:
            case DutyMissionBanditHunt:
            case DutyMissionDeserterSweep:
            case DutyMissionMountedPursuit:
            case DutyMissionScoutRoute:
            case DutyMissionReconSweep:
            case DutyMissionHideoutStrike:
                AdjustCommanderReputation(field: 2);
                break;
        }
    }

    private bool ShouldOfferTrustedDispatch()
    {
        return _serviceRecord.CommanderTrust >= 15
            && _serviceRecord.DutySuccesses >= 3
            && _serviceRecord.DutyFailures <= _serviceRecord.DutySuccesses / 2
            && !CommanderNeedsSupplies();
    }

    private RFEnlistmentAssignment GetDutyAssignmentForCurrentOffer()
    {
        if (_serviceRecord.InCommanderSiege || _serviceRecord.InCommanderBlockade)
        {
            return RFEnlistmentAssignment.Support;
        }

        if (_serviceRecord.InCommanderNavalService)
        {
            return _serviceRecord.Assignment == RFEnlistmentAssignment.Support
                ? RFEnlistmentAssignment.Support
                : RFEnlistmentAssignment.Cavalry;
        }

        if (_serviceRecord.CommanderTrust <= -5)
        {
            return _serviceRecord.Assignment == RFEnlistmentAssignment.Support
                ? RFEnlistmentAssignment.Support
                : RFEnlistmentAssignment.Infantry;
        }

        if (_serviceRecord.CommanderTrust < 10)
        {
            return _serviceRecord.Assignment;
        }

        string profile = GetDominantServiceProfile();
        if (profile == "siege hand")
        {
            return RFEnlistmentAssignment.Support;
        }

        if (profile == "fleet veteran")
        {
            return RFEnlistmentAssignment.Cavalry;
        }

        if (profile == "field scout" || profile == "scout rider")
        {
            return RFEnlistmentAssignment.Archer;
        }

        return _serviceRecord.Assignment;
    }

    private int GetPreferredFieldDutyMissionType()
    {
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        List<int> candidates = new();
        MobileParty? mainParty = MobileParty.MainParty;
        Settlement? anchorSettlement = GetMissionAnchorSettlement();

        if (CanStartReliefDispatchDuty())
        {
            AddDutyCandidate(candidates, DutyMissionReliefDispatch);
        }

        if (ShouldOfferTrustedDispatch())
        {
            AddDutyCandidate(candidates, DutyMissionTrustedDispatch);
        }

        if (CommanderNeedsSupplies())
        {
            AddDutyCandidate(candidates, DutyMissionSupplyDelivery);
        }

        RFEnlistmentAssignment dutyAssignment = GetDutyAssignmentForCurrentOffer();
        bool hasBanditTarget = GetBanditClanForDutyMission() != null;
        bool hasRecruitmentErrand = mainParty != null
            && anchorSettlement != null
            && FindRecruitmentDutySettlement(mainParty, anchorSettlement) != null;
        bool hasHideoutStrike = FindNearbyHideoutDutySettlement(mainParty, anchorSettlement) != null;

        if (rhythmState.LowSupplies)
        {
            AddDutyCandidate(
                candidates,
                FindNearbyForageSettlement(MobileParty.MainParty, GetMissionAnchorSettlement()) != null
                    ? DutyMissionForage
                    : DutyMissionSupplyDelivery);
        }

        if (!rhythmState.PreBattle
            && !rhythmState.ActiveCampaign
            && rhythmState.RecruitCount <= 4
            && _serviceRecord.CommanderTrust >= 0
            && hasRecruitmentErrand)
        {
            AddDutyCandidate(candidates, DutyMissionRecruitmentErrand);
        }

        if (rhythmState.PreBattle)
        {
            AddDutyCandidate(candidates, dutyAssignment switch
            {
                RFEnlistmentAssignment.Archer => DutyMissionScoutRoute,
                RFEnlistmentAssignment.Cavalry => DutyMissionMountedPursuit,
                RFEnlistmentAssignment.Infantry when hasBanditTarget && _serviceRecord.Rank >= RFEnlistmentRank.Soldier => DutyMissionBanditHunt,
                _ => DutyMissionRoadPatrol
            });
        }

        if (rhythmState.HighScrutiny && dutyAssignment == RFEnlistmentAssignment.Infantry)
        {
            AddDutyCandidate(candidates, DutyMissionRoadPatrol);
        }

        if (_serviceRecord.Rank >= RFEnlistmentRank.Sergeant
            && _serviceRecord.CommanderTrust >= 4
            && hasHideoutStrike
            && !rhythmState.LowSupplies
            && !rhythmState.SiegePressure)
        {
            AddDutyCandidate(candidates, DutyMissionHideoutStrike);
        }

        AddCareerTierFieldDutyCandidates(candidates, dutyAssignment, rhythmState, hasBanditTarget, hasRecruitmentErrand, hasHideoutStrike);

        if (_serviceRecord.CommanderTrust <= -4)
        {
            AddDutyCandidate(candidates, DutyMissionRoadPatrol);
        }

        return ChooseVariedDutyCandidate(
            candidates,
            _serviceRecord.LastFieldDutyType,
            _serviceRecord.LastFieldDutyDay,
            _serviceRecord.FieldDutyRepeatStreak,
            allowRepeat: rhythmState.PreBattle || rhythmState.SiegePressure || rhythmState.LowSupplies);
    }

    private int GetPreferredInteractiveDutyType()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || mainParty.IsCurrentlyAtSea || mainParty.MapEvent != null)
        {
            return InteractiveDutyNone;
        }

        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        if (rhythmState.NavalService)
        {
            return InteractiveDutyNone;
        }

        List<int> candidates = new();
        int officerDutyType = GetPreferredOfficerDutyType(rhythmState);
        if (officerDutyType != InteractiveDutyNone)
        {
            AddDutyCandidate(candidates, officerDutyType);
        }

        if (rhythmState.SiegePressure && rhythmState.Phase != "Night")
        {
            AddDutyCandidate(candidates, InteractiveDutyInspectDefenses);
        }

        if (rhythmState.LowSupplies && (rhythmState.QuietGarrison || rhythmState.Phase == "Dawn" || rhythmState.Phase == "Midday"))
        {
            AddDutyCandidate(candidates, InteractiveDutyQuartermasterShortage);
        }

        if (rhythmState.RecoveryState && rhythmState.WoundedCount > 0 && rhythmState.Phase != "Night")
        {
            AddDutyCandidate(candidates, InteractiveDutyTreatWounded);
        }

        if (rhythmState.QuietGarrison && rhythmState.Phase == "Night")
        {
            AddDutyCandidate(candidates, InteractiveDutyGateWatch);
        }

        if (rhythmState.Phase == "Night" && !rhythmState.QuietGarrison)
        {
            AddDutyCandidate(candidates, InteractiveDutyNightPatrol);
        }

        if (_serviceRecord.Rank >= RFEnlistmentRank.Soldier
            && rhythmState.RecruitCount >= 8
            && (rhythmState.Phase == "Dawn" || rhythmState.Phase == "Midday" || rhythmState.QuietGarrison))
        {
            AddDutyCandidate(candidates, InteractiveDutyTrainRecruits);
        }

        if (rhythmState.HighScrutiny && (rhythmState.QuietGarrison || rhythmState.Phase == "Midday" || rhythmState.Phase == "Dusk"))
        {
            AddDutyCandidate(candidates, InteractiveDutyEquipmentCheck);
        }

        if (rhythmState.WoundedCount > 0 && (rhythmState.Phase == "Midday" || rhythmState.Phase == "Dusk"))
        {
            AddDutyCandidate(candidates, InteractiveDutyTreatWounded);
        }

        if (rhythmState.Phase == "Night")
        {
            AddDutyCandidate(candidates, InteractiveDutyNightPatrol);
        }

        if (rhythmState.QuietGarrison && !rhythmState.SiegePressure)
        {
            AddDutyCandidate(candidates, InteractiveDutyEquipmentCheck);
        }

        AddCareerTierInteractiveDutyCandidates(candidates, rhythmState);

        return ChooseVariedDutyCandidate(
            candidates,
            _serviceRecord.LastInteractiveDutyType,
            _serviceRecord.LastInteractiveDutyDay,
            _serviceRecord.InteractiveDutyRepeatStreak,
            allowRepeat: rhythmState.SiegePressure || rhythmState.PreBattle);
    }

    private int GetPreferredOfficerDutyType(ArmyRhythmState rhythmState)
    {
        if (_serviceRecord.Rank < RFEnlistmentRank.Veteran || _serviceRecord.CommanderTrust < 5)
        {
            return InteractiveDutyNone;
        }

        if (_serviceRecord.Rank >= RFEnlistmentRank.Sergeant
            && rhythmState.PreBattle
            && GetCommanderDominantReputationDomain() == "field")
        {
            return InteractiveDutyLeadPatrol;
        }

        if (rhythmState.LowSupplies && GetCommanderDominantReputationDomain() == "logistics")
        {
            return InteractiveDutyCoordinateSupply;
        }

        if (rhythmState.SiegePressure && _serviceRecord.Rank >= RFEnlistmentRank.Sergeant)
        {
            return InteractiveDutyStrategicPlanning;
        }

        if (rhythmState.RecruitCount >= 8 || rhythmState.ActiveCampaign)
        {
            if (GetCommanderDominantReputationDomain() == "command" || _serviceRecord.Rank >= RFEnlistmentRank.Sergeant)
            {
                return InteractiveDutyCommandSquad;
            }
        }

        return InteractiveDutyNone;
    }

    private void AddCareerTierFieldDutyCandidates(
        List<int> candidates,
        RFEnlistmentAssignment dutyAssignment,
        ArmyRhythmState rhythmState,
        bool hasBanditTarget,
        bool hasRecruitmentErrand,
        bool hasHideoutStrike)
    {
        List<int> pool = new();

        switch (_serviceRecord.Rank)
        {
            case RFEnlistmentRank.Recruit:
                AddAssignmentFieldDutyPool(pool, dutyAssignment, allowCombatDuties: false);
                if (hasRecruitmentErrand && rhythmState.RecruitCount <= 6)
                {
                    AddDutyCandidate(pool, DutyMissionRecruitmentErrand);
                }

                break;
            case RFEnlistmentRank.Soldier:
                AddAssignmentFieldDutyPool(pool, dutyAssignment, allowCombatDuties: hasBanditTarget);
                if (hasBanditTarget && dutyAssignment != RFEnlistmentAssignment.Support)
                {
                    AddDutyCandidate(pool, DutyMissionBanditHunt);
                }

                if (hasRecruitmentErrand && !rhythmState.ActiveCampaign)
                {
                    AddDutyCandidate(pool, DutyMissionRecruitmentErrand);
                }

                break;
            case RFEnlistmentRank.Veteran:
                AddAssignmentFieldDutyPool(pool, dutyAssignment, allowCombatDuties: hasBanditTarget);
                if (hasBanditTarget)
                {
                    AddDutyCandidate(pool, DutyMissionBanditHunt);
                    AddDutyCandidate(pool, DutyMissionDeserterSweep);
                }

                if (ShouldOfferTrustedDispatch())
                {
                    AddDutyCandidate(pool, DutyMissionTrustedDispatch);
                }

                break;
            default:
                AddAssignmentFieldDutyPool(pool, dutyAssignment, allowCombatDuties: hasBanditTarget);
                if (hasHideoutStrike)
                {
                    AddDutyCandidate(pool, DutyMissionHideoutStrike);
                }

                if (hasBanditTarget)
                {
                    AddDutyCandidate(pool, DutyMissionDeserterSweep);
                    AddDutyCandidate(pool, DutyMissionBanditHunt);
                }

                if (ShouldOfferTrustedDispatch())
                {
                    AddDutyCandidate(pool, DutyMissionTrustedDispatch);
                }

                break;
        }

        AddRotatedDutyPool(candidates, pool);
    }

    private void AddAssignmentFieldDutyPool(List<int> pool, RFEnlistmentAssignment dutyAssignment, bool allowCombatDuties)
    {
        switch (dutyAssignment)
        {
            case RFEnlistmentAssignment.Support:
                AddDutyCandidate(pool, DutyMissionSupplyDelivery);
                AddDutyCandidate(pool, DutyMissionForage);
                AddDutyCandidate(pool, DutyMissionRecruitmentErrand);
                AddDutyCandidate(pool, DutyMissionRoadPatrol);
                break;
            case RFEnlistmentAssignment.Cavalry:
                AddDutyCandidate(pool, DutyMissionMountedPursuit);
                AddDutyCandidate(pool, DutyMissionScoutRoute);
                AddDutyCandidate(pool, DutyMissionRoadPatrol);
                if (allowCombatDuties)
                {
                    AddDutyCandidate(pool, DutyMissionDeserterSweep);
                }

                break;
            case RFEnlistmentAssignment.Archer:
                AddDutyCandidate(pool, DutyMissionScoutRoute);
                AddDutyCandidate(pool, DutyMissionReconSweep);
                AddDutyCandidate(pool, DutyMissionRoadPatrol);
                if (allowCombatDuties)
                {
                    AddDutyCandidate(pool, DutyMissionBanditHunt);
                }

                break;
            default:
                AddDutyCandidate(pool, DutyMissionRoadPatrol);
                AddDutyCandidate(pool, DutyMissionScoutRoute);
                if (allowCombatDuties)
                {
                    AddDutyCandidate(pool, DutyMissionBanditHunt);
                    AddDutyCandidate(pool, DutyMissionDeserterSweep);
                }

                break;
        }
    }

    private void AddCareerTierInteractiveDutyCandidates(List<int> candidates, ArmyRhythmState rhythmState)
    {
        List<int> pool = new();

        switch (_serviceRecord.Rank)
        {
            case RFEnlistmentRank.Recruit:
                AddDutyCandidate(pool, InteractiveDutyEquipmentCheck);
                AddDutyCandidate(pool, InteractiveDutyNightPatrol);
                AddDutyCandidate(pool, InteractiveDutyGateWatch);
                break;
            case RFEnlistmentRank.Soldier:
                AddDutyCandidate(pool, InteractiveDutyNightPatrol);
                AddDutyCandidate(pool, InteractiveDutyEquipmentCheck);
                AddDutyCandidate(pool, InteractiveDutyTreatWounded);
                if (rhythmState.RecruitCount >= 6)
                {
                    AddDutyCandidate(pool, InteractiveDutyTrainRecruits);
                }

                break;
            case RFEnlistmentRank.Veteran:
                AddDutyCandidate(pool, InteractiveDutyNightPatrol);
                AddDutyCandidate(pool, InteractiveDutyTreatWounded);
                AddDutyCandidate(pool, InteractiveDutyTrainRecruits);
                AddDutyCandidate(pool, InteractiveDutyQuartermasterShortage);
                break;
            default:
                AddDutyCandidate(pool, InteractiveDutyTrainRecruits);
                AddDutyCandidate(pool, InteractiveDutyLeadPatrol);
                AddDutyCandidate(pool, InteractiveDutyCoordinateSupply);
                AddDutyCandidate(pool, InteractiveDutyCommandSquad);
                break;
        }

        AddRotatedDutyPool(candidates, pool);
    }

    private static void AddDutyCandidate(List<int> candidates, int dutyType)
    {
        if (dutyType != 0 && !candidates.Contains(dutyType))
        {
            candidates.Add(dutyType);
        }
    }

    private void AddRotatedDutyPool(List<int> candidates, List<int> pool)
    {
        if (pool.Count == 0)
        {
            return;
        }

        int seed = Math.Abs(
            (int)CampaignTime.Now.ToDays
            + _serviceRecord.DaysServed
            + ((int)_serviceRecord.Rank * 3)
            + ((int)_serviceRecord.Assignment * 5));
        int start = seed % pool.Count;
        for (int i = 0; i < pool.Count; i++)
        {
            AddDutyCandidate(candidates, pool[(start + i) % pool.Count]);
        }
    }

    private static int ChooseVariedDutyCandidate(
        List<int> candidates,
        int lastType,
        int lastDay,
        int repeatStreak,
        bool allowRepeat)
    {
        if (candidates.Count == 0)
        {
            return 0;
        }

        int today = (int)CampaignTime.Now.ToDays;
        bool repeatedVeryRecently = lastType != 0 && today - lastDay <= 5;
        if (allowRepeat || !repeatedVeryRecently || repeatStreak <= 0)
        {
            return candidates[0];
        }

        foreach (int candidate in candidates)
        {
            if (candidate != lastType)
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private int GetPreferredIncidentType(ArmyRhythmState rhythmState)
    {
        List<int> candidates = new();
        if (_serviceRecord.DeferredWageAmount > 0)
        {
            AddDutyCandidate(candidates, IncidentPayDelay);
        }

        if (rhythmState.LowSupplies)
        {
            AddDutyCandidate(candidates, IncidentShortRations);
        }

        if (rhythmState.RecoveryState || rhythmState.HighScrutiny || rhythmState.ActiveCampaign)
        {
            AddDutyCandidate(candidates, IncidentCampDiscipline);
        }

        return ChooseVariedDutyCandidate(
            candidates,
            _serviceRecord.LastIncidentType,
            _serviceRecord.LastIncidentDay,
            _serviceRecord.IncidentRepeatStreak,
            allowRepeat: false);
    }

    private void NoteInteractiveDutyStarted(int dutyType)
    {
        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.LastInteractiveDutyType == dutyType && _serviceRecord.LastInteractiveDutyDay == today)
        {
            _serviceRecord.InteractiveDutyRepeatStreak++;
        }
        else if (_serviceRecord.LastInteractiveDutyType == dutyType && today - _serviceRecord.LastInteractiveDutyDay <= 5)
        {
            _serviceRecord.InteractiveDutyRepeatStreak = Math.Max(1, _serviceRecord.InteractiveDutyRepeatStreak + 1);
        }
        else
        {
            _serviceRecord.InteractiveDutyRepeatStreak = 0;
        }

        _serviceRecord.LastInteractiveDutyType = dutyType;
        _serviceRecord.LastInteractiveDutyDay = today;
    }

    private void NoteFieldDutyStarted(int dutyType)
    {
        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.LastFieldDutyType == dutyType && _serviceRecord.LastFieldDutyDay == today)
        {
            _serviceRecord.FieldDutyRepeatStreak++;
        }
        else if (_serviceRecord.LastFieldDutyType == dutyType && today - _serviceRecord.LastFieldDutyDay <= 5)
        {
            _serviceRecord.FieldDutyRepeatStreak = Math.Max(1, _serviceRecord.FieldDutyRepeatStreak + 1);
        }
        else
        {
            _serviceRecord.FieldDutyRepeatStreak = 0;
        }

        _serviceRecord.LastFieldDutyType = dutyType;
        _serviceRecord.LastFieldDutyDay = today;
    }

    private void NoteIncidentShown(int incidentType)
    {
        int today = (int)CampaignTime.Now.ToDays;
        if (_serviceRecord.LastIncidentType == incidentType && _serviceRecord.LastIncidentDay == today)
        {
            _serviceRecord.IncidentRepeatStreak++;
        }
        else if (_serviceRecord.LastIncidentType == incidentType && today - _serviceRecord.LastIncidentDay <= 5)
        {
            _serviceRecord.IncidentRepeatStreak = Math.Max(1, _serviceRecord.IncidentRepeatStreak + 1);
        }
        else
        {
            _serviceRecord.IncidentRepeatStreak = 0;
        }

        _serviceRecord.LastIncidentType = incidentType;
        _serviceRecord.LastIncidentDay = today;
    }

    private string GetCommanderDutyLeadText()
    {
        string commanderStyle = GetCommanderServiceStyle();
        ArmyRhythmState rhythmState = GetCurrentArmyRhythmState();
        string rhythmTail = rhythmState.Context switch
        {
            "pre-battle" => " The whole host is tightening for battle.",
            "recovery march" => " The army is carrying too many wounded to waste men carelessly.",
            "quiet garrison" => " The host is in a quieter holding pattern for now.",
            "active campaign" => " The army is moving in active war country.",
            "routine march" => " The column is keeping to the road and watching for trouble.",
            _ => string.Empty
        };

        if (_serviceRecord.InCommanderSiege)
        {
            return $"Your {commanderStyle} commander is driving the army through siege work and wants dependable hands.";
        }

        if (_serviceRecord.InCommanderBlockade)
        {
            return $"Your {commanderStyle} commander is tightening a blockade and needs disciplined service.";
        }

        if (_serviceRecord.InCommanderNavalService)
        {
            return $"Your {commanderStyle} commander is working the fleet hard and expects alert service.";
        }

        if (_serviceRecord.InCommanderArmy)
        {
            return $"Your {commanderStyle} commander is pushing the march and watching who can be relied upon.{rhythmTail}";
        }

        if (_serviceRecord.CommanderTrust >= 15)
        {
            return $"Your commander is beginning to reserve sensitive work for you.{rhythmTail}";
        }

        if (_serviceRecord.CommanderTrust >= 10)
        {
            return "Your commander now expects more from you.";
        }

        if (_serviceRecord.CommanderTrust <= -5)
        {
            return "Your commander is putting you on plain, closely watched duty.";
        }

        return "A service task has come down through the chain of command.";
    }

    private string GetCommanderServiceStyle()
    {
        Hero? commander = ResolveCommander();
        if (commander == null)
        {
            return "field";
        }

        int tactics = commander.GetSkillValue(DefaultSkills.Tactics);
        int leadership = commander.GetSkillValue(DefaultSkills.Leadership);
        int scouting = commander.GetSkillValue(DefaultSkills.Scouting);
        int engineering = commander.GetSkillValue(DefaultSkills.Engineering);

        if (engineering >= tactics && engineering >= scouting && engineering >= 80)
        {
            return "methodical";
        }

        if (scouting >= tactics && scouting >= leadership && scouting >= 80)
        {
            return "watchful";
        }

        if (leadership >= tactics && leadership >= 100)
        {
            return "demanding";
        }

        return tactics >= 90 ? "battlewise" : "field";
    }

    private ArmyRhythmState GetCurrentArmyRhythmState()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        MobileParty? serviceParty = commanderParty ?? mainParty;
        ArmyRhythmState rhythmState = new()
        {
            Phase = GetCurrentServicePhase(),
            NavalService = _serviceRecord.InCommanderNavalService || serviceParty?.IsCurrentlyAtSea == true,
            SiegePressure = _serviceRecord.InCommanderSiege || _serviceRecord.InCommanderBlockade,
            LowSupplies = CommanderNeedsSupplies(),
            WoundedCount = GetWoundedCountInServiceHost(),
            RecruitCount = GetRecruitCountInServiceHost()
        };

        int troopCount = serviceParty?.MemberRoster?.TotalManCount ?? 0;
        rhythmState.RecoveryState = rhythmState.WoundedCount >= Math.Max(6, troopCount / 6);
        rhythmState.HighScrutiny = _serviceRecord.CommanderTrust <= -4 || _serviceRecord.DutyFailures > _serviceRecord.DutySuccesses;

        bool inSettlement = mainParty?.CurrentSettlement != null || commanderParty?.CurrentSettlement != null;
        rhythmState.QuietGarrison = inSettlement && !rhythmState.SiegePressure && !rhythmState.NavalService;
        rhythmState.Marching = _serviceRecord.InCommanderArmy && !rhythmState.QuietGarrison && !rhythmState.SiegePressure && !rhythmState.NavalService;
        rhythmState.PreBattle = HasNearbyEnemyPressure(serviceParty);

        bool activeWar = serviceParty?.MapFaction != null
            && MobileParty.All.Any(party =>
                party != null
                && party.IsActive
                && party != serviceParty
                && party.MapFaction != null
                && party.MapFaction != serviceParty.MapFaction
                && party.MapFaction.IsAtWarWith(serviceParty.MapFaction));

        rhythmState.ActiveCampaign = rhythmState.Marching && (rhythmState.PreBattle || activeWar);

        rhythmState.Context = rhythmState.NavalService
            ? "naval service"
            : rhythmState.SiegePressure
                ? "siege line"
                : rhythmState.PreBattle
                    ? "pre-battle"
                    : rhythmState.QuietGarrison
                        ? "quiet garrison"
                        : rhythmState.RecoveryState
                            ? "recovery march"
                            : rhythmState.ActiveCampaign
                                ? "active campaign"
                                : rhythmState.Marching
                                    ? "routine march"
                                    : "field service";

        return rhythmState;
    }

    private static string GetCurrentServicePhase()
    {
        double hour = CampaignTime.Now.ToHours % 24d;
        if (hour < 0d)
        {
            hour += 24d;
        }

        if (hour < 5d)
        {
            return "Night";
        }

        if (hour < 10d)
        {
            return "Dawn";
        }

        if (hour < 17d)
        {
            return "Midday";
        }

        if (hour < 21d)
        {
            return "Dusk";
        }

        return "Night";
    }

    private static bool HasNearbyEnemyPressure(MobileParty? serviceParty)
    {
        if (serviceParty?.MapFaction == null || serviceParty.MapEvent != null)
        {
            return false;
        }

        float nearestDistanceSquared = float.MaxValue;
        foreach (MobileParty party in MobileParty.All)
        {
            if (party == null
                || !party.IsActive
                || party == serviceParty
                || party.MapFaction == null
                || !party.MapFaction.IsAtWarWith(serviceParty.MapFaction))
            {
                continue;
            }

            float distanceSquared = party.GetPosition2D.DistanceSquared(serviceParty.GetPosition2D);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearestDistanceSquared <= 64f;
    }

    private static MobileParty? FindScoutRouteSpottedEnemy(MobileParty? scoutParty)
    {
        if (scoutParty?.MapFaction == null || scoutParty.MapEvent != null)
        {
            return null;
        }

        const float scoutSpotDistanceSquared = 169f;
        MobileParty? bestCandidate = null;
        float bestDistanceSquared = float.MaxValue;

        foreach (MobileParty party in MobileParty.All)
        {
            if (party == null
                || !party.IsActive
                || !party.IsVisible
                || party == scoutParty
                || party.MapFaction == null
                || !party.MapFaction.IsAtWarWith(scoutParty.MapFaction))
            {
                continue;
            }

            bool relevantWarParty = party.Army != null || party.LeaderHero?.IsLord == true;
            if (!relevantWarParty)
            {
                continue;
            }

            float distanceSquared = party.GetPosition2D.DistanceSquared(scoutParty.GetPosition2D);
            if (distanceSquared > scoutSpotDistanceSquared || distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared = distanceSquared;
            bestCandidate = party;
        }

        return bestCandidate;
    }

    private static string GetArmyRhythmSummary(ArmyRhythmState rhythmState)
    {
        return $"{rhythmState.Context} ({rhythmState.Phase})";
    }

    private static string GetArmyPressureSummary(ArmyRhythmState rhythmState)
    {
        List<string> pressures = new();

        if (rhythmState.PreBattle)
        {
            pressures.Add("enemy nearby");
        }

        if (rhythmState.LowSupplies)
        {
            pressures.Add("low supplies");
        }

        if (rhythmState.RecoveryState)
        {
            pressures.Add($"wounded {rhythmState.WoundedCount}");
        }

        if (rhythmState.HighScrutiny)
        {
            pressures.Add("high scrutiny");
        }

        if (pressures.Count == 0)
        {
            pressures.Add("stable");
        }

        return string.Join(", ", pressures);
    }

    private bool CommanderNeedsSupplies()
    {
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (commanderParty == null || commanderParty.IsCurrentlyAtSea)
        {
            return false;
        }

        return commanderParty.Food <= 20f || commanderParty.GetNumDaysForFoodToLast() <= 2;
    }

    private int GetRequiredSupplyCount()
    {
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        int partySize = commanderParty?.MemberRoster.TotalManCount ?? 0;
        int requested = 8 + partySize / 25;
        if (CommanderNeedsSupplies())
        {
            requested += 4;
        }

        return MBMath.ClampInt(requested, 8, 20);
    }

    private int GetWoundedCountInServiceHost()
    {
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        return commanderParty?.Party?.NumberOfWoundedTotalMembers
            ?? MobileParty.MainParty?.Party?.NumberOfWoundedTotalMembers
            ?? 0;
    }

    private int GetRecruitCountInServiceHost()
    {
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        var roster = commanderParty?.MemberRoster ?? MobileParty.MainParty?.MemberRoster;
        if (roster == null)
        {
            return 0;
        }

        return roster.GetTroopRoster()
            .Where(element => element.Character != null && !element.Character.IsHero && element.Character.Tier <= 2)
            .Sum(element => element.Number);
    }

    private bool PassesInteractiveDutyCheck(SkillObject primarySkill, int difficulty)
    {
        int trustBonus = Math.Max(0, _serviceRecord.CommanderTrust * 2);
        int skill = Hero.MainHero.GetSkillValue(primarySkill);
        return skill + trustBonus + MBRandom.RandomInt(0, 51) >= difficulty;
    }

    private bool PassesTrainRecruitsDutyCheck(SkillObject supportSkill, int leadershipBias, int supportBias, int difficulty)
    {
        int trustBonus = Math.Max(0, _serviceRecord.CommanderTrust * 2);
        int rankBonus = _serviceRecord.Rank switch
        {
            RFEnlistmentRank.Soldier => 4,
            RFEnlistmentRank.Veteran => 8,
            RFEnlistmentRank.Sergeant => 12,
            _ => 0
        };
        int leadership = Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) + leadershipBias;
        int support = Hero.MainHero.GetSkillValue(supportSkill) + supportBias;
        return Math.Max(leadership, support) + trustBonus + rankBonus + MBRandom.RandomInt(0, 51) >= difficulty;
    }

    private void ApplyCommanderTrustRewardBonus(ref int serviceXp, ref int gold)
    {
        if (_serviceRecord.CommanderTrust >= 15)
        {
            serviceXp += 10;
            gold += 12;
            return;
        }

        if (_serviceRecord.CommanderTrust >= 10)
        {
            serviceXp += 6;
            gold += 8;
            return;
        }

        if (_serviceRecord.CommanderTrust <= -5)
        {
            serviceXp = Math.Max(1, serviceXp - 4);
            gold = Math.Max(1, gold - 6);
        }
    }

    private void RegisterDutySuccess(int trustGain)
    {
        _serviceRecord.DutySuccesses++;
        AdjustCommanderTrust(trustGain);
    }

    private void CompleteInteractiveDuty(
        string title,
        string resultText,
        int serviceXp,
        int gold,
        int trustGain,
        bool success,
        int fieldRep = 0,
        int logisticsRep = 0,
        int commandRep = 0,
        int siegeRep = 0)
    {
        Hero? commander = ResolveCommander();
        _serviceRecord.ServiceXp += serviceXp;
        GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
        _serviceRecord.TotalWagesPaid += gold;

        if (success)
        {
            RegisterDutySuccess(trustGain);
            if (commander != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(commander, 1, true, true);
            }
        }
        else
        {
            RegisterDutyFailure();
            if (commander != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(commander, -1, true, true);
            }
        }

        if (success)
        {
            AdjustCommanderReputation(fieldRep, logisticsRep, commandRep, siegeRep);
        }

        _serviceRecord.InteractiveDutyCount++;
        TryPromote();
        RecordServiceIncident(title, resultText, popup: false);
        ShowDutyPopup(title, $"{resultText}\n\nYou gained {serviceXp} service XP and {gold} gold.");
    }

    private void ShowNightPatrolInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Night Patrol",
            "A sentry reports movement beyond the camp fires. Do you move out at once, or hold the line and quietly wake more men first?",
            true,
            true,
            "Move out",
            "Wake the line",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Scouting, 80);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 18f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                if (_serviceRecord.Assignment == RFEnlistmentAssignment.Archer)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 8f);
                }

                CompleteInteractiveDuty(
                    "Night Patrol",
                    success
                        ? "You caught shadowing scouts before they could study the camp."
                        : "You chased a false alarm through the dark and returned cold and empty-handed.",
                    success ? 42 : 26,
                    success ? 38 : 18,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Tactics, 72);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 10f);
                CompleteInteractiveDuty(
                    "Night Patrol",
                    success
                        ? "You tightened the watch, prevented panic, and the night passed under discipline."
                        : "The delay let the movement vanish, though at least the camp stayed orderly.",
                    success ? 36 : 24,
                    success ? 32 : 16,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowTreatWoundedInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Treat Wounded",
            "The wounded need attention. Do you focus on the worst cases first, or try to stabilize as many as possible?",
            true,
            true,
            "Save the worst",
            "Stabilize many",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Medicine, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 20f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 8f);
                CompleteInteractiveDuty(
                    "Treat Wounded",
                    success
                        ? "Your triage saved the worst cases and the surgeons praised your judgment."
                        : "You committed to the hardest cases, but not all of them could be saved.",
                    success ? 44 : 28,
                    success ? 30 : 14,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Steward, 70);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 14f);
                CompleteInteractiveDuty(
                    "Treat Wounded",
                    success
                        ? "You organized the treatment line well and kept many wounded men alive through the night."
                        : "You worked hard, but the line became disordered and the surgeons were left dissatisfied.",
                    success ? 38 : 24,
                    success ? 26 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowTrainRecruitsInquiry()
    {
        bool inSettlement = MobileParty.MainParty?.CurrentSettlement != null;
        InformationManager.ShowInquiry(new InquiryData(
            "Train Recruits",
            inSettlement
                ? "Fresh recruits are losing formation in the practice yard. Do you drill them hard, or slow the pace and teach them properly?"
                : "Fresh recruits are losing formation on the march. Do you drill them hard, or slow the pace and teach them properly?",
            true,
            true,
            "Drill them hard",
            "Teach them steady",
            () =>
            {
                SkillObject weaponSkill = _serviceRecord.Assignment == RFEnlistmentAssignment.Cavalry
                    ? DefaultSkills.Polearm
                    : DefaultSkills.OneHanded;
                bool success = PassesTrainRecruitsDutyCheck(weaponSkill, 6, 12, 68);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 18f);
                Hero.MainHero.AddSkillXp(weaponSkill, 10f);
                CompleteInteractiveDuty(
                    "Train Recruits",
                    success
                        ? "The line snapped into shape under pressure, and the recruits began obeying like soldiers."
                        : "You pushed too hard and the recruits finished the drill rattled more than sharpened.",
                    success ? 40 : 24,
                    success ? 34 : 14,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesTrainRecruitsDutyCheck(DefaultSkills.Tactics, 8, 10, 58);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                CompleteInteractiveDuty(
                    "Train Recruits",
                    success
                        ? "You broke the drill down cleanly and turned raw recruits into a steadier line."
                        : "The men learned something, but the pace was too soft to impress the officers.",
                    success ? 34 : 22,
                    success ? 28 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowEquipmentCheckInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Equipment Check",
            "The officers want the line inspected before the next march. Do you run a strict inspection, or focus on getting worn gear turned around quickly?",
            true,
            true,
            "Strict inspection",
            "Quick turn-around",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Tactics, 74);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 8f);
                CompleteInteractiveDuty(
                    "Equipment Check",
                    success
                        ? "You caught weak kit, bad straps, and missing gear before the officers ever had to shout."
                        : "You were thorough, but the inspection dragged and the line grew resentful under it.",
                    success ? 38 : 24,
                    success ? 32 : 14,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Steward, 70);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 8f);
                CompleteInteractiveDuty(
                    "Equipment Check",
                    success
                        ? "You moved the gear line efficiently and put most of the host back in decent order before dusk."
                        : "You kept the line moving, but too much slipped past your eye to impress the officers.",
                    success ? 34 : 22,
                    success ? 28 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowInspectDefensesInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Inspect Defenses",
            "The commander wants the weak points found before the enemy does. Do you walk the walls and works yourself, or test the reserve line and response route?",
            true,
            true,
            "Walk the works",
            "Test the reserve line",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Engineering, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 18f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                CompleteInteractiveDuty(
                    "Inspect Defenses",
                    success
                        ? "You found weak timber, blind angles, and loose watch points before they could become real danger."
                        : "You made the round, but missed enough that the engineers were left muttering after you.",
                    success ? 42 : 24,
                    success ? 36 : 14,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Tactics, 74);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 8f);
                CompleteInteractiveDuty(
                    "Inspect Defenses",
                    success
                        ? "You tightened the reserve response and made sure men could reach the threatened points in time."
                        : "The men understood the order, but the reserve drill stayed clumsy and slow.",
                    success ? 36 : 22,
                    success ? 30 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowGateWatchInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Gate Watch",
            "Traffic through the gates has become uneasy. Do you search arrivals carefully, or double the watch line and keep the queue moving?",
            true,
            true,
            "Search arrivals",
            "Double the watch",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Scouting, 76);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 8f);
                CompleteInteractiveDuty(
                    "Gate Watch",
                    success
                        ? "You pulled suspicious travelers aside and kept bad business from slipping through the gate."
                        : "You searched hard, but found little and snarled the gate worse than before.",
                    success ? 38 : 22,
                    success ? 34 : 12,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Leadership, 72);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 8f);
                CompleteInteractiveDuty(
                    "Gate Watch",
                    success
                        ? "You kept the gate disciplined, the line orderly, and the watch responsive all through the shift."
                        : "The gate stayed open, but the watch drifted and the officers were not pleased with the looseness.",
                    success ? 34 : 22,
                    success ? 28 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowQuartermasterShortageInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Quartermaster Shortage",
            "The books, stores, and issue line are no longer matching cleanly. Do you ration the stores at once, or rework the ledgers first and search for the leak?",
            true,
            true,
            "Ration the stores",
            "Rework the ledgers",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Steward, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 20f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 8f);
                CompleteInteractiveDuty(
                    "Quartermaster Shortage",
                    success
                        ? "You imposed order on the issue line and stopped the shortage from turning into open grumbling."
                        : "You cut the line hard, but the rationing bit deeper than it needed to and bred resentment.",
                    success ? 40 : 24,
                    success ? 34 : 14,
                    success ? 2 : 0,
                    success);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Engineering, 72);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 14f);
                CompleteInteractiveDuty(
                    "Quartermaster Shortage",
                    success
                        ? "You found the break in the chain, balanced the stores, and spared the host a broader shortage."
                        : "You chased the books too long and the quartermaster line remained strained by the end of it.",
                    success ? 36 : 22,
                    success ? 30 : 12,
                    success ? 1 : 0,
                    success);
            }), true);
    }

    private void ShowLeadPatrolInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Lead Patrol",
            "You are being trusted with a patrol of your own. Do you sweep hard and fast for contact, or keep the patrol tight and control the ground carefully?",
            true,
            true,
            "Sweep for contact",
            "Control the ground",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Scouting, 82);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 18f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                CompleteInteractiveDuty(
                    "Lead Patrol",
                    success
                        ? "You found the enemy signs first and brought the patrol back with useful ground sense."
                        : "You pushed too hard and the patrol lost shape before it learned enough to matter.",
                    success ? 48 : 24,
                    success ? 44 : 14,
                    success ? 3 : 0,
                    success,
                    fieldRep: 3,
                    commandRep: 1);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Leadership, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 12f);
                CompleteInteractiveDuty(
                    "Lead Patrol",
                    success
                        ? "You kept the patrol disciplined, covered the route properly, and returned with a clean report."
                        : "You kept order, but moved too cautiously to give the commander the edge he wanted.",
                    success ? 44 : 24,
                    success ? 38 : 14,
                    success ? 2 : 0,
                    success,
                    fieldRep: 2,
                    commandRep: 2);
            }), true);
    }

    private void ShowCoordinateSupplyInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Coordinate Supply",
            "Wagons, pack animals, and issue lines are drifting out of step. Do you force priority to the fighting line first, or balance the whole column carefully?",
            true,
            true,
            "Priority to the line",
            "Balance the column",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Steward, 80);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 18f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);
                CompleteInteractiveDuty(
                    "Coordinate Supply",
                    success
                        ? "You kept the fighting line supplied first and prevented the march from stumbling at the worst moment."
                        : "You made the hard call, but the rear of the column felt the strain immediately.",
                    success ? 46 : 24,
                    success ? 40 : 14,
                    success ? 3 : 0,
                    success,
                    logisticsRep: 3,
                    commandRep: 1);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Engineering, 76);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 14f);
                CompleteInteractiveDuty(
                    "Coordinate Supply",
                    success
                        ? "You reorganized the supply flow and kept stores, baggage, and issue points from choking each other."
                        : "Your plan was sound on paper, but the column stayed slower and rougher than the quartermasters hoped.",
                    success ? 42 : 22,
                    success ? 36 : 12,
                    success ? 2 : 0,
                    success,
                    logisticsRep: 3);
            }), true);
    }

    private void ShowCommandSquadInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Command Squad",
            "A picked squad is under your word for a live assignment. Do you lead from the front and keep the pace high, or hold them steady and use cleaner control?",
            true,
            true,
            "Lead from the front",
            "Hold them steady",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Leadership, 82);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 18f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 8f);
                CompleteInteractiveDuty(
                    "Command Squad",
                    success
                        ? "The men followed your pace without hesitation and hit the task with real confidence."
                        : "You pulled the squad forward, but the rush cost you control before the work was done.",
                    success ? 48 : 24,
                    success ? 42 : 14,
                    success ? 3 : 0,
                    success,
                    fieldRep: 1,
                    commandRep: 3);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Tactics, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 16f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 12f);
                CompleteInteractiveDuty(
                    "Command Squad",
                    success
                        ? "You kept the squad ordered, responsive, and efficient enough for the officers to take real notice."
                        : "You kept the men controlled, but the execution stayed too flat to inspire confidence.",
                    success ? 44 : 24,
                    success ? 38 : 14,
                    success ? 2 : 0,
                    success,
                    commandRep: 3);
            }), true);
    }

    private void ShowStrategicPlanningInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Strategic Planning",
            "You are being asked to think over the next move with the officers. Do you argue for the safe line of advance, or the sharper plan that could break the enemy quicker?",
            true,
            true,
            "Safe line",
            "Sharper plan",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Tactics, 84);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 20f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);
                CompleteInteractiveDuty(
                    "Strategic Planning",
                    success
                        ? "Your reading of the ground and timing was accepted, and the officers kept your counsel in the final plan."
                        : "Your caution had merit, but it did not fully answer the urgency pressing on the command tent.",
                    success ? 50 : 24,
                    success ? 44 : 14,
                    success ? 3 : 0,
                    success,
                    commandRep: 1,
                    siegeRep: 3);
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Engineering, 78);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, 14f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 16f);
                CompleteInteractiveDuty(
                    "Strategic Planning",
                    success
                        ? "You pushed a bolder course, and the officers came away convinced you were seeing the battle in larger shape."
                        : "The sharper plan overreached what the army could really sustain, and the tent cooled quickly around it.",
                    success ? 46 : 22,
                    success ? 40 : 12,
                    success ? 2 : 0,
                    success,
                    fieldRep: 1,
                    siegeRep: 2,
                    commandRep: 1);
            }), true);
    }

    private void ShowDeferredPayIncidentInquiry()
    {
        int owed = _serviceRecord.DeferredWageAmount;
        InformationManager.ShowInquiry(new InquiryData(
            "Pay Delay",
            $"The pay clerk admits the host still owes you {owed} gold. Do you press the claim now, or swallow it and wait for the line to settle?",
            true,
            true,
            "Press the claim",
            "Wait it out",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Charm, 74);
                if (success)
                {
                    int release = Math.Min(owed, Math.Max(8, owed / 2));
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, release, false);
                    _serviceRecord.TotalWagesPaid += release;
                    _serviceRecord.DeferredWageAmount = Math.Max(0, _serviceRecord.DeferredWageAmount - release);
                    AdjustCommanderTrust(-1);
                    RecordServiceIncident("Pay Delay", $"You pressed your claim and managed to pry {release} gold loose without open trouble.", popup: true);
                }
                else
                {
                    AdjustCommanderTrust(-2);
                    RecordServiceIncident("Pay Delay", "You pressed the issue at the wrong time and the officers took it as selfish grumbling.", popup: true);
                }
            },
            () =>
            {
                AdjustCommanderTrust(1);
                AdjustCommanderReputation(logistics: 1, command: 1);
                RecordServiceIncident("Pay Delay", "You let the arrears stand for now and the officers marked the patience.", popup: true);
            }), true);
    }

    private void ShowShortRationsIncidentInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Short Rations",
            "The food line is tightening and men are starting to count each scoop. Do you enforce the reduction sharply, or shield the weakest part of the line first?",
            true,
            true,
            "Enforce sharply",
            "Shield the weakest",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Leadership, 74);
                if (success)
                {
                    AdjustCommanderTrust(1);
                    AdjustCommanderReputation(command: 1, logistics: 1);
                    RecordServiceIncident("Short Rations", "You kept the ration cut orderly and stopped the line from turning ugly.", popup: true);
                }
                else
                {
                    AdjustCommanderTrust(-1);
                    RecordServiceIncident("Short Rations", "You forced the issue, but the line turned sour and the camp remembered it.", popup: true);
                }
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Medicine, 70);
                if (success)
                {
                    AdjustCommanderTrust(1);
                    AdjustCommanderReputation(logistics: 1, siege: 1);
                    RecordServiceIncident("Short Rations", "You protected the weakest men first and kept the worst of the hunger from spreading into panic.", popup: true);
                }
                else
                {
                    AdjustCommanderTrust(0);
                    RecordServiceIncident("Short Rations", "Your intent was sound, but too many healthy men felt passed over for it to calm the line.", popup: true);
                }
            }), true);
    }

    private void ShowCampDisciplineIncidentInquiry()
    {
        InformationManager.ShowInquiry(new InquiryData(
            "Camp Discipline",
            "A knot of men is turning sour around the fires. Do you break the trouble hard, or pull the steadiest veterans in and cool it quietly?",
            true,
            true,
            "Break it hard",
            "Cool it quietly",
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.OneHanded, 70);
                if (success)
                {
                    AdjustCommanderTrust(1);
                    AdjustCommanderReputation(field: 1, command: 1);
                    RecordServiceIncident("Camp Discipline", "You shut the trouble down before it spread, though the line felt the hardness of it.", popup: true);
                }
                else
                {
                    AdjustCommanderTrust(-1);
                    RecordServiceIncident("Camp Discipline", "The hard answer only widened the quarrel and left the officers with more to settle after you.", popup: true);
                }
            },
            () =>
            {
                bool success = PassesInteractiveDutyCheck(DefaultSkills.Charm, 74);
                if (success)
                {
                    AdjustCommanderTrust(1);
                    AdjustCommanderReputation(command: 2);
                    RecordServiceIncident("Camp Discipline", "You cooled the fire without blood or scandal, and the veterans backed your handling of it.", popup: true);
                }
                else
                {
                    AdjustCommanderTrust(0);
                    RecordServiceIncident("Camp Discipline", "You bought time, but the grumbling never really died and the officers noticed the weakness.", popup: true);
                }
            }), true);
    }

    private void RegisterDutyFailure()
    {
        _serviceRecord.DutyFailures++;
        AdjustCommanderTrust(-2);
    }

    private void AdjustCommanderTrust(int delta)
    {
        _serviceRecord.CommanderTrust = MBMath.ClampInt(_serviceRecord.CommanderTrust + delta, -10, 20);
        if (_serviceRecord.CommanderTrust > _serviceRecord.HighestTrustReached)
        {
            _serviceRecord.HighestTrustReached = _serviceRecord.CommanderTrust;
        }
    }

    private string GetCommanderTrustLabel()
    {
        if (_serviceRecord.CommanderTrust >= 15)
        {
            return "elite confidence";
        }

        if (_serviceRecord.CommanderTrust >= 10)
        {
            return "trusted";
        }

        if (_serviceRecord.CommanderTrust >= 5)
        {
            return "proven";
        }

        if (_serviceRecord.CommanderTrust <= -5)
        {
            return "under suspicion";
        }

        return "ordinary";
    }

    private void AdjustCommanderReputation(int field = 0, int logistics = 0, int command = 0, int siege = 0)
    {
        _serviceRecord.FieldReputation = MBMath.ClampInt(_serviceRecord.FieldReputation + field, 0, 50);
        _serviceRecord.LogisticsReputation = MBMath.ClampInt(_serviceRecord.LogisticsReputation + logistics, 0, 50);
        _serviceRecord.CommandReputation = MBMath.ClampInt(_serviceRecord.CommandReputation + command, 0, 50);
        _serviceRecord.SiegeReputation = MBMath.ClampInt(_serviceRecord.SiegeReputation + siege, 0, 50);
    }

    private string GetCommanderDominantReputationDomain()
    {
        int best = Math.Max(Math.Max(_serviceRecord.FieldReputation, _serviceRecord.LogisticsReputation), Math.Max(_serviceRecord.CommandReputation, _serviceRecord.SiegeReputation));
        if (best <= 0)
        {
            return "none";
        }

        if (best == _serviceRecord.SiegeReputation)
        {
            return "siege";
        }

        if (best == _serviceRecord.LogisticsReputation)
        {
            return "logistics";
        }

        if (best == _serviceRecord.CommandReputation)
        {
            return "command";
        }

        return "field";
    }

    private string GetCommanderMemorySummary()
    {
        return GetCommanderDominantReputationDomain() switch
        {
            "field" => "trusted most in field work",
            "logistics" => "trusted most with supply and order",
            "command" => "trusted most to handle men",
            "siege" => "trusted most in siege or planning work",
            _ => "no strong service profile yet"
        };
    }

    private string GetLatestIncidentSummary()
    {
        return string.IsNullOrWhiteSpace(_serviceRecord.LastIncidentTitle)
            ? "none"
            : $"{_serviceRecord.LastIncidentTitle}: {_serviceRecord.LastIncidentText}";
    }

    private void RecordServiceIncident(string title, string text, bool popup)
    {
        _serviceRecord.LastIncidentTitle = title;
        _serviceRecord.LastIncidentText = text;
        _serviceRecord.LastIncidentDay = (int)CampaignTime.Now.ToDays;

        if (popup)
        {
            ShowDutyPopup(title, text);
        }
    }

    private string GetServiceRecordSummary()
    {
        string career = GetDominantServiceProfile();
        string conduct = _serviceRecord.CommanderTrust <= -8
            ? "discipline poor"
            : _serviceRecord.CommanderTrust <= -5
                ? "under watch"
                : _serviceRecord.CommanderTrust >= 15
                    ? "trusted core"
                    : "steady";
        return $"{career}, successes {_serviceRecord.DutySuccesses}, failures {_serviceRecord.DutyFailures}, {conduct}";
    }

    private string GetDominantServiceProfile()
    {
        int fieldScore = _serviceRecord.FieldServiceCount + (_serviceRecord.Assignment == RFEnlistmentAssignment.Infantry ? 2 : 0);
        int siegeScore = _serviceRecord.SiegeServiceCount + (_serviceRecord.Assignment == RFEnlistmentAssignment.Support ? 2 : 0);
        int navalScore = _serviceRecord.NavalServiceCount + (_serviceRecord.InCommanderNavalService ? 1 : 0);
        int scoutScore = _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => _serviceRecord.DutySuccesses + 2,
            RFEnlistmentAssignment.Cavalry => _serviceRecord.DutySuccesses + 1,
            _ => _serviceRecord.DutySuccesses
        };

        int best = Math.Max(Math.Max(fieldScore, siegeScore), Math.Max(navalScore, scoutScore));
        if (best <= 0)
        {
            return "new recruit";
        }

        if (best == siegeScore)
        {
            return "siege hand";
        }

        if (best == navalScore)
        {
            return "fleet veteran";
        }

        if (best == scoutScore)
        {
            return _serviceRecord.Assignment == RFEnlistmentAssignment.Cavalry ? "scout rider" : "field scout";
        }

        return "reliable soldier";
    }

    private string GetDutyMissionLabel()
    {
        return _serviceRecord.ActiveDutyMissionType switch
        {
            DutyMissionServiceShift => "service shift",
            DutyMissionReliefDispatch => "relief dispatch",
            DutyMissionTrustedDispatch => "trusted dispatch",
            DutyMissionSupplyDelivery => "supply delivery",
            DutyMissionForage => "forage",
            DutyMissionRecruitmentErrand => "recruitment errand",
            DutyMissionBanditHunt => "bandit hunt",
            DutyMissionDeserterSweep => "deserter sweep",
            DutyMissionHideoutStrike => "hideout strike",
            DutyMissionRoadPatrol => "road patrol",
            DutyMissionScoutRoute => "scout route",
            DutyMissionMountedPursuit => "mounted pursuit",
            _ => "recon sweep"
        };
    }

    private static Settlement? FindNearbyDutySettlement(MobileParty mainParty, Settlement anchorSettlement)
    {
        return Settlement.All
            .Where(settlement =>
                settlement != anchorSettlement
                && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage)
                && !settlement.IsUnderSiege)
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private static Settlement? FindNearbyHideoutDutySettlement(MobileParty? mainParty, Settlement? anchorSettlement)
    {
        if (mainParty == null || anchorSettlement == null || mainParty.IsCurrentlyAtSea)
        {
            return null;
        }

        const float maxDistance = 110f;
        float maxDistanceSquared = maxDistance * maxDistance;

        return Settlement.All
            .Where(settlement =>
                settlement.IsHideout
                && settlement.Hideout != null
                && settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D) <= maxDistanceSquared)
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private static string GetHideoutDutyReferenceName(Settlement? hideoutSettlement, Settlement? fallbackSettlement = null)
    {
        if (hideoutSettlement == null)
        {
            return fallbackSettlement?.Name?.ToString() ?? "unknown area";
        }

        Settlement? nearestNamedSettlement = Settlement.All
            .Where(settlement =>
                settlement != null
                && settlement != hideoutSettlement
                && !settlement.IsHideout
                && (settlement.IsVillage || settlement.IsTown || settlement.IsCastle))
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(hideoutSettlement.GetPosition2D))
            .FirstOrDefault();

        return nearestNamedSettlement?.Name?.ToString()
            ?? fallbackSettlement?.Name?.ToString()
            ?? hideoutSettlement.Name?.ToString()
            ?? "unknown area";
    }

    private static Settlement? FindNearbyForageSettlement(MobileParty? mainParty, Settlement? anchorSettlement)
    {
        if (mainParty == null || anchorSettlement == null)
        {
            return null;
        }

        return Settlement.All
            .Where(settlement =>
                settlement != anchorSettlement
                && settlement.IsVillage
                && !settlement.IsUnderSiege)
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private static Settlement? FindRecruitmentDutySettlement(MobileParty? mainParty, Settlement? anchorSettlement)
    {
        if (mainParty == null || anchorSettlement == null || mainParty.MapFaction == null)
        {
            return null;
        }

        return Settlement.All
            .Where(settlement =>
                settlement != anchorSettlement
                && (settlement.IsVillage || settlement.IsTown)
                && !settlement.IsUnderSiege
                && settlement.MapFaction != null
                && !settlement.MapFaction.IsAtWarWith(mainParty.MapFaction))
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private Settlement? FindTrustedDispatchSettlement(MobileParty mainParty, Settlement anchorSettlement)
    {
        Hero? targetHero = FindTrustedDispatchHero(mainParty, anchorSettlement);
        Settlement? targetHeroSettlement = targetHero != null ? GetHeroDutySettlement(targetHero) : null;
        if (targetHeroSettlement != null && targetHeroSettlement != anchorSettlement && !targetHeroSettlement.IsUnderSiege)
        {
            return targetHeroSettlement;
        }

        Hero? commander = ResolveCommander();
        Clan? commanderClan = commander?.Clan;

        return Settlement.All
            .Where(settlement =>
                settlement != anchorSettlement
                && (settlement.IsTown || settlement.IsCastle)
                && !settlement.IsUnderSiege
                && settlement.OwnerClan == commanderClan)
            .OrderByDescending(settlement => settlement.IsTown)
            .ThenBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault()
            ?? FindNearbyDutySettlement(mainParty, anchorSettlement);
    }

    private Hero? FindTrustedDispatchHero(MobileParty mainParty, Settlement anchorSettlement)
    {
        Hero? commander = ResolveCommander();
        Kingdom? commanderKingdom = commander?.MapFaction as Kingdom;
        if (commanderKingdom == null)
        {
            return null;
        }

        return commanderKingdom.Heroes
            .Where(hero =>
                hero != null
                && hero != commander
                && hero.IsLord
                && hero.IsAlive
                && !hero.IsPrisoner
                && GetHeroDutySettlement(hero) is Settlement settlement
                && settlement != anchorSettlement
                && !settlement.IsUnderSiege)
            .OrderBy(hero => GetHeroDutySettlement(hero)!.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private static Settlement? GetHeroDutySettlement(Hero hero)
    {
        return hero.CurrentSettlement
            ?? hero.StayingInSettlement
            ?? hero.PartyBelongedTo?.CurrentSettlement;
    }

    private static Clan? GetBanditClanForDutyMission()
    {
        Settlement? anchorSettlement = GetMissionAnchorSettlement();
        if (anchorSettlement?.Culture != null)
        {
            Clan? cultureBanditClan = Clan.BanditFactions.FirstOrDefault(clan => clan.Culture == anchorSettlement.Culture);
            if (cultureBanditClan != null)
            {
                return cultureBanditClan;
            }
        }

        return Clan.BanditFactions.FirstOrDefault();
    }

    private static Settlement? GetMissionAnchorSettlement()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || mainParty.IsCurrentlyAtSea)
        {
            return null;
        }

        return Settlement.All
            .Where(settlement => settlement.IsTown || settlement.IsCastle || settlement.IsVillage)
            .OrderBy(settlement => settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
            .FirstOrDefault();
    }

    private static CampaignVec2 FindDutyMissionSpawnPosition(MobileParty mainParty)
    {
        float angle = MBRandom.RandomFloat * MathF.PI * 2f;
        Vec2 randomOffset = new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * 2.5f;

        return new CampaignVec2(mainParty.GetPosition2D + randomOffset, !mainParty.IsCurrentlyAtSea);
    }

    private static int CountFoodUnitsInMainParty()
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null)
        {
            return 0;
        }

        int total = 0;
        foreach (ItemRosterElement item in mainParty.ItemRoster)
        {
            if (item.EquipmentElement.Item != null && item.EquipmentElement.Item.IsFood)
            {
                total += item.Amount;
            }
        }

        return total;
    }

    private static void ConsumeFoodUnitsFromMainParty(int amount)
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || amount <= 0)
        {
            return;
        }

        foreach (ItemRosterElement item in mainParty.ItemRoster.ToList())
        {
            if (amount <= 0)
            {
                break;
            }

            ItemObject? foodItem = item.EquipmentElement.Item;
            if (foodItem == null || !foodItem.IsFood || item.Amount <= 0)
            {
                continue;
            }

            int toRemove = Math.Min(item.Amount, amount);
            mainParty.ItemRoster.AddToCounts(foodItem, -toRemove);
            amount -= toRemove;
        }
    }

    private static void AddFoodUnitsToMainParty(int amount)
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mainParty == null || amount <= 0)
        {
            return;
        }

        ItemObject? foodItem = MBObjectManager.Instance.GetObject<ItemObject>("grain")
            ?? MBObjectManager.Instance.GetObject<ItemObject>("fish")
            ?? MBObjectManager.Instance.GetObject<ItemObject>("meat")
            ?? MBObjectManager.Instance.GetObject<ItemObject>("cheese");
        if (foodItem == null)
        {
            return;
        }

        mainParty.ItemRoster.AddToCounts(foodItem, amount);
    }

    private bool CanReturnIssuedEquipment()
    {
        return RFEnlistmentSettings.Instance.EnableArmorer
            && IsSpeakingToCommander()
            && HasOutstandingIssuedEquipment();
    }

    private void ReturnIssuedEquipment()
    {
        MarkCommanderAttachmentPendingFromConversation();
        List<string> itemIds = GetIssuedEquipmentIds();
        if (itemIds.Count == 0)
        {
            ShowMessage("There is no issued gear on your service record.");
            return;
        }

        foreach (string itemId in itemIds)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null || PartyBase.MainParty.ItemRoster.GetItemNumber(item) <= 0)
            {
                ShowMessage("You do not have the full issued set with you yet.");
                return;
            }
        }

        foreach (string itemId in itemIds)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item != null)
            {
                PartyBase.MainParty.ItemRoster.AddToCounts(item, -1);
            }
        }

        ClearIssuedEquipmentRecord();
        ShowMessage("Your issued gear has been returned and cleared from the record.");
    }

    private bool CanPayForIssuedEquipment()
    {
        return RFEnlistmentSettings.Instance.EnableArmorer
            && IsSpeakingToCommander()
            && (HasOutstandingIssuedEquipment() || _serviceRecord.OutstandingEquipmentDebt > 0);
    }

    private void PayForIssuedEquipment()
    {
        MarkCommanderAttachmentPendingFromConversation();
        List<string> itemIds = GetIssuedEquipmentIds();
        int cost = _serviceRecord.OutstandingEquipmentDebt > 0
            ? _serviceRecord.OutstandingEquipmentDebt
            : GetEquipmentPayoffCost(itemIds);

        if (cost <= 0)
        {
            ShowMessage("There is no equipment debt to settle.");
            ClearIssuedEquipmentRecord();
            _serviceRecord.OutstandingEquipmentDebt = 0;
            return;
        }

        if (Hero.MainHero.Gold < cost)
        {
            ShowMessage($"You need {cost} gold to keep the issued equipment.");
            return;
        }

        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, false);
        ClearIssuedEquipmentRecord();
        _serviceRecord.OutstandingEquipmentDebt = 0;
        ShowMessage($"You paid {cost} gold and the issued gear is now yours to keep.");
    }

    private void GrantAssignmentTrainingXp(float amount)
    {
        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, amount);
                break;
            case RFEnlistmentAssignment.Cavalry:
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, amount);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, amount * 0.5f);
                break;
            case RFEnlistmentAssignment.Support:
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, amount);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, amount * 0.5f);
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, amount);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, amount * 0.5f);
                break;
        }
    }

    private Hero? ResolveCommander()
    {
        if (string.IsNullOrWhiteSpace(_serviceRecord.CommanderId))
        {
            return null;
        }

        return Hero.FindFirst(hero => hero.StringId == _serviceRecord.CommanderId);
    }

    private void RefreshCommanderContextState()
    {
        _serviceRecord.InCommanderArmy = IsMainPartyInCommanderArmy();
        _serviceRecord.InCommanderSiege = IsMainPartyInCommanderSiege();
        _serviceRecord.InCommanderNavalService = IsMainPartyInCommanderNavalService();
        _serviceRecord.InCommanderBlockade = IsMainPartyInCommanderBlockade();
    }

    private bool IsMainPartyInCommanderArmy()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (mainParty == null || commanderParty == null)
        {
            return false;
        }

        if (mainParty.CurrentSettlement != null && mainParty.CurrentSettlement == commanderParty.CurrentSettlement)
        {
            return true;
        }

        if (mainParty.AttachedTo == commanderParty)
        {
            return true;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty && mainParty.TargetParty == commanderParty)
        {
            return true;
        }

        if (IsPlayerInCommanderServiceMode(commanderParty))
        {
            return true;
        }

        return mainParty.Army != null
            && commanderParty?.Army != null
            && mainParty.Army == commanderParty.Army;
    }

    private void RepairLegacyCommanderServiceState()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            _createdCommanderArmyForService = false;
            return;
        }

        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (mainParty == null)
        {
            _createdCommanderArmyForService = false;
            return;
        }

        if (_createdCommanderArmyForService)
        {
            TraceEnlistmentState("RepairLegacyCommanderServiceState", "Clearing legacy created-army flag.");
            _createdCommanderArmyForService = false;
        }

        if (mainParty.AttachedTo != null
            && (commanderParty == null || mainParty.AttachedTo != commanderParty || !mainParty.AttachedTo.IsActive))
        {
            TraceEnlistmentState("RepairLegacyCommanderServiceState", "Clearing stale attached party reference.");
            mainParty.AttachedTo = null;
        }

        if (mainParty.Army != null
            && (commanderParty == null || commanderParty.Army == null || mainParty.Army != commanderParty.Army))
        {
            TraceEnlistmentState("RepairLegacyCommanderServiceState", "Clearing stale legacy army reference from main party.");
            mainParty.Army = null;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty
            && mainParty.TargetParty != null
            && (commanderParty == null || mainParty.TargetParty != commanderParty || !mainParty.TargetParty.IsActive))
        {
            TraceEnlistmentState("RepairLegacyCommanderServiceState", "Clearing stale escort behavior.");
            mainParty.SetMoveModeHold();
        }
    }

    private void EnforceCommanderServiceMode()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (mainParty == null)
        {
            return;
        }

        if (!_serviceRecord.IsEnlisted || commanderParty == null || !commanderParty.IsActive)
        {
            RestorePlayerPartyCampaignPresence();
            return;
        }

        if (mainParty.MapEvent != null || commanderParty.MapEvent != null)
        {
            RestorePlayerPartyCampaignPresence();
            return;
        }

        if (mainParty.CurrentSettlement != null || commanderParty.CurrentSettlement != null)
        {
            RestorePlayerPartyCampaignPresence();
            return;
        }

        RestorePlayerPartyCampaignPresence();
    }

    private static void RestorePlayerPartyCampaignPresence()
    {
        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null)
        {
            return;
        }

        if (!mainParty.IsActive)
        {
            mainParty.IsActive = true;
        }

        if (!mainParty.IsVisible)
        {
            mainParty.IsVisible = true;
        }

        if (Campaign.Current.CameraFollowParty != PartyBase.MainParty)
        {
            PartyBase.MainParty.SetAsCameraFollowParty();
        }
    }

    private static void PreparePlayerPartyForCommanderBattle(MobileParty mainParty, MobileParty commanderParty)
    {
        if (mainParty.DefaultBehavior == AiBehavior.EscortParty)
        {
            mainParty.SetMoveModeHold();
        }

        mainParty.AttachedTo = null;
        mainParty.Position = commanderParty.Position;
    }

    private bool TryAttachPlayerToCommanderDuty(bool showFeedback)
    {
        TraceEnlistmentState("TryAttachPlayerToCommanderDutyStart", $"showFeedback={showFeedback}");

        // Never touch the main party while the player is a prisoner. Vanilla
        // deactivates/hides MainParty during captivity (StartCaptivityInternal);
        // reactivating/teleporting it here (or swapping the captivity menu for
        // the service wait menu) corrupts the capture state — free escape,
        // phantom party on the map. Keep the pending flag so the service
        // resumes automatically once captivity ends.
        if (Hero.MainHero != null && Hero.MainHero.IsPrisoner)
        {
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Player is a prisoner; deferring attachment.");
            return false;
        }

        if (!_serviceRecord.IsEnlisted)
        {
            _pendingCommanderAttachment = false;
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Service record is not enlisted.");
            return false;
        }

        if (IsDetachedDutyMissionInProgress())
        {
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Detached duty mission is still in progress.");
            return false;
        }

        MobileParty mainParty = MobileParty.MainParty;
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        if (mainParty == null || commanderParty == null || !commanderParty.IsActive)
        {
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Main party or commander party unavailable.");
            return false;
        }

        if (commanderParty.CurrentSettlement != null)
        {
            if (mainParty.CurrentSettlement == commanderParty.CurrentSettlement)
            {
                _pendingCommanderAttachment = false;
                RefreshCommanderContextState();
                TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Commander and player are in the same settlement.");
                return true;
            }

            if (mainParty.MapEvent == null)
            {
                RestorePlayerPartyCampaignPresence();
                EnterSettlementAction.ApplyForParty(mainParty, commanderParty.CurrentSettlement);
                ActivateCommanderServiceWaitMenu();
                _pendingCommanderAttachment = false;
                RefreshCommanderContextState();
                TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Moved player into commander settlement.");
                return true;
            }

            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Commander is inside a settlement but player is not yet there.");
            return false;
        }

        if (commanderParty.MapEvent != null)
        {
            TryClearStaleEncounterForCommanderBattle(commanderParty);
        }

        if (commanderParty.MapEvent != null && TryJoinCommanderEncounterBattle(commanderParty.MapEvent, commanderParty))
        {
            _pendingCommanderAttachment = false;
            RefreshCommanderContextState();
            TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Joined commander battle.");
            return true;
        }

        if (mainParty.MapEvent != null && mainParty.MapEvent != commanderParty.MapEvent)
        {
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Main party is busy in another map event.");
            return false;
        }

        if (mainParty.Army != null && commanderParty.Army != null && mainParty.Army == commanderParty.Army)
        {
            _pendingCommanderAttachment = false;
            RefreshCommanderContextState();
            TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Already inside commander army.");
            return true;
        }

        if (mainParty.AttachedTo == commanderParty)
        {
            _pendingCommanderAttachment = false;
            RefreshCommanderContextState();
            TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Already attached directly to commander party.");
            return true;
        }

        if (mainParty.CurrentSettlement != null
            && commanderParty.CurrentSettlement == null
            && mainParty.MapEvent == null)
        {
            LeaveSettlementAction.ApplyForParty(mainParty);
        }

        if (mainParty.MapEvent == null && commanderParty.MapEvent == null)
        {
            SyncPlayerPartyToCommanderServiceMode(commanderParty);
            ActivateCommanderServiceWaitMenu();
            _pendingCommanderAttachment = false;
            RefreshCommanderContextState();
            TraceEnlistmentState("TryAttachPlayerToCommanderDutySuccess", "Main party entered commander service mode.");

            if (showFeedback)
            {
                ShowMessage("You are now traveling in your commander's service.");
            }

            return true;
        }

        if (mainParty.CurrentSettlement != null || commanderParty.CurrentSettlement != null)
        {
            TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "One party is still inside a settlement and no army exists.");
            return false;
        }

        TraceEnlistmentState("TryAttachPlayerToCommanderDutyAbort", "Commander party could not be followed.");
        return false;
    }

    private static void ActivateCommanderServiceWaitMenu()
    {
        string? currentMenuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
        if (currentMenuId == ServiceWaitMenuId)
        {
            return;
        }

        if (Campaign.Current?.CurrentMenuContext != null)
        {
            GameMenu.SwitchToMenu(ServiceWaitMenuId);
            return;
        }

        GameMenu.ActivateGameMenu(ServiceWaitMenuId);
    }

    private void SyncPlayerPartyToCommanderServiceMode(MobileParty commanderParty)
    {
        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null)
        {
            return;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty)
        {
            mainParty.SetMoveModeHold();
        }

        mainParty.AttachedTo = null;
        mainParty.Position = commanderParty.Position;

        if (mainParty.IsVisible)
        {
            mainParty.IsVisible = false;
        }

        if (mainParty.IsActive)
        {
            mainParty.IsActive = false;
        }

        if (Campaign.Current.CameraFollowParty != commanderParty.Party)
        {
            commanderParty.Party.SetAsCameraFollowParty();
        }

        _serviceRecord.InCommanderArmy = true;
    }

    private bool TryJoinCommanderEncounterBattle(MapEvent mapEvent, MobileParty commanderParty)
    {
        MobileParty? mainParty = MobileParty.MainParty;
        if (mapEvent == null
            || commanderParty == null
            || PlayerEncounter.Current == null
            || PlayerEncounter.EncounteredBattle != mapEvent)
        {
            TraceEnlistmentState("TryJoinCommanderEncounterBattleAbort", "Encounter state does not match commander battle.");
            return false;
        }

        bool wasHiddenServiceMode = mainParty != null && !mainParty.IsActive && !mainParty.IsVisible;
        RestorePlayerPartyCampaignPresence();
        if (mainParty != null)
        {
            PreparePlayerPartyForCommanderBattle(mainParty, commanderParty);
        }

        BattleSideEnum? commanderSide = GetCommanderBattleSide(mapEvent, commanderParty);
        if (commanderSide == null || !mapEvent.CanPartyJoinBattle(PartyBase.MainParty, commanderSide.Value))
        {
            if (wasHiddenServiceMode)
            {
                SyncPlayerPartyToCommanderServiceMode(commanderParty);
                ActivateCommanderServiceWaitMenu();
            }

            TraceEnlistmentState("TryJoinCommanderEncounterBattleAbort", $"CanPartyJoinBattle=false side={commanderSide?.ToString() ?? "null"}");
            return false;
        }

        if (PlayerEncounter.InsideSettlement
            && PlayerEncounter.EncounterSettlement != null
            && PlayerEncounter.EncounterSettlement.IsUnderSiege)
        {
            PlayerEncounter.LeaveSettlement();
        }

        PlayerEncounter.JoinBattle(commanderSide.Value);
        GameMenu.SwitchToMenu("encounter");
        return true;
    }

    private void TryCreateCommanderBattleEncounter(MapEvent mapEvent, MobileParty commanderParty)
    {
        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null
            || (mainParty.MapEvent != null && mainParty.MapEvent != mapEvent)
            || !ShouldAutoJoinCommanderBattle(commanderParty))
        {
            TraceEnlistmentState("TryCreateCommanderBattleEncounterAbort", "Main party unavailable, already in map event, or auto-join disabled.");
            return;
        }

        bool wasHiddenServiceMode = !mainParty.IsActive && !mainParty.IsVisible;
        RestorePlayerPartyCampaignPresence();
        PreparePlayerPartyForCommanderBattle(mainParty, commanderParty);

        BattleSideEnum? commanderSide = GetCommanderBattleSide(mapEvent, commanderParty);
        bool canJoinBattle = commanderSide != null && mapEvent.CanPartyJoinBattle(PartyBase.MainParty, commanderSide.Value);
        if (commanderSide == null || (!canJoinBattle && !wasHiddenServiceMode))
        {
            if (wasHiddenServiceMode)
            {
                SyncPlayerPartyToCommanderServiceMode(commanderParty);
                ActivateCommanderServiceWaitMenu();
            }

            TraceEnlistmentState("TryCreateCommanderBattleEncounterAbort", $"CanPartyJoinBattle=false side={commanderSide?.ToString() ?? "null"}");
            return;
        }

        PartyBase? attackerLeader = mapEvent.AttackerSide?.LeaderParty;
        PartyBase? defenderLeader = mapEvent.DefenderSide?.LeaderParty;
        if (attackerLeader == null || defenderLeader == null)
        {
            if (wasHiddenServiceMode)
            {
                SyncPlayerPartyToCommanderServiceMode(commanderParty);
                ActivateCommanderServiceWaitMenu();
            }

            TraceEnlistmentState("TryCreateCommanderBattleEncounterAbort", "Map event leaders are missing.");
            return;
        }

        PlayerEncounter.RestartPlayerEncounter(defenderLeader, attackerLeader, forcePlayerOutFromSettlement: false);

        if (PlayerEncounter.Current != null && PlayerEncounter.EncounteredBattle == mapEvent)
        {
            TryJoinCommanderEncounterBattle(mapEvent, commanderParty);
            return;
        }

        if (wasHiddenServiceMode)
        {
            SyncPlayerPartyToCommanderServiceMode(commanderParty);
            ActivateCommanderServiceWaitMenu();
        }

        TraceEnlistmentState("TryCreateCommanderBattleEncounterPending", "Encounter created, waiting for join state.");
    }

    private void ReleasePlayerFromCommanderDuty()
    {
        TraceEnlistmentState("ReleasePlayerFromCommanderDutyStart");
        MobileParty mainParty = MobileParty.MainParty;
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;

        if (mainParty?.Army != null)
        {
            if (commanderParty?.Army != null && mainParty.Army == commanderParty.Army)
            {
                mainParty.Army = null;
            }
        }

        if (mainParty != null && mainParty.DefaultBehavior == AiBehavior.EscortParty)
        {
            mainParty.SetMoveModeHold();
        }

        if (mainParty?.AttachedTo == commanderParty)
        {
            mainParty.AttachedTo = null;
        }

        _pendingCommanderAttachment = false;
        _createdCommanderArmyForService = false;
        _nextAttachmentRetryHour = 0f;
        _serviceRecord.InCommanderArmy = false;
        _serviceRecord.InCommanderSiege = false;
        _serviceRecord.InCommanderNavalService = false;
        _serviceRecord.InCommanderBlockade = false;
        RestorePlayerPartyCampaignPresence();
        TraceEnlistmentState("ReleasePlayerFromCommanderDutyDone");
    }

    private void RestorePostServiceCampaignContext(Settlement? settlement)
    {
        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null)
        {
            return;
        }

        if (settlement != null
            && mainParty.MapEvent == null
            && mainParty.CurrentSettlement != settlement)
        {
            EnterSettlementAction.ApplyForParty(mainParty, settlement);
            string settlementMenu = settlement.IsVillage ? "village" : (settlement.IsCastle ? "castle" : "town");
            if (Campaign.Current?.CurrentMenuContext != null)
            {
                GameMenu.SwitchToMenu(settlementMenu);
            }
            else
            {
                GameMenu.ActivateGameMenu(settlementMenu);
            }

            return;
        }

        if (Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId == ServiceWaitMenuId)
        {
            GameMenu.ExitToLast();
        }
    }

    private bool NeedsCommanderAttachment()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return false;
        }

        if (IsDetachedDutyMissionInProgress())
        {
            return false;
        }

        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        MobileParty mainParty = MobileParty.MainParty;
        if (commanderParty == null || mainParty == null)
        {
            return false;
        }

        if (mainParty.MapEvent != null || commanderParty.MapEvent != null)
        {
            return false;
        }

        if (mainParty.CurrentSettlement != null && mainParty.CurrentSettlement == commanderParty.CurrentSettlement)
        {
            return false;
        }

        if (mainParty.AttachedTo == commanderParty)
        {
            return false;
        }

        if (mainParty.Army != null && commanderParty.Army != null && mainParty.Army == commanderParty.Army)
        {
            return false;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty && mainParty.TargetParty == commanderParty)
        {
            return false;
        }

        if (IsPlayerInCommanderServiceMode(commanderParty))
        {
            return false;
        }

        return true;
    }

    private static BattleSideEnum? GetCommanderBattleSide(MapEvent mapEvent, MobileParty commanderParty)
    {
        if (mapEvent.PartiesOnSide(BattleSideEnum.Attacker).Any(party => party.Party == commanderParty.Party))
        {
            return BattleSideEnum.Attacker;
        }

        if (mapEvent.PartiesOnSide(BattleSideEnum.Defender).Any(party => party.Party == commanderParty.Party))
        {
            return BattleSideEnum.Defender;
        }

        return null;
    }

    private bool ShouldAutoJoinCommanderBattle(MobileParty commanderParty)
    {
        MobileParty mainParty = MobileParty.MainParty;
        if (!_serviceRecord.IsEnlisted || mainParty == null || commanderParty == null)
        {
            return false;
        }

        if (HasActiveDutyMission() && _serviceRecord.ActiveDutyMissionType != DutyMissionServiceShift)
        {
            return false;
        }

        if (mainParty.MapEvent != null && commanderParty.MapEvent != null && mainParty.MapEvent == commanderParty.MapEvent)
        {
            return true;
        }

        if (mainParty.AttachedTo == commanderParty)
        {
            return true;
        }

        if (mainParty.Army != null && commanderParty.Army != null && mainParty.Army == commanderParty.Army)
        {
            return true;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty && mainParty.TargetParty == commanderParty)
        {
            return true;
        }

        if (!mainParty.IsActive && !mainParty.IsVisible)
        {
            return true;
        }

        if (mainParty.CurrentSettlement != null && mainParty.CurrentSettlement == commanderParty.CurrentSettlement)
        {
            return true;
        }

        return IsPlayerInCommanderServiceMode(commanderParty);
    }

    private bool IsMainPartyFollowingCommanderParty()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (mainParty == null || commanderParty == null)
        {
            return false;
        }

        if (mainParty.AttachedTo == commanderParty)
        {
            return true;
        }

        if (mainParty.DefaultBehavior == AiBehavior.EscortParty && mainParty.TargetParty == commanderParty)
        {
            return true;
        }

        return IsPlayerInCommanderServiceMode(commanderParty);
    }

    private bool ShouldForceCommanderServiceMenu(string menuId)
    {
        if (!_serviceRecord.IsEnlisted || !IsCommanderServiceNativeMenu(menuId))
        {
            return false;
        }

        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (mainParty == null || commanderParty == null)
        {
            return false;
        }

        bool hiddenServiceMode = !mainParty.IsActive && !mainParty.IsVisible;
        if (!hiddenServiceMode && !_pendingCommanderAttachment)
        {
            return false;
        }

        if ((menuId == "join_encounter" || menuId == "encounter") && commanderParty.MapEvent != null)
        {
            return false;
        }

        if (menuId == ServiceWaitMenuId)
        {
            return false;
        }

        return true;
    }

    private static bool IsCommanderServiceNativeMenu(string menuId)
    {
        switch (menuId)
        {
            case "army_wait":
            case "army_wait_at_settlement":
            case "town_wait":
            case "town_wait_menus":
            case "village_wait_menus":
            case "town_outside":
            case "castle_outside":
            case "join_encounter":
            case "encounter":
                return true;
            default:
                return false;
        }
    }

    private bool IsPlayerInCommanderServiceMode(MobileParty? commanderParty)
    {
        if (!_serviceRecord.IsEnlisted || commanderParty == null)
        {
            return false;
        }

        MobileParty mainParty = MobileParty.MainParty;
        if (mainParty == null || mainParty.IsActive || mainParty.IsVisible)
        {
            return false;
        }

        if (mainParty.CurrentSettlement != null || commanderParty.CurrentSettlement != null)
        {
            return false;
        }

        return true;
    }

    public bool ShouldSuppressNeutralThreatConversation()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return false;
        }

        Hero? speaker = Hero.OneToOneConversationHero;
        Hero? commander = ResolveCommander();
        MobileParty? commanderParty = commander?.PartyBelongedTo;
        MobileParty? speakerParty = speaker?.PartyBelongedTo;
        if (speaker == null || commander == null || commanderParty == null)
        {
            return false;
        }

        return speaker == commander
            || (speakerParty != null && IsCommanderRelatedParty(speakerParty, commanderParty));
    }

    private void TraceEnlistmentState(string tag, string? detail = null)
    {
        if (!RFEnlistmentDebug.Enabled)
        {
            return;
        }

        if (!_serviceRecord.IsEnlisted
            && string.IsNullOrEmpty(_pendingEnlistmentCommanderId)
            && string.IsNullOrEmpty(_serviceRecord.CommanderId))
        {
            return;
        }

        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        string menuId = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "-";
        string traceSignature = $"{tag}|{detail ?? string.Empty}|{_serviceRecord.IsEnlisted}|{_pendingCommanderAttachment}|{_createdCommanderArmyForService}|{_serviceRecord.CommanderId ?? "-"}"
            + $"|mainSettlement={mainParty?.CurrentSettlement?.StringId ?? "-"}|mainAttached={mainParty?.AttachedTo?.StringId ?? "-"}"
            + $"|commanderSettlement={commanderParty?.CurrentSettlement?.StringId ?? "-"}|commanderArmyLeader={commanderParty?.Army?.LeaderParty?.StringId ?? "-"}"
            + $"|encounterActive={PlayerEncounter.Current != null}|menu={menuId}";
        long nowTicks = DateTime.UtcNow.Ticks;
        if (traceSignature == _lastTraceSignature && nowTicks - _lastTraceWriteTicks < TimeSpan.TicksPerSecond)
        {
            return;
        }

        _lastTraceSignature = traceSignature;
        _lastTraceWriteTicks = nowTicks;
        string message = $"{tag} | enlisted={_serviceRecord.IsEnlisted} pending={_pendingCommanderAttachment} createdArmy={_createdCommanderArmyForService} nextRetry={_nextAttachmentRetryHour:0.00} commanderId={_serviceRecord.CommanderId ?? "-"}"
            + $" | main={mainParty.DescribeParty()} mainArmy={mainParty?.Army.DescribeArmy()} attachedTo={mainParty?.AttachedTo.DescribeParty()}"
            + $" | commander={commanderParty.DescribeParty()} commanderArmy={commanderParty?.Army.DescribeArmy()}"
            + $" | encounter={RFEnlistmentDebugExtensions.DescribeEncounter()} menu={RFEnlistmentDebugExtensions.DescribeMenu()}";

        if (!string.IsNullOrWhiteSpace(detail))
        {
            message += $" | {detail}";
        }

        RFEnlistmentDebug.Log(message);
    }

    private void TrackCommanderDecisionState(bool forceLog, string trigger)
    {
        if (!_serviceRecord.IsEnlisted || !RFCommanderDecisionDebug.Enabled)
        {
            return;
        }

        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        if (commanderParty == null || !commanderParty.IsActive)
        {
            return;
        }

        CommanderDecisionState current = CaptureCommanderDecisionState(commanderParty);
        if (_lastCommanderDecisionState == null || !string.Equals(_lastCommanderDecisionState.CommanderId, current.CommanderId, StringComparison.Ordinal))
        {
            _lastCommanderDecisionState = current;
            RFCommanderDecisionDebug.Log($"[CommanderAI] init | trigger={trigger} | reason={DescribeCommanderIntent(current)} | {current.BuildSummary()}");
            return;
        }

        string delta = DescribeCommanderDecisionDelta(_lastCommanderDecisionState, current);
        if (!forceLog && string.IsNullOrEmpty(delta))
        {
            return;
        }

        _lastCommanderDecisionState = current;
        string changeText = string.IsNullOrEmpty(delta) ? "no_state_delta" : delta;
        RFCommanderDecisionDebug.Log($"[CommanderAI] update | trigger={trigger} | change={changeText} | reason={DescribeCommanderIntent(current)} | {current.BuildSummary()}");
    }

    private static CommanderDecisionState CaptureCommanderDecisionState(MobileParty commanderParty)
    {
        return new CommanderDecisionState
        {
            CommanderId = commanderParty.LeaderHero?.StringId ?? "-",
            PartyId = commanderParty.StringId ?? "-",
            Behavior = commanderParty.DefaultBehavior.ToString(),
            TargetSettlementId = commanderParty.TargetSettlement?.StringId ?? "-",
            TargetPartyId = commanderParty.TargetParty?.StringId ?? "-",
            CurrentSettlementId = commanderParty.CurrentSettlement?.StringId ?? "-",
            BesiegedSettlementId = commanderParty.BesiegedSettlement?.StringId ?? "-",
            ArmyLeaderId = commanderParty.Army?.LeaderParty?.StringId ?? "-",
            AttachedToId = commanderParty.AttachedTo?.StringId ?? "-",
            MapEventType = commanderParty.MapEvent?.EventType.ToString() ?? "-",
            ArmyPartyCount = commanderParty.Army?.Parties?.Count ?? 0,
            WarCount = commanderParty.MapFaction?.FactionsAtWarWith?.Count ?? 0,
            NearbySieges = CountNearbySieges(commanderParty),
            StrengthBucket = (int)Math.Round(commanderParty.Party?.EstimatedStrength ?? 0f),
            NearestEnemyDistance = GetNearestEnemyDistance(commanderParty)
        };
    }

    private static string DescribeCommanderDecisionDelta(CommanderDecisionState previous, CommanderDecisionState current)
    {
        List<string> parts = new();
        AppendCommanderChange(parts, "behavior", previous.Behavior, current.Behavior);
        AppendCommanderChange(parts, "targetSettlement", previous.TargetSettlementId, current.TargetSettlementId);
        AppendCommanderChange(parts, "targetParty", previous.TargetPartyId, current.TargetPartyId);
        AppendCommanderChange(parts, "currentSettlement", previous.CurrentSettlementId, current.CurrentSettlementId);
        AppendCommanderChange(parts, "besieged", previous.BesiegedSettlementId, current.BesiegedSettlementId);
        AppendCommanderChange(parts, "armyLeader", previous.ArmyLeaderId, current.ArmyLeaderId);
        AppendCommanderChange(parts, "attachedTo", previous.AttachedToId, current.AttachedToId);
        AppendCommanderChange(parts, "mapEvent", previous.MapEventType, current.MapEventType);

        if (previous.ArmyPartyCount != current.ArmyPartyCount)
        {
            parts.Add($"armyCount:{previous.ArmyPartyCount}->{current.ArmyPartyCount}");
        }

        if (previous.WarCount != current.WarCount)
        {
            parts.Add($"wars:{previous.WarCount}->{current.WarCount}");
        }

        if (previous.NearbySieges != current.NearbySieges)
        {
            parts.Add($"nearbySieges:{previous.NearbySieges}->{current.NearbySieges}");
        }

        if (Math.Abs(previous.NearestEnemyDistance - current.NearestEnemyDistance) >= 10f)
        {
            parts.Add($"nearestEnemy:{previous.NearestEnemyDistance:0.0}->{current.NearestEnemyDistance:0.0}");
        }

        if (Math.Abs(previous.StrengthBucket - current.StrengthBucket) >= 20)
        {
            parts.Add($"strength:{previous.StrengthBucket}->{current.StrengthBucket}");
        }

        return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
    }

    private static void AppendCommanderChange(List<string> parts, string name, string previous, string current)
    {
        if (!string.Equals(previous, current, StringComparison.Ordinal))
        {
            parts.Add($"{name}:{previous}->{current}");
        }
    }

    private static string DescribeCommanderIntent(CommanderDecisionState state)
    {
        if (state.MapEventType != "-")
        {
            return $"engaged in {state.MapEventType}";
        }

        if (state.BesiegedSettlementId != "-")
        {
            return $"pressing siege on {state.BesiegedSettlementId}";
        }

        if (state.CurrentSettlementId != "-")
        {
            return $"inside {state.CurrentSettlementId}, likely regrouping or recruiting";
        }

        if (state.TargetPartyId != "-")
        {
            return state.NearestEnemyDistance >= 0f && state.NearestEnemyDistance <= 18f
                ? $"moving on party {state.TargetPartyId} with enemy contact close"
                : $"tracking party {state.TargetPartyId}";
        }

        if (state.TargetSettlementId != "-")
        {
            return $"moving toward settlement {state.TargetSettlementId}";
        }

        if (state.ArmyPartyCount > 1 && state.NearestEnemyDistance >= 0f && state.NearestEnemyDistance <= 20f)
        {
            return "army is concentrated and enemy is close";
        }

        if (state.NearbySieges > 0)
        {
            return "reacting to nearby siege pressure";
        }

        if (state.NearestEnemyDistance >= 0f && state.NearestEnemyDistance <= 15f)
        {
            return "enemy pressure nearby";
        }

        return "routine movement or no clear objective change";
    }

    private static int CountNearbySieges(MobileParty party)
    {
        return Settlement.All.Count(settlement =>
            settlement != null
            && settlement.IsUnderSiege
            && party.Position.DistanceSquared(settlement.GatePosition) <= 1600f);
    }

    private static float GetNearestEnemyDistance(MobileParty party)
    {
        if (party.MapFaction == null || party.MapEvent != null)
        {
            return -1f;
        }

        float nearestDistanceSquared = float.MaxValue;
        foreach (MobileParty otherParty in MobileParty.All)
        {
            if (otherParty == null
                || !otherParty.IsActive
                || otherParty == party
                || otherParty.MapFaction == null
                || !party.MapFaction.IsAtWarWith(otherParty.MapFaction))
            {
                continue;
            }

            float distanceSquared = party.GetPosition2D.DistanceSquared(otherParty.GetPosition2D);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
            }
        }

        return nearestDistanceSquared == float.MaxValue ? -1f : (float)Math.Sqrt(nearestDistanceSquared);
    }

    private static bool IsCommanderRelatedParty(MobileParty? candidateParty, MobileParty commanderParty)
    {
        if (candidateParty == null)
        {
            return false;
        }

        if (candidateParty == commanderParty)
        {
            return true;
        }

        if (candidateParty.Army != null && commanderParty.Army != null && candidateParty.Army == commanderParty.Army)
        {
            return true;
        }

        MobileParty? armyLeader = commanderParty.Army?.LeaderParty;
        if (armyLeader == null)
        {
            return false;
        }

        return candidateParty == armyLeader || armyLeader.AttachedParties.Contains(candidateParty);
    }

    private bool IsMainPartyInCommanderSiege()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        SiegeEvent? mainSiege = mainParty?.SiegeEvent;
        SiegeEvent? commanderSiege = commanderParty?.SiegeEvent;

        return mainSiege != null && commanderSiege != null && mainSiege == commanderSiege;
    }

    private bool IsMainPartyInCommanderNavalService()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;

        if (mainParty == null || commanderParty == null || !mainParty.IsCurrentlyAtSea || !commanderParty.IsCurrentlyAtSea)
        {
            return false;
        }

        if (mainParty.MapEvent != null && commanderParty.MapEvent != null && mainParty.MapEvent == commanderParty.MapEvent)
        {
            return true;
        }

        if (mainParty.Army != null && commanderParty.Army != null && mainParty.Army == commanderParty.Army)
        {
            return true;
        }

        return mainParty.CurrentSettlement != null
            && mainParty.CurrentSettlement == commanderParty.CurrentSettlement
            && mainParty.CurrentSettlement.HasPort;
    }

    private bool IsMainPartyInCommanderBlockade()
    {
        MobileParty mainParty = MobileParty.MainParty;
        MobileParty? commanderParty = ResolveCommander()?.PartyBelongedTo;
        SiegeEvent? mainSiege = mainParty?.SiegeEvent;
        SiegeEvent? commanderSiege = commanderParty?.SiegeEvent;

        return mainSiege != null
            && commanderSiege != null
            && mainSiege == commanderSiege
            && mainSiege.IsBlockadeActive
            && mainParty.IsCurrentlyAtSea;
    }

    private static Hero? GetSettlementCommander()
    {
        Settlement? settlement = Settlement.CurrentSettlement;
        if (settlement?.OwnerClan?.Leader == null)
        {
            return null;
        }

        Hero commander = settlement.OwnerClan.Leader;
        if (commander == Hero.MainHero)
        {
            return null;
        }

        return commander;
    }

    private static IEnumerable<Hero> GetSettlementEnlistmentCandidates()
    {
        Settlement? settlement = Settlement.CurrentSettlement;
        Clan? clan = settlement?.OwnerClan;
        if (clan == null)
        {
            return Enumerable.Empty<Hero>();
        }

        return clan.AliveLords
            .Where(hero =>
                hero != null
                && hero != Hero.MainHero
                && !hero.IsPrisoner
                && hero.PartyBelongedTo != null
                && hero.PartyBelongedTo.IsActive)
            .OrderByDescending(hero => hero.PartyBelongedTo?.CurrentSettlement == settlement)
            .ThenByDescending(hero => hero.PartyBelongedTo?.Army?.LeaderParty == hero.PartyBelongedTo)
            .ThenBy(hero => hero.Name.ToString());
    }

    private static RFEnlistmentAssignment ChooseDefaultAssignment()
    {
        int riding = Hero.MainHero.GetSkillValue(DefaultSkills.Riding);
        int bow = Hero.MainHero.GetSkillValue(DefaultSkills.Bow);
        int crossbow = Hero.MainHero.GetSkillValue(DefaultSkills.Crossbow);
        int throwing = Hero.MainHero.GetSkillValue(DefaultSkills.Throwing);
        int steward = Hero.MainHero.GetSkillValue(DefaultSkills.Steward);
        int medicine = Hero.MainHero.GetSkillValue(DefaultSkills.Medicine);
        int engineering = Hero.MainHero.GetSkillValue(DefaultSkills.Engineering);

        if (riding >= 60)
        {
            return RFEnlistmentAssignment.Cavalry;
        }

        if (medicine >= 50 || steward >= 50 || engineering >= 50)
        {
            return RFEnlistmentAssignment.Support;
        }

        if (bow >= 40 || crossbow >= 40 || throwing >= 40)
        {
            return RFEnlistmentAssignment.Archer;
        }

        return RFEnlistmentAssignment.Infantry;
    }

    private bool CanEnlistWithHero(Hero? hero)
    {
        return GetEnlistmentBlockReason(hero) == null;
    }

    public bool IsEnlistedForConversationOverrides()
    {
        return _serviceRecord.IsEnlisted;
    }

    private bool HasPendingEnlistmentPetition()
    {
        return !string.IsNullOrWhiteSpace(_pendingEnlistmentCommanderId);
    }

    private void ClearPendingEnlistmentPetition()
    {
        _pendingEnlistmentCommanderId = string.Empty;
        _pendingEnlistmentCommanderName = string.Empty;
    }

    private string? GetEnlistmentBlockReason(Hero? hero)
    {
        if (_serviceRecord.IsEnlisted)
        {
            return "You are already under contract.";
        }

        if (hero == null)
        {
            return "There is no valid lord here to accept your service.";
        }

        if (!hero.IsLord)
        {
            return "The commander here cannot accept military service.";
        }

        if (hero == Hero.MainHero)
        {
            return "You cannot enlist under your own banner.";
        }

        RFEnlistmentSettings settings = RFEnlistmentSettings.Instance;
        if (!settings.AllowEnlistingAsALord && Hero.MainHero.IsLord)
        {
            return "You cannot enlist while already serving as a lord.";
        }

        if (!settings.AllowEnlistingInMinorFactions && hero.Clan?.IsMinorFaction == true)
        {
            return "Service in minor factions is currently disabled.";
        }

        return null;
    }

    private bool IsCommanderPresentInBattle(MapEvent mapEvent)
    {
        Hero? commander = ResolveCommander();
        PartyBase? commanderParty = commander?.PartyBelongedTo?.Party;
        return commanderParty != null && mapEvent.InvolvedParties.Any(party => party == commanderParty);
    }

    private static bool IsBattleTypeCounted(MapEvent mapEvent)
    {
        return mapEvent.IsFieldBattle
            || mapEvent.IsHideoutBattle
            || mapEvent.IsRaid
            || mapEvent.IsSiegeAmbush
            || mapEvent.IsSiegeAssault
            || mapEvent.IsNavalMapEvent
            || IsBlockadeBattle(mapEvent);
    }

    private static bool IsBlockadeBattle(MapEvent mapEvent)
    {
        return mapEvent.EventType == MapEvent.BattleTypes.BlockadeBattle
            || mapEvent.EventType == MapEvent.BattleTypes.BlockadeSallyOutBattle;
    }

    private void GrantAssignmentBattleXp(bool isNavalBattle, bool isSiegeLike, bool isVictory)
    {
        float primaryXp = isVictory ? 10f : 4f;
        float secondaryXp = isVictory ? 5f : 2f;

        if (isNavalBattle)
        {
            primaryXp += 4f;
            secondaryXp += 2f;
        }
        else if (isSiegeLike)
        {
            primaryXp += 2f;
            secondaryXp += 1f;
        }

        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, primaryXp);
                Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, secondaryXp);
                break;
            case RFEnlistmentAssignment.Cavalry:
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, primaryXp);
                Hero.MainHero.AddSkillXp(isNavalBattle ? DefaultSkills.Throwing : DefaultSkills.Polearm, secondaryXp);
                break;
            case RFEnlistmentAssignment.Support:
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, primaryXp);
                Hero.MainHero.AddSkillXp(isSiegeLike || isNavalBattle ? DefaultSkills.Engineering : DefaultSkills.Medicine, secondaryXp);
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, primaryXp);
                Hero.MainHero.AddSkillXp(isSiegeLike ? DefaultSkills.Polearm : DefaultSkills.Athletics, secondaryXp);
                break;
        }
    }

    private void ApplySiegeAssignmentRewards(bool isVictory, bool isBlockadeResolution)
    {
        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, isVictory ? 12f : 6f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Crossbow, isVictory ? 6f : 3f);
                if (isBlockadeResolution)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.Throwing, 4f);
                }
                break;
            case RFEnlistmentAssignment.Cavalry:
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, isVictory ? 8f : 4f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, isVictory ? 10f : 5f);
                if (isBlockadeResolution)
                {
                    Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 5f);
                }
                break;
            case RFEnlistmentAssignment.Support:
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, isVictory ? 10f : 5f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Steward, isVictory ? 10f : 5f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, isVictory ? 8f : 4f);
                break;
            default:
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, isVictory ? 10f : 5f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, isVictory ? 8f : 4f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, isVictory ? 6f : 3f);
                break;
        }
    }

    private string GetSiegeRoleSummary(bool isBlockadeResolution)
    {
        return _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => isBlockadeResolution
                ? "Your missile fire helped keep the enemy ships under pressure."
                : "Your missile fire helped control the walls and approaches.",
            RFEnlistmentAssignment.Cavalry => isBlockadeResolution
                ? "Your boarding detail was judged on speed and discipline."
                : "Your role in reserve and pursuit was noted.",
            RFEnlistmentAssignment.Support => isBlockadeResolution
                ? "Your work on supplies, damage control, and wounded care stood out."
                : "Your work on engines, supplies, and wounded care stood out.",
            _ => isBlockadeResolution
                ? "Your boarding service in close fighting was noted."
                : "Your steady work in the assault line was noted."
        };
    }

    private void ApplyTournamentAssignmentRewards(ref int serviceXp)
    {
        switch (_serviceRecord.Assignment)
        {
            case RFEnlistmentAssignment.Archer:
                serviceXp = 48;
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 4f);
                break;
            case RFEnlistmentAssignment.Cavalry:
                serviceXp = 50;
                Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 12f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 6f);
                break;
            case RFEnlistmentAssignment.Support:
                serviceXp = 42;
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 8f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, 4f);
                break;
            default:
                serviceXp = 45;
                Hero.MainHero.AddSkillXp(DefaultSkills.OneHanded, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 10f);
                Hero.MainHero.AddSkillXp(DefaultSkills.Polearm, 4f);
                break;
        }
    }

    private string GetCommanderDutyStatusLabel()
    {
        if (_serviceRecord.InCommanderBlockade)
        {
            return "blockade";
        }

        if (_serviceRecord.InCommanderSiege)
        {
            return "siege";
        }

        if (_serviceRecord.InCommanderNavalService)
        {
            return "naval";
        }

        return "no";
    }

    private bool HasOutstandingIssuedEquipment()
    {
        return !string.IsNullOrWhiteSpace(_serviceRecord.IssuedEquipmentIds);
    }

    private List<string> GetIssuedEquipmentIds()
    {
        return string.IsNullOrWhiteSpace(_serviceRecord.IssuedEquipmentIds)
            ? new List<string>()
            : _serviceRecord.IssuedEquipmentIds
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
    }

    private void ClearIssuedEquipmentRecord()
    {
        _serviceRecord.IssuedEquipmentIds = string.Empty;
    }

    private static int GetEquipmentPayoffCost(IEnumerable<string> itemIds)
    {
        int totalCost = 0;

        foreach (string itemId in itemIds)
        {
            ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item != null)
            {
                totalCost += Math.Max(25, item.Value);
            }
        }

        return totalCost;
    }

    private bool CanRequestCommissionRecommendation()
    {
        return IsSpeakingToCommander()
            && !Hero.MainHero.IsLord
            && _serviceRecord.Rank >= RFEnlistmentRank.Sergeant
            && _serviceRecord.DaysServed >= 120
            && Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) >= 80
            && _serviceRecord.OutstandingEquipmentDebt <= 0
            && HasCommissionMerit();
    }

    private void RequestCommissionRecommendation()
    {
        MarkCommanderAttachmentPendingFromConversation();
        Hero? commander = ResolveCommander();
        if (commander == null)
        {
            return;
        }

        if (HasOutstandingIssuedEquipment())
        {
            ShowMessage("Settle your quartermaster record before asking for higher station.");
            return;
        }

        if (!HasCommissionMerit())
        {
            ShowMessage("Your commander expects a stronger service record first. Earn more trust, complete your duties well, and prove yourself in the field.");
            return;
        }

        int distinction = GetCommissionDistinction();
        float renownReward = 50f + (distinction * 15f);
        float influenceReward = 25f + (distinction * 8f);
        int relationReward = 12 + (distinction * 2);
        string serviceProfile = GetDominantServiceProfile();
        string commendation = GetCommissionCommendation(distinction);

        Hero.MainHero.Clan.AddRenown(renownReward, false);
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, influenceReward);
        ChangeRelationAction.ApplyPlayerRelation(commander, relationReward, true, true);

        string commanderName = commander.Name.ToString();
        string consequenceText = ApplyServiceCareerConsequences(isCommission: true);
        _serviceRecord.Clear();

        ShowMessage($"{commanderName} endorses your rise as a {serviceProfile}. You leave service with renown, influence, and a recommendation for higher station. {commendation} {consequenceText}".Trim());
    }

    private bool HasCommissionMerit()
    {
        int serviceActions = _serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount;
        int dutyBalance = _serviceRecord.DutySuccesses - _serviceRecord.DutyFailures;
        return _serviceRecord.CommanderTrust >= 5
            && dutyBalance >= 2
            && (serviceActions >= 3 || _serviceRecord.TournamentWins >= 1);
    }

    private int GetCommissionDistinction()
    {
        int serviceActions = _serviceRecord.FieldServiceCount + _serviceRecord.SiegeServiceCount + _serviceRecord.NavalServiceCount;
        int distinction = 0;

        if (_serviceRecord.CommanderTrust >= 10)
        {
            distinction++;
        }

        if (_serviceRecord.CommanderTrust >= 15)
        {
            distinction++;
        }

        if (_serviceRecord.DutySuccesses >= 6 && _serviceRecord.DutyFailures <= 1)
        {
            distinction++;
        }

        if (serviceActions >= 6 || _serviceRecord.TournamentWins >= 2)
        {
            distinction++;
        }

        return Math.Min(3, distinction);
    }

    private string GetCommissionCommendation(int distinction)
    {
        return distinction switch
        {
            3 => "Your commander speaks of you as a proven veteran ready to hold responsibility.",
            2 => "Your commander presents you as a reliable hand with the respect of the ranks.",
            1 => "Your commander notes that your service has been solid and upward-looking.",
            _ => "Your commander confirms that you have earned the chance to rise."
        };
    }

    private string GetCommissionStatusText()
    {
        if (_serviceRecord.Rank < RFEnlistmentRank.Sergeant)
        {
            return "need sergeant rank";
        }

        if (_serviceRecord.DaysServed < 120)
        {
            return $"serve {_serviceRecord.DaysServed}/120 days";
        }

        int leadership = Hero.MainHero.GetSkillValue(DefaultSkills.Leadership);
        if (leadership < 80)
        {
            return $"leadership {leadership}/80";
        }

        if (_serviceRecord.OutstandingEquipmentDebt > 0)
        {
            return $"settle debt {_serviceRecord.OutstandingEquipmentDebt}";
        }

        if (!HasCommissionMerit())
        {
            return "earn more trust and field merit";
        }

        return "ready for recommendation";
    }

    private string GetCareerConsequencePreview()
    {
        if (_serviceRecord.CommanderTrust >= 15 && _serviceRecord.DutyFailures == 0)
        {
            return "excellent standing likely to improve honor and commander relations";
        }

        if (_serviceRecord.CommanderTrust >= 8 && _serviceRecord.DutySuccesses >= 4)
        {
            return "strong standing likely to improve honor and future lord respect";
        }

        if (_serviceRecord.CommanderTrust <= -5 || _serviceRecord.DutyFailures > _serviceRecord.DutySuccesses)
        {
            return "poor standing may hurt honor and leave a weak military reputation";
        }

        return "steady standing with modest long-term effects";
    }

    private string ApplyServiceCareerConsequences(bool isCommission)
    {
        List<string> effects = new();
        Hero? commander = ResolveCommander();

        if (_serviceRecord.CommanderTrust >= 15 && _serviceRecord.DutyFailures == 0)
        {
            Hero.MainHero.Clan.AddRenown(8f, false);
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 12f);
            if (commander != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(commander, 2, true, true);
            }
            effects.Add("Your service leaves you with a stronger honorable reputation.");
        }
        else if (_serviceRecord.CommanderTrust >= 8 && _serviceRecord.DutySuccesses >= 4)
        {
            Hero.MainHero.Clan.AddRenown(4f, false);
            Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 10f);
            effects.Add("Your campaign record marks you as a proven soldier.");
        }
        else if (_serviceRecord.CommanderTrust <= -5 || _serviceRecord.DutyFailures > _serviceRecord.DutySuccesses)
        {
            if (commander != null)
            {
                ChangeRelationAction.ApplyPlayerRelation(commander, -2, true, true);
            }
            effects.Add("Your uneven service leaves a stain on your military name.");
        }

        if (isCommission)
        {
            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, 5f);
            Hero.MainHero.AddSkillXp(DefaultSkills.Charm, 10f);
            effects.Add("The recommendation strengthens your standing among ambitious lords.");
        }

        return string.Join(" ", effects);
    }

    private string GetCompanionSupportSummary()
    {
        List<string> supports = new();

        Hero? scout = GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout);
        Hero? surgeon = GetSupportingCompanion(MobileParty.MainParty?.EffectiveSurgeon);
        Hero? engineer = GetSupportingCompanion(MobileParty.MainParty?.EffectiveEngineer);
        Hero? quartermaster = GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster);

        if (scout != null)
        {
            supports.Add($"scout {scout.Name} ({GetSupportTierLabel(GetCompanionRoleSkill(scout, DefaultSkills.Scouting))})");
        }

        if (surgeon != null)
        {
            supports.Add($"surgeon {surgeon.Name} ({GetSupportTierLabel(GetCompanionRoleSkill(surgeon, DefaultSkills.Medicine))})");
        }

        if (engineer != null)
        {
            supports.Add($"engineer {engineer.Name} ({GetSupportTierLabel(GetCompanionRoleSkill(engineer, DefaultSkills.Engineering))})");
        }

        if (quartermaster != null)
        {
            supports.Add($"quartermaster {quartermaster.Name} ({GetSupportTierLabel(GetCompanionRoleSkill(quartermaster, DefaultSkills.Steward))})");
        }

        return supports.Count > 0 ? string.Join(", ", supports) : "none";
    }

    private string ApplyQuartermasterContractSupport()
    {
        Hero? quartermaster = GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster);
        if (quartermaster == null)
        {
            return string.Empty;
        }

        int tier = GetSupportTier(GetCompanionRoleSkill(quartermaster, DefaultSkills.Steward));
        if (tier <= 0)
        {
            return string.Empty;
        }

        _serviceRecord.ContractEnd = _serviceRecord.ContractEnd + CampaignTime.Days(tier);
        return $"{quartermaster.Name} stretches supplies and paperwork, adding {tier} extra day{(tier > 1 ? "s" : string.Empty)} to the term.";
    }

    private string ApplyQuartermasterEquipmentSupport(ref int payoffCost)
    {
        Hero? quartermaster = GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster);
        if (quartermaster == null)
        {
            return string.Empty;
        }

        int tier = GetSupportTier(GetCompanionRoleSkill(quartermaster, DefaultSkills.Steward));
        if (tier <= 0)
        {
            return string.Empty;
        }

        int discount = 10 * tier;
        payoffCost = Math.Max(25, payoffCost - discount);
        return $"{quartermaster.Name} keeps the quartermaster books in order and reduces the payoff by {discount} gold.";
    }

    private void ApplyEngineerContextSupport(ref int serviceXp)
    {
        Hero? engineer = GetSupportingCompanion(MobileParty.MainParty?.EffectiveEngineer);
        if (engineer == null)
        {
            return;
        }

        int tier = GetSupportTier(GetCompanionRoleSkill(engineer, DefaultSkills.Engineering));
        if (tier <= 0)
        {
            return;
        }

        serviceXp += tier;
        Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, tier);
    }

    private void ApplyScoutContextSupport(ref int serviceXp)
    {
        Hero? scout = GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout);
        if (scout == null)
        {
            return;
        }

        int tier = GetSupportTier(GetCompanionRoleSkill(scout, DefaultSkills.Scouting));
        if (tier <= 0)
        {
            return;
        }

        serviceXp += tier;
        Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, tier);
    }

    private void ApplyRoleSpecificCompanionDutySupport(bool isNavalDuty, bool isBlockadeDuty, ref int serviceXp, ref int gold)
    {
        if (_serviceRecord.Assignment == RFEnlistmentAssignment.Support)
        {
            Hero? surgeon = GetSupportingCompanion(MobileParty.MainParty?.EffectiveSurgeon);
            if (surgeon != null)
            {
                int surgeonTier = GetSupportTier(GetCompanionRoleSkill(surgeon, DefaultSkills.Medicine));
                if (surgeonTier > 0)
                {
                    serviceXp += surgeonTier;
                    Hero.MainHero.AddSkillXp(DefaultSkills.Medicine, surgeonTier);
                }
            }
        }

        if (isNavalDuty || isBlockadeDuty || _serviceRecord.Assignment == RFEnlistmentAssignment.Archer || _serviceRecord.Assignment == RFEnlistmentAssignment.Cavalry)
        {
            Hero? scout = GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout);
            if (scout != null)
            {
                int scoutTier = GetSupportTier(GetCompanionRoleSkill(scout, DefaultSkills.Scouting));
                if (scoutTier > 0)
                {
                    serviceXp += scoutTier;
                    gold += scoutTier;
                }
            }
        }
    }

    private string ApplyCompanionTrainingSupport(ref float trainingXp, ref int serviceXpGain)
    {
        Hero? trainer = GetRelevantTrainingCompanion();
        if (trainer == null)
        {
            return string.Empty;
        }

        int supportSkill = GetCompanionTrainingSkill(trainer);
        int supportTier = GetSupportTier(supportSkill);
        trainingXp *= 1f + (0.05f * supportTier);
        serviceXpGain += 2 + (2 * supportTier);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 1f + supportTier);

        return $"{trainer.Name} helps supervise the drill as a {GetSupportTierLabel(supportSkill)} companion.";
    }

    private void ApplyCompanionDutySupport(int missionType, ref int serviceXp, ref int gold)
    {
        Hero? supporter = GetRelevantDutyCompanion(missionType);
        if (supporter == null)
        {
            return;
        }

        int supportSkill = GetCompanionDutySkill(supporter, missionType);
        int supportTier = GetSupportTier(supportSkill);
        serviceXp += 2 + (2 * supportTier);
        gold += 3 + (3 * supportTier);
        Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 1f + supportTier);
        Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 1f + supportTier);
    }

    private Hero? GetRelevantTrainingCompanion()
    {
        return _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            RFEnlistmentAssignment.Cavalry => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            RFEnlistmentAssignment.Support => GetSupportingCompanion(MobileParty.MainParty?.EffectiveEngineer)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveSurgeon),
            _ => GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout)
        };
    }

    private Hero? GetRelevantDutyCompanion(int missionType)
    {
        return missionType switch
        {
            DutyMissionReliefDispatch => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster),
            DutyMissionTrustedDispatch => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster),
            DutyMissionSupplyDelivery => GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveEngineer),
            DutyMissionForage => GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionRecruitmentErrand => GetSupportingCompanion(MobileParty.MainParty?.EffectiveQuartermaster)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionBanditHunt => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionDeserterSweep => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionHideoutStrike => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout)
                ?? GetSupportingCompanion(MobileParty.MainParty?.EffectiveEngineer),
            DutyMissionRoadPatrol => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionScoutRoute => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            DutyMissionMountedPursuit => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout),
            _ => GetSupportingCompanion(MobileParty.MainParty?.EffectiveScout)
        };
    }

    private int GetCompanionTrainingSkill(Hero hero)
    {
        return _serviceRecord.Assignment switch
        {
            RFEnlistmentAssignment.Archer => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            RFEnlistmentAssignment.Cavalry => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            RFEnlistmentAssignment.Support => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Engineering),
                GetCompanionRoleSkill(hero, DefaultSkills.Medicine)),
            _ => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Steward),
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting))
        };
    }

    private int GetCompanionDutySkill(Hero hero, int missionType)
    {
        return missionType switch
        {
            DutyMissionReliefDispatch => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
                GetCompanionRoleSkill(hero, DefaultSkills.Leadership)),
            DutyMissionTrustedDispatch => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
                GetCompanionRoleSkill(hero, DefaultSkills.Steward)),
            DutyMissionSupplyDelivery => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Steward),
                GetCompanionRoleSkill(hero, DefaultSkills.Engineering)),
            DutyMissionForage => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Steward),
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting)),
            DutyMissionRecruitmentErrand => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Leadership),
                GetCompanionRoleSkill(hero, DefaultSkills.Steward)),
            DutyMissionBanditHunt => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            DutyMissionDeserterSweep => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
                GetCompanionRoleSkill(hero, DefaultSkills.Tactics)),
            DutyMissionHideoutStrike => Math.Max(
                GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
                GetCompanionRoleSkill(hero, DefaultSkills.Engineering)),
            DutyMissionRoadPatrol => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            DutyMissionScoutRoute => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            DutyMissionMountedPursuit => GetCompanionRoleSkill(hero, DefaultSkills.Scouting),
            _ => GetCompanionRoleSkill(hero, DefaultSkills.Scouting)
        };
    }

    private static int GetCompanionRoleSkill(Hero hero, SkillObject skill)
    {
        return hero.GetSkillValue(skill);
    }

    private static int GetSupportTier(int skillValue)
    {
        if (skillValue >= 140)
        {
            return 3;
        }

        if (skillValue >= 100)
        {
            return 2;
        }

        if (skillValue >= 60)
        {
            return 1;
        }

        return 0;
    }

    private static string GetSupportTierLabel(int skillValue)
    {
        return GetSupportTier(skillValue) switch
        {
            3 => "elite",
            2 => "seasoned",
            1 => "capable",
            _ => "limited"
        };
    }

    private static Hero? GetSupportingCompanion(Hero? hero)
    {
        if (hero == null || hero == Hero.MainHero || hero.Clan != Clan.PlayerClan)
        {
            return null;
        }

        return hero;
    }

    private int GetDaysRemaining()
    {
        if (!_serviceRecord.IsEnlisted)
        {
            return 0;
        }

        if (_serviceRecord.ContractEnd.IsPast)
        {
            return 0;
        }

        return Math.Max(0, (int)(_serviceRecord.ContractEnd.RemainingDaysFromNow));
    }

    private static TextObject GetAssignmentName(RFEnlistmentAssignment assignment)
    {
        return assignment switch
        {
            RFEnlistmentAssignment.Archer => new TextObject("{=rf_enlistment_assignment_archer}ranged soldier"),
            RFEnlistmentAssignment.Cavalry => new TextObject("{=rf_enlistment_assignment_cavalry}mounted soldier"),
            RFEnlistmentAssignment.Support => new TextObject("{=rf_enlistment_assignment_support}support soldier"),
            _ => new TextObject("{=rf_enlistment_assignment_infantry}infantry soldier")
        };
    }

    private static TextObject GetRankName(RFEnlistmentRank rank)
    {
        return rank switch
        {
            RFEnlistmentRank.Soldier => new TextObject("{=rf_enlistment_rank_soldier}Soldier"),
            RFEnlistmentRank.Veteran => new TextObject("{=rf_enlistment_rank_veteran}Veteran"),
            RFEnlistmentRank.Sergeant => new TextObject("{=rf_enlistment_rank_sergeant}Sergeant"),
            _ => new TextObject("{=rf_enlistment_rank_recruit}Recruit")
        };
    }

    private static TextObject GetConfiguredText(string id, string fallback)
    {
        return RFEnlistmentSettings.Instance.CustomText
            ? new TextObject("{=" + id + "}" + fallback)
            : new TextObject(fallback);
    }

    private static void ShowMessage(string text)
    {
        if (!RFEnlistmentSettings.Instance.MessagesEnabled)
        {
            return;
        }

        InformationManager.DisplayMessage(new InformationMessage(text));
    }

    private static void ShowDutyPopup(string title, string body)
    {
        InformationManager.ShowInquiry(new InquiryData(
            title,
            body,
            true,
            false,
            "Continue",
            string.Empty,
            null,
            null), true);
    }

    private string BuildCommanderBriefing(string body)
    {
        return $"{GetCommanderDutyLeadText()}\n\n{body}";
    }
}
