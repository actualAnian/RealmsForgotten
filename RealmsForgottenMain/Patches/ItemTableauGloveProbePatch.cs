using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace RealmsForgotten.Patches
{
    // DIAGNOSTIC (temporary): every glove — vanilla or modded — renders with no
    // icon and orbits a strange point in the inspect view. This postfix logs the
    // live facts for EVERY inspected item (gloves AND healthy baselines like
    // helmets/swords) to RF_GloveTableauProbe.log: the resolved pose ANIMATION
    // NAME, the intended placement frame, and the actual geometry bounding box.
    // Comparing a broken glove line against a healthy helmet line shows exactly
    // which link breaks. Remove once the glove issue is solved.
    [HarmonyPatch(typeof(ItemTableau), "RefreshItemTableau")]
    public static class ItemTableauGloveProbePatch
    {
        private static int _logsLeft = 24;

        private static void Postfix(ItemTableau __instance)
        {
            if (_logsLeft <= 0)
            {
                return;
            }

            try
            {
                ItemRosterElement element = (ItemRosterElement)AccessTools.Field(typeof(ItemTableau), "_itemRosterElement").GetValue(__instance);
                ItemObject item = element.EquipmentElement.Item;
                if (item == null)
                {
                    return;
                }

                _logsLeft--;

                // Where the tableau INTENDS to place the item.
                MatrixFrame placement;
                try
                {
                    placement = element.GetItemFrameForItemTooltip();
                }
                catch
                {
                    placement = MatrixFrame.Identity;
                }

                string poseInfo = "";
                if (item.ItemType == ItemObject.ItemTypeEnum.HandArmor)
                {
                    Monster defaultMonster = Game.Current?.DefaultMonster;
                    string actionSetCode = defaultMonster?.ActionSetCode ?? "NULL";
                    string poseAnim = "?";
                    string baselineAnim = "?";
                    try
                    {
                        MBActionSet actionSet = MBActionSet.GetActionSet(actionSetCode);
                        poseAnim = MBActionSet.GetActionAnimationName(actionSet, in ActionIndexCache.act_tableau_hand_armor_pose) ?? "NULL";
                        // Known-good combat action as a resolution baseline.
                        ActionIndexCache baseline = ActionIndexCache.Create("act_greeting_front_6");
                        baselineAnim = MBActionSet.GetActionAnimationName(actionSet, in baseline) ?? "NULL";
                    }
                    catch (Exception ex)
                    {
                        poseAnim = "THREW " + ex.GetType().Name;
                    }
                    poseInfo = $" | monster='{defaultMonster?.StringId}' set='{actionSetCode}' poseAnim='{poseAnim}' baselineAnim='{baselineAnim}'";
                }

                GameEntity entity = AccessTools.Field(typeof(ItemTableau), "_itemTableauEntity").GetValue(__instance) as GameEntity;
                string entityInfo = "NULL entity";
                if (entity != null)
                {
                    Vec3 bbMin = entity.GlobalBoxMin;
                    Vec3 bbMax = entity.GlobalBoxMax;
                    Vec3 center = (bbMin + bbMax) * 0.5f;
                    Vec3 size = bbMax - bbMin;
                    entityInfo = $"skeleton={(entity.Skeleton != null)} bbCenter=({center.x:F2},{center.y:F2},{center.z:F2}) bbSize=({size.x:F2},{size.y:F2},{size.z:F2})";
                }

                Log($"{item.ItemType} '{item.StringId}' mesh='{item.MultiMeshName}' | placement=({placement.origin.x:F2},{placement.origin.y:F2},{placement.origin.z:F2}) | {entityInfo}{poseInfo}");
            }
            catch (Exception ex)
            {
                Log("probe failed (harmless): " + ex.Message);
            }
        }

        private static void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [GloveTableauProbe] {msg}\n";
            try { Debug.Print("[RF] " + line); } catch { }
            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_GloveTableauProbe.log");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                File.AppendAllText(path, line);
            }
            catch
            {
            }
        }
    }
}
