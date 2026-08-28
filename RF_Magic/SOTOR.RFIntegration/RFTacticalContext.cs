using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.RFIntegration;

/// <summary>
/// A SITUACAO do conjurador, para que a escolha do feitico deixe de ser so "qual
/// alvo rende mais" e passe a ser "o que faz sentido AGORA".
///
/// Os eixos do SOTOR avaliam o ALVO (poder da formacao, dispersao, distancia ate
/// ela). Nenhum deles olha para o proprio mago: se ele esta sendo flechado, se
/// tem cavalaria em cima, se ja esta no corpo a corpo. O resultado in-game foi um
/// mago que lancava protecao no comeco da batalha, longe de tudo, e depois nao
/// trocava para ataque quando o inimigo chegava.
///
/// Regra que estas medidas servem, nas palavras do autor:
///   - prestes a lutar, sob flecha ou com cavalaria vindo -> DEFESA importa
///   - a distancia de tiro                                -> ATAQUE A DISTANCIA
///   - prestes a engajar corpo a corpo                    -> feitico CURTO, se houver
///
/// Tudo aqui e leitura barata e sem estado: distancia ao inimigo mais proximo,
/// contagem de projeteis por perto, e presenca de montado. Nada de raycast, nada
/// de varrer a missao inteira por agente.
/// </summary>
public static class RFTacticalContext
{
	/// <summary>Ate aqui o mago esta praticamente em contato.</summary>
	public const float MeleeRange = 6f;

	/// <summary>Faixa em que um feitico de tiro rende mais.</summary>
	public const float RangedSweetSpot = 35f;

	/// <summary>Raio para procurar ameaca montada.</summary>
	private const float CavalryWatchRange = 25f;

	/// <summary>
	/// Distancia ate o inimigo mais proximo. <see cref="float.MaxValue" /> quando
	/// nao ha nenhum — o chamador decide o que isso significa.
	/// </summary>
	public static float DistanceToNearestEnemy(Agent agent)
	{
		if (agent == null || Mission.Current == null || agent.Team == null)
		{
			return float.MaxValue;
		}

		try
		{
			float best = float.MaxValue;
			foreach (Agent other in Mission.Current.Agents)
			{
				if (other == null || !other.IsActive() || !other.IsHuman || other == agent)
				{
					continue;
				}
				if (other.Team == null || !other.Team.IsValid || agent.Team == null || !agent.Team.IsValid || !other.Team.IsEnemyOf(agent.Team))
				{
					continue;
				}
				float d = agent.Position.Distance(other.Position);
				if (d < best)
				{
					best = d;
				}
			}
			return best;
		}
		catch (Exception)
		{
			return float.MaxValue;
		}
	}

	/// <summary>Ja esta em contato (ou a um passo dele)?</summary>
	public static bool IsInMelee(Agent agent)
	{
		return DistanceToNearestEnemy(agent) <= MeleeRange;
	}

	/// <summary>
	/// Ha ameaca montada perto? Cavalaria chegando e o caso classico em que uma
	/// protecao vale mais que um projetil — o mago nao vai vencer a corrida.
	/// </summary>
	public static bool IsCavalryThreatening(Agent agent)
	{
		if (agent == null || Mission.Current == null || agent.Team == null)
		{
			return false;
		}

		try
		{
			foreach (Agent other in Mission.Current.Agents)
			{
				if (other == null || !other.IsActive() || !other.HasMount || other == agent)
				{
					continue;
				}
				if (other.Team == null || !other.Team.IsValid || !agent.Team.IsValid || !other.Team.IsEnemyOf(agent.Team))
				{
					continue;
				}
				if (agent.Position.Distance(other.Position) <= CavalryWatchRange)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	/// <summary>
	/// A formacao do mago esta sob fogo? Usa o proprio contador do jogo, o mesmo
	/// que a IA de formacao consulta — sem inventar deteccao.
	/// </summary>
	public static bool IsUnderMissileFire(Agent agent)
	{
		try
		{
			Formation formation = agent?.Formation;
			return formation?.QuerySystem != null && formation.QuerySystem.UnderRangedAttackRatio > 0.05f;
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Quanto uma DEFESA (buff/cura) faz sentido agora, de 0 a 1.
	///
	/// Sobe com contato iminente, fogo de flecha e cavalaria. Fica em zero quando
	/// nao ha nada acontecendo — e esse zero e proposital: veta o buff lancado no
	/// vazio no comeco da batalha.
	/// </summary>
	public static float DefensiveNeed(Agent agent)
	{
		float need = 0f;

		float d = DistanceToNearestEnemy(agent);
		if (d <= MeleeRange * 3f)
		{
			// 0 a 18m -> 1.0 no contato, caindo ate 0 na borda
			need = Math.Max(need, 1f - (d / (MeleeRange * 3f)));
		}

		if (IsUnderMissileFire(agent))
		{
			need = Math.Max(need, 0.75f);
		}

		if (IsCavalryThreatening(agent))
		{
			need = Math.Max(need, 0.85f);
		}

		return MBMath.ClampFloat(need, 0f, 1f);
	}

	// RangedOpportunity / CloseQuartersOpportunity viviam aqui. Foram removidos:
	// classificavam o feitico pela CLASSE do comportamento em C#, entao um Ice
	// Purge de 9m de alcance recebia a curva de um projetil de 100m. Quem faz esse
	// julgamento agora e RFSpellProfile.Opportunity, lendo alcance e raio do
	// proprio template. Uma fonte de verdade so.
}
