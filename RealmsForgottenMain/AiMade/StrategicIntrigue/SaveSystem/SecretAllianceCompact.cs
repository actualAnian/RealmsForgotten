using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class SecretAllianceCompact
{
    [SaveableField(1)]
    private Clan _instigatorClan;

    [SaveableField(2)]
    private Clan _allyClan;

    [SaveableField(3)]
    private Clan _supportedClan;

    [SaveableField(4)]
    private Kingdom _targetKingdom;

    [SaveableField(5)]
    private IntrigueAllianceObjective _objective;

    [SaveableField(6)]
    private IntrigueAllianceRewardType _rewardType;

    [SaveableField(7)]
    private Settlement _promisedSettlement;

    [SaveableField(8)]
    private Clan _settlementRecipientClan;

    [SaveableField(9)]
    private CampaignTime _createdAt;

    [SaveableField(10)]
    private CampaignTime _resolveBy;

    [SaveableField(11)]
    private bool _isSupportTriggered;

    [SaveableField(12)]
    private bool _isFulfilled;

    [SaveableField(13)]
    private bool _isBroken;

    [SaveableField(14)]
    private bool _isExposed;

    public Clan InstigatorClan => _instigatorClan;

    public Clan AllyClan => _allyClan;

    public Clan SupportedClan => _supportedClan;

    public Kingdom TargetKingdom => _targetKingdom;

    public IntrigueAllianceObjective Objective => _objective;

    public IntrigueAllianceRewardType RewardType => _rewardType;

    public Settlement PromisedSettlement => _promisedSettlement;

    public Clan SettlementRecipientClan => _settlementRecipientClan;

    public CampaignTime CreatedAt => _createdAt;

    public CampaignTime ResolveBy
    {
        get => _resolveBy;
        set => _resolveBy = value;
    }

    public bool IsSupportTriggered
    {
        get => _isSupportTriggered;
        set => _isSupportTriggered = value;
    }

    public bool IsFulfilled
    {
        get => _isFulfilled;
        set => _isFulfilled = value;
    }

    public bool IsBroken
    {
        get => _isBroken;
        set => _isBroken = value;
    }

    public bool IsExposed
    {
        get => _isExposed;
        set => _isExposed = value;
    }

    public bool IsActive => !_isFulfilled && !_isBroken && !_isExposed;

    private SecretAllianceCompact()
    {
    }

    public SecretAllianceCompact(
        Clan instigatorClan,
        Clan allyClan,
        Clan supportedClan,
        Kingdom targetKingdom,
        IntrigueAllianceObjective objective,
        IntrigueAllianceRewardType rewardType,
        Settlement promisedSettlement,
        Clan settlementRecipientClan,
        CampaignTime resolveBy)
    {
        _instigatorClan = instigatorClan;
        _allyClan = allyClan;
        _supportedClan = supportedClan;
        _targetKingdom = targetKingdom;
        _objective = objective;
        _rewardType = rewardType;
        _promisedSettlement = promisedSettlement;
        _settlementRecipientClan = settlementRecipientClan;
        _createdAt = CampaignTime.Now;
        _resolveBy = resolveBy;
    }

    public bool MatchesClan(Clan clan)
    {
        return _instigatorClan == clan
            || _allyClan == clan
            || _supportedClan == clan
            || _settlementRecipientClan == clan;
    }
}
