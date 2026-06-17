using RealmsForgotten.Career;
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
        public static bool ShouldShrugOffDamage(this Agent agent, int damage)
        {
            if (damage >= 15 && PlayerCareerExtension.GetCareer() != null && agent.Character != null && agent.Character.IsPlayerCharacter && PlayerCareerExtension.HasCareerChoice("FieldMarshall1_2"))
                return true;
            return false;
        }
    }
}
