using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class EspionageReport
{
    [SaveableField(1)]
    private Hero _companion;

    [SaveableField(2)]
    private EspionageOperationType _type;

    [SaveableField(3)]
    private Clan _targetClan;

    [SaveableField(4)]
    private Kingdom _targetKingdom;

    [SaveableField(5)]
    private CampaignTime _createdAt;

    [SaveableField(6)]
    private EspionageOperationStatus _outcome;

    [SaveableField(7)]
    private EspionageReportConfidence _confidence;

    [SaveableField(8)]
    private float _estimatedDissidence;

    [SaveableField(9)]
    private float _estimatedTrustToPlayer;

    [SaveableField(10)]
    private float _estimatedFearOfRuler;

    [SaveableField(11)]
    private float _estimatedClaimantAmbition;

    [SaveableField(12)]
    private float _estimatedBreakawayViability;

    [SaveableField(13)]
    private float _estimatedRoyalLegitimacy;

    [SaveableField(14)]
    private float _estimatedCourtFragmentation;

    [SaveableField(15)]
    private float _estimatedClaimantPressure;

    [SaveableField(16)]
    private float _estimatedRulerSupport;

    [SaveableField(17)]
    private string _recommendedAction;

    public Hero Companion => _companion;

    public EspionageOperationType Type => _type;

    public Clan TargetClan => _targetClan;

    public Kingdom TargetKingdom => _targetKingdom;

    public CampaignTime CreatedAt => _createdAt;

    public EspionageOperationStatus Outcome => _outcome;

    public EspionageReportConfidence Confidence => _confidence;

    public float EstimatedDissidence => _estimatedDissidence;

    public float EstimatedTrustToPlayer => _estimatedTrustToPlayer;

    public float EstimatedFearOfRuler => _estimatedFearOfRuler;

    public float EstimatedClaimantAmbition => _estimatedClaimantAmbition;

    public float EstimatedBreakawayViability => _estimatedBreakawayViability;

    public float EstimatedRoyalLegitimacy => _estimatedRoyalLegitimacy;

    public float EstimatedCourtFragmentation => _estimatedCourtFragmentation;

    public float EstimatedClaimantPressure => _estimatedClaimantPressure;

    public float EstimatedRulerSupport => _estimatedRulerSupport;

    public string RecommendedAction => _recommendedAction ?? string.Empty;

    private EspionageReport()
    {
    }

    public EspionageReport(
        Hero companion,
        EspionageOperationType type,
        Clan targetClan,
        Kingdom targetKingdom,
        CampaignTime createdAt,
        EspionageOperationStatus outcome,
        EspionageReportConfidence confidence,
        float estimatedDissidence,
        float estimatedTrustToPlayer,
        float estimatedFearOfRuler,
        float estimatedClaimantAmbition,
        float estimatedBreakawayViability,
        float estimatedRoyalLegitimacy,
        float estimatedCourtFragmentation,
        float estimatedClaimantPressure,
        float estimatedRulerSupport,
        string recommendedAction)
    {
        _companion = companion;
        _type = type;
        _targetClan = targetClan;
        _targetKingdom = targetKingdom;
        _createdAt = createdAt;
        _outcome = outcome;
        _confidence = confidence;
        _estimatedDissidence = estimatedDissidence;
        _estimatedTrustToPlayer = estimatedTrustToPlayer;
        _estimatedFearOfRuler = estimatedFearOfRuler;
        _estimatedClaimantAmbition = estimatedClaimantAmbition;
        _estimatedBreakawayViability = estimatedBreakawayViability;
        _estimatedRoyalLegitimacy = estimatedRoyalLegitimacy;
        _estimatedCourtFragmentation = estimatedCourtFragmentation;
        _estimatedClaimantPressure = estimatedClaimantPressure;
        _estimatedRulerSupport = estimatedRulerSupport;
        _recommendedAction = recommendedAction ?? string.Empty;
    }

    public void ClampValues()
    {
        _estimatedDissidence = MBMath.ClampFloat(_estimatedDissidence, 0f, 100f);
        _estimatedTrustToPlayer = MBMath.ClampFloat(_estimatedTrustToPlayer, 0f, 100f);
        _estimatedFearOfRuler = MBMath.ClampFloat(_estimatedFearOfRuler, 0f, 100f);
        _estimatedClaimantAmbition = MBMath.ClampFloat(_estimatedClaimantAmbition, 0f, 100f);
        _estimatedBreakawayViability = MBMath.ClampFloat(_estimatedBreakawayViability, 0f, 100f);
        _estimatedRoyalLegitimacy = MBMath.ClampFloat(_estimatedRoyalLegitimacy, 0f, 100f);
        _estimatedCourtFragmentation = MBMath.ClampFloat(_estimatedCourtFragmentation, 0f, 100f);
        _estimatedClaimantPressure = MBMath.ClampFloat(_estimatedClaimantPressure, 0f, 100f);
        _estimatedRulerSupport = MBMath.ClampFloat(_estimatedRulerSupport, 0f, 100f);
    }
}
