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
            // ✅ Proteção COMPLETA contra nulls que causam crashes no base
            if (party == null || party.Party == null || party.LeaderHero == null || party.LeaderHero.Culture == null)
            {
                return finalSpeed;
            }

            // 🔐 Agora é seguro chamar o método base
            finalSpeed = base.CalculateFinalSpeed(party, finalSpeed);

            // ✅ Aplica a bênção da religião
            if (ReligionBehavior.Instance != null &&
                ReligionBehavior.Instance.IsHeroBlessed(party.LeaderHero, RFReligions.TengralorOrkhai))
            {
                finalSpeed.Add(0.3f, new TextObject("Tengralor Orkhai Blessing"));
            }

            return finalSpeed;
        }
    }
}
