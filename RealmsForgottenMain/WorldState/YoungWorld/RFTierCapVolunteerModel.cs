using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// Caps the tier of volunteers offered by notables to the military stage of
    /// their settlement's KINGDOM, read off its wall development:
    ///   • mostly wall level 1 (palisades)  → tiers 1-2 only
    ///   • mostly wall level 2 (keeps)       → up to tier 4
    ///   • two or more level-3 towns         → no cap (tier 5+ unlocked)
    ///
    /// The cap binds PLAYER and AI alike by construction: it shapes the
    /// generation of the notables' volunteer pool, which is the single
    /// recruitment source both the player and AI lords draw from.
    ///
    /// Pure decorator: it extends the abstract <see cref="VolunteerModel"/> and
    /// delegates EVERY member to the previous model, so the existing
    /// <c>RFVolunteerModel</c> stays live in the chain. The only behavioural
    /// change is in <see cref="GetDailyVolunteerProductionProbability"/>, where a
    /// slot already at the cap is frozen (probability 0) so it never upgrades past
    /// it — slots below the cap keep their normal probability and climb up to it.
    /// This gates growth without ever touching <c>VolunteerTypes</c> directly, so
    /// it is fully save-safe. With the toggle off, every call delegates unchanged.
    ///
    /// The IEF ±1 adjustment (strong economy raises the cap, weak lowers it) is
    /// layered on top via <see cref="RFFactionEconomyIndex.GetIef"/>.
    /// </summary>
    internal class RFTierCapVolunteerModel : VolunteerModel
    {
        private readonly VolunteerModel _previousModel;

        public RFTierCapVolunteerModel(VolunteerModel previousModel)
        {
            _previousModel = previousModel;
        }

        // ── Delegated members (unchanged behaviour) ──────────────────────────
        public override int MaxVolunteerTier => _previousModel.MaxVolunteerTier;

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
            => _previousModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

        public override int MaximumIndexGarrisonCanRecruitFromHero(Settlement settlement, Hero sellerHero)
            => _previousModel.MaximumIndexGarrisonCanRecruitFromHero(settlement, sellerHero);

        public override CharacterObject GetBasicVolunteer(Hero hero)
            => _previousModel.GetBasicVolunteer(hero);

        public override bool CanHaveRecruits(Hero hero)
            => _previousModel.CanHaveRecruits(hero);

        // ── The cap ──────────────────────────────────────────────────────────
        public override float GetDailyVolunteerProductionProbability(Hero hero, int index, Settlement settlement)
        {
            float baseProbability = _previousModel.GetDailyVolunteerProductionProbability(hero, index, settlement);
            if (!RFWorldSettings.TierCap)
            {
                return baseProbability;
            }

            CharacterObject[]? slots = hero?.VolunteerTypes;
            if (slots == null || index < 0 || index >= slots.Length)
            {
                return baseProbability;
            }

            CharacterObject current = slots[index];
            // The settlement's KINGDOM stage decides what its notables can arm
            // (author's decision 2026-07-24: kingdom level, not troop culture).
            int cap = RFWorldStageCap.GetKingdomCap(settlement?.MapFaction as Kingdom);
            if (cap == RFWorldStageCap.NoCap)
            {
                return baseProbability;
            }
            // Freeze a slot once it reaches the cap: vanilla would otherwise roll
            // to upgrade it a tier further. Null or below-cap slots are left with
            // their normal probability, so they still fill and grow up to the cap.
            if (current != null && current.Tier >= cap)
            {
                return 0f;
            }

            return baseProbability;
        }

    }
}
