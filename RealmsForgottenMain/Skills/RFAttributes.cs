using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.CustomSkills
{
    public class RFAttributes
    {
        private CharacterAttribute _discipline;
        private CharacterAttribute _naval;

        public static RFAttributes Instance { get; private set; }

        public static CharacterAttribute Discipline => Instance._discipline;
        public static CharacterAttribute Seafaring => Instance._naval;

        public void Initialize()
        {
            _discipline = Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute("discipline"));
            _discipline.Initialize(new TextObject("{=discipline}Discipline", null), new TextObject("{=discipline_desc}Discipline is the ability to refine your skill in certain skills which require practice or focus.", null), new TextObject("{=!}DIS", null));

            _naval = Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute("naval"));
            _naval.Initialize(new TextObject("{=seafaring}Seafaring", null), new TextObject("{=seafaring_desc}Seafaring is the ability to command, navigate, and fight effectively at sea.", null), new TextObject("{=!}SEA", null));

            AssignNavalSkillsToSeafaring();
        }

        public static void AssignNavalSkillsToSeafaring()
        {
            if (Instance?._naval == null || Game.Current?.ObjectManager == null)
            {
                return;
            }

            SetSkillAttribute("Mariner");
            SetSkillAttribute("Boatswain");
            SetSkillAttribute("Shipmaster");
        }

        private static void SetSkillAttribute(string skillId)
        {
            SkillObject skill = Game.Current.ObjectManager.GetObject<SkillObject>(skillId);
            if (skill == null)
            {
                return;
            }

            CharacterAttribute[] attributes = new[] { Seafaring };
            MethodInfo setter = AccessTools.PropertySetter(typeof(SkillObject), nameof(SkillObject.Attributes));
            if (setter != null)
            {
                setter.Invoke(skill, new object[] { attributes });
                return;
            }

            AccessTools.Field(typeof(SkillObject), "<Attributes>k__BackingField")
                ?.SetValue(skill, attributes);
        }

        public RFAttributes()
        {
            Instance = this;
        }
    }
}
