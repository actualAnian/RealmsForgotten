using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SOTOR.RFIntegration;

namespace SOTOR.MagicAccessories;

/// <summary>
/// Assigns runes to NPC and troop templates. A hero's saved sockets take priority;
/// this registry is the fallback used by ordinary troops and unsocketed NPC heroes.
/// </summary>
public static class MagicRuneLoadoutRegistry
{
	private const string FileName = "rf_magic_rune_loadouts.xml";
	private static readonly Dictionary<string, string[]> _loadouts =
		new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
	private static bool _loaded;

	public static void Load()
	{
		_loaded = true;
		_loadouts.Clear();
		string path = RFModulePath.Combine("ModuleData", FileName);
		if (!File.Exists(path))
		{
			SotorLog.Warn("MagicRuneLoadoutRegistry: '" + path + "' not found; NPC rune loadouts are disabled.");
			return;
		}

		try
		{
			XmlDocument document = new XmlDocument();
			document.Load(path);
			foreach (XmlNode node in document.SelectNodes("//Loadout"))
			{
				string characterId = node.Attributes?["characterId"]?.Value?.Trim();
				if (string.IsNullOrEmpty(characterId))
				{
					continue;
				}
				string[] slots = new string[MagicRuneService.WeaponSlotCount];
				for (int i = 0; i < slots.Length; i++)
				{
					slots[i] = node.Attributes?["slot" + i]?.Value?.Trim();
				}
				_loadouts[characterId] = slots;
			}
			SotorLog.Info($"MagicRuneLoadoutRegistry: loaded {_loadouts.Count} NPC/troop loadouts.");
		}
		catch (Exception ex)
		{
			_loadouts.Clear();
			SotorLog.Error("MagicRuneLoadoutRegistry: failed to parse " + FileName + ": " + ex.Message);
		}
	}

	public static string GetRuneItemId(string characterId, int slotIndex)
	{
		if (!_loaded)
		{
			Load();
		}
		if (string.IsNullOrEmpty(characterId) || slotIndex < 0 || slotIndex >= MagicRuneService.WeaponSlotCount ||
			!_loadouts.TryGetValue(characterId, out string[] slots))
		{
			return null;
		}
		return slots[slotIndex];
	}
}
