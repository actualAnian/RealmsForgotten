using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.CampaignCheats
{
    internal class FixCampaignStateCheats
    {
        readonly static List<Occupation> _villageNotableOccupations = new() { Occupation.Headman, Occupation.RuralNotable };
        readonly static List<Occupation> _townNotableOccupations = new() { Occupation.Artisan, Occupation.Merchant, Occupation.GangLeader };
        readonly static List<Occupation> _castleNotableOccupation = new() { Occupation.Headman };
        [CommandLineFunctionality.CommandLineArgumentFunction("fix_campaign", "rf")]
        public static string FixCampaignCheat(List<string> args)
        {
            var builder = new StringBuilder("Removed {char_rem_amount} incorrect notables from settlements: ");
            try
            {
                int char_rem_amount = 0;
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
                            char_rem_amount += 1;
                            builder.Append($"{notable.Occupation} from {settlement.Name}, ");
                            notable.SetNewOccupation(Occupation.Mercenary);
                            settlement.Notables.RemoveAt(i);
                            KillCharacterAction.ApplyByRemove(notable);
                        }
                    }
                }
                builder.Replace("{char_rem_amount}", char_rem_amount.ToString());
                builder.Append(Environment.NewLine);
                int fixedParties = 0;
                foreach (var party in MobileParty.AllLordParties)
                {
                    if (party.LeaderHero == null)
                    {
                        fixedParties += 1;
                        AddHeroToPartyAction.Apply(party.Owner, party);
                        party.ChangePartyLeader(party.Owner);
                    }
                }
                builder.AppendLine($"restored leader heroes to {fixedParties} lord parties.");
            }
            catch(Exception)
            {
                builder.Append(Environment.NewLine + "ERROR, fixing the campaign failed");
            }
            return builder.ToString();
        }
    }
}