using System;
using SOTOR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class SotorCastingAIMissionLogic : MissionLogic
{
	private bool IsActive()
	{
		// [RF-B] o interruptor "EnableCompanionSpellcasters" desligava a IA de
		// conjuracao INTEIRA, nao so a de companheiros — com ele em false nenhuma
		// tropa conjurava, e no RF a tropa conjuradora e a regra, nao a excecao.
		// Quem decide se um agente conjura e o foco arcano que ele carrega.
		return AbilityMissionModeHelper.IsBattleAbilityContext(base.Mission);
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		base.OnAgentBuild(agent, banner);
		if (!IsActive())
		{
			return;
		}
		try
		{
			if (ShouldControlAsCaster(agent) && agent.GetComponent<WizardAIComponent>() == null)
			{
				agent.AddComponent(new WizardAIComponent(agent));
			}
		}
		catch (Exception ex)
		{
			SotorLog.Error("CastingAI OnAgentBuild for '" + agent?.Name + "' failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	/// <summary>
	/// [RF-B] Diagnostico: uma linha por tropa CONJURADORA que nao recebeu a IA,
	/// dizendo em qual condicao parou.
	///
	/// Sem isto, "a tropa nao lancou magia" nao distingue entre nao ser caster, nao
	/// ter repertorio ou nao ter recebido a IA — e cada causa fica a um ciclo de
	/// jogo de distancia. Foi exatamente o que custou rodadas de teste.
	/// </summary>
	private static readonly System.Collections.Generic.HashSet<string> _loggedRejections =
		new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static bool Reject(Agent agent, string why)
	{
		// So interessa quem CARREGA um foco: senao todo soldado do mapa logaria.
		if (SOTOR.RFIntegration.ArcaneFocusCasterSource.GetCasterFocus(agent) != null
			&& _loggedRejections.Add(agent.Character?.StringId ?? "?"))
		{
			SotorLog.Info($"CastingAI: '{agent.Character?.StringId}' NAO virou caster — {why}.");
		}
		return false;
	}

	private static bool ShouldControlAsCaster(Agent agent)
	{
		if (agent == null || agent.IsPlayerControlled || agent.IsMainAgent || !agent.IsAIControlled || !agent.IsHuman)
		{
			return false;
		}
		if (!agent.IsAbilityUser())
		{
			return Reject(agent, "sem o atributo AbilityUser");
		}

		// [RF-B] Um mago-HEROI desarmado nao vira caster de IA: sem foco empunhado
		// ele nao conjuraria nada e ficaria travado tentando.
		//
		// Para TROPA a checagem por arma EMPUNHADA nao serve: este metodo roda antes
		// de a tropa sacar o cajado, entao CanCastAtAll (que le WieldedWeapon)
		// devolvia false e ela nunca recebia a IA — ficava batendo com o cajado, que
		// e exatamente o sintoma relatado. Para tropa vale o foco CARREGADO, o mesmo
		// criterio que a tornou conjuradora.
		if (!SOTOR.RFIntegration.ArcaneFocusGate.CanCastAtAll(agent)
			&& !SOTOR.RFIntegration.ArcaneFocusCasterSource.IsFocusCaster(agent))
		{
			return Reject(agent, "sem foco empunhado nem carregado");
		}

		AbilityComponent component = agent.GetComponent<AbilityComponent>();
		if (component == null)
		{
			// [RF-B] Os dois MissionLogic tem OnAgentBuild, e a ordem entre eles
			// depende da ordem de registro no SubModule — fragil demais para confiar.
			// Se o componente ainda nao existe e a tropa e conjuradora por foco,
			// criamos AQUI. Idempotente: o AbilityManager checa antes de criar o dele.
			if (!SOTOR.RFIntegration.ArcaneFocusCasterSource.IsFocusCaster(agent))
			{
				return Reject(agent, "sem AbilityComponent e nao e caster por foco");
			}

			agent.AddComponent(new AbilityComponent(agent));
			component = agent.GetComponent<AbilityComponent>();
			if (component == null)
			{
				return Reject(agent, "AbilityComponent nao pode ser criado");
			}
			SotorLog.Info($"CastingAI: AbilityComponent criado sob demanda para '{agent.Character?.StringId}'.");
		}

		if (component.KnownAbilitySystem.Count == 0)
		{
			return Reject(agent, "AbilityComponent vazio (repertorio do foco saiu sem feiticos)");
		}

		SotorLog.Info($"CastingAI: '{agent.Character?.StringId}' virou caster de IA com {component.KnownAbilitySystem.Count} feitico(s).");
		return true;
	}
}
