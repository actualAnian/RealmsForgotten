using RealmsForgotten.Career.Ability;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Linq;

namespace RealmsForgotten.Career
{
    public class RFCareers
    {
        public enum PointsSystemType
        {
            LevelUp,
        }

        private MBReadOnlyList<CareerObject> _allCareers;
        private CareerObject _grailKnight;
        private CareerObject _mercenary;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public RFCareers()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            instance = this;
            RegisterAll();
            InitializeAll();
        }
        public static RFCareers? instance;
        public static RFCareers Instance
        {
            get
            {
                instance ??= new RFCareers();
                return instance;
            }
        }
        public static CareerObject GrailKnight => Instance._grailKnight;
        public static CareerObject Mercenary => Instance._mercenary;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;
        private void RegisterAll()
        {
            ClassAbility.RegisterAll();

            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("knight", ClassAbility.All.First(a => a.StringId == "merc_ability"), Career.PointsSystemType.LevelUp));
            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("mercenary", ClassAbility.All.First(a => a.StringId == "merc_ability"), Career.PointsSystemType.Renown));

            _allCareers = new()
            {
                _grailKnight,
                _mercenary,
            };

        }
        private void InitializeAll()
        {
            _grailKnight.Initialize("Grail Knight");
            _mercenary.Initialize("Mercenary");
        }
    }
}