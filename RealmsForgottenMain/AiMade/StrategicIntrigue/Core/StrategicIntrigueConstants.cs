using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Core;

public static class StrategicIntrigueConstants
{
    public const int SaveBaseId = 908500;

    public const float MaxIntrigueValue = 100f;
    public const float MinIntrigueValue = 0f;

    public const float ConspirableDissidenceThreshold = 70f;
    public const float BreakawayDissidenceThreshold = 85f;
    public const float SecretPactTrustThreshold = 25f;
    public const float RumorCampaignTrustThreshold = 15f;

    public const int StabilityDiscussionRelationThreshold = 5;
    public const int RumorCampaignRelationThreshold = 15;
    public const int SecretPactRelationThreshold = 25;
    public const int BreakawayRelationThreshold = 35;
    public const int AllianceRelationThreshold = 30;
    public const int ForeignAllianceRelationThreshold = 10;
    public const float AllianceTrustThreshold = 30f;
    public const float AllianceDurationDays = 70f;
    public const float AllianceSettlementGraceDays = 35f;
    public const float ClaimantCoupLegitimacyThreshold = 38f;
    public const float ClaimantCoupFragmentationThreshold = 42f;
    public const int DeposedRulerIndependentWarFiefThreshold = 2;
    public const float DeposedRulerIndependentWarFiefShareThreshold = 0.35f;
    public const float DeposedRulerAsylumWarDurationDays = 60f;
    public const float AutoEscalationCommitmentThreshold = 48f;
    public const float AutoEscalationDelayDays = 2f;
    public const float RulerCountermoveCooldownDays = 7f;
    public const float OrganicRumorCooldownDays = 4f;
    public const float OrganicPactCooldownDays = 7f;
    public const float OrganicRumorCrisisThreshold = 34f;
    public const float OrganicPactCrisisThreshold = 44f;
    public const float OrganicExternalPactSoftDefectionThreshold = 68f;
    public const float OrganicRumorMaxChance = 0.3f;
    public const float OrganicPactMaxChance = 0.18f;
    public const float KingdomObjectiveSupportRewardCooldownDays = 12f;
    public const float KingdomObjectiveDirectiveCooldownDays = 10f;
    public const int KingdomObjectiveDirectiveBaseInfluenceCost = 20;
    public const int KingdomObjectiveDirectiveBaseGoldCost = 2000;
    public const float SeverePunishmentSuspicionThreshold = 82f;
    public const float SeverePunishmentThreatThreshold = 108f;
    public const int ExecutionRelationThreshold = -25;
    public const int ImprisonmentRelationThreshold = 20;

    public const float DailySuspicionDecay = 1.25f;
    public const float DailyRoyalFavorDecay = 0.5f;
    public const float DailyInfiltrationDecay = 0.35f;

    public static CampaignTime DefaultRumorDuration => CampaignTime.Days(3f);
    public static CampaignTime DefaultBreakawayPreparation => CampaignTime.Days(4f);
}
