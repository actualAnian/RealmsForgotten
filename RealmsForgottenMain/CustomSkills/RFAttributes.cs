using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.CustomSkills
{
    public class RFAttributes
    {
        private CharacterAttribute _discipline;

        public static RFAttributes Instance { get; private set; }

        public static CharacterAttribute Discipline => Instance._discipline;

        public void Initialize()
        {
            _discipline = Game.Current.ObjectManager.RegisterPresumedObject(new CharacterAttribute("discipline"));
            _discipline.Initialize(new TextObject("{=discipline}Discipline", null), new TextObject("{=discipline_desc}Discipline is the ability to refine your skill in certain skills which require practice or focus.", null), new TextObject("{=!}DIS", null));
        }
        public RFAttributes()
        {
            Instance = this;
        }
    }
}