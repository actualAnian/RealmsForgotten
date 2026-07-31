using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

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
            // Faith perks
            _ilacsPrayer.Initialize("{=faith_perk_title.1}Ilac's Prayer", RFSkills.Faith, 50, _druidsSong,
                "{=faith_perk_desc.1}Augment the morale of your troop by x1.2.",
                PartyRole.PartyLeader, 0.12f, EffectIncrementType.AddFactor);

            _druidsSong.Initialize("{=faith_perk_title.2}Druids Song", RFSkills.Faith, 50, _ilacsPrayer,
                "{=faith_perk_desc.2}Heals 10% of the player hit points after battle.",
                PartyRole.Personal, 0.1f, EffectIncrementType.AddFactor);

            _thuriksPrayer.Initialize("{=faith_perk_title.3}Thurik's Prayer", RFSkills.Faith, 100, _druidsSongII,
                "{=faith_perk_desc.3}Augment the morale of your troops by x2.0.",
                PartyRole.PartyLeader, 0.2f, EffectIncrementType.AddFactor);

            _druidsSongII.Initialize("{=faith_perk_title.4}Druids Song II", RFSkills.Faith, 100, _thuriksPrayer,
                "{=faith_perk_desc.4}Heals 25% of the player hit points after battle.",
                PartyRole.Personal, 0.25f, EffectIncrementType.AddFactor);

            _quatzulsPrayer.Initialize("{=faith_perk_title.5}Quatzul's Prayer", RFSkills.Faith, 150, _druidsWave,
                "{=faith_perk_desc.5}Increases troops athletics in x1.5.",
                PartyRole.PartyLeader, 1.5f, EffectIncrementType.AddFactor);

            _druidsWave.Initialize("{=faith_perk_title.6}Druids Wave", RFSkills.Faith, 150, _quatzulsPrayer,
                "{=faith_perk_desc.6}Every damage made on battle regenerates your hit points.",
                PartyRole.Personal, 0f, EffectIncrementType.Invalid);

            _igathurilsPrayer.Initialize("{=faith_perk_title.7}Igathuril's Prayer", RFSkills.Faith, 250, _druidsBlessing,
                "{=faith_perk_desc.7}Increases the combat skills of all the troops in 15%.",
                PartyRole.PartyLeader, 1.5f, EffectIncrementType.AddFactor);

            _druidsBlessing.Initialize("{=faith_perk_title.8}Druids Blessing", RFSkills.Faith, 250, _igathurilsPrayer,
                "{=faith_perk_desc.8}Regenerates 25% of the wounded troops after battle.",
                PartyRole.PartyLeader, 0.25f, EffectIncrementType.AddFactor);

            // ── Arcane perks ────────────────────────────────────────────────────
            // RECALIBRADOS 2026-07-30 para o motor do SOTOR (fase 5c). Os valores
            // antigos foram feitos para o motor legado (dano de Cartridge / splash de
            // wand) e nao se traduzem:
            //
            //   Talisman (DANO do feitico): era 0.9 / 1.2 / 1.2 — o primeiro perk da
            //   arvore era uma PENALIDADE (x0.9) e os dois ultimos eram identicos,
            //   sem progressao. Agora 1.10 / 1.20 / 1.30.
            //
            //   Staff (RAIO de area): era 1.5 / 3.0 / 4.5. Medido no proprio conteudo,
            //   os feiticos do SOTOR tem raio de 0.4 a 30 (massa em 3-10). Com x4.5 um
            //   feitico de raio 20 cobriria 90m — cerca de um quarto do mapa de
            //   batalha. Agora 1.15 / 1.30 / 1.50, o que ainda multiplica a AREA por
            //   1.3 / 1.7 / 2.25 (pi.r^2), mais forte que o eixo de dano — justo,
            //   porque o raio so ajuda feitico de area, enquanto o Talisman vale
            //   tambem para alvo unico.
            _neophytesTalisman.Initialize("{=arcane_perk_title.1}Neophytes Talisman", RFSkills.Arcane, 50, _neophytesStaff,
                "{=arcane_perk_desc.1}Increases your spell damage by x1.1.",
                PartyRole.Personal, 1.1f, EffectIncrementType.AddFactor);

            _neophytesStaff.Initialize("{=arcane_perk_title.2}Neophytes Staff", RFSkills.Arcane, 50, _neophytesTalisman,
                "{=arcane_perk_desc.2}Increases the area of effect radius of your spells by x1.15.",
                PartyRole.Personal, 1.15f, EffectIncrementType.AddFactor);

            _initiatesTalisman.Initialize("{=arcane_perk_title.3}Initiates Talisman", RFSkills.Arcane, 100, _initiatesStaff,
                "{=arcane_perk_desc.3}Increases your spell damage by x1.2.",
                PartyRole.Personal, 1.2f, EffectIncrementType.AddFactor);

            _initiatesStaff.Initialize("{=arcane_perk_title.4}Initiates Staff", RFSkills.Arcane, 100, _initiatesTalisman,
                "{=arcane_perk_desc.4}Increases the area of effect radius of your spells by x1.3.",
                PartyRole.Personal, 1.3f, EffectIncrementType.AddFactor);

            _hierophantsTalisman.Initialize("{=arcane_perk_title.5}Hierophant's Talisman", RFSkills.Arcane, 150, _hierophantsStaff,
                "{=arcane_perk_desc.5}Increases your spell damage by x1.3.",
                PartyRole.Personal, 1.3f, EffectIncrementType.AddFactor);

            _hierophantsStaff.Initialize("{=arcane_perk_title.6}Hierophant's Staff", RFSkills.Arcane, 150, _hierophantsTalisman,
                "{=arcane_perk_desc.6}Increases the area of effect radius of your spells by x1.5.",
                PartyRole.Personal, 1.5f, EffectIncrementType.AddFactor);

            // Alchemy perks
            _novicesLuck.Initialize("{=alchemy_perk_title.1}Novice's Luck", RFSkills.Alchemy, 50, _novicesDedication,
                "{=alchemy_perk_desc.1}Increase all throwable bombs damage in x0.5.",
                PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor);

            _novicesDedication.Initialize("{=alchemy_perk_title.2}Novice's Dedication", RFSkills.Alchemy, 50, _novicesLuck,
                "{=alchemy_perk_desc.2}Increase all throwable bombs area of damage in x0.5.",
                PartyRole.PartyLeader, 0.5f, EffectIncrementType.AddFactor);

            _apprenticesLuck.Initialize("{=alchemy_perk_title.3}Apprentices Luck", RFSkills.Alchemy, 100, _apprenticesDedication,
                "{=alchemy_perk_desc.3}Increase all throwable bombs damage in x1.0.",
                PartyRole.PartyLeader, 1.0f, EffectIncrementType.AddFactor);

            _apprenticesDedication.Initialize("{=alchemy_perk_title.4}Apprentices Dedication", RFSkills.Alchemy, 100, _apprenticesLuck,
                "{=alchemy_perk_desc.4}Increase all throwable bombs area of damage in x1.0.",
                PartyRole.PartyLeader, 1.0f, EffectIncrementType.AddFactor);

            _adeptsLuck.Initialize("{=alchemy_perk_title.5}Adept's Luck", RFSkills.Alchemy, 150, _adeptsDedication,
                "{=alchemy_perk_desc.5}Increase all throwable bombs damage in x1.5.",
                PartyRole.PartyLeader, 1.5f, EffectIncrementType.AddFactor);

            _adeptsDedication.Initialize("{=alchemy_perk_title.6}Adept's Dedication", RFSkills.Alchemy, 150, _adeptsLuck,
                "{=alchemy_perk_desc.6}Increase all throwable bombs area of damage in x1.5.",
                PartyRole.PartyLeader, 1.5f, EffectIncrementType.AddFactor);

            _mastersLuck.Initialize("{=alchemy_perk_title.7}Master's Luck", RFSkills.Alchemy, 250, _mastersDedication,
                "{=alchemy_perk_desc.7}Increase all throwable bombs damage in x2.0.",
                PartyRole.PartyLeader, 2.0f, EffectIncrementType.AddFactor);

            _mastersDedication.Initialize("{=alchemy_perk_title.8}Master's Dedication", RFSkills.Alchemy, 250, _mastersLuck,
                "{=alchemy_perk_desc.8}Increase all throwable bombs area of damage in x2.0.",
                PartyRole.PartyLeader, 2.0f, EffectIncrementType.AddFactor);
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
