using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using HarmonyLib;
using Helpers;
using Homesteads.Views;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Models;

public static class HomesteadSettlementBuilder
{
	public const float MaxGateDetourRatio = 2.5f;

	public static List<string> SessionSettlementXmls = new List<string>();

	public static Dictionary<string, float> SettlementRotations = new Dictionary<string, float>();

	private const string TemplateFolder = "HomesteadSettlementTemplates";

	private static bool? _femaleVillageNotableModActive;

	private static readonly FieldInfo? WallSectionListField = AccessTools.Field(typeof(Settlement), "_settlementWallSectionHitPointsRatioList");

	private static readonly MethodInfo? SettlementHitPointsSetter = AccessTools.PropertySetter(typeof(Settlement), "SettlementHitPoints");

	private static readonly FieldInfo? WorkshopSettlementField = AccessTools.Field(typeof(Workshop), "_settlement");

	public const float VillageStartingHearths = 200f;

	private static readonly string[] VillageNamePatterns = new string[5] { "{=hsr_village_villa}{NAME}'s Villa", "{=hsr_village_grange}{NAME}'s Grange", "{=hsr_village_holding}{NAME} Holding", "{=hsr_village_steading}{NAME} Steading", "{=hsr_village_croft}{NAME}'s Croft" };

	public static Settlement? FindUnrelatedVanillaVillage(CultureObject? preferredCulture = null)
	{
		object obj = Campaign.Current?.Settlements.FirstOrDefault(delegate(Settlement s)
		{
			if (s.IsVillage)
			{
				string stringId = s.StringId;
				if (stringId == null || !stringId.StartsWith("hsr_settlement_"))
				{
					if (preferredCulture != null)
					{
						return s.Culture == preferredCulture;
					}
					return true;
				}
			}
			return false;
		});
		if (obj == null)
		{
			Campaign current = Campaign.Current;
			if (current == null)
			{
				return null;
			}
			obj = current.Settlements.FirstOrDefault(delegate(Settlement s)
			{
				if (s.IsVillage)
				{
					string stringId = s.StringId;
					if (stringId == null)
					{
						return true;
					}
					return !stringId.StartsWith("hsr_settlement_");
				}
				return false;
			});
		}
		return (Settlement?)obj;
	}

