using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class Target : Threat
{
	public Vec3 SelectedWorldPosition = Vec3.Zero;

	public TacticalPosition TacticalPosition;

	public float UtilityValue
	{
		get
		{
			return ThreatValue;
		}
		set
		{
			ThreatValue = value;
		}
	}

	public new Agent Agent
	{
		get
		{
			if (base.Agent == null && Formation != null)
			{
				return Formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, (SelectedWorldPosition == Vec3.Zero) ? Formation.CurrentPosition : SelectedWorldPosition.AsVec2);
			}
			return base.Agent;
		}
		set
		{
			base.Agent = value;
		}
	}

	public Vec3 Position => GetPosition();

	public Vec3 GetPosition()
	{
		try
		{
			if (Agent != null)
			{
				return Agent.CollisionCapsuleCenter;
			}
			if (Formation != null)
			{
				return Formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, Formation.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false))?.Position ?? Vec3.Invalid;
			}
			if (SelectedWorldPosition != Vec3.Zero)
			{
				return SelectedWorldPosition;
			}
			if (TacticalPosition != null)
			{
				return TacticalPosition.Position.GetGroundVec3MT();
			}
			return Vec3.Invalid;
		}
		catch (NullReferenceException)
		{
			return Vec3.Invalid;
		}
	}

	public Vec3 GetPositionPrioritizeCalculated()
	{
		if (SelectedWorldPosition != Vec3.Zero)
		{
			return SelectedWorldPosition;
		}
		if (TacticalPosition != null)
		{
			return TacticalPosition.Position.GetGroundVec3MT();
		}
		try
		{
			return Position;
		}
		catch (NullReferenceException)
		{
			return Vec3.Invalid;
		}
	}
}
