using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public class RFCareerChoiceGroups
    {
        public static RFCareerChoiceGroups? Instance { get; private set; }

        //Mercenary
        private CareerChoiceGroupObject _wandering_blade_1;
        private CareerChoiceGroupObject _wandering_blade_2;
        private CareerChoiceGroupObject _warlord_of_coin_1;
        private CareerChoiceGroupObject _warlord_of_coin_2;
        private CareerChoiceGroupObject _mercenary_lord_1;
        private CareerChoiceGroupObject _mercenary_lord_2;

        //Grail Knight
        private CareerChoiceGroupObject _knight_errant_1;
        private CareerChoiceGroupObject _knight_errant_2;
        private CareerChoiceGroupObject _field_marshall_1;
        private CareerChoiceGroupObject _field_marshall_2;
        private CareerChoiceGroupObject _paragon_of_virtue_1;
        private CareerChoiceGroupObject _paragon_of_virtue_2;

        //Lightwarden
        private CareerChoiceGroupObject _word_spreader_1;
        private CareerChoiceGroupObject _word_spreader_2;
        private CareerChoiceGroupObject _blessed_one_1;
        private CareerChoiceGroupObject _blessed_one_2;
        private CareerChoiceGroupObject _beacon_of_light_1;
        private CareerChoiceGroupObject _beacon_of_light_2;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public RFCareerChoiceGroups()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
        }
        private void RegisterAll()
        {
            //Mercenary
            _wandering_blade_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WanderingBlade1"));
            _wandering_blade_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WanderingBlade2"));
            _warlord_of_coin_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin1"));
            _warlord_of_coin_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin2"));
            _mercenary_lord_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord1"));
            _mercenary_lord_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord2"));

            //Knight
            _knight_errant_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("KnightErrant1"));
            _knight_errant_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("KnightErrant2"));
            _field_marshall_1= Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FieldMarshall1"));
            _field_marshall_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("FieldMarshall2"));
            _paragon_of_virtue_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ParagonOfVirtue1"));
            _paragon_of_virtue_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ParagonOfVirtue2"));

            //Lightwarden
            _word_spreader_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WordSpreader1"));
            _word_spreader_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WordSpreader2"));
            _blessed_one_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BlessedOne1"));
            _blessed_one_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BlessedOne2"));
            _beacon_of_light_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BeaconOfLight1"));
            _beacon_of_light_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("BeaconOfLight2"));
        }

        private void InitializeAll()
        {
            //Mercenary
            _wandering_blade_1.Initialize("{=rf_career_merc_1}Wandering Blade", RFCareers.Mercenary, 1);
            _wandering_blade_2.Initialize("{=rf_career_merc_1}Wandering Blade", RFCareers.Mercenary, 1);
            _warlord_of_coin_1.Initialize("{=rf_career_merc_2}Warlord of Coin", RFCareers.Mercenary, 2);
            _warlord_of_coin_2.Initialize("{=rf_career_merc_2}Warlord of Coin", RFCareers.Mercenary, 2);
            _mercenary_lord_2.Initialize("{=rf_career_merc_3}Mercenary Lord", RFCareers.Mercenary, 3);
            _mercenary_lord_1.Initialize("{=rf_career_merc_3}Mercenary Lord", RFCareers.Mercenary, 3);

            //Knight
            _knight_errant_1.Initialize("{=rf_career_knight_1}Knight Errant", RFCareers.Knight, 1);
            _knight_errant_2.Initialize("{=rf_career_knight_1}Knight Errant", RFCareers.Knight, 1);
            _field_marshall_2.Initialize("{=rf_career_knight_2}Field Marshall", RFCareers.Knight, 2);
            _field_marshall_1.Initialize("{=rf_career_knight_2}Field Marshall", RFCareers.Knight, 2);
            _paragon_of_virtue_1.Initialize("{=rf_career_knight_3}Paragon Of Virtue", RFCareers.Knight, 3);
            _paragon_of_virtue_2.Initialize("{=rf_career_knight_3}Paragon Of Virtue", RFCareers.Knight, 3);

            //Lightwarden
            _word_spreader_1.Initialize("{=rf_career_lightwarden_1}Word Spreader", RFCareers.Lightwarden, 1);
            _word_spreader_2.Initialize("{=rf_career_lightwarden_1}Word Spreader", RFCareers.Lightwarden, 1);
            _blessed_one_2.Initialize("{=rf_career_lightwarden_2}Blessed One", RFCareers.Lightwarden, 2);
            _blessed_one_1.Initialize("{=rf_career_lightwarden_2}Blessed One", RFCareers.Lightwarden, 2);
            _beacon_of_light_1.Initialize("{=rf_career_lightwarden_3}Beacon of Light", RFCareers.Lightwarden, 3);
            _beacon_of_light_2.Initialize("{=rf_career_lightwarden_3}Beacon of Light", RFCareers.Lightwarden, 3);
        }
    }
}
