using RealmsForgotten.Behaviors;
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
            CampaignHealthChecker.FixCampaignIssues();
            return "Campaign issues fixed.";
        }
    }
}