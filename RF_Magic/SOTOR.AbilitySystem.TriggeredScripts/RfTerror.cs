using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.TriggeredScripts;

/// <summary>
/// TERROR — quebra a moral em vez de ferir o corpo.
///
/// Resgata o único efeito do sistema de magia antigo do RealmsForgotten que o
/// motor não tinha equivalente: lá, <c>WeaponEffectConsequences.Terror</c> fazia
/// <c>affectedAgent.ChangeMorale(-25)</c> e avisava o jogador quando o inimigo
/// estava prestes a debandar. Nenhum dos 86 feitiços do motor mexe em moral
/// (varri <c>tor_triggeredeffects.xml</c>: não há um único campo de moral), então
/// isto entra como script, não como dado.
///
/// Diferenças deliberadas em relação ao original:
/// * O original atingia UM alvo por golpe; aqui o efeito é de ÁREA — é um feitiço
///   conjurado, não uma arma que acerta um sujeito. Quem entra na área é atingido.
/// * A perda de moral escala com a **bravura** do alvo: tropas de tier alto
///   resistem mais, então aterrorizar uma linha de recrutas rende mais do que
///   assustar veteranos. Isso dá ao feitiço um papel tático (quebrar a ala fraca)
///   em vez de ser dano disfarçado.
/// * Heróis levam metade — um lorde não foge de um crânio flamejante.
/// </summary>
public class RfTerror : ITriggeredScript
{
	/// <summary>Perda de moral base, igual à do sistema antigo.</summary>
	private const float BaseMoraleLoss = 25f;

	/// <summary>Quanto cada tier de tropa reduz o terror sofrido.</summary>
	private const float ResistPerTier = 0.08f;

	public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration, TriggeredEffectTemplate template, string originSpell)
	{
		if (triggeredByAgent == null || !triggeredByAgent.IsActive())
		{
			return;
		}

		List<Agent> targets = triggeredAgents?.Where((Agent a) => a != null && a.IsActive() && a.IsHuman && a.IsEnemyOf(triggeredByAgent)).ToList();
		if (targets == null || targets.Count == 0)
		{
			return;
		}

		int routed = 0;
		foreach (Agent target in targets)
		{
			float loss = BaseMoraleLoss;

			// Tropa mais experiente encara melhor o horror.
			if (target.Character is CharacterObject character)
			{
				float resist = 1f - character.Tier * ResistPerTier;
				if (resist < 0.4f)
				{
					resist = 0.4f;
				}
				loss *= resist;
				if (character.IsHero)
				{
					loss *= 0.5f;
				}
			}

			target.ChangeMorale(0f - loss);
			if (target.GetMorale() <= 0f)
			{
				routed++;
			}
		}

		// Feedback só para o jogador conjurador — o mesmo gesto do sistema antigo,
		// que avisava quando o inimigo estava a um passo de debandar.
		if (triggeredByAgent.IsMainAgent)
		{
			TextObject message = ((routed > 0)
				? new TextObject("{=rf_terror_routed}Terror takes hold — {COUNT} enemies break!")
				: new TextObject("{=rf_terror_shaken}{COUNT} enemies are shaken by terror."));
			message.SetTextVariable("COUNT", (routed > 0) ? routed : targets.Count);
			InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Magenta));
		}

		SotorLog.Info($"RfTerror ({originSpell}): {targets.Count} enemy(ies) hit, {routed} routed.");
	}
}
