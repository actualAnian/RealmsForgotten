using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace RF_Enlistment;

internal static class RFEnlistmentDebugExtensions
{
    public static string DescribeParty(this MobileParty? party)
    {
        if (party == null)
        {
            return "null";
        }

        string leaderId = party.LeaderHero?.StringId ?? "-";
        string settlementId = party.CurrentSettlement?.StringId ?? "-";
        return $"{party.StringId}[active={party.IsActive},leader={leaderId},settlement={settlementId}]";
    }

    public static string DescribeArmy(this Army? army)
    {
        if (army == null)
        {
            return "null";
        }

        string leaderId = army.LeaderParty?.StringId ?? "-";
        return $"leader={leaderId},cohesion={army.Cohesion:0.0},parties={army.Parties.Count}";
    }

    public static string DescribeEncounter()
    {
        if (PlayerEncounter.Current == null)
        {
            return "null";
        }

        string encounteredParty = PlayerEncounter.EncounteredMobileParty?.StringId ?? "-";
        string battle = PlayerEncounter.EncounteredBattle?.StringId ?? "-";
        return $"party={encounteredParty},battle={battle},inside={PlayerEncounter.InsideSettlement}";
    }

    public static string DescribeMenu()
    {
        return Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId ?? "null";
    }

    public static string DescribeMapEvent(MapEvent? mapEvent)
    {
        if (mapEvent == null)
        {
            return "null";
        }

        string attackerLeader = mapEvent.AttackerSide?.LeaderParty?.MobileParty?.StringId ?? "-";
        string defenderLeader = mapEvent.DefenderSide?.LeaderParty?.MobileParty?.StringId ?? "-";
        return $"{mapEvent.StringId}[attackerLeader={attackerLeader};defenderLeader={defenderLeader};playerEvent={mapEvent.IsPlayerMapEvent}]";
    }
}
