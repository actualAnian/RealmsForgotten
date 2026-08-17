using System.Collections.Generic;

namespace SOTOR.AbilitySystem;

public static class SotorLores
{
	public struct LoreDisplay
	{
		public string LoreId;

		public string Title;

		public string SymbolSprite;
	}

	public const string MinorMagic = "MinorMagic";

	public const string LoreOfFire = "LoreOfFire";

	public const string LoreOfHeavens = "LoreOfHeavens";

	public const string LoreOfLight = "LoreOfLight";

	public const string LoreOfDeath = "LoreOfDeath";

	public const string LoreOfNecromancy = "LoreOfNecromancy";

	public const string LoreOfBeasts = "LoreOfBeasts";

	public const string LoreOfLife = "LoreOfLife";

	public const string LoreOfMetal = "LoreOfMetal";

	public const string HighMagic = "HighMagic";

	public const string DarkMagic = "DarkMagic";

	public static readonly string[] ArchmageUnlockableLores = new string[2] { "HighMagic", "DarkMagic" };

	public static readonly string[] DefaultOwnedLores = new string[2] { "MinorMagic", "LoreOfFire" };

	public static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
	{
		["MinorMagic"] = 10000,
		["LoreOfFire"] = 100000,
		["LoreOfHeavens"] = 100000,
		["LoreOfLight"] = 100000,
		["LoreOfDeath"] = 100000,
		["LoreOfNecromancy"] = 150000,
		["LoreOfBeasts"] = 100000,
		["LoreOfLife"] = 100000,
		["LoreOfMetal"] = 100000,
		["HighMagic"] = 200000,
		["DarkMagic"] = 200000
	};

	public static readonly Dictionary<string, SpellCastingLevel> RequiredCasterLevel = new Dictionary<string, SpellCastingLevel>
	{
		["LoreOfNecromancy"] = SpellCastingLevel.Adept,
		["HighMagic"] = SpellCastingLevel.Archmage,
		["DarkMagic"] = SpellCastingLevel.Archmage
	};

	public static readonly string[] AllShownLores = new string[11]
	{
		"MinorMagic", "LoreOfLight", "LoreOfLife", "LoreOfHeavens", "LoreOfBeasts", "LoreOfFire", "LoreOfMetal", "LoreOfDeath", "LoreOfNecromancy", "DarkMagic",
		"HighMagic"
	};

	public static readonly HashSet<string> RightSideLores = new HashSet<string> { "LoreOfNecromancy", "HighMagic", "DarkMagic" };

	public static readonly Dictionary<string, LoreDisplay> Display = new Dictionary<string, LoreDisplay>
	{
		["MinorMagic"] = new LoreDisplay
		{
			LoreId = "MinorMagic",
			Title = "Thyrn-Kaeth",
			SymbolSprite = "minormagic_symbol"
		},
		["LoreOfFire"] = new LoreDisplay
		{
			LoreId = "LoreOfFire",
			Title = "Bael-Vornir",
			SymbolSprite = "firemagic_symbol"
		},
		["LoreOfHeavens"] = new LoreDisplay
		{
			LoreId = "LoreOfHeavens",
			Title = "Vel-Karûn",
			SymbolSprite = "celestial_symbol"
		},
		["LoreOfLight"] = new LoreDisplay
		{
			LoreId = "LoreOfLight",
			Title = "Aer-Lúthien",
			SymbolSprite = "lightmagic_symbol"
		},
		["LoreOfDeath"] = new LoreDisplay
		{
			LoreId = "LoreOfDeath",
			Title = "Morg-Syl",
			SymbolSprite = "deathmagic_symbol"
		},
		["LoreOfNecromancy"] = new LoreDisplay
		{
			LoreId = "LoreOfNecromancy",
			Title = "Bar-Gûlath",
			SymbolSprite = "necromancy_symbol"
		},
		["LoreOfBeasts"] = new LoreDisplay
		{
			LoreId = "LoreOfBeasts",
			Title = "Ulfr-Hâl",
			SymbolSprite = "beastmagic_symbol"
		},
		["LoreOfLife"] = new LoreDisplay
		{
			LoreId = "LoreOfLife",
			Title = "Edra-Sûl",
			SymbolSprite = "lifemagic_symbol"
		},
		["LoreOfMetal"] = new LoreDisplay
		{
			LoreId = "LoreOfMetal",
			Title = "Orand-Ûr",
			SymbolSprite = "metalmagic_symbol"
		},
		["HighMagic"] = new LoreDisplay
		{
			LoreId = "HighMagic",
			Title = "Elda-Sîrion",
			SymbolSprite = "highmagic_symbol"
		},
		["DarkMagic"] = new LoreDisplay
		{
			LoreId = "DarkMagic",
			Title = "Dwimm-Vaen",
			SymbolSprite = "darkmagic_symbol"
		}
	};

	public static SpellCastingLevel GetRequiredCasterLevel(string loreId)
	{
		if (!RequiredCasterLevel.TryGetValue(loreId, out var value))
		{
			return SpellCastingLevel.None;
		}
		return value;
	}

	public static bool IsRightSideLore(string loreId)
	{
		return RightSideLores.Contains(loreId);
	}

	public static int GetPrice(string loreId)
	{
		if (!Prices.TryGetValue(loreId, out var value))
		{
			return 0;
		}
		return value;
	}
}
