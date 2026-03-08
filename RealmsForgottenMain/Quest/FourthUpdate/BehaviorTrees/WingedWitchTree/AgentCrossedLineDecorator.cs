using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    public class AgentCrossedLineDecorator : BannerlordTickTimedDecorator
    {
        private Vec3 _a;
        private Vec3 _b;
        readonly float _lineA;
        readonly float _lineB;
        readonly bool _shouldStayBelowLine;
        private readonly Agent _agent;
        bool _eventFired = false;
        bool _checkOnce;

        public AgentCrossedLineDecorator(Vec3 a, Vec3 b, bool shouldStayBelowLine, Agent agent, bool checkOnce = true, double secondsTillEvent = 1.0f) : base(secondsTillEvent)
        {
            _a = a;
            _b = b;
            _shouldStayBelowLine = shouldStayBelowLine;
            _agent = agent;
            _checkOnce = checkOnce;
            _lineA = (_b.y - _a.y) / (_b.x - _a.x);
            _lineB = _a.y - _lineA * _a.x;
        }
        public override bool Evaluate()
        {
            if (_checkOnce && _eventFired) return false;
            var a = Agent.Main.Position;
            var y = _lineA * _agent.Position.x + _lineB;
            if (_shouldStayBelowLine && y > _agent.Position.y
                || !_shouldStayBelowLine && y < _agent.Position.y)
            {
                _eventFired = true;
                return true;
            }
            return false;
        }
        public override void Notify(object[] data) { }
    }
}