using BehaviorTrees;
using BehaviorTrees.Nodes;

namespace RealmsForgotten.Quest.FourthUpdate.BehaviorTrees.WitchTreeFirst.Tasks
{
    public class CompleteWitchQuestTask : BTTask
    {
        public CompleteWitchQuestTask() : base() { }

        public override BTTaskStatus Execute()
        {
            SeventhQuest.OnWitchDefeated();
            return BTTaskStatus.FinishedWithTrue;
        }
    }
}
