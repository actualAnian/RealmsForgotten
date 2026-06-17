using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class KingdomIntrigueState
{
    [SaveableField(1)]
    private Kingdom _kingdom;

    [SaveableField(2)]
    private float _rulerLegitimacy;

    [SaveableField(3)]
    private float _warExhaustion;

    [SaveableField(4)]
    private float _recentLosses;

    [SaveableField(5)]
    private float _rebellionPressure;

    [SaveableField(6)]
    private float _courtFragmentation;

    [SaveableField(7)]
    private CampaignTime _lastUpdated;

    [SaveableField(8)]
    private float _claimantPressure;

    [SaveableField(9)]
    private CampaignTime _lastCountermoveAt;

    [SaveableField(10)]
    private CampaignTime _lastOrganicRumorAt;

    [SaveableField(11)]
    private CampaignTime _lastOrganicPactAt;

    [SaveableField(12)]
    private KingdomObjectiveType _objectiveType;

    [SaveableField(13)]
    private float _objectiveProgress;

    [SaveableField(14)]
    private float _objectiveMomentum;

    [SaveableField(15)]
    private float _objectivePressure;

    [SaveableField(16)]
    private int _objectiveMilestone;

    [SaveableField(17)]
    private bool _playerSupportsObjective;

    [SaveableField(18)]
    private float _objectiveWarScore;

    [SaveableField(19)]
    private CampaignTime _lastObjectiveRewardAt;

    [SaveableField(20)]
    private CampaignTime _lastObjectiveDirectiveAt;

    [SaveableField(21)]
    private bool _playerSupportsRivalAgenda;

    [SaveableField(22)]
    private float _rivalAgendaStrength;

    public Kingdom Kingdom => _kingdom;

    public float RulerLegitimacy
    {
        get => _rulerLegitimacy;
        set => _rulerLegitimacy = value;
    }

    public float WarExhaustion
    {
        get => _warExhaustion;
        set => _warExhaustion = value;
    }

    public float RecentLosses
    {
        get => _recentLosses;
        set => _recentLosses = value;
    }

    public float RebellionPressure
    {
        get => _rebellionPressure;
        set => _rebellionPressure = value;
    }

    public float CourtFragmentation
    {
        get => _courtFragmentation;
        set => _courtFragmentation = value;
    }

    public CampaignTime LastUpdated
    {
        get => _lastUpdated;
        set => _lastUpdated = value;
    }

    public float ClaimantPressure
    {
        get => _claimantPressure;
        set => _claimantPressure = value;
    }

    public CampaignTime LastCountermoveAt
    {
        get => _lastCountermoveAt;
        set => _lastCountermoveAt = value;
    }

    public CampaignTime LastOrganicRumorAt
    {
        get => _lastOrganicRumorAt;
        set => _lastOrganicRumorAt = value;
    }

    public CampaignTime LastOrganicPactAt
    {
        get => _lastOrganicPactAt;
        set => _lastOrganicPactAt = value;
    }

    public KingdomObjectiveType ObjectiveType
    {
        get => _objectiveType;
        set => _objectiveType = value;
    }

    public float ObjectiveProgress
    {
        get => _objectiveProgress;
        set => _objectiveProgress = value;
    }

    public float ObjectiveMomentum
    {
        get => _objectiveMomentum;
        set => _objectiveMomentum = value;
    }

    public float ObjectivePressure
    {
        get => _objectivePressure;
        set => _objectivePressure = value;
    }

    public int ObjectiveMilestone
    {
        get => _objectiveMilestone;
        set => _objectiveMilestone = value;
    }

    public bool PlayerSupportsObjective
    {
        get => _playerSupportsObjective;
        set => _playerSupportsObjective = value;
    }

    public float ObjectiveWarScore
    {
        get => _objectiveWarScore;
        set => _objectiveWarScore = value;
    }

    public CampaignTime LastObjectiveRewardAt
    {
        get => _lastObjectiveRewardAt;
        set => _lastObjectiveRewardAt = value;
    }

    public CampaignTime LastObjectiveDirectiveAt
    {
        get => _lastObjectiveDirectiveAt;
        set => _lastObjectiveDirectiveAt = value;
    }

    public bool PlayerSupportsRivalAgenda
    {
        get => _playerSupportsRivalAgenda;
        set => _playerSupportsRivalAgenda = value;
    }

    public float RivalAgendaStrength
    {
        get => _rivalAgendaStrength;
        set => _rivalAgendaStrength = value;
    }

    private KingdomIntrigueState()
    {
    }

    public KingdomIntrigueState(Kingdom kingdom)
    {
        _kingdom = kingdom;
        _rulerLegitimacy = 55f;
        _lastUpdated = CampaignTime.Now;
        _lastCountermoveAt = CampaignTime.Now;
        _lastOrganicRumorAt = CampaignTime.Now;
        _lastOrganicPactAt = CampaignTime.Now;
        _lastObjectiveRewardAt = CampaignTime.Now;
        _lastObjectiveDirectiveAt = CampaignTime.Zero;
    }

    public void ClampValues()
    {
        RulerLegitimacy = MBMath.ClampFloat(RulerLegitimacy, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        WarExhaustion = MBMath.ClampFloat(WarExhaustion, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RecentLosses = MBMath.ClampFloat(RecentLosses, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RebellionPressure = MBMath.ClampFloat(RebellionPressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        CourtFragmentation = MBMath.ClampFloat(CourtFragmentation, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ClaimantPressure = MBMath.ClampFloat(ClaimantPressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ObjectiveProgress = MBMath.ClampFloat(ObjectiveProgress, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ObjectivePressure = MBMath.ClampFloat(ObjectivePressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ObjectiveWarScore = MBMath.ClampFloat(ObjectiveWarScore, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RivalAgendaStrength = MBMath.ClampFloat(RivalAgendaStrength, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ObjectiveMomentum = MBMath.ClampFloat(ObjectiveMomentum, -25f, 25f);
        ObjectiveMilestone = MathF.Min(4, MathF.Max(0, ObjectiveMilestone));
    }
}
