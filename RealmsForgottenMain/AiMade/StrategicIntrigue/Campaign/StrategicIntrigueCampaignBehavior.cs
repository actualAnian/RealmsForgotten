using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.ClanAlignment;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.InciteBreak;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.KingdomObjectives;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.RumorCampaigns;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.SecretPacts;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using RF_warsystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Campaign;

public sealed class StrategicIntrigueCampaignBehavior : CampaignBehaviorBase
{
    private const bool PersistEspionageSaveData = true;

    private enum CrackdownPunishmentOutcome
    {
        None,
        Exile,
        Imprisonment,
        Execution
    }

    private Dictionary<Clan, ClanIntrigueState> _clanStates = new();
    private Dictionary<Kingdom, KingdomIntrigueState> _kingdomStates = new();
    private List<SecretPact> _secretPacts = new();
    private List<SecretAllianceCompact> _secretAlliances = new();
    private List<IntrigueOperation> _pendingOperations = new();
    private List<EspionageOperation> _espionageOperations = new();
    private List<EspionageReport> _espionageReports = new();
    private string _espionageOperationsState = "";
    private string _espionageReportsState = "";
    private CampaignTime _lastIntrigueExecutionAt = CampaignTime.Zero;
    private bool _isInitialized;
    private bool _isInitializing;
    private string _warTableReturnMenuId = "castle";

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
        CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
        CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
        CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
        CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, OnClanInfluenceChanged);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, OnTownRebelliosStateChanged);
        CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    public override void SyncData(IDataStore dataStore)
    {
        RFLogger.Log($"[StrategicIntrigue] SyncData begin | loading={dataStore.IsLoading} | saving={dataStore.IsSaving} | persistEspionage={PersistEspionageSaveData}");
        dataStore.SyncData("_clanStates", ref _clanStates);
        dataStore.SyncData("_kingdomStates", ref _kingdomStates);
        dataStore.SyncData("_secretPacts", ref _secretPacts);
        dataStore.SyncData("_secretAlliances", ref _secretAlliances);
        dataStore.SyncData("_pendingOperations", ref _pendingOperations);
        dataStore.SyncData("_lastIntrigueExecutionAt", ref _lastIntrigueExecutionAt);
        if (PersistEspionageSaveData)
        {
            if (!dataStore.IsLoading)
            {
                _espionageOperationsState = SerializeEspionageOperations();
                _espionageReportsState = SerializeEspionageReports();
            }

            dataStore.SyncData("_espionageOperationsState", ref _espionageOperationsState);
            dataStore.SyncData("_espionageReportsState", ref _espionageReportsState);
        }
        dataStore.SyncData("_isInitialized", ref _isInitialized);

        _clanStates ??= new Dictionary<Clan, ClanIntrigueState>();
        _kingdomStates ??= new Dictionary<Kingdom, KingdomIntrigueState>();
        _secretPacts ??= new List<SecretPact>();
        _secretAlliances ??= new List<SecretAllianceCompact>();
        _pendingOperations ??= new List<IntrigueOperation>();
        _espionageOperations ??= new List<EspionageOperation>();
        _espionageReports ??= new List<EspionageReport>();

        if (!PersistEspionageSaveData)
        {
            _espionageOperations.Clear();
            _espionageReports.Clear();
        }
        else if (dataStore.IsLoading)
        {
            _espionageOperations = DeserializeEspionageOperations(_espionageOperationsState);
            _espionageReports = DeserializeEspionageReports(_espionageReportsState);
        }

        if (dataStore.IsLoading)
        {
            RepairEspionageCompanions();
        }

        RFLogger.Log($"[StrategicIntrigue] SyncData end | ops={_pendingOperations.Count} | espionageOps={_espionageOperations.Count} | espionageReports={_espionageReports.Count}");
    }

    private string SerializeEspionageOperations()
    {
        try
        {
            var payload = _espionageOperations.Select(x => new EspionageOperationRecord
            {
                Type = x.Type,
                CompanionId = x.Companion?.StringId ?? "",
                TargetClanId = x.TargetClan?.StringId ?? "",
                TargetKingdomId = x.TargetKingdom?.StringId ?? "",
                StartedAtDays = (float)x.StartedAt.ToDays,
                ResolveAtDays = (float)x.ResolveAt.ToDays,
                Status = x.Status,
                Power = x.Power,
                Risk = x.Risk
            }).ToList();

            return JsonConvert.SerializeObject(payload);
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[StrategicIntrigue] SerializeEspionageOperations failed | {ex.Message}");
            return "[]";
        }
    }

    private string SerializeEspionageReports()
    {
        try
        {
            var payload = _espionageReports.Select(x => new EspionageReportRecord
            {
                CompanionId = x.Companion?.StringId ?? "",
                Type = x.Type,
                TargetClanId = x.TargetClan?.StringId ?? "",
                TargetKingdomId = x.TargetKingdom?.StringId ?? "",
                CreatedAtDays = (float)x.CreatedAt.ToDays,
                Outcome = x.Outcome,
                Confidence = x.Confidence,
                EstimatedDissidence = x.EstimatedDissidence,
                EstimatedTrustToPlayer = x.EstimatedTrustToPlayer,
                EstimatedFearOfRuler = x.EstimatedFearOfRuler,
                EstimatedClaimantAmbition = x.EstimatedClaimantAmbition,
                EstimatedBreakawayViability = x.EstimatedBreakawayViability,
                EstimatedRoyalLegitimacy = x.EstimatedRoyalLegitimacy,
                EstimatedCourtFragmentation = x.EstimatedCourtFragmentation,
                EstimatedClaimantPressure = x.EstimatedClaimantPressure,
                EstimatedRulerSupport = x.EstimatedRulerSupport,
                RecommendedAction = x.RecommendedAction
            }).ToList();

            return JsonConvert.SerializeObject(payload);
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[StrategicIntrigue] SerializeEspionageReports failed | {ex.Message}");
            return "[]";
        }
    }

    private List<EspionageOperation> DeserializeEspionageOperations(string serialized)
    {
        try
        {
            var payload = JsonConvert.DeserializeObject<List<EspionageOperationRecord>>(serialized ?? "[]")
                ?? new List<EspionageOperationRecord>();

            var result = new List<EspionageOperation>();
            foreach (EspionageOperationRecord record in payload)
            {
                Hero companion = ResolveHero(record.CompanionId);
                Clan targetClan = ResolveClan(record.TargetClanId);
                Kingdom targetKingdom = ResolveKingdom(record.TargetKingdomId);

                var operation = new EspionageOperation(
                    record.Type,
                    companion,
                    targetClan,
                    targetKingdom,
                    CampaignTime.Days(record.StartedAtDays),
                    CampaignTime.Days(record.ResolveAtDays),
                    record.Power,
                    record.Risk);
                operation.Status = record.Status;
                operation.ClampValues();
                result.Add(operation);
            }

            RFLogger.Log($"[StrategicIntrigue] DeserializeEspionageOperations ok | count={result.Count}");
            return result;
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[StrategicIntrigue] DeserializeEspionageOperations failed | {ex.Message}");
            return new List<EspionageOperation>();
        }
    }

    private List<EspionageReport> DeserializeEspionageReports(string serialized)
    {
        try
        {
            var payload = JsonConvert.DeserializeObject<List<EspionageReportRecord>>(serialized ?? "[]")
                ?? new List<EspionageReportRecord>();

            var result = new List<EspionageReport>();
            foreach (EspionageReportRecord record in payload)
            {
                Hero companion = ResolveHero(record.CompanionId);
                Clan targetClan = ResolveClan(record.TargetClanId);
                Kingdom targetKingdom = ResolveKingdom(record.TargetKingdomId);

                var report = new EspionageReport(
                    companion,
                    record.Type,
                    targetClan,
                    targetKingdom,
                    CampaignTime.Days(record.CreatedAtDays),
                    record.Outcome,
                    record.Confidence,
                    record.EstimatedDissidence,
                    record.EstimatedTrustToPlayer,
                    record.EstimatedFearOfRuler,
                    record.EstimatedClaimantAmbition,
                    record.EstimatedBreakawayViability,
                    record.EstimatedRoyalLegitimacy,
                    record.EstimatedCourtFragmentation,
                    record.EstimatedClaimantPressure,
                    record.EstimatedRulerSupport,
                    record.RecommendedAction ?? "");
                report.ClampValues();
                result.Add(report);
            }

            RFLogger.Log($"[StrategicIntrigue] DeserializeEspionageReports ok | count={result.Count}");
            return result;
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[StrategicIntrigue] DeserializeEspionageReports failed | {ex.Message}");
            return new List<EspionageReport>();
        }
    }

    private static Hero ResolveHero(string heroId)
    {
        return string.IsNullOrWhiteSpace(heroId) ? null : Hero.FindFirst(x => x.StringId == heroId);
    }

    private static Clan ResolveClan(string clanId)
    {
        return string.IsNullOrWhiteSpace(clanId) ? null : Clan.All.FirstOrDefault(x => x.StringId == clanId);
    }

    private static Kingdom ResolveKingdom(string kingdomId)
    {
        return string.IsNullOrWhiteSpace(kingdomId) ? null : Kingdom.All.FirstOrDefault(x => x.StringId == kingdomId);
    }

    private sealed class EspionageOperationRecord
    {
        public EspionageOperationType Type { get; set; }
        public string CompanionId { get; set; } = "";
        public string TargetClanId { get; set; } = "";
        public string TargetKingdomId { get; set; } = "";
        public float StartedAtDays { get; set; }
        public float ResolveAtDays { get; set; }
        public EspionageOperationStatus Status { get; set; }
        public float Power { get; set; }
        public float Risk { get; set; }
    }

    private sealed class EspionageReportRecord
    {
        public string CompanionId { get; set; } = "";
        public EspionageOperationType Type { get; set; }
        public string TargetClanId { get; set; } = "";
        public string TargetKingdomId { get; set; } = "";
        public float CreatedAtDays { get; set; }
        public EspionageOperationStatus Outcome { get; set; }
        public EspionageReportConfidence Confidence { get; set; }
        public float EstimatedDissidence { get; set; }
        public float EstimatedTrustToPlayer { get; set; }
        public float EstimatedFearOfRuler { get; set; }
        public float EstimatedClaimantAmbition { get; set; }
        public float EstimatedBreakawayViability { get; set; }
        public float EstimatedRoyalLegitimacy { get; set; }
        public float EstimatedCourtFragmentation { get; set; }
        public float EstimatedClaimantPressure { get; set; }
        public float EstimatedRulerSupport { get; set; }
        public string RecommendedAction { get; set; } = "";
    }

    public ClanIntrigueState GetState(Clan clan)
    {
        if (clan == null)
        {
            return null;
        }

        EnsureInitialized();
        return _clanStates.TryGetValue(clan, out ClanIntrigueState state) ? state : null;
    }

    public KingdomIntrigueState GetKingdomState(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return null;
        }

        EnsureInitialized();
        return _kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState state) ? state : null;
    }

    public bool HasActivePact(Clan clan)
    {
        EnsureInitialized();
        return HasActivePactInternal(clan);
    }

    public bool HasPlayerPact(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan && x.SponsorClan == Clan.PlayerClan);
    }

    public IntriguePactGoal? GetActivePactGoal(Clan clan)
    {
        EnsureInitialized();
        SecretPact pact = clan == null ? null : _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
        return pact?.Goal;
    }

    public bool HasPendingRumorCampaign(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.TargetClan == clan
            && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
    }

    public bool HasPendingPlayerRumorCampaign(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.InstigatorClan == Clan.PlayerClan
            && x.TargetClan == clan
                && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
    }

    public bool HasActiveEspionageAgainstClan(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _espionageOperations.Any(x =>
            x.Status == EspionageOperationStatus.Pending
            && x.Type == EspionageOperationType.ClanInfiltration
            && x.TargetClan == clan);
    }

    public float GetRemainingEspionageDaysForClan(Clan clan)
    {
        EnsureInitialized();
        EspionageOperation operation = clan == null
            ? null
            : _espionageOperations.FirstOrDefault(x =>
                x.Status == EspionageOperationStatus.Pending
                && x.Type == EspionageOperationType.ClanInfiltration
                && x.TargetClan == clan);

        return operation == null
            ? 0f
            : MathF.Max(0f, (float)(operation.ResolveAt - CampaignTime.Now).ToDays);
    }

    public EspionageReport GetLatestEspionageReportForClan(Clan clan)
    {
        EnsureInitialized();
        return clan == null
            ? null
            : _espionageReports
                .Where(x => x.TargetClan == clan)
                .OrderByDescending(x => x.CreatedAt.ToDays)
                .FirstOrDefault();
    }

    public bool HasActiveEspionageAgainstKingdom(Kingdom kingdom)
    {
        EnsureInitialized();
        return kingdom != null && _espionageOperations.Any(x =>
            x.Status == EspionageOperationStatus.Pending
            && x.Type == EspionageOperationType.CourtListening
            && x.TargetKingdom == kingdom);
    }

    public EspionageReport GetLatestEspionageReportForKingdom(Kingdom kingdom)
    {
        EnsureInitialized();
        return kingdom == null
            ? null
            : _espionageReports
                .Where(x => x.TargetKingdom == kingdom && x.Type == EspionageOperationType.CourtListening)
                .OrderByDescending(x => x.CreatedAt.ToDays)
                .FirstOrDefault();
    }

    public bool IsCompanionAvailableForEspionage(Hero companion)
    {
        EnsureInitialized();
        return CanAssignCompanionToEspionage(companion);
    }

    public bool HasCompanionEspionageActivity(Hero companion)
    {
        EnsureInitialized();
        return companion != null
            && (_espionageOperations.Any(x => x.Status == EspionageOperationStatus.Pending && x.Companion == companion)
                || _espionageReports.Any(x => x.Companion == companion));
    }

    public IEnumerable<Clan> GetAvailableEspionageClanTargets()
    {
        EnsureInitialized();
        return Clan.All
            .Where(IsValidIntrigueClan)
            .Where(x => x != Clan.PlayerClan)
            .OrderBy(x => x.Kingdom?.Name?.ToString())
            .ThenBy(x => x.Name.ToString());
    }

    public IEnumerable<Kingdom> GetAvailableEspionageKingdomTargets()
    {
        EnsureInitialized();
        return Kingdom.All
            .Where(IsValidIntrigueKingdom)
            .Where(x => x != Clan.PlayerClan?.Kingdom)
            .OrderBy(x => x.Name.ToString());
    }

    public bool TryStartClanInfiltration(Hero companion, Clan targetClan, out TextObject response)
    {
        EnsureInitialized();
        response = new TextObject("{=rf_si_espionage_start_failed}The mission cannot be started right now.");

        if (!IsValidIntrigueClan(targetClan))
        {
            response = new TextObject("{=rf_si_espionage_invalid_target}There is no worthwhile clan target here for an infiltration.");
            return false;
        }

        if (!CanAssignCompanionToEspionage(companion))
        {
            response = new TextObject("{=rf_si_espionage_companion_unavailable}That companion is not available to leave the party on a quiet mission.");
            return false;
        }

        if (HasActiveEspionageAgainstClan(targetClan))
        {
            response = new TextObject("{=rf_si_espionage_duplicate}You already have an operative moving against that house.");
            return false;
        }

        float power = CalculateEspionagePower(companion, targetClan, EspionageOperationType.ClanInfiltration);
        float risk = CalculateEspionageRisk(companion, targetClan, EspionageOperationType.ClanInfiltration);
        float durationDays = CalculateEspionageDurationDays(companion, targetClan, EspionageOperationType.ClanInfiltration);
        EspionageOperation operation = new(
            EspionageOperationType.ClanInfiltration,
            companion,
            targetClan,
            targetClan.Kingdom,
            CampaignTime.Now,
            CampaignTime.DaysFromNow(durationDays),
            power,
            risk);
        operation.ClampValues();

        RemoveCompanionForEspionage(companion);
        _espionageOperations.Add(operation);

        response = new TextObject("{=rf_si_espionage_started}{COMPANION} slips away to watch the affairs of {CLAN}. If all goes well, a report should return within about {DAYS} days.");
        response.SetTextVariable("COMPANION", companion.Name);
        response.SetTextVariable("CLAN", targetClan.Name);
        response.SetTextVariable("DAYS", MathF.Round(durationDays));
        return true;
    }

    public bool TryStartCourtListening(Hero companion, Kingdom targetKingdom, out TextObject response)
    {
        EnsureInitialized();
        response = new TextObject("{=rf_si_espionage_start_failed}The mission cannot be started right now.");

        if (!IsValidIntrigueKingdom(targetKingdom))
        {
            response = new TextObject("{=rf_si_espionage_invalid_kingdom}There is no coherent court there worth listening to.");
            return false;
        }

        if (!CanAssignCompanionToEspionage(companion))
        {
            response = new TextObject("{=rf_si_espionage_companion_unavailable}That companion is not available to leave the party on a quiet mission.");
            return false;
        }

        if (HasActiveEspionageAgainstKingdom(targetKingdom))
        {
            response = new TextObject("{=rf_si_espionage_duplicate_kingdom}You already have an operative listening at that court.");
            return false;
        }

        float power = CalculateEspionagePower(companion, targetKingdom.RulingClan, EspionageOperationType.CourtListening);
        float risk = CalculateEspionageRisk(companion, targetKingdom.RulingClan, EspionageOperationType.CourtListening);
        float durationDays = CalculateEspionageDurationDays(companion, targetKingdom.RulingClan, EspionageOperationType.CourtListening);
        EspionageOperation operation = new(
            EspionageOperationType.CourtListening,
            companion,
            null,
            targetKingdom,
            CampaignTime.Now,
            CampaignTime.DaysFromNow(durationDays),
            power,
            risk);
        operation.ClampValues();

        RemoveCompanionForEspionage(companion);
        _espionageOperations.Add(operation);

        response = new TextObject("{=rf_si_espionage_court_started}{COMPANION} slips away to listen at the court of {KINGDOM}. If all goes well, word should return within about {DAYS} days.");
        response.SetTextVariable("COMPANION", companion.Name);
        response.SetTextVariable("KINGDOM", targetKingdom.Name);
        response.SetTextVariable("DAYS", MathF.Round(durationDays));
        return true;
    }

    public TextObject GetEspionageReportBriefingForClan(Clan clan)
    {
        EnsureInitialized();
        if (HasActiveEspionageAgainstClan(clan))
        {
            TextObject pending = new TextObject("{=rf_si_espionage_pending}{COMPANION} is still moving quietly around {CLAN}. Give the agent about {DAYS} more days.");
            EspionageOperation operation = _espionageOperations.FirstOrDefault(x =>
                x.Status == EspionageOperationStatus.Pending
                && x.Type == EspionageOperationType.ClanInfiltration
                && x.TargetClan == clan);
            pending.SetTextVariable("COMPANION", operation?.Companion?.Name ?? new TextObject("{=rf_si_unknown_companion}your agent"));
            pending.SetTextVariable("CLAN", clan?.Name ?? new TextObject("{=rf_si_unknown_clan}the target house"));
            pending.SetTextVariable("DAYS", MathF.Ceiling(GetRemainingEspionageDaysForClan(clan)));
            return pending;
        }

        EspionageReport report = GetLatestEspionageReportForClan(clan);
        if (report == null)
        {
            return new TextObject("{=rf_si_espionage_no_report}No agent has yet returned with anything useful about that house.");
        }

        TextObject text = report.Outcome switch
        {
            EspionageOperationStatus.Exposed => new TextObject("{=rf_si_espionage_report_exposed}{COMPANION} was noticed while probing {CLAN}. Even so, a few scraps survived: the house seems {DISSIDENCE}, its appetite for a claimant is {CLAIMANT}, and the wiser course is {ACTION}. Confidence: {CONFIDENCE}."),
            EspionageOperationStatus.Failed => new TextObject("{=rf_si_espionage_report_failed}{COMPANION} came back empty-handed from {CLAN}. The house remains difficult to read. Confidence: {CONFIDENCE}."),
            EspionageOperationStatus.Partial => new TextObject("{=rf_si_espionage_report_partial}{COMPANION} returned with an incomplete read on {CLAN}: dissidence appears {DISSIDENCE}, fear of the ruler {FEAR}, and the best opening looks like {ACTION}. Confidence: {CONFIDENCE}."),
            _ => new TextObject("{=rf_si_espionage_report_success}{COMPANION} has finished sounding out {CLAN}. Dissidence appears {DISSIDENCE}, fear of the ruler {FEAR}, trust toward your house {TRUST}, and claimant appetite {CLAIMANT}. The recommended next move is {ACTION}. Confidence: {CONFIDENCE}.")
        };

        text.SetTextVariable("COMPANION", report.Companion?.Name ?? new TextObject("{=rf_si_unknown_companion}your agent"));
        text.SetTextVariable("CLAN", report.TargetClan?.Name ?? new TextObject("{=rf_si_unknown_clan}the target house"));
        text.SetTextVariable("DISSIDENCE", DescribeEspionageBand(report.EstimatedDissidence, "fractured", "strained", "steady"));
        text.SetTextVariable("FEAR", DescribeEspionageBand(report.EstimatedFearOfRuler, "high", "mixed", "low"));
        text.SetTextVariable("TRUST", DescribeEspionageBand(report.EstimatedTrustToPlayer, "encouraging", "uncertain", "cold"));
        text.SetTextVariable("CLAIMANT", DescribeEspionageBand(report.EstimatedClaimantAmbition, "strong", "present", "weak"));
        text.SetTextVariable("ACTION", new TextObject(report.RecommendedAction));
        text.SetTextVariable("CONFIDENCE", GetConfidenceText(report.Confidence));
        return text;
    }

    public TextObject GetEspionageReportBriefingForKingdom(Kingdom kingdom)
    {
        EnsureInitialized();
        if (HasActiveEspionageAgainstKingdom(kingdom))
        {
            TextObject pending = new TextObject("{=rf_si_espionage_kingdom_pending}{COMPANION} is still listening at the court of {KINGDOM}. Give the agent about {DAYS} more days.");
            EspionageOperation operation = _espionageOperations.FirstOrDefault(x =>
                x.Status == EspionageOperationStatus.Pending
                && x.Type == EspionageOperationType.CourtListening
                && x.TargetKingdom == kingdom);
            pending.SetTextVariable("COMPANION", operation?.Companion?.Name ?? new TextObject("{=rf_si_unknown_companion}your agent"));
            pending.SetTextVariable("KINGDOM", kingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            pending.SetTextVariable("DAYS", MathF.Ceiling(MathF.Max(0f, (float)((operation?.ResolveAt ?? CampaignTime.Now) - CampaignTime.Now).ToDays)));
            return pending;
        }

        EspionageReport report = GetLatestEspionageReportForKingdom(kingdom);
        if (report == null)
        {
            return new TextObject("{=rf_si_espionage_no_kingdom_report}No agent has yet returned with anything useful about that court.");
        }

        TextObject text = report.Outcome switch
        {
            EspionageOperationStatus.Exposed => new TextObject("{=rf_si_espionage_kingdom_exposed}{COMPANION} was noticed while listening at {KINGDOM}. Even so, the court seems {LEGITIMACY} in legitimacy, {FRACTURE} in cohesion, and the wiser course is {ACTION}. Confidence: {CONFIDENCE}."),
            EspionageOperationStatus.Failed => new TextObject("{=rf_si_espionage_kingdom_failed}{COMPANION} came back empty-handed from {KINGDOM}. The court remains difficult to read. Confidence: {CONFIDENCE}."),
            EspionageOperationStatus.Partial => new TextObject("{=rf_si_espionage_kingdom_partial}{COMPANION} returned with a partial read on {KINGDOM}: legitimacy seems {LEGITIMACY}, claimant pressure {CLAIMANTS}, and the best opening looks like {ACTION}. Confidence: {CONFIDENCE}."),
            _ => new TextObject("{=rf_si_espionage_kingdom_success}{COMPANION} has finished listening at {KINGDOM}. Royal legitimacy seems {LEGITIMACY}, the court is {FRACTURE}, claimant pressure is {CLAIMANTS}, and the recommended next move is {ACTION}. Confidence: {CONFIDENCE}.")
        };

        text.SetTextVariable("COMPANION", report.Companion?.Name ?? new TextObject("{=rf_si_unknown_companion}your agent"));
        text.SetTextVariable("KINGDOM", report.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
        text.SetTextVariable("LEGITIMACY", DescribeEspionageBand(report.EstimatedRoyalLegitimacy, "strong", "contested", "failing"));
        text.SetTextVariable("FRACTURE", DescribeEspionageBand(report.EstimatedCourtFragmentation, "splintering", "strained", "coherent"));
        text.SetTextVariable("CLAIMANTS", DescribeEspionageBand(report.EstimatedClaimantPressure, "dangerous", "restless", "contained"));
        text.SetTextVariable("ACTION", new TextObject(report.RecommendedAction));
        text.SetTextVariable("CONFIDENCE", GetConfidenceText(report.Confidence));
        return text;
    }

    public TextObject GetCompanionEspionageStatus(Hero companion)
    {
        EnsureInitialized();
        if (companion == null)
        {
            return new TextObject("{=rf_si_espionage_no_companion_status}There is no agent to report on.");
        }

        EspionageOperation activeOperation = _espionageOperations
            .FirstOrDefault(x => x.Status == EspionageOperationStatus.Pending && x.Companion == companion);
        if (activeOperation != null)
        {
            return activeOperation.Type == EspionageOperationType.CourtListening
                ? GetEspionageReportBriefingForKingdom(activeOperation.TargetKingdom)
                : GetEspionageReportBriefingForClan(activeOperation.TargetClan);
        }

        EspionageReport report = _espionageReports
            .Where(x => x.Companion == companion)
            .OrderByDescending(x => x.CreatedAt.ToDays)
            .FirstOrDefault();
        if (report == null)
        {
            return new TextObject("{=rf_si_espionage_no_companion_report}I have not returned with any intelligence for you yet.");
        }

        return report.Type == EspionageOperationType.CourtListening
            ? GetEspionageReportBriefingForKingdom(report.TargetKingdom)
            : GetEspionageReportBriefingForClan(report.TargetClan);
    }

    public bool HasKingdomObjective(Kingdom kingdom)
    {
        return GetKingdomState(kingdom)?.ObjectiveType != KingdomObjectiveType.None;
    }

    public bool IsPlayerSupportingObjective(Kingdom kingdom)
    {
        return GetKingdomState(kingdom)?.PlayerSupportsObjective == true;
    }

    public bool IsPlayerSupportingRivalAgenda(Kingdom kingdom)
    {
        return GetKingdomState(kingdom)?.PlayerSupportsRivalAgenda == true;
    }

    public TextObject GetKingdomObjectiveBriefing(Kingdom kingdom)
    {
        EnsureInitialized();
        KingdomIntrigueState state = GetKingdomState(kingdom);
        return state?.PlayerSupportsObjective == true
            ? KingdomObjectiveService.BuildSupportedBriefing(kingdom, state)
            : KingdomObjectiveService.BuildBriefing(kingdom, state);
    }

    public bool TrySupportKingdomObjective(Kingdom kingdom, out TextObject response)
    {
        EnsureInitialized();
        response = TextObject.GetEmpty();

        if (kingdom == null)
        {
            response = new TextObject("{=rf_ko_support_invalid}There is no realm objective to support here.");
            return false;
        }

        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            response = new TextObject("{=rf_ko_support_missing}This realm has not settled on a grand design worth swearing to.");
            return false;
        }

        if (Clan.PlayerClan?.Kingdom != kingdom)
        {
            response = new TextObject("{=rf_ko_support_outside}You can only bind yourself to the grand design of a realm you presently serve.");
            return false;
        }

        if (state.PlayerSupportsObjective)
        {
            response = new TextObject("{=rf_ko_support_already}Your clan is already counted among the backers of that design.");
            return false;
        }

        ClearPlayerObjectiveSupportExcept(kingdom);
        state.PlayerSupportsObjective = true;
        state.PlayerSupportsRivalAgenda = false;
        state.LastObjectiveRewardAt = CampaignTime.Now;

        response = new TextObject("{=rf_ko_support_response}Then let it be known quietly: my clan will put its strength behind {TITLE}.");
        response.SetTextVariable("TITLE", KingdomObjectiveService.GetTitle(state.ObjectiveType));
        return true;
    }

    public bool TrySupportKingdomRivalAgenda(Kingdom kingdom, out TextObject response)
    {
        EnsureInitialized();
        response = TextObject.GetEmpty();

        if (kingdom == null)
        {
            response = new TextObject("{=rf_ko_rival_invalid}There is no rival court line to back here.");
            return false;
        }

        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            response = new TextObject("{=rf_ko_rival_missing}This realm has no settled doctrine to oppose from within.");
            return false;
        }

        if (Clan.PlayerClan?.Kingdom != kingdom)
        {
            response = new TextObject("{=rf_ko_rival_outside}You can only feed a rival agenda inside a realm you presently serve.");
            return false;
        }

        if (state.PlayerSupportsRivalAgenda)
        {
            response = new TextObject("{=rf_ko_rival_already}Your clan is already counted among the quiet backers of the rival court line.");
            return false;
        }

        ClearPlayerObjectiveSupportExcept(kingdom);
        state.PlayerSupportsObjective = false;
        state.PlayerSupportsRivalAgenda = true;
        state.RivalAgendaStrength = MBMath.ClampFloat(state.RivalAgendaStrength + 8f, 0f, 100f);
        state.ObjectivePressure = MBMath.ClampFloat(state.ObjectivePressure + 4f, 0f, 100f);
        state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + 3f, 0f, 100f);
        state.LastObjectiveRewardAt = CampaignTime.Now;

        Hero ruler = kingdom.RulingClan?.Leader;
        if (Hero.MainHero != null && ruler != null && ruler != Hero.MainHero)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, ruler, -2, false);
        }

        response = new TextObject("{=rf_ko_rival_support_response}Very well. My clan will lend quiet strength to {TITLE}, and let the court believe the doctrine has more enemies than it knows.");
        response.SetTextVariable("TITLE", GetRivalAgendaName(kingdom, state));
        return true;
    }

    public TextObject GetKingdomObjectiveWarTableReport(Kingdom kingdom)
    {
        EnsureInitialized();
        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return new TextObject("{=rf_ko_wartable_missing}No settled realm design is being tracked here.");
        }

        RefreshKingdomState(kingdom);
        state = GetKingdomState(kingdom);

        TextObject text = new TextObject("{=rf_ko_wartable_report}{TITLE}\n\n{BRIEFING}\n\nCampaign posture: {POSTURE}\nCourt pressure: {PRESSURE}\nYour standing: {SUPPORT}\nImmediate need: {NEED}");
        text.SetTextVariable("TITLE", KingdomObjectiveService.GetTitle(state.ObjectiveType));
        text.SetTextVariable("BRIEFING", GetKingdomObjectiveBriefing(kingdom));
        text.SetTextVariable("POSTURE", BuildObjectivePostureText(state));
        text.SetTextVariable("PRESSURE", BuildObjectivePressureText(kingdom, state));
        text.SetTextVariable("SUPPORT", BuildObjectiveSupportText(state));
        text.SetTextVariable("NEED", BuildObjectiveNeedText(kingdom, state));
        return text;
    }

    public TextObject GetKingdomRivalAgendaReport(Kingdom kingdom)
    {
        EnsureInitialized();
        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return new TextObject("{=rf_ko_rival_report_missing}There is no meaningful rival line to chart here.");
        }

        RefreshKingdomState(kingdom);
        state = GetKingdomState(kingdom);

        TextObject text = new TextObject("{=rf_ko_rival_report}{TITLE}\n\n{SUMMARY}\n\nBloc strength: {STRENGTH}\nCourt opening: {OPENING}\nYour standing: {SUPPORT}");
        text.SetTextVariable("TITLE", GetRivalAgendaName(kingdom, state));
        text.SetTextVariable("SUMMARY", GetRivalAgendaSummary(kingdom, state));
        text.SetTextVariable("STRENGTH", BuildRivalAgendaStrengthText(state));
        text.SetTextVariable("OPENING", GetRivalAgendaOpeningText(kingdom, state));
        text.SetTextVariable("SUPPORT", state.PlayerSupportsRivalAgenda
            ? new TextObject("{=rf_ko_rival_support_yes}Your clan is already counted among the bloc's discreet supporters.")
            : new TextObject("{=rf_ko_rival_support_no}You have not yet tied your name to this court opposition."));
        return text;
    }

    public TextObject GetKingdomObjectiveCourtReport(Kingdom kingdom)
    {
        EnsureInitialized();
        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return new TextObject("{=rf_ko_court_missing}The court has no clear strategic line to discuss.");
        }

        RefreshKingdomState(kingdom);
        state = GetKingdomState(kingdom);

        TextObject text = new TextObject("{=rf_ko_court_report}Court reading for {KINGDOM}:\n\nRoyal legitimacy: {LEGITIMACY}\nFactional strain: {FRACTURE}\nClaimant danger: {CLAIMANTS}\nRealm unrest: {REBELLION}\nOperational momentum: {MOMENTUM}");
        text.SetTextVariable("KINGDOM", kingdom.Name);
        text.SetTextVariable("LEGITIMACY", DescribeBand(state.RulerLegitimacy, "strong", "contested", "failing"));
        text.SetTextVariable("FRACTURE", DescribeBand(100f - state.CourtFragmentation, "coherent", "strained", "splintering"));
        text.SetTextVariable("CLAIMANTS", DescribeBand(100f - state.ClaimantPressure, "contained", "restless", "dangerous"));
        text.SetTextVariable("REBELLION", DescribeBand(100f - state.RebellionPressure, "quiet", "uneasy", "volatile"));
        text.SetTextVariable("MOMENTUM", BuildObjectiveMomentumText(state));
        return text;
    }

    public TextObject GetKingdomObjectiveDirectivePreview(Kingdom kingdom)
    {
        EnsureInitialized();
        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return new TextObject("{=rf_ko_directive_missing}There is no doctrine-linked court directive available here.");
        }

        int influenceCost = GetObjectiveDirectiveInfluenceCost(state.ObjectiveType);
        int goldCost = GetObjectiveDirectiveGoldCost(state.ObjectiveType);
        float remaining = GetObjectiveDirectiveCooldownRemaining(state);
        TextObject text = new TextObject("{=rf_ko_directive_preview}{NAME}\n\n{DESCRIPTION}\n\nCost: {INFLUENCE} influence and {GOLD} gold.\nCooldown: {COOLDOWN}");
        text.SetTextVariable("NAME", GetObjectiveDirectiveName(state.ObjectiveType));
        text.SetTextVariable("DESCRIPTION", GetObjectiveDirectiveDescription(kingdom, state));
        text.SetTextVariable("INFLUENCE", influenceCost);
        text.SetTextVariable("GOLD", goldCost);
        text.SetTextVariable("COOLDOWN", remaining > 0.05f
            ? new TextObject("{=rf_ko_directive_cooldown_active}{DAYS} days remain before this can be ordered again.")
            : new TextObject("{=rf_ko_directive_cooldown_ready}Ready to be issued now."));
        text.SetTextVariable("DAYS", MathF.Ceiling(remaining));
        return text;
    }

    public bool TryIssueObjectiveDirective(Kingdom kingdom, out TextObject response)
    {
        EnsureInitialized();
        response = TextObject.GetEmpty();

        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            response = new TextObject("{=rf_ko_directive_invalid}There is no active doctrine here to order around.");
            return false;
        }

        if (Clan.PlayerClan?.Kingdom != kingdom)
        {
            response = new TextObject("{=rf_ko_directive_outside}You can only direct the strategy of a realm you presently serve.");
            return false;
        }

        if (!state.PlayerSupportsObjective)
        {
            response = new TextObject("{=rf_ko_directive_nosupport}You have not yet committed your banner to this doctrine, so the court will not move on your word.");
            return false;
        }

        float remainingCooldown = GetObjectiveDirectiveCooldownRemaining(state);
        if (remainingCooldown > 0.05f)
        {
            response = new TextObject("{=rf_ko_directive_cooldown_msg}The court has already acted on your last directive. It will take {DAYS} more days before another can be pressed through.");
            response.SetTextVariable("DAYS", MathF.Ceiling(remainingCooldown));
            return false;
        }

        int influenceCost = GetObjectiveDirectiveInfluenceCost(state.ObjectiveType);
        int goldCost = GetObjectiveDirectiveGoldCost(state.ObjectiveType);
        if (Clan.PlayerClan.Influence < influenceCost)
        {
            response = new TextObject("{=rf_ko_directive_no_influence}You need {INFLUENCE} influence to force this through the court.");
            response.SetTextVariable("INFLUENCE", influenceCost);
            return false;
        }

        if (Hero.MainHero == null || Hero.MainHero.Gold < goldCost)
        {
            response = new TextObject("{=rf_ko_directive_no_gold}You need {GOLD} gold to finance this directive.");
            response.SetTextVariable("GOLD", goldCost);
            return false;
        }

        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -influenceCost);
        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, goldCost, false);
        ApplyObjectiveDirectiveEffects(kingdom, state);
        state.LastObjectiveDirectiveAt = CampaignTime.Now;

        Hero ruler = kingdom.RulingClan?.Leader;
        if (Hero.MainHero != null && ruler != null && ruler != Hero.MainHero)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, ruler, 3, false);
        }

        response = new TextObject("{=rf_ko_directive_issued}{NAME} has been pressed through the court. The realm has begun to move in accordance with your directive.");
        response.SetTextVariable("NAME", GetObjectiveDirectiveName(state.ObjectiveType));
        return true;
    }

    public float? GetPendingRumorDaysRemaining(Clan clan)
    {
        EnsureInitialized();
        IntrigueOperation operation = clan == null
            ? null
            : _pendingOperations.FirstOrDefault(x =>
                x.Status == IntrigueOperationStatus.Pending
                && x.TargetClan == clan
                && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
        if (operation == null)
        {
            return null;
        }

        return MathF.Max(0f, (float)(operation.ResolveAt - CampaignTime.Now).ToDays);
    }

    public float? GetPendingPlayerRumorDaysRemaining(Clan clan)
    {
        EnsureInitialized();
        IntrigueOperation operation = clan == null
            ? null
            : _pendingOperations.FirstOrDefault(x =>
                x.Status == IntrigueOperationStatus.Pending
                && x.InstigatorClan == Clan.PlayerClan
                && x.TargetClan == clan
                && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
        if (operation == null)
        {
            return null;
        }

        return MathF.Max(0f, (float)(operation.ResolveAt - CampaignTime.Now).ToDays);
    }

    public bool TryCreateSecretPact(Clan sponsorClan, Clan memberClan, out TextObject reason)
    {
        EnsureInitialized();
        return ApplySecretPactAction.TryApply(_clanStates, _secretPacts, sponsorClan, memberClan, out reason);
    }

    public SecretAllianceCompact GetPlayerAllianceWithClan(Clan clan)
    {
        EnsureInitialized();
        return _secretAlliances.FirstOrDefault(x =>
            IsAllianceActive(x)
            && x.InstigatorClan == Clan.PlayerClan
            && x.AllyClan == clan
            && x.SupportedClan == clan);
    }

    public SecretAllianceCompact GetPlayerForeignAllianceWithClan(Clan clan)
    {
        EnsureInitialized();
        return _secretAlliances.FirstOrDefault(x =>
            IsAllianceActive(x)
            && x.InstigatorClan == Clan.PlayerClan
            && x.AllyClan == clan
            && x.SupportedClan != clan);
    }

    public Clan GetPreferredPlayerConspiracyTargetForForeignAlly(Clan allyClan)
    {
        EnsureInitialized();
        return GetPreferredPlayerConspiracyTarget(allyClan?.Kingdom);
    }

    public bool TryCreatePlayerAlliance(Clan memberClan, out TextObject reason, out SecretAllianceCompact alliance)
    {
        EnsureInitialized();
        alliance = null;
        reason = TextObject.GetEmpty();

        if (memberClan?.Kingdom == null || memberClan == memberClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=rf_si_alliance_invalid_target}This lord cannot bind a meaningful alliance here.");
            return false;
        }

        SecretPact pact = GetActivePact(memberClan);
        if (pact == null || pact.SponsorClan != Clan.PlayerClan)
        {
            reason = new TextObject("{=rf_si_alliance_requires_pact}A firmer secret understanding must come first.");
            return false;
        }

        if (GetPlayerAllianceWithClan(memberClan) != null)
        {
            reason = new TextObject("{=rf_si_alliance_already_exists}You already have terms with this conspirator.");
            return false;
        }

        if (!_clanStates.TryGetValue(memberClan, out ClanIntrigueState state)
            || state.TrustToPlayer < StrategicIntrigueConstants.AllianceTrustThreshold)
        {
            reason = new TextObject("{=rf_si_alliance_low_trust}They are not ready to tie hard promises to your cause.");
            return false;
        }

        Settlement promisedSettlement = ChoosePromisedSettlement(memberClan.Kingdom, memberClan, Clan.PlayerClan);
        if (promisedSettlement == null)
        {
            reason = new TextObject("{=rf_si_alliance_no_settlement}There is no credible prize to pledge from this realm right now.");
            return false;
        }

        alliance = new SecretAllianceCompact(
            Clan.PlayerClan,
            memberClan,
            memberClan,
            memberClan.Kingdom,
            MapAllianceObjective(pact.Goal),
            IntrigueAllianceRewardType.PromisedSettlement,
            promisedSettlement,
            Clan.PlayerClan,
            CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceDurationDays));
        _secretAlliances.Add(alliance);
        pact.Commitment += 10f;
        pact.Secrecy -= 4f;
        pact.ClampValues();
        state.TrustToPlayer += 6f;
        state.ClampValues();
        return true;
    }

    public bool TryCreateForeignAlliance(Clan allyClan, out TextObject reason, out SecretAllianceCompact alliance)
    {
        EnsureInitialized();
        alliance = null;
        reason = TextObject.GetEmpty();

        if (allyClan?.Kingdom == null || allyClan != allyClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_invalid}Only a ruling clan can promise the weight of a foreign realm.");
            return false;
        }

        if (allyClan == Clan.PlayerClan || allyClan.Kingdom == Clan.PlayerClan?.Kingdom)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_same_realm}That is not a foreign hand to bargain with.");
            return false;
        }

        if (GetPlayerForeignAllianceWithClan(allyClan) != null)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_exists}You already have a discreet understanding with this ruler.");
            return false;
        }

        Clan targetClan = GetPreferredPlayerConspiracyTarget(allyClan.Kingdom);
        SecretPact pact = GetActivePact(targetClan);
        if (targetClan?.Kingdom == null || pact?.SponsorClan != Clan.PlayerClan)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_no_target}You have no live conspiracy worth selling to a foreign ruler.");
            return false;
        }

        Settlement promisedSettlement = ChoosePromisedSettlement(targetClan.Kingdom, targetClan, allyClan);
        if (promisedSettlement == null)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_no_prize}There is no believable border prize to offer for outside help.");
            return false;
        }

        alliance = new SecretAllianceCompact(
            Clan.PlayerClan,
            allyClan,
            targetClan,
            targetClan.Kingdom,
            IntrigueAllianceObjective.ForeignIntervention,
            IntrigueAllianceRewardType.PromisedSettlement,
            promisedSettlement,
            allyClan,
            CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceDurationDays));
        _secretAlliances.Add(alliance);

        if (_clanStates.TryGetValue(targetClan, out ClanIntrigueState targetState))
        {
            targetState.SoftDefectionPressure += 6f;
            targetState.ClaimantAmbition += 4f;
            targetState.ClampValues();
        }

        return true;
    }

    public bool TryStartRumorCampaign(Clan instigatorClan, Clan targetClan, out TextObject reason, float power = 30f, float risk = 25f)
    {
        EnsureInitialized();
        reason = TextObject.GetEmpty();

        if (instigatorClan == null || targetClan == null || targetClan.Kingdom?.RulingClan == null)
        {
            reason = new TextObject("{=si_invalid_rumor_target}There is no valid intrigue target here.");
            return false;
        }

        if (targetClan == targetClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_rumor_target_ruler}You need a dissatisfied vassal, not the ruler directly.");
            return false;
        }

        if (!_clanStates.TryGetValue(targetClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (HasPendingRumorCampaign(targetClan))
        {
            reason = new TextObject("{=si_existing_rumor_campaign}A rumor campaign is already underway for this clan.");
            return false;
        }

        if (state.Dissidence < 45f)
        {
            reason = new TextObject("{=si_rumor_not_needed}This clan is not unsettled enough for whispers to spread.");
            return false;
        }

        if (state.TrustToPlayer < StrategicIntrigueConstants.RumorCampaignTrustThreshold)
        {
            reason = new TextObject("{=si_rumor_low_trust}They do not trust your hand in this matter yet.");
            return false;
        }

        _pendingOperations.Add(new IntrigueOperation(
            IntrigueOperationType.RumorCampaign,
            instigatorClan,
            targetClan,
            targetClan.Kingdom.RulingClan,
            power,
            risk,
            CampaignTime.DaysFromNow(3f)));
        return true;
    }

    public bool TryInciteBreak(Clan targetClan, out TextObject reason, out IntrigueBreakOutcome outcome)
    {
        EnsureInitialized();
        reason = TextObject.GetEmpty();
        outcome = IntrigueBreakOutcome.None;

        if (targetClan?.Kingdom == null || targetClan == targetClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_invalid_break_target}This clan cannot be pushed into open rebellion.");
            return false;
        }

        if (!HasActivePact(targetClan))
        {
            reason = new TextObject("{=si_missing_pact_break}You need a secret pact before attempting a break.");
            return false;
        }

        if (!_clanStates.TryGetValue(targetClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (!state.IsBreakawayReady)
        {
            reason = new TextObject("{=si_breakaway_not_ready}The clan is not angry enough to abandon the realm.");
            return false;
        }

        if (state.TrustToPlayer < StrategicIntrigueConstants.SecretPactTrustThreshold)
        {
            reason = new TextObject("{=si_breakaway_low_trust}They still distrust your timing.");
            return false;
        }

        IntriguePactGoal? goal = GetActivePactGoal(targetClan);
        if (goal == IntriguePactGoal.SupportFutureClaimant && !CanTriggerClaimantCoup(targetClan, out reason))
        {
            return false;
        }

        outcome = ApplyInciteBreakAction.Apply(_clanStates, _kingdomStates, _secretPacts, targetClan);
        if (outcome == IntrigueBreakOutcome.None)
        {
            reason = new TextObject("{=si_breakaway_failed}The breakaway attempt fizzled before it began.");
            return false;
        }

        ProcessAllianceBreakOutcome(targetClan, outcome);
        return true;
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        EnsureInitialized();
        AddWarTableMenus(starter);
    }

    private void AddWarTableMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu(
            "rf_realm_war_table",
            "{=rf_ko_wartable_menu}Maps, ledgers, and sealed reports lie across the chamber. This is where the realm's greater design is weighed against its real condition.",
            null,
            GameMenu.MenuOverlayType.SettlementWithBoth);

        starter.AddGameMenuOption(
            "town_keep",
            "rf_realm_war_table_open_town",
            "{=rf_ko_wartable_open}Review the realm's war table",
            CanOpenWarTableFromKeep,
            OpenWarTable,
            false,
            6,
            false);

        starter.AddGameMenuOption(
            "castle",
            "rf_realm_war_table_open_castle",
            "{=rf_ko_wartable_open}Review the realm's war table",
            CanOpenWarTableFromCastle,
            OpenWarTable,
            false,
            6,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_briefing",
            "{=rf_ko_wartable_briefing}Hear the court's strategic briefing",
            CanUseWarTable,
            ShowWarTableBriefing,
            false,
            1,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_court",
            "{=rf_ko_wartable_court}Review the court's pressure report",
            CanUseWarTable,
            ShowWarTableCourtReading,
            false,
            2,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_directive_preview",
            "{=rf_ko_wartable_directive_preview}Review a doctrine directive",
            CanReviewObjectiveDirectiveFromWarTable,
            ShowObjectiveDirectivePreview,
            false,
            3,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_directive_issue",
            "{=rf_ko_wartable_directive_issue}Issue a doctrine directive",
            CanIssueObjectiveDirectiveFromWarTable,
            IssueObjectiveDirectiveFromWarTable,
            false,
            4,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_rival_report",
            "{=rf_ko_wartable_rival_report}Review the rival court agenda",
            CanReviewRivalAgendaFromWarTable,
            ShowRivalAgendaFromWarTable,
            false,
            5,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_rival_support",
            "{=rf_ko_wartable_rival_support}Quietly back the rival agenda",
            CanSupportRivalAgendaFromWarTable,
            SupportRivalAgendaFromWarTable,
            false,
            6,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_support",
            "{=rf_ko_wartable_support}Commit your banner to this design",
            CanSupportObjectiveFromWarTable,
            SupportObjectiveFromWarTable,
            false,
            7,
            false);

        starter.AddGameMenuOption(
            "rf_realm_war_table",
            "rf_realm_war_table_leave",
            "{=rf_ko_wartable_leave}Step away from the war table",
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            },
            LeaveWarTable,
            false,
            99,
            false);
    }

    private bool CanOpenWarTableFromKeep(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        _warTableReturnMenuId = "town_keep";
        return CanAccessWarTable();
    }

    private bool CanOpenWarTableFromCastle(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
        _warTableReturnMenuId = "castle";
        return CanAccessWarTable();
    }

    private void OpenWarTable(MenuCallbackArgs args)
    {
        GameMenu.SwitchToMenu("rf_realm_war_table");
    }

    private bool CanUseWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        return CanAccessWarTable();
    }

    private bool CanSupportObjectiveFromWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        return CanAccessWarTable()
            && kingdom != null
            && HasKingdomObjective(kingdom)
            && !IsPlayerSupportingObjective(kingdom);
    }

    private bool CanReviewObjectiveDirectiveFromWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        return CanAccessWarTable() && kingdom != null && HasKingdomObjective(kingdom);
    }

    private bool CanIssueObjectiveDirectiveFromWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        return CanAccessWarTable()
            && kingdom != null
            && HasKingdomObjective(kingdom)
            && IsPlayerSupportingObjective(kingdom);
    }

    private bool CanReviewRivalAgendaFromWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        return CanAccessWarTable() && kingdom != null && HasKingdomObjective(kingdom);
    }

    private bool CanSupportRivalAgendaFromWarTable(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        return CanAccessWarTable()
            && kingdom != null
            && HasKingdomObjective(kingdom)
            && !IsPlayerSupportingRivalAgenda(kingdom);
    }

    private void ShowWarTableBriefing(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        ShowIntrigueInquiry(
            new TextObject("{=rf_ko_wartable_briefing_title}Realm War Table"),
            GetKingdomObjectiveWarTableReport(kingdom));
    }

    private void ShowWarTableCourtReading(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        ShowIntrigueInquiry(
            new TextObject("{=rf_ko_wartable_court_title}Court Pressure Report"),
            GetKingdomObjectiveCourtReport(kingdom));
    }

    private void ShowObjectiveDirectivePreview(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        ShowIntrigueInquiry(
            new TextObject("{=rf_ko_wartable_directive_title}Doctrine Directive"),
            GetKingdomObjectiveDirectivePreview(kingdom));
    }

    private void ShowRivalAgendaFromWarTable(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        ShowIntrigueInquiry(
            new TextObject("{=rf_ko_wartable_rival_title}Rival Court Agenda"),
            GetKingdomRivalAgendaReport(kingdom));
    }

    private void SupportObjectiveFromWarTable(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        if (TrySupportKingdomObjective(kingdom, out TextObject response))
        {
            ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_support_title}Banner Committed"), response);
            return;
        }

        ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_support_refused}Commitment Refused"), response);
    }

    private void IssueObjectiveDirectiveFromWarTable(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        if (TryIssueObjectiveDirective(kingdom, out TextObject response))
        {
            ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_directive_issued_title}Directive Issued"), response);
            return;
        }

        ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_directive_refused_title}Directive Refused"), response);
    }

    private void SupportRivalAgendaFromWarTable(MenuCallbackArgs args)
    {
        Kingdom kingdom = Clan.PlayerClan?.Kingdom;
        if (TrySupportKingdomRivalAgenda(kingdom, out TextObject response))
        {
            ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_rival_support_title}Rival Agenda Backed"), response);
            return;
        }

        ShowIntrigueInquiry(new TextObject("{=rf_ko_wartable_rival_support_refused}Rival Backing Refused"), response);
    }

    private void LeaveWarTable(MenuCallbackArgs args)
    {
        GameMenu.SwitchToMenu(_warTableReturnMenuId);
    }

    private bool CanAccessWarTable()
    {
        Settlement settlement = Settlement.CurrentSettlement;
        Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
        return settlement != null
            && playerKingdom != null
            && settlement.OwnerClan?.Kingdom == playerKingdom
            && HasKingdomObjective(playerKingdom);
    }

    private static TextObject BuildObjectivePostureText(KingdomIntrigueState state)
    {
        if (state == null)
        {
            return new TextObject("{=rf_ko_posture_unknown}unclear.");
        }

        return state.ObjectiveMilestone switch
        {
            4 => new TextObject("{=rf_ko_posture_4}The doctrine is dominating court thought and shaping the realm's decisions."),
            3 => new TextObject("{=rf_ko_posture_3}The doctrine is close to maturity, and the court believes it can be turned into lasting advantage."),
            2 => new TextObject("{=rf_ko_posture_2}The doctrine has real force behind it, but its enemies and doubters are not yet beaten."),
            1 => new TextObject("{=rf_ko_posture_1}The doctrine has taken root, though it still needs victories and discipline."),
            _ => new TextObject("{=rf_ko_posture_0}The doctrine is still more ambition than settled reality.")
        };
    }

    private static TextObject GetObjectiveDirectiveName(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance => new TextObject("{=rf_ko_directive_name_sturgia}Northern Suppression Edict"),
            KingdomObjectiveType.NobleWealthSupremacy => new TextObject("{=rf_ko_directive_name_vlandia}House Charter and Tax Drive"),
            KingdomObjectiveType.PreserveBattanianHomelands => new TextObject("{=rf_ko_directive_name_battania}Homeland Muster"),
            KingdomObjectiveType.UniteAseraiRealms => new TextObject("{=rf_ko_directive_name_aserai}Desert Concord Offensive"),
            KingdomObjectiveType.ClaimImperialLegitimacy => new TextObject("{=rf_ko_directive_name_empire}Imperial Legitimacy Campaign"),
            KingdomObjectiveType.ForgeBorderEmpire => new TextObject("{=rf_ko_directive_name_khuzait}Border Khanate Offensive"),
            KingdomObjectiveType.ArcaneFrontier => new TextObject("{=rf_ko_directive_name_mage}Frontier Seizure Mandate"),
            KingdomObjectiveType.SecureMountainHolds => new TextObject("{=rf_ko_directive_name_dwarf}Holdfast Reinforcement Order"),
            KingdomObjectiveType.DefileMountainHolds => new TextObject("{=rf_ko_directive_name_urkhai}Black Siege Decree"),
            KingdomObjectiveType.MartialGlory => new TextObject("{=rf_ko_directive_name_wulf}Warrior Muster"),
            KingdomObjectiveType.UnbreakableRealm => new TextObject("{=rf_ko_directive_name_grimwatch}Iron Bastion Program"),
            KingdomObjectiveType.GuardianFrenzy => new TextObject("{=rf_ko_directive_name_giant}Vigil of the Peaks"),
            KingdomObjectiveType.MercenaryCreed => new TextObject("{=rf_ko_directive_name_mercenary}Muster Rolls and Coffers"),
            KingdomObjectiveType.WovenAlliances => new TextObject("{=rf_ko_directive_name_valthorne}Envoys and Oaths"),
            KingdomObjectiveType.ColonialExpansion => new TextObject("{=rf_ko_directive_name_nord}Frontier Charter"),
            _ => new TextObject("{=rf_ko_directive_name_generic}Grand Court Directive")
        };
    }

    private TextObject GetObjectiveDirectiveDescription(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject("{=rf_ko_directive_desc_none}There is no coherent court action prepared.");
        }

        bool activeFront = HasObjectiveTargetWar(kingdom, state.ObjectiveType);
        return state.ObjectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance =>
                new TextObject(activeFront
                    ? "{=rf_ko_directive_desc_sturgia_war}Order the northern nobles to fund scouts, harden the frontier, and press every campaign gain against Battania."
                    : "{=rf_ko_directive_desc_sturgia_peace}Order the northern nobles to gather scouts, stock supplies, and prepare the next crushing campaign against Battania."),
            KingdomObjectiveType.NobleWealthSupremacy =>
                new TextObject("{=rf_ko_directive_desc_vlandia}Redirect court effort into tariffs, noble credit, and estate privilege so the great houses grow richer and more invested in the realm."),
            KingdomObjectiveType.PreserveBattanianHomelands =>
                new TextObject("{=rf_ko_directive_desc_battania}Call the clans to secure the forests, steady threatened settlements, and put homeland defense above private disputes."),
            KingdomObjectiveType.UniteAseraiRealms =>
                new TextObject("{=rf_ko_directive_desc_aserai}Push the court toward a desert-wide campaign of legitimacy, pressure, and negotiated submission against rival Aserai thrones."),
            KingdomObjectiveType.ClaimImperialLegitimacy =>
                new TextObject("{=rf_ko_directive_desc_empire}Mobilize scribes, governors, and loyal houses to hammer home the claim that only this court has the right to rule the Empire."),
            KingdomObjectiveType.ForgeBorderEmpire =>
                new TextObject(activeFront
                    ? "{=rf_ko_directive_desc_khuzait_war}Drive the border war harder, rewarding speed, cavalry victories, and relentless pressure on the frontier kingdoms."
                    : "{=rf_ko_directive_desc_khuzait_peace}Prepare the steppe for the next border war, gathering horse-levies and sharpening the court's appetite for conquest."),
            KingdomObjectiveType.ArcaneFrontier =>
                new TextObject("{=rf_ko_directive_desc_mage}Commit court resources to seizing and reorganizing the frontier so arcane authority takes root in contested lands."),
            KingdomObjectiveType.SecureMountainHolds =>
                new TextObject("{=rf_ko_directive_desc_dwarf}Spend heavily on fortified lines, drilled garrisons, and coordinated hold defense to deny every Urkhai assault."),
            KingdomObjectiveType.DefileMountainHolds =>
                new TextObject("{=rf_ko_directive_desc_urkhai}Concentrate brutal siege effort and raiding strength so dwarven holds crack under sustained terror and assault."),
            KingdomObjectiveType.MartialGlory =>
                new TextObject("{=rf_ko_directive_desc_wulf}Order the warrior houses into an aggressive muster, privileging battle-readiness, renown, and public demonstrations of strength."),
            KingdomObjectiveType.UnbreakableRealm =>
                new TextObject("{=rf_ko_directive_desc_grimwatch}Direct the court to pour labor and coin into walls, discipline, and settlement resilience until the realm feels unassailable."),
            KingdomObjectiveType.GuardianFrenzy =>
                new TextObject(activeFront
                    ? "{=rf_ko_directive_desc_giant_war}Loose the wrath the aggressor called down: no ground given, no rest taken, until the offense is answered in full."
                    : "{=rf_ko_directive_desc_giant_peace}Set the watch on every pass and slope, so that whoever tries the giants' lands finds them awake and unwelcoming."),
            KingdomObjectiveType.MercenaryCreed =>
                new TextObject("{=rf_ko_directive_desc_mercenary}Put the court to the ledgers and the muster rolls: coffers full, walls manned, contracts honoured, and no crown owed a thing."),
            KingdomObjectiveType.WovenAlliances =>
                new TextObject("{=rf_ko_directive_desc_valthorne}Send the envoys out with gifts and lent swords, so that every friend made is a war Valthorne will not have to fight alone."),
            KingdomObjectiveType.ColonialExpansion =>
                new TextObject(activeFront
                    ? "{=rf_ko_directive_desc_nord_war}Press the campaign and turn what is taken into settled colony, not a raided ruin left behind."
                    : "{=rf_ko_directive_desc_nord_peace}Ready the ships and the charters: the colonies want new land marked out before another season passes."),
            _ => new TextObject("{=rf_ko_directive_desc_generic}The court will concentrate its effort behind the realm's declared doctrine.")
        };
    }

    private static int GetObjectiveDirectiveInfluenceCost(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            KingdomObjectiveType.UniteAseraiRealms or KingdomObjectiveType.ClaimImperialLegitimacy
                => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseInfluenceCost + 6,
            KingdomObjectiveType.NobleWealthSupremacy or KingdomObjectiveType.UnbreakableRealm
                => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseInfluenceCost + 4,
            KingdomObjectiveType.MartialGlory or KingdomObjectiveType.PreserveBattanianHomelands
                => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseInfluenceCost - 2,
            _ => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseInfluenceCost
        };
    }

    private static int GetObjectiveDirectiveGoldCost(KingdomObjectiveType objectiveType)
    {
        return objectiveType switch
        {
            KingdomObjectiveType.NobleWealthSupremacy => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseGoldCost + 900,
            KingdomObjectiveType.UnbreakableRealm or KingdomObjectiveType.SecureMountainHolds
                => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseGoldCost + 700,
            KingdomObjectiveType.UniteAseraiRealms or KingdomObjectiveType.ClaimImperialLegitimacy
                => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseGoldCost + 500,
            KingdomObjectiveType.MartialGlory => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseGoldCost - 400,
            _ => StrategicIntrigueConstants.KingdomObjectiveDirectiveBaseGoldCost
        };
    }

    private static float GetObjectiveDirectiveCooldownRemaining(KingdomIntrigueState state)
    {
        if (state == null || state.LastObjectiveDirectiveAt == CampaignTime.Zero)
        {
            return 0f;
        }

        return MathF.Max(
            0f,
            StrategicIntrigueConstants.KingdomObjectiveDirectiveCooldownDays - (float)(CampaignTime.Now - state.LastObjectiveDirectiveAt).ToDays);
    }

    private static TextObject BuildObjectiveSupportText(KingdomIntrigueState state)
    {
        if (state?.PlayerSupportsObjective == true)
        {
            return new TextObject("{=rf_ko_support_status_yes}Your clan is already counted among the realm's committed backers.");
        }

        if (state?.PlayerSupportsRivalAgenda == true)
        {
            return new TextObject("{=rf_ko_support_status_rival}Your clan is quietly aligned with the court's rival bloc rather than the declared doctrine.");
        }

        return new TextObject("{=rf_ko_support_status_no}Your clan has not yet formally bound itself to this doctrine.");
    }

    private static TextObject GetRivalAgendaName(Kingdom kingdom, KingdomIntrigueState state)
    {
        KingdomObjectiveType objectiveType = state?.ObjectiveType ?? KingdomObjectiveType.None;
        return objectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance => new TextObject("{=rf_ko_rival_name_sturgia}Boyar Restraint League"),
            KingdomObjectiveType.NobleWealthSupremacy => new TextObject("{=rf_ko_rival_name_vlandia}Royal Monopoly Circle"),
            KingdomObjectiveType.PreserveBattanianHomelands => new TextObject("{=rf_ko_rival_name_battania}Clan Autonomy Compact"),
            KingdomObjectiveType.UniteAseraiRealms => new TextObject("{=rf_ko_rival_name_aserai}League of Independent Emirs"),
            KingdomObjectiveType.ClaimImperialLegitimacy => new TextObject("{=rf_ko_rival_name_empire}Provincial Claimant Bloc"),
            KingdomObjectiveType.ForgeBorderEmpire => new TextObject("{=rf_ko_rival_name_khuzait}Council of Steppe Prudence"),
            KingdomObjectiveType.ArcaneFrontier => new TextObject("{=rf_ko_rival_name_mage}Inner Realm Preservation Circle"),
            KingdomObjectiveType.SecureMountainHolds => new TextObject("{=rf_ko_rival_name_dwarf}High Hold Autonomy Bloc"),
            KingdomObjectiveType.DefileMountainHolds => new TextObject("{=rf_ko_rival_name_urkhai}Warchief Claimants"),
            KingdomObjectiveType.MartialGlory => new TextObject("{=rf_ko_rival_name_wulf}High Seat Claimants"),
            KingdomObjectiveType.UnbreakableRealm => new TextObject("{=rf_ko_rival_name_grimwatch}Marcher War Party"),
            KingdomObjectiveType.GuardianFrenzy => new TextObject("{=rf_ko_rival_name_giant}Elders of the Long Sleep"),
            KingdomObjectiveType.MercenaryCreed => new TextObject("{=rf_ko_rival_name_mercenary}Banner-Sworn Faction"),
            KingdomObjectiveType.WovenAlliances => new TextObject("{=rf_ko_rival_name_valthorne}Isolationist Circle"),
            KingdomObjectiveType.ColonialExpansion => new TextObject("{=rf_ko_rival_name_nord}Old Country Party"),
            _ => kingdom != null
                ? new TextObject("{=rf_ko_rival_name_generic}Dissident Court Bloc")
                : new TextObject("{=rf_ko_rival_name_none}No Rival Agenda")
        };
    }

    private TextObject GetRivalAgendaSummary(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject("{=rf_ko_rival_summary_none}No coherent rival line has formed.");
        }

        bool activeFront = HasObjectiveTargetWar(kingdom, state.ObjectiveType);
        return state.ObjectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance =>
                new TextObject(activeFront
                    ? "{=rf_ko_rival_summary_sturgia_war}Some boyars argue the crown is spending northern blood too freely and would rather rein in the frontier than deepen the forest war."
                    : "{=rf_ko_rival_summary_sturgia_peace}Some boyars are trying to keep the realm from being dragged into another costly forest war before the frontier is ready."),
            KingdomObjectiveType.NobleWealthSupremacy =>
                new TextObject("{=rf_ko_rival_summary_vlandia}A rival bloc wants wealth drawn back under the crown instead of allowing great houses to become untouchable in their own right."),
            KingdomObjectiveType.PreserveBattanianHomelands =>
                new TextObject("{=rf_ko_rival_summary_battania}A quieter current among the clans prefers local autonomy and survival bargains over a single central strategy for the woods."),
            KingdomObjectiveType.UniteAseraiRealms =>
                new TextObject("{=rf_ko_rival_summary_aserai}Independent emirs are resisting any dream of desert unification that would turn them into mere servants of one throne."),
            KingdomObjectiveType.ClaimImperialLegitimacy =>
                new TextObject("{=rf_ko_rival_summary_empire}Provincial lords and claimant sympathizers are testing whether the court's claim to lawful empire can be broken from within."),
            KingdomObjectiveType.ForgeBorderEmpire =>
                new TextObject(activeFront
                    ? "{=rf_ko_rival_summary_khuzait_war}A cautious border faction thinks endless frontier war will overstrain the khanate before the gains can be secured."
                    : "{=rf_ko_rival_summary_khuzait_peace}A cautious border faction prefers consolidation, tribute, and patience over rushing into the next frontier conquest."),
            KingdomObjectiveType.ArcaneFrontier =>
                new TextObject("{=rf_ko_rival_summary_mage}Some court circles would rather protect the inner realm and magical order than keep spending strength on a dangerous frontier seizure."),
            KingdomObjectiveType.SecureMountainHolds =>
                new TextObject("{=rf_ko_rival_summary_dwarf}A bloc of great holds wants more autonomy and less crown direction, trusting each fortress to guard its own fate."),
            KingdomObjectiveType.DefileMountainHolds =>
                new TextObject("{=rf_ko_rival_summary_urkhai}Ambitious war chiefs would rather fight over prestige and leadership than let a single campaign line define the whole horde."),
            KingdomObjectiveType.MartialGlory =>
                new TextObject("{=rf_ko_rival_summary_wulf}When glory is scarce, rival champions begin to imagine replacing the present high command with a stronger war leader."),
            KingdomObjectiveType.UnbreakableRealm =>
                new TextObject("{=rf_ko_rival_summary_grimwatch}A marcher war party is starting to argue that a realm built only on walls will eventually be strangled unless it takes the fight outward."),
            KingdomObjectiveType.GuardianFrenzy =>
                new TextObject(activeFront
                    ? "{=rf_ko_rival_summary_giant_war}Older voices want the fury called off before the giants forget how to stop, and become the very thing they wake to punish."
                    : "{=rf_ko_rival_summary_giant_peace}Some elders would rather sleep deeper still, arguing that even watching the passes invites the world to bother them."),
            KingdomObjectiveType.MercenaryCreed =>
                new TextObject("{=rf_ko_rival_summary_mercenary}A faction is tired of selling swords and wants the realm to swear to a crown at last, trading independence for a place at someone's table."),
            KingdomObjectiveType.WovenAlliances =>
                new TextObject("{=rf_ko_rival_summary_valthorne}An isolationist circle argues that every oath lent is a war borrowed, and that Valthorne should owe nothing to anyone beyond its own borders."),
            KingdomObjectiveType.ColonialExpansion =>
                new TextObject("{=rf_ko_rival_summary_nord}An old-country party wants the colonies to stop swallowing land they cannot hold and consolidate what has already been settled."),
            _ => new TextObject("{=rf_ko_rival_summary_generic}A dissatisfied bloc inside the court is looking for a different path than the crown's declared design.")
        };
    }

    private static TextObject BuildRivalAgendaStrengthText(KingdomIntrigueState state)
    {
        float strength = state?.RivalAgendaStrength ?? 0f;
        return strength switch
        {
            >= 75f => new TextObject("{=rf_ko_rival_strength_3}Severe. The rival line is becoming a true faction of court politics."),
            >= 45f => new TextObject("{=rf_ko_rival_strength_2}Significant. Enough nobles are entertaining it that the crown cannot dismiss it outright."),
            >= 20f => new TextObject("{=rf_ko_rival_strength_1}Growing. It is no longer just grumbling in private halls."),
            _ => new TextObject("{=rf_ko_rival_strength_0}Faint. The opposition exists, but it is still scattered and cautious.")
        };
    }

    private TextObject GetRivalAgendaOpeningText(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject("{=rf_ko_rival_opening_none}No clear opening is visible.");
        }

        if (state.ObjectivePressure >= 70f || state.CourtFragmentation >= 70f)
        {
            return new TextObject("{=rf_ko_rival_opening_high}The doctrine is under strain and the court is badly divided. This is the sort of atmosphere in which a rival line can seize real ground.");
        }

        if (state.RulerLegitimacy <= 40f || state.ClaimantPressure >= 55f)
        {
            return new TextObject("{=rf_ko_rival_opening_mid}The crown looks vulnerable enough that wavering lords may listen to a disciplined opposition.");
        }

        return new TextObject("{=rf_ko_rival_opening_low}The rival bloc is watching for failure, but the court has not yet opened wide enough for a decisive push.");
    }

    private TextObject BuildObjectivePressureText(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (state == null)
        {
            return new TextObject("{=rf_ko_pressure_unknown}The court has no confident reading.");
        }

        if (state.ObjectivePressure >= 75f)
        {
            return new TextObject("{=rf_ko_pressure_high}Severe. The court feels the design is being denied and is starting to fracture over it.");
        }

        if (state.ObjectivePressure >= 45f)
        {
            return new TextObject("{=rf_ko_pressure_mid}Noticeable. Support remains, but nobles are beginning to ask whether the crown can still deliver.");
        }

        if (IsExpansionistObjective(state.ObjectiveType) && !HasObjectiveTargetWar(kingdom, state.ObjectiveType))
        {
            return new TextObject("{=rf_ko_pressure_idle}Contained, but restless. The realm still hungers for a campaign worthy of its doctrine.");
        }

        return new TextObject("{=rf_ko_pressure_low}Manageable. The doctrine is not presently tearing the court apart.");
    }

    private TextObject BuildObjectiveMomentumText(KingdomIntrigueState state)
    {
        if (state == null)
        {
            return new TextObject("{=rf_ko_momentum_unknown}unclear");
        }

        return state.ObjectiveMomentum switch
        {
            >= 8f => new TextObject("{=rf_ko_momentum_surging}surging"),
            >= 2f => new TextObject("{=rf_ko_momentum_gaining}gaining ground"),
            <= -8f => new TextObject("{=rf_ko_momentum_falling}falling apart"),
            <= -2f => new TextObject("{=rf_ko_momentum_stalling}stalling"),
            _ => new TextObject("{=rf_ko_momentum_even}holding steady")
        };
    }

    private TextObject BuildObjectiveNeedText(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject("{=rf_ko_need_unknown}The realm has no immediate reading.");
        }

        return state.ObjectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance => HasObjectiveTargetWar(kingdom, state.ObjectiveType)
                ? new TextObject("{=rf_ko_need_sturgia_war}Break Battanian resistance in battle and hold more of the northern forests.")
                : new TextObject("{=rf_ko_need_sturgia_nowar}The court wants a renewed campaign against Battania rather than another season of delay."),
            KingdomObjectiveType.NobleWealthSupremacy => new TextObject("{=rf_ko_need_vlandia}Richer towns, stronger noble houses, and fewer signs of house-level discontent."),
            KingdomObjectiveType.PreserveBattanianHomelands => new TextObject("{=rf_ko_need_battania}Secure the forest heartland, hold the old towns, and keep invaders from making the woods feel lost."),
            KingdomObjectiveType.UniteAseraiRealms => new TextObject("{=rf_ko_need_aserai}Reduce rival desert crowns and prove that one Aserai power can gather the sands under itself."),
            KingdomObjectiveType.ClaimImperialLegitimacy => new TextObject("{=rf_ko_need_empire}Break rival imperial claimants and make the court look like the only lawful center of rule."),
            KingdomObjectiveType.ForgeBorderEmpire => new TextObject("{=rf_ko_need_khuzait}Push the frontier outward through real border victories against Sturgia and the imperial realms."),
            KingdomObjectiveType.ArcaneFrontier => new TextObject("{=rf_ko_need_mage}Seize more of Battania and turn those holdings into a stable arcane frontier instead of a thin occupation."),
            KingdomObjectiveType.SecureMountainHolds => new TextObject("{=rf_ko_need_dwarf}Cripple Urkhai pressure and make the holds feel permanently secure again."),
            KingdomObjectiveType.DefileMountainHolds => new TextObject("{=rf_ko_need_urkhai}Shatter dwarf defenses and prove that even their mountain strongholds can be broken."),
            KingdomObjectiveType.MartialGlory => HasObjectiveTargetWar(kingdom, state.ObjectiveType)
                ? new TextObject("{=rf_ko_need_wulf_war}Keep winning hard battles and do not let warrior prestige cool.")
                : new TextObject("{=rf_ko_need_wulf_nowar}The warrior lords want a worthy war. Peace without glory is starting to sour the realm."),
            KingdomObjectiveType.UnbreakableRealm => new TextObject("{=rf_ko_need_grimwatch}Stronger garrisons, steadier walls, and settlements that can survive shame-free through siege."),
            KingdomObjectiveType.GuardianFrenzy => HasObjectiveTargetWar(kingdom, state.ObjectiveType)
                ? new TextObject("{=rf_ko_need_giant_war}The aggressor must be broken. Nothing else will settle the wrath that was woken.")
                : new TextObject("{=rf_ko_need_giant_peace}Nothing beyond the giants' own lands, held quiet and held safe."),
            KingdomObjectiveType.MercenaryCreed => new TextObject("{=rf_ko_need_mercenary}Full coffers, manned walls, and no crown with a claim on the realm."),
            KingdomObjectiveType.WovenAlliances => new TextObject("{=rf_ko_need_valthorne}Fewer enemies, more realms fighting beside us, and never a war fought alone."),
            KingdomObjectiveType.ColonialExpansion => HasObjectiveTargetWar(kingdom, state.ObjectiveType)
                ? new TextObject("{=rf_ko_need_nord_war}Turn the campaign into settled ground: more holdings, held and colonised.")
                : new TextObject("{=rf_ko_need_nord_nowar}The colonies want new land, and no season of peace has ever marked out a frontier."),
            _ => new TextObject("{=rf_ko_need_default}The realm needs proof that its doctrine can still shape events.")
        };
    }

    private static TextObject DescribeBand(float value, string highKey, string midKey, string lowKey)
    {
        string choice = value switch
        {
            >= 67f => highKey,
            >= 38f => midKey,
            _ => lowKey
        };

        return new TextObject(choice);
    }

    private void OnDailyTickClan(Clan clan)
    {
        if (!IsValidIntrigueClan(clan))
        {
            return;
        }

        EnsureInitialized();
        RefreshKingdomState(clan.Kingdom);

        ClanIntrigueState state = GetOrCreateState(clan);
        RefreshTrustToPlayer(state);
        RefreshDerivedClanState(state);
        ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
    }

    private void OnDailyTick()
    {
        EnsureInitialized();
        Dictionary<Clan, bool> breakawayReadinessBeforeTick = CaptureActivePactBreakawayReadiness();
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsObjectiveCapableKingdom(kingdom))
            {
                continue;
            }

            RefreshKingdomState(kingdom);

            // Exactly once per realm per day, and only from here. RefreshKingdomState
            // also runs per-clan and out of report getters; pushing from inside it
            // filed one war request per vassal per day, and RegisterPendingWar
            // escalates intensity and support on every repeat — a large court would
            // saturate the request to maximum urgency on its first day.
            PushGrandDesignWarIntent(kingdom, GetOrCreateKingdomState(kingdom));
        }

        foreach (Clan clan in Clan.All)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
        }

        DecayDailyValues();
        ResolveEspionageOperations();
        ProcessRulerCountermoves();
        ScheduleAutomaticEscalations();
        List<IntrigueOperationResolution> resolutions = IntrigueOperationResolver.ResolveDueOperations(_clanStates, _kingdomStates, _secretPacts, _pendingOperations);
        ProcessAllianceBreakOutcomes(resolutions);
        ShowOperationResolutionNotifications(resolutions);
        ShowNewBreakawayReadyNotifications(
            breakawayReadinessBeforeTick,
            resolutions
                .Where(x => x.BecameBreakawayReady && x.TargetClan != null)
                .Select(x => x.TargetClan)
                .ToHashSet());
        GenerateOrganicIntrigueMoves();
        ProcessAllianceDeadlines();
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (mapEvent == null || mapEvent.BattleState == BattleState.None)
        {
            return;
        }

        Kingdom attacker = mapEvent.AttackerSide.LeaderParty?.MapFaction as Kingdom;
        Kingdom defender = mapEvent.DefenderSide.LeaderParty?.MapFaction as Kingdom;
        if (attacker == null || defender == null)
        {
            return;
        }

        Kingdom winner = mapEvent.BattleState == BattleState.AttackerVictory ? attacker : defender;
        Kingdom loser = winner == attacker ? defender : attacker;
        if (!IsValidIntrigueKingdom(winner))
        {
            return;
        }

        KingdomIntrigueState winnerState = GetOrCreateKingdomState(winner);
        float glory = 5f;
        switch (winnerState.ObjectiveType)
        {
            case KingdomObjectiveType.CrushBattanianResistance when loser.StringId == "battania":
            case KingdomObjectiveType.ArcaneFrontier when loser.StringId == "battania":
            case KingdomObjectiveType.SecureMountainHolds when loser.StringId == "urkhai_kingdom":
            case KingdomObjectiveType.DefileMountainHolds when loser.StringId == "dwarf_kingdom":
                glory = 12f;
                break;
            case KingdomObjectiveType.PreserveBattanianHomelands when loser.StringId == "sturgia" || loser.Culture?.StringId == "mage":
                glory = 11f;
                break;
            case KingdomObjectiveType.UniteAseraiRealms when IsAseraiRealm(loser):
            case KingdomObjectiveType.ClaimImperialLegitimacy when IsImperialRealm(loser):
                glory = 9f;
                break;
            case KingdomObjectiveType.ForgeBorderEmpire when IsKhuzaitBorderTarget(loser):
                glory = 10f;
                break;
            case KingdomObjectiveType.MartialGlory:
                glory = 14f;
                break;
        }

        winnerState.ObjectiveWarScore += glory;
        winnerState.ClampValues();

        if (IsValidIntrigueKingdom(loser) && _kingdomStates.TryGetValue(loser, out KingdomIntrigueState loserState))
        {
            loserState.ObjectiveWarScore -= glory * 0.3f;
            loserState.ClampValues();
        }
    }

    private void OnHeroRelationChanged(
        Hero hero1,
        Hero hero2,
        int relationChange,
        bool showQuickNotification,
        ChangeRelationAction.ChangeRelationDetail detail,
        Hero originalHero,
        Hero originalGainedRelationWith)
    {
        if (hero1?.Clan == null || hero2?.Clan == null || hero1.Clan == hero2.Clan)
        {
            return;
        }

        if (hero1.Clan.Kingdom != null
            && hero1.Clan.Kingdom == hero2.Clan.Kingdom
            && hero2.Clan == hero1.Clan.Kingdom.RulingClan
            && IsValidIntrigueClan(hero1.Clan))
        {
            ClanIntrigueState state = GetOrCreateState(hero1.Clan);
            KingdomIntrigueState kingdomState = GetOrCreateKingdomState(hero1.Clan.Kingdom);
            if (relationChange < 0)
            {
                state.Dissidence += -relationChange * 0.75f;
                state.VoteResentment += -relationChange * 0.45f;
                state.ClaimantAmbition += -relationChange * 0.35f;
                kingdomState.CourtFragmentation += -relationChange * 0.25f;
            }
            else
            {
                state.RoyalFavor += relationChange * 0.5f;
                state.VoteResentment -= relationChange * 0.2f;
                kingdomState.RulerLegitimacy += relationChange * 0.08f;
            }

            state.ClampValues();
            kingdomState.ClampValues();
        }

        if (Hero.MainHero == null)
        {
            return;
        }

        Clan affectedClan = hero1 == Hero.MainHero ? hero2.Clan : hero2 == Hero.MainHero ? hero1.Clan : null;
        if (affectedClan != null && IsValidIntrigueClan(affectedClan))
        {
            ClanIntrigueState state = GetOrCreateState(affectedClan);
            state.TrustToPlayer += relationChange * 0.75f;
            if (relationChange > 0)
            {
                state.SoftDefectionPressure += relationChange * 0.12f;
            }

            state.ClampValues();
        }
    }

    private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isCancelled)
    {
        if (isCancelled || decision?.Kingdom == null || !IsValidIntrigueKingdom(decision.Kingdom))
        {
            return;
        }

        Clan favoredClan = chosenOutcome?.SponsorClan;
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(decision.Kingdom);

        ApplyGeneralDecisionPressure(decision.Kingdom, favoredClan, favoredClan == decision.Kingdom.RulingClan ? 2.5f : 1.25f);

        switch (decision)
        {
            case SettlementClaimantDecision settlementDecision:
                ApplySettlementClaimantDecisionEffects(settlementDecision, favoredClan, kingdomState);
                break;
            case KingdomPolicyDecision policyDecision:
                ApplyPolicyDecisionEffects(policyDecision, favoredClan, kingdomState);
                break;
            case KingSelectionKingdomDecision kingSelectionDecision:
                ApplyKingSelectionDecisionEffects(kingSelectionDecision, favoredClan, kingdomState);
                break;
            case ExpelClanFromKingdomDecision expelClanDecision:
                ApplyExpelClanDecisionEffects(expelClanDecision, favoredClan, kingdomState);
                break;
            default:
                kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 2f : 0.75f;
                break;
        }

        kingdomState.LastUpdated = CampaignTime.Now;
        kingdomState.ClampValues();
    }

    private void OnClanChangedKingdom(
        Clan clan,
        Kingdom oldKingdom,
        Kingdom newKingdom,
        ChangeKingdomAction.ChangeKingdomActionDetail detail,
        bool showNotification = true)
    {
        if (clan == null)
        {
            return;
        }

        if (clan == Clan.PlayerClan)
        {
            ClearPlayerObjectiveSupportExcept(newKingdom);
        }

        RemoveDeadReferencesForClan(clan);

        if (oldKingdom != null && IsValidIntrigueKingdom(oldKingdom))
        {
            RefreshKingdomState(oldKingdom);
        }

        if (newKingdom != null && IsValidIntrigueKingdom(newKingdom))
        {
            RefreshKingdomState(newKingdom);
        }

        if (IsValidIntrigueClan(clan))
        {
            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
        }
    }

    private void OnRulingClanChanged(Kingdom kingdom, Clan newRulingClan)
    {
        if (kingdom == null || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom!);
        kingdomState.RulerLegitimacy = 60f;
        kingdomState.CourtFragmentation += 12f;
        kingdomState.WarExhaustion *= 0.85f;
        kingdomState.ClampValues();

        foreach (Clan clan in kingdom.Clans)
        {
            if (IsValidIntrigueClan(clan))
            {
                ClanIntrigueState state = GetOrCreateState(clan);
                if (clan == newRulingClan)
                {
                    state.Dissidence = 0f;
                    state.ClaimantAmbition = 0f;
                    state.VoteResentment *= 0.5f;
                }

                RefreshTrustToPlayer(state);
                RefreshDerivedClanState(state);
                ClanAlignmentService.Recalculate(state, kingdomState);
            }
        }
    }

    private void OnClanDestroyed(Clan clan)
    {
        if (clan == null)
        {
            return;
        }

        _clanStates.Remove(clan);
        RemoveDeadReferencesForClan(clan);

        if (clan.Kingdom != null && IsValidIntrigueKingdom(clan.Kingdom))
        {
            RefreshKingdomState(clan.Kingdom);
        }
    }

    private void OnClanInfluenceChanged(Clan clan, float influence)
    {
        if (!IsValidIntrigueClan(clan))
        {
            return;
        }

        ClanIntrigueState state = GetOrCreateState(clan);
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(clan.Kingdom);
        float expectedInfluence = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan) * 0.05f;

        if (influence < expectedInfluence * 0.75f)
        {
            state.VoteResentment += 1.75f;
            state.FiefGrievance += 0.75f;
        }
        else if (influence > expectedInfluence * 1.5f)
        {
            state.RoyalFavor += 1.25f;
            state.ClaimantAmbition += 0.75f;
            kingdomState.CourtFragmentation += clan == clan.Kingdom.RulingClan ? -0.5f : 1f;
        }

        state.ClampValues();
        kingdomState.ClampValues();
    }

    private void OnSettlementOwnerChanged(
        Settlement settlement,
        bool openToClaim,
        Hero newOwner,
        Hero oldOwner,
        Hero capturerHero,
        ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        Clan? oldClan = oldOwner?.Clan;
        Clan? newClan = newOwner?.Clan;
        if (settlement == null || oldClan == newClan)
        {
            return;
        }

        float lossWeight = settlement.IsTown ? 18f : 12f;

        if (oldClan?.Kingdom != null && IsValidIntrigueKingdom(oldClan.Kingdom))
        {
            KingdomIntrigueState oldKingdomState = GetOrCreateKingdomState(oldClan.Kingdom);
            oldKingdomState.RecentLosses += lossWeight;
            oldKingdomState.WarExhaustion += settlement.IsTown ? 8f : 5f;
            oldKingdomState.RulerLegitimacy -= settlement.IsTown ? 10f : 6f;
            oldKingdomState.CourtFragmentation += settlement.IsTown ? 4f : 2f;
            oldKingdomState.ClampValues();

            foreach (Clan clan in oldClan.Kingdom.Clans)
            {
                if (!IsValidIntrigueClan(clan))
                {
                    continue;
                }

                ClanIntrigueState state = GetOrCreateState(clan);
                state.MilitaryFrustration += settlement.IsTown ? 6f : 4f;
                if (clan == oldClan)
                {
                    state.FiefGrievance += settlement.IsTown ? 16f : 10f;
                    state.SoftDefectionPressure += settlement.IsTown ? 10f : 6f;
                }

                state.ClampValues();
            }
        }

        if (newClan?.Kingdom != null && IsValidIntrigueKingdom(newClan.Kingdom))
        {
            KingdomIntrigueState newKingdomState = GetOrCreateKingdomState(newClan.Kingdom);
            newKingdomState.RulerLegitimacy += settlement.IsTown ? 4f : 2f;
            newKingdomState.RecentLosses -= 4f;
            newKingdomState.CourtFragmentation += newClan == newClan.Kingdom.RulingClan ? 2f : -1f;
            newKingdomState.ClampValues();

            if (IsValidIntrigueClan(newClan))
            {
                ClanIntrigueState state = GetOrCreateState(newClan);
                state.RoyalFavor += settlement.IsTown ? 8f : 5f;
                state.FiefGrievance -= settlement.IsTown ? 10f : 6f;
                state.ClampValues();
            }

            if (oldClan?.Kingdom != null && oldClan.Kingdom == newClan.Kingdom && newClan == newClan.Kingdom.RulingClan)
            {
                foreach (Clan clan in newClan.Kingdom.Clans)
                {
                    if (!IsValidIntrigueClan(clan) || clan == newClan)
                    {
                        continue;
                    }

                    ClanIntrigueState state = GetOrCreateState(clan);
                    state.FiefGrievance += settlement.IsTown ? 6f : 3f;
                    state.VoteResentment += settlement.IsTown ? 4f : 2f;
                    state.ClampValues();
                }
            }
        }

        ProcessAllianceSettlementPromises(settlement);
    }

    private void OnTownRebelliosStateChanged(Town town, bool isRebellious)
    {
        if (town?.OwnerClan?.Kingdom == null || !IsValidIntrigueKingdom(town.OwnerClan.Kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(town.OwnerClan.Kingdom);
        if (isRebellious)
        {
            kingdomState.RebellionPressure += 18f;
            kingdomState.RulerLegitimacy -= 12f;
            kingdomState.CourtFragmentation += 8f;
        }
        else
        {
            kingdomState.RebellionPressure -= 8f;
            kingdomState.RulerLegitimacy += 3f;
            kingdomState.CourtFragmentation -= 4f;
        }

        kingdomState.ClampValues();

        if (IsValidIntrigueClan(town.OwnerClan))
        {
            ClanIntrigueState state = GetOrCreateState(town.OwnerClan);
            state.FiefGrievance += isRebellious ? 8f : -4f;
            state.MilitaryFrustration += isRebellious ? 6f : -2f;
            state.ClampValues();
        }
    }

    private void OnRebellionFinished(Settlement settlement, Clan rebelClan)
    {
        Kingdom? kingdom = settlement?.OwnerClan?.Kingdom ?? rebelClan?.Kingdom;
        if (!IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        kingdomState.RebellionPressure -= 14f;
        kingdomState.RulerLegitimacy += 5f;
        kingdomState.CourtFragmentation -= 4f;
        kingdomState.ClampValues();
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        ApplyWarStateShift(faction1, 6f, -2f);
        ApplyWarStateShift(faction2, 6f, -2f);
        RefreshFactionStates(faction1);
        RefreshFactionStates(faction2);
        TriggerGuardianFrenzyIfAttacked(faction1 as Kingdom, faction2 as Kingdom);
    }

    /// <summary>Giant grand design (GuardianFrenzy): the giants never seek war,
    /// but being ATTACKED wakes a fury the war system treats as an enduring
    /// rivalry — they press the aggressor relentlessly until broken or peace.</summary>
    private void TriggerGuardianFrenzyIfAttacked(Kingdom attacker, Kingdom defender)
    {
        if (attacker == null || defender == null || attacker.IsEliminated || defender.IsEliminated)
        {
            return;
        }

        if (KingdomObjectiveService.ResolveObjective(defender) != KingdomObjectiveType.GuardianFrenzy)
        {
            return;
        }

        RFWarExternalIntentApi.ReinforceEnduringRivalryWar(defender, attacker);

        TextObject message = new TextObject("{=rf_ko_giant_frenzy}The giants of {DEFENDER} have been roused — they will not rest until {ATTACKER} is broken.");
        message.SetTextVariable("DEFENDER", defender.Name);
        message.SetTextVariable("ATTACKER", attacker.Name);
        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFC86E6Eu)));
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        ApplyWarStateShift(faction1, -10f, 4f);
        ApplyWarStateShift(faction2, -10f, 4f);
        RefreshFactionStates(faction1);
        RefreshFactionStates(faction2);
        StartGrandDesignWarQuietPeriod(faction1 as Kingdom, faction2 as Kingdom);
        StartGrandDesignWarQuietPeriod(faction2 as Kingdom, faction1 as Kingdom);
    }

    private void ApplyGeneralDecisionPressure(Kingdom kingdom, Clan favoredClan, float baseResentment)
    {
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            if (clan == favoredClan)
            {
                state.VoteResentment -= 6f;
                state.RoyalFavor += 4f;
            }
            else
            {
                state.VoteResentment += baseResentment;
                if (favoredClan == kingdom.RulingClan)
                {
                    state.FiefGrievance += 1f;
                }
            }

            state.ClampValues();
        }
    }

    private void ApplySettlementClaimantDecisionEffects(
        SettlementClaimantDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            bool isDispossessed = clan.Fiefs.Count() <= 1;
            if (clan == favoredClan)
            {
                state.FiefGrievance -= decision.Settlement?.IsTown == true ? 18f : 12f;
                state.VoteResentment -= 10f;
                state.RoyalFavor += 6f;
            }
            else
            {
                float resentment = isDispossessed ? 8f : 3f;
                if (favoredClan == decision.Kingdom.RulingClan)
                {
                    resentment += 4f;
                }

                if (clan == decision.ClanToExclude)
                {
                    resentment += 10f;
                }

                state.FiefGrievance += resentment;
                state.VoteResentment += resentment * 0.8f;
            }

            state.ClampValues();
        }

        kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 8f : 3f;
        kingdomState.RulerLegitimacy += favoredClan == decision.Kingdom.RulingClan ? -4f : 1.5f;
    }

    private void ApplyPolicyDecisionEffects(
        KingdomPolicyDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            float support = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.ClanPoliticsModel.CalculateSupportForPolicyInClan(clan, decision.Policy);
            if (support < 0f)
            {
                state.VoteResentment += -support * 12f;
                state.Dissidence += -support * 3f;
            }
            else
            {
                state.RoyalFavor += support * 4f;
                state.VoteResentment -= support * 3f;
            }

            if (favoredClan == decision.Kingdom.RulingClan && support < 0f)
            {
                state.FiefGrievance += 2f;
            }

            state.ClampValues();
        }

        kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 5f : 1.5f;
    }

    private void ApplyKingSelectionDecisionEffects(
        KingSelectionKingdomDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        float rulerStrength = favoredClan == null
            ? 0f
            : global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(favoredClan);

        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);

            if (clan == favoredClan)
            {
                state.ClaimantAmbition *= 0.35f;
                state.VoteResentment *= 0.5f;
                state.RoyalFavor += 8f;
            }
            else
            {
                state.ClaimantAmbition += clanStrength >= rulerStrength * 0.75f ? 14f : 6f;
                state.VoteResentment += 6f;
                if (favoredClan?.Leader != null && clan.Leader.GetRelation(favoredClan.Leader) < 0)
                {
                    state.ClaimantAmbition += 4f;
                }
            }

            state.ClampValues();
        }

        kingdomState.RulerLegitimacy = 60f;
        kingdomState.CourtFragmentation += 10f;
    }

    private void ApplyExpelClanDecisionEffects(
        ExpelClanFromKingdomDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        if (IsValidIntrigueClan(decision.ClanToExpel))
        {
            ClanIntrigueState expelledState = GetOrCreateState(decision.ClanToExpel);
            expelledState.Dissidence += 25f;
            expelledState.VoteResentment += 25f;
            expelledState.SoftDefectionPressure += 25f;
            expelledState.ClampValues();
        }

        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == decision.ClanToExpel)
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            state.VoteResentment += 4f;
            if (clan.Fiefs.Count() <= 1)
            {
                state.FiefGrievance += 4f;
            }

            if (favoredClan == decision.Kingdom.RulingClan)
            {
                state.ClaimantAmbition += 2f;
            }

            state.ClampValues();
        }

        kingdomState.RulerLegitimacy -= 12f;
        kingdomState.CourtFragmentation += 10f;
    }

    private void ApplyWarStateShift(IFaction faction, float warExhaustionDelta, float legitimacyDelta)
    {
        if (faction is not Kingdom kingdom || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState state = GetOrCreateKingdomState(kingdom);
        state.WarExhaustion += warExhaustionDelta;
        state.RulerLegitimacy += legitimacyDelta;
        state.ClampValues();
    }

    private void RefreshFactionStates(IFaction faction)
    {
        if (faction is not Kingdom kingdom || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        RefreshKingdomState(kingdom);
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, kingdomState);
        }
    }

    private void EnsureInitialized()
    {
        if (_isInitialized || _isInitializing)
        {
            return;
        }

        _isInitializing = true;
        try
        {
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (IsObjectiveCapableKingdom(kingdom))
                {
                    GetOrCreateKingdomState(kingdom);
                }
            }

            foreach (Clan clan in Clan.All)
            {
                if (!IsValidIntrigueClan(clan))
                {
                    continue;
                }

                ClanIntrigueState state = GetOrCreateState(clan);
                RefreshTrustToPlayer(state);
                RefreshDerivedClanState(state);
                state.ClampValues();
            }

            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (IsObjectiveCapableKingdom(kingdom))
                {
                    RefreshKingdomState(kingdom);
                }
            }

            _isInitialized = true;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private ClanIntrigueState GetOrCreateState(Clan clan)
    {
        if (!_clanStates.TryGetValue(clan, out ClanIntrigueState state))
        {
            state = new ClanIntrigueState(clan);
            _clanStates.Add(clan, state);
        }

        return state;
    }

    private KingdomIntrigueState GetOrCreateKingdomState(Kingdom kingdom)
    {
        if (!_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState state))
        {
            state = new KingdomIntrigueState(kingdom);
            _kingdomStates.Add(kingdom, state);
        }

        return state;
    }

    private bool HasActivePactInternal(Clan clan)
    {
        return clan != null && _secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan);
    }

    private void RefreshTrustToPlayer(ClanIntrigueState state)
    {
        if (state?.Clan?.Leader == null || Hero.MainHero == null)
        {
            return;
        }

        int relation = state.Clan.Leader.GetRelation(Hero.MainHero);
        float relationTrust = relation <= 0
            ? MBMath.Map(relation, -100f, 0f, 0f, 22f)
            : MBMath.Map(relation, 0f, 100f, 22f, 100f);
        state.TrustToPlayer = MBMath.ClampFloat((state.TrustToPlayer * 0.4f) + (relationTrust * 0.6f), 0f, 100f);
    }

    private void RefreshDerivedClanState(ClanIntrigueState state)
    {
        if (state?.Clan == null || state.Clan.Kingdom == null || state.Clan == state.Clan.Kingdom.RulingClan)
        {
            return;
        }

        Clan clan = state.Clan;
        Kingdom kingdom = clan.Kingdom;
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        float ownStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
        float averageStrength = GetAverageClanStrength(kingdom);
        float averageInfluence = kingdom.Clans.Average(x => x.Influence);
        float averageFiefs = kingdom.Clans.Average(x => (float)x.Fiefs.Count());
        // RulingClan.Leader can be null during a regency / leader-death window.
        int relationToRuler = (kingdom.RulingClan?.Leader != null && clan.Leader != null)
            ? clan.Leader.GetRelation(kingdom.RulingClan.Leader)
            : 0;

        float baseFiefGrievance = 0f;
        if (clan.Fiefs.Count() == 0)
        {
            baseFiefGrievance += 24f;
        }
        else if (clan.Fiefs.Count() == 1)
        {
            baseFiefGrievance += 12f;
        }

        if (clan.Fiefs.Count() < averageFiefs)
        {
            baseFiefGrievance += (averageFiefs - clan.Fiefs.Count()) * 4.5f;
        }

        if (clan.Fiefs.Count() == 0 && ownStrength > averageStrength * 0.85f)
        {
            baseFiefGrievance += 8f;
        }

        state.FiefGrievance = MBMath.ClampFloat((state.FiefGrievance * 0.6f) + baseFiefGrievance, 0f, 100f);

        float expectedInfluence = ownStrength * 0.05f;
        float baseVoteResentment = expectedInfluence > clan.Influence
            ? (expectedInfluence - clan.Influence) * 0.35f
            : 0f;
        if (relationToRuler < 0)
        {
            baseVoteResentment += -relationToRuler * 0.05f;
        }

        if (clan.Influence < averageInfluence * 0.65f)
        {
            baseVoteResentment += 4f;
        }

        state.VoteResentment = MBMath.ClampFloat((state.VoteResentment * 0.78f) + baseVoteResentment, 0f, 100f);
        float baseMilitaryFrustration = kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction) * 6f;
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            float progress = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(kingdom, enemy).ResultNumber;
            if (progress < 0f)
            {
                baseMilitaryFrustration += MBMath.ClampFloat(-progress / 4f, 0f, 10f);
            }
        }

        if (clan.Fiefs.Count() == 0)
        {
            baseMilitaryFrustration += 4f;
        }

        state.MilitaryFrustration = MBMath.ClampFloat((state.MilitaryFrustration * 0.74f) + baseMilitaryFrustration, 0f, 100f);

        float baseClaimantAmbition = 0f;
        if (ownStrength > averageStrength)
        {
            baseClaimantAmbition += ((ownStrength - averageStrength) / Math.Max(averageStrength, 1f)) * 25f;
        }

        if (clan.Influence > averageInfluence)
        {
            baseClaimantAmbition += MBMath.Map(clan.Influence - averageInfluence, 0f, Math.Max(averageInfluence, 1f), 0f, 12f);
        }

        if (relationToRuler < 0)
        {
            baseClaimantAmbition += -relationToRuler * 0.08f;
        }

        state.ClaimantAmbition = MBMath.ClampFloat((state.ClaimantAmbition * 0.72f) + baseClaimantAmbition, 0f, 100f);

        float defectionPressure = (100f - kingdomState.RulerLegitimacy) * 0.55f;
        defectionPressure += state.FiefGrievance * 0.2f;
        defectionPressure += state.VoteResentment * 0.25f;
        defectionPressure += state.MilitaryFrustration * 0.15f;
        defectionPressure += state.ClaimantAmbition * 0.15f;
        if (HasActivePactInternal(clan))
        {
            defectionPressure += 12f;
        }

        if (Hero.MainHero != null)
        {
            int relationToPlayer = clan.Leader.GetRelation(Hero.MainHero);
            if (relationToPlayer > 0)
            {
                defectionPressure += relationToPlayer * 0.15f;
            }
        }

        state.SoftDefectionPressure = MBMath.ClampFloat((state.SoftDefectionPressure * 0.68f) + defectionPressure, 0f, 100f);
        state.ClampValues();
    }

    private void DecayDailyValues()
    {
        foreach (ClanIntrigueState state in _clanStates.Values)
        {
            state.Suspicion -= StrategicIntrigueConstants.DailySuspicionDecay;
            state.RoyalFavor -= StrategicIntrigueConstants.DailyRoyalFavorDecay;
            state.Infiltration -= StrategicIntrigueConstants.DailyInfiltrationDecay;
            state.FiefGrievance -= 0.35f;
            state.VoteResentment -= 0.9f;
            state.MilitaryFrustration -= 0.65f;
            state.ClaimantAmbition -= 0.25f;
            state.SoftDefectionPressure -= 0.45f;
            state.ClampValues();
        }

        foreach (KingdomIntrigueState state in _kingdomStates.Values)
        {
            state.WarExhaustion -= 1.2f;
            state.RecentLosses -= 2.5f;
            state.RebellionPressure -= 0.8f;
            state.CourtFragmentation -= 0.65f;
            state.ClaimantPressure -= 0.6f;
            state.ObjectiveWarScore -= 1.35f;
            state.ClampValues();
        }

        foreach (SecretPact pact in _secretPacts)
        {
            if (!pact.IsExposed)
            {
                pact.Secrecy -= 0.5f;
                pact.Commitment += 0.35f;
                pact.ClampValues();
            }
        }
    }

    private Dictionary<Clan, bool> CaptureActivePactBreakawayReadiness()
    {
        Dictionary<Clan, bool> readiness = new();
        foreach (SecretPact pact in _secretPacts)
        {
            if (pact.IsExposed || pact.MemberClan == null || readiness.ContainsKey(pact.MemberClan))
            {
                continue;
            }

            readiness[pact.MemberClan] = GetState(pact.MemberClan)?.IsBreakawayReady == true;
        }

        return readiness;
    }

    private void ScheduleAutomaticEscalations()
    {
        foreach (SecretPact pact in _secretPacts.Where(x => !x.IsExposed).ToList())
        {
            Clan clan = pact.MemberClan;
            if (clan == null
                || !_clanStates.TryGetValue(clan, out ClanIntrigueState state)
                || !state.IsBreakawayReady
                || pact.Commitment < StrategicIntrigueConstants.AutoEscalationCommitmentThreshold
                || HasPendingBreakOperation(clan))
            {
                continue;
            }

            if (pact.Goal == IntriguePactGoal.SupportFutureClaimant && !CanTriggerClaimantCoup(clan, out _))
            {
                continue;
            }

            _pendingOperations.Add(new IntrigueOperation(
                IntrigueOperationType.PrepareBreakaway,
                pact.SponsorClan,
                clan,
                clan.Kingdom?.RulingClan,
                pact.Commitment,
                35f,
                CampaignTime.DaysFromNow(StrategicIntrigueConstants.AutoEscalationDelayDays)));
        }
    }

    private void GenerateOrganicIntrigueMoves()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsValidIntrigueKingdom(kingdom) || !_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState kingdomState))
            {
                continue;
            }

            float crisis = GetOrganicCrisisScore(kingdomState);
            if (crisis < StrategicIntrigueConstants.OrganicRumorCrisisThreshold)
            {
                continue;
            }

            TryGenerateOrganicRumor(kingdom, kingdomState, crisis);

            if (crisis >= StrategicIntrigueConstants.OrganicPactCrisisThreshold)
            {
                TryGenerateOrganicPact(kingdom, kingdomState, crisis);
            }
        }
    }

    private void TryGenerateOrganicRumor(Kingdom kingdom, KingdomIntrigueState kingdomState, float crisis)
    {
        if ((CampaignTime.Now - kingdomState.LastOrganicRumorAt).ToDays < StrategicIntrigueConstants.OrganicRumorCooldownDays)
        {
            return;
        }

        List<(Clan Clan, float Weight)> candidates = new();
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == kingdom.RulingClan || HasPendingRumorCampaign(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetState(clan);
            if (state == null)
            {
                continue;
            }

            float weight = GetOrganicRumorTargetWeight(kingdom, state);
            if (weight >= 28f)
            {
                candidates.Add((clan, weight));
            }
        }

        Clan targetClan = ChooseWeightedClan(candidates);
        if (targetClan == null)
        {
            return;
        }

        Clan sponsorClan = FindOrganicRumorSponsor(targetClan);
        if (sponsorClan == null)
        {
            return;
        }

        ClanIntrigueState targetState = GetState(targetClan);
        float chance = MBMath.ClampFloat(
            0.04f + ((crisis - StrategicIntrigueConstants.OrganicRumorCrisisThreshold) * 0.003f) + ((targetState.Dissidence - 45f) * 0.0035f),
            0f,
            StrategicIntrigueConstants.OrganicRumorMaxChance);
        if (MBRandom.RandomFloat > chance)
        {
            return;
        }

        float power = MBMath.ClampFloat(18f + (crisis * 0.28f), 18f, 48f);
        float risk = sponsorClan.Kingdom == targetClan.Kingdom ? 22f : 28f;
        _pendingOperations.Add(new IntrigueOperation(
            IntrigueOperationType.RumorCampaign,
            sponsorClan,
            targetClan,
            targetClan.Kingdom?.RulingClan,
            power,
            risk,
            CampaignTime.DaysFromNow(2f + (MBRandom.RandomFloat * 2f))));
        kingdomState.LastOrganicRumorAt = CampaignTime.Now;
    }

    private void TryGenerateOrganicPact(Kingdom kingdom, KingdomIntrigueState kingdomState, float crisis)
    {
        if ((CampaignTime.Now - kingdomState.LastOrganicPactAt).ToDays < StrategicIntrigueConstants.OrganicPactCooldownDays)
        {
            return;
        }

        List<(Clan Clan, float Weight)> candidates = new();
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == kingdom.RulingClan || HasActivePact(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetState(clan);
            if (state == null || !state.IsConspirable || state.Suspicion >= 78f)
            {
                continue;
            }

            float weight = GetOrganicPactTargetWeight(kingdom, state);
            if (weight >= 40f)
            {
                candidates.Add((clan, weight));
            }
        }

        Clan targetClan = ChooseWeightedClan(candidates);
        if (targetClan == null)
        {
            return;
        }

        Clan sponsorClan = FindOrganicPactSponsor(targetClan);
        if (sponsorClan == null)
        {
            return;
        }

        ClanIntrigueState targetState = GetState(targetClan);
        float chance = MBMath.ClampFloat(
            0.025f + ((crisis - StrategicIntrigueConstants.OrganicPactCrisisThreshold) * 0.0025f) + ((targetState.Dissidence - StrategicIntrigueConstants.ConspirableDissidenceThreshold) * 0.003f),
            0f,
            StrategicIntrigueConstants.OrganicPactMaxChance);
        if (MBRandom.RandomFloat > chance)
        {
            return;
        }

        if (ApplySecretPactAction.TryApplyOrganic(_clanStates, _secretPacts, sponsorClan, targetClan, out _))
        {
            kingdomState.LastOrganicPactAt = CampaignTime.Now;
            if (sponsorClan.Kingdom != null && sponsorClan.Kingdom != kingdom && _kingdomStates.TryGetValue(sponsorClan.Kingdom, out KingdomIntrigueState sponsorKingdomState))
            {
                sponsorKingdomState.LastOrganicPactAt = CampaignTime.Now;
            }
        }
    }

    private Clan FindOrganicRumorSponsor(Clan targetClan)
    {
        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Clan clan in targetClan.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == targetClan || clan == targetClan.Kingdom.RulingClan)
            {
                continue;
            }

            float weight = GetInternalSponsorWeight(clan, targetClan);
            if (weight >= 26f)
            {
                sponsors.Add((clan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    private Clan FindOrganicPactSponsor(Clan targetClan)
    {
        ClanIntrigueState targetState = GetState(targetClan);
        bool preferExternal = targetState != null
            && targetState.SoftDefectionPressure >= StrategicIntrigueConstants.OrganicExternalPactSoftDefectionThreshold
            && targetState.ClaimantAmbition < 60f;

        Clan sponsor = preferExternal
            ? FindExternalPactSponsor(targetClan)
            : FindInternalPactSponsor(targetClan);
        if (sponsor != null)
        {
            return sponsor;
        }

        return preferExternal
            ? FindInternalPactSponsor(targetClan)
            : FindExternalPactSponsor(targetClan);
    }

    private Clan FindInternalPactSponsor(Clan targetClan)
    {
        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Clan clan in targetClan.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == targetClan || clan == targetClan.Kingdom.RulingClan || HasActivePact(clan))
            {
                continue;
            }

            float weight = GetInternalSponsorWeight(clan, targetClan) + (GetState(clan)?.ClaimantAmbition ?? 0f) * 0.18f;
            if (weight >= 34f)
            {
                sponsors.Add((clan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    private Clan FindExternalPactSponsor(Clan targetClan)
    {
        ClanIntrigueState targetState = GetState(targetClan);
        if (targetState == null || targetState.SoftDefectionPressure < StrategicIntrigueConstants.OrganicExternalPactSoftDefectionThreshold)
        {
            return null;
        }

        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom == targetClan.Kingdom || kingdom.IsEliminated || kingdom.RulingClan?.Leader == null)
            {
                continue;
            }

            Hero sponsorLeader = kingdom.RulingClan.Leader;
            Hero targetLeader = targetClan.Leader;
            int relation = targetLeader?.GetRelation(sponsorLeader) ?? 0;

            // Hard block: hostile relation AND no cultural affinity AND not a neighbour
            float proximity = GetTerritorialProximityScore(targetClan, kingdom);
            if (relation < -10 && kingdom.Culture != targetClan.Culture && proximity < 0f)
            {
                continue;
            }

            // ── Base weight factors ───────────────────────────────────────
            float weight = MathF.Max(0f, (float)relation) * 0.85f;

            if (kingdom.Culture == targetClan.Culture)
                weight += 14f;

            if (_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState sponsorKingdomState))
                weight += sponsorKingdomState.RulerLegitimacy * 0.10f;

            weight += targetState.SoftDefectionPressure * 0.28f;

            // ── Territorial proximity ─────────────────────────────────────
            // A clan surrounded by kingdom X's territory has real geographic
            // reason to seek shelter there. Distant kingdoms make no political sense.
            weight += proximity;

            // ── Current war relationship ──────────────────────────────────
            // If the sponsor is already at war with the origin kingdom, absorbing
            // a defecting clan is strategically motivated (weakens the enemy).
            // If at peace, the sponsor risks diplomatic fallout.
            bool atWarWithOrigin = targetClan.Kingdom != null
                && kingdom.IsAtWarWith(targetClan.Kingdom);
            if (atWarWithOrigin)
                weight += 12f;  // strong strategic incentive
            else
                weight -= 8f;   // absorbing a defector risks diplomatic incident

            // ── Threshold raised vs original ─────────────────────────────
            // Harder to qualify: only genuinely plausible sponsors emerge.
            if (weight >= 50f)
            {
                sponsors.Add((kingdom.RulingClan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    /// <summary>
    /// Returns a score for how geographically sensible it would be for the
    /// target clan to defect to the given sponsor kingdom.
    /// Computed as the minimum gate-to-gate distance between the clan's fiefs
    /// and the sponsor kingdom's fiefs, converted to a [-20, +25] score.
    /// Landless clans are location-neutral (score 0).
    /// </summary>
    private static float GetTerritorialProximityScore(Clan targetClan, Kingdom sponsorKingdom)
    {
        try
        {
            var targetFiefs = targetClan.Fiefs;
            if (targetFiefs == null || targetFiefs.Count == 0)
                return 0f; // landless clan has no geographic anchor

            var sponsorFiefs = sponsorKingdom.Fiefs;
            if (sponsorFiefs == null || sponsorFiefs.Count == 0)
                return 0f; // sponsor has no land either

            float minDist = float.MaxValue;
            foreach (var tf in targetFiefs)
            {
                var tp = tf.Settlement?.GatePosition.ToVec2();
                if (tp == null) continue;
                foreach (var sf in sponsorFiefs)
                {
                    var sp = sf.Settlement?.GatePosition.ToVec2();
                    if (sp == null) continue;
                    float dist = tp.Value.Distance(sp.Value);
                    if (dist < minDist) minDist = dist;
                }
            }

            if (minDist == float.MaxValue) return 0f;

            // Convert distance to score:
            //   < 80  → adjacent territory          → +25 (very strong pull)
            //   < 150 → nearby territory             → +15
            //   < 250 → moderate distance            → +5
            //   < 400 → far but conceivable          → -5
            //   >= 400 → geographically implausible → -20
            if (minDist < 80f)  return 25f;
            if (minDist < 150f) return 15f;
            if (minDist < 250f) return  5f;
            if (minDist < 400f) return -5f;
            return -20f;
        }
        catch
        {
            return 0f;
        }
    }

    private float GetOrganicCrisisScore(KingdomIntrigueState kingdomState)
    {
        return ((100f - kingdomState.RulerLegitimacy) * 0.42f)
            + (kingdomState.CourtFragmentation * 0.24f)
            + (kingdomState.RebellionPressure * 0.17f)
            + (kingdomState.ClaimantPressure * 0.17f)
            + (kingdomState.ObjectivePressure * 0.12f)
            + GetObjectiveCrisisBias(kingdomState);
    }

    private float GetOrganicRumorTargetWeight(Kingdom kingdom, ClanIntrigueState state)
    {
        return (state.Dissidence * 0.5f)
            + (state.VoteResentment * 0.18f)
            + (state.FiefGrievance * 0.14f)
            + (state.ClaimantAmbition * 0.08f)
            + (state.SoftDefectionPressure * 0.1f)
            + (state.MilitaryFrustration * 0.08f)
            - (state.FearOfRuler * 0.18f)
            - (state.Suspicion * 0.08f)
            + GetObjectiveTargetBias(kingdom, state, false);
    }

    private float GetOrganicPactTargetWeight(Kingdom kingdom, ClanIntrigueState state)
    {
        return (state.Dissidence * 0.54f)
            + (state.ClaimantAmbition * 0.18f)
            + (state.SoftDefectionPressure * 0.22f)
            + (state.Infiltration * 0.08f)
            + (state.MilitaryFrustration * 0.1f)
            - (state.FearOfRuler * 0.12f)
            - (state.Suspicion * 0.1f)
            + GetObjectiveTargetBias(kingdom, state, true);
    }

    private float GetInternalSponsorWeight(Clan sponsorClan, Clan targetClan)
    {
        ClanIntrigueState sponsorState = GetState(sponsorClan);
        ClanIntrigueState targetState = GetState(targetClan);
        if (sponsorState == null || targetState == null || sponsorClan.Leader == null || targetClan.Leader == null)
        {
            return 0f;
        }

        Hero ruler = targetClan.Kingdom?.RulingClan?.Leader;
        int relationToTarget = sponsorClan.Leader.GetRelation(targetClan.Leader);
        int sponsorToRuler = ruler == null ? 0 : sponsorClan.Leader.GetRelation(ruler);
        int targetToRuler = ruler == null ? 0 : targetClan.Leader.GetRelation(ruler);
        if (relationToTarget < -18)
        {
            return 0f;
        }

        float weight = MathF.Max(0f, (float)relationToTarget) * 0.85f;
        weight += MathF.Max(0f, (float)(-sponsorToRuler)) * 0.45f;
        weight += MathF.Max(0f, (float)(-targetToRuler)) * 0.35f;
        weight += sponsorState.Dissidence * 0.28f;
        weight += sponsorState.ClaimantAmbition * 0.16f;
        if (sponsorClan.Culture == targetClan.Culture)
        {
            weight += 8f;
        }

        weight += GetObjectiveSponsorBias(sponsorClan, targetClan);
        return weight;
    }

    private float GetObjectiveCrisisBias(KingdomIntrigueState kingdomState)
    {
        if (kingdomState == null || kingdomState.ObjectiveType == KingdomObjectiveType.None)
        {
            return 0f;
        }

        float bias = 0f;
        if (IsExpansionistObjective(kingdomState.ObjectiveType) && !HasObjectiveTargetWar(kingdomState.Kingdom, kingdomState.ObjectiveType))
        {
            bias += 6f;
        }

        if (kingdomState.ObjectiveType == KingdomObjectiveType.MartialGlory && kingdomState.Kingdom?.FactionsAtWarWith.Any(x => x.IsKingdomFaction) != true)
        {
            bias += 9f;
        }

        if (kingdomState.ObjectiveType == KingdomObjectiveType.UnbreakableRealm)
        {
            bias -= 4f;
        }

        return bias;
    }

    private float GetObjectiveTargetBias(Kingdom kingdom, ClanIntrigueState state, bool forPact)
    {
        if (kingdom == null || state == null || !_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState kingdomState))
        {
            return 0f;
        }

        float bias = 0f;
        switch (kingdomState.ObjectiveType)
        {
            case KingdomObjectiveType.NobleWealthSupremacy:
                bias += (state.FiefGrievance * 0.12f) + (state.VoteResentment * 0.08f);
                break;
            case KingdomObjectiveType.UniteAseraiRealms:
            case KingdomObjectiveType.ClaimImperialLegitimacy:
                bias += state.ClaimantAmbition * (forPact ? 0.18f : 0.12f);
                break;
            case KingdomObjectiveType.MartialGlory:
                bias += state.MilitaryFrustration * (forPact ? 0.2f : 0.14f);
                break;
            case KingdomObjectiveType.UnbreakableRealm:
                bias -= 4f;
                break;
            default:
                if (IsExpansionistObjective(kingdomState.ObjectiveType) && !HasObjectiveTargetWar(kingdom, kingdomState.ObjectiveType))
                {
                    bias += state.MilitaryFrustration * 0.1f;
                    bias += state.SoftDefectionPressure * 0.08f;
                }

                break;
        }

        return bias;
    }

    private float GetObjectiveSponsorBias(Clan sponsorClan, Clan targetClan)
    {
        if (sponsorClan?.Kingdom == null || !_kingdomStates.TryGetValue(sponsorClan.Kingdom, out KingdomIntrigueState kingdomState))
        {
            return 0f;
        }

        ClanIntrigueState sponsorState = GetState(sponsorClan);
        if (sponsorState == null)
        {
            return 0f;
        }

        return kingdomState.ObjectiveType switch
        {
            KingdomObjectiveType.NobleWealthSupremacy => MathF.Min(10f, sponsorClan.Gold / 35000f) + (sponsorClan.Fiefs.Count() >= 2 ? 4f : 0f),
            KingdomObjectiveType.UniteAseraiRealms or KingdomObjectiveType.ClaimImperialLegitimacy => (sponsorState.ClaimantAmbition * 0.14f) + (sponsorClan.Fiefs.Count() * 1.5f),
            KingdomObjectiveType.MartialGlory => sponsorState.MilitaryFrustration * 0.16f,
            KingdomObjectiveType.UnbreakableRealm => -8f,
            _ => IsExpansionistObjective(kingdomState.ObjectiveType) && !HasObjectiveTargetWar(sponsorClan.Kingdom, kingdomState.ObjectiveType)
                ? sponsorState.MilitaryFrustration * 0.1f
                : 0f
        };
    }

    private static Clan ChooseWeightedClan(List<(Clan Clan, float Weight)> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        float totalWeight = options.Sum(x => MathF.Max(0.01f, x.Weight));
        float roll = MBRandom.RandomFloat * totalWeight;
        foreach ((Clan clan, float weight) in options)
        {
            roll -= MathF.Max(0.01f, weight);
            if (roll <= 0f)
            {
                return clan;
            }
        }

        return options[options.Count - 1].Clan;
    }

    private SecretPact GetActivePact(Clan clan)
    {
        return clan == null
            ? null
            : _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
    }

    private Clan GetPreferredPlayerConspiracyTarget(Kingdom excludedKingdom)
    {
        return _secretPacts
            .Where(x =>
                !x.IsExposed
                && x.SponsorClan == Clan.PlayerClan
                && x.MemberClan?.Kingdom != null
                && x.MemberClan.Kingdom != excludedKingdom)
            .OrderByDescending(x =>
            {
                ClanIntrigueState state = GetState(x.MemberClan);
                if (state == null)
                {
                    return 0f;
                }

                float score = state.Dissidence
                    + (state.IsBreakawayReady ? 40f : 0f)
                    + (state.IsConspirable ? 15f : 0f)
                    + (state.SoftDefectionPressure * 0.2f)
                    + (state.ClaimantAmbition * 0.18f);
                return score;
            })
            .Select(x => x.MemberClan)
            .FirstOrDefault();
    }

    private static bool IsAllianceActive(SecretAllianceCompact alliance)
    {
        return alliance != null
            && alliance.IsActive
            && alliance.AllyClan != null
            && alliance.SupportedClan != null
            && alliance.TargetKingdom != null;
    }

    private static IntrigueAllianceObjective MapAllianceObjective(IntriguePactGoal goal)
    {
        return goal == IntriguePactGoal.SupportFutureClaimant
            ? IntrigueAllianceObjective.BackClaimant
            : IntrigueAllianceObjective.BackBreakaway;
    }

    private static Settlement ChoosePromisedSettlement(Kingdom targetKingdom, Clan supportedClan, Clan recipientClan)
    {
        if (targetKingdom == null)
        {
            return null;
        }

        Town promisedTown = targetKingdom.Fiefs
            .Where(x => x.OwnerClan != null && x.OwnerClan != recipientClan)
            .OrderByDescending(x => x.OwnerClan == targetKingdom.RulingClan)
            .ThenBy(x => x.IsTown ? 1 : 0)
            .ThenByDescending(x => x.OwnerClan != supportedClan)
            .FirstOrDefault();

        return promisedTown?.Settlement;
    }

    private void ProcessAllianceBreakOutcomes(IEnumerable<IntrigueOperationResolution> resolutions)
    {
        foreach (IntrigueOperationResolution resolution in resolutions)
        {
            if (resolution.BreakOutcome != IntrigueBreakOutcome.None && resolution.TargetClan != null)
            {
                ProcessAllianceBreakOutcome(resolution.TargetClan, resolution.BreakOutcome);
            }
        }
    }

    private void ProcessAllianceBreakOutcome(Clan targetClan, IntrigueBreakOutcome outcome)
    {
        foreach (SecretAllianceCompact alliance in _secretAlliances
                     .Where(x => IsAllianceActive(x) && x.SupportedClan == targetClan)
                     .ToList())
        {
            TriggerAllianceSupport(alliance, outcome);
            TryFulfillSettlementPromise(alliance);
        }
    }

    private void TriggerAllianceSupport(SecretAllianceCompact alliance, IntrigueBreakOutcome outcome)
    {
        if (alliance == null || alliance.IsSupportTriggered)
        {
            return;
        }

        alliance.IsSupportTriggered = true;
        alliance.ResolveBy = CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceSettlementGraceDays);

        Kingdom allyKingdom = alliance.AllyClan?.Kingdom;
        if (alliance.Objective == IntrigueAllianceObjective.ForeignIntervention
            && outcome != IntrigueBreakOutcome.ClaimantCoup
            && allyKingdom != null
            && alliance.TargetKingdom != null
            && allyKingdom != alliance.TargetKingdom
            && !allyKingdom.IsAtWarWith(alliance.TargetKingdom))
        {
            RFWarExternalIntentApi.ReinforceStrategicIntrigueWar(
                allyKingdom,
                alliance.TargetKingdom,
                alliance.PromisedSettlement);
        }

        if (alliance.InstigatorClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_support_title}Secret Alliance Moved");
            TextObject body = alliance.Objective == IntrigueAllianceObjective.ForeignIntervention
                ? new TextObject("{=rf_si_alliance_support_body_foreign}{ALLY} has begun to move openly against {KINGDOM}. If the plot holds, {SETTLEMENT} is the promised price of that support.")
                : new TextObject("{=rf_si_alliance_support_body_internal}Your secret alliance with {ALLY} has moved beyond whispers. If the plot holds, {SETTLEMENT} is now due.");
            body.SetTextVariable("ALLY", alliance.AllyClan?.Name ?? new TextObject("{=rf_si_unknown_ally}your ally"));
            body.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            body.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}the promised fief"));
            ShowIntrigueInquiry(title, body);
        }
    }

    private void ProcessAllianceSettlementPromises(Settlement settlement)
    {
        if (settlement == null)
        {
            return;
        }

        foreach (SecretAllianceCompact alliance in _secretAlliances
                     .Where(x => IsAllianceActive(x) && x.PromisedSettlement == settlement)
                     .ToList())
        {
            TryFulfillSettlementPromise(alliance);
        }
    }

    private bool TryFulfillSettlementPromise(SecretAllianceCompact alliance)
    {
        if (!IsAllianceActive(alliance)
            || alliance.RewardType != IntrigueAllianceRewardType.PromisedSettlement
            || alliance.PromisedSettlement == null)
        {
            return false;
        }

        Settlement settlement = alliance.PromisedSettlement;
        Clan recipientClan = alliance.SettlementRecipientClan;
        if (settlement.OwnerClan == recipientClan)
        {
            alliance.IsFulfilled = true;
            return true;
        }

        Hero recipientHero = recipientClan == Clan.PlayerClan ? Hero.MainHero : recipientClan?.Leader;
        if (recipientHero == null)
        {
            return false;
        }

        bool claimantCanPay = alliance.TargetKingdom?.RulingClan == alliance.SupportedClan
            && settlement.OwnerClan?.Kingdom == alliance.TargetKingdom;
        bool controllerCanPay = settlement.OwnerClan == alliance.SupportedClan
            || settlement.OwnerClan == alliance.InstigatorClan
            || settlement.OwnerClan == Clan.PlayerClan;
        if (!claimantCanPay && !controllerCanPay)
        {
            return false;
        }

        alliance.IsFulfilled = true;
        ChangeOwnerOfSettlementAction.ApplyByGift(settlement, recipientHero);

        if (alliance.InstigatorClan == Clan.PlayerClan || recipientClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_reward_title}Promise Honored");
            TextObject body = new TextObject("{=rf_si_alliance_reward_body}{SETTLEMENT} has changed hands in accordance with your secret bargain. {RECIPIENT} has now received the price promised by {ALLY}.");
            body.SetTextVariable("SETTLEMENT", settlement.Name);
            body.SetTextVariable("RECIPIENT", recipientClan?.Name ?? new TextObject("{=rf_si_unknown_clan}the recipient"));
            body.SetTextVariable("ALLY", alliance.AllyClan?.Name ?? new TextObject("{=rf_si_unknown_ally}your ally"));
            ShowIntrigueInquiry(title, body);
        }

        Hero allyLeader = alliance.AllyClan?.Leader;
        if (allyLeader != null && recipientHero != null && allyLeader != recipientHero)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(allyLeader, recipientHero, 8, false);
        }

        return true;
    }

    private void ProcessAllianceDeadlines()
    {
        foreach (SecretAllianceCompact alliance in _secretAlliances.Where(IsAllianceActive).ToList())
        {
            if ((CampaignTime.Now - alliance.ResolveBy).ToDays <= 0f)
            {
                continue;
            }

            alliance.IsBroken = true;
            ApplyBrokenAllianceConsequences(alliance);
        }
    }

    private void ApplyBrokenAllianceConsequences(SecretAllianceCompact alliance)
    {
        Hero instigator = alliance.InstigatorClan == Clan.PlayerClan ? Hero.MainHero : alliance.InstigatorClan?.Leader;
        Hero ally = alliance.AllyClan?.Leader;
        if (instigator != null && ally != null && instigator != ally)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(instigator, ally, alliance.IsSupportTriggered ? -18 : -10, false);
        }

        if (_clanStates.TryGetValue(alliance.SupportedClan, out ClanIntrigueState state))
        {
            state.Suspicion += alliance.IsSupportTriggered ? 12f : 6f;
            state.TrustToPlayer -= alliance.InstigatorClan == Clan.PlayerClan ? 10f : 0f;
            state.ClampValues();
        }

        if (alliance.InstigatorClan == Clan.PlayerClan || alliance.SettlementRecipientClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_broken_title}Secret Bargain Collapsed");
            TextObject body = new TextObject("{=rf_si_alliance_broken_body}The promised transfer of {SETTLEMENT} tied to the struggle in {KINGDOM} never came. Your terms with {ALLY} have soured, and trust has collapsed.");
            body.SetTextVariable("ALLY", alliance.AllyClan?.Name ?? new TextObject("{=rf_si_unknown_ally}your ally"));
            body.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}the promised fief"));
            body.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            ShowIntrigueInquiry(title, body);
        }
    }

    private bool HasPendingBreakOperation(Clan clan)
    {
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.TargetClan == clan
            && x.Type == IntrigueOperationType.PrepareBreakaway);
    }

    private bool CanTriggerClaimantCoup(Clan targetClan, out TextObject reason)
    {
        reason = TextObject.GetEmpty();
        if (targetClan?.Kingdom == null || !_kingdomStates.TryGetValue(targetClan.Kingdom, out KingdomIntrigueState kingdomState))
        {
            reason = new TextObject("{=si_claimant_no_kingdom}There is no viable royal crisis to exploit here.");
            return false;
        }

        float rulingStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(targetClan.Kingdom.RulingClan);
        float claimantStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(targetClan);
        bool claimantHasStanding = targetClan.Influence >= targetClan.Kingdom.RulingClan.Influence * 0.55f
            || claimantStrength >= rulingStrength * 0.8f;

        if (kingdomState.RulerLegitimacy > StrategicIntrigueConstants.ClaimantCoupLegitimacyThreshold)
        {
            reason = new TextObject("{=si_claimant_legitimacy_too_high}The crown is still too secure for a claimant move.");
            return false;
        }

        if (kingdomState.CourtFragmentation < StrategicIntrigueConstants.ClaimantCoupFragmentationThreshold)
        {
            reason = new TextObject("{=si_claimant_court_too_orderly}The court has not fractured enough to back a new ruler.");
            return false;
        }

        if (!claimantHasStanding)
        {
            reason = new TextObject("{=si_claimant_not_strong_enough}This lord lacks the weight to seize the crown, even now.");
            return false;
        }

        return true;
    }

    private void ProcessRulerCountermoves()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsValidIntrigueKingdom(kingdom) || !_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState kingdomState))
            {
                continue;
            }

            if ((CampaignTime.Now - kingdomState.LastCountermoveAt).ToDays < StrategicIntrigueConstants.RulerCountermoveCooldownDays)
            {
                continue;
            }

            ClanIntrigueState threatenedState = null;
            SecretPact activePact = null;
            Clan threatenedClan = null;
            float highestThreat = 0f;

            foreach (Clan clan in kingdom.Clans)
            {
                if (!IsValidIntrigueClan(clan) || !_clanStates.TryGetValue(clan, out ClanIntrigueState state))
                {
                    continue;
                }

                SecretPact pact = _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
                float threat = state.Dissidence
                    + (state.Suspicion * 0.55f)
                    + (state.SoftDefectionPressure * 0.25f)
                    + (state.ClaimantAmbition * 0.2f)
                    + (pact != null ? 18f : 0f);
                if (threat <= highestThreat || threat < 80f)
                {
                    continue;
                }

                highestThreat = threat;
                threatenedState = state;
                threatenedClan = clan;
                activePact = pact;
            }

            if (threatenedState == null || threatenedClan == null)
            {
                continue;
            }

            bool useCrackdown = threatenedState.Suspicion >= 50f || activePact != null;
            if (useCrackdown)
            {
                threatenedState.FearOfRuler += 18f;
                threatenedState.Dissidence -= 8f;
                threatenedState.TrustToPlayer -= 14f;
                threatenedState.Suspicion += 12f;
                threatenedState.RoyalFavor += 3f;
                kingdomState.CourtFragmentation += 4f;
                kingdomState.RulerLegitimacy += 2f;

                if (activePact != null)
                {
                    activePact.Secrecy -= 22f;
                    activePact.ClampValues();
                    if (activePact.Secrecy <= 35f)
                    {
                        activePact.IsExposed = true;
                        threatenedState.Suspicion = 100f;
                        if (activePact.SponsorClan == Clan.PlayerClan)
                        {
                            TextObject title = new TextObject("{=rf_si_exposed_pact_title}Secret Pact Exposed");
                            TextObject body = new TextObject("{=rf_si_exposed_pact_body}Agents of {KINGDOM} have uncovered your private understanding with {CLAN}. Their unrest was already being fed by {REASONS}, and the court is now alert to your hand in it.");
                            body.SetTextVariable("KINGDOM", kingdom.Name);
                            body.SetTextVariable("CLAN", threatenedClan.Name);
                            body.SetTextVariable("REASONS", BuildDissidenceReasonSummary(threatenedClan, kingdom));
                            ShowIntrigueInquiry(title, body);
                        }
                    }
                }

                bool shouldApplyHeavyPunishment =
                    kingdom.RulingClan != Clan.PlayerClan
                    && ((activePact?.IsExposed ?? false)
                        || threatenedState.Suspicion >= StrategicIntrigueConstants.SeverePunishmentSuspicionThreshold
                        || highestThreat >= StrategicIntrigueConstants.SeverePunishmentThreatThreshold);

                if (shouldApplyHeavyPunishment)
                {
                    CrackdownPunishmentOutcome punishmentOutcome = ApplyCrackdownPunishment(kingdom, threatenedClan, threatenedState);
                    if (punishmentOutcome != CrackdownPunishmentOutcome.None)
                    {
                        RemoveDeadReferencesForClan(threatenedClan);

                        switch (punishmentOutcome)
                        {
                            case CrackdownPunishmentOutcome.Execution:
                                kingdomState.RulerLegitimacy -= 4f;
                                kingdomState.CourtFragmentation += 12f;
                                break;
                            case CrackdownPunishmentOutcome.Imprisonment:
                                kingdomState.CourtFragmentation += 8f;
                                break;
                            default:
                                kingdomState.RulerLegitimacy -= 1.5f;
                                kingdomState.CourtFragmentation += 6f;
                                break;
                        }

                        if ((activePact?.SponsorClan == Clan.PlayerClan) || threatenedClan == Clan.PlayerClan)
                        {
                            ShowCrackdownPunishmentNotification(kingdom, threatenedClan, punishmentOutcome);
                        }
                    }
                }
            }
            else
            {
                threatenedState.RoyalFavor += 15f;
                threatenedState.Dissidence -= 12f;
                threatenedState.VoteResentment -= 10f;
                threatenedState.FiefGrievance -= 8f;
                threatenedState.ClaimantAmbition -= 6f;
                kingdomState.RulerLegitimacy += 6f;
                kingdomState.CourtFragmentation -= 4f;
                kingdomState.ClaimantPressure -= 8f;
            }

            threatenedState.ClampValues();
            kingdomState.LastCountermoveAt = CampaignTime.Now;
            kingdomState.ClampValues();
        }
    }
    private static void HandlePartyOfDissident(Hero dissident)
    {
        var party = dissident.PartyBelongedTo;
        party.RemovePartyLeader();
        party.MemberRoster.RemoveTroop(dissident.CharacterObject, 1);
        Hero? heroToAdd;
        if (party.Owner.IsAlive && party.Owner.PartyBelongedTo == null && !party.Owner.IsPrisoner)
            heroToAdd = party.Owner;
        else heroToAdd = party.Owner.Clan.AliveLords.FirstOrDefault(l => l.PartyBelongedTo == null && !l.IsPrisoner && !l.IsChild);
        if (heroToAdd == null)
            DestroyPartyAction.Apply(null, party);
        else
        {
            AddHeroToPartyAction.Apply(heroToAdd, party);
            party.ChangePartyLeader(heroToAdd);
            party.LordPartyComponent.ClearCachedName();
        }
    }

    private CrackdownPunishmentOutcome ApplyCrackdownPunishment(
        Kingdom kingdom,
        Clan threatenedClan,
        ClanIntrigueState threatenedState)
    {
        Hero ruler = kingdom?.RulingClan?.Leader;
        Hero dissident = threatenedClan?.Leader;
        if (ruler == null || dissident == null)
        {
            return CrackdownPunishmentOutcome.None;
        }

        int relationToRuler = dissident.GetRelation(ruler);

        threatenedState.TrustToPlayer -= 28f;
        threatenedState.Dissidence -= 18f;
        threatenedState.Suspicion += 18f;
        threatenedState.FearOfRuler += 30f;
        threatenedState.RoyalFavor = 0f;

        if (dissident != Hero.MainHero
            && relationToRuler <= StrategicIntrigueConstants.ExecutionRelationThreshold
            && dissident.IsAlive
            && !dissident.IsChild
            && CanApplyIntrigueExecution(kingdom, threatenedClan))
        {
            KillCharacterAction.ApplyByExecution(dissident, ruler, showNotification: true, isForced: true);
            _lastIntrigueExecutionAt = CampaignTime.Now;
            ApplyPostCrackdownStabilization(threatenedState, CrackdownPunishmentOutcome.Execution);
            return CrackdownPunishmentOutcome.Execution;
        }

        if (relationToRuler <= StrategicIntrigueConstants.ImprisonmentRelationThreshold
            && TryImprisonDissident(kingdom, dissident))
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(dissident, ruler, -20, false);
            ApplyPostCrackdownStabilization(threatenedState, CrackdownPunishmentOutcome.Imprisonment);
            return CrackdownPunishmentOutcome.Imprisonment;
        }

        if (TryExileClan(threatenedClan))
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(dissident, ruler, -30, false);
            ApplyPostCrackdownStabilization(threatenedState, CrackdownPunishmentOutcome.Exile);
            return CrackdownPunishmentOutcome.Exile;
        }

        if (dissident != Hero.MainHero && TryImprisonDissident(kingdom, dissident))
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(dissident, ruler, -8, false);
            ApplyPostCrackdownStabilization(threatenedState, CrackdownPunishmentOutcome.Imprisonment);
            return CrackdownPunishmentOutcome.Imprisonment;
        }

        threatenedState.ClampValues();
        return CrackdownPunishmentOutcome.None;
    }

    private static void ApplyPostCrackdownStabilization(ClanIntrigueState state, CrackdownPunishmentOutcome outcome)
    {
        if (state == null)
        {
            return;
        }

        switch (outcome)
        {
            case CrackdownPunishmentOutcome.Execution:
            case CrackdownPunishmentOutcome.Exile:
                state.Dissidence = 0f;
                state.TrustToPlayer = 0f;
                state.Suspicion = Math.Min(state.Suspicion, 35f);
                state.SoftDefectionPressure -= 35f;
                state.ClaimantAmbition -= 25f;
                break;
            case CrackdownPunishmentOutcome.Imprisonment:
                state.Dissidence -= 35f;
                state.Suspicion = Math.Min(state.Suspicion, 45f);
                state.SoftDefectionPressure -= 20f;
                state.ClaimantAmbition -= 15f;
                break;
        }

        state.ClampValues();
    }

    private bool CanApplyIntrigueExecution(Kingdom kingdom, Clan threatenedClan)
    {
        if (_lastIntrigueExecutionAt != CampaignTime.Zero
            && (CampaignTime.Now - _lastIntrigueExecutionAt).ToDays < StrategicIntrigueConstants.ExecutionGlobalCooldownDays)
        {
            return false;
        }

        int worldAliveLords = Hero.AllAliveHeroes.Count(IsLivingNobleLord);
        if (worldAliveLords < StrategicIntrigueConstants.ExecutionMinimumWorldAliveLordCount)
        {
            return false;
        }

        int kingdomAliveLords = kingdom?.AliveLords?.Count() ?? 0;
        if (kingdomAliveLords < StrategicIntrigueConstants.ExecutionMinimumKingdomAliveLordCount)
        {
            return false;
        }

        int clanAliveLords = threatenedClan?.AliveLords?.Count() ?? 0;
        return clanAliveLords > StrategicIntrigueConstants.ExecutionMinimumClanAliveLordCount;
    }

    private static bool IsLivingNobleLord(Hero hero)
    {
        return hero != null
            && hero.IsAlive
            && hero.IsLord
            && hero.Clan != null
            && hero.Clan.IsNoble
            && !hero.Clan.IsMinorFaction
            && !hero.Clan.IsEliminated;
    }

    private static bool TryImprisonDissident(Kingdom kingdom, Hero dissident)
    {
        if (dissident == null || dissident == Hero.MainHero || !dissident.IsAlive || dissident.IsPrisoner)
        {
            return false;
        }

        PartyBase capturerParty = GetRulerCapturerParty(kingdom);
        if (capturerParty == null)
        {
            return false;
        }

        if (dissident.PartyBelongedTo != null)
            HandlePartyOfDissident(dissident);
        TakePrisonerAction.Apply(capturerParty, dissident);
        return true;
    }

    private static bool TryExileClan(Clan clan)
    {
        if (clan?.Kingdom == null)
        {
            return false;
        }

        ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
        return true;
    }

    private static PartyBase GetRulerCapturerParty(Kingdom kingdom)
    {
        Hero ruler = kingdom?.RulingClan?.Leader;
        if (ruler?.PartyBelongedTo?.Party != null)
        {
            return ruler.PartyBelongedTo.Party;
        }

        if (ruler?.CurrentSettlement?.Party != null)
        {
            return ruler.CurrentSettlement.Party;
        }

        return kingdom?.RulingClan?.Settlements?.FirstOrDefault()?.Party;
    }

    private string BuildDissidenceReasonSummary(Clan clan, Kingdom kingdom)
    {
        List<(string Text, float Weight)> reasons = new();
        ClanIntrigueState state = clan != null && _clanStates.TryGetValue(clan, out ClanIntrigueState clanState) ? clanState : null;
        KingdomIntrigueState kingdomState = kingdom != null && _kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState stateForKingdom) ? stateForKingdom : null;

        if (clan?.Leader != null && kingdom?.RulingClan?.Leader != null && clan != kingdom.RulingClan)
        {
            // RulingClan.Leader can be null during a regency / leader-death window.
        int relationToRuler = (kingdom.RulingClan?.Leader != null && clan.Leader != null)
            ? clan.Leader.GetRelation(kingdom.RulingClan.Leader)
            : 0;
            if (relationToRuler < -10)
            {
                reasons.Add(("personal hostility toward the ruler", -relationToRuler));
            }
        }

        if (state != null)
        {
            if (state.FiefGrievance >= 35f)
            {
                reasons.Add(("anger over land and rewards", state.FiefGrievance));
            }

            if (state.VoteResentment >= 35f)
            {
                reasons.Add(("resentment over court decisions", state.VoteResentment));
            }

            if (state.MilitaryFrustration >= 35f)
            {
                reasons.Add(("military frustration and bad campaigning", state.MilitaryFrustration));
            }

            if (state.ClaimantAmbition >= 35f)
            {
                reasons.Add(("ambition for the crown", state.ClaimantAmbition));
            }

            if (state.SoftDefectionPressure >= 35f)
            {
                reasons.Add(("pressure to abandon the realm", state.SoftDefectionPressure));
            }
        }

        if (kingdomState != null)
        {
            if (kingdomState.RulerLegitimacy <= 45f)
            {
                reasons.Add(("a weakening royal legitimacy", 100f - kingdomState.RulerLegitimacy));
            }

            if (kingdomState.CourtFragmentation >= 35f)
            {
                reasons.Add(("a fractured court", kingdomState.CourtFragmentation));
            }

            if (kingdomState.WarExhaustion >= 35f)
            {
                reasons.Add(("war exhaustion", kingdomState.WarExhaustion));
            }

            if (kingdomState.RebellionPressure >= 35f)
            {
                reasons.Add(("unrest across the realm", kingdomState.RebellionPressure));
            }
        }

        List<string> topReasons = reasons
            .OrderByDescending(x => x.Weight)
            .Select(x => x.Text)
            .Distinct()
            .Take(3)
            .ToList();

        return topReasons.Count == 0
            ? "private grievances and a weakening court"
            : JoinReasonFragments(topReasons);
    }

    private static string JoinReasonFragments(IReadOnlyList<string> reasons)
    {
        return reasons.Count switch
        {
            0 => "private grievances",
            1 => reasons[0],
            2 => $"{reasons[0]} and {reasons[1]}",
            _ => $"{reasons[0]}, {reasons[1]}, and {reasons[2]}"
        };
    }

    private void ShowCrackdownPunishmentNotification(
        Kingdom kingdom,
        Clan threatenedClan,
        CrackdownPunishmentOutcome outcome)
    {
        TextObject title;
        TextObject body;
        string reasons = BuildDissidenceReasonSummary(threatenedClan, kingdom);
        switch (outcome)
        {
            case CrackdownPunishmentOutcome.Execution:
                title = new TextObject("{=rf_si_crackdown_execute_title}Dissident Executed");
                body = new TextObject("{=rf_si_crackdown_execute_body}{CLAN} of {KINGDOM} has been crushed completely. {RULER} chose the axe over mercy after unrest driven by {REASONS} was laid bare.");
                break;
            case CrackdownPunishmentOutcome.Imprisonment:
                title = new TextObject("{=rf_si_crackdown_prison_title}Dissident Imprisoned");
                body = new TextObject("{=rf_si_crackdown_prison_body}{CLAN} of {KINGDOM} has been seized by the crown. The suspected dissident now sits in chains after tensions over {REASONS} finally turned into a crackdown.");
                break;
            default:
                title = new TextObject("{=rf_si_crackdown_exile_title}Dissident Banished");
                body = new TextObject("{=rf_si_crackdown_exile_body}{CLAN} has been cast out of {KINGDOM}. Old ties were not enough to save them once disloyalty fed by {REASONS} was exposed.");
                break;
        }

        body.SetTextVariable("CLAN", threatenedClan?.Name ?? new TextObject("{=rf_si_unknown_clan}the clan"));
        body.SetTextVariable("KINGDOM", kingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
        body.SetTextVariable("RULER", kingdom?.RulingClan?.Leader?.Name ?? new TextObject("{=rf_si_unknown_ruler}the ruler"));
        body.SetTextVariable("REASONS", reasons);
        ShowIntrigueInquiry(title, body);
    }

    private void ShowOperationResolutionNotifications(IEnumerable<IntrigueOperationResolution> resolutions)
    {
        foreach (IntrigueOperationResolution resolution in resolutions)
        {
            if (resolution.TargetClan == null)
            {
                continue;
            }

            if (resolution.Type is not IntrigueOperationType.RumorCampaign and not IntrigueOperationType.SponsorDissidence)
            {
                if (resolution.BreakOutcome != IntrigueBreakOutcome.None)
                {
                    ShowBreakOutcomeNotification(resolution);
                }

                continue;
            }

            bool playerInvolved = resolution.InstigatorClan == Clan.PlayerClan
                || resolution.TargetClan == Clan.PlayerClan
                || resolution.TargetRulerClan == Clan.PlayerClan;
            if (!playerInvolved)
            {
                continue;
            }

            TextObject title = new TextObject("{=rf_si_rumor_popup_title}Rumor Network Report");
            Kingdom rumorKingdom = resolution.TargetClan.Kingdom;
            string reasons = BuildDissidenceReasonSummary(resolution.TargetClan, rumorKingdom);
            TextObject body = resolution.WasExposed
                ? new TextObject("{=rf_si_rumor_popup_exposed}The whispers around {CLAN} of {KINGDOM} were noticed at court. Suspicion is rising, but the strain over {REASONS} is still real.")
                : new TextObject("{=rf_si_rumor_popup_resolved}The whispers around {CLAN} of {KINGDOM} have taken hold. Court opinion is shifting against the current order, especially over {REASONS}.");
            body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            body.SetTextVariable("KINGDOM", rumorKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            body.SetTextVariable("REASONS", reasons);
            string baseBody = body.ToString();

            if (resolution.BecameBreakawayReady)
            {
                body = new TextObject("{=rf_si_rumor_popup_breakaway}{BASE}\n\nThis has pushed {CLAN} into open breakaway territory. Your private understanding can now be turned into action.");
                body.SetTextVariable("BASE", baseBody);
                body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            }
            else if (resolution.BecameConspirable)
            {
                body = new TextObject("{=rf_si_rumor_popup_conspirable}{BASE}\n\n{CLAN} is now ready for a deeper private arrangement against their ruler.");
                body.SetTextVariable("BASE", baseBody);
                body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            }

            ShowIntrigueInquiry(title, body);
        }
    }

    private void ShowNewBreakawayReadyNotifications(
        Dictionary<Clan, bool> readinessBeforeTick,
        HashSet<Clan> alreadyReported)
    {
        foreach (KeyValuePair<Clan, bool> entry in readinessBeforeTick)
        {
            Clan clan = entry.Key;
            bool wasReady = entry.Value;
            if (clan == null
                || wasReady
                || alreadyReported.Contains(clan)
                || !_secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan && x.SponsorClan == Clan.PlayerClan)
                || GetState(clan)?.IsBreakawayReady != true)
            {
                continue;
            }

            TextObject title = new TextObject("{=rf_si_breakaway_popup_title}Conspiracy Hardened");
            TextObject body = new TextObject("{=rf_si_breakaway_popup_body}{CLAN} of {KINGDOM} is now prepared to move from secret understanding to open rupture. The pressure is being driven by {REASONS}. If you speak with them again, you can press for decisive action.");
            body.SetTextVariable("CLAN", clan.Name);
            body.SetTextVariable("KINGDOM", clan.Kingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            body.SetTextVariable("REASONS", BuildDissidenceReasonSummary(clan, clan.Kingdom));
            ShowIntrigueInquiry(title, body);
        }
    }

    private static void ShowIntrigueInquiry(TextObject title, TextObject body)
    {
        InformationManager.ShowInquiry(new InquiryData(
            title.ToString(),
            body.ToString(),
            true,
            false,
            new TextObject("{=rf_si_popup_ack}Understood").ToString(),
            string.Empty,
            null,
            null));
    }

    private void ShowBreakOutcomeNotification(IntrigueOperationResolution resolution)
    {
        if (resolution.TargetClan == null)
        {
            return;
        }

        TextObject title;
        TextObject body;
        Kingdom originKingdom = resolution.OriginKingdom ?? resolution.TargetClan.Kingdom;
        string reasons = BuildDissidenceReasonSummary(resolution.TargetClan, originKingdom);
        switch (resolution.BreakOutcome)
        {
            case IntrigueBreakOutcome.Defection:
                title = new TextObject("{=rf_si_defection_popup_title}Secret Defection");
                body = resolution.InstigatorClan == Clan.PlayerClan
                    ? new TextObject("{=rf_si_defection_popup_body}{CLAN} has broken from {KINGDOM} and entered your orbit as planned. The split was driven by {REASONS}.")
                    : new TextObject("{=rf_si_defection_popup_body_ai}{CLAN} has broken from {KINGDOM} and entered the protection of {SPONSOR}. The split was driven by {REASONS}.");
                body.SetTextVariable("SPONSOR", resolution.InstigatorClan?.Name ?? new TextObject("{=rf_si_unknown_sponsor}another power"));
                break;
            case IntrigueBreakOutcome.ClaimantCoup:
                title = new TextObject("{=rf_si_claimant_popup_title}Claimant Rising");
                body = new TextObject("{=rf_si_claimant_popup_body}{CLAN} has moved from conspiracy to coup inside {KINGDOM}. The bid for the crown was fed by {REASONS}.");
                break;
            default:
                title = new TextObject("{=rf_si_break_popup_title}Realm Fractured");
                body = new TextObject("{=rf_si_break_popup_body}{CLAN} has broken openly with {KINGDOM}. The conspiracy has turned into rebellion after pressure over {REASONS}.");
                break;
        }

        body.SetTextVariable("CLAN", resolution.TargetClan.Name);
        body.SetTextVariable("KINGDOM", originKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
        body.SetTextVariable("REASONS", reasons);
        ShowIntrigueInquiry(title, body);
    }

    private void RemoveDeadReferencesForClan(Clan clan)
    {
        _secretPacts.RemoveAll(x => x.MatchesClan(clan));
        _secretAlliances.RemoveAll(x => x.MatchesClan(clan));
        _pendingOperations.RemoveAll(x =>
            x.TargetClan == clan || x.InstigatorClan == clan || x.TargetRulerClan == clan);
    }

    private void RefreshKingdomObjectiveState(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return;
        }

        KingdomObjectiveType resolvedObjective = KingdomObjectiveService.ResolveObjective(kingdom);
        if (state.ObjectiveType != resolvedObjective)
        {
            state.ObjectiveType = resolvedObjective;
            state.ObjectiveMilestone = 0;
            state.ObjectiveProgress = 0f;
            state.ObjectivePressure = 0f;
            state.ObjectiveMomentum = 0f;
            state.RivalAgendaStrength = 0f;
            state.PlayerSupportsRivalAgenda = false;
        }

        if (state.ObjectiveType == KingdomObjectiveType.None)
        {
            state.RivalAgendaStrength = 0f;
            state.PlayerSupportsRivalAgenda = false;
            return;
        }

        float previousProgress = state.ObjectiveProgress;
        int previousMilestone = state.ObjectiveMilestone;
        float targetProgress = KingdomObjectiveService.EvaluateProgress(kingdom, state);
        state.ObjectiveProgress = MBMath.ClampFloat((state.ObjectiveProgress * 0.72f) + (targetProgress * 0.28f), 0f, 100f);
        state.ObjectiveMomentum = MBMath.ClampFloat(state.ObjectiveProgress - previousProgress, -25f, 25f);

        float pressureTarget = KingdomObjectiveService.EvaluatePressure(kingdom, state);
        state.ObjectivePressure = MBMath.ClampFloat((state.ObjectivePressure * 0.68f) + (pressureTarget * 0.32f), 0f, 100f);

        int milestone = GetObjectiveMilestone(state.ObjectiveProgress);
        state.ObjectiveMilestone = milestone;

        if (_isInitialized && milestone != previousMilestone)
        {
            HandleObjectiveMilestoneChange(kingdom, state, previousMilestone, milestone);
        }
    }

    /// <summary>Hands the realm's grand design to the war director. Without this
    /// the designs never leave the court: the director runs its own objective
    /// model and has no idea a realm was ever meant to covet anything.
    ///
    /// Two channels, deliberately unequal. Every day the design nudges WHO the
    /// realm resents and WHERE it wants to march, which biases the director's own
    /// war and target choices — so a design shows up as organic appetite. Only a
    /// court that is desperate, idle and getting nowhere additionally presses for
    /// the declaration itself.</summary>
    private void PushGrandDesignWarIntent(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return;
        }

        // The giants covet nothing. Their war comes to them, never from them.
        if (state.ObjectiveType == KingdomObjectiveType.GuardianFrenzy)
        {
            return;
        }

        Kingdom target = SelectGrandDesignTarget(kingdom, state.ObjectiveType);
        if (target == null)
        {
            Mechanics.KingdomObjectives.KingdomObjectiveTrace.Write(kingdom, state, null, 0f, false, "no_target");
            return;
        }

        float conviction = MBMath.ClampFloat(
            (state.ObjectivePressure * 0.7f) + (Math.Max(0f, 60f - state.ObjectiveProgress) * 0.5f),
            12f,
            100f) / 100f;

        bool pressForWar = ShouldPressGrandDesignWar(kingdom, state);
        if (Mechanics.KingdomObjectives.KingdomObjectiveTrace.Enabled)
        {
            Mechanics.KingdomObjectives.KingdomObjectiveTrace.Write(
                kingdom, state, target, conviction, pressForWar,
                pressForWar ? "-" : GetGrandDesignWarBlocker(kingdom, state));
        }

        RFWarExternalIntentApi.ReinforceGrandDesignIntent(
            kingdom,
            target,
            GetCovetedSettlements(target, state.ObjectiveType),
            conviction,
            pressForWar);
    }

    /// <summary>The one realm the design is pointed at right now. A design already
    /// at war with one of its targets stays pointed at that war — that is the case
    /// where the bridge earns its keep, aiming the armies at the right ground
    /// instead of wherever the director happened to drift.</summary>
    private static Kingdom SelectGrandDesignTarget(Kingdom kingdom, KingdomObjectiveType objectiveType)
    {
        Kingdom activeWarTarget = kingdom.FactionsAtWarWith
            .OfType<Kingdom>()
            .FirstOrDefault(enemy => IsObjectiveTargetKingdom(kingdom, objectiveType, enemy));
        if (activeWarTarget != null)
        {
            return activeWarTarget;
        }

        Kingdom best = null;
        float bestScore = float.MinValue;
        foreach (Kingdom candidate in Kingdom.All)
        {
            if (!IsObjectiveTargetKingdom(kingdom, objectiveType, candidate)
                || kingdom.IsAtWarWith(candidate)
                || !candidate.Fiefs.Any())
            {
                continue;
            }

            float score = GetGrandDesignTargetScore(kingdom, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>Prefer the weak and the near. Designs that name no particular
    /// enemy (Wulf glory, Nord colonies) would otherwise pick a victim across
    /// the map from a realm they have never met.</summary>
    private static float GetGrandDesignTargetScore(Kingdom kingdom, Kingdom candidate)
    {
        float weakness = kingdom.CurrentTotalStrength / Math.Max(1f, candidate.CurrentTotalStrength);
        float score = MBMath.ClampFloat(weakness, 0f, 3f);

        float borderDistanceSquared = GetBorderDistanceSquared(kingdom, candidate);
        if (borderDistanceSquared < float.MaxValue)
        {
            float distance = (float)Math.Sqrt(borderDistanceSquared);
            score += MBMath.ClampFloat(1.5f - (distance / 250f), -0.75f, 1.5f);
        }

        return score;
    }

    /// <summary>Distance between the two realms' nearest holdings — how far the
    /// design would actually have to reach. Walked once per candidate per realm
    /// per day while the realm is not yet at war with a target of its design;
    /// Kingdom.Fiefs is a cached list, so this stays in the tens of thousands of
    /// float compares a day even in the worst case.</summary>
    private static float GetBorderDistanceSquared(Kingdom left, Kingdom right)
    {
        float nearest = float.MaxValue;
        foreach (Town leftFief in left.Fiefs)
        {
            if (leftFief?.Settlement == null)
            {
                continue;
            }

            foreach (Town rightFief in right.Fiefs)
            {
                if (rightFief?.Settlement == null)
                {
                    continue;
                }

                float distanceSquared = leftFief.Settlement.GatePosition.DistanceSquared(rightFief.Settlement.GatePosition);
                if (distanceSquared < nearest)
                {
                    nearest = distanceSquared;
                }
            }
        }

        return nearest;
    }

    /// <summary>The ground the design wants out of this target. A design that
    /// names no culture (the Nord colonies just want land) takes anything the
    /// target holds.</summary>
    private static IEnumerable<Settlement> GetCovetedSettlements(Kingdom target, KingdomObjectiveType objectiveType)
    {
        IEnumerable<Settlement> fiefs = target.Fiefs
            .Select(fief => fief?.Settlement)
            .Where(settlement => settlement != null);

        IReadOnlyList<string> covetedCultures = KingdomObjectiveService.GetCovetedCultureIds(objectiveType);
        return covetedCultures.Count == 0
            ? fiefs
            : fiefs.Where(settlement => covetedCultures.Contains(settlement.Culture?.StringId));
    }

    /// <summary>Whether the court is desperate enough to press for the declaration
    /// itself. A special request goes stale after 3 silent days, so this stays true
    /// day after day while the conditions hold and the daily push keeps it alive;
    /// it goes false the moment the war it asked for exists.</summary>
    /// <summary>Which gate (if any) stops the design from pressing for war —
    /// the diagnostic twin of <see cref="ShouldPressGrandDesignWar"/>. Kept
    /// beside it so the two can never drift apart.</summary>
    private string GetGrandDesignWarBlocker(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (!IsWarSeekingObjective(state.ObjectiveType)) return "not_war_seeking";
        if (HasObjectiveTargetWar(kingdom, state.ObjectiveType)) return "already_at_war_with_target";
        if (state.ObjectivePressure < StrategicIntrigueConstants.GrandDesignWarPressureThreshold) return "pressure_below_threshold";
        if (state.ObjectiveProgress >= StrategicIntrigueConstants.GrandDesignWarMaxProgress) return "progress_too_high";
        if (state.WarExhaustion >= StrategicIntrigueConstants.GrandDesignWarMaxExhaustion) return "war_exhaustion";
        if (kingdom.FactionsAtWarWith.Any(x => x.IsKingdomFaction)) return "second_front_guard";
        if (CampaignTime.Now < state.ObjectiveWarQuietUntil) return "quiet_period";
        return "-";
    }

    private bool ShouldPressGrandDesignWar(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (!IsWarSeekingObjective(state.ObjectiveType)
            || HasObjectiveTargetWar(kingdom, state.ObjectiveType)
            || state.ObjectivePressure < StrategicIntrigueConstants.GrandDesignWarPressureThreshold
            || state.ObjectiveProgress >= StrategicIntrigueConstants.GrandDesignWarMaxProgress
            || state.WarExhaustion >= StrategicIntrigueConstants.GrandDesignWarMaxExhaustion)
        {
            return false;
        }

        // Never open a second front. A stalled design must not pile a war of
        // choice on top of a war the realm is already fighting.
        if (kingdom.FactionsAtWarWith.Any(x => x.IsKingdomFaction))
        {
            return false;
        }

        // Zero (never fought a design war, or an older save) is already in the past.
        return CampaignTime.Now >= state.ObjectiveWarQuietUntil;
    }

    /// <summary>A realm that has just fought the war its design demanded does not
    /// get to demand it again the week after the peace. Anchored to the end of the
    /// war on purpose: a rest measured from the declaration would elapse during a
    /// long war and expire before the fighting even stopped.</summary>
    private void StartGrandDesignWarQuietPeriod(Kingdom kingdom, Kingdom formerEnemy)
    {
        if (kingdom == null || formerEnemy == null)
        {
            return;
        }

        KingdomIntrigueState state = GetKingdomState(kingdom);
        if (state == null
            || state.ObjectiveType == KingdomObjectiveType.None
            || !IsObjectiveTargetKingdom(kingdom, state.ObjectiveType, formerEnemy))
        {
            return;
        }

        state.ObjectiveWarQuietUntil = CampaignTime.DaysFromNow(StrategicIntrigueConstants.GrandDesignWarQuietDays);
    }

    private void HandleObjectiveMilestoneChange(Kingdom kingdom, KingdomIntrigueState state, int previousMilestone, int newMilestone)
    {
        if (kingdom == null || state == null || newMilestone == previousMilestone)
        {
            return;
        }

        bool playerServesRealm = Clan.PlayerClan?.Kingdom == kingdom;
        bool shouldNotify = state.PlayerSupportsObjective || (playerServesRealm && newMilestone > previousMilestone);
        if (!shouldNotify)
        {
            return;
        }

        TextObject rewardNote = new TextObject(string.Empty);
        TextObject consequenceNote = new TextObject(string.Empty);
        if (newMilestone > previousMilestone && state.PlayerSupportsObjective)
        {
            rewardNote = MaybeGrantObjectiveReward(kingdom, state, newMilestone);
        }
        else if (newMilestone < previousMilestone && state.PlayerSupportsRivalAgenda)
        {
            rewardNote = MaybeGrantRivalAgendaReward(kingdom, state, newMilestone);
        }

        consequenceNote = newMilestone > previousMilestone
            ? ApplyObjectiveAdvanceConsequences(kingdom, state, newMilestone)
            : ApplyObjectiveSetbackConsequences(kingdom, state, newMilestone);

        TextObject title = newMilestone > previousMilestone
            ? new TextObject("{=rf_ko_progress_title}Realm Objective Advanced")
            : new TextObject("{=rf_ko_slip_title}Realm Objective Slipping");
        TextObject body = new TextObject("{=rf_ko_progress_body}{KINGDOM} is pursuing {OBJECTIVE}. {BRIEFING}{REWARD_NOTE}{CONSEQUENCE_NOTE}");
        body.SetTextVariable("KINGDOM", kingdom.Name);
        body.SetTextVariable("OBJECTIVE", KingdomObjectiveService.GetTitle(state.ObjectiveType));
        body.SetTextVariable("BRIEFING", KingdomObjectiveService.BuildBriefing(kingdom, state));
        body.SetTextVariable("REWARD_NOTE", rewardNote);
        body.SetTextVariable("CONSEQUENCE_NOTE", consequenceNote);
        ShowIntrigueInquiry(title, body);
    }

    private void ApplyObjectiveDirectiveEffects(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null)
        {
            return;
        }

        float strength = state.ObjectiveMilestone >= 3 ? 1.2f : 1f;
        int previousMilestone = state.ObjectiveMilestone;

        state.ObjectiveProgress = MBMath.ClampFloat(state.ObjectiveProgress + (4f * strength), 0f, 100f);
        state.ObjectiveMomentum = MBMath.ClampFloat(state.ObjectiveMomentum + (4.5f * strength), -25f, 25f);
        state.ObjectivePressure = MBMath.ClampFloat(state.ObjectivePressure - (7f * strength), 0f, 100f);

        switch (state.ObjectiveType)
        {
            case KingdomObjectiveType.CrushBattanianResistance:
                state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (10f * strength), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (4f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (3f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.NobleWealthSupremacy:
                PulseProsperity(GetPrimaryObjectiveFiefs(kingdom, 3), 120f * strength);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (6f * strength), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (3f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.PreserveBattanianHomelands:
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "battania"), 2.4f * strength, 1.4f * strength);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (8f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (4f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.UniteAseraiRealms:
            case KingdomObjectiveType.ClaimImperialLegitimacy:
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (8f * strength), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (8f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (6f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.ForgeBorderEmpire:
            case KingdomObjectiveType.ArcaneFrontier:
            case KingdomObjectiveType.DefileMountainHolds:
                state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (9f * strength), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (5f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (2f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.SecureMountainHolds:
                PulseSettlementSecurity(kingdom.Fiefs, 2.2f * strength, 1.2f * strength);
                state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (6f * strength), 0f, 100f);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (5f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.MartialGlory:
                state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (12f * strength), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (8f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (4f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.UnbreakableRealm:
                PulseSettlementSecurity(kingdom.Fiefs, 2.8f * strength, 1.8f * strength);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (7f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (5f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.GuardianFrenzy:
                // At peace the vigil hardens the slopes; once roused, the same
                // order becomes the fury that will not let the aggressor rest.
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "giant"), 2.6f * strength, 1.5f * strength);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (6f * strength), 0f, 100f);
                if (HasObjectiveTargetWar(kingdom, state.ObjectiveType))
                {
                    state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (9f * strength), 0f, 100f);
                    state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (7f * strength), 0f, 100f);
                }

                break;

            case KingdomObjectiveType.MercenaryCreed:
                PulseSettlementSecurity(kingdom.Fiefs, 2.4f * strength, 1.2f * strength);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (7f * strength), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (5f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.WovenAlliances:
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (6f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (7f * strength), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (4f * strength), 0f, 100f);
                break;

            case KingdomObjectiveType.ColonialExpansion:
                PulseProsperity(GetPrimaryObjectiveFiefs(kingdom, 3), 90f * strength);
                state.ObjectiveWarScore = MBMath.ClampFloat(state.ObjectiveWarScore + (8f * strength), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (5f * strength), 0f, 100f);
                break;
        }

        state.ObjectiveMilestone = GetObjectiveMilestone(state.ObjectiveProgress);
        state.ClampValues();

        if (_isInitialized && state.ObjectiveMilestone != previousMilestone)
        {
            HandleObjectiveMilestoneChange(kingdom, state, previousMilestone, state.ObjectiveMilestone);
        }
    }

    private TextObject MaybeGrantObjectiveReward(Kingdom kingdom, KingdomIntrigueState state, int newMilestone)
    {
        if (Clan.PlayerClan?.Kingdom != kingdom
            || (CampaignTime.Now - state.LastObjectiveRewardAt).ToDays < StrategicIntrigueConstants.KingdomObjectiveSupportRewardCooldownDays)
        {
            return new TextObject(string.Empty);
        }

        int influenceReward = newMilestone >= 4 ? 18 : 8;
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, influenceReward);

        Hero ruler = kingdom.RulingClan?.Leader;
        if (Hero.MainHero != null && ruler != null && ruler != Hero.MainHero)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, ruler, newMilestone >= 4 ? 4 : 2, false);
        }

        state.LastObjectiveRewardAt = CampaignTime.Now;
        TextObject rewardNote = new TextObject(" {=rf_ko_reward_note}Your support is noticed: you gain {REWARD} influence, and the court marks your service.");
        rewardNote.SetTextVariable("REWARD", influenceReward);
        return rewardNote;
    }

    private TextObject MaybeGrantRivalAgendaReward(Kingdom kingdom, KingdomIntrigueState state, int newMilestone)
    {
        if (Clan.PlayerClan?.Kingdom != kingdom
            || (CampaignTime.Now - state.LastObjectiveRewardAt).ToDays < (StrategicIntrigueConstants.KingdomObjectiveSupportRewardCooldownDays * 0.75f))
        {
            return new TextObject(string.Empty);
        }

        int influenceReward = newMilestone <= 0 ? 10 : 5;
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, influenceReward);
        state.LastObjectiveRewardAt = CampaignTime.Now;

        TextObject rewardNote = new TextObject(" {=rf_ko_rival_reward_note}The dissident bloc marks the doctrine's failure in your favor: you gain {REWARD} influence among the realm's dissatisfied voices.");
        rewardNote.SetTextVariable("REWARD", influenceReward);
        return rewardNote;
    }

    private TextObject ApplyObjectiveAdvanceConsequences(Kingdom kingdom, KingdomIntrigueState state, int newMilestone)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject(string.Empty);
        }

        float strength = newMilestone >= 4 ? 1.35f : 1f;
        switch (state.ObjectiveType)
        {
            case KingdomObjectiveType.NobleWealthSupremacy:
                if (kingdom.RulingClan?.Leader != null)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, kingdom.RulingClan.Leader, (int)(2500f * strength), true);
                }

                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (5f * strength), 0f, 100f);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (2f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_vlandia_up} The treasuries swell, noble confidence rises, and the great houses quiet for a time.");

            case KingdomObjectiveType.PreserveBattanianHomelands:
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "battania"), 1.4f * strength, 0.9f * strength);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (7f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (4f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_battania_up} The clans feel the old woods are holding. Homeland loyalty hardens and internal fear eases.");

            case KingdomObjectiveType.UniteAseraiRealms:
            case KingdomObjectiveType.ClaimImperialLegitimacy:
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (8f * strength), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (5f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (6f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_unify_up} The court smells legitimacy in the air. Rival claimants lose ground and wavering lords fall in line.");

            case KingdomObjectiveType.UnbreakableRealm:
                PulseSettlementSecurity(kingdom.Fiefs, 1.8f * strength, 1.2f * strength);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (6f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_grimwatch_up} The realm's defenses are vindicated. Garrison towns grow steadier and the people trust the walls again.");

            case KingdomObjectiveType.MartialGlory:
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (8f * strength), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (4f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (4f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_wulf_up} Victory steels the warrior nobles. Prestige rises, fatigue falls, and the court rallies behind strength.");

            default:
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (6f * strength), 0f, 100f);
                state.RulerLegitimacy = MBMath.ClampFloat(state.RulerLegitimacy + (4f * strength), 0f, 100f);
                return new TextObject("{=rf_ko_effect_expansion_up} The realm believes its grand design is working. Confidence returns to the court and the ruler stands taller.");
        }
    }

    private TextObject ApplyObjectiveSetbackConsequences(Kingdom kingdom, KingdomIntrigueState state, int newMilestone)
    {
        if (kingdom == null || state == null)
        {
            return new TextObject(string.Empty);
        }

        float severity = newMilestone <= 0 ? 1.25f : 1f;
        switch (state.ObjectiveType)
        {
            case KingdomObjectiveType.NobleWealthSupremacy:
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (7f * severity), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + (5f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_vlandia_down} The houses begin to count losses and blame the crown. Pride turns into private resentment.");

            case KingdomObjectiveType.PreserveBattanianHomelands:
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "battania"), -1.1f * severity, -0.7f * severity);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure + (8f * severity), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (4f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_battania_down} Each failure in the old woods spreads despair. The clans whisper that the homeland is slipping away.");

            case KingdomObjectiveType.UniteAseraiRealms:
            case KingdomObjectiveType.ClaimImperialLegitimacy:
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + (9f * severity), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (6f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_unify_down} Rival claimants gain courage. The dream of unity now breeds sharper division inside the court.");

            case KingdomObjectiveType.UnbreakableRealm:
                PulseSettlementSecurity(kingdom.Fiefs, -1.4f * severity, -0.8f * severity);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure + (8f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_grimwatch_down} Public weakness at the walls cuts deep. Once the myth of invulnerability cracks, doubt spreads fast.");

            case KingdomObjectiveType.MartialGlory:
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (7f * severity), 0f, 100f);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure + (4f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_wulf_down} Warrior pride sours into anger. If glory is denied for too long, the strongest lords begin to turn on each other.");

            default:
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (6f * severity), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + (5f * severity), 0f, 100f);
                return new TextObject("{=rf_ko_effect_expansion_down} The realm's design has stalled. Blame falls on the crown, and ambitious nobles start imagining a different order.");
        }
    }

    private static int GetObjectiveMilestone(float progress)
    {
        return progress switch
        {
            >= 95f => 4,
            >= 75f => 3,
            >= 50f => 2,
            >= 25f => 1,
            _ => 0
        };
    }

    private void ClearPlayerObjectiveSupportExcept(Kingdom kingdomToKeep)
    {
        foreach (KeyValuePair<Kingdom, KingdomIntrigueState> entry in _kingdomStates)
        {
            Kingdom kingdom = entry.Key;
            KingdomIntrigueState state = entry.Value;
            if (state != null && kingdom != kingdomToKeep)
            {
                state.PlayerSupportsObjective = false;
                state.PlayerSupportsRivalAgenda = false;
            }
        }
    }

    private void ApplyOngoingObjectiveEffects(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return;
        }

        float progressFactor = MBMath.ClampFloat((state.ObjectiveProgress - 40f) / 60f, 0f, 1f);
        float pressureFactor = MBMath.ClampFloat(state.ObjectivePressure / 100f, 0f, 1f);
        bool hasTargetWar = HasObjectiveTargetWar(kingdom, state.ObjectiveType);

        switch (state.ObjectiveType)
        {
            case KingdomObjectiveType.NobleWealthSupremacy:
                PulseProsperity(GetPrimaryObjectiveFiefs(kingdom, 2), 0.12f + (progressFactor * 0.16f));
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (progressFactor * 0.35f) + (pressureFactor * 0.22f), 0f, 100f);
                break;

            case KingdomObjectiveType.PreserveBattanianHomelands:
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "battania"), 0.18f + (progressFactor * 0.18f), 0.1f + (progressFactor * 0.12f));
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (progressFactor * 0.35f) + (pressureFactor * 0.28f), 0f, 100f);
                break;

            case KingdomObjectiveType.UniteAseraiRealms:
            case KingdomObjectiveType.ClaimImperialLegitimacy:
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (progressFactor * 0.5f) + (pressureFactor * 0.32f), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (progressFactor * 0.28f) + (pressureFactor * 0.18f), 0f, 100f);
                break;

            case KingdomObjectiveType.UnbreakableRealm:
                PulseSettlementSecurity(kingdom.Fiefs, 0.22f + (progressFactor * 0.22f), 0.14f + (progressFactor * 0.16f));
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (progressFactor * 0.3f) + (pressureFactor * 0.18f), 0f, 100f);
                break;

            case KingdomObjectiveType.MartialGlory:
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - ((hasTargetWar ? 0.42f : 0.08f) * progressFactor), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + ((!hasTargetWar ? 0.38f : 0f) * pressureFactor), 0f, 100f);
                break;

            // The three designs whose success state IS peace (author decision
            // 2026-07-15). They must never take the default branch: it fractures
            // a court for having no war on, which is exactly what these realms
            // are supposed to want.
            case KingdomObjectiveType.GuardianFrenzy:
                PulseSettlementSecurity(kingdom.Fiefs.Where(x => x.Settlement.Culture?.StringId == "giant"), 0.2f + (progressFactor * 0.2f), 0.12f + (progressFactor * 0.14f));
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - ((hasTargetWar ? 0.44f : 0.2f) * progressFactor), 0f, 100f);
                state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure - (progressFactor * 0.3f), 0f, 100f);
                break;

            case KingdomObjectiveType.MercenaryCreed:
                PulseSettlementSecurity(kingdom.Fiefs, 0.16f + (progressFactor * 0.18f), 0.1f + (progressFactor * 0.12f));
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (progressFactor * 0.32f) + (pressureFactor * 0.18f), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure - (progressFactor * 0.24f) + (pressureFactor * 0.12f), 0f, 100f);
                break;

            case KingdomObjectiveType.WovenAlliances:
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation - (progressFactor * 0.34f) + (pressureFactor * 0.16f), 0f, 100f);
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - (progressFactor * 0.26f), 0f, 100f);
                break;

            default:
                state.WarExhaustion = MBMath.ClampFloat(state.WarExhaustion - ((hasTargetWar ? 0.34f : 0f) * progressFactor), 0f, 100f);
                state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + ((!hasTargetWar ? 0.26f : 0f) * pressureFactor), 0f, 100f);
                state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + ((!hasTargetWar ? 0.18f : 0f) * pressureFactor), 0f, 100f);
                break;
        }
    }

    private void RefreshRivalAgendaState(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.ObjectiveType == KingdomObjectiveType.None)
        {
            return;
        }

        float baseStrength = (state.ObjectivePressure * 0.44f) + (state.CourtFragmentation * 0.31f) + (state.ClaimantPressure * 0.25f);
        bool hasTargetWar = HasObjectiveTargetWar(kingdom, state.ObjectiveType);

        switch (state.ObjectiveType)
        {
            case KingdomObjectiveType.MartialGlory:
                if (!hasTargetWar)
                {
                    baseStrength += 16f;
                }

                break;

            case KingdomObjectiveType.CrushBattanianResistance:
            case KingdomObjectiveType.ForgeBorderEmpire:
            case KingdomObjectiveType.ArcaneFrontier:
            case KingdomObjectiveType.DefileMountainHolds:
                if (!hasTargetWar)
                {
                    baseStrength += 10f;
                }

                break;

            case KingdomObjectiveType.UnbreakableRealm:
                if (kingdom.Fiefs.Count() > 2)
                {
                    baseStrength += 8f;
                }

                break;

            case KingdomObjectiveType.NobleWealthSupremacy:
                baseStrength += MathF.Min(14f, kingdom.Clans.Count(x => x != kingdom.RulingClan && x.Fiefs.Count() >= 2) * 2f);
                break;
        }

        if (state.PlayerSupportsRivalAgenda)
        {
            baseStrength += 8f;
        }

        state.RivalAgendaStrength = MBMath.ClampFloat((state.RivalAgendaStrength * 0.72f) + (baseStrength * 0.28f), 0f, 100f);
    }

    private void ApplyOngoingRivalAgendaEffects(Kingdom kingdom, KingdomIntrigueState state)
    {
        if (kingdom == null || state == null || state.RivalAgendaStrength <= 0f)
        {
            return;
        }

        float rivalFactor = MBMath.ClampFloat(state.RivalAgendaStrength / 100f, 0f, 1f);
        state.ObjectivePressure = MBMath.ClampFloat(state.ObjectivePressure + (0.24f * rivalFactor), 0f, 100f);
        state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (0.34f * rivalFactor), 0f, 100f);
        state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + (0.22f * rivalFactor), 0f, 100f);

        if (state.PlayerSupportsRivalAgenda)
        {
            state.ObjectivePressure = MBMath.ClampFloat(state.ObjectivePressure + 0.35f, 0f, 100f);
            state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + 0.28f, 0f, 100f);
            state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure + 0.16f, 0f, 100f);
        }
    }

    /// <summary>Refreshes only the layers a court-less realm still has. A realm of
    /// a single clan (a young colony, a mercenary band, a freshly proclaimed
    /// kingdom) has no vassals to resent the crown, so every clan-politics factor
    /// would read zero anyway — but it still carries a grand design, and used to
    /// be dropped from the feature entirely with no sign that anything was
    /// missing.</summary>
    private void RefreshObjectiveOnlyKingdomState(Kingdom kingdom)
    {
        if (!IsObjectiveCapableKingdom(kingdom) || IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState state = GetOrCreateKingdomState(kingdom);
        state.WarExhaustion = MBMath.ClampFloat((state.WarExhaustion * 0.65f) + GetWarExhaustionFactor(kingdom), 0f, 100f);
        state.RecentLosses = MBMath.ClampFloat(state.RecentLosses * 0.82f, 0f, 100f);
        RefreshKingdomObjectiveState(kingdom, state);
        ApplyOngoingObjectiveEffects(kingdom, state);
        state.LastUpdated = CampaignTime.Now;
        state.ClampValues();
    }

    private void RefreshKingdomState(Kingdom kingdom)
    {
        if (!IsValidIntrigueKingdom(kingdom))
        {
            RefreshObjectiveOnlyKingdomState(kingdom);
            return;
        }

        KingdomIntrigueState state = GetOrCreateKingdomState(kingdom);
        state.WarExhaustion = MBMath.ClampFloat((state.WarExhaustion * 0.65f) + GetWarExhaustionFactor(kingdom), 0f, 100f);
        state.RecentLosses = MBMath.ClampFloat(state.RecentLosses * 0.82f, 0f, 100f);
        state.RebellionPressure = MBMath.ClampFloat((state.RebellionPressure * 0.9f) + GetRebellionFactor(kingdom), 0f, 100f);
        state.CourtFragmentation = MBMath.ClampFloat((state.CourtFragmentation * 0.78f) + GetCourtFragmentationFactor(kingdom), 0f, 100f);
        state.ClaimantPressure = MBMath.ClampFloat((state.ClaimantPressure * 0.74f) + GetClaimantPressureFactor(kingdom), 0f, 100f);
        RefreshKingdomObjectiveState(kingdom, state);
        RefreshRivalAgendaState(kingdom, state);
        ApplyOngoingObjectiveEffects(kingdom, state);
        ApplyOngoingRivalAgendaEffects(kingdom, state);
        state.RebellionPressure = MBMath.ClampFloat(state.RebellionPressure + (state.ObjectivePressure * 0.04f), 0f, 100f);
        state.CourtFragmentation = MBMath.ClampFloat(state.CourtFragmentation + (state.ObjectivePressure * 0.06f), 0f, 100f);
        state.ClaimantPressure = MBMath.ClampFloat(state.ClaimantPressure + (state.ObjectivePressure * 0.05f), 0f, 100f);

        float legitimacy = 65f;
        legitimacy += GetRulerSupportFactor(kingdom);
        legitimacy += GetFiefSecurityFactor(kingdom);
        legitimacy += (state.ObjectiveProgress - 50f) * 0.08f;
        legitimacy -= state.WarExhaustion * 0.25f;
        legitimacy -= state.RecentLosses * 0.3f;
        legitimacy -= state.RebellionPressure * 0.32f;
        legitimacy -= state.CourtFragmentation * 0.18f;
        legitimacy -= state.ClaimantPressure * 0.22f;
        legitimacy -= state.ObjectivePressure * 0.14f;
        if (kingdom.Fiefs.Count() <= 2)
        {
            legitimacy -= 8f;
        }

        state.RulerLegitimacy = MBMath.ClampFloat(legitimacy, 0f, 100f);
        state.LastUpdated = CampaignTime.Now;
        state.ClampValues();
    }

    private float GetWarExhaustionFactor(Kingdom kingdom)
    {
        float pressure = kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction) * 8f;
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            float progress = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(kingdom, enemy).ResultNumber;
            if (progress < 0f)
            {
                pressure += MBMath.ClampFloat(-progress / 3f, 0f, 14f);
            }
            else
            {
                pressure -= MBMath.ClampFloat(progress / 6f, 0f, 5f);
            }
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetRebellionFactor(Kingdom kingdom)
    {
        float pressure = 0f;
        foreach (Town fief in kingdom.Fiefs)
        {
            if (!fief.IsTown)
            {
                continue;
            }

            pressure += Math.Max(0f, 40f - fief.Loyalty) * 0.12f;
            pressure += Math.Max(0f, 35f - fief.Security) * 0.08f;
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetCourtFragmentationFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        float fragmentation = 0f;
        float averageStrength = GetAverageClanStrength(kingdom);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            int relation = clan.Leader.GetRelation(kingdom.RulingClan.Leader);
            if (relation < 0)
            {
                fragmentation += -relation * 0.08f;
            }

            if (clan.Fiefs.Count() <= 1)
            {
                fragmentation += 2.5f;
            }

            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
            if (clanStrength > averageStrength * 1.1f)
            {
                fragmentation += 2f;
            }

            if (clan.Influence > kingdom.RulingClan.Influence * 0.65f)
            {
                fragmentation += 1.5f;
            }
        }

        return MBMath.ClampFloat(fragmentation, 0f, 100f);
    }

    private float GetClaimantPressureFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        float pressure = 0f;
        float rulerStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(kingdom.RulingClan);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || !_clanStates.TryGetValue(clan, out ClanIntrigueState state))
            {
                continue;
            }

            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
            if (clanStrength >= rulerStrength * 0.8f)
            {
                pressure += 4f;
            }

            if (state.ClaimantAmbition >= 50f)
            {
                pressure += (state.ClaimantAmbition - 45f) * 0.18f;
            }

            if (clan.Leader.GetRelation(kingdom.RulingClan.Leader) < -10)
            {
                pressure += 3f;
            }
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetRulerSupportFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        List<Clan> vassals = kingdom.Clans.Where(IsValidIntrigueClan).ToList();
        if (vassals.Count == 0)
        {
            return 0f;
        }

        float averageRelation = (float)vassals.Average(x => x.Leader.GetRelation(kingdom.RulingClan.Leader));
        return MBMath.ClampFloat(averageRelation * 0.18f, -18f, 18f);
    }

    private float GetFiefSecurityFactor(Kingdom kingdom)
    {
        List<Town> fiefs = kingdom.Fiefs.ToList();
        if (fiefs.Count == 0)
        {
            return -12f;
        }

        float averageLoyalty = fiefs.Where(x => x.IsTown).DefaultIfEmpty().Average(x => x?.Loyalty ?? 50f);
        float averageSecurity = fiefs.Where(x => x.IsTown).DefaultIfEmpty().Average(x => x?.Security ?? 50f);
        float loyaltyFactor = (averageLoyalty - 50f) * 0.16f;
        float securityFactor = (averageSecurity - 50f) * 0.12f;
        return MBMath.ClampFloat(loyaltyFactor + securityFactor, -12f, 12f);
    }

    private float GetAverageClanStrength(Kingdom kingdom)
    {
        return Math.Max(
            kingdom.Clans.Average(x => global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(x)),
            1f);
    }

    private static void PulseProsperity(IEnumerable<Town> fiefs, float prosperityDelta)
    {
        foreach (Town fief in fiefs.Where(x => x != null))
        {
            fief.Prosperity = Math.Max(0f, fief.Prosperity + prosperityDelta);
        }
    }

    private static void PulseSettlementSecurity(IEnumerable<Town> fiefs, float securityDelta, float loyaltyDelta)
    {
        foreach (Town fief in fiefs.Where(x => x != null))
        {
            fief.Security = MBMath.ClampFloat(fief.Security + securityDelta, 0f, 100f);
            if (fief.IsTown)
            {
                fief.Loyalty = MBMath.ClampFloat(fief.Loyalty + loyaltyDelta, 0f, 100f);
            }
        }
    }

    private static IEnumerable<Town> GetPrimaryObjectiveFiefs(Kingdom kingdom, int count)
    {
        return kingdom?.Fiefs
            .OrderByDescending(x => x.IsTown)
            .ThenByDescending(x => x.Prosperity)
            .Take(count)
            ?? Enumerable.Empty<Town>();
    }

    private bool HasObjectiveTargetWar(Kingdom kingdom, KingdomObjectiveType objectiveType)
    {
        if (kingdom == null)
        {
            return false;
        }

        return kingdom.FactionsAtWarWith
            .OfType<Kingdom>()
            .Any(enemy => IsObjectiveTargetKingdom(kingdom, objectiveType, enemy));
    }

    /// <summary>Whether <paramref name="candidate"/> is a realm this design names
    /// as its target. Single source of truth for both "is the realm already
    /// fighting the war its design demands?" (HasObjectiveTargetWar) and "which
    /// realm should it be fighting?" (the war-system bridge).</summary>
    private static bool IsObjectiveTargetKingdom(Kingdom kingdom, KingdomObjectiveType objectiveType, Kingdom candidate)
    {
        if (kingdom == null || candidate == null || candidate == kingdom || candidate.IsEliminated)
        {
            return false;
        }

        return objectiveType switch
        {
            KingdomObjectiveType.CrushBattanianResistance or KingdomObjectiveType.ArcaneFrontier
                => candidate.StringId == "battania",
            KingdomObjectiveType.SecureMountainHolds
                => candidate.StringId == "urkhai_kingdom",
            KingdomObjectiveType.DefileMountainHolds
                => candidate.StringId == "dwarf_kingdom",
            KingdomObjectiveType.ForgeBorderEmpire
                => IsKhuzaitBorderTarget(candidate),
            KingdomObjectiveType.UniteAseraiRealms
                => IsAseraiRealm(candidate),
            KingdomObjectiveType.ClaimImperialLegitimacy
                => IsImperialRealm(candidate),
            KingdomObjectiveType.PreserveBattanianHomelands
                => candidate.StringId == "sturgia" || candidate.StringId == "mage_kingdom" || candidate.Culture?.StringId == "mage",
            // No named enemy: any realm will do.
            KingdomObjectiveType.MartialGlory
                or KingdomObjectiveType.GuardianFrenzy
                or KingdomObjectiveType.ColonialExpansion
                => true,
            _ => false
        };
    }

    /// <summary>Designs that actively want a war, as opposed to those that only
    /// want to hold what they have. GuardianFrenzy is NOT here: the giants never
    /// seek anyone — their fury is wired to being attacked
    /// (<see cref="TriggerGuardianFrenzyIfAttacked"/>).</summary>
    private static bool IsWarSeekingObjective(KingdomObjectiveType objectiveType)
    {
        return IsExpansionistObjective(objectiveType)
            || objectiveType is KingdomObjectiveType.MartialGlory
            or KingdomObjectiveType.ColonialExpansion;
    }

    private static bool IsExpansionistObjective(KingdomObjectiveType objectiveType)
    {
        return objectiveType is KingdomObjectiveType.CrushBattanianResistance
            or KingdomObjectiveType.UniteAseraiRealms
            or KingdomObjectiveType.ClaimImperialLegitimacy
            or KingdomObjectiveType.ForgeBorderEmpire
            or KingdomObjectiveType.ArcaneFrontier
            or KingdomObjectiveType.SecureMountainHolds
            or KingdomObjectiveType.DefileMountainHolds;
    }

    private static bool IsAseraiRealm(Kingdom kingdom)
    {
        return kingdom?.StringId is "aserai" or "aserai_a" or "aserai_b" or "aserai_c" or "aserai_d" or "aserai_e"
            || kingdom?.Culture?.StringId == "aserai";
    }

    private static bool IsImperialRealm(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return false;
        }

        string cultureId = kingdom.Culture?.StringId;
        return kingdom.StringId is "empire" or "empire_w" or "empire_s" or "south_realm" or "west_realm"
            || cultureId == "empire"
            || cultureId == "south_realm"
            || cultureId == "west_realm";
    }

    private static bool IsKhuzaitBorderTarget(Kingdom kingdom)
    {
        return kingdom?.StringId is "sturgia" or "empire" or "empire_w" or "empire_s" or "south_realm" or "west_realm";
    }

    private static bool IsValidIntrigueClan(Clan clan)
    {
        if (clan == null || clan.Leader == null || clan.Kingdom == null)
        {
            return false;
        }

        if (clan.IsMinorFaction || clan.IsUnderMercenaryService || clan.IsEliminated)
        {
            return false;
        }

        return clan.Kingdom.Clans.Count > 1;
    }

    private static bool IsValidIntrigueKingdom(Kingdom kingdom)
    {
        return IsObjectiveCapableKingdom(kingdom) && kingdom.Clans.Count > 1;
    }

    /// <summary>A realm that can hold a grand design. Unlike
    /// <see cref="IsValidIntrigueKingdom"/> this does not demand vassals: the
    /// clan-politics layer needs a court to work on, a design does not.</summary>
    private static bool IsObjectiveCapableKingdom(Kingdom kingdom)
    {
        return kingdom != null
            && !kingdom.IsEliminated
            && kingdom.RulingClan?.Leader != null;
    }

    private bool CanAssignCompanionToEspionage(Hero companion)
    {
        return companion != null
            && companion.IsPlayerCompanion
            && !companion.IsDead
            && companion.PartyBelongedTo == MobileParty.MainParty
            && !_espionageOperations.Any(x => x.Status == EspionageOperationStatus.Pending && x.Companion == companion);
    }

    private Hero GetBestAvailableEspionageCompanion(Clan targetClan)
    {
        var troopRoster = MobileParty.MainParty?.MemberRoster?.GetTroopRoster();
        if (troopRoster == null)
        {
            return null;
        }

        return troopRoster
            .Select(x => x.Character?.HeroObject)
            .Where(CanAssignCompanionToEspionage)
            .OrderByDescending(x => CalculateEspionagePower(x, targetClan, EspionageOperationType.ClanInfiltration))
            .ThenByDescending(x => x.GetSkillValue(DefaultSkills.Roguery))
            .FirstOrDefault();
    }

    private float CalculateEspionagePower(Hero companion, Clan targetClan, EspionageOperationType operationType)
    {
        if (companion == null || targetClan == null)
        {
            return 0f;
        }

        float roguery = companion.GetSkillValue(DefaultSkills.Roguery);
        float scouting = companion.GetSkillValue(DefaultSkills.Scouting);
        float charm = companion.GetSkillValue(DefaultSkills.Charm);
        float relation = Hero.MainHero == null || targetClan.Leader == null ? 0f : targetClan.Leader.GetRelation(Hero.MainHero);
        float cultureBonus = companion.Culture == targetClan.Culture ? 8f : 0f;
        float relationBonus = MBMath.ClampFloat(relation * 0.25f, -10f, 12f);
        float typeBonus = operationType == EspionageOperationType.CourtListening ? 4f : 0f;

        return MBMath.ClampFloat((roguery * 0.42f) + (scouting * 0.22f) + (charm * 0.18f) + cultureBonus + relationBonus + typeBonus, 5f, 95f);
    }

    private float CalculateEspionageRisk(Hero companion, Clan targetClan, EspionageOperationType operationType)
    {
        ClanIntrigueState clanState = GetState(targetClan);
        KingdomIntrigueState kingdomState = GetKingdomState(targetClan?.Kingdom);
        float suspicion = clanState?.Suspicion ?? 35f;
        float royalFavor = clanState?.RoyalFavor ?? 30f;
        float courtPressure = kingdomState?.CourtFragmentation ?? 30f;
        float power = CalculateEspionagePower(companion, targetClan, operationType);
        float risk = 28f + (suspicion * 0.34f) + (royalFavor * 0.12f) - (power * 0.22f) - (courtPressure * 0.08f);
        return MBMath.ClampFloat(risk, 5f, 95f);
    }

    private float CalculateEspionageDurationDays(Hero companion, Clan targetClan, EspionageOperationType operationType)
    {
        float scouting = companion?.GetSkillValue(DefaultSkills.Scouting) ?? 0f;
        float infiltration = GetState(targetClan)?.Infiltration ?? 20f;
        float baseDays = operationType == EspionageOperationType.CourtListening ? 5.5f : 4.5f;
        float reduction = (scouting * 0.01f) + (infiltration * 0.015f);
        return MBMath.ClampFloat(baseDays - reduction, 2.5f, 7f);
    }

    private void RemoveCompanionForEspionage(Hero companion)
    {
        // Intentionally no-op for save stability.
        // Removing a player companion from MainParty without using Bannerlord's
        // official "companion mission / alternative solution" lifecycle can leave
        // the hero in an orphaned state across save/load.
    }

    private void ReturnCompanionFromEspionage(Hero companion)
    {
        if (companion == null || companion.IsDead || MobileParty.MainParty == null)
        {
            return;
        }

        if (companion.PartyBelongedTo != MobileParty.MainParty)
        {
            AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, false);
        }
    }

    private void RepairEspionageCompanions()
    {
        if (MobileParty.MainParty == null)
        {
            return;
        }

        foreach (EspionageOperation operation in _espionageOperations.Where(x => x?.Companion != null))
        {
            Hero companion = operation.Companion;
            if (companion.IsDead)
            {
                continue;
            }

            if (companion.PartyBelongedTo != MobileParty.MainParty)
            {
                try
                {
                    AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, false);
                }
                catch
                {
                    // Best-effort repair only. If Bannerlord rejects the add here,
                    // we leave the original state untouched rather than worsening it.
                }
            }
        }
    }

    private void ResolveEspionageOperations()
    {
        foreach (EspionageOperation operation in _espionageOperations.Where(x => x.IsDue).ToList())
        {
            ResolveEspionageOperation(operation);
        }
    }

    private void ResolveEspionageOperation(EspionageOperation operation)
    {
        if (operation == null)
        {
            return;
        }

        ReturnCompanionFromEspionage(operation.Companion);

        EspionageReport report = BuildEspionageReport(operation);
        report.ClampValues();
        _espionageReports.RemoveAll(x =>
            x.Type == report.Type
            && (report.Type == EspionageOperationType.CourtListening
                ? x.TargetKingdom == report.TargetKingdom
                : x.TargetClan == report.TargetClan));
        _espionageReports.Add(report);

        operation.Status = report.Outcome;
        ShowEspionageResolutionNotification(report);
    }

    private EspionageReport BuildEspionageReport(EspionageOperation operation)
    {
        if (operation == null || (operation.TargetClan == null && operation.TargetKingdom == null))
        {
            return new EspionageReport(
                operation?.Companion,
                operation?.Type ?? EspionageOperationType.ClanInfiltration,
                operation?.TargetClan,
                operation?.TargetKingdom,
                CampaignTime.Now,
                EspionageOperationStatus.Failed,
                EspionageReportConfidence.Low,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                "{=rf_si_espionage_action_none}wait and watch");
        }

        Kingdom targetKingdom = operation.TargetKingdom ?? operation.TargetClan?.Kingdom;
        ClanIntrigueState clanState = operation.TargetClan == null ? null : GetState(operation.TargetClan);
        KingdomIntrigueState kingdomState = GetKingdomState(targetKingdom);
        float power = operation.Power;
        float risk = operation.Risk;
        float successScore = power - risk + MBRandom.RandomFloatRanged(-18f, 18f);

        EspionageOperationStatus outcome;
        EspionageReportConfidence confidence;
        float accuracySpread;

        if (successScore >= 20f)
        {
            outcome = EspionageOperationStatus.Succeeded;
            confidence = EspionageReportConfidence.High;
            accuracySpread = 4f;
        }
        else if (successScore >= 0f)
        {
            outcome = EspionageOperationStatus.Partial;
            confidence = EspionageReportConfidence.Medium;
            accuracySpread = 9f;
        }
        else if (successScore <= -18f)
        {
            outcome = EspionageOperationStatus.Exposed;
            confidence = EspionageReportConfidence.Low;
            accuracySpread = 16f;
            if (clanState != null)
            {
                clanState.Suspicion += 10f;
                clanState.ClampValues();
            }
        }
        else
        {
            outcome = EspionageOperationStatus.Failed;
            confidence = EspionageReportConfidence.Low;
            accuracySpread = 22f;
        }

        float Estimate(float actual) => MBMath.ClampFloat(actual + MBRandom.RandomFloatRanged(-accuracySpread, accuracySpread), 0f, 100f);

        float estimatedDissidence = Estimate(clanState?.Dissidence ?? 0f);
        float estimatedTrust = Estimate(clanState?.TrustToPlayer ?? 0f);
        float estimatedFear = Estimate(clanState?.FearOfRuler ?? 0f);
        float estimatedClaimant = Estimate(clanState?.ClaimantAmbition ?? 0f);
        float estimatedBreakaway = MBMath.ClampFloat((estimatedDissidence * 0.55f) + (estimatedClaimant * 0.25f) - (estimatedFear * 0.15f), 0f, 100f);
        float estimatedLegitimacy = Estimate(kingdomState?.RulerLegitimacy ?? 50f);
        float estimatedFragmentation = Estimate(kingdomState?.CourtFragmentation ?? 40f);
        float estimatedPressure = Estimate(kingdomState?.ClaimantPressure ?? 35f);
        float estimatedSupport = Estimate(100f - (kingdomState == null ? 50f : kingdomState.CourtFragmentation * 0.45f));

        return new EspionageReport(
            operation.Companion,
            operation.Type,
            operation.TargetClan,
            targetKingdom,
            CampaignTime.Now,
            outcome,
            confidence,
            estimatedDissidence,
            estimatedTrust,
            estimatedFear,
            estimatedClaimant,
            estimatedBreakaway,
            estimatedLegitimacy,
            estimatedFragmentation,
            estimatedPressure,
            estimatedSupport,
            BuildRecommendedEspionageAction(estimatedDissidence, estimatedFear, estimatedTrust, estimatedClaimant));
    }

    private static string BuildRecommendedEspionageAction(float dissidence, float fear, float trust, float claimant)
    {
        if (dissidence >= 72f && trust >= 35f)
        {
            return "{=rf_si_espionage_action_pact}cultivate a secret understanding";
        }

        if (dissidence >= 58f && fear < 55f)
        {
            return "{=rf_si_espionage_action_rumor}press with whispers and rumor";
        }

        if (claimant >= 55f)
        {
            return "{=rf_si_espionage_action_claimant}probe for claimant sympathies";
        }

        if (fear >= 65f)
        {
            return "{=rf_si_espionage_action_wait}wait and let pressure on the ruler deepen";
        }

        return "{=rf_si_espionage_action_observe}keep the house under quiet observation";
    }

    private void ShowEspionageResolutionNotification(EspionageReport report)
    {
        if (report == null)
        {
            return;
        }

        TextObject title = new TextObject("{=rf_si_espionage_title}Spy Returned");
        TextObject body = report.Outcome switch
        {
            EspionageOperationStatus.Exposed => new TextObject("{=rf_si_espionage_body_exposed}{COMPANION} has returned from {TARGET}, but the operation was noticed. The target will be more watchful now."),
            EspionageOperationStatus.Failed => new TextObject("{=rf_si_espionage_body_failed}{COMPANION} returned from {TARGET} without anything solid enough to trust."),
            EspionageOperationStatus.Partial => new TextObject("{=rf_si_espionage_body_partial}{COMPANION} returned from {TARGET} with fragments of useful court intelligence."),
            _ => new TextObject("{=rf_si_espionage_body_success}{COMPANION} returned from {TARGET} with a credible reading of the political ground.")
        };
        body.SetTextVariable("COMPANION", report.Companion?.Name ?? new TextObject("{=rf_si_unknown_companion}your agent"));
        body.SetTextVariable("TARGET", report.Type == EspionageOperationType.CourtListening
            ? report.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm")
            : report.TargetClan?.Name ?? new TextObject("{=rf_si_unknown_clan}the target house"));
        ShowIntrigueInquiry(title, body);
    }

    private static TextObject GetConfidenceText(EspionageReportConfidence confidence)
    {
        return confidence switch
        {
            EspionageReportConfidence.High => new TextObject("{=rf_si_espionage_confidence_high}high"),
            EspionageReportConfidence.Medium => new TextObject("{=rf_si_espionage_confidence_medium}moderate"),
            _ => new TextObject("{=rf_si_espionage_confidence_low}low")
        };
    }

    private static TextObject DescribeEspionageBand(float value, string high, string mid, string low)
    {
        return value switch
        {
            >= 67f => new TextObject(high),
            >= 38f => new TextObject(mid),
            _ => new TextObject(low)
        };
    }
}
