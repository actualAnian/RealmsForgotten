using System.Linq;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace RealmsForgotten.AiMade.Career
{
    public static class MapEventSideExtensions
    {
        public static bool IsMainPartyFactionSide(this MapEventSide side)
        {
            if (side.Parties == null)
                return false;

            foreach (var party in side.Parties)
            {
                if (party.Party == PartyBase.MainParty)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasCaravan(this MapEvent mapEvent)
        {
            return mapEvent.AttackerSide.Parties.Any(p => p.Party.IsCaravan()) ||
                   mapEvent.DefenderSide.Parties.Any(p => p.Party.IsCaravan());
        }

        public static bool IsCaravan(this PartyBase party)
        {
            return party.MobileParty?.PartyComponent is CaravanPartyComponent;
        }
        public static bool IsPlayerParticipating(this MapEvent mapEvent)
        {
            return mapEvent.AttackerSide.Parties.Any(p => p.Party == PartyBase.MainParty) ||
                   mapEvent.DefenderSide.Parties.Any(p => p.Party == PartyBase.MainParty);
        }
    }
}