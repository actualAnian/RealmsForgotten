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
            _ilacsPrayer.Initialize("{=faith_perk_title.1}Ilac's Prayer", RFSkills.Faith, 50, _druidsSong,
                "{=faith_perk_desc.1}Augment the morale of your troop by x1.5.",
                SkillEffect.PerkRole.PartyLeader, 0.15f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsSong.Initialize("{=faith_perk_title.2}Druids Song", RFSkills.Faith, 50, _ilacsPrayer,
                "{=faith_perk_desc.2}Heals 20% of the player hit points after battle.",
                SkillEffect.PerkRole.Personal, 0.2f, SkillEffect.EffectIncrementType.AddFactor);

            _thuriksPrayer.Initialize("{=faith_perk_title.3}Thurik's Prayer", RFSkills.Faith, 100, _druidsSongII,
                "{=faith_perk_desc.3}Augment the morale of your troops by x3.5.",
                SkillEffect.PerkRole.PartyLeader, 0.35f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsSongII.Initialize("{=faith_perk_title.4}Druids Song II", RFSkills.Faith, 100, _thuriksPrayer,
                "{=faith_perk_desc.4}Heals 50% of the player hit points after battle.",
                SkillEffect.PerkRole.Personal, 0.5f, SkillEffect.EffectIncrementType.AddFactor);

            _quatzulsPrayer.Initialize("{=faith_perk_title.5}Quatzul's Prayer", RFSkills.Faith, 150, _druidsWave,
                "{=faith_perk_desc.5}Increases troops athletics in x3.",
                SkillEffect.PerkRole.PartyLeader, 3f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsWave.Initialize("{=faith_perk_title.6}Druids Wave", RFSkills.Faith, 150, _quatzulsPrayer,
                "{=faith_perk_desc.6}Every damage made on battle regenerates your hit points.",
                SkillEffect.PerkRole.Personal, 0f, SkillEffect.EffectIncrementType.Invalid);

            _igathurilsPrayer.Initialize("{=faith_perk_title.7}Igathuril's Prayer", RFSkills.Faith, 250, _druidsBlessing,
                "{=faith_perk_desc.7}Increases the combat skills of all the troops in 30%.",
                SkillEffect.PerkRole.PartyLeader, 5.0f, SkillEffect.EffectIncrementType.AddFactor);

            _druidsBlessing.Initialize("{=faith_perk_title.8}Druids Blessing", RFSkills.Faith, 250, _igathurilsPrayer,
                "{=faith_perk_desc.8}Regenerates 50% of the wounded troops after battle.",
                SkillEffect.PerkRole.PartyLeader, 0.5f, SkillEffect.EffectIncrementType.AddFactor);

            //Arcane perks
            _neophytesTalisman.Initialize("{=arcane_perk_title.1}Neophytes Talisman", RFSkills.Arcane, 50, _neophytesStaff,
    "{=arcane_perk_desc.1}Augments the effectiveness of your magic damage by x1.5.",
            SkillEffect.PerkRole.Personal, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _neophytesStaff.Initialize("{=arcane_perk_title.2}Neophytes Staff", RFSkills.Arcane, 50, _neophytesTalisman,
                "{=arcane_perk_desc.2}Augments the area damage by x1.5.",
                SkillEffect.PerkRole.Personal, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _initiatesTalisman.Initialize("{=arcane_perk_title.3}Initiates Talisman", RFSkills.Arcane, 100, _initiatesStaff,
                "{=arcane_perk_desc.3}Augments the effectiveness your magic damage by x2.5.",
                SkillEffect.PerkRole.Personal, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _initiatesStaff.Initialize("{=arcane_perk_title.4}Initiates Staff", RFSkills.Arcane, 100, _initiatesTalisman,
                "{=arcane_perk_desc.4}Augments the area damage by x3.0.",
                SkillEffect.PerkRole.Personal, 3.0f, SkillEffect.EffectIncrementType.AddFactor);

            _hierophantsTalisman.Initialize("{=arcane_perk_title.5}Hierophant's Talisman", RFSkills.Arcane, 150, _hierophantsStaff,
                "{=arcane_perk_desc.5}Augments the  effectiveness your magic items by 2.5x (must be equipped with a magic item).",
                SkillEffect.PerkRole.Personal, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _hierophantsStaff.Initialize("{=arcane_perk_title.6}Hierophant's Staff", RFSkills.Arcane, 150, _hierophantsTalisman,
                "{=arcane_perk_desc.6}Augments the area damage by x4.5.",
                SkillEffect.PerkRole.Personal, 4.5f, SkillEffect.EffectIncrementType.AddFactor);

            //Alchemy perks
            _novicesLuck.Initialize("{=alchemy_perk_title.1}Novice's Luck", RFSkills.Alchemy, 50, _novicesDedication,
                "{=alchemy_perk_desc.1}Increase all throwable bombs damage in x1.5.",
                SkillEffect.PerkRole.PartyLeader, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _novicesDedication.Initialize("{=alchemy_perk_title.2}Novice's Dedication", RFSkills.Alchemy, 50, _novicesLuck,
                "{=alchemy_perk_desc.2}Increase all throwable bombs area of damage in x1.5.",
                SkillEffect.PerkRole.PartyLeader, 1.5f, SkillEffect.EffectIncrementType.AddFactor);

            _apprenticesLuck.Initialize("{=alchemy_perk_title.3}Apprentices Luck", RFSkills.Alchemy, 100, _apprenticesDedication,
                "{=alchemy_perk_desc.3}Increase all throwable bombs damage in x3.5.",
                SkillEffect.PerkRole.PartyLeader, 2.5f, SkillEffect.EffectIncrementType.AddFactor);

            _apprenticesDedication.Initialize("{=alchemy_perk_title.4}Apprentices Dedication", RFSkills.Alchemy, 100, _apprenticesLuck,
                "{=alchemy_perk_desc.4}Increase all throwable bombs area of damage in x3.5.",
                SkillEffect.PerkRole.PartyLeader, 3.5f, SkillEffect.EffectIncrementType.AddFactor);

            _adeptsLuck.Initialize("{=alchemy_perk_title.5}Adept's Luck", RFSkills.Alchemy, 150, _adeptsDedication,
                "{=alchemy_perk_desc.5}Increase all throwable bombs damage in x5.5.",
                SkillEffect.PerkRole.PartyLeader, 4.5f, SkillEffect.EffectIncrementType.AddFactor);

            _adeptsDedication.Initialize("{=alchemy_perk_title.6}Adept's Dedication", RFSkills.Alchemy, 150, _adeptsLuck,
                "{=alchemy_perk_desc.6}Increase all throwable bombs area of damage in x5.5.",
                SkillEffect.PerkRole.PartyLeader, 5.5f, SkillEffect.EffectIncrementType.AddFactor);

            _mastersLuck.Initialize("{=alchemy_perk_title.7}Master's Luck", RFSkills.Alchemy, 250, _mastersDedication,
                "{=alchemy_perk_desc.7}Increase all throwable bombs damage in x8.5.",
                SkillEffect.PerkRole.PartyLeader, 6.5f, SkillEffect.EffectIncrementType.AddFactor);

            _mastersDedication.Initialize("{=alchemy_perk_title.8}Master's Dedication", RFSkills.Alchemy, 250, _mastersLuck,
                "{=alchemy_perk_desc.8}Increase all throwable bombs area of damage in x8.5.",
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
