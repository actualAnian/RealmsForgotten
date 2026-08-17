using System.Collections.Generic;
using SOTOR.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.TriggeredScripts;

public class Transform : ITriggeredScript
{
	public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration, TriggeredEffectTemplate template, string originSpell)
	{
		TransformationMissionLogic missionBehavior = Mission.Current?.GetMissionBehavior<TransformationMissionLogic>();
		if (missionBehavior == null || template == null)
		{
			return;
		}
		switch (template.TransformationTargetGroup)
		{
		case TransformationTargetGroup.Self:
			missionBehavior.TryTransform(triggeredByAgent, template.TransformationTroopId, duration);
			break;
		case TransformationTargetGroup.Companions:
			TransformCompanions(missionBehavior, triggeredByAgent, triggeredAgents, template, duration);
			break;
		case TransformationTargetGroup.RegularTroops:
			TransformRegularTroops(missionBehavior, triggeredAgents, template, duration);
			break;
		}
	}

	private static void TransformCompanions(TransformationMissionLogic missionBehavior, Agent caster, IEnumerable<Agent> targets, TriggeredEffectTemplate template, float duration)
	{
		if (targets == null)
		{
			return;
		}
		foreach (Agent target in targets)
		{
			Hero hero = target?.GetHero();
			if (target != caster && hero?.IsPlayerCompanion == true)
			{
				missionBehavior.TryTransform(target, template.TransformationTroopId, duration);
			}
		}
	}

	private static void TransformRegularTroops(TransformationMissionLogic missionBehavior, IEnumerable<Agent> targets, TriggeredEffectTemplate template, float duration)
	{
		if (targets == null)
		{
			return;
		}
		float chance = MBMath.ClampFloat(template.RegularTroopTransformationPercent, 0f, 100f);
		foreach (Agent target in targets)
		{
			if (target != null && target.IsHuman && target.GetHero() == null && MBRandom.RandomFloat * 100f < chance)
			{
				missionBehavior.TryTransform(target, template.TransformationTroopId, duration);
			}
		}
	}
}
