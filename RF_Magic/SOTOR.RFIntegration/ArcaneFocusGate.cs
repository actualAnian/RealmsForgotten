using SOTOR.AbilitySystem;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// Ponto UNICO de decisao do gate de foco arcano da Fase B. Todos os lugares que
/// gateiam (Ability.IsDisabled, o menu QuickCast, a IA de conjuracao e o HUD)
/// perguntam aqui, para que a regra nunca divirja entre eles.
///
/// O gate so vale para quem tem <c>HeroExtendedInfo</c> — herois e o jogador.
/// Tropas conjuradoras do proprio SOTOR (esqueletos invocados, unidades caster)
/// passam direto: elas nao carregam foco e nao deveriam ser silenciadas por uma
/// regra que e sobre o instrumento do heroi.
/// </summary>
public static class ArcaneFocusGate
{
	/// <summary>
	/// Interruptor geral. Vive AQUI, e nao no SotorSettings, para nao tocar o
	/// arquivo de configuracao do SOTOR (regra do projeto: logica RF fica em
	/// RFIntegration). Desligar devolve o comportamento do SOTOR puro.
	/// </summary>
	public static bool Enabled = true;

	/// <summary>
	/// O gate esta valendo? Precisa do interruptor ligado E de pelo menos um foco
	/// registrado. Registro vazio = XML ausente/quebrado: fail-open deliberado,
	/// senao um arquivo faltando trancaria a magia inteira do jogo.
	/// </summary>
	public static bool IsActive => Enabled && !ArcaneFocusRegistry.IsEmpty;

	/// <summary>
	/// O gate se aplica a este agente? Só a heróis/jogador com info estendida —
	/// e só em campanha, que é onde o conceito de equipamento de herói existe.
	/// </summary>
	public static bool AppliesTo(Agent agent)
	{
		if (!IsActive || agent == null)
		{
			return false;
		}

		if (!(Game.Current?.GameType is Campaign))
		{
			return false;
		}

		Hero hero = agent.GetHero();
		if (hero == null)
		{
			// FASE 2a: o gate vale so para herois/jogador. Tropas conjuradoras do
			// proprio SOTOR (esqueletos invocados, unidades caster) passam direto —
			// elas nao carregam foco e nao devem ser silenciadas por uma regra que
			// e sobre o instrumento do heroi. Quando as tropas do RF virarem
			// conjuradoras por foco (fase 7), este ramo passa a consultar o
			// registro de tropas-caster.
			return false;
		}

		return hero.GetExtendedInfo() != null;
	}

	// ── Foco suspenso pelo proprio cast ──────────────────────────────────────
	// O SOTOR EMBAINHA a arma ao entrar em modo de mira (EnableTargetingMode ->
	// _shouldSheathWeapon) para tocar a animacao de conjuracao. Como o gate le
	// Agent.WieldedWeapon, sem isto ele se AUTO-BLOQUEARIA: a mao fica vazia e
	// todo feitico com mira (AbilityTargetType != Self) viraria impossivel.
	// O AbilityManager registra aqui, ANTES de embainhar, qual item saiu da mao;
	// o registro so vale enquanto o cast durar e e limpo quando a arma volta.
	private static string _suspendedFocusItemId;

	/// <summary>Item que o sistema de cast tirou da mao (null = nenhum).</summary>
	public static string SuspendedFocusItemId => _suspendedFocusItemId;

	/// <summary>Chamado pelo AbilityManager imediatamente antes de embainhar.</summary>
	public static void NoteFocusSuspendedForCast(string itemId)
	{
		_suspendedFocusItemId = itemId;
	}

	/// <summary>
	/// Versao que resolve o item sozinha, para que o gancho no AbilityManager seja
	/// de uma linha. Recebe os slots que ele acabou de cachear e registra o
	/// primeiro que for um foco conhecido.
	/// </summary>
	public static void NoteFocusSuspendedForWieldedItem(Agent agent, EquipmentIndex mainHand, EquipmentIndex offHand)
	{
		if (!IsActive || agent == null)
		{
			return;
		}

		try
		{
			foreach (EquipmentIndex slot in new[] { mainHand, offHand })
			{
				if (slot == EquipmentIndex.None)
				{
					continue;
				}
				string itemId = agent.Equipment?[slot].Item?.StringId;
				if (!string.IsNullOrEmpty(itemId) && ArcaneFocusRegistry.TryGetFocus(itemId, out ArcaneFocusData _))
				{
					_suspendedFocusItemId = itemId;
					return;
				}
			}
		}
		catch
		{
			// nunca deixar o registro do foco suspenso derrubar a conjuracao
		}
	}

	/// <summary>Chamado quando a arma volta a mao, o agente morre ou a missao troca.</summary>
	public static void ClearSuspendedFocus()
	{
		_suspendedFocusItemId = null;
	}

