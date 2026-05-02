using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;

public sealed class ClanIntrigueState
{
    [SaveableField(1)]
    private Clan _clan;

    [SaveableField(2)]
    private float _dissidence;

    [SaveableField(3)]
    private float _trustToPlayer;

    [SaveableField(4)]
    private float _fearOfRuler;

    [SaveableField(5)]
    private float _suspicion;

    [SaveableField(6)]
    private float _infiltration;

    [SaveableField(7)]
    private float _royalFavor;

    [SaveableField(8)]
    private CampaignTime _lastUpdated;

    [SaveableField(9)]
    private float _fiefGrievance;

    [SaveableField(10)]
    private float _voteResentment;

    [SaveableField(11)]
    private float _militaryFrustration;

    [SaveableField(12)]
    private float _claimantAmbition;

    [SaveableField(13)]
    private float _softDefectionPressure;

    public Clan Clan => _clan;

    public float Dissidence
    {
        get => _dissidence;
        set => _dissidence = value;
    }

    public float TrustToPlayer
    {
        get => _trustToPlayer;
        set => _trustToPlayer = value;
    }

    public float FearOfRuler
    {
        get => _fearOfRuler;
        set => _fearOfRuler = value;
    }

    public float Suspicion
    {
        get => _suspicion;
        set => _suspicion = value;
    }

    public float Infiltration
    {
        get => _infiltration;
        set => _infiltration = value;
    }

    public float RoyalFavor
    {
        get => _royalFavor;
        set => _royalFavor = value;
    }

    public CampaignTime LastUpdated
    {
        get => _lastUpdated;
        set => _lastUpdated = value;
    }

    public float FiefGrievance
    {
        get => _fiefGrievance;
        set => _fiefGrievance = value;
    }

    public float VoteResentment
    {
        get => _voteResentment;
        set => _voteResentment = value;
    }

    public float MilitaryFrustration
    {
        get => _militaryFrustration;
        set => _militaryFrustration = value;
    }

    public float ClaimantAmbition
    {
        get => _claimantAmbition;
        set => _claimantAmbition = value;
    }

    public float SoftDefectionPressure
    {
        get => _softDefectionPressure;
        set => _softDefectionPressure = value;
    }

    public bool IsConspirable => Dissidence >= StrategicIntrigueConstants.ConspirableDissidenceThreshold;

    public bool IsBreakawayReady => Dissidence >= StrategicIntrigueConstants.BreakawayDissidenceThreshold;

    private ClanIntrigueState()
    {
    }

    public ClanIntrigueState(Clan clan)
    {
        _clan = clan;
        _lastUpdated = CampaignTime.Now;
    }

    public void ClampValues()
    {
        Dissidence = MBMath.ClampFloat(Dissidence, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        TrustToPlayer = MBMath.ClampFloat(TrustToPlayer, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        FearOfRuler = MBMath.ClampFloat(FearOfRuler, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        Suspicion = MBMath.ClampFloat(Suspicion, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        Infiltration = MBMath.ClampFloat(Infiltration, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        RoyalFavor = MBMath.ClampFloat(RoyalFavor, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        FiefGrievance = MBMath.ClampFloat(FiefGrievance, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        VoteResentment = MBMath.ClampFloat(VoteResentment, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        MilitaryFrustration = MBMath.ClampFloat(MilitaryFrustration, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        ClaimantAmbition = MBMath.ClampFloat(ClaimantAmbition, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
        SoftDefectionPressure = MBMath.ClampFloat(SoftDefectionPressure, StrategicIntrigueConstants.MinIntrigueValue, StrategicIntrigueConstants.MaxIntrigueValue);
    }
}
