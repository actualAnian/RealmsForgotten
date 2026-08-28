using System;
using SOTOR.AbilitySystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// O QUE UM FEITICO E, do ponto de vista de quem decide lanca-lo.
///
/// O SOTOR escolhe os eixos de utilidade pela CLASSE de comportamento em C#
/// (AgentCastingBehaviorConfiguration.UtilityByType). Isso junta numa unica
/// cesta coisas que nao tem nada a ver: Blast, Bombardment, Vortex, MindControl
/// e Missile compartilham o mesmo conjunto de eixos. Na pratica um Ice Purge de
/// raio 5 era pontuado como se fosse um raio de 100m, e a IA o lancava a 15m do
/// inimigo — longe demais para o efeito tocar em alguem.
///
/// Aqui a classificacao vem do TEMPLATE: alcance, raio, se e ofensivo ou de
/// protecao. Dois feiticos com o mesmo AbilityEffectType podem receber notas
/// completamente diferentes se um alcanca 9m e o outro 100m, que e como deveria
/// ser.
///
/// Regra que estas medidas servem, nas palavras do autor:
///   - prestes a lutar, sob flecha ou com cavalaria vindo -> DEFESA importa
///   - a distancia de tiro                                -> ATAQUE A DISTANCIA
///   - prestes a engajar corpo a corpo                    -> feitico CURTO
/// </summary>
public static class RFSpellProfile
{
	/// <summary>Abaixo disto um feitico e "de perto" e nao serve para trocar tiro.</summary>
	public const float ShortRangeCutoff = 20f;

	/// <summary>
	/// Ate onde o efeito REALMENTE toca alguem, em metros.
	///
	/// Nao e MaxDistance. MaxDistance e a permissao de mira; o que mata e onde a
	/// area cai. Para um feitico de area centrado no ponto mirado, o alcance util
	/// e a mira mais o raio da explosao.
	/// </summary>
	public static float EffectiveReach(AbilityTemplate template)
	{
		if (template == null)
		{
			return 0f;
		}

		float aimRange = (template.MaxDistanceSpecified && template.MaxDistance > 0f)
			? template.MaxDistance
			: 0f;

		switch (template.AbilityEffectType)
		{
			case AbilityEffectType.Missile:
			case AbilityEffectType.SeekerMissile:
				// Projetil: viaja. O alcance e o de mira.
				return aimRange;

			case AbilityEffectType.Wind:
				// Cone que sai do conjurador: alcanca o offset mais o raio, e nunca
				// mais que a permissao de mira.
				return Math.Min(Math.Max(aimRange, 1f), template.Offset + template.Radius);

			default:
				// Area posta sobre o alvo: a borda da explosao estende o alcance util.
				return aimRange + template.Radius;
		}
	}

	/// <summary>Feitico de socorro/protecao — julgado pela AMEACA, nao pelo alvo.</summary>
	public static bool IsDefensive(AbilityTemplate template)
	{
		if (template == null)
		{
			return false;
		}
		AbilityEffectType effect = template.AbilityEffectType;
		if (effect == AbilityEffectType.Heal)
		{
			return true;
		}
		if (effect != AbilityEffectType.Augment)
		{
			return false;
		}
		// Augment tambem serve para AMPLIAR quem ataca; so conta como defesa quando
		// recai sobre o proprio lado.
		AbilityTargetType target = template.AbilityTargetType;
		return target == AbilityTargetType.Self
			|| target == AbilityTargetType.SingleAlly
			|| target == AbilityTargetType.AlliesInAOE;
	}

	/// <summary>Feitico de perto: o mago tem de estar em cima para valer alguma coisa.</summary>
	public static bool IsShortRanged(AbilityTemplate template)
	{
		return EffectiveReach(template) < ShortRangeCutoff;
	}

