using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class IntrigueOperation
{
    [SaveableField(1)]
    private IntrigueOperationType _type;

    [SaveableField(2)]
    private Clan _instigatorClan;

    [SaveableField(3)]
    private Clan _targetClan;

    [SaveableField(4)]
    private Clan _targetRulerClan;

    [SaveableField(5)]
    private float _power;

    [SaveableField(6)]
    private float _risk;

    [SaveableField(7)]
    private CampaignTime _resolveAt;

    [SaveableField(8)]
    private IntrigueOperationStatus _status;

    public IntrigueOperationType Type => _type;

    public Clan InstigatorClan => _instigatorClan;

    public Clan TargetClan => _targetClan;

    public Clan TargetRulerClan => _targetRulerClan;

    public float Power
    {
        get => _power;
        set => _power = value;
    }

    public float Risk
    {
        get => _risk;
        set => _risk = value;
    }

    public CampaignTime ResolveAt
    {
        get => _resolveAt;
        set => _resolveAt = value;
    }

    public IntrigueOperationStatus Status
    {
        get => _status;
        set => _status = value;
    }

    private IntrigueOperation()
    {
    }

    public IntrigueOperation(
        IntrigueOperationType type,
        Clan instigatorClan,
        Clan targetClan,
        Clan targetRulerClan,
        float power,
        float risk,
        CampaignTime resolveAt)
    {
        _type = type;
        _instigatorClan = instigatorClan;
        _targetClan = targetClan;
        _targetRulerClan = targetRulerClan;
        _power = power;
        _risk = risk;
        _resolveAt = resolveAt;
        _status = IntrigueOperationStatus.Pending;
    }

    public bool IsDue => Status == IntrigueOperationStatus.Pending && CampaignTime.Now >= ResolveAt;

    public void ClampValues()
    {
        Power = MBMath.ClampFloat(Power, 0f, 100f);
        Risk = MBMath.ClampFloat(Risk, 0f, 100f);
    }
}
