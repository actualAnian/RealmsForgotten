using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.InciteBreak;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Campaign;

public sealed class StrategicIntrigueConversationBehavior : CampaignBehaviorBase
{
    private readonly HashSet<Clan> _pactsStartedThisConversation = new();
    private readonly HashSet<Clan> _alliancesStartedThisConversation = new();
    private readonly HashSet<Clan> _rumorsStartedThisConversation = new();
    private readonly HashSet<Clan> _foreignAlliancesStartedThisConversation = new();
    private TextObject _lastAlignmentResponse = TextObject.GetEmpty();
    private TextObject _lastPactResponse = TextObject.GetEmpty();
    private TextObject _lastAllianceResponse = TextObject.GetEmpty();
    private TextObject _lastRumorResponse = TextObject.GetEmpty();
    private TextObject _lastBreakResponse = TextObject.GetEmpty();
    private TextObject _lastPactStatusResponse = TextObject.GetEmpty();
    private TextObject _lastAllianceStatusResponse = TextObject.GetEmpty();
    private TextObject _lastRumorStatusResponse = TextObject.GetEmpty();
    private TextObject _lastObjectiveResponse = TextObject.GetEmpty();
    private TextObject _lastObjectiveSupportResponse = TextObject.GetEmpty();
    private TextObject _lastForeignIntroResponse = TextObject.GetEmpty();
    private TextObject _lastForeignAllianceResponse = TextObject.GetEmpty();
    private TextObject _lastForeignAllianceStatusResponse = TextObject.GetEmpty();

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        starter.AddPlayerLine(
            "rf_si_open_menu",
            "hero_main_options",
            "rf_si_menu_intro",
            "{=rf_si_open_menu}I want to speak privately about the politics of your realm.",
            CanDiscussIntrigue,
            null);
        starter.AddDialogLine(
            "rf_si_menu_intro_line",
            "rf_si_menu_intro",
            "rf_si_menu_options",
            "{=rf_si_menu_intro_line}Very well. Speak plainly.",
            null,
            null);

        starter.AddPlayerLine(
            "rf_si_probe_alignment_player",
            "rf_si_menu_options",
            "rf_si_probe_alignment_response",
            "{=rf_si_probe_alignment_player}Tell me honestly. How stable is your kingdom?",
            CanDiscussIntrigue,
            PrepareAlignmentLine);
        starter.AddDialogLine(
            "rf_si_probe_alignment_npc",
            "rf_si_probe_alignment_response",
            "rf_si_menu_options",
            "{=rf_si_probe_alignment_npc}{SI_ALIGNMENT_TEXT}",
            SetAlignmentLine,
            null);

        starter.AddPlayerLine(
            "rf_si_objective_player",
            "rf_si_menu_options",
            "rf_si_objective_response",
            "{=rf_si_objective_player}What greater design drives this realm?",
            CanDiscussKingdomObjective,
            PrepareObjectiveLine);
        starter.AddDialogLine(
            "rf_si_objective_npc",
            "rf_si_objective_response",
            "rf_si_menu_options",
            "{=rf_si_objective_npc}{SI_OBJECTIVE_TEXT}",
            SetObjectiveLine,
            null);
        starter.AddPlayerLine(
            "rf_si_objective_support_player",
            "rf_si_menu_options",
            "rf_si_objective_support_response",
            "{=rf_si_objective_support_player}Then I will put my strength behind that design.",
            CanSupportKingdomObjective,
            SupportKingdomObjective);
        starter.AddDialogLine(
            "rf_si_objective_support_npc",
            "rf_si_objective_support_response",
            "rf_si_menu_options",
            "{=rf_si_objective_support_npc}{SI_OBJECTIVE_SUPPORT_TEXT}",
            SetObjectiveSupportLine,
            null);

        starter.AddPlayerLine(
            "rf_si_offer_pact_player",
            "rf_si_menu_options",
            "rf_si_offer_pact_response",
            "{=rf_si_offer_pact_player}Some rulers can be opposed without open war.",
            CanOfferSecretPact,
            OfferSecretPact);
        starter.AddDialogLine(
            "rf_si_offer_pact_npc",
            "rf_si_offer_pact_response",
            "rf_si_menu_options",
            "{=rf_si_offer_pact_npc}{SI_PACT_TEXT}",
            SetPactLine,
            null);
        starter.AddPlayerLine(
            "rf_si_review_pact_player",
            "rf_si_menu_options",
            "rf_si_review_pact_response",
            "{=rf_si_review_pact_player}And our private understanding?",
            CanReviewSecretPact,
            PreparePactStatus);
        starter.AddDialogLine(
            "rf_si_review_pact_npc",
            "rf_si_review_pact_response",
            "rf_si_menu_options",
            "{=rf_si_review_pact_npc}{SI_PACT_STATUS_TEXT}",
            SetPactStatusLine,
            null);

