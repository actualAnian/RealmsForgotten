using System;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SOTOR.RFIntegration;

/// <summary>
/// FASE 1 — a magia passa a progredir pela skill <c>arcane</c> do RealmsForgotten,
/// em vez da <c>SotorSpellcraft</c> propria do SOTOR.
///
/// Por que aqui e nao no SotorSkills: a regra do projeto e que arquivo do SOTOR
/// so recebe gancho de uma linha; toda a logica RF vive nesta pasta. O gancho
/// esta em <c>SotorSkills.Spellcraft</c>, que e o FUNIL UNICO — os 13 pontos que
/// usam a skill leem essa propriedade, entao trocar a resolucao num lugar cobre
/// o sistema inteiro (dano, tiers, perks, grimorio, cemiterio, raise dead).
///
/// Por que a resolucao e PREGUICOSA (e nao no construtor do SotorSkills):
/// a ordem de registro e desfavoravel e foi verificada no codigo —
///   SOTOR: postfix de <c>Game.InitializeDefaultGameObjects</c>
///   RF:    <c>new RFSkills().Initialize()</c> dentro de OnGameStart
/// O SOTOR registra ANTES, quando 'arcane' ainda nao existe. Resolver no
/// construtor daria null para sempre. Por isso resolvemos na primeira leitura e
/// so guardamos em cache QUANDO ACHAMOS — assim uma leitura precoce nao envenena
/// o cache com o fallback.
///
/// Fallback: se 'arcane' nao existir (RealmsForgotten ausente), devolve a
/// SotorSpellcraft que o SOTOR registrou. O modulo continua jogavel sozinho.
/// </summary>
public static class RFArcaneSkill
{
	/// <summary>StringId da skill do RF (RFSkills.Initialize: new SkillObject("arcane")).</summary>
	public const string ArcaneSkillId = "arcane";

	private static SkillObject _resolved;

	private static bool _loggedResolved;

	private static bool _loggedFallback;

	/// <summary>
	/// Skill que governa a magia. Devolve a 'arcane' do RF quando ela existir,
	/// senao o <paramref name="sotorFallback" /> (SotorSpellcraft).
	/// </summary>
	public static SkillObject Resolve(SkillObject sotorFallback)
	{
		if (_resolved != null)
		{
			return _resolved;
		}

		try
		{
			SkillObject found = MBObjectManager.Instance?.GetObject<SkillObject>(ArcaneSkillId);

			if (found == null)
			{
				// Plano B: varredura por StringId. E o padrao usado no resto do RF
				// e nao depende de a skill estar no indice do object manager.
				foreach (SkillObject skill in Skills.All)
				{
					if (skill != null && string.Equals(skill.StringId, ArcaneSkillId, StringComparison.OrdinalIgnoreCase))
					{
						found = skill;
						break;
					}
				}
			}

			if (found != null)
			{
				_resolved = found;
				if (!_loggedResolved)
				{
					_loggedResolved = true;
					SotorLog.Info("RFArcaneSkill: magia agora progride pela skill '" + ArcaneSkillId + "' do RealmsForgotten.");
				}
				return _resolved;
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("RFArcaneSkill: resolucao falhou (" + ex.GetType().Name + "); usando SotorSpellcraft.");
		}

		if (!_loggedFallback)
		{
			_loggedFallback = true;
			SotorLog.Info("RFArcaneSkill: '" + ArcaneSkillId + "' ainda nao registrada — usando SotorSpellcraft por ora.");
		}
		return sotorFallback;
	}

	/// <summary>Solta o cache. Necessario entre partidas: SkillObject e por jogo.</summary>
	public static void Reset()
	{
		_resolved = null;
		_loggedResolved = false;
		_loggedFallback = false;
	}
}
