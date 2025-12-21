using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using RealmsForgotten.MusicSounds;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.BaseTasks
{
    internal class PlaySoundTask : BTTask, IBTBannerlordBase
    {
        readonly string _soundStringId;
        public PlaySoundTask(string soundStringId)
        {
            _soundStringId = soundStringId;
        }
        BTBlackboardValue<Agent> _agent;
        public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
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