        starter.AddPlayerLine(
            "rf_si_offer_alliance_player",
            "rf_si_menu_options",
            "rf_si_offer_alliance_response",
            "{=rf_si_offer_alliance_player}If I back you in earnest, what do I gain when the realm breaks?",
            CanOfferAllianceTerms,
            OfferAllianceTerms);
        starter.AddDialogLine(
            "rf_si_offer_alliance_npc",
            "rf_si_offer_alliance_response",
            "rf_si_menu_options",
            "{=rf_si_offer_alliance_npc}{SI_ALLIANCE_TEXT}",
            SetAllianceLine,
            null);
        starter.AddPlayerLine(
            "rf_si_review_alliance_player",
            "rf_si_menu_options",
            "rf_si_review_alliance_response",
            "{=rf_si_review_alliance_player}And the price of our alliance?",
            CanReviewAllianceTerms,
            PrepareAllianceStatus);
        starter.AddDialogLine(
            "rf_si_review_alliance_npc",
            "rf_si_review_alliance_response",
            "rf_si_menu_options",
            "{=rf_si_review_alliance_npc}{SI_ALLIANCE_STATUS_TEXT}",
            SetAllianceStatusLine,
            null);

        starter.AddPlayerLine(
            "rf_si_offer_rumor_player",
            "rf_si_menu_options",
            "rf_si_offer_rumor_response",
            "{=rf_si_offer_rumor_player}Perhaps your liege deserves a few carefully placed whispers.",
            CanStartRumorCampaign,
            StartRumorCampaign);
        starter.AddDialogLine(
            "rf_si_offer_rumor_npc",
            "rf_si_offer_rumor_response",
            "rf_si_menu_options",
            "{=rf_si_offer_rumor_npc}{SI_RUMOR_TEXT}",
            SetRumorLine,
            null);
        starter.AddPlayerLine(
            "rf_si_review_rumor_player",
            "rf_si_menu_options",
            "rf_si_review_rumor_response",
            "{=rf_si_review_rumor_player}Are the whispers spreading?",
            CanReviewRumorCampaign,
            PrepareRumorStatus);
        starter.AddDialogLine(
            "rf_si_review_rumor_npc",
            "rf_si_review_rumor_response",
            "rf_si_menu_options",
            "{=rf_si_review_rumor_npc}{SI_RUMOR_STATUS_TEXT}",
            SetRumorStatusLine,
            null);

        starter.AddPlayerLine(
            "rf_si_offer_break_player",
            "rf_si_menu_options",
            "rf_si_offer_break_response",
            "{=rf_si_offer_break_player}If you mean to break from this realm, I can help tip the scales.",
            CanInciteBreak,
            InciteBreak);
        starter.AddDialogLine(
            "rf_si_offer_break_npc",
            "rf_si_offer_break_response",
            "rf_si_menu_options",
            "{=rf_si_offer_break_npc}{SI_BREAK_TEXT}",
            SetBreakLine,
            null);

        starter.AddPlayerLine(
            "rf_si_menu_leave",
            "rf_si_menu_options",
            "lord_pretalk",
            "{=rf_si_menu_leave}That is all for now.",
            null,
            null);

        starter.AddPlayerLine(
            "rf_si_open_foreign_menu",
            "hero_main_options",
            "rf_si_foreign_intro",
            "{=rf_si_open_foreign_menu}I have a discreet proposal concerning another crown.",
            CanDiscussForeignAlliance,
            PrepareForeignAllianceIntro);
        starter.AddDialogLine(
            "rf_si_foreign_intro_line",
            "rf_si_foreign_intro",
            "rf_si_foreign_options",
            "{=rf_si_foreign_intro_line}{SI_FOREIGN_INTRO_TEXT}",
            SetForeignAllianceIntroLine,
            null);
        starter.AddPlayerLine(
            "rf_si_offer_foreign_alliance_player",
            "rf_si_foreign_options",
            "rf_si_offer_foreign_alliance_response",
            "{=rf_si_offer_foreign_alliance_player}If the realm I name fractures, will your banners move for a border prize?",
            CanOfferForeignAlliance,
            OfferForeignAlliance);
        starter.AddDialogLine(
            "rf_si_offer_foreign_alliance_npc",
            "rf_si_offer_foreign_alliance_response",
            "rf_si_foreign_options",
            "{=rf_si_offer_foreign_alliance_npc}{SI_FOREIGN_ALLIANCE_TEXT}",
            SetForeignAllianceLine,
            null);
        starter.AddPlayerLine(
            "rf_si_review_foreign_alliance_player",
            "rf_si_foreign_options",
            "rf_si_review_foreign_alliance_response",
            "{=rf_si_review_foreign_alliance_player}And our discreet understanding?",
            CanReviewForeignAlliance,
            PrepareForeignAllianceStatus);
        starter.AddDialogLine(
            "rf_si_review_foreign_alliance_npc",
            "rf_si_review_foreign_alliance_response",
            "rf_si_foreign_options",
            "{=rf_si_review_foreign_alliance_npc}{SI_FOREIGN_ALLIANCE_STATUS_TEXT}",
            SetForeignAllianceStatusLine,
            null);
        starter.AddPlayerLine(
            "rf_si_foreign_leave",
            "rf_si_foreign_options",
            "lord_pretalk",
            "{=rf_si_foreign_leave}That is all for now.",
            null,
            null);

