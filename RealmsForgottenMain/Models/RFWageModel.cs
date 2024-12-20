﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    internal class RFWageModel : DefaultPartyWageModel
    {
        private PartyWageModel _previousModel;
        
        public RFWageModel(PartyWageModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override int GetCharacterWage(CharacterObject character)
        {
            return base.GetCharacterWage(character) * (character.IsGiant() ? Globals.GiantsCostMult : 1);
        }
        public override int GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            int baseValue = base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
            if (buyerHero == null)
                return baseValue;
            int nasoriaBonus = (int)(baseValue - 15f / 100f * baseValue);
            if (buyerHero.Culture.StringId == "vlandia" && troop.Occupation == Occupation.Mercenary && nasoriaBonus > 0)
                return nasoriaBonus;
            return baseValue;
        }
    }
}
