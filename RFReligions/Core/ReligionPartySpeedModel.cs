using RealmsForgotten.RFReligions.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFReligions.Core
{
    public class ReligionPartySpeedModel : DefaultPartySpeedCalculatingModel
    {
        public new ExplainedNumber CalculateFinalSpeed(MobileParty party, ExplainedNumber finalSpeed)
        {
            if (party == null || party.Party == null || party.LeaderHero == null)
            {
                return finalSpeed;
            }
            finalSpeed = base.CalculateFinalSpeed(party, finalSpeed);

            if (ReligionBehavior.Instance != null &&
                ReligionBehavior.Instance.IsHeroBlessed(party.LeaderHero, RFReligions.TengralorOrkhai))
            {
                finalSpeed.Add(0.3f, new TextObject("Tengralor Orkhai Blessing"));
            }

            return finalSpeed;
        }
    }
}
