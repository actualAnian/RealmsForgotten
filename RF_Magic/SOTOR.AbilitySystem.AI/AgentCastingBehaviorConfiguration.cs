using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public static class AgentCastingBehaviorConfiguration
{
	public static readonly Dictionary<AbilityEffectType, Func<Agent, int, AbilityTemplate, AbstractAgentCastingBehavior>> BehaviorByType = new Dictionary<AbilityEffectType, Func<Agent, int, AbilityTemplate, AbstractAgentCastingBehavior>>
	{
		{
			AbilityEffectType.Blast,
			(Agent a, int i, AbilityTemplate t) => new AoETargetedCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.Bombardment,
			(Agent a, int i, AbilityTemplate t) => new AoETargetedCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.Vortex,
			(Agent a, int i, AbilityTemplate t) => new AoETargetedCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.Heal,
			delegate(Agent a, int i, AbilityTemplate t)
			{
				if (t.AbilityTargetType == AbilityTargetType.AlliesInAOE)
				{
					return new SelectMultiTargetCastingBehavior(a, t, i);
				}
				return (t.AbilityTargetType != AbilityTargetType.Self && t.AbilityTargetType != AbilityTargetType.SingleAlly) ? ((AoETargetedCastingBehavior)new SelectMultiTargetCastingBehavior(a, t, i)) : ((AoETargetedCastingBehavior)new SelectSingleTargetCastingBehavior(a, t, i));
			}
		},
		{
			AbilityEffectType.Hex,
			delegate(Agent a, int i, AbilityTemplate t)
			{
				if (t.AbilityTargetType == AbilityTargetType.EnemiesInAOE)
				{
					return new SelectMultiTargetCastingBehavior(a, t, i);
				}
				return (t.AbilityTargetType != AbilityTargetType.SingleEnemy) ? new AoETargetedCastingBehavior(a, t, i) : new SelectSingleTargetCastingBehavior(a, t, i);
			}
		},
		{
			AbilityEffectType.Augment,
			delegate(Agent a, int i, AbilityTemplate t)
			{
				if (t.AbilityTargetType == AbilityTargetType.AlliesInAOE)
				{
					return new SelectMultiTargetCastingBehavior(a, t, i);
				}
				return (t.AbilityTargetType != AbilityTargetType.Self && t.AbilityTargetType != AbilityTargetType.SingleAlly) ? ((AoETargetedCastingBehavior)new SelectMultiTargetCastingBehavior(a, t, i)) : ((AoETargetedCastingBehavior)new SelectSingleTargetCastingBehavior(a, t, i));
			}
		},
		{
			AbilityEffectType.Missile,
			(Agent a, int i, AbilityTemplate t) => new MissileCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.SeekerMissile,
			(Agent a, int i, AbilityTemplate t) => new MissileCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.Summoning,
			(Agent a, int i, AbilityTemplate t) => new SummoningCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.Wind,
			(Agent a, int i, AbilityTemplate t) => new AoEDirectionalCastingBehavior(a, t, i)
		},
		{
			AbilityEffectType.MindControl,
			(Agent a, int i, AbilityTemplate t) => new AoETargetedCastingBehavior(a, t, i)
		}
	};

	public static readonly Dictionary<Type, Func<AbstractAgentCastingBehavior, List<Axis>>> UtilityByType = new Dictionary<Type, Func<AbstractAgentCastingBehavior, List<Axis>>>
	{
		{
			typeof(PreserveWindsAgentCastingBehavior),
			CreatePreserveWindsAxis()
		},
		{
			typeof(MissileCastingBehavior),
			CreateAoETargetedOffensiveSpellAxis()
		},
		{
			typeof(AoETargetedCastingBehavior),
			CreateAoETargetedOffensiveSpellAxis()
		},
		{
			typeof(AoEDirectionalCastingBehavior),
			CreateAoEDirectionalSpellAxis()
		},
		{
			typeof(SelectMultiTargetCastingBehavior),
			CreateBuffSpellAxis()
		},
		{
			typeof(SelectSingleTargetCastingBehavior),
			CreateBuffSpellAxis()
		},
		{
			typeof(SummoningCastingBehavior),
			CreateSummoningAxis()
		}
	};

	public static List<Target> FindTargets(Agent agent, AbilityTemplate abilityTemplate)
	{
		if (abilityTemplate.AbilityTargetType == AbilityTargetType.AlliesInAOE || abilityTemplate.AbilityEffectType == AbilityEffectType.Heal || abilityTemplate.AbilityTargetType == AbilityTargetType.SingleAlly)
		{
			return (from form in agent.Team.GetAllyTeams().SelectMany((Team team) => team.GetFormations()).Where(IsValidFormationTarget)
				select new Target
				{
					Formation = form
				}).ToList();
		}
		if (abilityTemplate.AbilityEffectType == AbilityEffectType.Summoning || abilityTemplate.AbilityTargetType == AbilityTargetType.Self)
		{
			return new List<Target>
			{
				new Target
				{
					Formation = agent.Formation,
					Agent = agent
				}
			};
		}
		try
		{
			return (from form in agent.Team.GetEnemyTeams().SelectMany((Team team) => team.GetFormations()).Where(IsValidFormationTarget)
				select new Target
				{
					Formation = form
				}).ToList();
		}
		catch (Exception)
		{
			return new List<Target>();
		}
	}

	public static List<AbstractAgentCastingBehavior> PrepareCastingBehaviors(Agent agent)
	{
		List<AbstractAgentCastingBehavior> list = new List<AbstractAgentCastingBehavior>();
		int num = 0;
		AbilityComponent component = agent.GetComponent<AbilityComponent>();
		if (component != null)
		{
			foreach (AbilityTemplate knownAbilityTemplate in component.GetKnownAbilityTemplates())
			{
				Func<Agent, int, AbilityTemplate, AbstractAgentCastingBehavior> value;
				Func<Agent, int, AbilityTemplate, AbstractAgentCastingBehavior> func = (BehaviorByType.TryGetValue(knownAbilityTemplate.AbilityEffectType, out value) ? value : BehaviorByType[AbilityEffectType.Missile]);
				list.Add(func(agent, num, knownAbilityTemplate));
				num++;
			}
		}
		list.Add(new PreserveWindsAgentCastingBehavior(agent, new AbilityTemplate
		{
			AbilityTargetType = AbilityTargetType.Self
		}, num));
		return list;
	}

	private static Func<AbstractAgentCastingBehavior, List<Axis>> CreateSummoningAxis()
	{
		return (AbstractAgentCastingBehavior behavior) => new List<Axis>
		{
			new Axis(0f, 1f, (float x) => 1f - x, CommonAIDecisionFunctions.BalanceOfPower(behavior.Agent))
		};
	}

	private static Func<AbstractAgentCastingBehavior, List<Axis>> CreatePreserveWindsAxis()
	{
		return (AbstractAgentCastingBehavior behavior) => new List<Axis>
		{
			// [RF-B] "Poupar mana" so deve competir quando a mana esta REALMENTE baixa.
			//
			// O original era min(0.4, 1 - restante): CRESCIA a cada gasto e saturava em
			// 0.40 com 60% de pool ainda no bolso. Um feitico bom pontua ~0.39, entao a
			// tropa CONGELAVA depois de dois ou tres lancamentos e passava a batalha
			// parada — o sintoma "so alguns magos lancam, mesmo todos em boa distancia".
			//
			// Agora: zero enquanto houver mais de 35% de mana (nesse regime conjurar e
			// sempre melhor que poupar), subindo dai ate 0.25 no fundo do pool. O teto
			// menor garante que poupar nunca vence um feitico bem pontuado; serve para
			// desempatar contra opcoes ruins quando a reserva esta no fim.
			new Axis(0f, 1f, (float x) => (x >= PreserveWindsThreshold) ? 0f : (PreserveWindsCeiling * (1f - x / PreserveWindsThreshold)), CommonAIDecisionFunctions.WindsOfMagicRemainingRatio(behavior.Agent))
		};
	}

	public static Func<AbstractAgentCastingBehavior, List<Axis>> CreateAoETargetedOffensiveSpellAxis()
	{
		return (AbstractAgentCastingBehavior behavior) => new List<Axis>
		{
			// [RF-B] SITUACAO: o feitico e julgado pelo ALCANCE DELE, nao pela classe
			// que o implementa. Veta quando nada esta ao alcance. Ver RFSpellProfile.
			new Axis(0f, 1f, (float x) => x, (Target _) => SOTOR.RFIntegration.RFSpellProfile.Opportunity(behavior.Agent, behavior.AbilityTemplate)),
			// [RF-B] Fogo amigo: nao lancar area onde a propria linha esta dentro.
			new Axis(0f, 1f, (float x) => x, (Target t) => SOTOR.RFIntegration.RFSpellProfile.IsAreaEffect(behavior.AbilityTemplate)
				? SOTOR.RFIntegration.RFSpellProfile.AreaWorth(behavior.Agent, behavior.AbilityTemplate, t.GetPositionPrioritizeCalculated())
				: 1f),
			new Axis(0f, 120f, (float x) => (1f - x) * (1f - x), CommonAIDecisionFunctions.DistanceToTarget(() => behavior.Agent.Position)),
			new Axis(0f, PowerScale(CommonAIDecisionFunctions.CalculateEnemyTotalPower(behavior.Agent.Team) / 4f), (float x) => x, CommonAIDecisionFunctions.FormationPower()),
			new Axis(0f, 1f, (float x) => x + 0.3f, CommonAIDecisionFunctions.RangedUnitRatio())
		};
	}

	public static Func<AbstractAgentCastingBehavior, List<Axis>> CreateBuffSpellAxis()
	{
		return (AbstractAgentCastingBehavior behavior) => new List<Axis>
		{
			// [RF-B] SITUACAO: defesa vale quando ha contato iminente, flecha caindo ou
			// cavalaria vindo. Zero no vazio — e o que impede o buff lancado no inicio
			// da batalha, longe de tudo. Um Augment OFENSIVO cai no eixo de alcance.
			new Axis(0f, 1f, (float x) => x, (Target _) => SOTOR.RFIntegration.RFSpellProfile.Opportunity(behavior.Agent, behavior.AbilityTemplate)),
			new Axis(0f, 50f, (float x) => ScoringFunctions.Logistic(0.4f, 1f, 20f)(1f - x), CommonAIDecisionFunctions.DistanceToTarget(() => behavior.Agent.Position)),
			new Axis(0f, 20f, (float x) => 1f - x, CommonAIDecisionFunctions.TargetDistanceToHostiles()),
			new Axis(0f, PowerScale(CommonAIDecisionFunctions.CalculateTeamTotalPower(behavior.Agent.Team)), (float x) => x, CommonAIDecisionFunctions.FormationPower()),
			new Axis(1f, 2.5f, (float x) => 1f - x, CommonAIDecisionFunctions.Dispersedness())
		};
	}

	public static Func<AbstractAgentCastingBehavior, List<Axis>> CreateAoEDirectionalSpellAxis()
	{
		return (AbstractAgentCastingBehavior behavior) => new List<Axis>
		{
			// [RF-B] SITUACAO: sopro/cone e arma de PERTO — o perfil ja mede isso pelo
			// offset+raio do proprio template, sem supor nada pela classe.
			new Axis(0f, 1f, (float x) => x, (Target _) => SOTOR.RFIntegration.RFSpellProfile.Opportunity(behavior.Agent, behavior.AbilityTemplate)),
			// [RF-B] Fogo amigo: o cone sai do conjurador e varre a propria linha.
			new Axis(0f, 1f, (float x) => x, (Target t) => SOTOR.RFIntegration.RFSpellProfile.AreaWorth(behavior.Agent, behavior.AbilityTemplate, t.GetPositionPrioritizeCalculated())),
			new Axis(0f, 50f, (float x) => ScoringFunctions.Logistic(0.4f, 1f, 20f)(1f - x), CommonAIDecisionFunctions.DistanceToTarget(() => behavior.Agent.Position)),
			new Axis(0f, 15f, (float x) => 1f - x, CommonAIDecisionFunctions.TargetDistanceToHostiles()),
			new Axis(0f, PowerScale(CommonAIDecisionFunctions.CalculateEnemyTotalPower(behavior.Agent.Team)), (float x) => x, CommonAIDecisionFunctions.FormationPower()),
			new Axis(1f, 2.5f, (float x) => 1f - x, CommonAIDecisionFunctions.Dispersedness()),
			new Axis(0f, 1f, (float x) => 1f - x, CommonAIDecisionFunctions.CavalryUnitRatio())
		};
	}

	/// <summary>
	/// [RF-B] Piso 1 para o TETO dos eixos de poder.
	///
	/// Os eixos normalizam o poder da formacao alvo pelo poder total do time. Em
	/// escaramuca pequena — ou no instante em que o exercito inimigo ainda nao
	/// spawnou — esse total chega a 0, o teto do eixo vira 0 e o eixo devolve 0.
	/// Como a utilidade final e MEDIA GEOMETRICA, um unico eixo zero zera tudo: o
	/// mago simplesmente nunca conjurava. Com piso 1 o eixo degrada em vez de
	/// anular.
	/// </summary>
	private static float PowerScale(float raw)
	{
		return Math.Max(1f, raw);
	}

	/// <summary>Abaixo desta fracao de mana, poupar comeca a valer alguma coisa.</summary>
	private const float PreserveWindsThreshold = 0.35f;

	/// <summary>Teto de "poupar mana" — abaixo da nota de um feitico bem pontuado.</summary>
	private const float PreserveWindsCeiling = 0.25f;

	private static bool IsValidFormationTarget(Formation formation)
	{
		if (formation == null)
		{
			return false;
		}
		try
		{
			if (formation.QuerySystem == null)
			{
				return false;
			}
			if (formation.CountOfUnits > 0)
			{
				return true;
			}
			return formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, formation.CurrentPosition) != null;
		}
		catch (NullReferenceException)
		{
			return false;
		}
	}
}
