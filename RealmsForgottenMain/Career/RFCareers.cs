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

        // NEW
        private CareerObject _wizard;

#pragma warning disable CS8618
        public RFCareers()
#pragma warning restore CS8618
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

        // NEW
        public static CareerObject Wizard => Instance._wizard;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;

        private void RegisterAll()
        {
            ClassAbility.RegisterAll();

            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerObject("mercenary",
                    ClassAbility.All.First(a => a.StringId == "merc_ability"),
                    Career.PointsSystemType.Renown));

            _Knight = Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerObject("knight",
                    ClassAbility.All.First(a => a.StringId == "knight_ability"),
                    Career.PointsSystemType.Deeds));

            // NEW: Wizard (Renown)
            _wizard = Game.Current.ObjectManager.RegisterPresumedObject(
                new CareerObject("wizard",
                    ClassAbility.All.First(a => a.StringId == "wizard_ability"),
                    Career.PointsSystemType.Renown));

            _allCareers = new()
            {
                _mercenary,
                _Knight,
                _wizard
            };
        }

        private void InitializeAll()
        {
            _mercenary.Initialize("Mercenary", "{=class_description_mercenary}We are mercenaries. We have the resources. The will. To make these hours count! The clock is ticking, gentlemen. Let's begin.");
            _Knight.Initialize("Knight", "{=class_description_knight}TODO");
            _wizard.Initialize("Wizard", "{=class_description_wizard}You dedicated your life to unfold the misteries of arcane knowledge. As such your mistic ability extend beyond spell casting.");
        }
    }
}