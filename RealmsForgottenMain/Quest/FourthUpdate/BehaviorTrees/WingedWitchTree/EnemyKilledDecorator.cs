using BehaviorTreeWrapper;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WingedWitchTree
{
    internal class EnemyKilledDecorator : BannerlordEventDecorator
    {
        private string interestedEnemy;
        private BTBlackboardBannerlordBase bbBase;
        private WitchFinalFightBlackBoard bbWitch;

        public EnemyKilledDecorator(string enemy, SubscriptionPossibilities onSelfIsHit, BTBlackboardBannerlordBase basebb, WitchFinalFightBlackBoard bbwitch) : base(onSelfIsHit)
        {
            interestedEnemy = enemy;
            bbBase = basebb;
            bbWitch = bbwitch;
        }
        public override bool Evaluate()
        {
            if (bbWitch.counterBeforeNextSpawn == 0)
            {
                bbWitch.counterBeforeNextSpawn = WitchFinalFightBlackBoard.KilledSpawnsBeforeSpawnWave;
                return true;
            }
            return bbWitch.counterBeforeNextSpawn == 0;
        }
        public override void Notify(object[] data)
        {
            Agent killedEnemy = (Agent)data[0];
            if (killedEnemy.Character?.StringId == interestedEnemy)
                bbWitch.counterBeforeNextSpawn -= 1;
        }
    }
}