using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using SOTOR.RFIntegration;

namespace SOTOR.MagicAccessories;

public static class MagicAccessoryRegistry
{
	private const string FileName = "rf_magic_accessories.xml";

	private static readonly Dictionary<string, MagicAccessoryData> _byItemId =
		new Dictionary<string, MagicAccessoryData>(StringComparer.OrdinalIgnoreCase);

	private static bool _loaded;

	public static int Count => _byItemId.Count;

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
			SotorLog.Error("MagicAccessoryRegistry: could not resolve module path: " + ex.Message);
			return;
		}

		if (!File.Exists(path))
		{
			SotorLog.Warn("MagicAccessoryRegistry: '" + path + "' not found; accessory bonuses stay disabled.");
			return;
		}

		try
		{
			XmlDocument document = new XmlDocument();
			document.Load(path);
			XmlNodeList nodes = document.SelectNodes("//Accessory");
			if (nodes == null)
			{
				return;
			}

			int skipped = 0;
			foreach (XmlNode node in nodes)
			{
				MagicAccessoryData accessory = Parse(node);
				if (accessory == null)
				{
					skipped++;
					continue;
				}

				if (_byItemId.ContainsKey(accessory.ItemId))
				{
					SotorLog.Warn("MagicAccessoryRegistry: duplicate itemId '" + accessory.ItemId + "'; later entry wins.");
				}
				_byItemId[accessory.ItemId] = accessory;
			}

			SotorLog.Info($"MagicAccessoryRegistry: loaded {_byItemId.Count} accessories from {FileName} (skipped {skipped}).");
		}
		catch (Exception ex)
		{
			_byItemId.Clear();
			SotorLog.Error("MagicAccessoryRegistry: failed to parse " + FileName + ": " + ex.Message);
		}
	}

	public static bool TryGet(string itemId, out MagicAccessoryData accessory)
	{
		accessory = null;
		if (string.IsNullOrEmpty(itemId))
		{
			return false;
		}

		if (!_loaded)
		{
			Load();
		}

		return _byItemId.TryGetValue(itemId, out accessory);
	}

	private static MagicAccessoryData Parse(XmlNode node)
	{
		string itemId = GetAttribute(node, "itemId")?.Trim();
		if (string.IsNullOrEmpty(itemId))
		{
			SotorLog.Warn("MagicAccessoryRegistry: <Accessory> without itemId ignored.");
			return null;
		}

		if (!Enum.TryParse(GetAttribute(node, "slot"), ignoreCase: true, out MagicAccessorySlot slot))
		{
			SotorLog.Warn("MagicAccessoryRegistry: '" + itemId + "' has invalid slot; use Ring or Necklace.");
			return null;
		}

		return new MagicAccessoryData(
			itemId,
			slot,
			ParseFloat(GetAttribute(node, "maxWindsBonus"), 0f),
			ParsePositiveMultiplier(GetAttribute(node, "rechargeMult")),
			ParsePositiveMultiplier(GetAttribute(node, "effectivenessMult")),
			ParsePositiveMultiplier(GetAttribute(node, "windsCostMult")),
			ParsePositiveMultiplier(GetAttribute(node, "cooldownMult")));
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
