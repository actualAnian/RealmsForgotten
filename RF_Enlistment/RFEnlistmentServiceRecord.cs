using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RF_Enlistment;

public sealed class RFEnlistmentServiceRecord
{
    [SaveableField(1)]
    public bool IsEnlisted;

    [SaveableField(2)]
    public string CommanderId = string.Empty;

    [SaveableField(3)]
    public string CommanderName = string.Empty;

    [SaveableField(4)]
    public CampaignTime EnlistedAt = CampaignTime.Never;

    [SaveableField(5)]
    public CampaignTime ContractEnd = CampaignTime.Never;

    [SaveableField(6)]
    public RFEnlistmentRank Rank = RFEnlistmentRank.Recruit;

    [SaveableField(7)]
    public RFEnlistmentAssignment Assignment = RFEnlistmentAssignment.Infantry;

    [SaveableField(8)]
    public int ServiceXp;

    [SaveableField(9)]
    public int DaysServed;

    [SaveableField(10)]
    public bool ContractExpiredNoticeShown;

    [SaveableField(11)]
    public int TotalWagesPaid;

    [SaveableField(12)]
    public int HighestEquipmentRankIssued;

    [SaveableField(13)]
    public int LastTrainingDay = -1;

    [SaveableField(14)]
    public string IssuedEquipmentIds = string.Empty;

    [SaveableField(15)]
    public int OutstandingEquipmentDebt;

    [SaveableField(16)]
    public int LastServiceEventDay = -1;

    [SaveableField(17)]
    public bool InCommanderArmy;

    [SaveableField(18)]
    public bool InCommanderSiege;

    [SaveableField(19)]
    public int ActiveDutyMissionType;

    [SaveableField(20)]
    public int ActiveDutyMissionExpiresDay = -1;

    [SaveableField(21)]
    public string ActiveDutyMissionTargetPartyId = string.Empty;

    [SaveableField(22)]
    public string ActiveDutyMissionTargetSettlementId = string.Empty;

    [SaveableField(23)]
    public bool ActiveDutyMissionCompleted;

    [SaveableField(24)]
    public string ActiveDutyMissionOutcomeText = string.Empty;

    [SaveableField(25)]
    public bool InCommanderNavalService;

    [SaveableField(26)]
    public bool InCommanderBlockade;

    [SaveableField(27)]
    public int CommanderTrust;

    [SaveableField(28)]
    public int DutySuccesses;

    [SaveableField(29)]
    public int DutyFailures;

    [SaveableField(30)]
    public int FieldServiceCount;

    [SaveableField(31)]
    public int SiegeServiceCount;

    [SaveableField(32)]
    public int NavalServiceCount;

    [SaveableField(33)]
    public int TournamentWins;

    [SaveableField(34)]
    public CampaignTime ActiveDutyMissionDueTime = CampaignTime.Never;

    [SaveableField(35)]
    public int ActiveDutyMissionRequiredSupplyCount;

    [SaveableField(36)]
    public int FieldReputation;

    [SaveableField(37)]
    public int LogisticsReputation;

    [SaveableField(38)]
    public int CommandReputation;

    [SaveableField(39)]
    public int SiegeReputation;

    [SaveableField(40)]
    public int DeferredWageAmount;

    [SaveableField(41)]
    public int LastTensionEventDay = -1;

    [SaveableField(42)]
    public string LastIncidentTitle = string.Empty;

    [SaveableField(43)]
    public string LastIncidentText = string.Empty;

    [SaveableField(44)]
    public int LastIncidentDay = -1;

    [SaveableField(45)]
    public int BattleMeritCount;

    [SaveableField(46)]
    public int LastBattleMeritScore = -1;

    [SaveableField(47)]
    public string LastBattleMeritGrade = string.Empty;

    [SaveableField(48)]
    public string LastBattleMeritText = string.Empty;

    [SaveableField(49)]
    public int LastBattleMeritDay = -1;

    [SaveableField(50)]
    public int LastInteractiveDutyType;

    [SaveableField(51)]
    public int LastInteractiveDutyDay = -1;

    [SaveableField(52)]
    public int InteractiveDutyRepeatStreak;

    [SaveableField(53)]
    public int LastFieldDutyType;

    [SaveableField(54)]
    public int LastFieldDutyDay = -1;

    [SaveableField(55)]
    public int FieldDutyRepeatStreak;

    [SaveableField(56)]
    public int LastIncidentType;

    [SaveableField(57)]
    public int IncidentRepeatStreak;

    [SaveableField(58)]
    public int InteractiveDutyCount;

    [SaveableField(59)]
    public int DetachedDutyCount;

    [SaveableField(60)]
    public int ServiceShiftCount;

    [SaveableField(61)]
    public int BattleVictories;

    [SaveableField(62)]
    public int BattleDefeats;

    [SaveableField(63)]
    public int PromotionCount;

    [SaveableField(64)]
    public int HighestTrustReached;

    [SaveableField(65)]
    public int LastPromotionDay = -1;

    public RFEnlistmentServiceRecord()
    {
    }

