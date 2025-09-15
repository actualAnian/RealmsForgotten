using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class WerewolfPartyComponent : BanditPartyComponent
    {
        public WerewolfPartyComponent() : base(null) { }

        public static MobileParty CreateWerewolfParty(Settlement village)
        {
            // pega o werewolf no XML
            CharacterObject werewolf = MBObjectManager.Instance.GetObject<CharacterObject>("werewolf");
            if (werewolf == null)
                return null;

            // cria uma party bandida hostil na vila
            MobileParty werewolfParty = BanditPartyComponent.CreateBanditParty(
                "werewolf_party_" + village.StringId,
                Clan.BanditFactions.First(), // usa qualquer facção bandida como base
                null,
                false
            );

            werewolfParty.InitializeMobilePartyAroundPosition(
                new TroopRoster(werewolfParty.Party),
                new TroopRoster(werewolfParty.Party),
                village.GatePosition,
                1f
            );

            // adiciona apenas 1 werewolf
            werewolfParty.MemberRoster.AddToCounts(werewolf, 3);

            werewolfParty.Aggressiveness = 100f;
            werewolfParty.SetCustomName(new TaleWorlds.Localization.TextObject("Werewolf"));

            return werewolfParty;
        }
    }
}

