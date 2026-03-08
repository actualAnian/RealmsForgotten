using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Decorators
{
    public class PlayerNearPointDecorator : BannerlordTickTimedDecorator
    {
        Vec3 position;
        public PlayerNearPointDecorator(Vec3 position, double secondsTillEvent = 0.66667) : base(secondsTillEvent) { this.position = position; }
        bool isTrue;
        public override bool Evaluate()
        {
            if (Agent.Main?.Position.DistanceSquared(position) < 40f)
                isTrue = true;
            return isTrue;
        }

        public override void Notify(object[] data)
        {
            isTrue = false;
        }
    }
}
