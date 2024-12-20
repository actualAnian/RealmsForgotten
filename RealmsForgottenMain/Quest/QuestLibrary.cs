﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using HarmonyLib;
using TaleWorlds.Library;

namespace RealmsForgotten.Quest
{
    public static class QuestLibrary
    {
        public static void InitializeHideoutIfNeeded(Hideout hideout, bool reset = false)
        {
            if (reset || !hideout.IsInfested)
            {

                Clan clan = Clan.All.Find(x => x.StringId == "mountain_bandits");
                for (int i = 0; i <= 2; i++)
                {
                    MobileParty bandits = BanditPartyComponent.CreateBanditParty("bandits_quest_" + i, clan, hideout, i == 2 ? true : false);
                    bandits.InitializeMobilePartyAtPosition(clan.DefaultPartyTemplate, hideout.Settlement.Position2D);
                    bandits.Ai.SetMoveGoToSettlement(hideout.Settlement);
                    bandits.Ai.RecalculateShortTermAi();
                    EnterSettlementAction.ApplyForParty(bandits, hideout.Settlement);
                }
                AccessTools.Field(typeof(Hideout), "_nextPossibleAttackTime").SetValue(hideout, CampaignTime.Now);

                hideout.IsSpotted = true;


            }
            hideout.Settlement.IsVisible = true;

        }
        public static void RegisterQuestEvents(object obj)
        {
            CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(obj,
                (Hero hero, KillCharacterAction.KillCharacterActionDetail detail, ref bool canDie) =>
                {
                    if (hero == QuestQueen || hero == QuestQueen.Spouse || hero == TheOwl ||
                        hero == QuestLibrary.AnoritLord)
                    {
                        canDie = false;
                    }
                });
        }
        public static void MergeDisbandParty(MobileParty disbandParty, PartyBase mergeToParty)
        {
            mergeToParty.ItemRoster.Add(disbandParty.ItemRoster.AsEnumerable());
            foreach (TroopRosterElement item in disbandParty.PrisonRoster.GetTroopRoster())
            {
                if (item.Character.IsHero)
                {
                    TransferPrisonerAction.Apply(item.Character, disbandParty.Party, mergeToParty);
                }
                else
                {
                    mergeToParty.PrisonRoster.AddToCounts(item.Character, item.Number, insertAtFront: false, item.WoundedNumber, item.Xp);
                }
            }

            foreach (TroopRosterElement item2 in disbandParty.MemberRoster.GetTroopRoster().ToList())
            {
                disbandParty.MemberRoster.RemoveTroop(item2.Character);
                if (item2.Character.IsHero)
                {
                    AddHeroToPartyAction.Apply(item2.Character.HeroObject, mergeToParty.MobileParty);
                }
                else
                {
                    mergeToParty.MemberRoster.AddToCounts(item2.Character, item2.Number, insertAtFront: false, item2.WoundedNumber, item2.Xp);
                }
            }
            disbandParty.AddElementToMemberRoster(CharacterObject.Find("imperial_equite"), 1);
        }
        public static Hero QuestQueen;
        public static Hero AnoritLord
        {
            get
            {
                try
                {
                    return Hero.FindFirst(x => x.StringId == "lord_WE9_l");
                }
                catch (Exception)
                {
                    InformationManager.ShowInquiry(new InquiryData("Error", "Error initializing the quest hero AnoritLord", true, false, GameTexts.FindText("str_done").ToString(), "",
                        null, null), true);
                    return null;
                }
            }
        }

        public static Hero TheOwl
        {
            get
            {
                try
                {
                    return Hero.FindFirst(x => x.StringId == "rf_the_owl");
                }
                catch (Exception)
                {
                    InformationManager.ShowInquiry(new InquiryData("Error", "Error initializing the quest hero Owl", true, false, GameTexts.FindText("str_done").ToString(), "",
                        null, null), true);
                    return null;
                }
            }
        }
        public static void InitializeVariables()
        {
            try
            {
                QuestQueen = Kingdom.All.First(x => x.StringId == "empire").Leader.Spouse;
                if (QuestQueen == null)
                    throw new Exception();
            }
            catch (Exception)
            {
                InformationManager.ShowInquiry(new InquiryData("Error", "Error initializing the quest hero QuestQueen", true, false, GameTexts.FindText("str_done").ToString(), "",
                    null, null), true);
            }

        }
    }


}
