using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class MeetingEvilLordSceneNotificationItem : SceneNotificationData
    {
        private readonly CampaignTime _creationCampaignTime;

        public CharacterObject Character { get; }
        public CharacterObject SecondCharacter { get; } // This replaces the Hero

        public override string SceneID => "scn_cutscene_meeting_evil_lord";

        public override RelevantContextType RelevantContext => RelevantContextType.Mission;

        public override TextObject TitleText
        {
            get
            {
                return new TextObject("In the meantime, somewhere inside the mountain hall: ´Milord, we have successfully assembled the army. They are marching and soon will be crossing the mountain pass´");
            }
        }

        public override IEnumerable<SceneNotificationCharacter> GetSceneNotificationCharacters()
        {
            List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
            Equipment overriddenEquipment1 = Character.Equipment.Clone(false);
            CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment1, false, false);
            list.Add(new SceneNotificationData.SceneNotificationCharacter(Character, overriddenEquipment1, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));

            Equipment overriddenEquipment2 = SecondCharacter.Equipment.Clone(false);
            CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment2, false, false);
            list.Add(new SceneNotificationData.SceneNotificationCharacter(SecondCharacter, overriddenEquipment2, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));

            return list;
        }

        public MeetingEvilLordSceneNotificationItem(CharacterObject character, CharacterObject secondCharacter)
        {
            Character = character;
            SecondCharacter = secondCharacter;
            _creationCampaignTime = CampaignTime.Now;
        }
    }
}
