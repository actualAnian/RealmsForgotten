using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SOTOR.RFIntegration;
using TaleWorlds.Core;

namespace SOTOR.MagicAccessories;

public static class MagicRuneRegistry
{
	private const string FileName = "rf_magic_runes.xml";

	private static readonly Dictionary<string, MagicRuneData> _byItemId =
		new Dictionary<string, MagicRuneData>(StringComparer.OrdinalIgnoreCase);

	private static bool _loaded;

	public static void Load()
	{
		_loaded = true;
		_byItemId.Clear();

		string path;
		try
		{
			path = RFModulePath.Combine("ModuleData", FileName);
		}
		catch (Exception ex)
		{
			SotorLog.Error("MagicRuneRegistry: could not resolve module path: " + ex.Message);
			return;
		}

		if (!File.Exists(path))
		{
			SotorLog.Warn("MagicRuneRegistry: '" + path + "' not found; rune sockets stay disabled.");
			return;
		}

		try
		{
			XmlDocument document = new XmlDocument();
			document.Load(path);
			XmlNodeList nodes = document.SelectNodes("//Rune");
			if (nodes == null)
			{
				return;
			}

			int skipped = 0;
			foreach (XmlNode node in nodes)
			{
				MagicRuneData rune = Parse(node);
				if (rune == null)
				{
					skipped++;
					continue;
				}
				_byItemId[rune.ItemId] = rune;
			}

			SotorLog.Info($"MagicRuneRegistry: loaded {_byItemId.Count} runes from {FileName} (skipped {skipped}).");
		}
		catch (Exception ex)
		{
			_byItemId.Clear();
			SotorLog.Error("MagicRuneRegistry: failed to parse " + FileName + ": " + ex.Message);
		}
	}

	public static bool TryGet(string itemId, out MagicRuneData rune)
	{
		rune = null;
		if (string.IsNullOrEmpty(itemId))
		{
			return false;
		}
		if (!_loaded)
		{
			Load();
		}
		return _byItemId.TryGetValue(itemId, out rune);
	}

	public static IEnumerable<MagicRuneData> All
	{
		get
		{
			if (!_loaded)
			{
				Load();
			}
			return _byItemId.Values;
		}
	}

	public static bool CanApply(MagicRuneData rune, ItemObject targetItem)
	{
		return rune != null && (rune.Targets & GetTarget(targetItem)) != 0;
	}

	public static MagicRuneTarget GetTarget(ItemObject item)
	{
		if (item == null)
		{
			return MagicRuneTarget.None;
		}
		if (ArcaneFocusRegistry.TryGetFocus(item.StringId, out _))
		{
			return MagicRuneTarget.MagicFocus;
		}

		return item.Type switch
		{
			ItemObject.ItemTypeEnum.OneHandedWeapon => MagicRuneTarget.Melee,
			ItemObject.ItemTypeEnum.TwoHandedWeapon => MagicRuneTarget.Melee,
			ItemObject.ItemTypeEnum.Polearm => MagicRuneTarget.Melee,
			ItemObject.ItemTypeEnum.Shield => MagicRuneTarget.Shield,
			ItemObject.ItemTypeEnum.Bow => MagicRuneTarget.Bow,
			ItemObject.ItemTypeEnum.Crossbow => MagicRuneTarget.Bow,
			ItemObject.ItemTypeEnum.Sling => MagicRuneTarget.Bow,
			ItemObject.ItemTypeEnum.Thrown => MagicRuneTarget.Thrown,
			ItemObject.ItemTypeEnum.Arrows => MagicRuneTarget.Ammunition,
			ItemObject.ItemTypeEnum.Bolts => MagicRuneTarget.Ammunition,
			ItemObject.ItemTypeEnum.SlingStones => MagicRuneTarget.Ammunition,
			ItemObject.ItemTypeEnum.Bullets => MagicRuneTarget.Ammunition,
			_ => MagicRuneTarget.None
		};
	}

	private static MagicRuneData Parse(XmlNode node)
	{
		string itemId = GetAttribute(node, "itemId")?.Trim();
		if (string.IsNullOrEmpty(itemId))
		{
			SotorLog.Warn("MagicRuneRegistry: <Rune> without itemId ignored.");
			return null;
		}

		MagicRuneTarget targets = ParseTargets(GetAttribute(node, "targets"));
		if (targets == MagicRuneTarget.None)
		{
			SotorLog.Warn("MagicRuneRegistry: '" + itemId + "' has no valid targets.");
			return null;
		}

		return new MagicRuneData(
			itemId,
			targets,
			GetAttribute(node, "name"),
			GetAttribute(node, "description"),
			ParseEnum(GetAttribute(node, "tier"), MagicRuneTier.Lesser),
			ParseEnum(GetAttribute(node, "effect"), MagicRuneEffect.None),
			ParseFloat(GetAttribute(node, "primary"), 0f),
			ParseFloat(GetAttribute(node, "secondary"), 0f),
			ParseFloat(GetAttribute(node, "maxWindsBonus"), 0f),
			ParsePositiveMultiplier(GetAttribute(node, "rechargeMult")),
			ParsePositiveMultiplier(GetAttribute(node, "effectivenessMult")),
			ParsePositiveMultiplier(GetAttribute(node, "windsCostMult")),
			ParsePositiveMultiplier(GetAttribute(node, "cooldownMult")));
	}

	private static T ParseEnum<T>(string raw, T defaultValue) where T : struct
	{
		return !string.IsNullOrWhiteSpace(raw) && Enum.TryParse(raw.Trim(), true, out T parsed)
			? parsed
			: defaultValue;
	}

	private static MagicRuneTarget ParseTargets(string raw)
	{
		MagicRuneTarget result = MagicRuneTarget.None;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return result;
		}

		foreach (string part in raw.Split('|', ',', ';'))
		{
			if (Enum.TryParse(part.Trim(), true, out MagicRuneTarget parsed))
			{
				result |= parsed;
			}
		}
		return result;
	}

	private static string GetAttribute(XmlNode node, string name)
	{
		return node?.Attributes?[name]?.Value;
	}

	private static float ParsePositiveMultiplier(string raw)
	{
		float value = ParseFloat(raw, 1f);
		return value > 0f ? value : 1f;
	}

	private static float ParseFloat(string raw, float defaultValue)
	{
		if (!string.IsNullOrWhiteSpace(raw) &&
			float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
		{
			return value;
		}
		return defaultValue;
	}
}
