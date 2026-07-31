using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public class SotorTarget
{
	public Agent Agent { get; set; }

	public bool IsValid
	{
		get
		{
			if (Agent != null && Agent.IsActive() && Agent.Health > 0f)
			{
				return !Agent.IsFadingOut();
			}
			return false;
		}
	}

	public Vec3 GetPosition()
	{
		if (!IsValid)
		{
			return Vec3.Invalid;
		}
		return Agent.CollisionCapsuleCenter;
	}
}
