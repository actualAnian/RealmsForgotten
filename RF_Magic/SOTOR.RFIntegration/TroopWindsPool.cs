using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// Winds of Magic para TROPAS (agentes sem herói).
///
/// O custo de conjuração do SOTOR mora em <c>Spell.IsDisabled</c> e
/// <c>Spell.OnCastSucceeded</c>, ambos dentro de <c>TryGetWindsHero(...)</c>, que
/// exige um <see cref="TaleWorlds.CampaignSystem.Hero" /> com
/// <c>HeroExtendedInfo</c>. Tropa não tem herói: o bloco inteiro era pulado e o
/// resultado era **mana infinito** — limitado só pelo cooldown de cada feitiço.
/// Com 8 feitiços de cooldowns diferentes, a tropa alternava entre eles e
/// conjurava quase sem parar.
///
/// No sistema legado do RF isso não acontecia porque a tropa carregava munição-
/// feitiço em quantidade finita. Ao migrar para o motor novo esse limite sumiu e
/// nada ocupou o lugar. Este pool é o substituto.
///
/// <para>Regras</para>
/// <list type="bullet">
/// <item>O tamanho do pool é o <c>maxWinds</c> do foco (rf_arcane_foci.xml, 10 a
/// 60) — o cajado é a bateria, exatamente como para o jogador.</item>
/// <item>É por BATALHA e não regenera. Espelha a munição finita do legado e
/// mantém o balanceamento previsível: um cajado tier 1 rende poucos feitiços,
/// um tier 5 sustenta uma batalha inteira.</item>
/// <item>Vive só em memória, indexado pelo agente. Nada entra no save — tropa
/// não é herói e não deve criar dado persistente.</item>
/// </list>
/// </summary>
public static class TroopWindsPool
{
	/// <summary>
	/// Intervalo mínimo entre duas conjurações da MESMA tropa, em segundos.
	///
	/// Não é uma segunda trava de segurança — o pool já limita o total. Isto
	/// limita a CADÊNCIA: sem ele, uma tropa de pool cheio dispara toda a reserva
	/// em poucos segundos (cada feitiço tem cooldown próprio, então não competem
	/// entre si) e depois fica muda o resto da batalha. Com o intervalo, a magia
	/// se distribui ao longo da luta.
	/// </summary>
	private const float MinSecondsBetweenCasts = 4f;

	private static readonly Dictionary<Agent, float> _remaining = new Dictionary<Agent, float>();

	private static readonly Dictionary<Agent, float> _nextCastAllowedAt = new Dictionary<Agent, float>();

	/// <summary>Esta tropa usa este pool? (herói nunca usa — tem o do save.)</summary>
	public static bool AppliesTo(Agent agent)
	{
		return agent != null && agent.GetHero() == null && ArcaneFocusCasterSource.IsFocusCaster(agent);
	}

	/// <summary>
	/// Tamanho do pool, pela MESMA fórmula do jogador
	/// (<c>SotorSpellcraftHelper.GetMaxWinds</c>): base do foco + 0.3 × Arcane.
	///
	/// A parte da skill não é enfeite. Só com a base do cajado os pools ficavam
	/// impraticáveis — medido: o <c>necromancer_staff</c> (base 28) não pagava nem
	/// um único feitiço de 30 da própria escola. Somando o Arcane da tropa (que
	/// vai de 45 a 300 no conteúdo do RF), o mesmo cajado rende mais na mão de
	/// quem sabe mais — que é a regra que já vale para o jogador.
	///
	/// 0 se não houver foco: sem instrumento não se acumula mana, nem para tropas.
	/// </summary>
	public static float GetMax(Agent agent)
	{
		ArcaneFocusData focus = ArcaneFocusCasterSource.GetCasterFocus(agent);
		if (focus == null)
		{
			return 0f;
		}

		float arcane = 0f;
		SkillObject skill = SotorSkills.Spellcraft;
		if (skill != null)
		{
			arcane = agent.Character?.GetSkillValue(skill) ?? 0;
		}

		return focus.MaxWinds + 0.3f * arcane;
	}

	/// <summary>
	/// Regeneração por segundo de batalha, escalada pelo ARCANE da tropa e pelo
	/// <c>rechargeMult</c> do foco (que já estava no rf_arcane_foci.xml sem uso).
	///
	/// Por que existe: o pool era fixo por batalha, imitando a munição finita do
	/// legado. Medido em jogo, isso quebrava a economia — <c>PreserveWinds</c> vale
	/// <c>min(0.4, 1 − poolRestante)</c>, então CRESCE conforme o pool esvazia. Com
	/// pool que não volta, a tropa batia no teto de 0.40 e ficava lá: 58% das
	/// decisões viraram "não fazer nada", contra uma nota média de 0.389 dos
	/// feitiços. O mago congelava no meio da luta.
	///
	/// Com regeneração o ciclo fecha: conjura → poupa → recupera → conjura. Um
	/// Arcane 65 devolve ~0.9/s, um Arcane 300 ~2.4/s (mais o multiplicador do
	/// cajado) — o mago experiente sustenta o ritmo, o aprendiz precisa esperar.
	/// </summary>
	private const float RegenBasePerSecond = 0.5f;

