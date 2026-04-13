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
    }

    public void ClampValues()
    {
        RulerLegitimacy = MBMath.ClampFloat(RulerLegitimacy, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        WarExhaustion = MBMath.ClampFloat(WarExhaustion, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RecentLosses = MBMath.ClampFloat(RecentLosses, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RebellionPressure = MBMath.ClampFloat(RebellionPressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        CourtFragmentation = MBMath.ClampFloat(CourtFragmentation, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ClaimantPressure = MBMath.ClampFloat(ClaimantPressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
    }
}
