using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

public class BombardmentScript : AbilityScript
{
	private bool _impulseGiven;

	protected override void OnAfterTick(float dt)
	{
		if (!_impulseGiven && base.Ability != null && base.Ability.Template.TriggerType == TriggerType.OnCollision)
		{
			_impulseGiven = true;
			WeakGameEntity gameEntity = base.GameEntity;
			if (gameEntity.IsValid)
			{
				gameEntity.ApplyLocalImpulseToDynamicBody(gameEntity.CenterOfMass, new Vec3(0f, 0f, -100f));
			}
		}
		if (TryGetWaterCrossing(base.LastFrameGlobalPosition, base.CurrentGlobalPosition, out var crossPoint, out var _))
		{
			HandleCollision(crossPoint, Vec3.Up);
		}
	}

	protected override void HandleCollision(Vec3 position, Vec3 normal)
	{
		normal.RotateAboutX(90f.ToRadians());
		base.HandleCollision(position, normal);
	}
}