        starter.AddPlayerLine(
            "rf_si_companion_espionage_player",
            "hero_main_options",
            "close_window",
            "{=rf_si_companion_espionage_player}I need you to gather intelligence for me.",
            CanOpenCompanionEspionageMenu,
            OpenCompanionEspionageMenu);
        starter.AddPlayerLine(
            "rf_si_companion_espionage_report_player",
            "hero_main_options",
            "close_window",
            "{=rf_si_companion_espionage_report_player}Report on your latest intelligence work.",
            CanReviewCompanionEspionage,
            ReviewCompanionEspionage);
    }

    private void OnConversationEnded(IEnumerable<CharacterObject> _)
    {
        _pactsStartedThisConversation.Clear();
        _alliancesStartedThisConversation.Clear();
        _rumorsStartedThisConversation.Clear();
        _foreignAlliancesStartedThisConversation.Clear();
    }

    private bool CanDiscussIntrigue()
    {
        Hero hero = Hero.OneToOneConversationHero;
        return IsPoliticalLord(hero);
    }

    private void PrepareAlignmentLine()
    {
        Hero hero = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        ClanIntrigueState state = behavior?.GetState(hero?.Clan);
        KingdomIntrigueState kingdomState = behavior?.GetKingdomState(hero?.Clan?.Kingdom);

        _lastAlignmentResponse = BuildAlignmentResponse(hero, state, kingdomState);
    }

    private bool SetAlignmentLine()
    {
        MBTextManager.SetTextVariable("SI_ALIGNMENT_TEXT", _lastAlignmentResponse.ToString());
        return true;
    }

    private bool CanDiscussKingdomObjective()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsPoliticalLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return behavior?.HasKingdomObjective(hero.Clan?.Kingdom) == true;
    }

    private void PrepareObjectiveLine()
    {
        Hero hero = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        _lastObjectiveResponse = behavior?.GetKingdomObjectiveBriefing(hero?.Clan?.Kingdom)
            ?? new TextObject("{=rf_ko_briefing_missing}There is no settled grand design here to speak of.");
    }

    private bool SetObjectiveLine()
    {
        MBTextManager.SetTextVariable("SI_OBJECTIVE_TEXT", _lastObjectiveResponse.ToString());
        return true;
    }

    private bool CanSupportKingdomObjective()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsPoliticalLord(hero) || hero?.Clan?.Kingdom == null)
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return Hero.MainHero?.Clan?.Kingdom == hero.Clan.Kingdom
            && hero.Clan == hero.Clan.Kingdom.RulingClan
            && behavior?.HasKingdomObjective(hero.Clan.Kingdom) == true
            && behavior.IsPlayerSupportingObjective(hero.Clan.Kingdom) == false;
    }

    private void SupportKingdomObjective()
    {
        Hero hero = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        _lastObjectiveSupportResponse = new TextObject("{=rf_ko_support_default}Words are cheap. We will see if your banner truly follows our design.");
        if (behavior == null || hero?.Clan?.Kingdom == null)
        {
            return;
        }

        if (behavior.TrySupportKingdomObjective(hero.Clan.Kingdom, out TextObject response))
        {
            _lastObjectiveSupportResponse = response;
            TextObject supportMessage = new TextObject("{=rf_ko_support_started_msg}Your clan is now backing the grand design of {KINGDOM}.");
            supportMessage.SetTextVariable("KINGDOM", hero.Clan.Kingdom.Name);
            ShowIntrigueMessage(supportMessage);
        }
        else
        {
            _lastObjectiveSupportResponse = response;
        }
    }

    private bool SetObjectiveSupportLine()
    {
        MBTextManager.SetTextVariable("SI_OBJECTIVE_SUPPORT_TEXT", _lastObjectiveSupportResponse.ToString());
        return true;
    }

    private bool CanOfferSecretPact()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        ClanIntrigueState state = behavior?.GetState(hero.Clan);
        return state != null
            && state.IsConspirable
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.SecretPactRelationThreshold
            && state.TrustToPlayer >= StrategicIntrigueConstants.SecretPactTrustThreshold
            && !behavior.HasActivePact(hero.Clan);
    }

    private bool SetPactLine()
    {
        MBTextManager.SetTextVariable("SI_PACT_TEXT", _lastPactResponse.ToString());
        return true;
    }

    private bool CanReviewSecretPact()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return behavior?.HasPlayerPact(hero.Clan) == true
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.SecretPactRelationThreshold
            && !_pactsStartedThisConversation.Contains(hero.Clan);
    }

    private void PreparePactStatus()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        Hero hero = Hero.OneToOneConversationHero;
        ClanIntrigueState state = behavior?.GetState(hero?.Clan);
        IntriguePactGoal? goal = behavior?.GetActivePactGoal(hero?.Clan);

        _lastPactStatusResponse = goal switch
        {
            IntriguePactGoal.PrepareProtectedVassalage => new TextObject("{=rf_si_pact_status_vassalage}I have not forgotten. If the realm stumbles, I know where I will seek shelter."),
            IntriguePactGoal.SupportFutureClaimant => new TextObject("{=rf_si_pact_status_claimant}The thought remains alive. A weaker crown invites stronger hands."),
            IntriguePactGoal.BreakAwayFromKingdom => new TextObject("{=rf_si_pact_status_breakaway}The bond still holds. When the moment comes, we cut loose cleanly."),
            _ => new TextObject("{=rf_si_pact_status_default}The understanding stands. We speak of it to no one.")
        };

        if (state != null && !state.IsBreakawayReady)
        {
            _lastPactStatusResponse = new TextObject("{=rf_si_pact_status_not_ready}The understanding stands, but the realm is not cracked open yet. More strain is needed.");
        }
    }

    private bool SetPactStatusLine()
    {
        MBTextManager.SetTextVariable("SI_PACT_STATUS_TEXT", _lastPactStatusResponse.ToString());
        return true;
    }

    private bool CanOfferAllianceTerms()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        ClanIntrigueState state = behavior?.GetState(hero.Clan);
        return state != null
            && behavior.HasPlayerPact(hero.Clan)
            && behavior.GetPlayerAllianceWithClan(hero.Clan) == null
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.AllianceRelationThreshold
            && state.TrustToPlayer >= StrategicIntrigueConstants.AllianceTrustThreshold;
    }

    private bool CanReviewAllianceTerms()
    {
        Hero hero = Hero.OneToOneConversationHero;
        return IsIntrigueLord(hero)
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.AllianceRelationThreshold
            && !_alliancesStartedThisConversation.Contains(hero.Clan)
            && global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>()?.GetPlayerAllianceWithClan(hero.Clan) != null;
    }

    private void OfferAllianceTerms()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        Hero hero = Hero.OneToOneConversationHero;
        _lastAllianceResponse = new TextObject("{=rf_si_alliance_default_reply}A bargain this sharp cannot be spoken lightly.");

        if (behavior == null || hero?.Clan == null)
        {
            return;
        }

        if (behavior.TryCreatePlayerAlliance(hero.Clan, out TextObject reason, out SecretAllianceCompact alliance))
        {
            _alliancesStartedThisConversation.Add(hero.Clan);
            _lastAllianceResponse = new TextObject("{=rf_si_alliance_accept}Then let us speak plainly. Stand behind me, and {SETTLEMENT} will be yours when the realm breaks our way.");
            _lastAllianceResponse.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}a border fief"));

            TextObject allianceMessage = new TextObject("{=rf_si_alliance_started_msg}Alliance terms set with {CLAN}. {SETTLEMENT} has been promised.");
            allianceMessage.SetTextVariable("CLAN", hero.Clan.Name);
            allianceMessage.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}a border fief"));
            ShowIntrigueMessage(allianceMessage);
        }
        else
        {
            _lastAllianceResponse = reason;
        }
    }

    private void PrepareAllianceStatus()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        SecretAllianceCompact alliance = behavior?.GetPlayerAllianceWithClan(Hero.OneToOneConversationHero?.Clan);

        _lastAllianceStatusResponse = new TextObject("{=rf_si_alliance_status_none}There is no sharper bargain to review.");
        if (alliance == null)
        {
            return;
        }

        _lastAllianceStatusResponse = alliance.IsSupportTriggered
            ? new TextObject("{=rf_si_alliance_status_triggered}The bargain has moved beyond whispers. {SETTLEMENT} remains the price of my success.")
            : new TextObject("{=rf_si_alliance_status_waiting}The terms stand. If I rise cleanly, {SETTLEMENT} passes to you.");
        _lastAllianceStatusResponse.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}the promised fief"));
    }

    private bool SetAllianceLine()
    {
        MBTextManager.SetTextVariable("SI_ALLIANCE_TEXT", _lastAllianceResponse.ToString());
        return true;
    }

    private bool SetAllianceStatusLine()
    {
        MBTextManager.SetTextVariable("SI_ALLIANCE_STATUS_TEXT", _lastAllianceStatusResponse.ToString());
        return true;
    }

    private void OfferSecretPact()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        _lastPactResponse = new TextObject("{=rf_si_default_pact_reply}I will remember this approach.");

        if (behavior == null || Hero.OneToOneConversationHero?.Clan == null)
        {
            return;
        }

        if (behavior.TryCreateSecretPact(Clan.PlayerClan, Hero.OneToOneConversationHero.Clan, out TextObject reason))
        {
            _pactsStartedThisConversation.Add(Hero.OneToOneConversationHero.Clan);
            IntriguePactGoal? goal = behavior.GetActivePactGoal(Hero.OneToOneConversationHero.Clan);
            _lastPactResponse = goal switch
            {
                IntriguePactGoal.PrepareProtectedVassalage => new TextObject("{=rf_si_pact_vassalage}Then when the realm cracks, my banners will not rise for my current liege."),
                IntriguePactGoal.SupportFutureClaimant => new TextObject("{=rf_si_pact_claimant}Then remember this hour. Some crowns are lighter than they look."),
                IntriguePactGoal.BreakAwayFromKingdom => new TextObject("{=rf_si_pact_breakaway}Then let this remain between us. We prepare for a severing, not a complaint."),
                _ => new TextObject("{=rf_si_pact_accepted}Then let this remain between us. For now.")
            };
            TextObject pactMessage = new TextObject("{=rf_si_pact_started_msg}Secret understanding reached with {CLAN}.");
            pactMessage.SetTextVariable("CLAN", Hero.OneToOneConversationHero.Clan.Name);
            ShowIntrigueMessage(pactMessage);
        }
        else
        {
            _lastPactResponse = reason;
        }
    }

    private bool CanStartRumorCampaign()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        ClanIntrigueState state = behavior?.GetState(hero.Clan);
        return state != null
            && state.Dissidence >= 45f
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.RumorCampaignRelationThreshold
            && state.TrustToPlayer >= StrategicIntrigueConstants.RumorCampaignTrustThreshold
            && !behavior.HasPendingRumorCampaign(hero.Clan);
    }

    private bool SetRumorLine()
    {
        MBTextManager.SetTextVariable("SI_RUMOR_TEXT", _lastRumorResponse.ToString());
        return true;
    }

    private bool CanReviewRumorCampaign()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return behavior?.HasPendingPlayerRumorCampaign(hero.Clan) == true
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.RumorCampaignRelationThreshold
            && !_rumorsStartedThisConversation.Contains(hero.Clan);
    }

    private void PrepareRumorStatus()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        float? daysRemaining = behavior?.GetPendingPlayerRumorDaysRemaining(Hero.OneToOneConversationHero?.Clan);

        _lastRumorStatusResponse = new TextObject("{=rf_si_rumor_status_default}The whispers are moving through the court. Give them a little more time.");
        if (daysRemaining.HasValue)
        {
            _lastRumorStatusResponse = new TextObject("{=rf_si_rumor_status_days}The whispers are moving. Come back in about {DAYS} days and we will see what took root.");
            _lastRumorStatusResponse.SetTextVariable("DAYS", daysRemaining.Value.ToString("0.0"));
        }
    }

    private bool SetRumorStatusLine()
    {
        MBTextManager.SetTextVariable("SI_RUMOR_STATUS_TEXT", _lastRumorStatusResponse.ToString());
        return true;
    }

    private void StartRumorCampaign()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        _lastRumorResponse = new TextObject("{=rf_si_default_rumor_reply}I will weigh the risk carefully.");

        if (behavior == null || Hero.OneToOneConversationHero?.Clan == null)
        {
            return;
        }

        if (behavior.TryStartRumorCampaign(Clan.PlayerClan, Hero.OneToOneConversationHero.Clan, out TextObject reason))
        {
            _rumorsStartedThisConversation.Add(Hero.OneToOneConversationHero.Clan);
            _lastRumorResponse = new TextObject("{=rf_si_rumor_accepted}Then let the court hear what it is ready to believe. Give the whispers a few days.");
            TextObject rumorMessage = new TextObject("{=rf_si_rumor_started_msg}Rumor campaign started against {CLAN}.");
            rumorMessage.SetTextVariable("CLAN", Hero.OneToOneConversationHero.Clan.Name);
            ShowIntrigueMessage(rumorMessage);
        }
        else
        {
            _lastRumorResponse = reason;
        }
    }

    private bool CanInciteBreak()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsIntrigueLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        ClanIntrigueState state = behavior?.GetState(hero.Clan);
        return state != null
            && state.IsBreakawayReady
            && behavior.HasPlayerPact(hero.Clan)
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.BreakawayRelationThreshold
            && state.TrustToPlayer >= StrategicIntrigueConstants.SecretPactTrustThreshold;
    }

    private bool SetBreakLine()
    {
        MBTextManager.SetTextVariable("SI_BREAK_TEXT", _lastBreakResponse.ToString());
        return true;
    }

    private void InciteBreak()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        _lastBreakResponse = new TextObject("{=rf_si_default_break_reply}I will consider how far I am willing to go.");

        if (behavior == null || Hero.OneToOneConversationHero?.Clan == null)
        {
            return;
        }

        if (behavior.TryInciteBreak(Hero.OneToOneConversationHero.Clan, out TextObject reason, out IntrigueBreakOutcome outcome))
        {
            _lastBreakResponse = outcome switch
            {
                IntrigueBreakOutcome.Defection => new TextObject("{=rf_si_break_defection}Then it is settled. My banners will abandon this realm and turn toward yours."),
                IntrigueBreakOutcome.ClaimantCoup => new TextObject("{=rf_si_break_claimant}Then we stop whispering. The crown itself will change hands."),
                _ => new TextObject("{=rf_si_break_accepted}Then the old bonds are finished. Let the realm choke on the news.")
            };
            TextObject breakMessage = outcome switch
            {
                IntrigueBreakOutcome.Defection => new TextObject("{=rf_si_break_defection_msg}{CLAN} has defected as planned."),
                IntrigueBreakOutcome.ClaimantCoup => new TextObject("{=rf_si_break_claimant_msg}{CLAN} has moved for the crown."),
                _ => new TextObject("{=rf_si_break_started_msg}{CLAN} has moved from whispers to action.")
            };
            breakMessage.SetTextVariable("CLAN", Hero.OneToOneConversationHero.Clan.Name);
            ShowIntrigueMessage(breakMessage);
        }
        else
        {
            _lastBreakResponse = reason;
        }
    }

    private bool CanDiscussForeignAlliance()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsForeignAllianceLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return behavior?.GetPlayerForeignAllianceWithClan(hero.Clan) != null
            || behavior?.GetPreferredPlayerConspiracyTargetForForeignAlly(hero.Clan) != null;
    }

    private void PrepareForeignAllianceIntro()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        Hero hero = Hero.OneToOneConversationHero;
        SecretAllianceCompact alliance = behavior?.GetPlayerForeignAllianceWithClan(hero?.Clan);
        Kingdom targetKingdom = alliance?.TargetKingdom ?? behavior?.GetPreferredPlayerConspiracyTargetForForeignAlly(hero?.Clan)?.Kingdom;

        _lastForeignIntroResponse = new TextObject("{=rf_si_foreign_intro_default}Speak, then. Which crown are you trying to loosen?");
        if (targetKingdom != null)
        {
            _lastForeignIntroResponse = new TextObject("{=rf_si_foreign_intro_target}Speak, then. I take it this concerns the fate of {KINGDOM}.");
            _lastForeignIntroResponse.SetTextVariable("KINGDOM", targetKingdom.Name);
        }
    }

    private bool SetForeignAllianceIntroLine()
    {
        MBTextManager.SetTextVariable("SI_FOREIGN_INTRO_TEXT", _lastForeignIntroResponse.ToString());
        return true;
    }

    private bool CanOfferForeignAlliance()
    {
        Hero hero = Hero.OneToOneConversationHero;
        if (!IsForeignAllianceLord(hero))
        {
            return false;
        }

        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return behavior?.GetPlayerForeignAllianceWithClan(hero.Clan) == null
            && behavior?.GetPreferredPlayerConspiracyTargetForForeignAlly(hero.Clan) != null
            && GetPlayerRelation(hero) >= StrategicIntrigueConstants.ForeignAllianceRelationThreshold;
    }

    private bool CanReviewForeignAlliance()
    {
        Hero hero = Hero.OneToOneConversationHero;
        return IsForeignAllianceLord(hero)
            && !_foreignAlliancesStartedThisConversation.Contains(hero.Clan)
            && global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>()?.GetPlayerForeignAllianceWithClan(hero.Clan) != null;
    }

    private void OfferForeignAlliance()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        Hero hero = Hero.OneToOneConversationHero;
        _lastForeignAllianceResponse = new TextObject("{=rf_si_foreign_alliance_default}Such bargains are not made without a credible prize.");

        if (behavior == null || hero?.Clan == null)
        {
            return;
        }

        if (behavior.TryCreateForeignAlliance(hero.Clan, out TextObject reason, out SecretAllianceCompact alliance))
        {
            _foreignAlliancesStartedThisConversation.Add(hero.Clan);
            _lastForeignAllianceResponse = new TextObject("{=rf_si_foreign_alliance_accept}Then we understand one another. Deliver {SETTLEMENT} when {KINGDOM} cracks, and my banners will not remain idle.");
            _lastForeignAllianceResponse.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}the promised fief"));
            _lastForeignAllianceResponse.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));

            TextObject allianceMessage = new TextObject("{=rf_si_foreign_alliance_msg}A foreign understanding has been reached with {CLAN} against {KINGDOM}.");
            allianceMessage.SetTextVariable("CLAN", hero.Clan.Name);
            allianceMessage.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            ShowIntrigueMessage(allianceMessage);
        }
        else
        {
            _lastForeignAllianceResponse = reason;
        }
    }

    private void PrepareForeignAllianceStatus()
    {
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        SecretAllianceCompact alliance = behavior?.GetPlayerForeignAllianceWithClan(Hero.OneToOneConversationHero?.Clan);

        _lastForeignAllianceStatusResponse = new TextObject("{=rf_si_foreign_alliance_status_none}There is no foreign bargain to review.");
        if (alliance == null)
        {
            return;
        }

        _lastForeignAllianceStatusResponse = alliance.IsSupportTriggered
            ? new TextObject("{=rf_si_foreign_alliance_status_triggered}Our understanding is now in motion. When the dust settles, {SETTLEMENT} is still owed to my house.")
            : new TextObject("{=rf_si_foreign_alliance_status_waiting}The understanding stands. When {KINGDOM} cracks, {SETTLEMENT} is the agreed price.");
        _lastForeignAllianceStatusResponse.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
        _lastForeignAllianceStatusResponse.SetTextVariable("SETTLEMENT", alliance.PromisedSettlement?.Name ?? new TextObject("{=rf_si_unknown_settlement}the promised fief"));
    }

    private bool SetForeignAllianceLine()
    {
        MBTextManager.SetTextVariable("SI_FOREIGN_ALLIANCE_TEXT", _lastForeignAllianceResponse.ToString());
        return true;
    }

    private bool SetForeignAllianceStatusLine()
    {
        MBTextManager.SetTextVariable("SI_FOREIGN_ALLIANCE_STATUS_TEXT", _lastForeignAllianceStatusResponse.ToString());
        return true;
    }

    private bool CanOpenCompanionEspionageMenu()
    {
        Hero hero = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return hero?.IsPlayerCompanion == true && behavior?.IsCompanionAvailableForEspionage(hero) == true;
    }

    private bool CanReviewCompanionEspionage()
    {
        Hero hero = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        return hero?.IsPlayerCompanion == true && behavior?.HasCompanionEspionageActivity(hero) == true;
    }

    private void OpenCompanionEspionageMenu()
    {
        Hero companion = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        if (companion?.IsPlayerCompanion != true || behavior == null)
        {
            return;
        }

        List<InquiryElement> options = new()
        {
            new InquiryElement("clan", new TextObject("{=rf_si_companion_target_clan}Investigate a clan").ToString(), null),
            new InquiryElement("kingdom", new TextObject("{=rf_si_companion_target_kingdom}Listen at a court").ToString(), null)
        };

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                new TextObject("{=rf_si_companion_menu_title}Intelligence Assignment").ToString(),
                new TextObject("{=rf_si_companion_menu_desc}Choose the kind of intelligence mission you want this companion to undertake.").ToString(),
                options,
                true,
                1,
                1,
                GameTexts.FindText("str_done", null).ToString(),
                GameTexts.FindText("str_cancel", null).ToString(),
                selected =>
                {
                    string choice = selected.FirstOrDefault()?.Identifier as string;
                    if (choice == "kingdom")
                    {
                        OpenKingdomEspionageTargetSelection(companion, behavior);
                    }
                    else
                    {
                        OpenClanEspionageTargetSelection(companion, behavior);
                    }
                },
                null,
                string.Empty,
                false),
            false,
            false);
    }

    private void OpenClanEspionageTargetSelection(Hero companion, StrategicIntrigueCampaignBehavior behavior)
    {
        List<InquiryElement> targets = behavior.GetAvailableEspionageClanTargets()
            .Select(clan => new InquiryElement(clan, $"{clan.Name} ({clan.Kingdom?.Name})", null))
            .ToList();

        if (targets.Count == 0)
        {
            ShowIntrigueMessage(new TextObject("{=rf_si_no_clan_targets}There are no valid clan targets for espionage right now."));
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                new TextObject("{=rf_si_clan_target_title}Choose Clan Target").ToString(),
                new TextObject("{=rf_si_clan_target_desc}Select the clan you want watched.").ToString(),
                targets,
                true,
                1,
                1,
                GameTexts.FindText("str_done", null).ToString(),
                GameTexts.FindText("str_cancel", null).ToString(),
                selected =>
                {
                    Clan targetClan = selected.FirstOrDefault()?.Identifier as Clan;
                    if (targetClan == null)
                    {
                        return;
                    }

                    if (behavior.TryStartClanInfiltration(companion, targetClan, out TextObject response))
                    {
                        ShowIntrigueMessage(response);
                    }
                    else
                    {
                        ShowIntrigueMessage(response);
                    }
                },
                null,
                string.Empty,
                false),
            false,
            false);
    }

    private void OpenKingdomEspionageTargetSelection(Hero companion, StrategicIntrigueCampaignBehavior behavior)
    {
        List<InquiryElement> targets = behavior.GetAvailableEspionageKingdomTargets()
            .Select(kingdom => new InquiryElement(kingdom, kingdom.Name.ToString(), null))
            .ToList();

        if (targets.Count == 0)
        {
            ShowIntrigueMessage(new TextObject("{=rf_si_no_kingdom_targets}There are no valid court targets for espionage right now."));
            return;
        }

        MBInformationManager.ShowMultiSelectionInquiry(
            new MultiSelectionInquiryData(
                new TextObject("{=rf_si_kingdom_target_title}Choose Court Target").ToString(),
                new TextObject("{=rf_si_kingdom_target_desc}Select the court you want quietly observed.").ToString(),
                targets,
                true,
                1,
                1,
                GameTexts.FindText("str_done", null).ToString(),
                GameTexts.FindText("str_cancel", null).ToString(),
                selected =>
                {
                    Kingdom targetKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (targetKingdom == null)
                    {
                        return;
                    }

                    if (behavior.TryStartCourtListening(companion, targetKingdom, out TextObject response))
                    {
                        ShowIntrigueMessage(response);
                    }
                    else
                    {
                        ShowIntrigueMessage(response);
                    }
                },
                null,
                string.Empty,
                false),
            false,
            false);
    }

    private void ReviewCompanionEspionage()
    {
        Hero companion = Hero.OneToOneConversationHero;
        StrategicIntrigueCampaignBehavior behavior = global::TaleWorlds.CampaignSystem.Campaign.Current?.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
        if (companion?.IsPlayerCompanion != true || behavior == null)
        {
            return;
        }

        TextObject report = behavior.GetCompanionEspionageStatus(companion);
        InformationManager.ShowInquiry(new InquiryData(
            new TextObject("{=rf_si_companion_report_title}Intelligence Report").ToString(),
            report.ToString(),
            true,
            false,
            new TextObject("{=rf_si_popup_ack}Understood").ToString(),
            string.Empty,
            null,
            null));
    }

    private static void ShowIntrigueMessage(TextObject message)
    {
        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
    }

    private static bool IsIntrigueLord(Hero hero)
    {
        return hero?.Clan != null
            && hero.IsLord
            && hero.Clan.Kingdom != null
            && hero.Clan != hero.Clan.Kingdom.RulingClan;
    }

    private static bool IsPoliticalLord(Hero hero)
    {
        return hero?.Clan != null
            && hero.IsLord
            && hero.Clan.Kingdom != null;
    }

    private static bool IsForeignAllianceLord(Hero hero)
    {
        return hero?.Clan != null
            && hero.IsLord
            && hero == hero.Clan.Leader
            && hero.Clan.Kingdom != null
            && hero.Clan == hero.Clan.Kingdom.RulingClan;
    }

    private static int GetPlayerRelation(Hero hero)
    {
        return hero == null || Hero.MainHero == null
            ? -100
            : hero.GetRelation(Hero.MainHero);
    }

    private TextObject BuildAlignmentResponse(Hero hero, ClanIntrigueState state, KingdomIntrigueState kingdomState)
    {
        int relation = GetPlayerRelation(hero);
        if (state == null)
        {
            return new TextObject("{=rf_si_alignment_no_state}I have nothing useful to tell you about the temper of this realm. (No intrigue state)");
        }

        if (relation <= -15)
        {
            bool accusation = state.Suspicion >= 55f || state.TrustToPlayer <= 8f;
            ApplyProbePenalty(hero, state, accusation ? 5 : 3, accusation ? 10f : 6f, accusation ? 9f : 5f);

            TextObject hostile = accusation
                ? new TextObject("{=rf_si_alignment_hostile_accuse}You ask like a conspirator, not an ally. Press this matter again and I may carry your name to the court myself.")
                : new TextObject("{=rf_si_alignment_hostile}That is bold talk from someone I do not trust. You will get no open complaint from me.");
            return hostile;
        }

        if (relation < StrategicIntrigueConstants.StabilityDiscussionRelationThreshold)
        {
            ApplyProbePenalty(hero, state, 1, 3f, 2f);
            TextObject guarded = state.Dissidence >= 45f
                ? new TextObject("{=rf_si_alignment_guarded_unhappy}There is strain in the realm, but you have not earned the right to hear it plainly from me.")
                : new TextObject("{=rf_si_alignment_guarded_calm}You ask a delicate question for one still outside my confidence. I will say only that the court listens closely.");
            return guarded;
        }

        if (relation < StrategicIntrigueConstants.RumorCampaignRelationThreshold)
        {
            TextObject cautious = state.Dissidence >= 70f
                ? new TextObject("{=rf_si_alignment_cautious_high}There are fractures enough, but I would be a fool to name them carelessly.")
                : state.Dissidence >= 45f
                    ? new TextObject("{=rf_si_alignment_cautious_mid}Some choices at court have not gone unnoticed, though I will go no further than that.")
                    : new TextObject("{=rf_si_alignment_cautious_low}I have no public grievance to air, and no wish to invite one.");
            return cautious;
        }

        TextObject detailed = new(DescribeDissidence(state));
        return detailed;
    }

    private static string DescribeDissidence(ClanIntrigueState state)
    {
        if (state.Dissidence >= 85f)
        {
            return "The realm is brittle. Many would welcome a decisive push.";
        }

        if (state.Dissidence >= 70f)
        {
            return "Let us say that loyalty is not what it once was.";
        }

        if (state.Dissidence >= 45f)
        {
            return "Some choices at court have not gone unnoticed.";
        }

        return "I have no reason to complain.";
    }

    private void ApplyProbePenalty(Hero hero, ClanIntrigueState state, int relationLossMagnitude, float suspicionGain, float trustLoss)
    {
        if (hero == null || Hero.MainHero == null)
        {
            return;
        }

        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, hero, -relationLossMagnitude, false);
        state.Suspicion += suspicionGain;
        state.TrustToPlayer -= trustLoss;
        state.ClampValues();

        if (relationLossMagnitude >= 5)
        {
            ShowIntrigueMessage(new TextObject("{=rf_si_alignment_penalty_heavy}Your probing has been noticed. You leave the exchange under a darker cloud than before."));
        }
    }
}
