using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (baseValue == null || baseValue.Count < 1 || currentRaidParty.Owner?.Culture.StringId != "giant")
                return baseValue;
            for (int i = 0; i < baseValue.Count; i++)
            {
                (ItemObject, float) tuple = (baseValue[i].Item1, ((25f / 100f) * baseValue[i].Item2) + baseValue[i].Item2);
                baseValue[i] = tuple;
            }

            return baseValue;
        }

        public override float CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
        {
            float baseValue = _previousModel.CalculateHitDamage(attackerSide, settlementHitPoints);
            if (attackerSide.LeaderParty.Owner?.Culture.StringId == "giant")
                return ((25f / 100f) * baseValue) + baseValue;
            return baseValue;

        }
    }
}
