using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    internal class MaestersTowerBehavior : CampaignBehaviorBase
    {
        public override void SyncData(IDataStore dataStore) { }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
        }

        public void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town", "town_maesters_tower", "{=ADODVISITMAESTER}Visit the Phisician's Tower", new GameMenuOption.OnConditionDelegate(game_menu_go_to_maesters_tower_on_condition), delegate (MenuCallbackArgs x)
            {
                GameMenu.SwitchToMenu("town_maesters_tower");
            }, false, 4, false);
            campaignGameStarter.AddGameMenu("town_maesters_tower", "You arrive at the Phisician's Tower. As you enter the Tower you see him reading a scroll.", new OnInitDelegate(town_maesters_tower_on_init), GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            campaignGameStarter.AddGameMenuOption("town_maesters_tower", "town_maesters_tower_self_heal", "{=ADODHEALTREATED}Get treated by the Phisician. ({HEAL_SELF_AMOUNT}{GOLD_ICON})", new GameMenuOption.OnConditionDelegate(player_needs_heal_on_condition), delegate (MenuCallbackArgs x)
            {
                HealPlayerCharacter();
            }, false, -1, false);
            campaignGameStarter.AddGameMenuOption("town_maesters_tower", "town_maesters_tower_companion_heal", "{=ADODCOMPANIONTREATED}Have your companions treated by the Phisician. ({HEAL_COMPANION_AMOUNT}{GOLD_ICON})", new GameMenuOption.OnConditionDelegate(companions_needs_heal_on_condition), delegate (MenuCallbackArgs x)
            {
                HealPartyCharacters(false);
            }, false, -1, false);
            campaignGameStarter.AddGameMenuOption("town_maesters_tower", "town_maesters_tower_player_and_companion_heal", "{=ADODMAESTERSCOMPLATREAT}Get yourself and your companions treated by the Phisician. ({HEAL_ALL_AMOUNT}{GOLD_ICON})", new GameMenuOption.OnConditionDelegate(party_characters_needs_heal_on_condition), delegate (MenuCallbackArgs x)
            {
                HealPartyCharacters(true);
            }, false, -1, false);
            campaignGameStarter.AddGameMenuOption("town_maesters_tower", "town_maesters_tower_back", "{=ADODMAESTERBACK}Back to town center", new GameMenuOption.OnConditionDelegate(back_on_condition), delegate (MenuCallbackArgs x)
            {
                GameMenu.SwitchToMenu("town");
            }, false, -1, false);
        }
        private bool game_menu_go_to_maesters_tower_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return MenuHelper.SetOptionProperties(args, true, false, TextObject.Empty);
        }
        private void town_maesters_tower_on_init(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("Phisician's Tower", null);
        }

        private bool player_needs_heal_on_condition(MenuCallbackArgs args)
        {
            int price = PriceToHeal(Hero.MainHero);
            MBTextManager.SetTextVariable("HEAL_SELF_AMOUNT", price);
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;

            if (Hero.MainHero.HitPoints >= Hero.MainHero.MaxHitPoints)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You do not need any treatment.");
            }
            else if (Hero.MainHero.Gold < price)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You need more Gold to receive treatment from the Phisician.");
            }

            return true;  // The option is always shown
        }

        private void HealPlayerCharacter()
        {
            int price = PriceToHeal(Hero.MainHero);
            if (Hero.MainHero.Gold >= price)
            {
                Hero.MainHero.HitPoints = Hero.MainHero.MaxHitPoints;
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -price, false);
            }
            GameMenu.SwitchToMenu("town_maesters_tower");
        }

        private bool companions_needs_heal_on_condition(MenuCallbackArgs args)
        {
            int numberInjured = 0;
            int price = 0;
            CalculatePriceAndNumInjured(ref price, ref numberInjured, false, false);
            MBTextManager.SetTextVariable("HEAL_COMPANION_AMOUNT", price);
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;

            if (numberInjured == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Your companions do not need any treatment, or you don't have any.");
            }
            else if (Hero.MainHero.Gold < price)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You need more Gold Dragons to heal your companions.");
            }

            return true;  // The option is always shown
        }

        private bool party_characters_needs_heal_on_condition(MenuCallbackArgs args)
        {
            int numberInjured = 0;
            int price = 0;
            CalculatePriceAndNumInjured(ref price, ref numberInjured, true, false);
            MBTextManager.SetTextVariable("HEAL_ALL_AMOUNT", price);
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;

            if (Hero.MainHero.HitPoints >= Hero.MainHero.MaxHitPoints && numberInjured == 0)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Neither you nor your companions need any treatment.");
            }
            else if (Hero.MainHero.Gold < price)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("You need more Gold Dragons to treat yourself and your companions.");
            }

            return true;  // The option is always shown
        }

        private void HealPartyCharacters(bool healplayer)
        {
            int numberTreated = 0;
            int price = 0;
            CalculatePriceAndNumInjured(ref price, ref numberTreated, healplayer, true);
            if (numberTreated > 0 && Hero.MainHero.Gold >= price)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -price, false);
            }
            GameMenu.SwitchToMenu("town_maesters_tower");
        }


        private bool back_on_condition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        private int PriceToHeal(Hero hero)
        {
            double basePrice = 1000;
            double percentHpMissing = (double)(hero.MaxHitPoints - hero.HitPoints) / hero.MaxHitPoints;
            return Convert.ToInt32(basePrice * percentHpMissing);
        }

        private void CalculatePriceAndNumInjured(ref int price, ref int numberTreated, bool includeMainHero, bool restoreHealth)
        {
            TroopRoster memberRoster = MobileParty.MainParty.MemberRoster;
            if (memberRoster.TotalHeroes > 0)
            {
                for (int i = 0; i < memberRoster.Count; i++)
                {
                    Hero heroObject = memberRoster.GetCharacterAtIndex(i).HeroObject;
                    if (heroObject != null)
                    {
                        if (heroObject.HitPoints < heroObject.MaxHitPoints && (includeMainHero || heroObject != Hero.MainHero))
                        {
                            numberTreated++;
                            price += PriceToHeal(heroObject);
                            if (restoreHealth)
                            {
                                heroObject.HitPoints = heroObject.MaxHitPoints;
                            }
                        }
                    }
                }
            }
        }
    }
}