using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Career
{
    public class RFCareers
    {
        private MBReadOnlyList<CareerObject> _allCareers;
        private CareerObject _grailKnight;
        private CareerObject _mercenary;

        public RFCareers()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            AssignCareerButtons();
        }

        private void AssignCareerButtons()
        {
            foreach (var career in All)
            {
                CareerButtons.Instance.GetCareerButton(career);
            }
        }

        public static RFCareers Instance { get; private set; }

        public static CareerObject GrailKnight => Instance._grailKnight;
        public static CareerObject Mercenary => Instance._mercenary;

        public static MBReadOnlyList<CareerObject> All => Instance._allCareers;
        private void RegisterAll()
        {
            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailKnight"));
            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Mercenary"));

            _allCareers = new()
            {
                _grailKnight,
                _mercenary,
            };
        }
        private void InitializeAll()
        {
            _grailKnight.Initialize("Grail Knight", null);
            _mercenary.Initialize("Mercenary", null);
        }
    }
}