	/// <summary>
	/// Quanto ESTE feitico faz sentido AGORA, de 0 a 1 — o eixo situacional.
	///
	/// Zero e um veto deliberado (a utilidade final e media geometrica): serve
	/// para dizer "esta ferramenta nao alcanca nada daqui", que e exatamente o
	/// caso que fazia o mago gastar a decisao inteira num feitico inutil e passar
	/// a rodada parado.
	/// </summary>
	public static float Opportunity(Agent agent, AbilityTemplate template)
	{
		if (agent == null || template == null)
		{
			return 0f;
		}

		if (IsDefensive(template))
		{
			// Protecao vale pela ameaca sobre o conjurador, nunca pela distancia
			// ate uma formacao inimiga qualquer.
			return RFTacticalContext.DefensiveNeed(agent);
		}

		float distance = RFTacticalContext.DistanceToNearestEnemy(agent);
		if (distance == float.MaxValue)
		{
			return 0f; // ninguem para acertar
		}

		float reach = EffectiveReach(template);
		if (reach <= 0f)
		{
			return 0f;
		}

		// VETO: nem o inimigo mais proximo esta ao alcance deste feitico. Um Ice
		// Purge de 9m com o inimigo a 15m cai aqui — antes ele era lancado assim
		// mesmo e registrava "considered=0 applied=0".
		if (distance > reach)
		{
			return 0f;
		}

		if (IsShortRanged(template))
		{
			// Arma de perto: quanto mais dentro do alcance, melhor. Perde valor na
			// borda, onde metade do inimigo fica de fora da area.
			return MBMath.ClampFloat(1f - 0.5f * (distance / reach), 0.3f, 1f);
		}

		// Arma de longe: pessima com o inimigo colado — o mago devia estar se
		// defendendo ou usando algo curto, nao mirando um projetil a queima-roupa.
		if (distance <= RFTacticalContext.MeleeRange)
		{
			return 0.15f;
		}
		if (distance <= RFTacticalContext.MeleeRange * 2f)
		{
			return 0.5f;
		}
		return 1f;
	}

	/// <summary>
	/// A area vale a pena? Compara inimigos e aliados dentro do raio do efeito.
	///
	/// Devolve 0 quando a explosao pegaria tanto aliado quanto inimigo — o mago
	/// nao pode ser a maior ameaca a propria linha. No log da batalha um
	/// windblast_burst atingiu 17 agentes com metade do proprio lado, e um
	/// boltofaqshy_explosion MATOU um Mage Initiate Elite amigo.
	/// </summary>
	public static float AreaWorth(Agent caster, AbilityTemplate template, Vec3 aimPosition)
	{
		if (caster == null || template == null || aimPosition == Vec3.Invalid)
		{
			return 1f;
		}

		// ONDE o efeito cai de fato. Um cone sai do conjurador e varre para a
		// frente, entao a checagem tem de cobrir o corredor entre ele e o alvo —
		// nao um circulo em volta do inimigo, que deixaria a propria linha de fora
		// da conta justamente no caso em que ela e atingida.
		Vec3 center;
		float radius;
		if (template.AbilityEffectType == AbilityEffectType.Wind)
		{
			float reach = EffectiveReach(template);
			Vec3 forward = aimPosition - caster.Position;
			forward.z = 0f;
			float length = forward.Length;
			center = (length > 0.01f)
				? caster.Position + forward * (Math.Min(reach, length) * 0.5f / length)
				: caster.Position;
			radius = Math.Max(reach * 0.5f, template.Radius);
		}
		else
		{
			center = aimPosition;
			radius = Math.Max(template.Radius, 1f);
		}

		int enemies = 0;
		int allies = 0;

		try
		{
			foreach (Agent other in Mission.Current.Agents)
			{
				if (other == null || !other.IsActive() || !other.IsHuman || other.Team == null || !other.Team.IsValid || other == caster)
				{
					continue;
				}
				if (caster.Team == null || !caster.Team.IsValid)
				{
					continue;
				}
				if (other.Position.Distance(center) > radius)
				{
					continue;
				}
				if (other.Team.IsEnemyOf(caster.Team))
				{
					enemies++;
				}
				else
				{
					allies++;
				}
			}
		}
		catch (Exception)
		{
			return 1f; // sem leitura confiavel, nao e este eixo que deve vetar
		}

		if (enemies == 0)
		{
			return 0f; // area vazia de inimigos: puro desperdicio de mana
		}
		if (allies >= enemies)
		{
			return 0f; // pegaria o proprio lado tanto quanto o inimigo
		}

		// Proporcional a limpeza do tiro: 1.0 sem nenhum aliado dentro.
		return MBMath.ClampFloat(1f - (float)allies / enemies, 0.2f, 1f);
	}

	/// <summary>Feiticos cujo efeito ocupa area e portanto podem pegar aliados.</summary>
	public static bool IsAreaEffect(AbilityTemplate template)
	{
		if (template == null || template.Radius <= 1f)
		{
			return false;
		}
		switch (template.AbilityEffectType)
		{
			case AbilityEffectType.Blast:
			case AbilityEffectType.Bombardment:
			case AbilityEffectType.Vortex:
			case AbilityEffectType.Wind:
				return true;
			default:
				return false;
		}
	}
}
