using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public class RFCareerChoiceGroups
    {
        public static RFCareerChoiceGroups Instance { get; private set; }

        //Mercenary
        private CareerChoiceGroupObject _wandering_blade_1;
        private CareerChoiceGroupObject _wandering_blade_2;
        private CareerChoiceGroupObject _merc_eq_1;
        private CareerChoiceGroupObject _duel_champoin;
        private CareerChoiceGroupObject _mercenaryLord;
        private CareerChoiceGroupObject _merc_eq_2;

        //Grail Knight
        private CareerChoiceGroupObject _errantryWar;
        private CareerChoiceGroupObject _enhancedHorseCombat;
        private CareerChoiceGroupObject _questingVow;
        private CareerChoiceGroupObject _monsterSlayer;
        private CareerChoiceGroupObject _masterHorseman;
        private CareerChoiceGroupObject _grailVow;
        private CareerChoiceGroupObject _holyCrusader;
        public RFCareerChoiceGroups()
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
            _merc_eq_1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin1"));
            _duel_champoin = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("WarlordOfCoin2"));
            _mercenaryLord = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryLord"));
            _merc_eq_2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MercenaryEquipmentII"));

            //Grail Knight
            _errantryWar = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("ErrantryWar"));
            _enhancedHorseCombat = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("EnhancedHorseCombat")); ;
            _questingVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("QuestingVow"));
            _monsterSlayer = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MonsterSlayer"));
            _masterHorseman = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("MasterHorseman"));
            _grailVow = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("GrailVow"));
            _holyCrusader = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceGroupObject("HolyCrusader"));

        }

        private void InitializeAll()
        {
            //Mercenary

            _wandering_blade_1.Initialize("Wandering Blade", RFCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _wandering_blade_2.Initialize("Wandering Blade", RFCareers.Mercenary, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _merc_eq_1.Initialize("Warlord of Coin", RFCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _duel_champoin.Initialize("Warlord of Coin", RFCareers.Mercenary, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _merc_eq_2.Initialize("Commander", RFCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _mercenaryLord.Initialize("Mercenary's equipment II", RFCareers.Mercenary, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            //_commander.Initialize("{=commander_choice_group_str}The Commander", RFCareers.Mercenary, 3, (Hero hero, out string text) =>
            //{
            //    text = "Required clan renown: 4";
            //    return hero.Clan.Tier >= 4;
            //});

            //Grail Knight

            _errantryWar.Initialize("{=errantry_war_choice_group_str}Errantry War", RFCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _enhancedHorseCombat.Initialize("{=enhanced_horse_combat_choice_group_str}Enhanced Horse Combat", RFCareers.GrailKnight, 1, (Hero hero, out string text) =>
            {
                text = string.Empty;
                return true;
            });
            _questingVow.Initialize("{=questing_vow_choice_group_str}Questing Vow", RFCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _monsterSlayer.Initialize("{=monster_slayer_choice_group_str}Monster Slayer", RFCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _masterHorseman.Initialize("{=master_horseman_choice_group_str}Master Horseman", RFCareers.GrailKnight, 2, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 2";
                return hero.Clan.Tier >= 2;
            });
            _grailVow.Initialize("{=grail_vow_choice_group_str}Grail Vow", RFCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
            _holyCrusader.Initialize("{=holy_crusader_choice_group_str}Holy Crusader", RFCareers.GrailKnight, 3, (Hero hero, out string text) =>
            {
                text = "Required clan renown: 4";
                return hero.Clan.Tier >= 4;
            });
        }
    }
}
