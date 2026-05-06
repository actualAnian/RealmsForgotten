using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class EspionageOperation
{
    [SaveableField(1)]
    private EspionageOperationType _type;

    [SaveableField(2)]
    private Hero _companion;

    [SaveableField(3)]
    private Clan _targetClan;

    [SaveableField(4)]
    private Kingdom _targetKingdom;

    [SaveableField(5)]
    private CampaignTime _startedAt;

    [SaveableField(6)]
    private CampaignTime _resolveAt;

    [SaveableField(7)]
    private EspionageOperationStatus _status;

    [SaveableField(8)]
    private float _power;

    [SaveableField(9)]
    private float _risk;

    public EspionageOperationType Type => _type;

    public Hero Companion => _companion;

    public Clan TargetClan => _targetClan;

    public Kingdom TargetKingdom => _targetKingdom;

    public CampaignTime StartedAt => _startedAt;

    public CampaignTime ResolveAt => _resolveAt;

    public EspionageOperationStatus Status
    {
        get => _status;
        set => _status = value;
    }

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

    public bool IsDue => Status == EspionageOperationStatus.Pending && CampaignTime.Now >= ResolveAt;

    private EspionageOperation()
    {
    }

    public EspionageOperation(
        EspionageOperationType type,
        Hero companion,
        Clan targetClan,
        Kingdom targetKingdom,
        CampaignTime startedAt,
        CampaignTime resolveAt,
        float power,
        float risk)
    {
        _type = type;
        _companion = companion;
        _targetClan = targetClan;
        _targetKingdom = targetKingdom;
        _startedAt = startedAt;
        _resolveAt = resolveAt;
        _power = power;
        _risk = risk;
        _status = EspionageOperationStatus.Pending;
    }

    public void ClampValues()
    {
        Power = MBMath.ClampFloat(Power, 0f, 100f);
        Risk = MBMath.ClampFloat(Risk, 0f, 100f);
    }
}
