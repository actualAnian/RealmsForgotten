using System.Collections;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace RealmsForgotten.WorldState.YoungWorld
{
    /// <summary>
    /// Caps in-party troop UPGRADES by the wall stage of the party's KINGDOM
    /// (author's decision 2026-07-24: the realm's development gates training —
    /// palisades: tiers 1-2, keeps: up to tier 4, fortresses: everything).
    /// Parties without a kingdom (bandits, mercenaries, caravans, a clanless
    /// player) are never capped.
    ///
    /// Two enforcement points, because vanilla has two upgrade paths:
    ///   1. This decorator's <see cref="CanPartyUpgradeTroopToTarget"/> — gates
    ///      the PLAYER's party screen (every upgrade button checks it).
    ///   2. The Harmony postfix below on the AI upgrader — vanilla's
    ///      PartyUpgraderCampaignBehavior only consults the model for
    ///      bandit-occupation troops, so regular AI upgrades bypass path 1;
    ///      the postfix filters over-cap targets out of its candidate list
    ///      (blocking there is mandatory: with a single candidate the AI's
    ///      SelectPossibleUpgrade picks it even at upgrade chance 0).
    ///
    /// Pure decorator otherwise; with the toggle off everything delegates.
    /// </summary>
    internal class RFTierCapTroopUpgradeModel : PartyTroopUpgradeModel
    {
        private readonly PartyTroopUpgradeModel _previousModel;

        public RFTierCapTroopUpgradeModel(PartyTroopUpgradeModel previousModel)
        {
            _previousModel = previousModel;
        }

        internal static bool IsBlockedByKingdomStage(PartyBase? party, CharacterObject? target)
        {
            return RFWorldSettings.TierCap
                && target != null
                && RFWorldStageCap.IsAboveCap(RFWorldStageCap.GetKingdomCap(party?.MapFaction as Kingdom), target.Tier);
        }

        public override bool CanPartyUpgradeTroopToTarget(PartyBase party, CharacterObject character, CharacterObject target)
        {
            if (IsBlockedByKingdomStage(party, target))
            {
                return false;
            }
            return _previousModel.CanPartyUpgradeTroopToTarget(party, character, target);
        }

        // ── Delegated members (unchanged behaviour) ──────────────────────────
        public override bool IsTroopUpgradeable(PartyBase party, CharacterObject character)
            => _previousModel.IsTroopUpgradeable(party, character);

        public override bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject upgradeTarget)
            => _previousModel.DoesPartyHaveRequiredItemsForUpgrade(party, upgradeTarget);

        public override bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk)
            => _previousModel.DoesPartyHaveRequiredPerksForUpgrade(party, character, upgradeTarget, out requiredPerk);

        public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
            => _previousModel.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);

        public override int GetXpCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
            => _previousModel.GetXpCostForUpgrade(party, characterObject, upgradeTarget);

        public override int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops)
            => _previousModel.GetSkillXpFromUpgradingTroops(party, troop, numberOfTroops);

        public override float GetUpgradeChanceForTroopUpgrade(PartyBase party, CharacterObject troop, int upgradeTargetIndex)
            => _previousModel.GetUpgradeChanceForTroopUpgrade(party, troop, upgradeTargetIndex);
    }

    /// <summary>
    /// AI enforcement (see class doc above). GetPossibleUpgradeTargets returns a
    /// List of the PRIVATE nested struct TroopUpgradeArgs, so the postfix works
    /// through the non-generic IList and reads the UpgradeTarget field by
    /// reflection (resolved lazily off the first element; never throws).
    /// Applied by the SubModule's uncategorized [HarmonyPatch] attribute sweep.
    /// </summary>
    [HarmonyPatch(typeof(PartyUpgraderCampaignBehavior), "GetPossibleUpgradeTargets")]
    internal static class PartyUpgrader_GetPossibleUpgradeTargets_TierCapPatch
    {
        private static bool _resolved;
        private static FieldInfo? _upgradeTargetField;

        private static void Postfix(object __result, PartyBase party)
        {
            if (!RFWorldSettings.TierCap)
            {
                return;
            }
            try
            {
                if (__result is not IList list || list.Count == 0)
                {
                    return;
                }
                if (!_resolved)
                {
                    _resolved = true;
                    _upgradeTargetField = list[0]?.GetType().GetField("UpgradeTarget", BindingFlags.Public | BindingFlags.Instance);
                }
                if (_upgradeTargetField == null)
                {
                    return;
                }
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (_upgradeTargetField.GetValue(list[i]) is CharacterObject target
                        && RFTierCapTroopUpgradeModel.IsBlockedByKingdomStage(party, target))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
            catch
            {
                // A shape change in a future game version disables the AI gate
                // instead of crashing the daily tick; the player gate (model
                // decorator) keeps working regardless.
            }
        }
    }
}
