using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// AFINIDADE do cajado com uma escola de magia.
///
/// O campo <c>lores</c> de <c>rf_arcane_foci.xml</c> mudou de significado: era
/// RESTRIÇÃO (só estas escolas podem ser canalizadas) e virou AFINIDADE (estas
/// escolas rendem mais neste instrumento). Quem restringe agora é a cultura —
/// ver <see cref="RFCultureLores" />.
///
/// A ideia, nas palavras do autor: "os cajados passam a funcionar como bônus
/// específicos por serem constituídos por determinados estilos de magia". Um
/// Fire Crystal Wand nas mãos de um necromante ainda conjura necromancia — só
/// não ganha nada com isso, porque o instrumento não foi feito para aquilo.
///
/// O cajado continua obrigatório para conjurar: essa regra não muda.
/// </summary>
public static class ArcaneFocusAffinity
{
	/// <summary>Efetividade quando o cajado tem afinidade com a escola.</summary>
	private const float MatchBonus = 1.20f;

	/// <summary>
	/// Efetividade quando NÃO tem. Fica abaixo de 1 de propósito: sem penalidade
	/// o bônus viraria só um número maior para todo mundo, e a escolha de cajado
	/// deixaria de importar. 0.85 pesa na decisão sem inviabilizar o improviso.
	/// </summary>
	private const float MismatchPenalty = 0.85f;

	/// <summary>Este foco tem afinidade com a escola?</summary>
	public static bool HasAffinity(ArcaneFocusData focus, string loreId)
	{
		if (focus == null || string.IsNullOrEmpty(loreId))
		{
			return false;
		}

		// Foco sem `lores` declarado canaliza todas as escolas — é o caso do
		// Archmage Staff e do Winged Witch Staff. Instrumento universal tem
		// afinidade com tudo; é o que justifica o preço e o Arcane 200.
		if (focus.Lores == null || focus.Lores.Count == 0)
		{
			return true;
		}

		for (int i = 0; i < focus.Lores.Count; i++)
		{
			if (string.Equals(focus.Lores[i], loreId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Multiplicador de efetividade para este agente conjurando esta escola.
	/// 1.0 quando não há foco identificado (nada a premiar nem a punir).
	/// </summary>
	public static float GetMultiplier(Agent agent, string loreId)
	{
		if (agent == null || string.IsNullOrEmpty(loreId) || ArcaneFocusRegistry.IsEmpty)
		{
			return 1f;
		}

		// FASE 4: so o foco EMPUNHADO conta. O plano B pelo foco CARREGADO existia
		// para tropas conjuradoras (ArcaneFocusCasterSource), que entram na fase 7 —
		// quando aquela classe existir, este ?? volta.
		ArcaneFocusData focus = ArcaneFocusRegistry.GetEquippedFocus(agent);
		if (focus == null)
		{
			return 1f;
		}

		return HasAffinity(focus, loreId) ? MatchBonus : MismatchPenalty;
	}

	/// <summary>Mesma conta para um herói (jogador/companheiro).</summary>
	public static float GetMultiplier(Hero hero, string loreId)
	{
		if (hero == null || string.IsNullOrEmpty(loreId) || ArcaneFocusRegistry.IsEmpty)
		{
			return 1f;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetBatteryFocus(hero);
		if (focus == null)
		{
			return 1f;
		}

		return HasAffinity(focus, loreId) ? MatchBonus : MismatchPenalty;
	}

	/// <summary>
	/// Fator de CUSTO em Winds: instrumento afim gasta menos, instrumento
	/// estranho gasta mais. É o inverso da efetividade — conjurar fora da
	/// especialidade do cajado é mais trabalhoso.
	///
	/// Aplicado no custo (e não no dano) de propósito: o custo é determinístico e
	/// já passa por um ponto único, enquanto o dano se espalha por efeitos com
	/// atraso (DoT, área, status) e exigiria arrastar o AbilityTemplate por toda
	/// a cadeia. O efeito em jogo é o mesmo — o cajado certo rende mais magia.
	/// </summary>
	public static int ApplyToWindsCost(int baseCost, Agent agent, string loreId)
	{
		return Scale(baseCost, GetMultiplier(agent, loreId));
	}

	/// <summary>
	/// Mesma conta pelo HEROI. O custo de Winds passa por
	/// <c>HeroExtensions.GetEffectiveWindsCostForSpell(Hero, template)</c>, que e o
	/// ponto unico — e la nao existe Agent.
	/// </summary>
	public static int ApplyToWindsCost(int baseCost, Hero hero, string loreId)
	{
		return Scale(baseCost, GetMultiplier(hero, loreId));
	}

	/// <summary>Efetividade alta => custo/espera menor, e vice-versa.</summary>
	private static int Scale(int baseValue, float mult)
	{
		if (baseValue <= 0)
		{
			return baseValue;
		}
		if (Math.Abs(mult - 1f) < 0.001f)
		{
			return baseValue;
		}
		return Math.Max(1, (int)Math.Round(baseValue / mult));
	}

	/// <summary>
	/// Fator de COOLDOWN: o instrumento afim rearma mais rápido, o estranho
	/// demora mais. Mesma lógica do custo — efetividade alta encurta a espera.
	///
	/// Segundo eixo de premiação do cajado, pedido pelo autor. Como o custo, é um
	/// ponto único e determinístico (<c>Ability.TryCast</c> chama
	/// <c>SetCoolDown(Template.CoolDown)</c> uma vez por conjuração), então não
	/// exige propagar o template por efeitos com atraso.
	///
	/// Vale para tropas E para o jogador: o cooldown é do próprio objeto Ability,
	/// que existe nos dois casos.
	/// </summary>
	public static int ApplyToCooldown(int baseCooldown, Agent agent, string loreId)
	{
		return Scale(baseCooldown, GetMultiplier(agent, loreId));
	}
}
