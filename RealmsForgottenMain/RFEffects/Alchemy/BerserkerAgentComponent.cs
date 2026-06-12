using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.Alchemy
{
    public class BerserkerAgentComponent : AgentComponent
    {
        public BerserkerAgentComponent(Agent agent) : base(agent) { }
        public override void OnAIInputSet(ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            //if (movementFlag == Agent.MovementControlFlag.DefendUp || movementFlag == Agent.MovementControlFlag.DefendLeft || movementFlag == Agent.MovementControlFlag.DefendRight || movementFlag == Agent.MovementControlFlag.DefendDown || movementFlag == Agent.MovementControlFlag.DefendBlock)
            //    movementFlag = Agent.MovementControlFlag.AttackUp;
            //movementFlag &= ~Agent.MovementControlFlag.AttackDown & ~Agent.MovementControlFlag.AttackLeft& ~Agent.MovementControlFlag.AttackRight& ~Agent.MovementControlFlag.AttackUp & ~Agent.MovementControlFlag.AttackMask;
            movementFlag &= ~Agent.MovementControlFlag.DefendLeft & ~Agent.MovementControlFlag.DefendRight & ~Agent.MovementControlFlag.DefendUp & ~Agent.MovementControlFlag.DefendDown & ~Agent.MovementControlFlag.DefendBlock;
        }
    }
}
