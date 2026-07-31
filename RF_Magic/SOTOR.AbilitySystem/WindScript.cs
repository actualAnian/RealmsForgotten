using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class WindScript : AbilityScript
{
	protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
	{
		MatrixFrame nextGlobalFrame = base.GetNextGlobalFrame(oldFrame, dt);
		float groundHeightAtPosition;
		float waterLevelAtPosition;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(nextGlobalFrame.origin);
			waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(nextGlobalFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		}
		nextGlobalFrame.origin.z = Math.Max(groundHeightAtPosition, waterLevelAtPosition) + base.Ability.Template.Radius / 2f;
		return nextGlobalFrame;
	}
}
