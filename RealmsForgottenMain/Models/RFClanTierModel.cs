using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using RF_Promoted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace RealmsForgotten.Models
{
    public class RFClanTierModel : DefaultClanTierModel
    {
        public override int GetCompanionLimit(Clan clan)
        {
            int companionLimit = base.GetCompanionLimit(clan);

            if (clan != Clan.PlayerClan) return companionLimit;

            if (PlayerCareerExtension.HasAnyCareer())
                CareerHelper.ApplyBasicCareerPassives(ref companionLimit, PassiveEffectType.CompanionLimit);

            companionLimit += PromotedHelper.GetAdditionalCompanionLimit(clan);
            return companionLimit;
        }
    }
}
