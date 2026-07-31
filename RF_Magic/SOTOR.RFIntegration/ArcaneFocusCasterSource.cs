using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SOTOR.RFIntegration;

/// <summary>
/// FASE C2 — como uma TROPA (agente sem herói) se torna conjuradora.
///
/// O SOTOR só sabe lidar com heróis: os atributos "AbilityUser"/"SpellCaster"
/// moram no <c>HeroExtendedInfo</c> (salvo no save) e são semeados apenas para o
/// jogador e o clã dele. Para quem não tem herói, o motor cai em
/// <c>CharacterObjectExtensions.GetAttributes()</c>, que devolve lista vazia —
/// ou seja, no motor original NENHUM NPC conjura.
///
/// O RF sempre funcionou pelo caminho oposto: conjurador era quem EMPUNHAVA um
/// item <c>Type=Musket</c> (o cajado). Era o equipamento, definido no XML de
/// tropas, que fazia a bruxa e os druidas lançarem magia — sem código dedicado.
/// Desligar o legado sem isto não tiraria a magia "da bruxa": tiraria do mundo
/// inteiro.
///
/// Aqui esse modelo é reconstruído sobre o registro de focos, o que também é a
/// identidade do mod: <b>sem cajado não há magia</b>. O foco carregado responde
/// as três perguntas de uma vez — se a tropa conjura, QUAIS escolas canaliza e
/// até que força.
/// </summary>
public static class ArcaneFocusCasterSource
{
	/// <summary>
	/// Teto de <c>SpellTier</c> que um foco concede a uma TROPA, pelo tier do
	/// próprio foco (1-5 → 1-4, que é a escala real dos feitiços).
	///
	/// Não dá para reaproveitar <see cref="ArcaneFocusData.AllowsSpellTier" />
	/// sozinho: ele só limita varinhas, e um cajado qualquer libera qualquer
	/// tier. Isso serve ao jogador — que ainda precisa comprar o feitiço e ter
	/// Arcane — mas para tropas, que recebem o repertório de graça, deixaria um
	/// druida de cajado modesto despejando magia Master. As duas regras somam:
	/// a da varinha continua valendo por cima desta.
	/// </summary>
	private const int MaxSpellTier = 4;

	/// <summary>
	/// Quantos feitiços uma TROPA carrega, no máximo.
	///
	/// "Escola do foco até o tier dele" descreve o que a tropa PODE canalizar,
	/// não o que faz sentido carregar. Medido: o cajado do arquimago canaliza
	/// todas as escolas e daria 96 feitiços a cada um dos quatro
	/// spc_embers_of_flame_leader — 96 objetos Ability por agente, com a IA
	/// escolhendo entre 96. O cajado da bruxa alada daria 36.
	///
	/// O corte é determinístico (tier decrescente, desempate por StringID) para
	/// que a mesma tropa tenha sempre o mesmo repertório entre batalhas e saves.
	/// Prioriza o tier mais alto disponível: um arquimago usa o que tem de melhor.
	/// </summary>
	private const int MaxSpellsPerTroop = 8;

	/// <summary>
	/// Piso de feiticos de ATAQUE no repertorio. Sem cota o corte podia entregar um
	/// conjunto so de buffs, e a tropa ficava se protegendo sem nunca atacar.
	/// </summary>
	private const int MinOffensiveSpells = 5;

	/// <summary>Pool por (foco, cultura). Derivar varre todos os templates das
	/// escolas; numa batalha isso rodaria por agente criado, então fica em cache.
	/// A ESCOLHA final e por agente e nao entra no cache — ver PickForAgent.</summary>
	private static readonly Dictionary<string, RepertoirePool> _repertoireByFocus =
		new Dictionary<string, RepertoirePool>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Invalidação para recarga de XML (o registro pode ser relido).</summary>
	public static void ClearCache()
	{
		_repertoireByFocus.Clear();
	}

