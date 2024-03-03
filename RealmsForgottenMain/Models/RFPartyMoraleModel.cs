using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    internal class RFPartyMoraleModel : DefaultPartyMoraleModel
    {
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false)
        {
            ExplainedNumber baseNumber = base.GetEffectivePartyMorale(party, includeDescription);

            if (party.PartyComponent?.MobileParty == null)
            {
                return baseNumber;
            }
            
            //Tlachiquiy
            if (!party.IsBandit && party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("tlachiquiy") &&
                baseNumber.ResultNumber < 100)
                baseNumber = new ExplainedNumber(100, true, new TextObject("{=tc_boldness}Tlachiquiy's Boldness"));

            //Elvean
            if (party?.Party?.Culture == null)
                return baseNumber;
            TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
            if (party.Party.Culture.StringId == "battania" && faceTerrainType == TerrainType.Forest)
                baseNumber.AddFactor(0.12f, new TextObject("{=elvean_morale_bonus}Elvean Forest Morale Bonus"));

            //Monk knight
            int index = party.MemberRoster.FindIndexOfTroop(CulturesCampaignBehavior.WarriorMonkCharacter);
            if (index > -1)
            {
                int amount = party.MemberRoster.GetTroopCount(CulturesCampaignBehavior.WarriorMonkCharacter);
                float moraleFactor = amount * 0.015f;
                baseNumber.AddFactor(moraleFactor, new TextObject("{=priest_morale_bonus}Priests Morale Bonus"));
            }
            
            
            return baseNumber;
        }
    }
}
