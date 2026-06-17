using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade
{
    [SaveableClass]
    internal class PendingRecruitment
    {
        [SaveableField(1)] public CharacterObject Troop;
        [SaveableField(2)] public int Number;
        [SaveableField(3)] public CampaignTime DeliveryTime;

        public PendingRecruitment() { } // Required by SaveSystem

        public PendingRecruitment(CharacterObject troop, int number, CampaignTime time)
        {
            Troop = troop;
            Number = number;
            DeliveryTime = time;
        }
    }
}
