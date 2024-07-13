using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public static class BanditCharacterExtensions
    {
        public static bool IsBandit(this CharacterObject character)
    {
        return character.Occupation == Occupation.Bandit;
    }
}
}
