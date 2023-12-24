using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.RFLegendaryTroops
{
        static class Helper 
        {
        public static readonly Dictionary<string, string> legendaryTroops =new()
        {
            {"empire", "legendary_men"},
            {"battania", "legendary_elvean"},
            {"sturgia", "legendary_zombie"},
            {"khuzait", "legendary_steppe_horseman"},
            {"vlandia", "legendary_warlord"},
            {"aserai", "legendary_mull"},
            {"giant", "legendary_xilantlacay"},
            {"aqarun", "legendary_aqarun"},

        }; 
        public static CharacterObject ChooseLegendaryTroop(CultureObject culture)
        {
            CharacterObject? legendaryTroop = null;
            if (legendaryTroops.ContainsKey(culture.StringId))
                legendaryTroop = MBObjectManager.Instance.GetObject<CharacterObject>(legendaryTroops[culture.StringId]); ;
            return legendaryTroop ?? MBObjectManager.Instance.GetObject<CharacterObject>("mercenary_7");
        }

        public static bool CanMainHeroRecruit(Settlement castle, out bool shouldBeDisabled, out TextObject disabledText)
        {
            disabledText = TextObject.Empty;
            if (castle.MapFaction == Hero.MainHero.MapFaction && Hero.MainHero.IsFactionLeader)
            {
                shouldBeDisabled = false;
                return true;
            }
            else
            {
                shouldBeDisabled = true;
                disabledText = new TextObject("{=rf_legendary_recruitment}You need to be a king, and your faction needs to own this castle to recruit legendary troops.", null);
                return false;
            }
        }
        internal static bool IsRulerParty(this MobileParty mobileParty)
        {
            return mobileParty != null && mobileParty.Owner != null && mobileParty.LeaderHero != null && mobileParty.ActualClan != null && mobileParty.ActualClan.Kingdom != null && mobileParty.LeaderHero.IsFactionLeader && 
                Helper.legendaryTroops.ContainsKey(mobileParty.Owner.Clan.Kingdom.Culture.StringId) && mobileParty.LeaderHero != Hero.MainHero;
        }
        public static int GetTargetNotableCountForSettlement(Settlement settlement)
        {
            return 1;
            //return settlement.Town.GetWallLevel();    //  possible other way to spawn notables
        }

        public static bool CanRecruitIfInCastle(MobileParty party, Settlement settlement)
        {
            if (!settlement.IsCastle) return true;
            if(party.Owner != null && party.IsRulerParty()) return true; 
            else return false;
        }

    }
 
}