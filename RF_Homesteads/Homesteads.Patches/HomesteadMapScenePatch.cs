using System;
using HarmonyLib;
using Homesteads.Models;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(MapScene), "AddNewEntityToMapScene")]
internal static class HomesteadMapScenePatch
{
	[HarmonyPrefix]
	private static bool Prefix(Scene ____scene, string entityId, CampaignVec2 position)
	{
		try
		{
			if (string.IsNullOrEmpty(entityId) || !entityId.StartsWith("hsr_settlement_"))
			{
				return true;
			}
			string text = (entityId.StartsWith("hsr_settlement_castle_") ? "hsr_map_castle_empire" : ((!entityId.StartsWith("hsr_settlement_village_")) ? "hsr_map_town_empire" : "hsr_map_village_empire"));
			GameEntity gameEntity = GameEntity.Instantiate(____scene, text, callScriptCallbacks: true);
			if (gameEntity == null)
			{
				TraceLogger.Write("HomesteadMapScenePatch", "prefab '" + text + "' not found for '" + entityId + "' — falling back.");
				return true;
			}
			gameEntity.Name = entityId;
			gameEntity.SetLocalPosition(position.AsVec3());
			if (HomesteadSettlementBuilder.SettlementRotations.TryGetValue(entityId, out var value) && Math.Abs(value) > 0.0001f)
			{
				try
				{
					MatrixFrame frame = gameEntity.GetFrame();
					frame.rotation.RotateAboutUp(value);
					gameEntity.SetFrame(ref frame);
				}
				catch
				{
				}
			}
			TraceLogger.Write("HomesteadMapScenePatch", $"placed map entity for '{entityId}' yaw={value:0.###}.");
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadMapScenePatch", "AddNewEntityToMapScene failed: " + ex.Message);
			return true;
		}
	}
}
