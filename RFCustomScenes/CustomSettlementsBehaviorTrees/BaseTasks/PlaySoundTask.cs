using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.MusicSounds;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.BaseTasks
{
    internal class PlaySoundTask : BTTask
    {
        readonly string _soundStringId;
        BTBlackboardBannerlordBase _bbBase;
        public PlaySoundTask(string soundStringId, BTBlackboardBannerlordBase bbBase)
        {
            _soundStringId = soundStringId;
            _bbBase = bbBase;
        }
        public override BTTaskStatus Execute()
        {
            RFMissionSoundManager? soundManager = Mission.Current.GetMissionBehavior<RFMissionSoundManager>();
            if (soundManager == null || !soundManager.AddSoundEvent(_soundStringId, true))
                return BTTaskStatus.FinishedWithFalse;
            else
                return BTTaskStatus.FinishedWithTrue;
        }
    }
}