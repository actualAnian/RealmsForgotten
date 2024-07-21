using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Career
{
    [SaveableClass]
    public class MercenaryContract
    {
        [SaveableField(1)]
        private string id;

        [SaveableField(2)]
        private string name;

        [SaveableField(3)]
        private string description;

        [SaveableField(4)]
        private int progress;

        [SaveableField(5)]
        private int goal;

        public string Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }

        public int Progress
        {
            get => progress;
            set => progress = value;
        }

        public int Goal
        {
            get => goal;
            set => goal = value;
        }

        public bool IsCompleted => Progress >= Goal;
    }
}
