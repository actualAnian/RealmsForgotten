using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// Escolas de magia por CULTURA (lido de <c>rf_culture_lores.xml</c>).
///
/// Inversão de modelo pedida pelo autor: antes o CAJADO decidia quais escolas a
/// tropa canalizava, então <c>evil_witch</c> (Vortiak Black Church) e
/// <c>lord_mage_2</c> (Tremerid) — que empunham o mesmo Ancient Vortiak Staff —
/// tinham repertório idêntico. A facção não tinha identidade mágica.
///
/// Agora a cultura decide as escolas; o cajado continua sendo o instrumento
/// obrigatório (sem foco não há magia) e passa a dar BÔNUS nas escolas com que
/// tem afinidade (fase 4 — ArcaneFocusAffinity).
///
/// Fail-open deliberado: sem o XML, ou com cultura não listada, o sistema volta
/// ao comportamento anterior (escolas do cajado). Um mapeamento incompleto não
/// pode deixar o mundo sem magia — foi o erro que já cometemos com o gate de
/// Arcane, que zerou todas as tropas por causa de um id de skill errado.
/// </summary>
public static class RFCultureLores
{
	private const string FileName = "rf_culture_lores.xml";

	private static readonly Dictionary<string, List<string>> _byCulture =
		new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

	private static bool _loaded;

	/// <summary>Nada carregado → cada cajado volta a definir suas escolas.</summary>
	public static bool IsEmpty => !_loaded || _byCulture.Count == 0;

	public static void Load()
	{
		_loaded = true;
		_byCulture.Clear();

		string path;
		try
		{
			path = RFModulePath.Combine("ModuleData", FileName);
		}
		catch (Exception ex)
		{
			SotorLog.Error("RFCultureLores: could not resolve module path: " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		if (!File.Exists(path))
		{
			SotorLog.Warn("RFCultureLores: '" + path + "' not found — escolas voltam a vir do cajado.");
			return;
		}

		int skipped = 0;
		try
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			XmlNodeList nodes = doc.SelectNodes("//Culture");
			if (nodes != null)
			{
				foreach (XmlNode node in nodes)
				{
					string id = node.Attributes?["id"]?.Value;
					string raw = node.Attributes?["lores"]?.Value;
					if (string.IsNullOrWhiteSpace(id))
					{
						skipped++;
						continue;
					}

					List<string> lores = new List<string>();
					if (!string.IsNullOrWhiteSpace(raw))
					{
						foreach (string piece in raw.Split(','))
						{
							string s = piece.Trim();
							if (s.Length > 0 && !lores.Contains(s))
							{
								lores.Add(s);
							}
						}
					}
					_byCulture[id] = lores;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Error("RFCultureLores: failed to parse " + FileName + ": " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		SotorLog.Info($"RFCultureLores: loaded {_byCulture.Count} culture(s) from {FileName} (skipped {skipped} malformed).");
	}

	/// <summary>StringId da cultura do agente, ou null.</summary>
	public static string GetCultureId(Agent agent)
	{
		try
		{
			return (agent?.Character as CharacterObject)?.Culture?.StringId;
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Escolas desta cultura. Devolve <c>null</c> quando a cultura não foi
	/// mapeada — o chamador deve então cair no fallback do cajado. Devolve lista
	/// VAZIA quando a cultura existe mas foi declarada sem magia, o que é
	/// diferente e significa "esta gente não conjura".
	/// </summary>
	public static List<string> GetLoresForCulture(string cultureId)
	{
		if (IsEmpty || string.IsNullOrEmpty(cultureId))
		{
			return null;
		}
		return _byCulture.TryGetValue(cultureId, out List<string> lores) ? lores : null;
	}

	/// <summary>Conveniência: escolas da cultura do agente.</summary>
	public static List<string> GetLoresForAgent(Agent agent)
	{
		return GetLoresForCulture(GetCultureId(agent));
	}

	/// <summary>
	/// A escola pertence à cultura do HERÓI? Usado para cobrar mais caro do
	/// jogador quando ele aprende algo fora da própria tradição.
	/// Cultura não mapeada → true (não penaliza o que não sabemos classificar).
	/// </summary>
	public static bool IsNativeToHero(Hero hero, string loreId)
	{
		if (IsEmpty || hero == null || string.IsNullOrEmpty(loreId))
		{
			return true;
		}

		string cultureId = hero.Culture?.StringId;
		List<string> lores = GetLoresForCulture(cultureId);
		if (lores == null || lores.Count == 0)
		{
			return true;
		}

		for (int i = 0; i < lores.Count; i++)
		{
			if (string.Equals(lores[i], loreId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// DESCONTO da escola nativa (decisao do autor, 2026-07-30).
	///
	/// A primeira versao cobrava MAIS pela escola estrangeira (x1.75). Sobre a base
	/// de 100.000 do SOTOR isso virava 175.000 — na pratica a porta fechada que a
	/// regra dizia nao fechar (o jogador tinha 77.000 e nenhuma escola cabia, nem
	/// as nativas). Invertido: a escola da PROPRIA tradicao sai mais barata e a
	/// estrangeira custa o preco cheio. Mesma identidade cultural, sem transformar
	/// o resto do conteudo em inalcancavel.
	/// </summary>
	public const float NativeGoldMultiplier = 0.6f;

	/// <summary>Arcane extra exigido para uma escola fora da cultura.</summary>
	public const int ForeignArcanePenalty = 25;

	/// <summary>
	/// Preço em ouro já ajustado pela cultura: nativa recebe desconto, estrangeira
	/// paga o preço cheio.
	/// </summary>
	public static int AdjustPriceForHero(Hero hero, string loreId, int basePrice)
	{
		if (basePrice <= 0 || !IsNativeToHero(hero, loreId))
		{
			return basePrice;
		}
		return (int)Math.Round(basePrice * NativeGoldMultiplier);
	}

	/// <summary>Requisito de Arcane já ajustado pela cultura do herói.</summary>
	public static int AdjustArcaneForHero(Hero hero, string loreId, int baseArcane)
	{
		if (IsNativeToHero(hero, loreId))
		{
			return baseArcane;
		}
		return baseArcane + ForeignArcanePenalty;
	}
}
