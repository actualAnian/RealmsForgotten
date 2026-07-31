using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public static class SkeletonVoice
{
	public const string SkeletonCultureId = "sotor_skeleton";

	private const string BoneRattleSound = "sotor_skeleton_bonerattle";

	public static bool IsSkeleton(Agent agent)
	{
		BasicCultureObject basicCultureObject = agent?.Character?.Culture;
		if (basicCultureObject != null)
		{
			return basicCultureObject.StringId == "sotor_skeleton";
		}
		return false;
	}

	public static void PlayRattle(Agent agent)
	{
		if (agent != null && Mission.Current != null)
		{
			int eventIdFromString = SoundEvent.GetEventIdFromString("sotor_skeleton_bonerattle");
			if (eventIdFromString >= 0)
			{
				Mission.Current.MakeSound(eventIdFromString, agent.Position, soundCanBePredicted: false, isReliable: true, agent.Index, -1);
			}
		}
	}
}
