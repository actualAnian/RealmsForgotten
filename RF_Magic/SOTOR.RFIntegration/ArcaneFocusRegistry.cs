using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// Registro dos focos arcanos do RealmsForgotten (cajados e varinhas), lido de
/// <c>ModuleData/rf_arcane_foci.xml</c>.
///
/// Identidade do RF: magia depende do instrumento. Este registro e a fonte da
/// verdade sobre "isto e um foco?" e sobre os numeros do foco.
///
/// DUAS perguntas diferentes, de proposito:
/// <list type="bullet">
/// <item><b>Empunhado</b> (<see cref="GetEquippedFocus" />): usado pelo GATE de
/// conjuracao. Voce so conjura com o foco na mao — sacou a espada, nao conjura.</item>
/// <item><b>Carregado</b> (<see cref="GetBatteryFocus" />): usado pela BATERIA
/// (Winds max / recarga / efetividade). Basta ter o foco no inventario de
/// combate. Se a bateria exigisse "empunhado", puxar a espada zeraria o pool e a
/// regeneracao clamparia os Winds pra zero — punicao que o design nao pede.</item>
/// </list>
///
/// FAIL-OPEN por decisao explicita: se o XML nao existir, estiver vazio ou nao
/// parsear, o registro fica vazio e <see cref="IsEmpty" /> vira true; quem
/// consulta trata isso como "gate desligado" e o comportamento volta ao SOTOR
/// puro. Um XML ausente degrada o mod, nunca o trava.
/// </summary>
public static class ArcaneFocusRegistry
{
	private const string FileName = "rf_arcane_foci.xml";

	private static readonly Dictionary<string, ArcaneFocusData> _byItemId =
		new Dictionary<string, ArcaneFocusData>(StringComparer.OrdinalIgnoreCase);

	private static bool _loaded;

	/// <summary>Nenhum foco registrado — o gate inteiro deve se comportar como
	/// desligado (ver nota de fail-open na classe).</summary>
	public static bool IsEmpty => _byItemId.Count == 0;

	public static int Count => _byItemId.Count;

