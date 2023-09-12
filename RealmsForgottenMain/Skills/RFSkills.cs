using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.CustomSkills
{
    public class RFSkills
    {
        private SkillObject _faith;
        private SkillObject _arcane;
        private SkillObject _alchemy;

        public static RFSkills Instance { get; private set; }
        public static SkillObject Faith => Instance._faith;
        public static SkillObject Arcane => Instance._arcane;
        public static SkillObject Alchemy => Instance._alchemy;

        public void Initialize()
        { 
            new RFSkillEffects().InitializeAll();
            _faith = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("faith"));
            _faith.Initialize(new TextObject("{=!}Faith", null), new TextObject("{=!}Faith is your deeply held belief on  your chosen religion or a deep trust on your spiritual convictions."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttribute.Discipline);
           
            _arcane = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("arcane"));
            _arcane.Initialize(new TextObject("{=!}Arcane", null), new TextObject("{=!}Represents your knowledge in the ancient rites and supernatural phenomena, including the use of organic and inorganic materials in incantations. It defines your capacity to to access magic."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttribute.Discipline);
            
            _alchemy = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("alchemy"));
            _alchemy.Initialize(new TextObject("{=!}Alchemy", null), new TextObject("{=!}Alchemy represents your  understanding in manipulating matter and mixing base substances into higher or more purified forms."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttribute.Discipline);
        }
        public RFSkills()
        {
            Instance = this;


        }
    }
    public class RFSkillEffects
    {
        private SkillEffect _spellPrecision;
        private SkillEffect _gunAccuracy;
        private SkillEffect _spellEffectiveness;
        private SkillEffect _spellDuration;
        private SkillEffect _windsRechargeRate;
        private SkillEffect _maxWinds;
        private SkillEffect _faithWardSave;
        private SkillEffect _blessingDuration;

        public static RFSkillEffects Instance { get; private set; }
        public static SkillEffect SpellPrecision => Instance._spellPrecision;
        public static SkillEffect GunAccuracy => Instance._gunAccuracy;
        public static SkillEffect SpellEffectiveness => Instance._spellEffectiveness;
        public static SkillEffect SpellDuration => Instance._spellDuration;
        public static SkillEffect WindsRechargeRate => Instance._windsRechargeRate;
        public static SkillEffect MaxWinds => Instance._maxWinds;
        public static SkillEffect FaithWardSave => Instance._faithWardSave;
        public static SkillEffect BlessingDuration => Instance._blessingDuration;

        public void InitializeAll()
        {
            _spellPrecision = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("GunReloadSpeed"));



            _spellPrecision.Initialize(new TextObject("{=!}Spell area damage: +{a0} %", null), new SkillObject[]
            {
                RFSkills.Alchemy
            }, SkillEffect.PerkRole.Personal, 0.4f);

        }
        public RFSkillEffects()
        {
            Instance = this;
        }
    }

}
