using RealmsForgotten.Career.Ability;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Linq;

namespace RealmsForgotten.Career
{
    public class RFCareers
    {
        private MBReadOnlyList<CareerObject> _allCareers;
        private CareerObject _Knight;
        private CareerObject _mercenary;
        private CareerObject _lightwarden;

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
        public static CareerObject Knight => Instance._Knight;
        public static CareerObject Mercenary => Instance._mercenary;
        public static CareerObject Lightwarden => Instance._lightwarden;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;
        private void RegisterAll()
        {
            ClassAbility.RegisterAll();

            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("mercenary", ClassAbility.All.First(a => a.StringId == "merc_ability"), Career.PointsSystemType.Renown));
            _Knight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("knight", ClassAbility.All.First(a => a.StringId == "knight_ability"), Career.PointsSystemType.Deeds));
            _lightwarden = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("lightwarden", ClassAbility.All.First(a => a.StringId == "lightwarden_ability"), Career.PointsSystemType.Virtue));

            _allCareers = new()
            {
                _mercenary,
                _Knight,
                _lightwarden,
            };

        }
        private void InitializeAll()
        {
            _mercenary.Initialize("Mercenary");
            _Knight.Initialize("Knight");
            _lightwarden.Initialize("Lightwarden");
        }
    }
}