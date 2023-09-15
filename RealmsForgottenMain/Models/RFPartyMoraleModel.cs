using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            //Tlachiquiy
            if (party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("tlachiquiy") &&
                baseNumber.ResultNumber < 100)
                baseNumber = new ExplainedNumber(100, true, new TextObject("Tlachiquiy's Boldness"));

            //Elvean
            if (party?.Party?.Culture == null)
                return baseNumber;
            TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
            if (party.Party.Culture.StringId == "battania" && faceTerrainType == TerrainType.Forest)
                baseNumber.AddFactor(0.15f, new TextObject("{=SAvbh23had3}Elvean Forest Morale Bonus"));


            return baseNumber;
        }
    }
}
