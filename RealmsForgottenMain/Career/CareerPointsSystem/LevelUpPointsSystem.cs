using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class LevelUpPointsSystem : AbstractPointsSystem
    {
        public override string Description => new TextObject("{=rf_pointsystem_levelup}Each level up grants one perk point.").ToString();

        public override void OnLevelUp(Hero hero, bool arg2) { if (hero == Hero.MainHero) AddPoints(); }
    }
}