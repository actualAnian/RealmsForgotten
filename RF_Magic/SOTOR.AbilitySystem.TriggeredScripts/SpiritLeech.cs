using System.Collections.Generic;
using System.Linq;
using SOTOR.AbilitySystem.StatusEffects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.TriggeredScripts;

public class SpiritLeech : ITriggeredScript
{
	private const string HealEffectId = "spirit_leech_heal";

	public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration, TriggeredEffectTemplate template, string originSpell)
	{
		if (triggeredByAgent == null || !triggeredByAgent.IsActive())
		{
			return;
		}
		List<Agent> list = triggeredAgents?.Where((Agent a) => a?.IsActive() ?? false).ToList();
		if (list != null && list.Count != 0)
		{
			Agent agent = list.FirstOrDefault((Agent a) => a.Character is CharacterObject characterObject && characterObject.IsHero) ?? list.OrderByDescending((Agent a) => (a.Character as CharacterObject)?.Level ?? 0).First();
			int num = (agent.Character as CharacterObject)?.Tier ?? 1;
			if (num < 1)
			{
				num = 1;
			}
			float num2 = (float)num * duration;
			StatusEffectComponent component = triggeredByAgent.GetComponent<StatusEffectComponent>();
			if (component != null)
			{
				component.RunStatusEffect("spirit_leech_heal", triggeredByAgent, num2, append: true, originSpell);
				SotorLog.Info($"SpiritLeech: '{triggeredByAgent.Name}' drains '{agent.Name}' (tier {num}) -> heals self " + string.Format("'{0}' for {1:0.0}s.", "spirit_leech_heal", num2));
			}
		}
	}
}
