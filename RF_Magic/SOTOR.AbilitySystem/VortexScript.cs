using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class VortexScript : AbilityScript
{
	private float _counter = 1f;

	private float _maxDeviation;

	private float _currentDeviation;

	public override void Initialize(Ability ability, ref GameEntity entity)
	{
		base.Initialize(ability, ref entity);
		_maxDeviation = ability.Template.MaxRandomDeviation;
	}

	protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
	{
		MatrixFrame matrixFrame = new MatrixFrame(in oldFrame.rotation, in oldFrame.origin);
		if (_counter >= 1f)
		{
			_counter = 0f;
			_currentDeviation = MBRandom.RandomFloatRanged(0f - _maxDeviation, _maxDeviation) * dt;
		}
		else
		{
			_counter += dt;
		}
		matrixFrame.rotation.RotateAboutUp(_currentDeviation);
		matrixFrame.Advance(base.Ability.Template.BaseMovementSpeed * dt);
		float groundHeightAtPosition;
		float waterLevelAtPosition;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(matrixFrame.origin);
			waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(matrixFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		}
		matrixFrame.origin.z = Math.Max(groundHeightAtPosition, waterLevelAtPosition) + base.Ability.Template.Offset;
		oldFrame.origin = matrixFrame.origin;
		return oldFrame;
	}
}
