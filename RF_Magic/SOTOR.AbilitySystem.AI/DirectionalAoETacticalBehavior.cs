using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.AI;

public class DirectionalAoETacticalBehavior : AbstractAgentTacticalBehavior
{
	public Vec3 CastingPosition;

	public AbstractAgentCastingBehavior CastingBehavior { get; set; }

	public DirectionalAoETacticalBehavior(Agent agent, HumanAIComponent aiComponent, AbstractAgentCastingBehavior castingBehavior)
		: base(agent, aiComponent)
	{
		CastingBehavior = castingBehavior;
	}

	private Vec3 CalculateCastingPosition(Formation targetFormation)
	{
		Vec2 estimatedDirection = targetFormation.QuerySystem.EstimatedDirection;
		Agent medianAgent = targetFormation.GetMedianAgent(excludeDetachedUnits: true, excludePlayer: false, targetFormation.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false));
		if (medianAgent == null)
		{
			return Vec3.Zero;
		}
		float num = targetFormation.Width / 1.95f;
		Vec3 vec = medianAgent.Position + estimatedDirection.LeftVec().ToVec3() * num;
		Vec3 vec2 = medianAgent.Position + estimatedDirection.RightVec().ToVec3() * num;
		float num2 = Agent.Position.Distance(vec);
		float num3 = Agent.Position.Distance(vec2);
		if (!(num2 < num3))
		{
			return vec2;
		}
		return vec;
	}

	public override void ApplyBehaviorParams()
	{
	}

	public override void Tick()
	{
		if (CommonAIStateFunctions.CanAgentMoveFreely(Agent))
		{
			Target currentTarget = CastingBehavior.CurrentTarget;
			CastingPosition = ((currentTarget.Formation != null) ? CalculateCastingPosition(currentTarget.Formation) : Agent.Position);
			CastingPosition = ((CastingPosition != Vec3.Zero) ? CastingPosition : Agent.Position);
			WorldPosition position = new WorldPosition(Mission.Current.Scene, CastingPosition);
			Agent.SetScriptedPosition(ref position, addHumanLikeDelay: false);
		}
	}

	public override void SetCurrentTarget(Target target)
	{
		CastingBehavior.SetCurrentTarget(target);
	}

	public override void Terminate()
	{
		Agent.DisableScriptedMovement();
	}
}
