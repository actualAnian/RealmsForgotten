using BehaviorTrees;
using BehaviorTrees.Nodes;
using RealmsForgotten.MusicSounds;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
{
    public class PlaySoundEffectFollowingPlayerTask : BTTask
    {
        private readonly string soundStringId;

        public PlaySoundEffectFollowingPlayerTask(string soundStringId)
        {
            this.soundStringId = soundStringId;
        }

        public override BTTaskStatus Execute()
        {
            RFMissionSoundManager? soundManager = Mission.Current.GetMissionBehavior<RFMissionSoundManager>();
            if (soundManager == null || !soundManager.AddSoundEvent(soundStringId, true))
                return BTTaskStatus.FinishedWithFalse;
            else
                return BTTaskStatus.FinishedWithTrue;
        }
    }
}
