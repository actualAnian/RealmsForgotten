using TaleWorlds.CampaignSystem.GameComponents;

namespace RealmsForgotten.AiMade.TroopUnlocker
{
    public class CustomDefaultCharacterStatsModel : DefaultCharacterStatsModel
    {
        private readonly int troopMaxTier;

        public CustomDefaultCharacterStatsModel(int troopMaxTier) => this.troopMaxTier = troopMaxTier;

        public virtual int MaxCharacterTier => 7;
    }
}