	private static int[] LandExcludedFaceIds()
	{
		return Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Default);
	}

	private static Settlement? NearestNativeFortification(Vec2 pos)
	{
		return (from s in Campaign.Current?.Settlements.Where(delegate(Settlement s)
			{
				if (s != null && (s.IsTown || s.IsCastle))
				{
					string stringId = s.StringId;
					if (stringId == null)
					{
						return true;
					}
					return !stringId.StartsWith("hsr_settlement_");
				}
				return false;
			})
			orderby s.GatePosition.ToVec2().DistanceSquared(pos)
			select s).FirstOrDefault();
	}

	public static bool TryEvaluateConnectivity(Vec2 pos, out float ratio)
	{
		ratio = 0f;
		try
		{
			IMapScene mapScene = Campaign.Current?.MapSceneWrapper;
			if (mapScene == null)
			{
				return true;
			}
			CampaignVec2 campaignVec = new CampaignVec2(pos, isOnLand: true);
			if (!campaignVec.Face.IsValid())
			{
				return false;
			}
			Settlement settlement = NearestNativeFortification(pos);
			if (settlement == null)
			{
				return true;
			}
			CampaignVec2 gatePosition = settlement.GatePosition;
			MapDistanceModel mapDistanceModel = Campaign.Current.Models.MapDistanceModel;
			if (!mapScene.GetPathDistanceBetweenAIFaces(campaignVec.Face, gatePosition.Face, campaignVec.ToVec2(), gatePosition.ToVec2(), 0.3f, Campaign.MapDiagonalSquared, out var distance, LandExcludedFaceIds(), mapDistanceModel.RegionSwitchCostFromLandToSea, mapDistanceModel.RegionSwitchCostFromSeaToLand))
			{
				return false;
			}
			float num = pos.Distance(gatePosition.ToVec2());
			ratio = ((num < 1f) ? 1f : (distance / num));
			return true;
		}
		catch
		{
			ratio = 0f;
			return true;
		}
	}

	public static bool IsGateWellConnected(Vec2 pos)
	{
		if (TryEvaluateConnectivity(pos, out var ratio))
		{
			return ratio <= 2.5f;
		}
		return false;
	}

	public static bool TryFindBetterGate(Settlement s, out Vec2 bestGate, out float bestRatio)
	{
		bestGate = default(Vec2);
		bestRatio = float.MaxValue;
		if (s == null)
		{
			return false;
		}
		Vec2 getPosition2D = s.GetPosition2D;
		TryEvaluateConnectivity(s.GatePosition.ToVec2(), out var ratio);
		float num = float.MaxValue;
		float[] array = new float[9] { 3f, 6f, 10f, 14f, 18f, 24f, 30f, 36f, 42f };
		foreach (float num2 in array)
		{
			for (int j = 0; j < 16; j++)
			{
				float x = (float)j * (TaleWorlds.Library.MathF.PI / 8f);
				Vec2 vec = getPosition2D + new Vec2(TaleWorlds.Library.MathF.Cos(x) * num2, TaleWorlds.Library.MathF.Sin(x) * num2);
				if (TryEvaluateConnectivity(vec, out var ratio2) && !(ratio2 <= 0f))
				{
					float num3 = ratio2 + num2 / 200f;
					if (num3 < num)
					{
						num = num3;
						bestRatio = ratio2;
						bestGate = vec;
					}
				}
			}
		}
		if (num == float.MaxValue)
		{
			return false;
		}
		if (!(bestRatio + 0.15f < ratio))
		{
			return ratio > 2.5f;
		}
		return true;
	}

	public static bool ApplyGate(Settlement s, Vec2 gate)
	{
		if (s == null)
		{
			return false;
		}
		try
		{
			CampaignVec2 campaignVec = new CampaignVec2(gate, isOnLand: true);
			int num = (campaignVec.Face.IsValid() ? campaignVec.Face.FaceIndex : (-1));
			AccessTools.PropertySetter(typeof(Settlement), "GatePosition")?.Invoke(s, new object[1] { campaignVec });
			if (num > 0)
			{
				AccessTools.PropertySetter(typeof(Settlement), "NavMeshFaceIndex")?.Invoke(s, new object[1] { num });
			}
			PersistGateToXml(s.StringId, gate, num);
			TraceLogger.Write("HomesteadSettlementBuilder", $"ApplyGate: '{s.StringId}' gate → ({gate.x:0.#},{gate.y:0.#}) face={num}.");
			return true;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ApplyGate failed for '" + s?.StringId + "': " + ex.Message);
			return false;
		}
	}

	private static void PersistGateToXml(string id, Vec2 gate, int faceIdx)
	{
		HomesteadSettlementBehavior instance = HomesteadSettlementBehavior.Instance;
		if (instance == null || string.IsNullOrEmpty(id))
		{
			return;
		}
		string storedXml = instance.GetStoredXml(id);
		if (!string.IsNullOrEmpty(storedXml))
		{
			string text = gate.x.ToString("0.######", CultureInfo.InvariantCulture);
			string text2 = gate.y.ToString("0.######", CultureInfo.InvariantCulture);
			if (storedXml.Contains("gate_posX=\""))
			{
				storedXml = Regex.Replace(storedXml, "gate_posX=\"[^\"]*\"", "gate_posX=\"" + text + "\"");
				storedXml = Regex.Replace(storedXml, "gate_posY=\"[^\"]*\"", "gate_posY=\"" + text2 + "\"");
			}
			else
			{
				storedXml = storedXml.Replace("<Settlement ", "<Settlement gate_posX=\"" + text + "\" gate_posY=\"" + text2 + "\" ");
			}
			if (faceIdx > 0)
			{
				storedXml = ((!storedXml.Contains("hsr_navmesh=\"")) ? storedXml.Replace("<Settlement ", $"<Settlement hsr_navmesh=\"{faceIdx}\" ") : Regex.Replace(storedXml, "hsr_navmesh=\"[^\"]*\"", $"hsr_navmesh=\"{faceIdx}\""));
			}
			instance.UpdateStoredXml(id, storedXml);
		}
	}

	public static Settlement? CreateTown(HomesteadSettlementPlacementMapView.Result result, string name, CultureObject culture, Clan owner)
	{
		try
		{
			string text = ResolveTemplateFile("empire_town.xml");
			if (text == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateTown: no town template file found.");
				return null;
			}
			string text2 = "hsr_settlement_town_" + MBRandom.RandomInt(100000, 999999);
			int faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(result.Gate.Position, isOnLand: true)).FaceIndex;
			string text3 = File.ReadAllText(text).Replace("{{ID}}", text2).Replace("{{NAME}}", SecurityElement.Escape(name) ?? name)
				.Replace("{{PLAYER_CLAN}}", owner.StringId)
				.Replace("{{PLAYER_CULTURE}}", culture.StringId)
				.Replace("{{POS_X}}", result.Town.Position.X.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{POS_Y}}", result.Town.Position.Y.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_POS_X}}", result.Gate.Position.X.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_POS_Y}}", result.Gate.Position.Y.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_ROT}}", result.Gate.Rotation.ToString("0.######", CultureInfo.InvariantCulture));
			if (faceIndex > 0)
			{
				text3 = text3.Replace("<Settlement ", $"<Settlement hsr_navmesh=\"{faceIndex}\" ");
			}
			float rotation = result.Town.Rotation;
			text3 = text3.Replace("<Settlement ", "<Settlement hsr_yaw=\"" + rotation.ToString("R", CultureInfo.InvariantCulture) + "\" ");
			SettlementRotations[text2] = rotation;
			if (result.Port.HasValue)
			{
				int faceIndex2 = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(result.Port.Value.Position, isOnLand: false)).FaceIndex;
				if (faceIndex2 > 0)
				{
					string text4 = string.Format("port_posX=\"{0}\" port_posY=\"{1}\" port_navmesh=\"{2}\" ", result.Port.Value.Position.X.ToString("R", CultureInfo.InvariantCulture), result.Port.Value.Position.Y.ToString("R", CultureInfo.InvariantCulture), faceIndex2);
					text3 = text3.Replace("<Settlement ", "<Settlement " + text4);
				}
				text3 = InjectPortLocation(text3);
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateTown: injected port Location into XML for '" + text2 + "'.");
			}
			text3 = ApplyBorrowedScenesForCulture(text3, culture, "town");
			Settlement settlement = RegisterSettlementXml(text3, text2);
			if (settlement == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateTown: settlement '" + text2 + "' was not registered.");
				return null;
			}
			InitTownSettlement(settlement, owner);
			HomesteadSettlementBehavior.Instance?.Record(text2, text3, name, owner.StringId);
			SessionSettlementXmls.Add(text3);
			TraceLogger.Write("HomesteadSettlementBuilder", $"CreateTown: built town '{text2}' '{name}' at ({result.Town.Position.X:0.#},{result.Town.Position.Y:0.#}) owner '{owner.Name}'.");
			return settlement;
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"CreateTown failed: {arg}");
			return null;
		}
	}

	public static Settlement? CreateCastle(HomesteadSettlementPlacementMapView.Result result, string name, CultureObject culture, Clan owner)
	{
		try
		{
			string text = ResolveTemplateFile("empire_castle.xml");
			if (text == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateCastle: no castle template file found.");
				return null;
			}
			string text2 = "hsr_settlement_castle_" + MBRandom.RandomInt(100000, 999999);
			int faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(result.CastleGate.Position, isOnLand: true)).FaceIndex;
			string text3 = File.ReadAllText(text).Replace("{{ID}}", text2).Replace("{{NAME}}", SecurityElement.Escape(name) ?? name)
				.Replace("{{PLAYER_CLAN}}", owner.StringId)
				.Replace("{{PLAYER_CULTURE}}", culture.StringId)
				.Replace("{{POS_X}}", result.Castle.Position.X.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{POS_Y}}", result.Castle.Position.Y.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_POS_X}}", result.CastleGate.Position.X.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_POS_Y}}", result.CastleGate.Position.Y.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{G_ROT}}", result.CastleGate.Rotation.ToString("0.######", CultureInfo.InvariantCulture));
			if (faceIndex > 0)
			{
				text3 = text3.Replace("<Settlement ", $"<Settlement hsr_navmesh=\"{faceIndex}\" ");
			}
			float rotation = result.Castle.Rotation;
			text3 = text3.Replace("<Settlement ", "<Settlement hsr_yaw=\"" + rotation.ToString("R", CultureInfo.InvariantCulture) + "\" ");
			SettlementRotations[text2] = rotation;
			text3 = ApplyBorrowedScenesForCulture(text3, culture, "castle");
			Settlement settlement = RegisterSettlementXml(text3, text2);
			if (settlement == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateCastle: settlement '" + text2 + "' was not registered.");
				return null;
			}
			InitTownSettlement(settlement, owner);
			HomesteadSettlementBehavior.Instance?.Record(text2, text3, name, owner.StringId);
			SessionSettlementXmls.Add(text3);
			TraceLogger.Write("HomesteadSettlementBuilder", $"CreateCastle: built castle '{text2}' '{name}' at ({result.Castle.Position.X:0.#},{result.Castle.Position.Y:0.#}) owner '{owner.Name}'.");
			return settlement;
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"CreateCastle failed: {arg}");
			return null;
		}
	}

	public static Settlement? CreateVillage(Vec2 position, string name, CultureObject culture, Clan owner, string boundSettlementId, string villageTypeId)
	{
		try
		{
			string text = ResolveTemplateFile("empire_village.xml");
			if (text == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateVillage: no village template file found.");
				return null;
			}
			string text2 = "hsr_settlement_village_" + MBRandom.RandomInt(100000, 999999);
			int faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(new CampaignVec2(position, isOnLand: true)).FaceIndex;
			string text3 = File.ReadAllText(text).Replace("{{ID}}", text2).Replace("{{NAME}}", SecurityElement.Escape(name) ?? name)
				.Replace("{{PLAYER_CULTURE}}", culture.StringId)
				.Replace("{{POS_X}}", position.X.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{POS_Y}}", position.Y.ToString("0.######", CultureInfo.InvariantCulture))
				.Replace("{{VILLAGE_TYPE}}", villageTypeId)
				.Replace("{{BOUND_ID}}", boundSettlementId);
			if (faceIndex > 0)
			{
				text3 = text3.Replace("<Settlement ", $"<Settlement hsr_navmesh=\"{faceIndex}\" ");
			}
			text3 = ApplyBorrowedScenesForCulture(text3, culture, "village");
			Settlement settlement = RegisterSettlementXml(text3, text2);
			if (settlement == null)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "CreateVillage: settlement '" + text2 + "' was not registered.");
				return null;
			}
			InitVillageSettlement(settlement);
			HomesteadSettlementBehavior.Instance?.Record(text2, text3, name, owner.StringId);
			SessionSettlementXmls.Add(text3);
			TraceLogger.Write("HomesteadSettlementBuilder", "CreateVillage: built village '" + text2 + "' '" + name + "' type '" + villageTypeId + "' bound to '" + boundSettlementId + "'.");
			return settlement;
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"CreateVillage failed: {arg}");
			return null;
		}
	}

	public static Settlement CreatePlacedSettlements(HomesteadSettlementPlacementMapView.Result result, IReadOnlyList<Hero> apprentices, out Settlement castle, out List<(Settlement Village, Hero Headman)> villageHeadmen)
	{
		castle = null;
		villageHeadmen = new List<(Settlement, Hero)>();
		CultureObject cultureObject = Hero.MainHero?.Culture ?? MBObjectManager.Instance?.GetObject<CultureObject>("empire");
		Clan playerClan = Clan.PlayerClan;
		if (result == null || cultureObject == null || playerClan == null)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "CreatePlacedSettlements: no player culture/clan.");
			return null;
		}
		Homestead homestead = result.Homestead;
		string name = ((homestead != null) ? TownNameFor(homestead) : "Settlement");
		string name2 = ((homestead != null) ? CastleNameFor(homestead) : "Castle");
		Settlement settlement = CreateTown(result, name, cultureObject, playerClan);
		if (settlement == null)
		{
			return null;
		}
		castle = CreateCastle(result, name2, cultureObject, playerClan);
		int num = 0;
		for (int i = 0; i < result.Villages.Count; i++)
		{
			HomesteadSettlementPlacementMapView.Placed placed = result.Villages[i];
			Settlement settlement2 = ((placed.BoundToCastle && castle != null) ? castle : settlement);
			Hero hero = ((apprentices != null && i < apprentices.Count) ? apprentices[i] : null);
			string name3 = VillageNameFor(hero);
			Settlement settlement3 = CreateVillage(placed.Position, name3, cultureObject, playerClan, settlement2.StringId, placed.ResourceId);
			if (settlement3 != null)
			{
				num++;
				villageHeadmen.Add((settlement3, hero));
			}
		}
		TraceLogger.Write("HomesteadSettlementBuilder", string.Format("CreatePlacedSettlements: town='{0}' castle='{1}' villages={2}/{3}.", settlement.StringId, castle?.StringId ?? "null", num, result.Villages.Count));
		return settlement;
	}

	private static Settlement FindSceneDonor(CultureObject culture, string kind)
	{
		return Settlement.All.FirstOrDefault((Settlement s) => Match(s) && s.Culture == culture) ?? Settlement.All.FirstOrDefault(Match);
		bool Match(Settlement s)
		{
			if (s.LocationComplex != null && s.StringId != null && !s.StringId.StartsWith("hsr_settlement_"))
			{
				if (!(kind == "town"))
				{
					if (!(kind == "castle"))
					{
						return s.IsVillage;
					}
					return s.IsCastle;
				}
				return s.IsTown;
			}
			return false;
		}
	}

	private static string SafeGetSceneName(Location loc, int level)
	{
		try
		{
			return loc.GetSceneName(level);
		}
		catch
		{
			return null;
		}
	}

	private static string ApplyBorrowedScenes(string xml, Settlement donor)
	{
		if (donor?.LocationComplex == null)
		{
			return xml;
		}
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			foreach (XmlElement item in xmlDocument.GetElementsByTagName("Location"))
			{
				Location locationWithId = donor.LocationComplex.GetLocationWithId(item.GetAttribute("id"));
				if (locationWithId != null)
				{
					string value = SafeGetSceneName(locationWithId, 1);
					string value2 = SafeGetSceneName(locationWithId, 2);
					string value3 = SafeGetSceneName(locationWithId, 3);
					if (item.HasAttribute("scene_name") && !string.IsNullOrEmpty(value))
					{
						item.SetAttribute("scene_name", value);
					}
					if (item.HasAttribute("scene_name_1") && !string.IsNullOrEmpty(value))
					{
						item.SetAttribute("scene_name_1", value);
					}
					if (item.HasAttribute("scene_name_2") && !string.IsNullOrEmpty(value2))
					{
						item.SetAttribute("scene_name_2", value2);
					}
					if (item.HasAttribute("scene_name_3") && !string.IsNullOrEmpty(value3))
					{
						item.SetAttribute("scene_name_3", value3);
					}
				}
			}
			if (donor.SettlementComponent != null)
			{
				XmlElement xmlElement2 = xmlDocument.GetElementsByTagName("Town").Cast<XmlElement>().FirstOrDefault() ?? xmlDocument.GetElementsByTagName("Village").Cast<XmlElement>().FirstOrDefault();
				if (xmlElement2 != null)
				{
					if (!string.IsNullOrEmpty(donor.SettlementComponent.BackgroundMeshName))
					{
						xmlElement2.SetAttribute("background_mesh", donor.SettlementComponent.BackgroundMeshName);
					}
					if (!string.IsNullOrEmpty(donor.SettlementComponent.WaitMeshName))
					{
						xmlElement2.SetAttribute("wait_mesh", donor.SettlementComponent.WaitMeshName);
					}
					if (!string.IsNullOrEmpty(donor.SettlementComponent.CastleBackgroundMeshName))
					{
						xmlElement2.SetAttribute("castle_background_mesh", donor.SettlementComponent.CastleBackgroundMeshName);
					}
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"ApplyBorrowedScenes: borrowed location scenes from '{donor.Name}' (culture '{donor.Culture?.StringId}').");
			return xmlDocument.OuterXml;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ApplyBorrowedScenes failed: " + ex.Message);
			return xml;
		}
	}

	private static string ApplyBorrowedScenesForCulture(string xml, CultureObject culture, string kind)
	{
		if (culture == null || string.Equals(culture.StringId, "empire", StringComparison.OrdinalIgnoreCase))
		{
			return xml;
		}
		Settlement settlement = FindSceneDonor(culture, kind);
		if (settlement == null)
		{
			return xml;
		}
		return ApplyBorrowedScenes(xml, settlement);
	}

	private static string RewriteXmlCultureAndScenes(string xml, CultureObject newCulture, string kind)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			if (!(xmlDocument.SelectSingleNode("//Settlement") is XmlElement xmlElement))
			{
				return xml;
			}
			xmlElement.SetAttribute("culture", "Culture." + newCulture.StringId);
			return ApplyBorrowedScenesForCulture(xmlDocument.OuterXml, newCulture, kind);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RewriteXmlCultureAndScenes failed: " + ex.Message);
			return xml;
		}
	}

	private static string ExtractSettlementId(string xml)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			return (xmlDocument.SelectSingleNode("//Settlement") as XmlElement)?.GetAttribute("id");
		}
		catch
		{
			return null;
		}
	}

	public static int FixSettlementCultureForClan(Clan clan)
	{
		if (clan?.Culture == null)
		{
			return 0;
		}
		CultureObject culture = clan.Culture;
		int num = 0;
		foreach (Settlement item in (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
			where s.StringId != null && s.StringId.StartsWith("hsr_settlement_") && s.OwnerClan == clan
			select s).ToList())
		{
			try
			{
				if (item.Culture == culture)
				{
					continue;
				}
				string text = (item.IsCastle ? "castle" : (item.IsTown ? "town" : "village"));
				item.Culture = culture;
				for (int num2 = 0; num2 < SessionSettlementXmls.Count; num2++)
				{
					if (!(ExtractSettlementId(SessionSettlementXmls[num2]) != item.StringId))
					{
						SessionSettlementXmls[num2] = RewriteXmlCultureAndScenes(SessionSettlementXmls[num2], culture, text);
						break;
					}
				}
				try
				{
					item.Party?.SetVisualAsDirty();
				}
				catch
				{
				}
				num++;
				TraceLogger.Write("HomesteadSettlementBuilder", "FixSettlementCultureForClan: '" + item.StringId + "' (" + text + ") culture -> '" + culture.StringId + "'.");
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "FixSettlementCultureForClan failed for '" + item?.StringId + "': " + ex.Message);
			}
		}
		return num;
	}

	private static void InitVillageSettlement(Settlement s)
	{
		s.IsVisible = true;
		s.IsInspected = true;
		try
		{
			s.Party.SetVisualAsDirty();
		}
		catch
		{
		}
		TryAddSettlementVisual(s);
		SeedVillageEconomy(s);
		try
		{
			s.OnGameCreated();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "Village OnGameCreated: " + ex.Message);
		}
		try
		{
			s.AfterInitialized();
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "Village AfterInitialized: " + ex2.Message);
		}
		try
		{
			s.OnFinishLoadState();
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "Village OnFinishLoadState: " + ex3.Message);
		}
	}

	private static void SeedVillageEconomy(Settlement s)
	{
		try
		{
			if (s?.Village != null)
			{
				if (s.Village.Hearth <= 0f)
				{
					s.Village.Hearth = 300f;
				}
				Settlement bound = s.Village.Bound;
				Settlement settlement = ((bound != null && bound.IsTown) ? bound : ResolveNearestTown(s));
				if (settlement != null && s.Village.TradeBound != settlement)
				{
					s.Village.TradeBound = settlement;
					TraceLogger.Write("HomesteadSettlementBuilder", "SeedVillageEconomy: '" + s.StringId + "' TradeBound -> '" + settlement.StringId + "' (bound='" + (bound?.StringId ?? "null") + "').");
				}
				else if (s.Village.TradeBound == null && bound != null)
				{
					s.Village.TradeBound = bound;
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "SeedVillageEconomy '" + s?.StringId + "': " + ex.Message);
		}
	}

	private static Settlement ResolveNearestTown(Settlement from)
	{
		Settlement settlement = null;
		Settlement settlement2 = null;
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		foreach (Settlement item in Settlement.All)
		{
			if (item != null && item.IsTown)
			{
				float num3 = item.GetPosition2D.Distance(from.GetPosition2D);
				if (num3 < num2)
				{
					num2 = num3;
					settlement2 = item;
				}
				if (from.MapFaction != null && item.MapFaction == from.MapFaction && num3 < num)
				{
					num = num3;
					settlement = item;
				}
			}
		}
		return settlement ?? settlement2;
	}

	public static void AssignAlleyOwners(Settlement town)
	{
		try
		{
			if (town?.Alleys == null || town.Alleys.Count == 0)
			{
				return;
			}
			List<Hero> list = town.Notables?.Where((Hero n) => n != null && n.IsAlive && n.IsGangLeader).ToList();
			if (list == null || list.Count == 0)
			{
				return;
			}
			int num = 0;
			for (int num2 = 0; num2 < town.Alleys.Count && num2 < list.Count; num2++)
			{
				Alley alley = town.Alleys[num2];
				if (alley != null && alley.Owner != Hero.MainHero)
				{
					Hero hero = list[num2];
					if (alley.Owner != hero)
					{
						alley.SetOwner(hero);
						num++;
					}
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"AssignAlleyOwners: '{town.StringId}' assigned {num}/{town.Alleys.Count} alley(s) among {list.Count} gang leader(s).");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "AssignAlleyOwners '" + town?.StringId + "': " + ex.Message);
		}
	}

	public static void RestoreAlleyOwnersOnLoad()
	{
		try
		{
			foreach (Settlement objectType in MBObjectManager.Instance.GetObjectTypeList<Settlement>())
			{
				if (objectType != null && objectType.StringId != null && objectType.StringId.StartsWith("hsr_settlement_") && objectType.Town != null && !objectType.IsCastle)
				{
					AssignAlleyOwners(objectType);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreAlleyOwnersOnLoad: " + ex.Message);
		}
	}

	public static void PopulateVillageLandowners(Settlement village)
	{
		if (village?.Notables == null)
		{
			return;
		}
		try
		{
			int num = Campaign.Current.Models.NotableSpawnModel.GetTargetNotableCountForSettlement(village, Occupation.RuralNotable);
			if (num <= 0)
			{
				num = 2;
			}
			int num2 = village.Notables.Count((Hero n) => n != null && n.IsRuralNotable && !(HomesteadSettlementBehavior.Instance?.IsFemaleCompatNotable(n) ?? false));
			CultureObject playerCulture = Hero.MainHero?.Culture;
			CharacterObject characterObject = ((playerCulture != null) ? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && !c.IsFemale && c.Culture == playerCulture && c.Occupation == Occupation.RuralNotable) : null);
			for (int num3 = num2; num3 < num; num3++)
			{
				Hero hero = ((characterObject != null) ? HeroCreator.CreateSpecialHero(characterObject, village) : HeroCreator.CreateNotable(Occupation.RuralNotable, village));
				Utils.ApplyRandomPersonalityTraits(hero);
				try
				{
					EnterSettlementAction.ApplyForCharacterOnly(hero, village);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "PopulateVillageLandowners: EnterSettlementAction failed for '" + hero?.StringId + "': " + ex.Message);
				}
			}
			if (num2 < num)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", $"PopulateVillageLandowners: '{village.StringId}' {num2} -> {village.Notables.Count((Hero n) => n?.IsRuralNotable ?? false)} landowner(s) " + string.Format("(target {0}, template culture={1}).", num, characterObject?.Culture?.StringId ?? "native default (empire)"));
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "PopulateVillageLandowners failed for '" + village?.StringId + "': " + ex2.Message);
		}
	}

	private static bool IsFemaleVillageNotableModActive()
	{
		if (_femaleVillageNotableModActive.HasValue)
		{
			return _femaleVillageNotableModActive.Value;
		}
		try
		{
			_femaleVillageNotableModActive = Utilities.GetModulesNames().Any((string m) => string.Equals(m, "FemaleVillageNotable", StringComparison.OrdinalIgnoreCase));
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "IsFemaleVillageNotableModActive check failed: " + ex.Message);
			_femaleVillageNotableModActive = false;
		}
		return _femaleVillageNotableModActive.Value;
	}

	public static void ApplyFemaleVillageNotableCompat(Settlement village)
	{
		if (village?.Notables == null || !IsFemaleVillageNotableModActive())
		{
			return;
		}
		try
		{
			if (village.Notables.Any((Hero h) => h?.IsFemale ?? false))
			{
				return;
			}
			CultureObject culture = village.Culture;
			List<CharacterObject> list = CharacterObject.All.Where((CharacterObject c) => c != null && c.IsFemale && c.Culture == culture && c.Occupation == Occupation.RuralNotable).ToList();
			if (list.Count == 0)
			{
				return;
			}
			int num = MBRandom.RandomInt(2) + 1;
			int num2 = 0;
			for (int num3 = 0; num3 < num; num3++)
			{
				CharacterObject characterObject = list[MBRandom.RandomInt(list.Count)];
				Hero hero = HeroCreator.CreateSpecialHero(characterObject, village, null, null, 25 + MBRandom.RandomInt(20));
				if (hero != null)
				{
					Utils.ApplyRandomPersonalityTraits(hero);
					NameGenerator.Current.GenerateHeroNameAndHeroFullName(hero, out var firstName, out var _, useDeterministicValues: false);
					hero.SetName(new TextObject(firstName.ToString() + " of " + village.Name.ToString()), firstName);
					hero.AddPower(50f + MBRandom.RandomFloat * 100f);
					Equipment randomBattleEquipment = characterObject.RandomBattleEquipment;
					if (randomBattleEquipment != null)
					{
						EquipmentHelper.AssignHeroEquipmentFromEquipment(hero, randomBattleEquipment);
					}
					Equipment randomCivilianEquipment = characterObject.RandomCivilianEquipment;
					if (randomCivilianEquipment != null)
					{
						hero.CivilianEquipment.FillFrom(randomCivilianEquipment, useSourceEquipmentType: false);
					}
					hero.ChangeState(Hero.CharacterStates.Active);
					EnterSettlementAction.ApplyForCharacterOnly(hero, village);
					num2++;
				}
			}
			if (num2 > 0)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", $"ApplyFemaleVillageNotableCompat: added {num2} female notable(s) to '{village.StringId}'.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ApplyFemaleVillageNotableCompat failed for '" + village?.StringId + "': " + ex.Message);
		}
	}

	public static void RestoreVillageLandownersOnLoad()
	{
	}

	private static string SaveDir(string saveName)
	{
		if (string.IsNullOrEmpty(saveName))
		{
			saveName = Campaign.Current?.UniqueGameId ?? "default";
		}
		else if (saveName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
		{
			saveName = saveName.Substring(0, saveName.Length - 4);
		}
		return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mount and Blade II Bannerlord", "Configs", "HomesteadsReloaded", "Saves", saveName);
	}

	private static string SaveFilePath(string saveName)
	{
		return System.IO.Path.Combine(SaveDir(saveName), "settlements.xml");
	}

	public static void FlushToSaveFile(string saveName)
	{
		try
		{
			if (string.IsNullOrEmpty(saveName))
			{
				return;
			}
			if (saveName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
			{
				saveName = saveName.Substring(0, saveName.Length - 4);
			}
			Directory.CreateDirectory(SaveDir(saveName));
			string text = SaveFilePath(saveName);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml("<Settlements></Settlements>");
			foreach (string sessionSettlementXml in SessionSettlementXmls)
			{
				XmlDocument xmlDocument2 = new XmlDocument();
				xmlDocument2.LoadXml(sessionSettlementXml);
				foreach (XmlNode item in xmlDocument2.SelectNodes("//Settlement"))
				{
					XmlElement newChild = (XmlElement)xmlDocument.ImportNode(item, deep: true);
					xmlDocument.DocumentElement.AppendChild(newChild);
				}
			}
			xmlDocument.Save(text);
			TraceLogger.Write("HomesteadSettlementBuilder", $"FlushToSaveFile: wrote {SessionSettlementXmls.Count} settlements to '{text}'");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "FlushToSaveFile failed: " + ex.Message);
		}
	}

	public static int ClearSaveFile()
	{
		try
		{
			int num = 0;
			string text = MBSaveLoad.ActiveSaveSlotName;
			if (!string.IsNullOrEmpty(text))
			{
				if (text.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - 4);
				}
				string text2 = SaveFilePath(text);
				if (File.Exists(text2))
				{
					try
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(text2);
						num += xmlDocument.DocumentElement?.ChildNodes.Count ?? 0;
					}
					catch
					{
					}
					File.Delete(text2);
					TraceLogger.Write("HomesteadSettlementBuilder", "ClearSaveFile: deleted '" + text2 + "'");
				}
			}
			string text3 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mount and Blade II Bannerlord", "Configs", "HomesteadsReloaded", Campaign.Current?.UniqueGameId ?? "default", "settlements.xml");
			if (File.Exists(text3))
			{
				try
				{
					XmlDocument xmlDocument2 = new XmlDocument();
					xmlDocument2.Load(text3);
					num += xmlDocument2.DocumentElement?.ChildNodes.Count ?? 0;
				}
				catch
				{
				}
				File.Delete(text3);
				TraceLogger.Write("HomesteadSettlementBuilder", "ClearSaveFile: deleted legacy '" + text3 + "'");
			}
			SessionSettlementXmls.Clear();
			return num;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ClearSaveFile failed: " + ex.Message);
			return 0;
		}
	}

	public static void ReregisterFromSaveFile()
	{
		try
		{
			SessionSettlementXmls.Clear();
			string text = MBSaveLoad.ActiveSaveSlotName;
			if (string.IsNullOrEmpty(text))
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "ReregisterFromSaveFile: MBSaveLoad.ActiveSaveSlotName is empty! Using 'default'");
				text = "default";
			}
			if (text.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(0, text.Length - 4);
			}
			string text2 = SaveFilePath(text);
			if (!File.Exists(text2))
			{
				string text3 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mount and Blade II Bannerlord", "Configs", "HomesteadsReloaded", Campaign.Current?.UniqueGameId ?? "default", "settlements.xml");
				if (File.Exists(text3))
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "ReregisterFromSaveFile: '" + text2 + "' not found, falling back to legacy '" + text3 + "'");
					text2 = text3;
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"ReregisterFromSaveFile: saveName='{text}' file='{text2}' exists={File.Exists(text2)}");
			if (!File.Exists(text2))
			{
				return;
			}
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(text2);
			if (xmlDocument.DocumentElement == null)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (XmlNode childNode in xmlDocument.DocumentElement.ChildNodes)
			{
				if (childNode.NodeType == XmlNodeType.Comment)
				{
					continue;
				}
				string text4 = childNode.Attributes?["id"]?.Value;
				if (!string.IsNullOrEmpty(text4))
				{
					SessionSettlementXmls.Add(childNode.OuterXml);
					num2++;
					Settlement settlement = RegisterOneOnLoad(childNode, text4);
					if (settlement != null && settlement.IsReady)
					{
						num++;
						FinalizeLoadedSettlement(settlement, childNode);
					}
					TraceLogger.Write("HomesteadSettlementBuilder", $"ReregisterFromSaveFile: '{text4}' registered={settlement != null} ready={settlement?.IsReady ?? false}");
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"ReregisterFromSaveFile: {num}/{num2} settlement(s) ready in type list (now {MBObjectManager.Instance.GetObjectTypeList<Settlement>().Count}).");
			try
			{
				MBReadOnlyList<Settlement> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
				if (objectTypeList != null)
				{
					foreach (IGrouping<string, Settlement> item in from x in objectTypeList
						where x.StringId != null && x.StringId.StartsWith("hsr_settlement_")
						group x by x.StringId)
					{
						if (item.Count() <= 1)
						{
							continue;
						}
						Settlement settlement2 = item.First();
						foreach (Settlement item2 in item.Skip(1).ToList())
						{
							try
							{
								MBObjectManager.Instance.UnregisterObject(item2);
							}
							catch
							{
							}
							TraceLogger.Write("HomesteadSettlementBuilder", "ReregisterFromSaveFile: Purged ghost duplicate '" + item2.StringId + "'.");
						}
						try
						{
							Settlement settlement3 = MBObjectManager.Instance.GetObject<Settlement>(settlement2.StringId);
							if (settlement3 != settlement2)
							{
								if (settlement3 != null)
								{
									MBObjectManager.Instance.UnregisterObject(settlement3);
								}
								MBObjectManager.Instance.RegisterObject(settlement2);
							}
						}
						catch
						{
						}
					}
				}
			}
			catch (Exception arg)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", $"Ghost cleanup failed: {arg}");
			}
			try
			{
				foreach (Settlement settlement4 in Campaign.Current.Settlements)
				{
					bool flag = string.IsNullOrWhiteSpace(settlement4?.Name?.ToString());
					bool flag2 = settlement4 != null && settlement4.StringId != null && settlement4.StringId.StartsWith("hsr_settlement_");
					if (flag || flag2)
					{
						Vec2 getPosition2D = settlement4.GetPosition2D;
						TraceLogger.Write("HomesteadSettlementBuilder", $"ReregisterFromSaveFile: [FINAL SWEEP] '{settlement4.StringId}' name='{settlement4.Name}' blankName={flag} " + $"objId={RuntimeHelpers.GetHashCode(settlement4)} " + $"pos=({getPosition2D.x:0.###},{getPosition2D.y:0.###}) ready={settlement4.IsReady} isTown={settlement4.IsTown} isCastle={settlement4.IsCastle} isVillage={settlement4.IsVillage} ownerClan={settlement4.OwnerClan?.StringId}.");
					}
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "Final sweep diagnostic failed: " + ex.Message);
			}
		}
		catch (Exception arg2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"ReregisterFromSaveFile failed: {arg2}");
		}
	}

	private static Settlement RegisterOneOnLoad(XmlNode settlementNode, string stringId)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml("<Settlements>" + settlementNode.OuterXml + "</Settlements>");
		foreach (XmlNode item in xmlDocument.SelectNodes("//Settlement"))
		{
			if (item.Attributes?["owner"] != null)
			{
				item.Attributes.RemoveNamedItem("owner");
			}
		}
		string text = settlementNode.Attributes?["owner"]?.Value ?? settlementNode.SelectSingleNode(".//*[@owner]")?.Attributes?["owner"]?.Value;
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = (text.StartsWith("Faction.") ? text.Substring(8) : text);
			Clan clan = MBObjectManager.Instance.GetObject<Clan>(text2);
			if (clan == null)
			{
				try
				{
					clan = MBObjectManager.Instance.CreateObject<Clan>(text2);
				}
				catch
				{
				}
			}
			if (clan != null)
			{
				FixPresumedClanCaches(clan);
			}
		}
		FieldInfo fieldInfo = null;
		object value = null;
		try
		{
			fieldInfo = AccessTools.Field(typeof(Campaign), "_gameLoadingType");
			if (fieldInfo != null && Campaign.Current != null)
			{
				value = fieldInfo.GetValue(Campaign.Current);
				fieldInfo.SetValue(Campaign.Current, Campaign.GameLoadingType.NewCampaign);
			}
		}
		catch (Exception arg)
		{
			TraceLogger.Write("RegisterOneOnLoad", $"_gameLoadingType reflection threw: {arg}");
		}
		try
		{
			Clan clan2 = MBObjectManager.Instance.GetObject<Settlement>(stringId)?.Town?.OwnerClan;
			if (clan2 != null)
			{
				FixPresumedClanCaches(clan2);
			}
		}
		catch
		{
		}
		HashSet<string> beforeIds = new HashSet<string>(from x in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
			select x.StringId into id
			where id != null
			select id);
		bool flag = false;
		try
		{
			MBObjectManager.Instance.LoadXml(xmlDocument);
		}
		catch (Exception arg2)
		{
			flag = true;
			TraceLogger.Write("RegisterOneOnLoad", $"LoadXml threw: {arg2}");
		}
		Settlement settlement = MBObjectManager.Instance.GetObject<Settlement>(stringId);
		if (settlement?.OwnerClan != null)
		{
			FixPresumedClanCaches(settlement.OwnerClan);
		}
		if (settlement != null && (flag || !settlement.IsReady))
		{
			foreach (Settlement item2 in (from x in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
				where x.StringId != null && !beforeIds.Contains(x.StringId)
				select x).ToList())
			{
				try
				{
					MBObjectManager.Instance.UnregisterObject(item2);
					TraceLogger.Write("RegisterOneOnLoad", "Purged side-effect object '" + item2.StringId + "' from the failed first attempt before retrying '" + stringId + "'.");
				}
				catch (Exception ex)
				{
					TraceLogger.Write("RegisterOneOnLoad", "Failed to unregister side-effect object '" + item2.StringId + "': " + ex.Message);
				}
			}
			if (MBObjectManager.Instance.GetObject<Settlement>(stringId) != null)
			{
				MBObjectManager.Instance.UnregisterObject(settlement);
			}
			try
			{
				MBObjectManager.Instance.LoadXml(xmlDocument);
			}
			catch (Exception arg3)
			{
				TraceLogger.Write("RegisterOneOnLoad", $"RETRY LoadXml threw: {arg3}");
			}
			settlement = MBObjectManager.Instance.GetObject<Settlement>(stringId);
			TraceLogger.Write("RegisterOneOnLoad", $"Retried '{stringId}' (loadThrew={flag}) -> ready={settlement?.IsReady ?? false}.");
		}
		try
		{
			if (settlement != null && settlementNode.Attributes?["hsr_navmesh"] != null && int.TryParse(settlementNode.Attributes["hsr_navmesh"].Value, out var result))
			{
				AccessTools.PropertySetter(typeof(Settlement), "NavMeshFaceIndex")?.Invoke(settlement, new object[1] { result });
			}
		}
		catch
		{
		}
		try
		{
			if (settlement != null && settlementNode.Attributes?["hsr_yaw"] != null && float.TryParse(settlementNode.Attributes["hsr_yaw"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
			{
				SettlementRotations[stringId] = result2;
			}
		}
		catch
		{
		}
		try
		{
			if (fieldInfo != null && Campaign.Current != null)
			{
				fieldInfo.SetValue(Campaign.Current, value);
			}
		}
		catch (Exception arg4)
		{
			TraceLogger.Write("RegisterOneOnLoad", $"_gameLoadingType restore threw: {arg4}");
		}
		try
		{
			if (settlement != null)
			{
				string text3 = settlementNode.Attributes?["posX"]?.Value ?? "?";
				string text4 = settlementNode.Attributes?["posY"]?.Value ?? "?";
				Vec2 getPosition2D = settlement.GetPosition2D;
				TraceLogger.Write("RegisterOneOnLoad", $"'{stringId}' final objId={RuntimeHelpers.GetHashCode(settlement)} " + $"actualPos=({getPosition2D.x:0.###},{getPosition2D.y:0.###}) expectedPos=({text3},{text4}) " + $"name='{settlement.Name}' ready={settlement.IsReady}.");
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("RegisterOneOnLoad", "Position diagnostic failed: " + ex2.Message);
		}
		return settlement;
	}

	private static void FixPresumedClanCaches(Clan clan)
	{
		try
		{
			if (clan == null)
			{
				return;
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"FixPresumedClanCaches: running for '{clan.StringId}' (IsReady={clan.IsReady})");
			string[] array = new string[5] { "_fiefsCache", "_townsCache", "_castlesCache", "_villagesCache", "_settlementsCache" };
			string[] array2 = array;
			foreach (string text in array2)
			{
				FieldInfo fieldInfo = AccessTools.Field(typeof(Clan), text);
				if (fieldInfo != null)
				{
					if (fieldInfo.GetValue(clan) == null)
					{
						object value = Activator.CreateInstance(fieldInfo.FieldType);
						fieldInfo.SetValue(clan, value);
						TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches: initialized " + text + " for clan '" + clan.StringId + "'");
					}
					else
					{
						TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches: " + text + " was already non-null for clan '" + clan.StringId + "'");
					}
				}
				else
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches: Field " + text + " not found on Clan!");
				}
			}
			if (clan.Kingdom == null)
			{
				return;
			}
			TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches: running for Kingdom '" + clan.Kingdom.StringId + "'");
			array2 = array;
			foreach (string text2 in array2)
			{
				FieldInfo fieldInfo2 = AccessTools.Field(typeof(Kingdom), text2);
				if (fieldInfo2 != null && fieldInfo2.GetValue(clan.Kingdom) == null)
				{
					object value2 = Activator.CreateInstance(fieldInfo2.FieldType);
					fieldInfo2.SetValue(clan.Kingdom, value2);
					TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches: initialized " + text2 + " for kingdom '" + clan.Kingdom.StringId + "'");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "FixPresumedClanCaches failed: " + ex.Message);
		}
	}

	private static void FinalizeLoadedSettlement(Settlement s, XmlNode node)
	{
		try
		{
			s.IsVisible = true;
			s.IsInspected = true;
		}
		catch
		{
		}
	}

	public static void ApplyOwnershipOnLoad()
	{
		try
		{
			List<Settlement> list = (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
				where s.StringId != null && s.StringId.StartsWith("hsr_settlement_")
				select s).ToList();
			int num = 0;
			foreach (Settlement item in list)
			{
				if (item.Town == null)
				{
					continue;
				}
				string text = HomesteadSettlementBehavior.Instance?.GetOwnerClanId(item.StringId);
				if (string.IsNullOrEmpty(text))
				{
					try
					{
						item.IsVisible = false;
						if (item.Party != null)
						{
							item.Party.SetVisualAsDirty();
						}
					}
					catch
					{
					}
					TraceLogger.Write("HomesteadSettlementBuilder", "ApplyOwnershipOnLoad: '" + item.StringId + "' NOT OWNED by current save -> hiding it.");
					continue;
				}
				Clan clan = ResolveOwnerClan(text);
				string text2 = item.OwnerClan?.Name?.ToString() ?? "null";
				if (clan == null || clan.Leader == null)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "ApplyOwnershipOnLoad: '" + item.StringId + "' resolved owner invalid (owner='" + (clan?.Name?.ToString() ?? "null") + "' leader='" + (clan?.Leader?.Name?.ToString() ?? "null") + "') — leaving as is.");
					continue;
				}
				if (item.OwnerClan != clan)
				{
					item.Town.OwnerClan = clan;
					try
					{
						item.Party?.SetVisualAsDirty();
					}
					catch
					{
					}
					num++;
				}
				TraceLogger.Write("HomesteadSettlementBuilder", "ApplyOwnershipOnLoad: '" + item.StringId + "' owner " + text2 + " -> '" + (item.OwnerClan?.Name?.ToString() ?? "null") + "' (leader='" + (item.OwnerClan?.Leader?.Name?.ToString() ?? "null") + "') MapFaction='" + (item.MapFaction?.Name?.ToString() ?? "null") + "'");
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"ApplyOwnershipOnLoad: assigned owners to {num}/{list.Count} settlement(s).");
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"ApplyOwnershipOnLoad failed: {arg}");
		}
	}

	public static void FinalizeReloadedSettlements()
	{
		ApplyOwnershipOnLoad();
		try
		{
			List<Settlement> list = (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
				where s.StringId != null && s.StringId.StartsWith("hsr_settlement_")
				orderby s.IsVillage ? 1 : 0
				select s).ToList();
			foreach (Settlement item in list)
			{
				if (item.Village != null)
				{
					VillageType villageType = ResolveRealVillageType(item.Village.VillageType?.StringId);
					if (villageType != null && villageType != item.Village.VillageType)
					{
						item.Village.VillageType = villageType;
						TraceLogger.Write("HomesteadSettlementBuilder", "FinalizeReloadedSettlements: re-resolved village type '" + villageType.StringId + "' for '" + item.StringId + "'.");
					}
				}
				RunReloadLifecycle(item);
				try
				{
					item.Party?.SetVisualAsDirty();
				}
				catch
				{
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"FinalizeReloadedSettlements: ran lifecycle for {list.Count} settlement(s).");
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"FinalizeReloadedSettlements failed: {arg}");
		}
	}

	public static void RestoreNotablesToSettlements()
	{
		List<Settlement> ourSettlements = (from s in MBObjectManager.Instance.GetObjectTypeList<Settlement>()
			where s.StringId != null && s.StringId.StartsWith("hsr_settlement_")
			select s).ToList();
		RestoreNotablesToSettlements(ourSettlements);
		RestoreGarrisonPartySettlements(ourSettlements);
		RestorePatrolPartyHomes(ourSettlements);
	}

	private static void RestorePatrolPartyHomes(List<Settlement> ourSettlements)
	{
		if (ourSettlements == null || ourSettlements.Count == 0)
		{
			return;
		}
		Dictionary<string, Settlement> dictionary = new Dictionary<string, Settlement>();
		foreach (Settlement ourSettlement in ourSettlements)
		{
			if (ourSettlement.StringId != null)
			{
				dictionary[ourSettlement.StringId] = ourSettlement;
			}
		}
		FieldInfo field = typeof(PatrolPartyComponent).GetField("_homeSettlement", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			return;
		}
		int num = 0;
		foreach (MobileParty item in MobileParty.All.ToList())
		{
			try
			{
				if (item == null || !item.IsPatrolParty)
				{
					continue;
				}
				PatrolPartyComponent patrolPartyComponent = item.PatrolPartyComponent;
				if (patrolPartyComponent != null)
				{
					Settlement homeSettlement = patrolPartyComponent.HomeSettlement;
					string text = homeSettlement?.StringId;
					if (text != null && text.StartsWith("hsr_settlement_") && dictionary.TryGetValue(text, out var value) && homeSettlement != value)
					{
						field.SetValue(patrolPartyComponent, value);
						num++;
						TraceLogger.Write("HomesteadSettlementBuilder", $"RestorePatrolPartyHomes: '{item.StringId}' home re-pointed to canonical '{text}' (was stale, Town null={homeSettlement.Town == null}).");
					}
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "RestorePatrolPartyHomes: failed for '" + item?.StringId + "': " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		if (num > 0)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"RestorePatrolPartyHomes: fixed {num} patrol(s).");
		}
	}

	private static void RestoreGarrisonPartySettlements(List<Settlement> ourSettlements)
	{
		if (ourSettlements == null || ourSettlements.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (Settlement ourSettlement in ourSettlements)
		{
			try
			{
				MobileParty mobileParty = ourSettlement?.Town?.GarrisonParty;
				if (mobileParty != null && mobileParty.CurrentSettlement != ourSettlement)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "RestoreGarrisonPartySettlements: '" + ourSettlement.StringId + "' garrison CurrentSettlement was " + string.Format("'{0}' (townNull={1}) — re-pointing to canonical.", mobileParty.CurrentSettlement?.StringId ?? "null", mobileParty.CurrentSettlement?.Town == null));
					mobileParty.CurrentSettlement = ourSettlement;
					num++;
				}
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "RestoreGarrisonPartySettlements: failed for '" + ourSettlement?.StringId + "': " + ex.GetType().Name + ": " + ex.Message);
			}
		}
		if (num > 0)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreGarrisonPartySettlements: fixed {num} garrison party/parties.");
		}
	}

	internal static void RestoreNotablesToSettlements(List<Settlement> ourSettlements)
	{
		if (ourSettlements == null || ourSettlements.Count == 0)
		{
			return;
		}
		Dictionary<string, Settlement> dictionary = new Dictionary<string, Settlement>();
		foreach (Settlement ourSettlement in ourSettlements)
		{
			if (ourSettlement.StringId != null)
			{
				dictionary[ourSettlement.StringId] = ourSettlement;
			}
		}
		int num = 0;
		int num2 = 0;
		foreach (Hero item in Hero.AllAliveHeroes.ToList())
		{
			if (item == null || item.IsDead || item.IsDisabled)
			{
				continue;
			}
			Settlement value;
			Settlement value2;
			Settlement value3;
			Settlement settlement = ((item.BornSettlement != null && dictionary.TryGetValue(item.BornSettlement.StringId ?? "", out value)) ? value : ((item.CurrentSettlement != null && dictionary.TryGetValue(item.CurrentSettlement.StringId ?? "", out value2)) ? value2 : ((item.GovernorOf?.Settlement != null && dictionary.TryGetValue(item.GovernorOf.Settlement.StringId ?? "", out value3)) ? value3 : null)));
			if (settlement == null)
			{
				continue;
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreNotablesToSettlements: found '{item.Name}' ({item.Occupation}) → '{settlement.Name}' " + $"inNotables={settlement.Notables?.Contains(item) ?? false} locCx={settlement.LocationComplex != null}.");
			try
			{
				if (item.StayingInSettlement == settlement && !settlement.HeroesWithoutParty.Contains(item))
				{
					typeof(Hero).GetField("_stayingInSettlement", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(item, null);
					TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreNotablesToSettlements: cleared _stayingInSettlement for '{item.Name}' to force AddHeroWithoutParty.");
				}
				EnterSettlementAction.ApplyForCharacterOnly(item, settlement);
				if (item.IsNotable)
				{
					Homestead.ForceSetHomeSettlement(item, settlement);
				}
				if (settlement.Town != null && settlement.Town.Governor == null && item.Clan == Clan.PlayerClan && !item.IsHumanPlayerCharacter && (settlement.IsTown || settlement.IsCastle))
				{
					try
					{
						ChangeGovernorAction.Apply(settlement.Town, item);
					}
					catch
					{
					}
				}
				num++;
				TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreNotablesToSettlements: re-entered '{item.Name}' ({item.Occupation}) → '{settlement.Name}' " + $"notables={settlement.Notables?.Count ?? (-1)} inHWP={settlement.HeroesWithoutParty.Contains(item)} " + $"objId={RuntimeHelpers.GetHashCode(settlement)}.");
			}
			catch (Exception ex)
			{
				num2++;
				TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreNotablesToSettlements: FAILED '{item?.StringId}' ({item?.Occupation}) → '{settlement?.StringId}': {ex.Message}");
			}
		}
		TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreNotablesToSettlements: restored={num} failed={num2}.");
	}

	public static VillageType ResolveRealVillageType(string villageTypeId)
	{
		if (string.IsNullOrEmpty(villageTypeId))
		{
			return null;
		}
		switch (villageTypeId)
		{
		case "cattle":
			villageTypeId = "cattle_farm";
			break;
		case "wood":
			villageTypeId = "lumberjack";
			break;
		case "clay":
			villageTypeId = "clay_mine";
			break;
		}
		return MBObjectManager.Instance.GetObjectTypeList<VillageType>().FirstOrDefault((VillageType vt) => vt.StringId == villageTypeId && vt.Productions != null && vt.Productions.Count > 0);
	}

	private static void RunReloadLifecycle(Settlement s)
	{
		Town town = s.Town;
		Village village = s.Village;
		float loyalty = 0f;
		float security = 0f;
		int tradeTaxAccumulated = 0;
		int num = 0;
		bool flag = false;
		if (town != null)
		{
			loyalty = town.Loyalty;
			security = town.Security;
			tradeTaxAccumulated = town.TradeTaxAccumulated;
			num = town.Gold;
			flag = true;
		}
		int num2 = 0;
		Village.VillageStates villageState = Village.VillageStates.Normal;
		bool flag2 = false;
		if (village != null)
		{
			num2 = village.Gold;
			villageState = village.VillageState;
			flag2 = true;
		}
		IList list = WallSectionListField?.GetValue(s) as IList;
		List<object> list2 = null;
		if (list != null && list.Count > 0)
		{
			list2 = new List<object>();
			foreach (object item in list)
			{
				list2.Add(item);
			}
		}
		try
		{
			s.OnGameCreated();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RunReloadLifecycle OnGameCreated '" + s.StringId + "': " + ex.Message);
		}
		try
		{
			s.AfterInitialized();
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RunReloadLifecycle AfterInitialized '" + s.StringId + "': " + ex2.Message);
		}
		if (list != null && list2 != null && list.Count != list2.Count)
		{
			int count = list.Count;
			list.Clear();
			foreach (object item2 in list2)
			{
				list.Add(item2);
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"RunReloadLifecycle: '{s.StringId}' wall-section hitpoints list grew from {list2.Count} to {count} " + $"after OnGameCreated — restored to its pre-existing {list2.Count} entries.");
		}
		if (list != null && list.Count > s.WallSectionCount && s.WallSectionCount > 0)
		{
			int count2 = list.Count;
			while (list.Count > s.WallSectionCount)
			{
				list.RemoveAt(list.Count - 1);
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"RunReloadLifecycle: '{s.StringId}' wall-section hitpoints list was still oversized ({count2} entries, expected {s.WallSectionCount}) " + "after restore — truncated to fix a save bloated by earlier reloads.");
		}
		if (flag)
		{
			town.Loyalty = loyalty;
			town.Security = security;
			town.TradeTaxAccumulated = tradeTaxAccumulated;
			town.ChangeGold(num - town.Gold);
		}
		if (flag2)
		{
			village.VillageState = villageState;
			village.ChangeGold(num2 - village.Gold);
		}
		try
		{
			s.OnFinishLoadState();
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RunReloadLifecycle OnFinishLoadState '" + s.StringId + "': " + ex3.Message);
		}
		TryAddSettlementVisual(s);
		if (town != null)
		{
			InitTownBuildings(s);
			SeedMarket(town);
			EnsureShipyardBuilding(s);
			RestoreBuildingState(town, s);
			ReconcileBuildingsInProgress(town);
			RestoreDefaultBuilding(town, s);
			RestoreProsperity(town, s);
			ReconcileWorkshopSettlements(town, s);
			EnsureWeaponWorkshops(town);
		}
		if (village != null)
		{
			SeedVillageEconomy(s);
			RestoreHearth(village, s);
		}
		RestoreExtendedState(s);
	}

	private static void RestoreBuildingState(Town town, Settlement s)
	{
		try
		{
			HomesteadBuiltSettlement homesteadBuiltSettlement = HomesteadSettlementBehavior.Instance?.GetRecord(s.StringId);
			if (homesteadBuiltSettlement?.BuildingState == null)
			{
				return;
			}
			int num = 0;
			foreach (string item in homesteadBuiltSettlement.BuildingState)
			{
				string[] parts = item.Split(new char[1] { '|' });
				if (parts.Length != 3)
				{
					continue;
				}
				Building building = town.Buildings.FirstOrDefault((Building x) => x.BuildingType?.StringId == parts[0]);
				if (building != null)
				{
					if (int.TryParse(parts[1], out var result))
					{
						building.CurrentLevel = result;
					}
					if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
					{
						building.BuildingProgress = result2;
					}
					num++;
				}
			}
			if (homesteadBuiltSettlement.BuildingQueue != null)
			{
				town.BuildingsInProgress.Clear();
				foreach (string typeId in homesteadBuiltSettlement.BuildingQueue)
				{
					Building building2 = town.Buildings.FirstOrDefault((Building x) => x.BuildingType?.StringId == typeId);
					if (building2 != null)
					{
						town.BuildingsInProgress.Enqueue(building2);
					}
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreBuildingState: '{s.StringId}' applied {num} building level/progress entries, queue={homesteadBuiltSettlement.BuildingQueue?.Count ?? 0}.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreBuildingState failed for '" + s?.StringId + "': " + ex.Message);
		}
	}

	private static void RestoreDefaultBuilding(Town town, Settlement s)
	{
		try
		{
			string defId = HomesteadSettlementBehavior.Instance?.GetStoredDefaultBuildingId(s.StringId);
			TraceLogger.Write("HomesteadSettlementBuilder", string.Format("DIAG RestoreDefaultBuilding '{0}': InstanceNull={1}, storedDefId='{2}', currentDefault='{3}'.", s.StringId, HomesteadSettlementBehavior.Instance == null, defId ?? "NULL", town.CurrentDefaultBuilding?.BuildingType?.StringId ?? "null"));
			if (!string.IsNullOrEmpty(defId))
			{
				Building building = town.Buildings.FirstOrDefault((Building b) => b.BuildingType?.StringId == defId);
				if (building == null)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", $"DIAG RestoreDefaultBuilding '{s.StringId}': target building '{defId}' NOT FOUND in town.Buildings (count={town.Buildings.Count}).");
				}
				else if (!building.IsCurrentlyDefault)
				{
					BuildingHelper.ChangeDefaultBuilding(building, town);
					TraceLogger.Write("HomesteadSettlementBuilder", "RestoreDefaultBuilding: '" + s.StringId + "' daily default restored to '" + defId + "'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreDefaultBuilding failed for '" + s?.StringId + "': " + ex.Message);
		}
	}

	private static void RestoreExtendedState(Settlement s)
	{
		HomesteadBuiltSettlement homesteadBuiltSettlement = HomesteadSettlementBehavior.Instance?.GetRecord(s.StringId);
		if (homesteadBuiltSettlement == null)
		{
			return;
		}
		Town town = s.Town;
		if (town != null)
		{
			try
			{
				if (homesteadBuiltSettlement.FoodStocks > 0f)
				{
					town.FoodStocks = homesteadBuiltSettlement.FoodStocks;
				}
				else if (town.FoodStocks <= 0f)
				{
					town.FoodStocks = 1000f;
				}
				town.GarrisonAutoRecruitmentIsEnabled = !homesteadBuiltSettlement.GarrisonAutoRecruitDisabled;
				if (homesteadBuiltSettlement.GarrisonWagePaymentLimit > 0)
				{
					s.SetGarrisonWagePaymentLimit(homesteadBuiltSettlement.GarrisonWagePaymentLimit);
				}
				town.InRebelliousState = homesteadBuiltSettlement.InRebelliousState;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "RestoreExtendedState town fields '" + s.StringId + "': " + ex.Message);
			}
		}
		try
		{
			if (homesteadBuiltSettlement.SettlementHitPoints > 0f && SettlementHitPointsSetter != null)
			{
				SettlementHitPointsSetter.Invoke(s, new object[1] { homesteadBuiltSettlement.SettlementHitPoints });
			}
			if (homesteadBuiltSettlement.WallSectionHealth != null)
			{
				int num = Math.Min(homesteadBuiltSettlement.WallSectionHealth.Count, s.SettlementWallSectionHitPointsRatioList?.Count ?? 0);
				for (int i = 0; i < num; i++)
				{
					s.SetWallSectionHitPointsRatioAtIndex(i, homesteadBuiltSettlement.WallSectionHealth[i]);
				}
			}
			if (homesteadBuiltSettlement.HasVisited)
			{
				s.HasVisited = true;
			}
			if (homesteadBuiltSettlement.BribePaid > 0)
			{
				s.BribePaid = homesteadBuiltSettlement.BribePaid;
			}
			if (s.Village != null && homesteadBuiltSettlement.VillageTradeTax > 0)
			{
				s.Village.TradeTaxAccumulated = homesteadBuiltSettlement.VillageTradeTax;
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreExtendedState settlement fields '" + s.StringId + "': " + ex2.Message);
		}
		try
		{
			if (homesteadBuiltSettlement.StashItems == null || homesteadBuiltSettlement.StashItems.Count <= 0 || s.Stash == null || s.Stash.Count != 0)
			{
				return;
			}
			int num2 = 0;
			foreach (string stashItem in homesteadBuiltSettlement.StashItems)
			{
				string[] array = stashItem.Split(new char[1] { '|' });
				if (array.Length == 3 && int.TryParse(array[2], out var result) && result > 0)
				{
					ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(array[0]);
					if (itemObject != null)
					{
						ItemModifier itemModifier = (string.IsNullOrEmpty(array[1]) ? null : MBObjectManager.Instance.GetObject<ItemModifier>(array[1]));
						s.Stash.AddToCounts(new EquipmentElement(itemObject, itemModifier), result);
						num2 += result;
					}
				}
			}
			if (num2 > 0)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreExtendedState: '{s.StringId}' stash restored ({num2} items).");
			}
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreExtendedState stash '" + s.StringId + "': " + ex3.Message);
		}
	}

	private static void RestoreHearth(Village village, Settlement s)
	{
		try
		{
			float num = HomesteadSettlementBehavior.Instance?.GetStoredHearth(s.StringId) ?? 0f;
			if (!(num <= 0f) && !(Math.Abs(village.Hearth - num) < 0.01f))
			{
				float hearth = village.Hearth;
				village.Hearth = num;
				TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreHearth: '{s.StringId}' hearth restored from {hearth:0.#} to {num:0.#}.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreHearth failed for '" + s?.StringId + "': " + ex.Message);
		}
	}

	public static void RestoreMilitiasOnSessionLaunched()
	{
		try
		{
			foreach (MobileParty item in MobileParty.All.ToList())
			{
				if (!item.IsMilitia)
				{
					continue;
				}
				MilitiaPartyComponent militiaPartyComponent = item.PartyComponent as MilitiaPartyComponent;
				Settlement settlement = militiaPartyComponent?.Settlement;
				if (settlement != null && settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_") && settlement.MilitiaPartyComponent != militiaPartyComponent)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "RestoreMilitiasOnSessionLaunched: destroying orphaned duplicate militia party '" + item.StringId + "' of '" + settlement.StringId + "'.");
					try
					{
						DestroyPartyAction.Apply(null, item);
					}
					catch (Exception ex)
					{
						TraceLogger.Write("HomesteadSettlementBuilder", "Failed to destroy duplicate militia '" + item.StringId + "': " + ex.Message);
					}
				}
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "Duplicate-militia cleanup failed: " + ex2.Message);
		}
		foreach (Settlement item2 in Settlement.All.Where((Settlement s) => s != null && s.StringId != null && s.StringId.StartsWith("hsr_settlement_")).ToList())
		{
			try
			{
				float num = HomesteadSettlementBehavior.Instance?.GetStoredMilitia(item2.StringId) ?? 0f;
				if (!(num <= 0f) && !(item2.Militia >= num - 0.5f))
				{
					float militia = item2.Militia;
					item2.Militia = num;
					TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreMilitiasOnSessionLaunched: '{item2.StringId}' militia restored from {militia:0.#} to {num:0.#} (party attached={item2.MilitiaPartyComponent != null}).");
				}
			}
			catch (Exception ex3)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "RestoreMilitiasOnSessionLaunched failed for '" + item2?.StringId + "': " + ex3.Message);
			}
		}
	}

	private static void RestoreProsperity(Town town, Settlement s)
	{
		try
		{
			float num = HomesteadSettlementBehavior.Instance?.GetStoredProsperity(s.StringId) ?? (-1f);
			if (!(num <= 0f) && !(Math.Abs(town.Prosperity - num) < 0.01f))
			{
				town.Prosperity = num;
				TraceLogger.Write("HomesteadSettlementBuilder", $"RestoreProsperity: '{s.StringId}' prosperity restored to {num:0.#}.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RestoreProsperity failed for '" + s?.StringId + "': " + ex.Message);
		}
	}

	private static void ReconcileBuildingsInProgress(Town town)
	{
		try
		{
			if (town?.BuildingsInProgress == null || town.BuildingsInProgress.Count == 0 || town.Buildings == null)
			{
				return;
			}
			bool flag = false;
			List<Building> list = new List<Building>();
			foreach (Building queued in town.BuildingsInProgress)
			{
				if (queued == null)
				{
					continue;
				}
				if (town.Buildings.Any((Building b) => b == queued))
				{
					list.Add(queued);
					continue;
				}
				Building building = town.Buildings.FirstOrDefault((Building b) => b.BuildingType?.StringId == queued.BuildingType?.StringId);
				if (building != null)
				{
					building.BuildingProgress = queued.BuildingProgress;
					building.CurrentLevel = queued.CurrentLevel;
					building.IsCurrentlyDefault = queued.IsCurrentlyDefault;
					list.Add(building);
					flag = true;
					TraceLogger.Write("HomesteadSettlementBuilder", "ReconcileBuildingsInProgress: '" + town.Settlement?.StringId + "' re-pointed queued '" + queued.BuildingType?.StringId + "' " + $"onto the canonical Buildings-list object (progress={queued.BuildingProgress}, level={queued.CurrentLevel}).");
				}
				else
				{
					list.Add(queued);
				}
			}
			if (flag)
			{
				town.BuildingsInProgress = new Queue<Building>(list);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ReconcileBuildingsInProgress failed: " + ex.Message);
		}
	}

	private static string InjectPortLocation(string xml)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			XmlElement xmlElement = xmlDocument.GetElementsByTagName("Locations").Cast<XmlElement>().FirstOrDefault();
			if (xmlElement != null && xmlElement.SelectSingleNode("Location[@id='port']") == null)
			{
				XmlElement xmlElement2 = xmlDocument.CreateElement("Location");
				xmlElement2.SetAttribute("id", "port");
				xmlElement2.SetAttribute("scene_name", "empire_shipyard");
				xmlElement.AppendChild(xmlElement2);
			}
			return xmlDocument.OuterXml;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "InjectPortLocation failed: " + ex.Message);
			return xml;
		}
	}

	private static void EnsureShipyardBuilding(Settlement s)
	{
		try
		{
			if (s?.Town != null && s.HasPort && !s.Town.Buildings.Any((Building b) => b.BuildingType?.StringId == "building_shipyard"))
			{
				BuildingType buildingType = BuildingType.All.FirstOrDefault((BuildingType x) => x?.StringId == "building_shipyard");
				if (buildingType != null)
				{
					s.Town.Buildings.Add(new Building(buildingType, s.Town, 0f, 1));
					TraceLogger.Write("HomesteadSettlementBuilder", "EnsureShipyardBuilding: added building_shipyard to '" + s.StringId + "'.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "EnsureShipyardBuilding: " + ex.Message);
		}
	}

	private static Clan ResolveOwnerClan(string ownerId)
	{
		if (string.IsNullOrEmpty(ownerId))
		{
			return Clan.PlayerClan;
		}
		int num = ownerId.IndexOf('.');
		string id = ((num >= 0) ? ownerId.Substring(num + 1) : ownerId);
		return Clan.All.FirstOrDefault((Clan c) => c.StringId == id) ?? Clan.PlayerClan;
	}

	public static Settlement? RegisterSettlementXml(string xml, string stringId)
	{
		if (xml.Contains("port_posX") && !xml.Contains("port_navmesh"))
		{
			xml = Regex.Replace(xml, "\\s*port_posX=\"[^\"]*\"", "");
			xml = Regex.Replace(xml, "\\s*port_posY=\"[^\"]*\"", "");
		}
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		foreach (XmlNode item in xmlDocument.ChildNodes.Cast<XmlNode>().ToList())
		{
			if (item.NodeType == XmlNodeType.Comment)
			{
				xmlDocument.RemoveChild(item);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			stringBuilder.Append(childNode.Name).Append(' ');
		}
		int count = MBObjectManager.Instance.GetObjectTypeList<Settlement>().Count;
		FieldInfo fieldInfo = AccessTools.Field(typeof(Campaign), "_gameLoadingType");
		object value = fieldInfo.GetValue(Campaign.Current);
		try
		{
			fieldInfo.SetValue(Campaign.Current, Campaign.GameLoadingType.NewCampaign);
			MBObjectManager.Instance.LoadXml(xmlDocument);
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", $"RegisterSettlementXml: LoadXml threw for '{stringId}': {arg}");
			throw;
		}
		finally
		{
			fieldInfo.SetValue(Campaign.Current, value);
		}
		MBReadOnlyList<Settlement> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
		bool flag = objectTypeList.Any((Settlement s) => s.StringId == stringId);
		TraceLogger.Write("HomesteadSettlementBuilder", $"RegisterSettlementXml '{stringId}': rootNodes=[{stringBuilder.ToString().Trim()}] typeListCount {count}->{objectTypeList.Count} inList={flag}");
		if (!flag)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "RegisterSettlementXml '" + stringId + "': NOT in Settlement type list after LoadXml — registration failed (no matching <Settlements> dispatch or deserialize produced only a presumed object).");
			return null;
		}
		return MBObjectManager.Instance.GetObject<Settlement>(stringId);
	}

	private static void InitTownSettlement(Settlement s, Clan owner)
	{
		try
		{
			s.Town.OwnerClan = owner;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "OwnerClan: " + ex.Message);
		}
		try
		{
			s.Party.SetLevelMaskIsDirty();
		}
		catch
		{
		}
		s.IsVisible = true;
		s.IsInspected = true;
		try
		{
			s.Town.FoodStocks = 1000f;
		}
		catch
		{
		}
		try
		{
			s.Party.SetVisualAsDirty();
		}
		catch
		{
		}
		TryAddSettlementVisual(s);
		try
		{
			s.OnGameCreated();
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "OnGameCreated: " + ex2.Message);
		}
		try
		{
			s.AfterInitialized();
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "AfterInitialized: " + ex3.Message);
		}
		try
		{
			s.OnFinishLoadState();
		}
		catch (Exception ex4)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "OnFinishLoadState: " + ex4.Message);
		}
		InitTownBuildings(s);
		SeedMarket(s.Town);
		InjectIntoCraftingCampaignBehavior(s.Town);
	}

	private static void InjectIntoCraftingCampaignBehavior(Town town)
	{
		try
		{
			if (town == null)
			{
				return;
			}
			CraftingCampaignBehavior craftingCampaignBehavior = Campaign.Current?.GetCampaignBehavior<CraftingCampaignBehavior>();
			if (craftingCampaignBehavior == null)
			{
				return;
			}
			FieldInfo field = typeof(CraftingCampaignBehavior).GetField("_craftingOrders", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null && field.GetValue(craftingCampaignBehavior) is IDictionary dictionary && !dictionary.Contains(town))
			{
				Type type = typeof(CraftingCampaignBehavior).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((Type t) => t.Name == "CraftingOrderSlots");
				if (type != null)
				{
					object value = Activator.CreateInstance(type);
					dictionary.Add(town, value);
					TraceLogger.Write("HomesteadSettlementBuilder", $"InjectIntoCraftingCampaignBehavior: Injected '{town.Name}' into _craftingOrders.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "InjectIntoCraftingCampaignBehavior: " + ex.Message);
		}
	}

	private static void SeedMarket(Town town)
	{
		try
		{
			if (town?.MarketData == null)
			{
				return;
			}
			foreach (ItemCategory item in ItemCategories.All)
			{
				if (item != null && item.IsValid)
				{
					town.MarketData.AddDemand(item, 3f);
					town.MarketData.AddSupply(item, 2f);
				}
			}
			town.MarketData.UpdateStores();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "SeedMarket: " + ex.Message);
		}
	}

	public static void SeedWorkshops(Town town)
	{
		List<WorkshopType> shuffled;
		try
		{
			if (town == null)
			{
				return;
			}
			if (town.Workshops == null || town.Workshops.Length == 0)
			{
				try
				{
					town.InitializeWorkshops(3);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "SeedWorkshops InitializeWorkshops: " + ex.Message);
				}
			}
			if (town.Workshops == null || town.Workshops.Length == 0)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "SeedWorkshops: no workshop slots after InitializeWorkshops - skipping.");
				return;
			}
			List<WorkshopType> list = (from wt in MBObjectManager.Instance.GetObjectTypeList<WorkshopType>()
				where wt != null && !wt.IsHidden && wt.StringId != "crew"
				select wt).ToList();
			if (list.Count == 0)
			{
				return;
			}
			List<Hero> list2 = town.Settlement.Notables.Where((Hero n) => n != null && !n.IsDead && !n.IsDisabled).ToList();
			int count = list2.Count;
			int count2 = list.Count;
			if (count < 3)
			{
				try
				{
					int num = 3 - count;
					for (int num2 = 0; num2 < num; num2++)
					{
						Occupation occupation = ((num2 % 2 == 0) ? Occupation.Artisan : Occupation.Merchant);
						CharacterObject characterObject = CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == occupation && c.Culture == town.Culture) ?? CharacterObject.All.FirstOrDefault((CharacterObject c) => c.IsTemplate && c.Occupation == occupation);
						if (characterObject != null)
						{
							Hero hero = HeroCreator.CreateSpecialHero(characterObject, town.Settlement);
							if (hero != null)
							{
								Utils.ApplyRandomPersonalityTraits(hero);
								EnterSettlementAction.ApplyForCharacterOnly(hero, town.Settlement);
								list2.Add(hero);
							}
						}
					}
					count = list2.Count;
				}
				catch (Exception ex2)
				{
					TraceLogger.Write("HomesteadSettlementBuilder", "SeedWorkshops spawn notables: " + ex2.Message);
				}
			}
			if (count == 0)
			{
				TraceLogger.Write("HomesteadSettlementBuilder", "SeedWorkshops: no notables available to own workshops.");
				return;
			}
			Random rng = new Random();
			shuffled = list.OrderBy((WorkshopType _) => rng.Next()).ToList();
			Pin("brewery", 0);
			Pin("weaponsmith", 1);
			Pin("smithy", 2);
			WorkshopsCampaignBehavior workshopsCampaignBehavior = Campaign.Current?.GetCampaignBehavior<WorkshopsCampaignBehavior>();
			MethodInfo methodInfo = workshopsCampaignBehavior?.GetType().GetMethod("AddNewWorkshopData", BindingFlags.Instance | BindingFlags.NonPublic);
			int num3 = 0;
			for (int num4 = 0; num4 < 3 && num4 < town.Workshops.Length; num4++)
			{
				Workshop workshop = town.Workshops[num4];
				if (workshop != null && workshop.Owner == null)
				{
					workshop.InitializeWorkshop(list2[num4 % count], shuffled[num4 % count2]);
					workshop.ChangeGold(500);
					try
					{
						methodInfo?.Invoke(workshopsCampaignBehavior, new object[1] { workshop });
					}
					catch
					{
					}
					num3++;
				}
			}
			TraceLogger.Write("HomesteadSettlementBuilder", $"SeedWorkshops: seeded {num3} workshop(s) in '{town.Name}' (slots={town.Workshops.Length}).");
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "SeedWorkshops: " + ex3.Message);
		}
		void Pin(string id, int index)
		{
			WorkshopType workshopType = shuffled.FirstOrDefault((WorkshopType t) => t.StringId == id);
			if (workshopType != null)
			{
				shuffled.Remove(workshopType);
				shuffled.Insert(Math.Min(index, shuffled.Count), workshopType);
			}
		}
	}

	private static void ReconcileWorkshopSettlements(Town town, Settlement s)
	{
		try
		{
			if (town?.Workshops == null || WorkshopSettlementField == null)
			{
				return;
			}
			Workshop[] workshops = town.Workshops;
			foreach (Workshop workshop in workshops)
			{
				if (workshop != null && workshop.Settlement != s)
				{
					WorkshopSettlementField.SetValue(workshop, s);
					TraceLogger.Write("HomesteadSettlementBuilder", "ReconcileWorkshopSettlements: '" + s.StringId + "' workshop '" + workshop.WorkshopType?.StringId + "' re-pointed from stale settlement object to canonical.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "ReconcileWorkshopSettlements failed for '" + s?.StringId + "': " + ex.Message);
		}
	}

	private static void EnsureWeaponWorkshops(Town town)
	{
		try
		{
			if (town?.Workshops == null || town.Workshops.Length == 0)
			{
				return;
			}
			bool flag = town.Workshops.Any((Workshop w) => w?.WorkshopType?.StringId == "weaponsmith");
			bool flag2 = town.Workshops.Any((Workshop w) => w?.WorkshopType?.StringId == "smithy");
			if (flag && flag2)
			{
				return;
			}
			List<Workshop> list = town.Workshops.Where((Workshop w) => w?.WorkshopType != null && w.Owner != null && w.Owner != Hero.MainHero && w.WorkshopType.StringId != "brewery" && w.WorkshopType.StringId != "weaponsmith" && w.WorkshopType.StringId != "smithy").ToList();
			string[] array = new string[2] { "weaponsmith", "smithy" };
			foreach (string text in array)
			{
				if ((text == "weaponsmith" && flag) || (text == "smithy" && flag2))
				{
					continue;
				}
				Workshop workshop = list.FirstOrDefault();
				if (workshop == null)
				{
					break;
				}
				list.Remove(workshop);
				WorkshopType workshopType = MBObjectManager.Instance.GetObject<WorkshopType>(text);
				if (workshopType != null)
				{
					string stringId = workshop.WorkshopType.StringId;
					workshop.InitializeWorkshop(workshop.Owner, workshopType);
					if (workshop.Capital < 500)
					{
						workshop.ChangeGold(500 - workshop.Capital);
					}
					TraceLogger.Write("HomesteadSettlementBuilder", "EnsureWeaponWorkshops: '" + town.Settlement?.StringId + "' converted '" + stringId + "' -> '" + text + "' (owner kept).");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "EnsureWeaponWorkshops failed for '" + town?.Settlement?.StringId + "': " + ex.Message);
		}
	}

	public static string TownNameFor(Homestead homestead)
	{
		return homestead?.Name?.ToString() ?? "Settlement";
	}

	public static string CastleNameFor(Homestead homestead)
	{
		TextObject textObject = new TextObject("{=hsr_castle_name}Castle {NAME}");
		textObject.SetTextVariable("NAME", homestead?.Name ?? new TextObject(""));
		return textObject.ToString();
	}

	public static string VillageNameFor(Hero apprentice)
	{
		TextObject textObject = new TextObject(VillageNamePatterns[MBRandom.RandomInt(VillageNamePatterns.Length)]);
		textObject.SetTextVariable("NAME", apprentice?.FirstName ?? apprentice?.Name ?? new TextObject("Settler"));
		return textObject.ToString();
	}

	private static void InitTownBuildings(Settlement s)
	{
		try
		{
			Town town = s.Town;
			bool isCastle = s.IsCastle;
			TraceLogger.Write("HomesteadSettlementBuilder", $"DIAG InitTownBuildings ENTRY '{s.StringId}': town.Buildings.Count={town.Buildings.Count}, " + "daily-project entries=[" + string.Join(", ", from b in town.Buildings
				where b.BuildingType?.IsDailyProject ?? false
				select $"{b.BuildingType?.StringId}:default={b.IsCurrentlyDefault}") + "], CurrentDefaultBuilding(pre)=" + (town.CurrentDefaultBuilding?.BuildingType?.StringId ?? "null") + ".");
			foreach (BuildingType bt in BuildingType.All)
			{
				if (bt != null && !bt.IsDailyProject && !town.Buildings.Any((Building b) => b.BuildingType.StringId == bt.StringId) && (isCastle ? bt.StringId.StartsWith("building_castle") : bt.StringId.StartsWith("building_settlement")))
				{
					town.Buildings.Add(new Building(bt, town, 0f, 1));
				}
			}
			BuildingType[] array = ((!isCastle) ? new BuildingType[4]
			{
				DefaultBuildingTypes.SettlementDailyHousing,
				DefaultBuildingTypes.SettlementDailyTrainMilitia,
				DefaultBuildingTypes.SettlementDailyFestivalAndGames,
				DefaultBuildingTypes.SettlementDailyIrrigation
			} : new BuildingType[4]
			{
				DefaultBuildingTypes.CastleDailySlackenGarrison,
				DefaultBuildingTypes.CastleDailyRaiseTroops,
				DefaultBuildingTypes.CastleDailyDrills,
				DefaultBuildingTypes.CastleDailyIrrigation
			});
			foreach (BuildingType bt2 in array)
			{
				if (bt2 != null && !town.Buildings.Any((Building b) => b.BuildingType.StringId == bt2.StringId))
				{
					town.Buildings.Add(new Building(bt2, town, 0f, 1));
				}
			}
			if (town.CurrentDefaultBuilding == null)
			{
				Building building = town.Buildings.FirstOrDefault((Building b) => b.BuildingType.IsDailyProject);
				if (building != null)
				{
					BuildingHelper.ChangeDefaultBuilding(building, town);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "InitTownBuildings: " + ex.Message);
		}
		EnsureShipyardBuilding(s);
	}

	private static void TryAddSettlementVisual(Settlement s)
	{
		try
		{
			Type type = AppDomain.CurrentDomain.GetAssemblies().Select(delegate(Assembly a)
			{
				try
				{
					return a.GetType("SandBox.View.Map.Managers.SettlementVisualManager");
				}
				catch
				{
					return (Type)null;
				}
			}).FirstOrDefault((Type x) => x != null) ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(delegate(Assembly a)
			{
				try
				{
					return a.GetTypes();
				}
				catch
				{
					return Array.Empty<Type>();
				}
			}).FirstOrDefault((Type x) => x.Name == "SettlementVisualManager");
			if (type == null)
			{
				return;
			}
			object obj = type.GetProperty("Current", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
			if (obj == null || s?.Party == null)
			{
				return;
			}
			MethodInfo method = type.GetMethod("AddNewPartyVisualForParty", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[1] { s.Party.GetType() }, null);
			if (method != null)
			{
				method.Invoke(obj, new object[1] { s.Party });
				return;
			}
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!(methodInfo.Name != "AddNewPartyVisualForParty"))
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(s.Party))
					{
						methodInfo.Invoke(obj, new object[1] { s.Party });
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSettlementBuilder", "TryAddSettlementVisual: " + ex.Message);
		}
	}

	private static string? ResolveTemplateFile(string fileName)
	{
		try
		{
			string directoryName = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string text = ((directoryName == null) ? null : Directory.GetParent(directoryName)?.FullName);
			string text2 = ((text == null) ? null : Directory.GetParent(text)?.FullName);
			if (text2 == null)
			{
				return null;
			}
			string text3 = System.IO.Path.Combine(text2, "ModuleData", "HomesteadSettlementTemplates", fileName);
			return File.Exists(text3) ? text3 : null;
		}
		catch
		{
			return null;
		}
	}
}
