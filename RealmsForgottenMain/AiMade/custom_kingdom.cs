using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    public class KingdomCreationHelper
    {
        public static void CreateKingdom(Hero leaderHero, string kingdomName, string clanName, CultureObject culture)
        {
            if (leaderHero == null || culture == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Invalid hero or culture!"));
                return;
            }

            // Generate Unique IDs
            string clanID = "custom_clan_" + leaderHero.StringId;
            string kingdomID = "custom_kingdom_" + leaderHero.StringId;

            // Create Clan (Fixed Parameters)
            Clan newClan = Clan.CreateClan(clanID);

            newClan.SetInitialHomeSettlement(leaderHero.HomeSettlement ?? Settlement.All.FirstOrDefault(s => s.IsTown));
            newClan.SetLeader(leaderHero);
            newClan.Culture = culture;
            newClan.ChangeClanName(new TextObject(clanName), new TextObject(clanName));
            newClan.Leader.Gold += 10000;
            newClan.Renown = 500;
            
            // Create Empty Kingdom First
            Kingdom newKingdom = Kingdom.CreateKingdom(kingdomID);

            // Initialize Kingdom with Proper Data
            newKingdom.InitializeKingdom(
                new TextObject(kingdomName),  // Official name
                new TextObject(kingdomName),  // Informal name
                culture,                      // Culture
                new Banner(),                 // Default banner
                0xFFAA00,                     // Primary color
                0x550000,                     // Secondary color
                leaderHero.HomeSettlement,    // Initial homeland (Settlement)
                new TextObject("A new independent kingdom"), // Encyclopedia text
                new TextObject(kingdomName),  // Encyclopedia title
                new TextObject(leaderHero.Name.ToString())  // Encyclopedia ruler title
            );

            // Set Ruling Clan
            newKingdom.RulingClan = newClan;

            // Assign a Capital (Fixed Town Type Check)
            Settlement capital = leaderHero.HomeSettlement ?? Settlement.All.FirstOrDefault(s => s.IsTown);
            if (capital != null && capital.IsTown && capital.Town != null)
            {
                ChangeOwnerOfSettlementAction.ApplyByGift(capital, leaderHero); // Use `capital`, which is a Settlement
            }

            // Set the hero as ruler of the kingdom
            ChangeKingdomAction.ApplyByJoinToKingdom(newClan, newKingdom, default, true);

            // Ensure hero is leading their own faction
            if (leaderHero.Clan.Leader != leaderHero)
            {
                leaderHero.Clan.SetLeader(leaderHero);
            }

            // Display message
            InformationManager.DisplayMessage(new InformationMessage($"New kingdom '{kingdomName}' created, led by {leaderHero.Name}."));
        }
    }
}

