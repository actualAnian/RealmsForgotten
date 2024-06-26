﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
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

            _faith = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("faith"));
            _faith.Initialize(new TextObject("{=faith}Faith", null), new TextObject("{=faith_desc}Faith is your deeply held belief on  your chosen religion or a deep trust on your spiritual convictions."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttributes.Discipline);

            _arcane = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("arcane"));
            _arcane.Initialize(new TextObject("{=arcane}Arcane", null), new TextObject("{=arcane_desc}Represents your knowledge in the ancient rites and supernatural phenomena, including the use of organic and inorganic materials in incantations. It defines your capacity to to access magic."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttributes.Discipline);

            _alchemy = Game.Current.ObjectManager.RegisterPresumedObject(new SkillObject("alchemy"));
            _alchemy.Initialize(new TextObject("{=alchemy}Alchemy", null), new TextObject("{=alchemy_desc}Alchemy represents your  understanding in manipulating matter and mixing base substances into higher or more purified forms."), SkillObject.SkillTypeEnum.Personal)
                .SetAttribute(RFAttributes.Discipline);
        }
        public RFSkills()
        {
            Instance = this;


        }
    }
    public class RFSkillEffects
    {
        private SkillEffect _wandReloadSpeed;
        private SkillEffect _wandAccuracy;
        private SkillEffect _faithPerkMultiplier;
        private SkillEffect _bombStackMultiplier;
        private SkillEffect _magicStaffPower;

        public static RFSkillEffects Instance { get; private set; }
        public static SkillEffect WandReloadSpeed => Instance._wandReloadSpeed;
        public static SkillEffect WandAccuracy => Instance._wandAccuracy;
        public static SkillEffect FaithPerkMultiplier => Instance._faithPerkMultiplier;
        public static SkillEffect BombStackMultiplier => Instance._bombStackMultiplier;
        public static SkillEffect MagicStaffPower => Instance._magicStaffPower;

        public void InitializeAll()
        {
            _wandReloadSpeed = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("WandReloadSpeed"));
            _wandAccuracy = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("WandAccuracy"));
            _faithPerkMultiplier = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("FaithPerkMultiplier"));
            _bombStackMultiplier = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("BombStackMultiplier"));
            _magicStaffPower = Game.Current.ObjectManager.RegisterPresumedObject(new SkillEffect("MagicStaffPower"));

            _wandReloadSpeed.Initialize(new TextObject("{=arcane_skilleff_1}Wand reload speed: +{a0} %", null), new SkillObject[]
            {
                RFSkills.Arcane
            }, SkillEffect.PerkRole.Personal, 0.4f);


            _wandAccuracy.Initialize(new TextObject("{=arcane_skilleff_2}Wand accuracy: +{a0} %", null), new SkillObject[]
            {
                RFSkills.Arcane
            }, SkillEffect.PerkRole.Personal, 0.4f);

            _magicStaffPower.Initialize(new TextObject("{=arcane_skilleff_3}Magic staff power: +{a0} %", null), new SkillObject[]
            {
                RFSkills.Arcane
            }, SkillEffect.PerkRole.Personal, 0.4f);


            _faithPerkMultiplier.Initialize(new TextObject("{=faith_skilleff_1}Perk effect multiplier: +{a0} %", null), new SkillObject[]
            {
                RFSkills.Faith
            }, SkillEffect.PerkRole.Personal, 0.4f);

            _bombStackMultiplier.Initialize(new TextObject("{=alchemy_skilleff_1}Bomb stack multiplier: +{a0} %", null), new SkillObject[]
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
