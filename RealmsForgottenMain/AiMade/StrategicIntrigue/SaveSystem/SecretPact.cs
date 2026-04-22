using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class SecretPact
{
    [SaveableField(1)]
    private Clan _sponsorClan;

    [SaveableField(2)]
    private Clan _memberClan;

    [SaveableField(3)]
    private IntriguePactGoal _goal;

    [SaveableField(4)]
    private float _commitment;

    [SaveableField(5)]
    private float _secrecy;

    [SaveableField(6)]
    private bool _isExposed;

    [SaveableField(7)]
    private CampaignTime _createdAt;

    public Clan SponsorClan => _sponsorClan;

    public Clan MemberClan => _memberClan;

    public IntriguePactGoal Goal => _goal;

    public float Commitment
    {
        get => _commitment;
        set => _commitment = value;
    }

    public float Secrecy
    {
        get => _secrecy;
        set => _secrecy = value;
    }

    public bool IsExposed
    {
        get => _isExposed;
        set => _isExposed = value;
    }

    public CampaignTime CreatedAt => _createdAt;

    private SecretPact()
    {
    }

    public SecretPact(Clan sponsorClan, Clan memberClan, IntriguePactGoal goal)
    {
        _sponsorClan = sponsorClan;
        _memberClan = memberClan;
        _goal = goal;
        _createdAt = CampaignTime.Now;
        _commitment = 35f;
        _secrecy = 75f;
    }

    public bool MatchesClan(Clan clan)
    {
        return MemberClan == clan || SponsorClan == clan;
    }

    public void ClampValues()
    {
        Commitment = MBMath.ClampFloat(Commitment, 0f, 100f);
        Secrecy = MBMath.ClampFloat(Secrecy, 0f, 100f);
    }
}
