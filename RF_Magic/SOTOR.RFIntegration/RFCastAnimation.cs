using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// FASE 2a — como o conjurador do RF se move ao lancar magia.
///
/// O SOTOR foi feito para magos de MAO VAZIA: ao entrar em modo de mira ele
/// EMBAINHA a arma e toca <c>act_spellcasting_idle</c>. No RealmsForgotten a
/// magia E o cajado — guardar o instrumento para conjurar contradiz a
/// identidade do mod. Aqui o cajado FICA NA MAO e o gesto passa a ser o de
/// arremesso de pedra, que o jogo ja anima de mao cheia:
///
///   mirar   -> act_ready_stone
///   lancar  -> act_release_stone
///
/// Nomes confirmados em Native/ModuleData/action_types.xml (existem tambem as
/// variantes _with_shield / _with_handshield, que o jogo escolhe sozinho pelo
/// conjunto de acoes do agente).
///
/// Fail-safe: se a acao nao resolver (index == act_none), devolvemos o padrao do
/// SOTOR. Assim um action set que nao conheca a acao de pedra volta ao
/// comportamento original em vez de deixar o agente em T-pose.
/// </summary>
public static class RFCastAnimation
{
	/// <summary>Gesto de mira com o foco na mao.</summary>
	public const string ReadyAction = "act_ready_stone";

	/// <summary>Gesto de lancamento com o foco na mao.</summary>
	public const string ReleaseAction = "act_release_stone";

	private static ActionIndexCache? _ready;

	private static bool _loggedReady;

	/// <summary>
	/// O foco deve continuar empunhado durante o cast? Verdadeiro quando o agente
	/// tem um foco arcano na mao e o gate esta ativo — isto e, quando a magia vem
	/// do instrumento. Usado para NAO embainhar.
	/// </summary>
	public static bool ShouldKeepFocusInHand(Agent agent)
	{
		if (!ArcaneFocusGate.IsActive || agent == null)
		{
			return false;
		}

		try
		{
			return ArcaneFocusRegistry.HasFocusEquipped(agent);
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Acao de POSTURA durante a mira. Com foco na mao devolve act_ready_stone;
	/// senao (ou se a acao nao existir) devolve o padrao do SOTOR.
	/// </summary>
	public static ActionIndexCache GetIdleStanceAction(Agent agent, ActionIndexCache sotorDefault)
	{
		if (!ShouldKeepFocusInHand(agent))
		{
			return sotorDefault;
		}

		if (!_ready.HasValue)
		{
			_ready = ActionIndexCache.Create(ReadyAction);
		}

		if (_ready.Value.Index == ActionIndexCache.act_none.Index)
		{
			if (!_loggedReady)
			{
				_loggedReady = true;
				SotorLog.Warn("RFCastAnimation: '" + ReadyAction + "' nao resolveu; mantendo a postura do SOTOR.");
			}
			return sotorDefault;
		}

		if (!_loggedReady)
		{
			_loggedReady = true;
			SotorLog.Info("RFCastAnimation: postura de mira com o foco na mao = '" + ReadyAction + "'.");
		}
		return _ready.Value;
	}

	/// <summary>
	/// Nome da acao de LANCAMENTO. Com foco na mao usa a de pedra; senao mantem a
	/// do template do feitico.
	/// </summary>
	public static string GetReleaseActionName(Agent agent, string templateAction)
	{
		if (!ShouldKeepFocusInHand(agent))
		{
			return templateAction;
		}

		ActionIndexCache release = ActionIndexCache.Create(ReleaseAction);
		if (release.Index == ActionIndexCache.act_none.Index)
		{
			return templateAction;
		}
		return ReleaseAction;
	}

	/// <summary>Solta os caches resolvidos; chamar ao trocar de missao.</summary>
	public static void Reset()
	{
		_ready = null;
		_loggedReady = false;
	}
}
