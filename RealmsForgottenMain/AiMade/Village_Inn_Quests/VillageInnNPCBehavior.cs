using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    using TaleWorlds.CampaignSystem;
    using TaleWorlds.CampaignSystem.Conversation;
    using TaleWorlds.Core;
    using TaleWorlds.Localization;
    using TaleWorlds.CampaignSystem.Party;
    using TaleWorlds.CampaignSystem.Settlements;

    namespace RealmsForgotten.AiMade.Village_Inn_Quests
    {
        public class VillageInnNPCBehavior : CampaignBehaviorBase
        {
            public override void RegisterEvents()
            {
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            }

            private void OnSessionLaunched(CampaignGameStarter starter)
            {
                // Checa Tavernkeeper (não Innkeeper)
                starter.AddDialogLine("innkeeper_intro", "start", "innkeeper_options",
                    "Welcome to my inn, traveler. What can I do for you?",
                    () => CharacterObject.OneToOneConversationCharacter?.Occupation == Occupation.Tavernkeeper,
                    null);

                // 1) Comprar bebida
                starter.AddPlayerLine("innkeeper_buy_drink", "innkeeper_options", "close_window",
                    "I'd like a drink (10 gold).",
                    () => Hero.MainHero.Gold >= 10,
                    () =>
                    {
                        Hero.MainHero.ChangeHeroGold(-10);
                        MobileParty.MainParty.RecentEventsMorale += 2;
                        InformationManager.DisplayMessage(new InformationMessage("You enjoy a fine drink. (+2 morale)"));
                    });

                // 2) Ouvir rumor
                starter.AddPlayerLine("innkeeper_rumor", "innkeeper_options", "close_window",
                    "Heard any rumors lately?",
                    null,
                    () =>
                    {
                        var villages = Settlement.All.Where(s => s.IsVillage).ToList();
                        if (villages.Count > 0)
                        {
                            var idx = MBRandom.RandomInt(villages.Count);
                            var randomVillage = villages[idx];
                            TextObject rumor = new TextObject("They say trouble is brewing near {VILLAGE}...");
                            rumor.SetTextVariable("VILLAGE", randomVillage.Name);
                            InformationManager.DisplayMessage(new InformationMessage(rumor.ToString()));
                        }
                    });

                // 3) Contratar mercenário
                starter.AddPlayerLine("innkeeper_hire", "innkeeper_options", "close_window",
                    "Any mercenaries looking for work? (50 gold)",
                    () => Hero.MainHero.Gold >= 50,
                    () =>
                    {
                        Hero.MainHero.ChangeHeroGold(-50);
                        var troop = CharacterObject.All.FirstOrDefault(c => c.StringId == "mercenary_infantry");
                        if (troop != null)
                        {
                            MobileParty.MainParty.MemberRoster.AddToCounts(troop, 1);
                            InformationManager.DisplayMessage(new InformationMessage("A mercenary joins your party."));
                        }
                    });

                // 4) Descansar no quarto
                starter.AddPlayerLine("innkeeper_rest", "innkeeper_options", "close_window",
                    "I'd like to rent a room for the night (20 gold).",
                    () => Hero.MainHero.Gold >= 20,
                    () =>
                    {
                        Hero.MainHero.ChangeHeroGold(-20);
                        Hero.MainHero.Heal(20);
                        MobileParty.MainParty.RecentEventsMorale += 5;
                        InformationManager.DisplayMessage(new InformationMessage("You rest well and feel refreshed. (+20 HP, +5 morale)"));
                    });
            }

            public override void SyncData(IDataStore dataStore) { }
        }
    }
}

