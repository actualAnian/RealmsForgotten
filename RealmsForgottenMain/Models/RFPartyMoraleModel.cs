﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    internal class RFPartyMoraleModel : DefaultPartyMoraleModel
    {
        private PartyMoraleModel _previousModel;
        
        public RFPartyMoraleModel(PartyMoraleModel previousModel)
        {
            _previousModel = previousModel;
        }
        public bool IsPartyBandit(MobileParty party)
        {
            try
            {
                return party.IsBandit;
            }
            catch (Exception e)
            {
                return true;
            }
        }
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false)
        {
            ExplainedNumber baseNumber;
            try
            {
                baseNumber = _previousModel.GetEffectivePartyMorale(party, includeDescription);
            }
            catch (Exception ex)
            {
                return new ExplainedNumber(50);
            }

            if (party.PartyComponent?.MobileParty == null)
            {
                return baseNumber;
            }

            try
            {
                // Tlachiquiy
                if (!IsPartyBandit(party) && party.Owner?.CharacterObject.Race == FaceGen.GetRaceOrDefault("tlachiquiy") &&
                    baseNumber.ResultNumber < 100)
                    baseNumber = new ExplainedNumber(100, true, new TextObject("{=tc_boldness}Tlachiquiy's Boldness"));

                if (party?.Party?.Culture == null)
                    return baseNumber;

                // Dwarf
                if (party.Party.Culture.StringId == "dwarf")
                {
                    baseNumber = new ExplainedNumber(100, true, new TextObject("{=dwarf_unbreakable_morale}Dwarven Unbreakable Morale"));
                    return baseNumber;
                }
            }
            catch (Exception e)
            {
                return baseNumber;
            }
            
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
