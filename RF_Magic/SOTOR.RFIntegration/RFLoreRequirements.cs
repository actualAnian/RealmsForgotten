using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SOTOR.AbilitySystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;

namespace SOTOR.RFIntegration;

/// <summary>
/// Requisito de ARCANE para aprender cada escola de magia (lido de
/// <c>rf_lore_requirements.xml</c>).
///
/// Regra de design do RF: **magia não se compra, se merece**. No SOTOR original
/// só três escolas pediam algo além de ouro (nível de conjurador para
/// Necromancia e as duas restritas) — as outras oito bastava ter a bolsa cheia.
/// Aqui cada escola ganha um piso de Arcane; o ouro continua valendo, mas
/// sozinho não abre mais nada.
///
/// Isto SOMA aos gates que já existiam, não os substitui: o nível de conjurador
/// (perks) segue exigido onde estava, e o tier de cada feitiço continua limitado
/// pelo nível de conjurador do mago.
/// </summary>
public static class RFLoreRequirements
{
	private const string FileName = "rf_lore_requirements.xml";

	private static readonly Dictionary<string, int> _byLoreId = new Dictionary<string, int>();

	private static bool _loaded;

	/// <summary>Nenhum requisito carregado → fail-open (comportamento SOTOR puro).</summary>
	public static bool IsEmpty => !_loaded || _byLoreId.Count == 0;

	public static void Load()
	{
		_loaded = true;
		_byLoreId.Clear();

		string path;
		try
		{
			path = RFModulePath.Combine("ModuleData", FileName);
		}
		catch (Exception ex)
		{
			SotorLog.Error("RFLoreRequirements: could not resolve module path: " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		if (!File.Exists(path))
		{
			SotorLog.Warn("RFLoreRequirements: '" + path + "' not found — lores stay gold-only (pure SOTOR behaviour).");
			return;
		}

		int skipped = 0;
		try
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			XmlNodeList nodes = doc.SelectNodes("//Lore");
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					string id = node.Attributes?["id"]?.Value;
					string raw = node.Attributes?["arcane"]?.Value;
					if (string.IsNullOrWhiteSpace(id) || !int.TryParse(raw, out int arcane) || arcane < 0)
					{
						skipped++;
						continue;
					}
					_byLoreId[id] = arcane;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Error("RFLoreRequirements: failed to parse " + FileName + ": " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		SotorLog.Info($"RFLoreRequirements: loaded {_byLoreId.Count} lore requirement(s) from {FileName} (skipped {skipped} malformed).");
	}

	/// <summary>Arcane mínimo da escola; 0 = sem requisito.</summary>
	public static int GetRequiredArcane(string loreId)
	{
		if (string.IsNullOrEmpty(loreId) || IsEmpty)
		{
			return 0;
		}
		return _byLoreId.TryGetValue(loreId, out int value) ? value : 0;
	}

	/// <summary>Nível atual do herói na skill de magia ativa (Arcane).</summary>
	public static int GetArcaneValue(Hero hero)
	{
		SkillObject skill = SotorSkills.Spellcraft;
		if (hero == null || skill == null)
		{
			return 0;
		}
		return hero.GetSkillValue(skill);
	}

	/// <summary>
	/// Piso de Arcane desta escola PARA ESTE HEROI — o valor do XML, mais a
	/// penalidade quando a escola e estranha a cultura dele. E aqui que os dois
	/// sistemas se juntam: o XML diz o quanto a escola e difícil, a cultura diz o
	/// quanto ela e estranha a tradicao do conjurador.
	/// </summary>
	public static int GetRequiredArcaneForHero(Hero hero, string loreId)
	{
		int required = GetRequiredArcane(loreId);
		if (required <= 0)
		{
			return 0;
		}
		return RFCultureLores.AdjustArcaneForHero(hero, loreId, required);
	}

	/// <summary>O herói tem Arcane suficiente para esta escola (já com a cultura)?</summary>
	public static bool MeetsArcaneForLore(Hero hero, string loreId)
	{
		int required = GetRequiredArcaneForHero(hero, loreId);
		if (required <= 0)
		{
			return true;
		}
		return GetArcaneValue(hero) >= required;
	}
}
