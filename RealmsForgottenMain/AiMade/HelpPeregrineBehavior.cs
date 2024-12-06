using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
// Required for FirstOrDefault

namespace RealmsForgotten.AiMade
{
    public class HelpPeregrineBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject HelpPeregrineTitleText = new TextObject("{=HelpPeregrineTitle}HELP A PEREGRINE");
        private static readonly TextObject HelpPeregrineText = new TextObject("{=HelpPeregrineText}AS YOU WENT PAST A CROSSROADS, A LONELY PEREGRINE CALLED FOR YOUR HELP. HE HAS BEEN ROBBED AND LOOKS SCARED. THE THIEVES STOLE AN OFFERING HE WAS BRINGING TO A SHRINE. WILL YOU HELP HIM?");
        private static readonly TextObject AcceptText = new TextObject("{=Accept}ACCEPT AND ESCORT THE PEREGRINE");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}DECLINE AND RESUME YOUR PATH");

        private CampaignTime lastEventTime;  // Variable to store the last event trigger time.
        private Settlement targetSettlement;
        private int banditAttackCount = 0;
        private const int MaxBanditAttacks = 1; // Only one bandit party attack
        private CharacterObject questMonasteryMonk;
        private bool questAccepted = false;
        private CampaignTime questAcceptedTime;
        private const float MaxEscortTimeInDays = 7f; // Maximum time to complete the escort mission

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("HelpPeregrine_lastEventTime", ref lastEventTime);
            dataStore.SyncData("HelpPeregrine_targetSettlement", ref targetSettlement);
            dataStore.SyncData("HelpPeregrine_banditAttackCount", ref banditAttackCount);
            dataStore.SyncData("HelpPeregrine_questMonasteryMonk", ref questMonasteryMonk);
            dataStore.SyncData("HelpPeregrine_questAccepted", ref questAccepted);
            dataStore.SyncData("HelpPeregrine_questAcceptedTime", ref questAcceptedTime);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
            lastEventTime = CampaignTime.Now; // Set the initial time when the game starts
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
            // lastEventTime should already be loaded via SyncData
        }

        private void Initialize()
        {
            // Potential Initialization code.
        }

        private void DailyTick()
        {
            if (CampaignTime.Now - lastEventTime >= CampaignTime.Days(30))
            {
                if (!questAccepted)
                {
                    CreateHelpPeregrinePopUp();
                    lastEventTime = CampaignTime.Now; // Reset the last event time after triggering
                }
                else
                {
                    HealPlayerAndTroops();
                }
            }
        }

        private void HourlyTick()
        {
            if (!questAccepted || targetSettlement == null || questMonasteryMonk == null)
                return;

            // Check if the mission has exceeded the maximum escort time
            if (CampaignTime.Now - questAcceptedTime > CampaignTime.Days(MaxEscortTimeInDays))
            {
                InformationManager.DisplayMessage(new InformationMessage("THE PEREGRINE MONK HAS LEFT YOUR PARTY DUE TO THE DELAY.", Colors.Red));
                EndEscortMission();
                return;
            }

            // Check proximity to the destination and handle bandit attacks or mission completion
            if (MobileParty.MainParty.Position2D.Distance(targetSettlement.Position2D) < 5f)
            {
                if (banditAttackCount < MaxBanditAttacks)
                {
                    SpawnBanditParty();
                    banditAttackCount++;
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("YOU HAVE SUCCESSFULLY ESCORTED THE PEREGRINE TO THE TOWN.", Colors.Green));
                    EndEscortMission();
                }
            }
        }

        private void CreateHelpPeregrinePopUp()
        {
            InformationManager.ShowInquiry(new InquiryData(
                HelpPeregrineTitleText.ToString(),
                HelpPeregrineText.ToString(),
                true,
                true,
                AcceptText.ToString(),
                DeclineText.ToString(),
                OnAccept,
                OnDecline
            ));
        }

        private void OnAccept()
        {
            lastEventTime = CampaignTime.Now;
            InformationManager.DisplayMessage(new InformationMessage("YOU HAVE ACCEPTED TO ESCORT THE PEREGRINE.", Colors.Yellow));
            StartEscortMission();
            questAcceptedTime = CampaignTime.Now;
        }

        private void OnDecline()
        {
            InformationManager.DisplayMessage(new InformationMessage("YOU HAVE DECLINED TO HELP THE PEREGRINE.", Colors.Red));
        }

        private Settlement GetRandomTown()
        {
            var towns = Settlement.All.Where(x => x.IsTown).ToList();
            var randomIndex = MBRandom.RandomInt(towns.Count);
            return towns[randomIndex];
        }

        private void StartEscortMission()
        {
            targetSettlement = GetRandomTown(); // Ensure the town is randomly selected here.
            questMonasteryMonk = CharacterObject.Find("quest_monastery_monk");

            if (targetSettlement == null || questMonasteryMonk == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: TARGET SETTLEMENT OR MONK NOT FOUND.", Colors.Red));
                return;
            }

            MobileParty.MainParty.AddElementToMemberRoster(questMonasteryMonk, 1);
            questAccepted = true;

            // Updated message to include the name of the target settlement.
            InformationManager.DisplayMessage(new InformationMessage($"THE PEREGRINE MONK HAS JOINED YOUR PARTY. ESCORT HIM TO {targetSettlement.Name}.", Colors.Yellow));
        }

        private void EndEscortMission()
        {
            foreach (var element in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                InformationManager.DisplayMessage(new InformationMessage($"Roster contains: {element.Character.Name}, Count: {element.Number}", Colors.Yellow));
            }

            if (questMonasteryMonk != null && MobileParty.MainParty.MemberRoster.Contains(questMonasteryMonk))
            {
                MobileParty.MainParty.MemberRoster.RemoveTroop(questMonasteryMonk);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("WARNING: The peregrine monk was not found in the party roster.", Colors.Red));
            }

            targetSettlement = null;
            banditAttackCount = 0;
            questMonasteryMonk = null;
            questAccepted = false;

            InformationManager.DisplayMessage(new InformationMessage("THE PEREGRINE MONK HAS LEFT YOUR PARTY.", Colors.Green));
        }


        private void HealPlayerAndTroops()
        {
            // Heal the player's character
            Hero.MainHero.HitPoints = Math.Min(Hero.MainHero.MaxHitPoints, Hero.MainHero.HitPoints + (int)(Hero.MainHero.MaxHitPoints * 0.15));

            // Heal the troops in the player's party
            TroopRoster roster = MobileParty.MainParty.MemberRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                TroopRosterElement troop = roster.GetElementCopyAtIndex(i);

                if (troop.Character.IsHero)
                {
                    Hero hero = troop.Character.HeroObject;
                    hero.HitPoints = Math.Min(hero.MaxHitPoints, hero.HitPoints + (int)(hero.MaxHitPoints * 0.15));
                }
                else
                {
                    int healAmount = (int)(troop.Number * 0.15);
                    troop.WoundedNumber = Math.Max(0, troop.WoundedNumber - healAmount);
                }
            }

            InformationManager.DisplayMessage(new InformationMessage("THE PEREGRINE MONK HAS HEALED YOUR PARTY BY 15%.", Colors.Green));
        }


        private void SpawnBanditParty()
        {
            // Find the looter character object
            CharacterObject looter = CharacterObject.Find("looter");

            if (looter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: LOOTER CHARACTER OBJECT NOT FOUND.", Colors.Red));
                return;
            }

            // Create a new bandit party near the player
            Clan looterClan = Clan.BanditFactions.FirstOrDefault(clan => clan.StringId == "looters");
            if (looterClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: LOOTER CLAN NOT FOUND.", Colors.Red));
                return;
            }

            // Get the bandit party template using the ID from the XML
            PartyTemplateObject partyTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            if (partyTemplate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: LOOTER BANDIT PARTY TEMPLATE NOT FOUND.", Colors.Red));
                return;
            }
            // Generate a random position near the player
            float randomX = MBRandom.RandomFloatRanged(2f, 5f) * (MBRandom.RandomFloat >= 0.5f ? 1 : -1);
            float randomY = MBRandom.RandomFloatRanged(2f, 5f) * (MBRandom.RandomFloat >= 0.5f ? 1 : -1);
            Vec2 spawnPosition = MobileParty.MainParty.Position2D + new Vec2(randomX, randomY);

            // Create the bandit party using the standard method
            MobileParty banditParty = BanditPartyComponent.CreateLooterParty("looter_party", looterClan, null, false);
            banditParty.InitializeMobilePartyAroundPosition(partyTemplate, spawnPosition, 2f);

            if (banditParty == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: FAILED TO CREATE BANDIT PARTY.", Colors.Red));
                return;
            }

            // Add looters to the bandit party
            banditParty.MemberRoster.AddToCounts(looter, 20);

            // Set the AI behavior to engage the player's party
            SetBanditPartyAiToEngagePlayer(banditParty);

            // Make the bandit party visible
            banditParty.SetCustomName(new TextObject("BANDIT PARTY"));
            banditParty.IsVisible = true;

            InformationManager.DisplayMessage(new InformationMessage("A BANDIT PARTY HAS BEEN SPAWNED TO ATTACK YOU NEAR THE DESTINATION.", Colors.Red));
        }

        private void SetBanditPartyAiToEngagePlayer(MobileParty banditParty)
        {
            banditParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
            banditParty.Ai.SetDoNotMakeNewDecisions(true);
        }
    }
}



