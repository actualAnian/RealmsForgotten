using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFCustomBandits
{
    public class SlaversBanditPartyComponent : BanditPartyComponent
    {
        public override TextObject Name
        {
            get
            {
                TextObject textObject = new("Slavers");
                textObject.SetTextVariable("IS_BANDIT", 1);
                return textObject;
            }
        }
        protected internal SlaversBanditPartyComponent(Hideout hideout, bool isBossParty) : base(hideout, isBossParty) {}
    }
}