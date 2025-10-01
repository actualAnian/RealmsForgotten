using SandBox.GameComponents;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;

namespace RealmsForgotten.AiMade.CustomOrderofBattle
{
    public class SpearAwareBattleSpawnModel : SandboxBattleSpawnModel
    {
        public override List<(IAgentOriginBase origin, int formationIndex)>
            GetInitialSpawnAssignments(BattleSideEnum side, List<IAgentOriginBase> troopOrigins)
        {
            // Let vanilla/OOB do full pass first.
            var list = base.GetInitialSpawnAssignments(side, troopOrigins);

            var oob = Campaign.Current?.GetCampaignBehavior<OrderOfBattleCampaignBehavior>();
            if (oob == null || Mission.Current == null) return list;

            // Find two Infantry cards in OOB.
            var infantrySlots = new List<int>(2);
            for (int i = 0; i < 8; i++)
            {
                var data = oob.GetFormationDataAtIndex(i, Mission.Current.IsSiegeBattle);
                if (data == null) return list; // OOB not ready – abort
                if (data.FormationClass == DeploymentFormationClass.Infantry) infantrySlots.Add(i);
                if (infantrySlots.Count == 2) break;
            }
            if (infantrySlots.Count < 2) return list; // Need two infantry cards

            int nonSpearIdx = infantrySlots[0];
            int spearIdx = infantrySlots[1];

            int changed = 0, s = 0, ns = 0;

            for (int k = 0; k < list.Count; k++)
            {
                var origin = list[k].origin;
                var troop = origin?.Troop as CharacterObject;
                if (troop == null) continue;

                // Only touch troops that vanilla put into Infantry.
                var defClass = Mission.Current.GetAgentTroopClass(side, origin.Troop).DefaultClass();
                if (defClass != FormationClass.Infantry) continue;

                // Approx guess from troop template (pre-spawn; may be wrong – fixed later)
                bool looksSpear = SpearTemplateGuess(troop);
                int target = looksSpear ? spearIdx : nonSpearIdx;

                if (list[k].formationIndex != target)
                {
                    list[k] = (origin, target);
                    changed++;
                }
                if (looksSpear) s++; else ns++;
            }

            MBDebug.Print($"[RF Spear/SpawnModel] seeded split changed:{changed} spearGuess:{s} nonSpearGuess:{ns} -> [{nonSpearIdx},{spearIdx}]");
            return list;
        }

        // Very coarse: scan a few main slots on the template; exclude javelins.
        private static bool SpearTemplateGuess(CharacterObject co)
        {
            for (int i = 0; i <= 4; i++)
            {
                var el = co.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
                var item = el.Item;
                var w = item?.PrimaryWeapon;
                if (w == null) continue;

                var id = (item.StringId ?? "").ToLowerInvariant();
                if (id.Contains("javelin") || id.Contains("throw")) continue;
                if (id.Contains("spear") || id.Contains("pike") || id.Contains("yari") || id.Contains("hasta"))
                    return true;
            }
            return false;
        }
    }
}