using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace SOTOR.RFIntegration;

/// <summary>
/// Economia da magia lida de <c>ModuleData/sotor_spell_prices.xml</c>.
///
/// O SOTOR era distribuido COM esse arquivo e um comentario dizendo "edite os
/// numeros a gosto" — mas nenhuma versao do mod jamais o leu (verificado por
/// varredura no fonte decompilado e no fork): os precos viviam so na tabela
/// hardcoded <c>SotorLores.Prices</c> e no switch de tiers do
/// <c>SotorSpellcraftHelper</c>. Este loader implementa o que o arquivo prometia.
///
/// Por que importa: desbloquear uma escola custava 100.000 fixos, e o desconto/
/// penalidade cultural multiplica em cima disso. Sem poder ajustar a base, a
/// unica forma de balancear a economia era recompilar.
///
/// Tudo e opcional e tolerante a falha:
///   - arquivo ausente        -> mantem os valores do SOTOR
///   - entrada ausente        -> mantem o valor daquela escola/tier
///   - numero malformado      -> ignora aquela linha e segue
/// Nada aqui pode impedir o jogo de carregar.
///
/// Schema (o mesmo que o SOTOR documentava):
/// <code>
/// &lt;SpellPrices&gt;
///   &lt;SpellTiers&gt;  &lt;Tier level="1" cost="5000" /&gt;  ... &lt;/SpellTiers&gt;
///   &lt;LorePrices&gt;  &lt;Lore id="LoreOfFire" cost="100000" /&gt; ... &lt;/LorePrices&gt;
///   &lt;Spells&gt;      &lt;Spell id="Fireball" cost="8000" /&gt;  ... &lt;/Spells&gt;
/// &lt;/SpellPrices&gt;
/// </code>
/// </summary>
public static class RFSpellPrices
{
	private const string FileName = "sotor_spell_prices.xml";

	private static readonly Dictionary<string, int> _lorePrices =
		new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<int, int> _tierPrices = new Dictionary<int, int>();

	private static readonly Dictionary<string, int> _spellOverrides =
		new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static bool _loaded;

	/// <summary>Nada carregado: todo getter devolve o valor do SOTOR.</summary>
	public static bool IsEmpty => !_loaded
		|| (_lorePrices.Count == 0 && _tierPrices.Count == 0 && _spellOverrides.Count == 0);

	public static void Load()
	{
		_loaded = true;
		_lorePrices.Clear();
		_tierPrices.Clear();
		_spellOverrides.Clear();

		try
		{
			string path = RFModulePath.Combine("ModuleData", FileName);
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				SotorLog.Info("RFSpellPrices: '" + FileName + "' ausente — precos do SOTOR mantidos.");
				return;
			}

			XmlDocument doc = new XmlDocument();
			doc.Load(path);

			ReadInto(doc, "//SpellTiers/Tier", "level", "cost", (key, cost) =>
			{
				if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
				{
					_tierPrices[level] = cost;
				}
			});

			ReadInto(doc, "//LorePrices/Lore", "id", "cost", (key, cost) => _lorePrices[key] = cost);
			ReadInto(doc, "//Spells/Spell", "id", "cost", (key, cost) => _spellOverrides[key] = cost);

			SotorLog.Info($"RFSpellPrices: {_lorePrices.Count} escola(s), {_tierPrices.Count} tier(s), {_spellOverrides.Count} feitico(s) com preco proprio.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("RFSpellPrices: leitura falhou (" + ex.GetType().Name + ": " + ex.Message + ") — precos do SOTOR mantidos.");
		}
	}

	private static void ReadInto(XmlDocument doc, string xpath, string keyAttr, string costAttr, Action<string, int> apply)
	{
		XmlNodeList nodes = doc.SelectNodes(xpath);
		if (nodes == null)
		{
			return;
		}

		foreach (XmlNode node in nodes)
		{
			try
			{
				string key = node.Attributes?[keyAttr]?.Value;
				string raw = node.Attributes?[costAttr]?.Value;
				if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(raw))
				{
					continue;
				}
				if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cost) && cost >= 0)
				{
					apply(key.Trim(), cost);
				}
			}
			catch (Exception)
			{
				// uma linha ruim nao derruba o resto da tabela
			}
		}
	}

	/// <summary>Preço de desbloqueio da escola; <paramref name="fallback" /> se não listada.</summary>
	public static int GetLorePrice(string loreId, int fallback)
	{
		if (!string.IsNullOrEmpty(loreId) && _lorePrices.TryGetValue(loreId, out int price))
		{
			return price;
		}
		return fallback;
	}

	/// <summary>
	/// Preço de um feitiço: override próprio se houver, senão o preço do tier,
	/// senão <paramref name="fallback" /> (o switch original do SOTOR).
	/// </summary>
	public static int GetSpellPrice(string spellId, int spellTier, int fallback)
	{
		if (!string.IsNullOrEmpty(spellId) && _spellOverrides.TryGetValue(spellId, out int own))
		{
			return own;
		}
		if (_tierPrices.TryGetValue(spellTier, out int byTier))
		{
			return byTier;
		}
		return fallback;
	}
}
