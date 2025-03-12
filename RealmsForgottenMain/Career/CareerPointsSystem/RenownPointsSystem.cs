using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public class RenownPointsSystem : AbstractPointsSystem
    {
        readonly int renownForPoint = 20;
        public override void OnRenownGained(Hero hero, int arg2, bool arg3)
        {
            if (hero == Hero.MainHero) 
            {
                int clanRenown = (int)hero.Clan.Renown;
                int currentInfUsed = renownForPoint * pointsData.AllPoints;
                int pointsToGet = (clanRenown - currentInfUsed) / renownForPoint;
                AddPoints(pointsToGet);
            }
        }
    }
}