    public void Start(Hero commander, RFEnlistmentAssignment assignment, float contractDays)
    {
        IsEnlisted = true;
        CommanderId = commander.StringId;
        CommanderName = commander.Name?.ToString() ?? commander.StringId;
        EnlistedAt = CampaignTime.Now;
        ContractEnd = CampaignTime.DaysFromNow(contractDays);
        Rank = RFEnlistmentRank.Recruit;
        Assignment = assignment;
        ServiceXp = 0;
        DaysServed = 0;
        ContractExpiredNoticeShown = false;
        TotalWagesPaid = 0;
        HighestEquipmentRankIssued = -1;
        LastTrainingDay = -1;
        IssuedEquipmentIds = string.Empty;
        OutstandingEquipmentDebt = 0;
        LastServiceEventDay = -1;
        InCommanderArmy = false;
        InCommanderSiege = false;
        ActiveDutyMissionType = 0;
        ActiveDutyMissionExpiresDay = -1;
        ActiveDutyMissionTargetPartyId = string.Empty;
        ActiveDutyMissionTargetSettlementId = string.Empty;
        ActiveDutyMissionCompleted = false;
        ActiveDutyMissionOutcomeText = string.Empty;
        InCommanderNavalService = false;
        InCommanderBlockade = false;
        CommanderTrust = 0;
        DutySuccesses = 0;
        DutyFailures = 0;
        FieldServiceCount = 0;
        SiegeServiceCount = 0;
        NavalServiceCount = 0;
        TournamentWins = 0;
        ActiveDutyMissionDueTime = CampaignTime.Never;
        ActiveDutyMissionRequiredSupplyCount = 0;
        FieldReputation = 0;
        LogisticsReputation = 0;
        CommandReputation = 0;
        SiegeReputation = 0;
        DeferredWageAmount = 0;
        LastTensionEventDay = -1;
        LastIncidentTitle = string.Empty;
        LastIncidentText = string.Empty;
        LastIncidentDay = -1;
        BattleMeritCount = 0;
        LastBattleMeritScore = -1;
        LastBattleMeritGrade = string.Empty;
        LastBattleMeritText = string.Empty;
        LastBattleMeritDay = -1;
        LastInteractiveDutyType = 0;
        LastInteractiveDutyDay = -1;
        InteractiveDutyRepeatStreak = 0;
        LastFieldDutyType = 0;
        LastFieldDutyDay = -1;
        FieldDutyRepeatStreak = 0;
        LastIncidentType = 0;
        IncidentRepeatStreak = 0;
        InteractiveDutyCount = 0;
        DetachedDutyCount = 0;
        ServiceShiftCount = 0;
        BattleVictories = 0;
        BattleDefeats = 0;
        PromotionCount = 0;
        HighestTrustReached = 0;
        LastPromotionDay = -1;
    }

    public void Clear()
    {
        IsEnlisted = false;
        CommanderId = string.Empty;
        CommanderName = string.Empty;
        EnlistedAt = CampaignTime.Never;
        ContractEnd = CampaignTime.Never;
        Rank = RFEnlistmentRank.Recruit;
        Assignment = RFEnlistmentAssignment.Infantry;
        ServiceXp = 0;
        DaysServed = 0;
        ContractExpiredNoticeShown = false;
        TotalWagesPaid = 0;
        HighestEquipmentRankIssued = -1;
        LastTrainingDay = -1;
        IssuedEquipmentIds = string.Empty;
        OutstandingEquipmentDebt = 0;
        LastServiceEventDay = -1;
        InCommanderArmy = false;
        InCommanderSiege = false;
        ActiveDutyMissionType = 0;
        ActiveDutyMissionExpiresDay = -1;
        ActiveDutyMissionTargetPartyId = string.Empty;
        ActiveDutyMissionTargetSettlementId = string.Empty;
        ActiveDutyMissionCompleted = false;
        ActiveDutyMissionOutcomeText = string.Empty;
        InCommanderNavalService = false;
        InCommanderBlockade = false;
        CommanderTrust = 0;
        DutySuccesses = 0;
        DutyFailures = 0;
        FieldServiceCount = 0;
        SiegeServiceCount = 0;
        NavalServiceCount = 0;
        TournamentWins = 0;
        ActiveDutyMissionDueTime = CampaignTime.Never;
        ActiveDutyMissionRequiredSupplyCount = 0;
        FieldReputation = 0;
        LogisticsReputation = 0;
        CommandReputation = 0;
        SiegeReputation = 0;
        DeferredWageAmount = 0;
        LastTensionEventDay = -1;
        LastIncidentTitle = string.Empty;
        LastIncidentText = string.Empty;
        LastIncidentDay = -1;
        BattleMeritCount = 0;
        LastBattleMeritScore = -1;
        LastBattleMeritGrade = string.Empty;
        LastBattleMeritText = string.Empty;
        LastBattleMeritDay = -1;
        LastInteractiveDutyType = 0;
        LastInteractiveDutyDay = -1;
        InteractiveDutyRepeatStreak = 0;
        LastFieldDutyType = 0;
        LastFieldDutyDay = -1;
        FieldDutyRepeatStreak = 0;
        LastIncidentType = 0;
        IncidentRepeatStreak = 0;
        InteractiveDutyCount = 0;
        DetachedDutyCount = 0;
        ServiceShiftCount = 0;
        BattleVictories = 0;
        BattleDefeats = 0;
        PromotionCount = 0;
        HighestTrustReached = 0;
        LastPromotionDay = -1;
    }
}
