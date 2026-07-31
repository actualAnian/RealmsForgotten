using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public static class CommonAIFunctions
{
	private static int _rollCounter;

	public static Agent GetRandomAgent(Formation targetFormation)
	{
		if (targetFormation == null)
		{
			return null;
		}
		Agent medianAgent = targetFormation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: false, targetFormation.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false));
		if (medianAgent == null)
		{
			return null;
		}
		_rollCounter = (_rollCounter + 1) & 0x7FFFFFFF;
		float num = (float)((_rollCounter * 1103515245 + 12345) & 0x7FFFFFFF) / 2.1474836E+09f;
		float num2 = (float)((_rollCounter * 1140671485 + 12820163) & 0x7FFFFFFF) / 2.1474836E+09f;
		Vec3 position = medianAgent.Position;
		Vec2 estimatedDirection = targetFormation.QuerySystem.EstimatedDirection;
		Vec2 vec = estimatedDirection.RightVec();
		position += estimatedDirection.ToVec3() * (num * targetFormation.Depth - targetFormation.Depth / 2f);
		float num3 = targetFormation.Width * 0.9f;
		return targetFormation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: false, (position + vec.ToVec3() * (num2 * num3 - num3 / 2f)).AsVec2);
	}
}
