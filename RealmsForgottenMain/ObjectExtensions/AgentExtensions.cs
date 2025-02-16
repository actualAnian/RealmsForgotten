using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.ObjectExtensions
{
    public static class AgentExtensions
    {
        public static PartyBase GetOriginPartyBase(this Agent agent)
        {
            if (agent.Origin?.BattleCombatant is PartyBase) return agent.Origin.BattleCombatant as PartyBase;
            return null;
        }
        public static MobileParty GetOriginMobileParty(this Agent agent)
        {
            var party = agent.GetOriginPartyBase();
            if (party == null) return null;
            return party.MobileParty;
        }
        public static bool BelongsToMainParty(this Agent agent)
        {
            var party = agent.GetOriginMobileParty();
            return party != null && party.IsMainParty;
        }
    }
}
