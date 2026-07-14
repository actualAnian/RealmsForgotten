using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.Behaviors
{
    public class CampaignHealthChecker : CampaignBehaviorBase
    {
        private static readonly string BaseMessage = "Campaign Health Check Fixed issues: ";
        readonly static List<Occupation> _villageNotableOccupations = new() { Occupation.Headman, Occupation.RuralNotable };
        readonly static List<Occupation> _townNotableOccupations = new() { Occupation.Artisan, Occupation.Merchant, Occupation.GangLeader };
        readonly static List<Occupation> _castleNotableOccupation = new() { Occupation.Headman };

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, FixCampaignIssues);
        }

        public static void FixCampaignIssues()
        {
            StringBuilder builder = new(BaseMessage);
            try
            {
                FixIncorrectNotableTypesInSettlement(builder);
                //FixLeaderlessLordParties(builder);
                FixSettlementParties(builder);
                if (builder.Length > BaseMessage.Length)
                    InformationManager.DisplayMessage(new(builder.ToString(), new Color(1, 0, 0)));
            }
            catch
            {
                builder.Append(Environment.NewLine + "ERROR, fixing the campaign failed");
            }
        }
        private static void FixIncorrectNotableTypesInSettlement(StringBuilder sBuilder)
        {
            int notableFixCount = 0;
            foreach (var settlement in Settlement.All)
            {
                List<Occupation>? occupationList = null;// = settlement.IsTown? _townNotableOccupations : settlement.i
                if (settlement.IsTown) occupationList = _townNotableOccupations;
                if (settlement.IsVillage) occupationList = _villageNotableOccupations;
                if (settlement.IsCastle) occupationList = _castleNotableOccupation;
                if (occupationList == null) continue;
                for (int i = settlement.Notables.Count - 1; i >= 0; i--)
                {
                    Hero notable = settlement.Notables[i];
                    if (!occupationList.Contains(notable.Occupation))
                    {
                        notableFixCount++;
                        sBuilder.Append($"Removed {notable.Occupation} from {settlement.Name}, ");
                        notable.SetNewOccupation(Occupation.Mercenary);
                        settlement.Notables.RemoveAt(i);
                        KillCharacterAction.ApplyByRemove(notable);
                    }
                }
            }
            if (notableFixCount > 0)
                sBuilder.Append(Environment.NewLine);
        }
        private static void FixLeaderlessLordParties(StringBuilder sBuilder)
        {
            int fixedParties = 0;
            foreach (var party in MobileParty.AllLordParties)
            {
                if (party.LeaderHero == null && !party.IsDisbanding && party.IsInitialized)
                {
                    Hero? heroToAdd;
                    if (party.Owner.IsAlive && party.Owner.PartyBelongedTo == null && !party.Owner.IsPrisoner)
                        heroToAdd = party.Owner;
                    else heroToAdd = party.Owner.Clan.AliveLords.FirstOrDefault(l => l.PartyBelongedTo == null && !l.IsPrisoner);
                    if (heroToAdd == null) sBuilder.AppendLine($"could not restore a leader to {party.Name}. no available leaders");
                    else
                    {
                        fixedParties += 1;
                        sBuilder.AppendLine($"restored leader hero to {party.Name}.");
                        AddHeroToPartyAction.Apply(heroToAdd, party);
                        party.ChangePartyLeader(heroToAdd);
                        party.LordPartyComponent.ClearCachedName();
                    }
                }
            }
            if (fixedParties > 0)
                sBuilder.Append(Environment.NewLine);
        }

        private static void FixSettlementParties(StringBuilder sBuilder)
        {
            int fixedSettlements = 0;
            foreach (var settlement in Settlement.All)
            {
                if (settlement.Party.MemberRoster.TotalManCount > 0)
                {
                    fixedSettlements++;
                    settlement.Party.MemberRoster.Clear();
                    sBuilder.AppendLine($"removed party troops from {settlement.Name}");
                }
            }
            if (fixedSettlements > 0)
                sBuilder.Append(Environment.NewLine);
        }
        public override void SyncData(IDataStore dataStore) {}
    }
}