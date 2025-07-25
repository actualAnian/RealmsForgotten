using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class RenownPointsSystem : AbstractPointsSystem
    {
        public override string Description => new TextObject("{=rf_pointsystem_renown}As a mercenary, fame is everything that matters. For every 20 renown you get, you receive one perk point.").ToString();


        public static readonly int RenownForPoint = 20;
        public override void OnRenownGained(Hero hero, int arg2, bool arg3)
        {
            if (hero == Hero.MainHero) 
            {
                int clanRenown = (int)hero.Clan.Renown;
                int currentInfUsed = RenownForPoint * pointsData.AllPoints;
                int pointsToGet = (clanRenown - currentInfUsed) / RenownForPoint;
                AddPoints(pointsToGet);
            }
        }
    }
}