using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public class RFCareerChoiceGroups
    {
        public static RFCareerChoiceGroups? Instance { get; private set; }

        // Mercenary
        private CareerChoiceGroupObject _wandering_blade_1;
        private CareerChoiceGroupObject _wandering_blade_2;
        private CareerChoiceGroupObject _warlord_of_coin_1;
        private CareerChoiceGroupObject _warlord_of_coin_2;
        private CareerChoiceGroupObject _mercenary_lord_1;
        private CareerChoiceGroupObject _mercenary_lord_2;

        // Knight
        private CareerChoiceGroupObject _knight_errant_1;
        private CareerChoiceGroupObject _knight_errant_2;
        private CareerChoiceGroupObject _field_marshall_1;
        private CareerChoiceGroupObject _field_marshall_2;
        private CareerChoiceGroupObject _paragon_of_virtue_1;
        private CareerChoiceGroupObject _paragon_of_virtue_2;

        // NEW: Wizard
        private CareerChoiceGroupObject _apprentice_1;
        private CareerChoiceGroupObject _apprentice_2;
        private CareerChoiceGroupObject _magus_1;
        private CareerChoiceGroupObject _magus_2;
        private CareerChoiceGroupObject _archmage_1;
        private CareerChoiceGroupObject _archmage_2;

#pragma warning disable CS8618
        public RFCareerChoiceGroups()
#pragma warning restore CS8618
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }

        private void RegisterAll()
        {
            // Mercenary
            _wandering_blade_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WanderingBlade1"));
            _wandering_blade_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WanderingBlade2"));
            _warlord_of_coin_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin1"));
            _warlord_of_coin_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin2"));
            _mercenary_lord_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord1"));
            _mercenary_lord_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord2"));

            // Knight
            _knight_errant_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("KnightErrant1"));
            _knight_errant_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("KnightErrant2"));
            _field_marshall_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FieldMarshall1"));
            _field_marshall_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FieldMarshall2"));
            _paragon_of_virtue_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ParagonOfVirtue1"));
            _paragon_of_virtue_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ParagonOfVirtue2"));

            // NEW: Wizard
            _apprentice_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardApprentice1"));
            _apprentice_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardApprentice2"));
            _magus_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardMagus1"));
            _magus_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardMagus2"));
            _archmage_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardArchmage1"));
            _archmage_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WizardArchmage2"));
        }

        private void InitializeAll()
        {
            // Mercenary
            _wandering_blade_1.Initialize("{=rf_career_merc_1}Wandering Blade", RFCareers.Mercenary, 1);
            _wandering_blade_2.Initialize("{=rf_career_merc_1}Wandering Blade", RFCareers.Mercenary, 1);
            _warlord_of_coin_1.Initialize("{=rf_career_merc_2}Warlord of Coin", RFCareers.Mercenary, 2);
            _warlord_of_coin_2.Initialize("{=rf_career_merc_2}Warlord of Coin", RFCareers.Mercenary, 2);
            _mercenary_lord_2.Initialize("{=rf_career_merc_3}Mercenary Lord", RFCareers.Mercenary, 3);
            _mercenary_lord_1.Initialize("{=rf_career_merc_3}Mercenary Lord", RFCareers.Mercenary, 3);

            // Knight
            _knight_errant_1.Initialize("{=rf_career_knight_1}Knight Errant", RFCareers.Knight, 1);
            _knight_errant_2.Initialize("{=rf_career_knight_1}Knight Errant", RFCareers.Knight, 1);
            _field_marshall_2.Initialize("{=rf_career_knight_2}Field Marshall", RFCareers.Knight, 2);
            _field_marshall_1.Initialize("{=rf_career_knight_2}Field Marshall", RFCareers.Knight, 2);
            _paragon_of_virtue_1.Initialize("{=rf_career_knight_3}Paragon Of Virtue", RFCareers.Knight, 3);
            _paragon_of_virtue_2.Initialize("{=rf_career_knight_3}Paragon Of Virtue", RFCareers.Knight, 3);

            // NEW: Wizard
            _apprentice_1.Initialize("{=rf_career_wizard_1}Apprentice", RFCareers.Wizard, 1);
            _apprentice_2.Initialize("{=rf_career_wizard_1}Apprentice", RFCareers.Wizard, 1);
            _magus_1.Initialize("{=rf_career_wizard_2}Magus", RFCareers.Wizard, 2);
            _magus_2.Initialize("{=rf_career_wizard_2}Magus", RFCareers.Wizard, 2);
            _archmage_1.Initialize("{=rf_career_wizard_3}Archmage", RFCareers.Wizard, 3);
            _archmage_2.Initialize("{=rf_career_wizard_3}Archmage", RFCareers.Wizard, 3);
        }
    }
}