using TaleWorlds.CampaignSystem;
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
        //private CareerChoiceGroupObject _errantryWar;
        //private CareerChoiceGroupObject _enhancedHorseCombat;
        //private CareerChoiceGroupObject _questingVow;
        //private CareerChoiceGroupObject _monsterSlayer;
        //private CareerChoiceGroupObject _masterHorseman;
        //private CareerChoiceGroupObject _grailVow;
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

            //Grail Knight
            //_errantryWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ErrantryWar"));
            //_enhancedHorseCombat = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnhancedHorseCombat")); ;
            //_questingVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("QuestingVow"));
            //_monsterSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MonsterSlayer"));
            //_masterHorseman = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterHorseman"));
            //_grailVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GrailVow"));
        }

        private void InitializeAll()
        {
            //Mercenary

            _wandering_blade_1.Initialize("Wandering Blade", RFCareers.Mercenary, 1);
            _wandering_blade_2.Initialize("Wandering Blade", RFCareers.Mercenary, 1);
            _warlord_of_coin_1.Initialize("Warlord of Coin", RFCareers.Mercenary, 2);
            _warlord_of_coin_2.Initialize("Warlord of Coin", RFCareers.Mercenary, 2);
            _mercenary_lord_2.Initialize("Mercenary Lord", RFCareers.Mercenary, 3);
            _mercenary_lord_1.Initialize("Mercenary Lord", RFCareers.Mercenary, 3);

            //Grail Knight

            //_errantryWar.Initialize("{=errantry_war_choice_group_str}Errantry War", RFCareers.GrailKnight, 1);
            //_enhancedHorseCombat.Initialize("{=enhanced_horse_combat_choice_group_str}Enhanced Horse Combat", RFCareers.GrailKnight, 1);
            //_questingVow.Initialize("{=questing_vow_choice_group_str}Questing Vow", RFCareers.GrailKnight, 2);
            //_monsterSlayer.Initialize("{=monster_slayer_choice_group_str}Monster Slayer", RFCareers.GrailKnight, 2);
            //_masterHorseman.Initialize("{=master_horseman_choice_group_str}Master Horseman", RFCareers.GrailKnight, 3);
            //_grailVow.Initialize("{=grail_vow_choice_group_str}Grail Vow", RFCareers.GrailKnight, 3);
        }
    }
}
