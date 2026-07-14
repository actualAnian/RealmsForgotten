using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    internal class RFRaidModel : DefaultRaidModel
    {
        private static PartyBase currentRaidParty;
        
        private RaidModel _previousModel;
        
        public RFRaidModel(RaidModel previousModel)
        {
            _previousModel = previousModel;
        }
        
        [HarmonyPatch(typeof(RaidEventComponent), "Update")]
        public static class RaidUpdatePatch
        {
            public static void Prefix(ref bool finish, RaidEventComponent __instance)
            {
                currentRaidParty = __instance.AttackerSide.LeaderParty;
            }
        }
        public override MBReadOnlyList<(ItemObject, float)> GetCommonLootItemScores()
        {
            MBReadOnlyList<(ItemObject, float)> baseValue = _previousModel.GetCommonLootItemScores();
            if (baseValue == null || baseValue.Count < 1 || currentRaidParty?.Owner?.Culture?.StringId != "giant")
                return baseValue;
            // Return a NEW list — baseValue is the vanilla model's cached
            // _commonLootItems (built once). Mutating it in place inflated the
            // scores +25% COMPOUND and permanently for every faction's raids.
            var scaled = new System.Collections.Generic.List<(ItemObject, float)>(baseValue.Count);
            for (int i = 0; i < baseValue.Count; i++)
            {
                scaled.Add((baseValue[i].Item1, baseValue[i].Item2 * 1.25f));
            }

            return new MBReadOnlyList<(ItemObject, float)>(scaled);
        }

        public override ExplainedNumber CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
        {
            ExplainedNumber value = _previousModel.CalculateHitDamage(attackerSide, settlementHitPoints);
            if (attackerSide.LeaderParty.Owner?.Culture.StringId == "giant")
                value.AddFactor(1.25f);
            return value;
        }
    }
}