	/// <summary>
	/// O foco que torna esta tropa conjuradora. Usa o foco CARREGADO, não o
	/// empunhado: <c>OnAgentCreated</c> roda antes de a tropa sacar a arma, e
	/// nesse instante o cajado ainda está num slot.
	/// </summary>
	public static ArcaneFocusData GetCasterFocus(Agent agent)
	{
		if (agent == null || ArcaneFocusRegistry.IsEmpty)
		{
			return null;
		}

		// Heróis seguem pelo caminho do SOTOR (spellbook + atributos salvos).
		if (agent.GetHero() != null)
		{
			return null;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetCarriedFocus(agent);
		if (focus == null)
		{
			return null;
		}

		return MeetsArcaneForFocus(agent, focus) ? focus : null;
	}

	/// <summary>
	/// O cajado exige nível de Arcane, e isso vale para a TROPA como vale para o
	/// jogador: carregar o instrumento não basta, é preciso saber usá-lo.
	///
	/// O requisito é o <c>difficulty</c> que os 11 focos já declaram em
	/// <c>enchanteditems.xml</c> (5 a 80) — não inventamos um número novo, é o
	/// mesmo campo que a API usa como exigência de skill do item.
	///
	/// Tropa sem Arcane declarada no XML tem valor 0 e é barrada. Isso é
	/// proposital: silenciar a falta faria o requisito valer só para metade do
	/// mundo. O bloqueio é logado uma vez por par (tropa, foco) para aparecer no
	/// log sem virar spam.
	/// </summary>
	private static bool MeetsArcaneForFocus(Agent agent, ArcaneFocusData focus)
	{
		int required = GetFocusDifficulty(focus.ItemId);
		if (required <= 0)
		{
			return true;
		}

		SkillObject skill = SotorSkills.Spellcraft;
		if (skill == null)
		{
			return true; // skill ainda não resolvida — não é hora de barrar ninguém
		}

		int value = agent.Character?.GetSkillValue(skill) ?? 0;
		if (value >= required)
		{
			return true;
		}

		string key = (agent.Character?.StringId ?? "?") + "|" + focus.ItemId;

		// FAIL-OPEN em zero. Um gate que não consegue LER o dado não pode barrar o
		// mundo inteiro — seria pior que não existir.
		//
		// Motivo concreto: os XMLs do RF declaravam <skill id="Arcane">, mas a skill
		// é registrada como SkillObject("arcane") em RFSkills.cs. O id não casava, a
		// skill nunca era aplicada e TODA tropa lia 0 — o gate barrava até a
		// evil_witch com 300 declarados. O case foi corrigido nos 33 pontos, mas se
		// a leitura voltar a zerar (ordem de carregamento, tropa nova sem a skill,
		// outro mod), é melhor a tropa conjurar e o log avisar do que o mundo ficar
		// mudo em silêncio.
		//
		// Zero legítimo + cajado na mão é combinação improvável; e quando acontece,
		// esta linha no log é o convite para declarar a skill no XML.
		if (value <= 0)
		{
			if (_loggedBlocks.Add(key))
			{
				SotorLog.Warn($"Focus '{focus.ItemId}': '{agent.Character?.StringId}' precisa de Arcane {required} mas a skill leu 0 — " +
					"provavelmente não declarada/carregada. PERMITINDO mesmo assim (declare <skill id=\"arcane\" ...> no XML da tropa).");
			}
			return true;
		}

		if (_loggedBlocks.Add(key))
		{
			SotorLog.Info($"Focus '{focus.ItemId}' denied to '{agent.Character?.StringId}': needs Arcane {required}, has {value}.");
		}
		return false;
	}

	private static readonly HashSet<string> _loggedBlocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, int> _difficultyByItem =
		new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Exigência de skill do item, com cache (lookup por id não é barato).</summary>
	private static int GetFocusDifficulty(string itemId)
	{
		if (_difficultyByItem.TryGetValue(itemId, out int cached))
		{
			return cached;
		}

		int difficulty = 0;
		try
		{
			ItemObject item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
			if (item != null)
			{
				difficulty = item.Difficulty;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn($"ArcaneFocusCasterSource: difficulty lookup for '{itemId}' failed: {ex.Message}");
		}

		_difficultyByItem[itemId] = difficulty;
		return difficulty;
	}

	/// <summary>Esta tropa conjura?</summary>
	public static bool IsFocusCaster(Agent agent)
	{
		return GetCasterFocus(agent) != null;
	}

	/// <summary>
	/// Atributos derivados do foco, no mesmo formato que o motor espera de um
	/// herói. Vazio quando não há foco — é isto que mantém "sem cajado não há
	/// magia" válido também para NPCs.
	/// </summary>
	public static List<string> GetAttributes(Agent agent)
	{
		if (!IsFocusCaster(agent))
		{
			return new List<string>();
		}

		return new List<string> { "AbilityUser", "SpellCaster" };
	}

	/// <summary>
	/// Feitiços que esta tropa sabe: os das escolas do foco, até o teto de tier.
	/// Lista vazia (não nula) quando não há foco.
	/// </summary>
	public static List<string> GetAbilities(Agent agent)
	{
		ArcaneFocusData focus = GetCasterFocus(agent);
		if (focus == null)
		{
			return new List<string>();
		}

		// As escolas vêm da CULTURA; o cajado segue definindo o teto de tier e o
		// pool. Cultura não mapeada devolve null e caímos no comportamento antigo
		// (escolas do próprio cajado) — fail-open proposital: um mapeamento
		// incompleto não pode calar o mundo, que foi o erro do gate de Arcane.
		List<string> cultureLores = RFCultureLores.GetLoresForAgent(agent);
		string cultureId = RFCultureLores.GetCultureId(agent) ?? "?";

		// A chave do cache precisa incluir a cultura: o mesmo cajado rende
		// repertórios diferentes para facções diferentes — é o ponto de todo o
		// modelo novo.
		string cacheKey = focus.ItemId + "|" + (cultureLores != null ? cultureId : "focus");
		if (!_repertoireByFocus.TryGetValue(cacheKey, out RepertoirePool pool))
		{
			pool = BuildRepertoire(focus, cultureLores);
			_repertoireByFocus[cacheKey] = pool;
			SotorLog.Info($"Repertorio: foco '{focus.ItemId}' (tier {focus.Tier}) + cultura '{cultureId}'"
				+ $" [{(cultureLores != null ? "escolas da cultura" : "FALLBACK: escolas do cajado")}].");
		}

		// O POOL e por (foco, cultura); a ESCOLHA e por agente.
		return PickForAgent(pool, agent);
	}

	private static RepertoirePool BuildRepertoire(ArcaneFocusData focus, List<string> cultureLores)
	{
		List<AbilityTemplate> eligible = new List<AbilityTemplate>();

		// Prioridade: escolas da CULTURA. Sem cultura mapeada, valem as do cajado
		// (comportamento anterior); cajado sem `lores` declarado canaliza todas.
		IEnumerable<string> lores;
		if (cultureLores != null)
		{
			lores = cultureLores;
		}
		else if (focus.Lores == null || focus.Lores.Count == 0)
		{
			lores = SotorLores.AllShownLores;
		}
		else
		{
			lores = focus.Lores;
		}

		int tierCap = Math.Min(Math.Max(focus.Tier, 1), MaxSpellTier);

		foreach (string lore in lores)
		{
			if (string.IsNullOrEmpty(lore))
			{
				continue;
			}

			IReadOnlyList<AbilityTemplate> templates;
			try
			{
				templates = AbilityFactory.GetTemplatesByLore(lore);
			}
			catch (Exception ex)
			{
				SotorLog.Warn($"ArcaneFocusCasterSource: lore '{lore}' failed: {ex.Message}");
				continue;
			}

			if (templates == null)
			{
				continue;
			}

			foreach (AbilityTemplate t in templates)
			{
				if (t == null || string.IsNullOrEmpty(t.StringID))
				{
					continue;
				}

				if (t.SpellTier > tierCap)
				{
					continue;
				}

				// A regra da varinha (tier 1-2) soma por cima do teto do foco.
				if (!focus.AllowsSpellTier(t.SpellTier))
				{
					continue;
				}

				if (!eligible.Exists(e => string.Equals(e.StringID, t.StringID, StringComparison.OrdinalIgnoreCase)))
				{
					eligible.Add(t);
				}
			}
		}

		// Tier decrescente, desempate alfabético: ordem estável entre execuções.
		eligible.Sort(delegate(AbilityTemplate a, AbilityTemplate b)
		{
			int byTier = b.SpellTier.CompareTo(a.SpellTier);
			return byTier != 0 ? byTier : string.CompareOrdinal(a.StringID, b.StringID);
		});

		// Separa por PAPEL. O corte anterior pegava os 8 primeiros da lista ordenada,
		// o que na pratica era "os 8 primeiros em ordem alfabetica" — arbitrario, sem
		// garantia de feitico de ataque, e IDENTICO para toda tropa com o mesmo
		// cajado e cultura. Medido in-game: 16 magos com repertorio identico lancando
		// o mesmo feitico ao mesmo tempo (as "dezenas de amber spears").
		RepertoirePool pool = new RepertoirePool();
		foreach (AbilityTemplate t in eligible)
		{
			(IsOffensive(t) ? pool.Offensive : pool.Support).Add(t.StringID);
		}
		SotorLog.Info($"Foco '{focus.ItemId}': pool = {pool.Offensive.Count} ofensivo(s) + {pool.Support.Count} suporte.");
		return pool;
	}

	/// <summary>
	/// Feitico de ATAQUE? Classificado pelo tipo de efeito, nao por nome. Hex conta
	/// como ofensivo: e debuff lancado no inimigo, e a IA o trata como ataque.
	/// </summary>
	private static bool IsOffensive(AbilityTemplate t)
	{
		switch (t.AbilityEffectType)
		{
			case AbilityEffectType.Missile:
			case AbilityEffectType.SeekerMissile:
			case AbilityEffectType.Blast:
			case AbilityEffectType.Vortex:
			case AbilityEffectType.Bombardment:
			case AbilityEffectType.Wind:
			case AbilityEffectType.Hex:
				return true;
			default:
				return false;
		}
	}

	/// <summary>Pool de feiticos de um par (foco, cultura), separado por papel.</summary>
	private sealed class RepertoirePool
	{
		public readonly List<string> Offensive = new List<string>();

		public readonly List<string> Support = new List<string>();
	}

	/// <summary>
	/// Escolhe os feiticos DESTE agente a partir do pool.
	///
	/// Duas regras:
	///   1. COTA POR PAPEL — pelo menos <see cref="MinOffensiveSpells" /> ofensivos,
	///      o resto de suporte. Sem isso um repertorio podia sair so com buffs e a
	///      tropa nunca atacava (relatado in-game).
	///   2. VARIACAO POR AGENTE — a janela comeca em <c>agent.Index</c>, rodando o
	///      pool. Magos identicos passam a ter repertorios diferentes, o que acaba
	///      com "16 tropas lancam o mesmo feitico no mesmo instante". Deterministico:
	///      o mesmo agente sempre recebe a mesma lista.
	/// </summary>
	private static List<string> PickForAgent(RepertoirePool pool, Agent agent)
	{
		int seed = Math.Max(0, agent?.Index ?? 0);
		int offensiveWanted = Math.Min(MinOffensiveSpells, pool.Offensive.Count);
		int supportWanted = Math.Min(MaxSpellsPerTroop - offensiveWanted, pool.Support.Count);
		// Sobrou espaco (poucos suportes)? Preenche com mais ataque.
		offensiveWanted = Math.Min(pool.Offensive.Count, MaxSpellsPerTroop - supportWanted);

		List<string> result = new List<string>(offensiveWanted + supportWanted);
		TakeRotating(pool.Offensive, seed, offensiveWanted, result);
		TakeRotating(pool.Support, seed, supportWanted, result);
		return result;
	}

	private static void TakeRotating(List<string> source, int seed, int count, List<string> into)
	{
		if (source.Count == 0 || count <= 0)
		{
			return;
		}
		int start = seed % source.Count;
		for (int i = 0; i < count; i++)
		{
			into.Add(source[(start + i) % source.Count]);
		}
	}
}
