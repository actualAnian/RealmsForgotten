using System.Collections.Generic;

namespace SOTOR.AbilitySystem.AI;

public interface IAgentBehavior
{
	void Execute();

	void Terminate();

	List<BehaviorOption> CalculateUtility();

	void SetCurrentTarget(Target target);
}
