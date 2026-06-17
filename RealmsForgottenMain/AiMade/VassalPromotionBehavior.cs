using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.Core.ImageIdentifiers;

namespace RealmsForgotten.AiMade
{
    public class VassalPromotionBehavior : CampaignBehaviorBase
    {
        // --- Balance & Configuration ---
        private const int PromotionCostGold = 20000;
        private const int PromotionCostInfluence = 300;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddPromotionDialogues);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // This behavior is stateless, so we don't need to save any data.
        }

        private void AddPromotionDialogues(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "companion_promotion_start",
                "companion_talk_player_requests_something",
                "companion_promotion_confirm",
                "I wish to grant you a fief and recognize you as a noble.",
                CanPlayerPromoteCompanion,
                null
            );

            // FIX: Variables for dialogue are set on a TextObject, not the CampaignGameStarter.
            TextObject companionResponseText = new TextObject("My lord, this is a great honor! This will cost you {GOLD_COST}{GOLD_ICON} and {INFLUENCE_COST} influence. Are you certain?");
            companionResponseText.SetTextVariable("GOLD_COST", PromotionCostGold.ToString());
            companionResponseText.SetTextVariable("INFLUENCE_COST", PromotionCostInfluence.ToString());

            starter.AddDialogLine(
                "companion_promotion_confirm_response",
                "companion_promotion_confirm",
                "player_promotion_decision",
                companionResponseText.ToString(), // Pass the completed text here.
                null,
                null
            );

            starter.AddPlayerLine(
                "player_promotion_accept",
                "player_promotion_decision",
                "companion_promotion_fief_selection",
                "I am certain. You have earned it.",
                null,
                new ConversationSentence.OnConsequenceDelegate(StartFiefSelection)
            );

            starter.AddPlayerLine(
                "player_promotion_cancel",
                "player_promotion_decision",
                "companion_pretalk",
                "On second thought, perhaps another time.",
                null,
                null
            );

            starter.AddDialogLine(
                "companion_promotion_fief_selection_response",
                "companion_promotion_fief_selection",
                "close_window",
                "It shall be done, my lord.",
                null,
                null
            );
        }

        private bool CanPlayerPromoteCompanion()
        {
            var companion = Hero.OneToOneConversationHero;
            var player = Hero.MainHero;

            // FIX: Replaced 'IsPlayerFamily' with a check on the character's occupation, which is more reliable.
            return player.Clan?.Kingdom?.Leader == player &&
                   companion != null &&
                   companion.Clan == player.Clan &&
                   companion.CharacterObject.Occupation == Occupation.Wanderer &&
                   player.Gold >= PromotionCostGold &&
                   player.Clan.Influence >= PromotionCostInfluence &&
                   player.Clan.Fiefs.Any(f => f.IsTown || f.IsCastle);
        }

        private void StartFiefSelection()
        {
            var companion = Hero.OneToOneConversationHero;
            // Note: Clan.Fiefs returns Town objects, so 'fief' is a Town.
            var fiefs = Clan.PlayerClan.Fiefs.Where(f => f.IsTown || f.IsCastle).ToList();

            var inquiryElements = new List<InquiryElement>();

            foreach (var fief in fiefs)
            {
                // FIX: The definitive way to get the image.
                // We get the Town's Settlement, then its Party, then the Party's Owner (a Hero),
                // and finally the Owner's CharacterObject.
                CharacterImageIdentifier image = null;
                if (fief.Settlement.Party?.Owner?.CharacterObject != null)
                {
                    image = new CharacterImageIdentifier(CharacterCode.CreateFrom(fief.Settlement.Party.Owner.CharacterObject));
                }

                inquiryElements.Add(new InquiryElement(
                    fief.Settlement, // Pass the Settlement object as the identifier
                    fief.Name.ToString(),
                    image,
                    true,
                    $"Grant {fief.Name} to {companion.Name}."
                ));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Grant Fief",
                $"Select a fief to grant to your new vassal, {companion.Name}. This will be their clan's first territory.",
                inquiryElements,
                true,
                1,
                1,
                "Grant",
                "Cancel",
                (List<InquiryElement> selectedElements) => {
                    var selectedFief = selectedElements.First().Identifier as Settlement;
                    PromoteCompanionToVassal(companion, selectedFief);
                },
                null
            ));
        }

        private void PromoteCompanionToVassal(Hero companion, Settlement fief)
        {
            Hero.MainHero.ChangeHeroGold(-PromotionCostGold);
            Clan.PlayerClan.Influence -= PromotionCostInfluence;

            var newClanName = new TextObject("{COMPANION_NAME}'s Clan");
            newClanName.SetTextVariable("COMPANION_NAME", companion.Name);

            Clan newClan = Clan.CreateClan(newClanName.ToString());
            newClan.Culture = companion.Culture;
            newClan.Banner = Banner.CreateRandomClanBanner(companion.StringId.GetDeterministicHashCode());
            newClan.SetLeader(companion);

            ChangeOwnerOfSettlementAction.ApplyByGift(fief, companion);

            newClan.SetInitialHomeSettlement(fief);

            // FIX: A clan's tier is based on renown. We add enough renown to reach tier 2.
            float renownForTier2 = Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(2);
            newClan.AddRenown(renownForTier2, false); // 'false' hides the notification spam.

            ChangeKingdomAction.ApplyByJoinToKingdom(newClan, Clan.PlayerClan.Kingdom);

            TextObject message = new TextObject("{=yourmod_promotion_success}{COMPANION_NAME} is now the leader of their own clan and your loyal vassal, ruling from {FIEF_NAME}.", null);
            message.SetTextVariable("COMPANION_NAME", companion.Name);
            message.SetTextVariable("FIEF_NAME", fief.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));
        }
    }
}