	private const float RegenPerArcanePoint = 0.006f;

	private static float _lastRegenTime;

	/// <summary>
	/// Devolve Winds a todos os conjuradores. Chamado por tick da missão; usa o
	/// tempo decorrido real, então independe da taxa de quadros.
	/// </summary>
	public static void TickRegeneration()
	{
		Mission mission = Mission.Current;
		if (mission == null || _remaining.Count == 0)
		{
			return;
		}

		float now = mission.CurrentTime;
		float dt = now - _lastRegenTime;
		_lastRegenTime = now;
		if (dt <= 0f || dt > 5f)
		{
			return; // primeiro tick, pausa ou salto de tempo: não regenera em bloco
		}

		List<Agent> agents = new List<Agent>(_remaining.Keys);
		foreach (Agent agent in agents)
		{
			float max = GetMax(agent);
			float current = _remaining[agent];
			if (current >= max)
			{
				continue;
			}

			float arcane = 0f;
			SkillObject skill = SotorSkills.Spellcraft;
			if (skill != null)
			{
				arcane = agent.Character?.GetSkillValue(skill) ?? 0;
			}
			ArcaneFocusData focus = ArcaneFocusCasterSource.GetCasterFocus(agent);
			float rechargeMult = focus?.RechargeMult ?? 1f;

			float perSecond = (RegenBasePerSecond + RegenPerArcanePoint * arcane) * rechargeMult;
			_remaining[agent] = Math.Min(max, current + perSecond * dt);
		}
	}

	/// <summary>Quanto resta. Na primeira consulta o pool nasce cheio.</summary>
	public static float GetRemaining(Agent agent)
	{
		if (agent == null)
		{
			return 0f;
		}

		if (!_remaining.TryGetValue(agent, out float value))
		{
			value = GetMax(agent);
			_remaining[agent] = value;
		}
		return value;
	}

	/// <summary>
	/// A tropa pode pagar este feitiço agora? Considera reserva E cadência.
	/// </summary>
	public static bool CanAfford(Agent agent, AbilityTemplate template)
	{
		if (!AppliesTo(agent))
		{
			return true; // não é caso nosso — quem decide é o caminho do herói
		}

		float now = Mission.Current?.CurrentTime ?? 0f;
		if (_nextCastAllowedAt.TryGetValue(agent, out float allowedAt) && now < allowedAt)
		{
			return false;
		}

		// Afinidade do cajado: instrumento afim gasta menos (ver ArcaneFocusAffinity).
		int cost = ArcaneFocusAffinity.ApplyToWindsCost(template?.WindsOfMagicCost ?? 0, agent, template?.BelongsToLoreID);
		if (cost <= 0)
		{
			return true;
		}

		return GetRemaining(agent) >= cost;
	}

	/// <summary>Debita o custo e arma o intervalo até a próxima conjuração.</summary>
	public static void Spend(Agent agent, AbilityTemplate template)
	{
		if (!AppliesTo(agent))
		{
			return;
		}

		float now = Mission.Current?.CurrentTime ?? 0f;
		_nextCastAllowedAt[agent] = now + MinSecondsBetweenCasts;

		int cost = ArcaneFocusAffinity.ApplyToWindsCost(template?.WindsOfMagicCost ?? 0, agent, template?.BelongsToLoreID);
		if (cost <= 0)
		{
			return;
		}

		float before = GetRemaining(agent);
		float after = Math.Max(0f, before - cost);
		_remaining[agent] = after;

		SotorLog.Debug($"Troop winds: '{agent.Character?.StringId}' spent {cost} on {template?.StringID} | {before:0} -> {after:0} / {GetMax(agent):0}.");
	}

	/// <summary>Fração restante (0-1). A IA usa para decidir quando poupar.</summary>
	public static float GetRemainingRatio(Agent agent)
	{
		float max = GetMax(agent);
		if (max <= 0f)
		{
			return 1f;
		}
		return GetRemaining(agent) / max;
	}

	/// <summary>Agente morreu/saiu: solta as referências (o dicionário guarda Agent).</summary>
	public static void Forget(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		_remaining.Remove(agent);
		_nextCastAllowedAt.Remove(agent);
	}

	/// <summary>Fim de missão: nada sobrevive à batalha.</summary>
	public static void Reset()
	{
		_remaining.Clear();
		_nextCastAllowedAt.Clear();
	}
}
