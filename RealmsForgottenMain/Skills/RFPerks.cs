using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.CustomSkills
{
    public class RFPerks
    {
        //Faith perks
        private PerkObject _ilacsPrayer;
        private PerkObject _druidsSong;
        private PerkObject _thuriksPrayer;
        private PerkObject _druidsSongII;
        private PerkObject _quatzulsPrayer;
        private PerkObject _druidsWave;
        private PerkObject _igathurilsPrayer;
        private PerkObject _druidsBlessing;

        //Arcane perks
        private PerkObject _neophytesTalisman;
        private PerkObject _neophytesStaff;
        private PerkObject _initiatesTalisman;
        private PerkObject _initiatesStaff;
        private PerkObject _hierophantsTalisman;
        private PerkObject _hierophantsStaff;

        //Alchemy perks
        private PerkObject _novicesLuck;
        private PerkObject _novicesDedication;
        private PerkObject _apprenticesLuck;
        private PerkObject _apprenticesDedication;
        private PerkObject _adeptsLuck;
        private PerkObject _adeptsDedication;
        private PerkObject _mastersLuck;
        private PerkObject _mastersDedication;



        public static RFPerks Instance { get; private set; }

        public RFPerks()
        {
            Instance = this;

        }

        public void Initialize()
        {
            RegisterAll();
            InitializeAll();
        }
        private void RegisterAll()
        {
            //Faith perks
            _ilacsPrayer = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("IlacsPrayer"));
            _druidsSong = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DruidsSong"));
            _thuriksPrayer = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("ThuriksPrayer"));
            _druidsSongII = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DruidsSongII"));
            _quatzulsPrayer = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("QuatzulsPrayer"));
            _druidsWave = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DruidsWave"));
            _igathurilsPrayer = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("QuatzulsPrayerII"));
            _druidsBlessing = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("DruidsBlessing"));

            //Arcane perks
            _neophytesTalisman = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("NeophytesTalisman"));
            _neophytesStaff = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("NeophytesStaff"));
            _initiatesTalisman = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("InitiatesTalisman"));
            _initiatesStaff = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("InitiatesStaff"));
            _hierophantsTalisman = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("HierophantsTalisman"));
            _hierophantsStaff = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("HierophantsStaff"));

            //Alchemy perks
            _novicesLuck = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("novicesLuck"));
            _novicesDedication = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("novicesDedication"));
            _apprenticesLuck = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("apprenticesLuck"));
            _apprenticesDedication = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("apprenticesDedication"));
            _adeptsLuck = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("adeptsLuck"));
            _adeptsDedication = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("adeptsDedication"));
            _mastersLuck = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("mastersLuck"));
            _mastersDedication = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("mastersDedication"));


        }

        private void InitializeAll()    
        {
            //Se for perk dupla só dá pra ter um nome
            //Faith perks
            _ilacsPrayer.Initialize("{=!}Ilac's Prayer", RFSkills.Faith, 50, _druidsSong,
                "{=!}Augment the morale of your troop by x1.5.",
                SkillEffect.PerkRole.PartyLeader, 0.15f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsSong.Initialize("{=!}Druids Song", RFSkills.Faith, 50, _ilacsPrayer,
                "{=!}Heals 20% of the player hit points after battle.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor);

            _thuriksPrayer.Initialize("{=!}Thurik's Prayer", RFSkills.Faith, 100, _druidsSongII,
                "{=!}Augment the morale of your troops by x3.5.",
                SkillEffect.PerkRole.PartyLeader, 0.35f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsSongII.Initialize("{=!}Druids Song II", RFSkills.Faith, 100, _thuriksPrayer,
                "{=!}Heals 50% of the player hit points after battle.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor);

            _quatzulsPrayer.Initialize("{=!}Quatzul's Prayer", RFSkills.Faith, 150, _druidsWave,
                "{=!}Increases troops athletics in x3.",
                SkillEffect.PerkRole.PartyLeader, 3f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsWave.Initialize("{=!}Druids Wave", RFSkills.Faith, 150, _quatzulsPrayer,
                "{=!}Every damage made on battle regenerates your hit points.",
                SkillEffect.PerkRole.Personal, 0f, SkillEffect.EffectIncrementType.Invalid);

            _igathurilsPrayer.Initialize("{=!}Igathuril's Prayer", RFSkills.Faith, 250, _druidsBlessing,
                "{=!}Increases the combat skills of all the troops in 30%.",
                SkillEffect.PerkRole.PartyLeader, 5.0f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsBlessing.Initialize("{=!}Druids Blessing", RFSkills.Faith, 250, _igathurilsPrayer,
                "{=!}Regenerates 50% of the wounded troops after battle.",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor);

            //Arcane perks
            _neophytesTalisman.Initialize("{=!}Neophytes Talisman", RFSkills.Arcane, 50, _neophytesStaff,
    "{=!}Augments the effectiveness of your magic damage by x1.5.",
            SkillEffect.PerkRole.Personal, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _neophytesStaff.Initialize("{=!}Neophytes Staff", RFSkills.Arcane, 50, _neophytesTalisman,
                "{=!}Augments the area damage by x1.5.",
                SkillEffect.PerkRole.Personal, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _initiatesTalisman.Initialize("{=!}Initiates Talisman", RFSkills.Arcane, 100, _initiatesStaff,
                "{=!}Augments the effectiveness your magic damage by x2.5.",
                SkillEffect.PerkRole.Personal, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _initiatesStaff.Initialize("{=!}Initiates Staff", RFSkills.Arcane, 100, _initiatesTalisman,
                "{=!}Augments the area damage by x3.0.",
                SkillEffect.PerkRole.Personal, 3.0f, SkillEffect.EffectIncrementType.AddFactor);

            _hierophantsTalisman.Initialize("{=!}Hierophant's Talisman", RFSkills.Arcane, 150, _hierophantsStaff,
                "{=!}Augments the  effectiveness your magic items by 2.5x (must be equipped with a magic item).",
                SkillEffect.PerkRole.Personal, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _hierophantsStaff.Initialize("{=!}Hierophant's Staff", RFSkills.Arcane, 150, _hierophantsTalisman,
                "{=!}Augments the area damage by x4.5.",
                SkillEffect.PerkRole.Personal, 4.5f, SkillEffect.EffectIncrementType.AddFactor);

            //Alchemy perks
            _novicesLuck.Initialize("{=!}Novice's Luck", RFSkills.Alchemy, 50, _novicesDedication,
                "{=!}Increase all throwable bombs damage in x1.5.",
                SkillEffect.PerkRole.PartyLeader, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _novicesDedication.Initialize("{=!}Novice's Dedication", RFSkills.Alchemy, 50, _novicesLuck,
                "{=!}Increase all throwable bombs area of damage in x1.5.",
                SkillEffect.PerkRole.PartyLeader, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _apprenticesLuck.Initialize("{=!}Apprentices Luck", RFSkills.Alchemy, 100, _apprenticesDedication,
                "{=!}Increase all throwable bombs damage in x3.5.",
                SkillEffect.PerkRole.PartyLeader, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _apprenticesDedication.Initialize("{=!}Apprentices Dedication", RFSkills.Alchemy, 100, _apprenticesLuck,
                "{=!}Increase all throwable bombs area of damage in x3.5.",
                SkillEffect.PerkRole.PartyLeader, 3.5f, SkillEffect.EffectIncrementType.AddFactor);

            _adeptsLuck.Initialize("{=!}Adept's Luck", RFSkills.Alchemy, 150, _adeptsDedication,
                "{=!}Increase all throwable bombs damage in x5.5.",
                SkillEffect.PerkRole.PartyLeader, 4.5f, SkillEffect.EffectIncrementType.AddFactor);

            _adeptsDedication.Initialize("{=!}Adept's Dedication", RFSkills.Alchemy, 150, _adeptsLuck,
                "{=!}Increase all throwable bombs area of damage in x5.5.",
                SkillEffect.PerkRole.PartyLeader, 5.5f, SkillEffect.EffectIncrementType.AddFactor);

            _mastersLuck.Initialize("{=!}Master's Luck", RFSkills.Alchemy, 250, _mastersDedication,
                "{=!}Increase all throwable bombs damage in x8.5.",
                SkillEffect.PerkRole.PartyLeader, 6.5f, SkillEffect.EffectIncrementType.AddFactor);

            _mastersDedication.Initialize("{=!}Master's Dedication", RFSkills.Alchemy, 250, _mastersLuck,
                "{=!}Increase all throwable bombs area of damage in x8.5.",
                SkillEffect.PerkRole.PartyLeader, 8.5f, SkillEffect.EffectIncrementType.AddFactor);


        }

        public static class Faith
        {
            public static PerkObject IlacsPrayer => Instance._ilacsPrayer;
            public static PerkObject DruidsSong => Instance._druidsSong;
            public static PerkObject ThuriksPrayer => Instance._thuriksPrayer;
            public static PerkObject DruidsSongII => Instance._druidsSongII;
            public static PerkObject QuatzulsPrayer => Instance._quatzulsPrayer;
            public static PerkObject DruidsWave => Instance._druidsWave;
            public static PerkObject IgathurilsPrayer => Instance._igathurilsPrayer;
            public static PerkObject DruidsBlessing => Instance._druidsBlessing;
        }

        public static class Arcane
        {
            public static PerkObject NeophytesTalisman => Instance._neophytesTalisman;
            public static PerkObject NeophytesStaff => Instance._neophytesStaff;
            public static PerkObject InitiatesTalisman => Instance._initiatesTalisman;
            public static PerkObject InitiatesStaff => Instance._initiatesStaff;
            public static PerkObject HierophantsTalisman => Instance._hierophantsTalisman;
            public static PerkObject HierophantsStaff => Instance._hierophantsStaff;
        }

        public static class Alchemy
        {
            public static PerkObject NovicesLuck => Instance._novicesLuck;
            public static PerkObject NovicesDedication => Instance._novicesDedication;
            public static PerkObject ApprenticesLuck => Instance._apprenticesLuck;
            public static PerkObject ApprenticesDedication => Instance._apprenticesDedication;
            public static PerkObject AdeptsLuck => Instance._adeptsLuck;
            public static PerkObject AdeptsDedication => Instance._adeptsDedication;
            public static PerkObject MastersLuck => Instance._mastersLuck;
            public static PerkObject MastersDedication => Instance._mastersDedication;
        }

    }
}
