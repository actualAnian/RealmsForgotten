using System;
using System.IO;
using TaleWorlds.ModuleManager;

namespace SOTOR;

public static class SotorSettings
{
	public static bool UseThrownAmberSpear = true;

	public static bool EnableSkeletonArmies = true;

	public static bool EnableMindControlledArmies = true;

	public static bool EnableCompanionSpellcasters = false;

	public static string SpellcraftAttributeId = "intelligence";

	public static int HudMode = 0;

	public static bool EnableSpellDamageLog = true;

	public static bool EnableWindsOnMagicKill = false;

	public static float WindsOnMagicKillAmount = 0f;

	public static bool EnableArmorWomRechargeTweak = false;

	public static float ArmorWomRechargeEffectPercent = 0f;

	public static bool EnableSpellEffectivenessTweak = false;

	public static float SpellEffectivenessBonusPercent = 0f;

	public static bool DisableMagicInSieges = false;

	public static bool EnableSpellShipDamage = true;

	public static float SpellShipDamagePercent = 100f;

	public static bool EnableBurningDeckDamage = true;

	public static float BurningDeckDamagePerSecond = 4f;

	public static bool EnableAbandonShipAI = true;

	public static int JavelinAttackStateTest = -1;

	private const string FileName = "sotor_settings.txt";

	private static bool _loaded;

	public static float ArmorWomRechargeEffectMultiplier
	{
		get
		{
			if (!EnableArmorWomRechargeTweak)
			{
				return 1f;
			}
			return 1f + ArmorWomRechargeEffectPercent / 100f;
		}
	}

	public static float SpellEffectivenessBonusFraction
	{
		get
		{
			if (!EnableSpellEffectivenessTweak)
			{
				return 0f;
			}
			return SpellEffectivenessBonusPercent / 100f;
		}
	}

	public static float SpellShipDamageMultiplier
	{
		get
		{
			if (!EnableSpellShipDamage)
			{
				return 0f;
			}
			return SpellShipDamagePercent / 100f;
		}
	}

	private static string SettingsPath()
	{
		try
		{
			return Path.Combine(SOTOR.RFIntegration.RFModulePath.Root, "ModuleData", "sotor_settings.txt"); // [RF-D] identidade: o modulo hospedeiro agora e RF_Magic, nao SOTOR
		}
		catch
		{
			return null;
		}
	}

	public static void Load()
	{
		if (_loaded)
		{
			return;
		}
		_loaded = true;
		try
		{
			string text = SettingsPath();
			if (text == null || !File.Exists(text))
			{
				SotorLog.Info($"SotorSettings: no settings file; using defaults (UseThrownAmberSpear={UseThrownAmberSpear}).");
				return;
			}
			string[] array = File.ReadAllLines(text);
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i]?.Trim();
				if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("#"))
				{
					int num = text2.IndexOf('=');
					if (num > 0)
					{
						string key = text2.Substring(0, num).Trim();
						string val = text2.Substring(num + 1).Trim();
						ApplyKey(key, val);
					}
				}
			}
			SotorLog.Info($"SotorSettings loaded: UseThrownAmberSpear={UseThrownAmberSpear}.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorSettings.Load failed (" + ex.GetType().Name + "): " + ex.Message + "; using defaults.");
		}
	}

	private static void ApplyKey(string key, string val)
	{
		bool result2;
		if (!(key == "UseThrownAmberSpear"))
		{
			if (key == "JavelinAttackStateTest" && int.TryParse(val, out var result))
			{
				JavelinAttackStateTest = result;
			}
		}
		else if (bool.TryParse(val, out result2))
		{
			UseThrownAmberSpear = result2;
		}
	}

	public static void Save()
	{
		try
		{
			string text = SettingsPath();
			if (text != null)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(text));
				File.WriteAllText(text, "# SOTOR settings. Edit values (true/false); one key=value per line.\n# UseThrownAmberSpear: false = spell bolt (v1), true = real thrown amber javelin (v2).\n" + $"UseThrownAmberSpear={UseThrownAmberSpear}\n");
				SotorLog.Info($"SotorSettings saved: UseThrownAmberSpear={UseThrownAmberSpear}.");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorSettings.Save failed: " + ex.Message);
		}
	}
}