	/// <summary>
	/// Este agente pode conjurar ALGUMA coisa agora? (só a regra do foco em mão —
	/// não olha o feitiço.) É o que o QuickCast, a IA e o HUD consultam.
	/// </summary>
	public static bool CanCastAtAll(Agent agent)
	{
		if (!AppliesTo(agent))
		{
			return true;
		}

		return ArcaneFocusRegistry.HasFocusEquipped(agent);
	}

	/// <summary>
	/// Regra completa para um feitiço específico. Devolve true quando o cast deve
	/// ser BLOQUEADO, preenchendo <paramref name="reason" /> com o motivo exibido
	/// ao jogador. Três checagens, na ordem: foco na mão → tier da varinha →
	/// escola do foco.
	/// </summary>
	public static bool IsBlocked(Agent agent, AbilityTemplate template, out TextObject reason)
	{
		reason = null;
		if (!AppliesTo(agent))
		{
			return false;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetEquippedFocus(agent);
		if (focus == null)
		{
			reason = new TextObject("{=rf_no_focus}Sem um foco arcano empunhado.");
			return true;
		}

		if (template == null)
		{
			return false;
		}

		if (!focus.AllowsSpellTier(template.SpellTier))
		{
			reason = new TextObject("{=rf_wand_tier}Uma varinha só canaliza feitiços de tier 1-2.");
			return true;
		}

		// O `lores` do foco NAO restringe mais nada — virou AFINIDADE (bonus de
		// custo e de cooldown, ver ArcaneFocusAffinity). Quem decide QUAIS escolas
		// um conjurador acessa e a CULTURA (RFCultureLores).
		//
		// Este bloqueio ficou para tras quando o modelo mudou e derrubou toda a
		// magia das tropas: a cultura Tremerid concede 11 escolas, o repertorio
		// vinha completo, e entao o cajado barrava tudo que nao fosse a escola
		// dele — "CanCast=false: Este foco nao canaliza esta escola de magia",
		// repetido para os 8 feiticos, batalha inteira.
		//
		// O que o foco AINDA restringe continua acima: precisa estar EMPUNHADO
		// (a regra inviolavel do mod) e a varinha so canaliza tier 1-2.

		return false;
	}

	// ── Bateria (B3) ─────────────────────────────────────────────────────────
	// Multiplicadores neutros quando o gate está desligado, para que o caminho
	// "MCM OFF" seja aritmeticamente idêntico ao SOTOR puro.

	/// <summary>
	/// Quanto a skill Arcane AMPLIFICA o pool do foco, por ponto. 0.002 = +60% no
	/// nivel 300, o que leva o archmage (base 60) a ~96 — perto do teto do SOTOR
	/// original (100), sem inflar nem esvaziar a economia de mana.
	/// </summary>
	public const float SkillAmplificationPerPoint = 0.002f;

	/// <summary>
	/// Teto de Mana. No RF o cajado e DETERMINANTE: a skill multiplica o pool do
	/// instrumento em vez de somar por cima.
	///
	/// Por que multiplicativo (decisao do autor, 2026-07-30): na forma aditiva a
	/// skill somava um valor fixo que, em Arcane alto, encolhia a diferenca entre
	/// varinha e archmage de 6x para 2x — o instrumento virava menos decisivo que a
	/// skill, o oposto da identidade do mod. Multiplicando, a razao entre focos fica
	/// travada em qualquer nivel de skill: nenhuma quantidade de Arcane compensa um
	/// cajado pior.
	///
	/// Sem foco -> 0 (nao se acumula mana sem instrumento).
	/// Gate desligado -> <paramref name="sotorOriginal" />, para que o caminho
	/// "SOTOR puro" continue aritmeticamente identico ao mod original.
	/// </summary>
	public static float ComputeMaxWinds(Hero hero, int arcaneValue, float sotorOriginal)
	{
		if (!IsActive || hero == null)
		{
			return sotorOriginal;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetBatteryFocus(hero);
		if (focus == null)
		{
			return 0f;
		}

		return focus.MaxWinds * (1f + SkillAmplificationPerPoint * arcaneValue);
	}

	/// <summary>Multiplicador de recarga do foco; 0 sem foco (não regenera), 1
	/// com o gate desligado.</summary>
	public static float GetRechargeMultiplier(Hero hero)
	{
		if (!IsActive || hero == null)
		{
			return 1f;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetBatteryFocus(hero);
		return focus?.RechargeMult ?? 0f;
	}

	/// <summary>Multiplicador de efetividade do foco; 1 sem foco (neutro — o dano
	/// já é barrado pelo gate de cast, não faz sentido zerar aqui).</summary>
	public static float GetEffectivenessMultiplier(Hero hero)
	{
		if (!IsActive || hero == null)
		{
			return 1f;
		}

		ArcaneFocusData focus = ArcaneFocusRegistry.GetBatteryFocus(hero);
		return focus?.EffectivenessMult ?? 1f;
	}
}