	public static void Load()
	{
		_loaded = true;
		_byItemId.Clear();

		string path;
		try
		{
			// Mesmo padrao que o AbilityFactory usa para achar o ModuleData proprio.
			path = RFModulePath.Combine("ModuleData", FileName);
		}
		catch (Exception ex)
		{
			SotorLog.Error("ArcaneFocusRegistry: could not resolve module path: " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		if (!File.Exists(path))
		{
			SotorLog.Warn("ArcaneFocusRegistry: '" + path + "' not found — arcane-focus gating stays OFF (pure SOTOR behaviour).");
			return;
		}

		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(path);
			XmlNodeList nodes = xmlDocument.SelectNodes("//Focus");
			if (nodes == null)
			{
				SotorLog.Warn("ArcaneFocusRegistry: no <Focus> nodes in " + FileName + " — gating stays OFF.");
				return;
			}

			int skipped = 0;
			foreach (XmlNode node in nodes)
			{
				ArcaneFocusData focus = ParseFocus(node);
				if (focus == null)
				{
					skipped++;
					continue;
				}

				// Ultima definicao vence, mas avisa: id duplicado quase sempre e
				// erro de edicao manual do XML.
				if (_byItemId.ContainsKey(focus.ItemId))
				{
					SotorLog.Warn("ArcaneFocusRegistry: duplicate itemId '" + focus.ItemId + "' — the later entry wins.");
				}

				_byItemId[focus.ItemId] = focus;
			}

			SotorLog.Info($"ArcaneFocusRegistry: loaded {_byItemId.Count} arcane foci from {FileName} (skipped {skipped} malformed).");
		}
		catch (Exception ex)
		{
			_byItemId.Clear();
			SotorLog.Error("ArcaneFocusRegistry: failed to parse " + FileName + " — gating stays OFF. " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static ArcaneFocusData ParseFocus(XmlNode node)
	{
		string itemId = GetAttr(node, "itemId");
		if (string.IsNullOrWhiteSpace(itemId))
		{
			SotorLog.Warn("ArcaneFocusRegistry: <Focus> without itemId ignored.");
			return null;
		}

		var lores = new List<string>();
		string loreCsv = GetAttr(node, "lores");
		if (!string.IsNullOrWhiteSpace(loreCsv))
		{
			foreach (string part in loreCsv.Split(','))
			{
				string trimmed = part.Trim();
				if (trimmed.Length > 0)
				{
					lores.Add(trimmed);
				}
			}
		}

		return new ArcaneFocusData(
			itemId.Trim(),
			ParseInt(GetAttr(node, "tier"), 1),
			ParseFloat(GetAttr(node, "maxWinds"), 0f),
			ParseFloat(GetAttr(node, "rechargeMult"), 1f),
			ParseFloat(GetAttr(node, "effectivenessMult"), 1f),
			ParseBool(GetAttr(node, "isWand"), defaultValue: false),
			lores);
	}

	private static string GetAttr(XmlNode node, string name)
	{
		return node?.Attributes?[name]?.Value;
	}

	// InvariantCulture de proposito: o XML e escrito com ponto decimal e nao pode
	// depender da locale da maquina (pt-BR usa virgula e quebraria "1.2").
	private static float ParseFloat(string raw, float defaultValue)
	{
		if (!string.IsNullOrWhiteSpace(raw) && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
		{
			return value;
		}

		return defaultValue;
	}

	private static int ParseInt(string raw, int defaultValue)
	{
		if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
		{
			return value;
		}

		return defaultValue;
	}

	private static bool ParseBool(string raw, bool defaultValue)
	{
		if (!string.IsNullOrWhiteSpace(raw) && bool.TryParse(raw.Trim(), out bool value))
		{
			return value;
		}

		return defaultValue;
	}

	public static bool TryGetFocus(string itemId, out ArcaneFocusData data)
	{
		data = null;
		if (string.IsNullOrEmpty(itemId))
		{
			return false;
		}

		if (!_loaded)
		{
			Load();
		}

		return _byItemId.TryGetValue(itemId, out data);
	}

	// ── Consultas por agente (batalha) ───────────────────────────────────────

	/// <summary>
	/// O foco EMPUNHADO pelo agente, ou null. Esta e a consulta do gate de
	/// conjuracao: e avaliada a cada cast (nada e cacheado por missao), entao
	/// trocar de cajado no meio da batalha vale na hora.
	/// </summary>
	public static ArcaneFocusData GetEquippedFocus(Agent agent)
	{
		if (agent == null || IsEmpty)
		{
			return null;
		}

		try
		{
			MissionWeapon wielded = agent.WieldedWeapon;
			if (!wielded.IsEmpty && wielded.Item != null && TryGetFocus(wielded.Item.StringId, out ArcaneFocusData data))
			{
				return data;
			}

			// Mao vazia pode significar "o sistema de cast acabou de embainhar o
			// cajado para tocar a animacao" — nao "o mago esta desarmado". Sem
			// este fallback o gate se auto-bloquearia e nenhum feitico de mira
			// poderia ser conjurado. Vale so para o agente do jogador e so
			// enquanto o cast esta em curso (ver ArcaneFocusGate).
			if (agent.IsMainAgent && wielded.IsEmpty)
			{
				string suspended = ArcaneFocusGate.SuspendedFocusItemId;
				if (!string.IsNullOrEmpty(suspended) && TryGetFocus(suspended, out ArcaneFocusData suspendedData))
				{
					return suspendedData;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("ArcaneFocusRegistry.GetEquippedFocus failed: " + ex.Message);
		}

		return null;
	}

	public static bool HasFocusEquipped(Agent agent)
	{
		return GetEquippedFocus(agent) != null;
	}

	/// <summary>
	/// Melhor foco CARREGADO pelo agente (qualquer slot de arma), pelo maior
	/// MaxWinds. Usado pela bateria, nao pelo gate.
	/// </summary>
	public static ArcaneFocusData GetCarriedFocus(Agent agent)
	{
		if (agent == null || IsEmpty)
		{
			return null;
		}

		ArcaneFocusData best = null;
		try
		{
			MissionEquipment equipment = agent.Equipment;
			if (equipment == null)
			{
				return null;
			}

			for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
			{
				MissionWeapon weapon = equipment[i];
				if (weapon.IsEmpty || weapon.Item == null)
				{
					continue;
				}

				if (TryGetFocus(weapon.Item.StringId, out ArcaneFocusData data) && (best == null || data.MaxWinds > best.MaxWinds))
				{
					best = data;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("ArcaneFocusRegistry.GetCarriedFocus failed: " + ex.Message);
		}

		return best;
	}

	// ── Consultas por heroi (campanha) ───────────────────────────────────────

	/// <summary>
	/// Melhor foco no equipamento de batalha do heroi, pelo maior MaxWinds.
	/// Usado fora de missao (spellbook, tick de campanha).
	/// </summary>
	public static ArcaneFocusData GetHeroFocus(Hero hero)
	{
		if (hero == null || IsEmpty)
		{
			return null;
		}

		ArcaneFocusData best = null;
		try
		{
			Equipment equipment = hero.BattleEquipment;
			if (equipment == null)
			{
				return null;
			}

			for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
			{
				EquipmentElement element = equipment[i];
				if (element.IsEmpty || element.Item == null)
				{
					continue;
				}

				if (TryGetFocus(element.Item.StringId, out ArcaneFocusData data) && (best == null || data.MaxWinds > best.MaxWinds))
				{
					best = data;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("ArcaneFocusRegistry.GetHeroFocus failed: " + ex.Message);
		}

		return best;
	}

	/// <summary>
	/// Foco que alimenta a BATERIA do heroi (Winds max, recarga, efetividade).
	///
	/// Dentro de uma missao o agente do jogador manda — e o que faz "trocar de
	/// cajado no meio da batalha" mudar o pool de verdade. Fora disso (ou para
	/// herois sem agente barato de achar) cai no equipamento de campanha.
	/// </summary>
	public static ArcaneFocusData GetBatteryFocus(Hero hero)
	{
		if (hero == null || IsEmpty)
		{
			return null;
		}

		try
		{
			// Só o agente principal é resolvido aqui: é O(1) e cobre o caso que
			// importa (o jogador trocando de foco). Varrer Mission.Agents por
			// herói sairia caro — isto é chamado de getters de HUD e de ticks.
			if (Mission.Current != null && hero.IsHumanPlayerCharacter)
			{
				Agent main = Agent.Main;
				if (main != null)
				{
					// O foco EMPUNHADO manda. Antes isto usava direto o "melhor
					// foco carregado", e trocar de cajado em batalha nao mudava
					// nada — pior, bastava levar o cajado grande na mochila para
					// usar a varinha com a bateria dele, anulando a progressao.
					// (Inclui o foco que o proprio cast embainhou.)
					ArcaneFocusData wielded = GetEquippedFocus(main);
					if (wielded != null)
					{
						return wielded;
					}

					// Nenhum foco na mao (sacou a espada): cai para o carregado,
					// para nao zerar pool e recarga so por trocar de arma.
					ArcaneFocusData carried = GetCarriedFocus(main);
					if (carried != null)
					{
						return carried;
					}
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("ArcaneFocusRegistry.GetBatteryFocus (agent path) failed: " + ex.Message);
		}

		return GetHeroFocus(hero);
	}
}
