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
        bool _hasToCrossBetweenAB;

        public AgentCrossedLineDecorator(Vec3 a, Vec3 b, bool shouldStayBelowLine, Agent agent, bool checkOnce = true, bool hasToCrossBetweenAB = false, double secondsTillEvent = 1.0f) : base(secondsTillEvent)
        {
            _a = a;
            _b = b;
            _shouldStayBelowLine = shouldStayBelowLine;
            _agent = agent;
            _checkOnce = checkOnce;
            _hasToCrossBetweenAB = hasToCrossBetweenAB;
            _lineA = (_b.y - _a.y) / (_b.x - _a.x);
            _lineB = _a.y - _lineA * _a.x;
        }
        private bool IsBetweenPointAB()
        {
            var _c = _agent.Position;
            var ac = new Vec2(_c.X - _a.X, _c.Y - _a.Y);
            var ab = new Vec2(_b.X - _a.X, _b.Y - _a.Y);
            float dotA = ac.X * ab.X + ac.Y * ab.Y;

            var bc = new Vec2(_c.X - _b.X, _c.Y - _b.Y);
            var ba = new Vec2(_a.X - _b.X, _a.Y - _b.Y);
            float dotB = bc.X * ba.X + bc.Y * ba.Y;

            return dotA > 0 && dotB > 0;
            //return angleA && angleB;
            //var y = _lineA * _agent.Position.x + _lineB;
            //return _agent.Position.x > _a.x && _agent.Position.x < _b.x || _agent.Position.x < _a.x && _agent.Position.x > _b.x;
        }
        public override bool Evaluate()
        {
            if (_checkOnce && _eventFired) return false;
            var y = _lineA * _agent.Position.x + _lineB;
            if ((_shouldStayBelowLine && y > _agent.Position.y
                || !_shouldStayBelowLine && y < _agent.Position.y)
                && (!_hasToCrossBetweenAB || IsBetweenPointAB()))
            {
                _eventFired = true;
                return true;
            }
            return false;
        }
        public override void Notify(object[] data) { }
    }
}