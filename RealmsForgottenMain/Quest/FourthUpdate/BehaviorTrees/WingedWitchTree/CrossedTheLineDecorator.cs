using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    public class CrossedTheLineDecorator : BannerlordTickTimedDecorator
    {
        private Vec3 _a;
        private Vec3 _b;
        readonly float _lineA;
        readonly float _lineB;
        readonly bool _shouldStayBelowLine;
        private readonly BTBlackboardBannerlordBase _bbBase;

        public CrossedTheLineDecorator(Vec3 a, Vec3 b, bool shouldStayBelowLine, BTBlackboardBannerlordBase bbBase): base(1.0)
        {
            _a = a;
            _b = b;
            _shouldStayBelowLine = shouldStayBelowLine;
            _bbBase = bbBase;
            _lineA = (_b.y - _a.y) / (_b.x - _a.x);
            _lineB = _a.y - _lineA * _a.x;
        }
        public override bool Evaluate()
        {
            var a = Agent.Main.Position;
            var y = _lineA * _bbBase.Agent.Position.x + _lineB;
            if (_shouldStayBelowLine && y > _bbBase.Agent.Position.y)
                return true;
            else if (!_shouldStayBelowLine && y < _bbBase.Agent.Position.y)
                return true;
            return false;
        }
        public override void Notify(object[] data) { }
    }
}