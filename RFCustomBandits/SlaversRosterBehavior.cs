using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.RFCustomBandits
{
    public class SlaversRosterBehavior : CampaignBehaviorBase
    {
        private int currentNoSmallSlaverParties = 0;
        private int currentNoBigSlaverParties = 0;
        private readonly int maxNumberOfBigSlaverParties = 10;
        private readonly int maxNumberOfSmallSlaverParties = 20;
        private readonly float mountToUnitsPercentage = 0.8f;
        public static int ChangeTotalSizeLimitIfSlavers(PartyBase party)
        {
            if (party.IsSlaverParty())
            {
                return 50;
            }
            return 0;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
            //CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, SpawnDesertersIfPossible);
        }

        private void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddDialogLine("enslavers_start_defender", "start", "enslavers_defender", "{=!}{ENSLAVERS_START_DIALOGUE}", new ConversationSentence.OnConditionDelegate(this.enslavers_defenders_condition), null, 100, null);
            campaignGameSystemStarter.AddPlayerLine("enslavers_defender_1", "enslavers_defender", "enslavers_start_fight", "You will pay for that!", null, null, 100, null, null);
            campaignGameSystemStarter.AddDialogLine("enslavers_start_fight", "enslavers_start_fight", "close_window", "Bold, aren't you? That's exactly who we need...", null, null, 100, null);
            campaignGameSystemStarter.AddDialogLine("enslavers_start_attacker", "start", "enslavers_attacker", "What do you want", new ConversationSentence.OnConditionDelegate(this.enslavers_attacker_on_condition), null, 100, null);
            campaignGameSystemStarter.AddPlayerLine("enslavers_encounter_ultimatum", "enslavers_attacker", "enslavers_encounter_ultimatum_answer", "Have you ever thought of tasting slavery yourself? Now you have a chance", null, null, 100, null, null);
            campaignGameSystemStarter.AddPlayerLine("enslavers_encounter_fight", "enslavers_attacker", "bandit_attacker_leave", "{=3W3eEIIZ}Never mind. You can go.", null, null, 100, null, null);
            campaignGameSystemStarter.AddDialogLine("enslavers_encounter_ultimatum_war", "enslavers_encounter_ultimatum_answer", "close_window", "You will never take us alive[if:idle_angry][ib:aggressive]", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null);
        }

#pragma warning disable IDE1006 // Naming Styles
        private void conversation_bandit_set_hostile_on_consequence()
        {

        }

        private bool enslavers_attacker_on_condition()
        {
            return Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter
                && PlayerEncounter.Current != null
                && PlayerEncounter.EncounteredMobileParty != null
                && PlayerEncounter.EncounteredMobileParty.IsSlaverParty()
                && PlayerEncounter.PlayerIsAttacker
                && MobileParty.ConversationParty != null;

        }

        private bool enslavers_defenders_condition()
        {
            if(!(Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter
                && PlayerEncounter.Current != null
                && PlayerEncounter.EncounteredMobileParty != null
                && PlayerEncounter.EncounteredMobileParty.IsSlaverParty()
                && PlayerEncounter.PlayerIsDefender
                && MobileParty.ConversationParty != null)) return false;

            string enslaversText = ChooseEnslaversDefenderText(Hero.MainHero.CharacterObject, MobileParty.MainParty.MemberRoster.TotalManCount, Clan.PlayerClan.Tier);
            MBTextManager.SetTextVariable("ENSLAVERS_START_DIALOGUE", enslaversText, false);
            return true;
        }

#pragma warning restore IDE1006 // Naming Styles
        public void DailyTick()
        {
            var random = new Random();
            if (1-MathF.Log(currentNoSmallSlaverParties, maxNumberOfSmallSlaverParties) > random.NextDouble())
                SpawnSmallSlaverParty();
            if (1-MathF.Log(currentNoBigSlaverParties, maxNumberOfBigSlaverParties) > random.NextDouble())
                SpawnBigSlaverParty();
        }

        private void SpawnBigSlaverParty()
        {
            currentNoBigSlaverParties += 1;
            PartyTemplateObject troopTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("enslavers_template");
            SpawnSlavers(troopTemplate);
        }

        private void SpawnSmallSlaverParty()
        {
            currentNoSmallSlaverParties += 1;
            PartyTemplateObject troopTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("enslavers_template");
            SpawnSlavers(troopTemplate);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("numer_of_small_slaver_parties", ref currentNoSmallSlaverParties);
            dataStore.SyncData("numer_of_big_slaver_parties", ref currentNoBigSlaverParties);
            //dataStore.SyncData("deserter_troops_global_pool", ref _desertedTroops);
        }
        public void SpawnSlavers(PartyTemplateObject troopTemplate)
        {
            IEnumerable<Hideout> infestedHideouts = Hideout.All.WhereQ((Hideout h) => h.IsInfested);
            if (!infestedHideouts.Any()) return;
            Hideout randomHideout = infestedHideouts.ElementAt(MBRandom.RandomInt(0, infestedHideouts.Count()));

            //Clan looterClan = Clan.All.WhereQ((Clan c) => c.StringId == "looters").Single();
            Clan looterClan = Clan.All.WhereQ((Clan c) => c.StringId == "athas_enslavers").Single();
            MobileParty enslaversParty = MobileParty.CreateParty("Slavers", new SlaversBanditPartyComponent(randomHideout, false), delegate (MobileParty mobileParty)
            {
                mobileParty.ActualClan = looterClan;
            });

            if (randomHideout != null)
            {
                float num = 45f * 1.5f;
                enslaversParty.InitializeMobilePartyAtPosition(troopTemplate, randomHideout.Settlement.GatePosition, 50);
                Vec2 vec = enslaversParty.Position2D;
                float radiusAroundPlayerPartySquared = 20;
                for (int i = 0; i < 15; i++)
                {
                    Vec2 vec2 = MobilePartyHelper.FindReachablePointAroundPosition(vec, num, 0f);
                    if (vec2.DistanceSquared(MobileParty.MainParty.Position2D) > radiusAroundPlayerPartySquared)
                    {
                        vec = vec2;
                        break;
                    }
                }
                if (vec != enslaversParty.Position2D)
                {
                    enslaversParty.Position2D = vec;
                }
                enslaversParty.Party.SetVisualAsDirty();
                int initialGold = (int)(10f * (float)enslaversParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
                enslaversParty.InitializePartyTrade(initialGold);
                foreach (ItemObject itemObject in Items.All)
                {
                    if (itemObject.IsFood)
                    {
                        int num3 = 8;
                        int num2 = MBRandom.RoundRandomized((float)enslaversParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num3 * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                        if (num2 > 0)
                        {
                            enslaversParty.ItemRoster.AddToCounts(itemObject, num2);
                        }
                    }
                }
                if (Globals.Settings.SmartAthasEnslavers) AddHorsesToParty(enslaversParty);
                enslaversParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
                enslaversParty.Ai.SetMovePatrolAroundPoint(randomHideout.Settlement.Position2D);
            }
        }

        private void AddHorsesToParty(MobileParty enslaversParty)
        {
            int horsesToAdd = MathF.Round(mountToUnitsPercentage * enslaversParty.MemberRoster.TotalManCount) - CountMounted(enslaversParty.MemberRoster) - enslaversParty.ItemRoster.NumberOfMounts;
            ItemObject horseObject = MBObjectManager.Instance.GetObject<ItemObject>("aserai_horse");
            enslaversParty.ItemRoster.AddToCounts(horseObject, horsesToAdd);
        }
        public static int CountMounted(TroopRoster troopRoster)
        {
            return troopRoster.GetTroopRoster().WhereQ((TroopRosterElement t) => !t.Character.FirstBattleEquipment[10].IsEmpty).SumQ((TroopRosterElement t) => t.Number);
        }
        private string ChooseEnslaversDefenderText(CharacterObject player, int playerTroopNumber, int clanTier)
        {
            string enslaversText;
            if (clanTier > 2)
            {
                if (player.IsUndead() || player.IsGiant())
                    enslaversText = "I've heard of you, abomination, but I never expected to have you at my mercy. Surrender, there's lots of folks, who will want to see your performance.";
                else // if(player.IsHuman() || player.IsElvean() || player.IsXilantlacay()) || player.IsMull())
                    enslaversText = "A noble from a renowned clan? a fine catch, a fine catch, you will be a sight to behold... cuff" + (Hero.MainHero.IsFemale ? "her" : "him") + "men";
            }
            else if (MobileParty.MainParty.MemberRoster.TotalManCount == 1)
            {
                if (player.IsMull()) enslaversText = "Look boys, We found another pet." + (Hero.MainHero.IsFemale ? "This one looks to be a female as well." : "") + "let's collar" + (Hero.MainHero.IsFemale ? "her" : "him");
                else if (player.IsElvean()) enslaversText = "Look at our luck! We found an elvean, travelling alone. " + (player.IsFemale? "She" : "He") + " will definitely fetch a high price!";
                else if (player.IsUndead() || player.IsGiant() || player.IsXilantlacay()) enslaversText = "Men, look what we have found! Travelling alone... it's simply asking to be taken. Cuff it!";
                else if (Hero.MainHero.IsFemale) enslaversText = "It is dangerous for lone damsels to travel these parts alone, we will take care of you. Take her men";
                else enslaversText = "And what have we got here, a lone traveller, with no bodyguards? well, well, I claim you as my posession!";
            }
            else
            {
                if (player.IsElvean())
                {
                    if (player.IsFemale) enslaversText = "What a fortunate meeting! The crowd loves watching your kin in the arena, especially females. Surrender now, we wouldn't want you and your companions to get bruised before the first match.";
                    else enslaversText = "And what have I come across, you know what is better than two men in cuffs? An elvean of course. Grab them men!";
                }
                else if (player.IsHuman())
                {
                    if (player.IsFemale) enslaversText = "Trying to lead your own band, girl? You would make a better sight servicing customers in your local tavern. let's see if you can keep the attitude after some time with us.";
                    else enslaversText = "Yet another would be adventurer trying to form his own band, eh? Let's see how long it takes to break you and your companions in the arena.";
                }
                else if (player.IsMull())
                {
                    enslaversText = "Looks like you have some claws, if you managed to get some to follow you. Let's see if it will be enough in the arena!";
                }
                else if (player.IsXilantlacay())
                {
                    if (player.IsFemale) enslaversText = "So you aliens have females as well? Try to put up a good fight now, If you do, I might come to watch you during your matches";
                    else enslaversText = "I told you men we would find... something around here. I want to test these ones myself before selling them to the arena.";
                }
                else if (player.IsUndead())
                    enslaversText = "And what have we got boys? This one looks like it is already dead… maybe some time at the arena will make him looks healthier?";
                else if (player.IsGiant())
                    enslaversText = "Looks like you have enough in your brain to make others follow you. Just don't tire yourself before your match like the rest. We would like to have you for more than one battle.";
                else
                    enslaversText = "Did i stumble upon an adventuring party? Perfect, the more the merrier, you will make fine slaves!";
            }
            return enslaversText;
        }
    }
}


