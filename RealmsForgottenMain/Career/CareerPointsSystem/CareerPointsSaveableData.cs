using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class CareerPointsSaveableData
    {
        [SaveableField(0)] public int spentPoints = 0;
        [SaveableField(1)] public int availablePoints = 0;
        public int AllPoints {  get { return spentPoints + availablePoints; } }
    }
}