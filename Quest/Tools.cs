using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Quest.Tools
{
    class Tools
    {
        public static void MergeDisbandParty(MobileParty disbandParty, PartyBase mergeToParty)
        {
            mergeToParty.ItemRoster.Add(disbandParty.ItemRoster.AsEnumerable());
            foreach (TroopRosterElement item in disbandParty.PrisonRoster.GetTroopRoster())
            {
                if (item.Character.IsHero)
                {
                    TransferPrisonerAction.Apply(item.Character, disbandParty.Party, mergeToParty);
                }
                else
                {
                    mergeToParty.PrisonRoster.AddToCounts(item.Character, item.Number, insertAtFront: false, item.WoundedNumber, item.Xp);
                }
            }

            foreach (TroopRosterElement item2 in disbandParty.MemberRoster.GetTroopRoster().ToList())
            {
                disbandParty.MemberRoster.RemoveTroop(item2.Character);
                if (item2.Character.IsHero)
                {
                    AddHeroToPartyAction.Apply(item2.Character.HeroObject, mergeToParty.MobileParty);
                }
                else
                {
                    mergeToParty.MemberRoster.AddToCounts(item2.Character, item2.Number, insertAtFront: false, item2.WoundedNumber, item2.Xp);
                }
            }
            disbandParty.AddElementToMemberRoster(CharacterObject.Find("imperial_equite"), 1);
            
           // disbandParty.RemoveParty();
        }
    }


}
