using RealmsForgotten.Career.Ability;
using RealmsForgotten.ObjectExtensions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Localization;
using static RealmsForgotten.Career.CareerChoiceObject;

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
            _grailKnight = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("GrailKnight", new ClassAbility(new TextObject("{=rf_knight_ability_name}Ability"), "mercenary_ability_200x200", "mercenary_ability_200x200", 5, 5)));
            _mercenary = Game.Current.ObjectManager.RegisterPresumedObject(new CareerObject("Mercenary", new ClassAbility(new TextObject("{=rf_mercenary_ability_name}Battle Cry"), "mercenary_ability_200x200", "mercenary_ability_200x200", 15, 15,
                () => {
                    foreach (Agent? item in Mission.Current.PlayerTeam.ActiveAgents)
                    {
                        item.ChangeMorale(20);
                    }
                }, (Agent attacker, Agent victim, ref float[] additionalDamagePercentages, ref float[] resistancePercentages) => { if (victim.BelongsToMainParty()) resistancePercentages[1] += 0.5f; }))); // +50% melee damage resistance

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