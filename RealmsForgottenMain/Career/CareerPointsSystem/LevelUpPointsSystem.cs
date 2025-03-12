using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class LevelUpPointsSystem : AbstractPointsSystem
    {
        public override void OnLevelUp(Hero hero, bool arg2) { if (hero == Hero.MainHero) AddPoints(); }
    }
}