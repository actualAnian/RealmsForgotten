using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.RFReligions.Core;
using RealmsForgotten.RFReligions.Helper;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFReligions.Behavior;

internal class ReligionBehavior : CampaignBehaviorBase
{
    public ReligionBehavior()
    {
        _partyMoraleEffect = new Dictionary<MobileParty, float>();
        _settlementEffect = new Dictionary<Settlement, float>();
        Instance = this;
    }

    public static ReligionBehavior? Instance;


    public override void RegisterEvents()
    {
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this,
            new Action<CampaignGameStarter>(OnSessionLaunched));
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this,
            new Action<CampaignGameStarter>(NewGameCreated));
        CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, new Action(CharacterCreationOver));
        CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(OnHeroCreated));
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(OnWeeklyTick));
        CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(DailyHeroTick));
        CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this,
            new Action<MobileParty>(MobilePartyDailyTick));
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTick));
        CampaignEvents.TickEvent.AddNonSerializedListener(this, new Action<float>(OnTick));
    }


    private void CharacterCreationOver()
    {
        if (_heroes.Count > 0 && !_heroes.ContainsKey(Hero.MainHero))
        {
            var mainHero = Hero.MainHero;
            var culture = mainHero.Culture;
            var value = new HeroReligionModel(
                ReligionMapHelper.GetCultureReligion(culture != null ? culture.StringId : null),
                (float)MBRandom.RandomInt(20, 75));
            if (_heroes.ContainsKey(mainHero))
            {
                _heroes[mainHero] = value;
                return;
            }

            _heroes.Add(mainHero, value);
        }
    }


    private void OnHeroCreated(Hero hero, bool arg2)
    {
        if (hero != null && _heroes != null)
        {
            var culture = hero.Culture;
            var cultureString = culture != null ? culture.StringId : null;
            if (hero.Clan != null && hero.Clan.IsMinorFaction)
            {
                if (!hero.Clan.StringId.Contains("ember"))
                {
                    if (hero.Clan.StringId.Contains("tengralororkhai")) cultureString = "tengralororkhai";
                }
                else
                {
                    cultureString = "ember";
                }
            }

            var value = new HeroReligionModel(ReligionMapHelper.GetCultureReligion(cultureString),
                (float)MBRandom.RandomInt(20, 100));
            if (_heroes.ContainsKey(hero))
            {
                _heroes[hero] = value;
                return;
            }

            _heroes.Add(hero, value);
        }
    }


    private void OnDailyTick()
    {
        try
        {
            DONATION_COST = MBRandom.RandomInt(5000, 10000);
            foreach (var settlement in Campaign.Current.Settlements)
                if (settlement.IsTown)
                {
                    if (_settlements.ContainsKey(settlement))
                    {
                        _settlements[settlement].ReCalculateDevotion(settlement.Town);
                        _settlements[settlement].DailyReligionDevotion(
                            MBRandom.RandomFloatRanged(settlement.Town.Prosperity * 0.01f,
                                settlement.Town.Prosperity * 0.02f));
                    }

                    if (_settlementEffect.ContainsKey(settlement))
                    {
                        Dictionary<Settlement, float> settlementEffect = _settlementEffect;
                        var key = settlement;
                        settlementEffect[key] -= 0.5f;
                    }
                }
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage("Religions DailyTick Error"));
        }
    }


    private void OnWeeklyTick()
    {
        try
        {
            foreach (var hero in Campaign.Current.AliveHeroes)
            {
                if (!_heroes.TryGetValue(hero, out var heroReligionModel)) 
                    continue;

                if (!hero.IsDead)
                {
                    if (hero == Hero.MainHero || MBRandom.RandomInt(0, 100) >= 60) 
                        continue;
                        
                    if (hero.IsPlayerCompanion)
                    {
                        if (!_heroes.ContainsKey(Hero.MainHero))
                        {
                            ChangeRelationAction.ApplyPlayerRelation(hero, -1, true, true);
                        }
                        else
                        {
                            var heroReligionModel2 = _heroes[Hero.MainHero];
                            if (heroReligionModel2.Religion != heroReligionModel.Religion)
                            {
                                if (heroReligionModel2.GetDevotionToCurrentReligion() > 90f)
                                    ChangeRelationAction.ApplyPlayerRelation(hero,
                                        -1 * MBRandom.RandomInt(2, 3), true, true);
                                else if (heroReligionModel2.GetDevotionToCurrentReligion() > 50f)
                                    ChangeRelationAction.ApplyPlayerRelation(hero,
                                        -1 * MBRandom.RandomInt(1, 2), true, true);
                            }
                        }
                    }
                    else if (hero.IsLord)
                    {
                        if (hero.CurrentSettlement != null)
                        {
                            var currentSettlement = hero.CurrentSettlement;
                            Hero hero2;
                            if (currentSettlement == null)
                            {
                                hero2 = null;
                            }
                            else
                            {
                                var ownerClan = currentSettlement.OwnerClan;
                                hero2 = ownerClan != null ? ownerClan.Leader : null;
                            }

                            var hero3 = hero2;
                            if (hero3 != null) RelationshipReligionDecider(hero3, hero, heroReligionModel);
                        }

                        if (hero.MapFaction != null)
                            if (hero.MapFaction.Leader != hero)
                                RelationshipReligionDecider(hero.MapFaction.Leader, hero, heroReligionModel);
                        if (hero.Clan != null && hero.Clan.Leader != hero)
                            RelationshipReligionDecider(hero.Clan.Leader, hero, heroReligionModel);
                    }
                }
                else
                {
                    _heroes.Remove(hero);
                }
            }
        }
        catch
        {
            InformationManager.DisplayMessage(new InformationMessage("Religions OnWeeklyTick Error"));
        }
    }


    private void OnTick(float obj)
    {
        if (!_heroes.ContainsKey(Hero.MainHero)) PromptReligionSelector();
    }


    public void TriggerReligionMenuEvent()
    {
        if (Campaign.Current.MainParty.CurrentSettlement == null &&
            !Campaign.Current.MainParty.IsCurrentlyGoingToSettlement) GameMenu.ActivateGameMenu("religion_menu");
    }


    private void MobilePartyDailyTick(MobileParty party)
    {
        if (party.IsActive && !party.IsBandit && !party.IsCaravan && !party.IsGarrison && !party.IsMilitia &&
            party.LeaderHero != null && !party.IsVillager)
        {
            if (_partyMoraleEffect.ContainsKey(party))
            {
                _partyMoraleEffect[party] = CalculateMoraleBasedOnLeader(party);
                Dictionary<MobileParty, float> partyMoraleEffect = _partyMoraleEffect;
                partyMoraleEffect[party] -= 0.3f;
                return;
            }

            _partyMoraleEffect.Add(party, CalculateMoraleBasedOnLeader(party));
        }
    }


    private float CalculateMoraleBasedOnLeader(MobileParty party)
    {
        if (party.LeaderHero == null) 
            return 0f;
        
        if (!_heroes.TryGetValue(party.LeaderHero, out var heroReligionModel)) 
            return 0f;

        if (!(heroReligionModel.GetDevotionToCurrentReligion() >= 20f)) 
            return 0f;
        
        var array = party.MemberRoster.GetTroopRoster().ToArray();
        var num = (from troopRosterElement 
            in array 
            let rfReligions = ReligionMapHelper.GetCultureReligion(troopRosterElement.Character.Culture.StringId) 
            where heroReligionModel.Religion == rfReligions 
            select troopRosterElement.Number).Sum();

        return 2f * (heroReligionModel.GetDevotionToCurrentReligion() / 100f) /
            (float)party.MemberRoster.TotalManCount * (float)num;
    }


    public void AddMoraleEffectToParty(MobileParty party, float moraleEffect, Core.RFReligions moraleForReligion)
    {
        var num = moraleEffect / (float)party.MemberRoster.TotalManCount;
        foreach (var troopRosterElement in party.MemberRoster.GetTroopRoster())
            if (troopRosterElement.Character != null && troopRosterElement.Character.Culture != null &&
                ReligionMapHelper.GetCultureReligion(troopRosterElement.Character.Culture.StringId) !=
                moraleForReligion)
                moraleEffect -= num;
        if (_partyMoraleEffect.ContainsKey(party))
        {
            Dictionary<MobileParty, float> partyMoraleEffect = _partyMoraleEffect;
            partyMoraleEffect[party] += moraleEffect;
        }
        else
        {
            _partyMoraleEffect.Add(party, moraleEffect);
        }

        if (party.IsMainParty) MobileParty.MainParty.RecentEventsMorale += moraleEffect;
    }


    public float PartyGetMoraleEffect(MobileParty party) => 
        _partyMoraleEffect.TryGetValue(party, out var effect) ? effect : 0f;


    public float SettlementGetLoyaltyEffect(Town town)
    {
        var settlement = town.Settlement;
        if (!settlement.IsTown || settlement.OwnerClan?.Leader == null ||
            !_heroes.TryGetValue(settlement.OwnerClan.Leader, out var heroReligionModel)) 
            return 0f;
        
        var mainReligionRatio = _settlements[settlement].GetMainReligionRatio();
        var num = (double)mainReligionRatio < 0.6 ? mainReligionRatio / 2f * -1f : mainReligionRatio / 2f;
        if (_settlements[settlement].GetMainReligion() != heroReligionModel.Religion) num -= 2f;
        if (!_settlementEffect.ContainsKey(settlement))
            _settlementEffect.Add(settlement, num);
        else
            _settlementEffect[settlement] = num;
        return _settlementEffect[settlement];

    }


    private void DailyHeroTick(Hero hero)
    {
        try
        {
            if (!_heroes.TryGetValue(hero, out var heroReligionModel)) 
                return;
            if (!hero.IsDead)
            {
                if (hero == Hero.MainHero) 
                    return;
                if (!hero.IsPlayerCompanion)
                {
                    if (!hero.IsLord) 
                        return;
                    var num = MBRandom.RandomFloatRanged(2f);
                    if (MBRandom.RandomFloat < 0.5f) num *= -1f;
                    heroReligionModel.AddDevotion(num, hero);
                }
                else
                {
                    var num2 = MBRandom.RandomFloatRanged(2f);
                    if (MBRandom.RandomFloat < 0.4f) num2 *= -1f;
                    heroReligionModel.AddDevotion(num2, hero);
                }
            }
            else
            {
                _heroes.Remove(hero);
            }
        }
        catch
        {
            InformationManager.DisplayMessage(new InformationMessage("Religions DailyHeroTick Error"));
        }
    }


    private void RelationshipReligionDecider(Hero decideWith, Hero subject, HeroReligionModel subjectReligion)
    {
        if (MBRandom.RandomInt(0, 100) >= 40) 
            return;
        
        if (!_heroes.TryGetValue(decideWith, out var heroReligionModel))
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(subject, decideWith, -1, true);
        }
        else
        {
            if (heroReligionModel.Religion != subjectReligion.Religion)
            {
                if (heroReligionModel.GetDevotionToCurrentReligion() > 90f)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(subject, decideWith,
                        -1 * MBRandom.RandomInt(2, 3), true);
                    return;
                }

                if (heroReligionModel.GetDevotionToCurrentReligion() > 50f)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(subject, decideWith,
                        -1 * MBRandom.RandomInt(1, 2), true);
                    return;
                }
            }
            else
            {
                if (heroReligionModel.GetDevotionToCurrentReligion() > 90f &&
                    subjectReligion.GetDevotionToCurrentReligion() > 90f)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(subject, decideWith,
                        MBRandom.RandomInt(3, 4), true);
                    return;
                }

                if (heroReligionModel.GetDevotionToCurrentReligion() > 90f)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(subject, decideWith,
                        MBRandom.RandomInt(1, 2), true);
                    return;
                }
            }
        }
    }


    private void NewGameCreated(CampaignGameStarter obj)
    {
        Setup(false);
        AddGameMenus(obj);
    }


    private void OnSessionLaunched(CampaignGameStarter obj)
    {
        Setup(true);
        AddGameMenus(obj);
    }


    private void AddGameMenus(CampaignGameStarter starter)
    {
        starter.AddPlayerLine("lord_meet_player_response_religious", "lord_meet_player_response", "lord_introduction",
            "{RELIGIOUS_INTRODUCTION}",
            conversation_lord_meet_player_response_religious_on_condition, conversation_lord_meet_player_religious_response_on_consequence);
        /*starter.AddGameMenu("town_temple", GameTexts.FindText("RFRcDRduC").Value,
            new OnInitDelegate(game_menu_temple_enter_on_init), GameOverlays.MenuOverlayType.SettlementWithCharacters,
            GameMenu.MenuFlags.None, null);*/
        starter.AddGameMenuOption("town_temple", "town_temple_faelora", GameTexts.FindText("RFRgHOZTS").Value,
            game_menu_town_enter_sanctum_sapientiae_on_condition,
            x =>
            {
                tempSelecteddReligion = Core.RFReligions.Faelora;
                GameMenu.SwitchToMenu("town_temple_inner");
            });
        starter.AddGameMenuOption("town_temple", "town_temple_anorites", GameTexts.FindText("RFRcVAXGX").Value,
            game_menu_town_enter_caverna_on_condition,
            x =>
            {
                tempSelecteddReligion = Core.RFReligions.Anorites;
                GameMenu.SwitchToMenu("town_temple_inner");
            });
        starter.AddGameMenuOption("town_temple", "town_temple_aeternafide", GameTexts.FindText("RFRBPijwQ").Value,
            game_menu_town_enter_sylvan_sanctum_on_condition,
            x =>
            {
                tempSelecteddReligion = Core.RFReligions.AeternaFide;
                GameMenu.SwitchToMenu("town_temple_inner");
            });
        starter.AddGameMenuOption("town_temple", "town_temple_itan", GameTexts.FindText("RFRFCZtIn").Value,
            game_menu_town_enter_yurt_on_condition, delegate(MenuCallbackArgs x)
            {
                tempSelecteddReligion = Core.RFReligions.Xochxinti;
                GameMenu.SwitchToMenu("town_temple_inner");
            });
        starter.AddGameMenuOption("town_temple", "town_temple_kharazdrathar", GameTexts.FindText("RFRjHkWuQ").Value,
            game_menu_town_enter_khazrak_on_condition,
            delegate(MenuCallbackArgs x)
            {
                tempSelecteddReligion = Core.RFReligions.KharazDrathar;
                GameMenu.SwitchToMenu("town_temple_inner");
            });
        starter.AddGameMenu("town_temple_inner", "{CURRENT_TEMPLE_DESCRIPTION}",
            new OnInitDelegate(game_menu_temple_inner_religion_on_init),
            GameOverlays.MenuOverlayType.SettlementWithCharacters, GameMenu.MenuFlags.None, null);
        starter.AddGameMenuOption("town_temple_inner", "do_donation", GameTexts.FindText("RFR6LDN8i").Value,
            game_menu_temple_donation_on_condition,
            game_menu_temple_donation_on_consequence);
        starter.AddGameMenuOption("town_temple_inner", "do_sacrifice_animal",
            "{SACRIFICE_ACTION_TYPE}{REQUIRED_ANIMALS}",
            game_menu_temple_sacrifice_on_condition,
            game_menu_temple_sacrifice_on_consequence);
        starter.AddGameMenuOption("town_temple_inner", "do_sacrifice_item",
            "{ITEM_WORSHIP_ACTION_TYPE}{REQUIRED_MATERIALS}",
            game_menu_temple_item_worhsip_on_condition,
            game_menu_temple_item_worship_on_consequence);
        starter.AddGameMenuOption("town_temple_inner", "town_inner_temple_back", "{=qWAmxyYz}Back",
            back_on_condition,
            delegate(MenuCallbackArgs x) { GameMenu.SwitchToMenu("town_temple"); }, true, -1, false);
        starter.AddGameMenu("religion_menu", GameTexts.FindText("RFR86PaBs").Value,
            new OnInitDelegate(game_menu_religion_on_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None,
            null);
        starter.AddGameMenuOption("religion_menu", "do_sacrifice_animal", "{SACRIFICE_ACTION_TYPE}{REQUIRED_ANIMALS}",
            game_menu_religion_sacrifice_on_condition,
            game_menu_religion_sacrifice_on_consequence);
        starter.AddGameMenuOption("religion_menu", "convert_religion", GameTexts.FindText("RFR43uirM").Value,
            game_menu_religion_convert_on_condition,
            game_menu_religion_convert_on_consequence);
        starter.AddGameMenuOption("religion_menu", "close", "{=yQtzabbe}Close", null,
            game_menu_religion_close_on_consequence);
    }


    private void conversation_lord_meet_player_religious_response_on_consequence()
    {
        try
        {
            var heroReligionModel = _heroes[Hero.MainHero];
            var heroReligionModel2 = _heroes[Hero.OneToOneConversationHero];
            if (heroReligionModel2.Religion == heroReligionModel.Religion)
            {
                if (heroReligionModel2.GetDevotionToCurrentReligion() > 20f)
                    ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 5, true, true);
            }
            else if (IsThisReligionWelcomedInHere(heroReligionModel.Religion, heroReligionModel2.Religion))
            {
                ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, 1, true, true);
            }
            else
            {
                ChangeRelationAction.ApplyPlayerRelation(Hero.OneToOneConversationHero, -1, true, true);
            }
        }
        catch
        {
        }
    }


    private bool conversation_lord_meet_player_response_religious_on_condition()
    {
        bool result;
        try
        {
            var heroReligionModel = _heroes[Hero.MainHero];
            if (heroReligionModel.GetDevotionToCurrentReligion() < 50f)
            {
                result = false;
            }
            else
            {
                MBTextManager.SetTextVariable("RELIGIOUS_INTRODUCTION",
                    ReligionUIHelper.GetReligionName(heroReligionModel.Religion, Hero.MainHero), false);
                result = true;
            }
        }
        catch
        {
            result = false;
        }

        return result;
    }


    private void game_menu_temple_donation_on_consequence(MenuCallbackArgs args)
    {
        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, DONATION_COST, false);
        AddMoraleEffectToParty(MobileParty.MainParty, 12f, tempSelecteddReligion);
        _heroes[Hero.MainHero].AddDevotion(15f, tempSelecteddReligion, Hero.MainHero);
        
        foreach (var hero in Settlement.CurrentSettlement.Notables
                     .Where(hero => _heroes.ContainsKey(hero))
                     .Where(hero => _heroes[hero].Religion == tempSelecteddReligion))
        {
            ChangeRelationAction.ApplyPlayerRelation(hero, 7, true, true);
        }

        foreach (var hero2 in Campaign.Current.AliveHeroes.Where(hero2 => 
                         _heroes.ContainsKey(hero2) && hero2 is { HasMet: true, IsNotable: false } 
                                                    && _heroes[hero2].Religion == tempSelecteddReligion))
        {
            ChangeRelationAction.ApplyPlayerRelation(hero2, 2, false, false);
        }

        MBInformationManager.AddQuickInformation(GameTexts.FindText("RFRK4p76r"), 0, null, "");
        RefreshCurrentMenu();
    }


    private bool game_menu_temple_donation_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        MBTextManager.SetTextVariable("DONATION", DONATION_COST);
        var flag = true;
        var disabledText = TextObject.Empty;
        if (Hero.MainHero.Gold < DONATION_COST)
        {
            flag = false;
            disabledText = new TextObject("{=m6uSOtE4}You don't have required amount of gold", null);
        }

        args.optionLeaveType = GameMenuOption.LeaveType.Trade;
        return MenuHelper.SetOptionProperties(args, flag, !flag, disabledText);
    }
    
    private void game_menu_temple_item_worship_on_consequence(MenuCallbackArgs args)
    {
        if (ReligionLogicHelper.OfferItems(tempSelecteddReligion, MobileParty.MainParty.ItemRoster))
        {
            AddMoraleEffectToParty(MobileParty.MainParty, 20f, tempSelecteddReligion);
            _heroes[Hero.MainHero].AddDevotion(15f, tempSelecteddReligion, Hero.MainHero);
            foreach (var hero in Settlement.CurrentSettlement.Notables)
                if (_heroes.ContainsKey(hero))
                    if (_heroes[hero].Religion == tempSelecteddReligion)
                        ChangeRelationAction.ApplyPlayerRelation(hero, 7, true, true);
            MBInformationManager.AddQuickInformation(GameTexts.FindText("RFRHr5haI"), 0, null, "");
            RefreshCurrentMenu();
        }
    }

    private bool CanReligionSacrifice(Core.RFReligions rel)
    {
        return rel == Core.RFReligions.Xochxinti || rel == Core.RFReligions.TengralorOrkhai ||
               rel == Core.RFReligions.KharazDrathar;
    }


    private bool ReligionCanItemSacrifice(Core.RFReligions rel)
    {
        return rel <= Core.RFReligions.KharazDrathar;
    }
    private bool game_menu_temple_item_worhsip_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
        if (!ReligionCanItemSacrifice(tempSelecteddReligion)) 
            return false;
        
        if (tempSelecteddReligion == Core.RFReligions.Faelora)
        {
            MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFRQy4KCS"), false);
        }
        else if (tempSelecteddReligion != Core.RFReligions.Anorites)
        {
            if (tempSelecteddReligion != Core.RFReligions.AeternaFide)
            {
                if (tempSelecteddReligion == Core.RFReligions.Xochxinti)
                    MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFRyf8adE"),
                        false);
                else if (tempSelecteddReligion != Core.RFReligions.KharazDrathar)
                    MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFReBIqAx"),
                        false);
                else
                    MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFRUWpP4y"),
                        false);
            }
            else
            {
                MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFRJIXZFV"), false);
            }
        }
        else
        {
            MBTextManager.SetTextVariable("ITEM_WORSHIP_ACTION_TYPE", GameTexts.FindText("RFRogRNxY"), false);
        }

        MBTextManager.SetTextVariable("REQUIRED_MATERIALS",
            ReligionLogicHelper.ReligionTempleOfferItems(tempSelecteddReligion), false);
        var canOfferItems = ReligionLogicHelper.CheckRosterOfferItems(tempSelecteddReligion,
            MobileParty.MainParty.ItemRoster);
        var disabledText = TextObject.Empty;
        
        if (canOfferItems) 
            return MenuHelper.SetOptionProperties(args, canOfferItems, !canOfferItems, disabledText);
        
        var str = GameTexts.FindText("str_warning_crafing_materials").ToString();
        var textObject = ReligionLogicHelper.ReligionTempleOfferItems(tempSelecteddReligion);
        disabledText = new TextObject(str + (textObject != null ? textObject.ToString() : null), null);

        return MenuHelper.SetOptionProperties(args, canOfferItems, !canOfferItems, disabledText);

    }


    private void game_menu_temple_sacrifice_on_consequence(MenuCallbackArgs args)
    {
        if (!ReligionLogicHelper.SacrificeItems(tempSelecteddReligion, 5, MobileParty.MainParty.ItemRoster)) 
            return;
        
        AddMoraleEffectToParty(MobileParty.MainParty, 10f, tempSelecteddReligion);
        _heroes[Hero.MainHero].AddDevotion(15f, tempSelecteddReligion, Hero.MainHero);
        var haveReligionHero = false;
        foreach (var hero in Settlement.CurrentSettlement.Notables
                     .Where(hero => _heroes.ContainsKey(hero) && _heroes[hero].Religion == tempSelecteddReligion))
        {
            ChangeRelationAction.ApplyPlayerRelation(hero, 10, true, true);
            haveReligionHero = true;
        }

        MBInformationManager.AddQuickInformation(
            haveReligionHero ? GameTexts.FindText("RFRtKkVfk") : GameTexts.FindText("RFRPnIr3c"));
        RefreshCurrentMenu();
    }


    private bool game_menu_temple_sacrifice_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
        if (CanReligionSacrifice(tempSelecteddReligion))
        {
            if (tempSelecteddReligion == Core.RFReligions.Xochxinti)
                MBTextManager.SetTextVariable("SACRIFICE_ACTION_TYPE", GameTexts.FindText("RFRGhjVkq"), false);
            else
                MBTextManager.SetTextVariable("SACRIFICE_ACTION_TYPE", GameTexts.FindText("RFRryDfNh"), false);
            MBTextManager.SetTextVariable("REQUIRED_ANIMALS",
                ReligionLogicHelper.ReligionTempleSacrificeText(tempSelecteddReligion), false);
            var flag = ReligionLogicHelper.CheckItemSacrificeItems(tempSelecteddReligion, 5,
                MobileParty.MainParty.ItemRoster);
            var disabledText = TextObject.Empty;
            if (!flag)
            {
                var str = new TextObject("{=str_warning_crafing_materials}You don't have enough materials for ", null).ToString();
                var textObject = ReligionLogicHelper.ReligionTempleSacrificeText(tempSelecteddReligion);
                disabledText = new TextObject(str + (textObject != null ? textObject.ToString() : null), null);
            }

            return MenuHelper.SetOptionProperties(args, flag, !flag, disabledText);
        }

        return false;
    }


    private bool back_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
        return true;
    }


    private void game_menu_temple_inner_religion_on_init(MenuCallbackArgs args)
    {
        var religionTempleDescription = GetReligionTempleDescription(tempSelecteddReligion);
        MBTextManager.SetTextVariable("CURRENT_TEMPLE_DESCRIPTION", religionTempleDescription, false);
        args.MenuContext.SetBackgroundMeshName(GetReligionMenuText(tempSelecteddReligion));
    }


    private bool game_menu_town_enter_khazrak_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        var currentSettlement = Settlement.CurrentSettlement;
        var settlementReligionModel = _settlements[currentSettlement];
        return settlementReligionModel.GetMainReligion() == Core.RFReligions.KharazDrathar ||
               IsThisReligionWelcomedInHere(settlementReligionModel.GetMainReligion(), Core.RFReligions.KharazDrathar);
    }


    private bool game_menu_town_enter_yurt_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        var currentSettlement = Settlement.CurrentSettlement;
        var settlementReligionModel = _settlements[currentSettlement];
        return settlementReligionModel.GetMainReligion() == Core.RFReligions.Xochxinti ||
               IsThisReligionWelcomedInHere(settlementReligionModel.GetMainReligion(), Core.RFReligions.Xochxinti);
    }


    private bool game_menu_town_enter_sylvan_sanctum_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        var currentSettlement = Settlement.CurrentSettlement;
        var settlementReligionModel = _settlements[currentSettlement];
        return settlementReligionModel.GetMainReligion() == Core.RFReligions.AeternaFide ||
               IsThisReligionWelcomedInHere(settlementReligionModel.GetMainReligion(), Core.RFReligions.AeternaFide);
    }


    private bool game_menu_town_enter_caverna_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        var currentSettlement = Settlement.CurrentSettlement;
        var settlementReligionModel = _settlements[currentSettlement];
        return settlementReligionModel.GetMainReligion() == Core.RFReligions.Anorites ||
               IsThisReligionWelcomedInHere(settlementReligionModel.GetMainReligion(), Core.RFReligions.Anorites);
    }


    private bool game_menu_town_enter_sanctum_sapientiae_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
        var currentSettlement = Settlement.CurrentSettlement;
        var settlementReligionModel = _settlements[currentSettlement];
        return settlementReligionModel.GetMainReligion() == Core.RFReligions.Faelora ||
               IsThisReligionWelcomedInHere(settlementReligionModel.GetMainReligion(), Core.RFReligions.Faelora);
    }

    private void game_menu_religion_close_on_consequence(MenuCallbackArgs args)
    {
        GameMenu.ExitToLast();
    }


    private void game_menu_religion_convert_on_consequence(MenuCallbackArgs args)
    {
        PromptReligionSelector();
    }


    private bool game_menu_religion_convert_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
        return _heroes.ContainsKey(Hero.MainHero);
    }


    private void game_menu_religion_sacrifice_on_consequence(MenuCallbackArgs args)
    {
        var heroReligionModel = _heroes[Hero.MainHero];
        if (ReligionLogicHelper.SacrificeItems(heroReligionModel.Religion,
                MobileParty.MainParty.MemberRoster.TotalManCount / 10, MobileParty.MainParty.ItemRoster))
        {
            AddMoraleEffectToParty(MobileParty.MainParty, 10f, heroReligionModel.Religion);
            heroReligionModel.AddDevotion(15f, Hero.MainHero);
            RefreshCurrentMenu();
        }
    }


    private bool game_menu_religion_sacrifice_on_condition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
        if (!_heroes.TryGetValue(Hero.MainHero, out var heroReligionModel)) 
            return false;
        
        if (!CanReligionSacrifice(heroReligionModel.Religion)) 
            return false;

        MBTextManager.SetTextVariable("SACRIFICE_ACTION_TYPE",
            heroReligionModel.Religion == Core.RFReligions.Xochxinti
                ? GameTexts.FindText("RFRGhjVkq")
                : GameTexts.FindText("RFRryDfNh"), false);
        
        MBTextManager.SetTextVariable("REQUIRED_ANIMALS",
            ReligionLogicHelper.ReligionSacrificeText(heroReligionModel.Religion,
                MobileParty.MainParty.MemberRoster.TotalManCount), false);
        
        var requiredItemCount = MobileParty.MainParty.MemberRoster.TotalManCount / 10;
        var canReligionSacrificeItems = ReligionLogicHelper.CheckItemSacrificeItems(heroReligionModel.Religion, requiredItemCount,
            MobileParty.MainParty.ItemRoster);
        var disabledText = TextObject.Empty;
        if (canReligionSacrificeItems) 
            return MenuHelper.SetOptionProperties(args, canReligionSacrificeItems, !canReligionSacrificeItems, disabledText);
        var str = new TextObject("{=str_warning_crafing_materials}You don't have enough materials for ", null).ToString();
        var textObject = ReligionLogicHelper.ReligionSacrificeText(heroReligionModel.Religion,
            MobileParty.MainParty.MemberRoster.TotalManCount);
        disabledText = new TextObject(str + (textObject != null ? textObject.ToString() : null), null);

        return MenuHelper.SetOptionProperties(args, canReligionSacrificeItems, !canReligionSacrificeItems, disabledText);
    }


    private void game_menu_religion_on_init(MenuCallbackArgs args)
    {
        if (!_heroes.ContainsKey(Hero.MainHero))
        {
            PromptReligionSelector();
            return;
        }

        var heroReligionModel = _heroes[Hero.MainHero];
        args.MenuContext.SetBackgroundMeshName(GetReligionMenuText(heroReligionModel.Religion));
        SetReligionMenuTexts();
    }


    private void SetReligionMenuTexts()
    {
        var heroReligionModel = _heroes[Hero.MainHero];
        var religionDescription = GetReligionDescription(heroReligionModel.Religion);
        MBTextManager.SetTextVariable("PLAYER_RELIGION", ReligionUIHelper.GetReligionName(heroReligionModel.Religion),
            false);
        MBTextManager.SetTextVariable("DEVOTION", heroReligionModel.GetDevotionToCurrentReligion());
        var rel = ReligionMapHelper.GetCultureReligion(Hero.MainHero.Culture.StringId);
        MBTextManager.SetTextVariable("ANC_BELIEF", ReligionUIHelper.GetReligionName(rel), false);
        MBTextManager.SetTextVariable("RELIGION_SPECIFIC_REQIREMENTS", religionDescription, false);
        if (Campaign.Current.CurrentMenuContext != null)
            Campaign.Current.CurrentMenuContext.SetBackgroundMeshName(GetReligionMenuText(heroReligionModel.Religion));
    }


    private void PromptReligionSelector()
    {
        List<InquiryElement> list = new();
        foreach (var obj in Enum.GetValues(typeof(Core.RFReligions)))
        {
            var rfReligions = (Core.RFReligions)obj;
            if (rfReligions != Core.RFReligions.PharunAegis && rfReligions != Core.RFReligions.VyralethAmara 
                                                            && rfReligions != Core.RFReligions.TengralorOrkhai 
                                                            && rfReligions != _heroes[Hero.MainHero].Religion)
            {
                list.Add(new InquiryElement(rfReligions.ToString(),
                    ReligionUIHelper.GetReligionName(rfReligions).ToString(), null, true, ""));
            }
        }

        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
        MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
            GameTexts.FindText("RFRK7QMgB").ToString(),
            GameTexts.FindText("RFR51usdh").ToString(),
            list, true, 1, 1, new TextObject("{=WiNRdfsm}Done", null).ToString(),
            new TextObject("{=Giblfhdx}Cancel", null).ToString(),
            new Action<List<InquiryElement>>(ReligionSelected), null, ""), true);
    }


    private void ReligionSelected(List<InquiryElement> inqury)
    {
        var inquiryElement = inqury.First<InquiryElement>();
        var rfReligions = (Core.RFReligions)Enum.Parse(typeof(Core.RFReligions), inquiryElement.Identifier.ToString());
        if (!_heroes.ContainsKey(Hero.MainHero))
        {
            _heroes.Add(Hero.MainHero, new HeroReligionModel(rfReligions, 20f));
        }
        else
        {
            _heroes[Hero.MainHero].ConvertReligion(rfReligions);
            _heroes[Hero.MainHero].AddDevotion(20f, Hero.MainHero);
        }

        RefreshCurrentMenu();
        SetReligionMenuTexts();
    }


    private void RefreshCurrentMenu()
    {
        try
        {
            if (Campaign.Current.CurrentMenuContext != null)
                Campaign.Current.GameMenuManager.RefreshMenuOptions(Campaign.Current.CurrentMenuContext);
        }
        catch (Exception)
        {
        }
    }


    private TextObject GetReligionDescription(Core.RFReligions religion)
    {
        return religion switch
        {
            Core.RFReligions.Faelora => GameTexts.FindText("RFRaSG29N"),
            Core.RFReligions.AeternaFide => GameTexts.FindText("RFR2uCoyB"),
            Core.RFReligions.Anorites => GameTexts.FindText("RFRPZgSSM"),
            Core.RFReligions.Xochxinti => GameTexts.FindText("RFRY1YWE4"),
            Core.RFReligions.KharazDrathar => GameTexts.FindText("RFR3uBlHr"),
            Core.RFReligions.PharunAegis => GameTexts.FindText("RFRkIKSjR"),
            Core.RFReligions.TengralorOrkhai => GameTexts.FindText("RFRaiHGCf"),
            Core.RFReligions.VyralethAmara => GameTexts.FindText("RFRTlmiDH"),
            _ => GameTexts.FindText("RFRaSG29N")
        };
    }


    private TextObject GetReligionTempleDescription(Core.RFReligions religion)
    {
        return religion switch
        {
            Core.RFReligions.Faelora => GameTexts.FindText("RFRBkSTan"),
            Core.RFReligions.AeternaFide => GameTexts.FindText("RFRvm1ntw"),
            Core.RFReligions.Anorites => GameTexts.FindText("RFRiuCYDv"),
            Core.RFReligions.Xochxinti => GameTexts.FindText("RFRfh0egR"),
            Core.RFReligions.KharazDrathar => GameTexts.FindText("RFRR0xJoc"),
            _ => TextObject.Empty
        };
    }


    private bool IsThisReligionWelcomedInHere(Core.RFReligions mainReligion, Core.RFReligions shouldWelcome)
    {
        return ReligionLogicHelper.TolerableReligions[mainReligion] == Core.RFReligions.All ||
               ReligionLogicHelper.TolerableReligions[mainReligion] == shouldWelcome;
    }


    private string GetReligionMenuText(Core.RFReligions rel)
    {
        switch (rel)
        {
            case Core.RFReligions.Faelora:
                return "empire_keep";
            case Core.RFReligions.AeternaFide:
            case Core.RFReligions.PharunAegis:
                return "sturgia_keep";
            case Core.RFReligions.Anorites:
            case Core.RFReligions.TengralorOrkhai:
            case Core.RFReligions.VyralethAmara:
                return "aserai_keep";
            case Core.RFReligions.Xochxinti:
                return "khuzait_keep";
            case Core.RFReligions.KharazDrathar:
                return "battania_keep";
            default:
                return "empire_keep";
        }
    }


    private void Setup(bool includePlayer = true)
    {
        try
        {
            if (_settlements == null || _settlements.Count == 0)
            {
                _settlements = new Dictionary<Settlement, SettlementReligionModel>();
                foreach (var settlement in Campaign.Current.Settlements)
                    if (settlement.IsTown)
                    {
                        var settlementReligionModel = new SettlementReligionModel(settlement);
                        var rel = ReligionMapHelper.GetCultureReligion(settlement.Culture.StringId);
                        var num = settlement.Town.Prosperity * 0.8f;
                        var num2 = settlement.Town.Prosperity - num;
                        settlementReligionModel.AddDevotionToReligion(rel, num);
                        foreach (var obj in Enum.GetValues(typeof(Core.RFReligions)))
                        {
                            var rel2 = (Core.RFReligions)obj;
                            if (num2 < 1f) break;
                            var num3 = MBRandom.RandomFloatRanged(num2);
                            num2 -= num3;
                            settlementReligionModel.AddDevotionToReligion(rel2, num3);
                        }

                        _settlements.Add(settlement, settlementReligionModel);
                    }
            }

            if (_heroes != null && _heroes.Count != 0) 
                return;
            
            _heroes = new Dictionary<Hero, HeroReligionModel>();
            foreach (var hero in Campaign.Current.AliveHeroes)
                if ((hero != Hero.MainHero || includePlayer) && hero.IsAlive)
                {
                    var culture = hero.Culture;
                    var cultureString = culture?.StringId;
                    if (hero.Clan is { IsMinorFaction: true })
                    {
                        if (!hero.Clan.StringId.Contains("ember"))
                        {
                            if (hero.Clan.StringId.Contains("tengralororkhai")) cultureString = "tengralororkhai";
                        }
                        else
                        {
                            cultureString = "ember";
                        }
                    }

                    var value = new HeroReligionModel(ReligionMapHelper.GetCultureReligion(cultureString),
                        MBRandom.RandomInt(20, 75));
                    _heroes.Add(hero, value);
                }
        }
        catch
        {
            InformationManager.DisplayMessage(new InformationMessage("Religions Setup Error"));
        }
    }


    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("religionSettlements", ref _settlements);
        dataStore.SyncData("religionHeroes", ref _heroes);
        dataStore.SyncData("religionPartyMorale", ref _partyMoraleEffect);
        dataStore.SyncData("religionSettlementEffect", ref _settlementEffect);
        Setup();
    }


    [SaveableField(1)] public Dictionary<Settlement, SettlementReligionModel> _settlements;
    [SaveableField(2)] public Dictionary<Hero, HeroReligionModel> _heroes;
    [SaveableField(3)] private Dictionary<MobileParty, float> _partyMoraleEffect;
    [SaveableField(4)] private Dictionary<Settlement, float> _settlementEffect;
    
    private Core.RFReligions tempSelecteddReligion;
    private int DONATION_COST = 8300;
}