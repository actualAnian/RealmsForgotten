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
    // ── Escalation timing ─────────────────────────────────────────────────
    // A pact must reach this commitment before it can auto-escalate to breakaway.
    // With daily growth of +0.35/day from a starting point of ~20-35, this means
    // roughly 80-140 days of natural accumulation before a pact becomes dangerous.
    public const float AutoEscalationCommitmentThreshold = 68f;
    // Once escalation conditions are met, the breakaway takes this many days to
    // execute — giving the player and world time to react and potentially intervene.
    public const float AutoEscalationDelayDays = 20f;

    public const float RulerCountermoveCooldownDays = 14f;

    // ── Organic event frequency ────────────────────────────────────────────
    // Per-kingdom cooldowns: how often the system CAN fire an organic event.
    // With 6+ kingdoms, even conservative per-kingdom rates add up. Keep these high.
    public const float OrganicRumorCooldownDays = 10f;   // was 4
    public const float OrganicPactCooldownDays  = 20f;   // was 7

    // Crisis score thresholds: a kingdom must be in serious trouble before organic
    // events fire. Raising these means only genuinely unstable kingdoms conspire.
    public const float OrganicRumorCrisisThreshold = 50f;  // was 34
    public const float OrganicPactCrisisThreshold  = 65f;  // was 44

    public const float OrganicExternalPactSoftDefectionThreshold = 68f;

    // Per-trigger probability: even when the threshold is met and cooldown passed,
    // the event still rolls against these chances.
    public const float OrganicRumorMaxChance = 0.12f;  // was 0.30
    public const float OrganicPactMaxChance  = 0.06f;  // was 0.18
    public const float KingdomObjectiveSupportRewardCooldownDays = 12f;
    public const float KingdomObjectiveDirectiveCooldownDays = 10f;
    public const int KingdomObjectiveDirectiveBaseInfluenceCost = 20;
    public const int KingdomObjectiveDirectiveBaseGoldCost = 2000;
    public const float SeverePunishmentSuspicionThreshold = 92f;
    public const float SeverePunishmentThreatThreshold = 135f;
    public const int ExecutionRelationThreshold = -70;
    public const int ImprisonmentRelationThreshold = 0;
    public const float ExecutionGlobalCooldownDays = 90f;
    public const int ExecutionMinimumWorldAliveLordCount = 650;
    public const int ExecutionMinimumKingdomAliveLordCount = 12;
    public const int ExecutionMinimumClanAliveLordCount = 2;

    public const float DailySuspicionDecay = 1.25f;
    public const float DailyRoyalFavorDecay = 0.5f;
    public const float DailyInfiltrationDecay = 0.35f;

    public static CampaignTime DefaultRumorDuration => CampaignTime.Days(3f);
    public static CampaignTime DefaultBreakawayPreparation => CampaignTime.Days(4f);
}
