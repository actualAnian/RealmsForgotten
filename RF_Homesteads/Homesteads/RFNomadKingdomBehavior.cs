using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Homesteads;

/// <summary>
/// RF nomad-horde feature: lets the player PROCLAIM A KINGDOM from their
/// homestead, without owning any conventional town/castle. The homestead acts
/// as the kingdom's seat ("capital"). Vanilla already never auto-discontinues
/// the PLAYER's fiefless kingdom (CanKingdomBeDiscontinued excludes it); the
/// remaining threat is RealmsForgotten's CapitulationSystemBehavior, which we
/// exempt via its static hook (set below by reflection, no hard dependency).
/// </summary>
public class RFNomadKingdomBehavior : CampaignBehaviorBase
{
    private const int SackCooldownDays = 30;
    private const float SackGoldPerProsperity = 2.5f;
    private const int SackGoldMin = 2000;
    private const int SackGoldMax = 40000;

    // Last sack day per settlement StringId (prevents capture->sack->recapture
    // gold farming). Dictionary<string,int> container is already defined in the
    // Homesteads CustomSaveDefiner.
    private Dictionary<string, int> _sackDayBySettlementId = new Dictionary<string, int>();

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_rfNomadSackDays", ref _sackDayBySettlementId);
        _sackDayBySettlementId ??= new Dictionary<string, int>();
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        InstallCapitulationExemption();
        AddProclaimKingdomMenuOption(starter);
    }

    // ------------------------------------------------------------------
    // Nomad kingdom definition (stateless, evaluated on demand)
    // ------------------------------------------------------------------

    public static bool PlayerOwnsAnyHomestead()
    {
        return HomesteadBehavior.Instance != null
            && HomesteadBehavior.Instance.HomesteadMobileParties.Count > 0;
    }

    public static bool IsNomadKingdom(Kingdom kingdom)
    {
        return kingdom != null
            && !kingdom.IsEliminated
            && kingdom.RulingClan == Clan.PlayerClan
            && kingdom.Fiefs.Count == 0
            && PlayerOwnsAnyHomestead();
    }

    // ------------------------------------------------------------------
    // Capitulation exemption (reflection — RealmsForgotten.dll loads first,
    // but keep RF_Homesteads free of a compile-time reference to it)
    // ------------------------------------------------------------------

    private static void InstallCapitulationExemption()
    {
        try
        {
            Type type = Type.GetType("RealmsForgotten.CapitulationSystemBehavior, RealmsForgotten", throwOnError: false)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("RealmsForgotten.CapitulationSystemBehavior"))
                    .FirstOrDefault(t => t != null);
            FieldInfo field = type?.GetField("IsKingdomExemptFromCapitulation",
                BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                field.SetValue(null, new Func<Kingdom, bool>(IsNomadKingdom));
                TraceLogger.Write("RFNomadKingdom", "Capitulation exemption hook installed.");
            }
            else
            {
                TraceLogger.Write("RFNomadKingdom", "Capitulation hook NOT found (RealmsForgotten main missing?) — nomad kingdom may receive capitulation offers.");
            }
        }
        catch (Exception ex)
        {
            TraceLogger.Write("RFNomadKingdom", "InstallCapitulationExemption failed: " + ex.Message);
        }
    }

    // ------------------------------------------------------------------
    // "Proclaim kingdom" homestead menu option
    // ------------------------------------------------------------------

    private void AddProclaimKingdomMenuOption(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_proclaim_kingdom",
            "{=rf_nomad_proclaim_kingdom}Proclaim your nomad kingdom",
            ProclaimKingdomCondition, ProclaimKingdomConsequence, isLeave: false, index: 1);
    }

    private bool ProclaimKingdomCondition(MenuCallbackArgs args)
    {
        args.optionLeaveType = GameMenuOption.LeaveType.Manage;

        // Only for independent (kingdomless) player clans.
        if (Clan.PlayerClan.Kingdom != null)
        {
            return false;
        }

        var model = Campaign.Current.Models.KingdomCreationModel;
        int minTier = model.MinimumClanTierToCreateKingdom;
        int minTroops = model.MinimumTroopCountToCreateKingdom;

        bool tierOk = Clan.PlayerClan.Tier >= minTier;
        // Nomads have no garrisons — count the clan's war parties only.
        int troops = Clan.PlayerClan.WarPartyComponents.Sum(
            (WarPartyComponent w) => w.MobileParty?.MemberRoster?.TotalHealthyCount ?? 0);
        bool troopsOk = troops >= minTroops;

        if (!tierOk || !troopsOk)
        {
            args.IsEnabled = false;
            TextObject reason = new TextObject(
                "{=rf_nomad_proclaim_reqs}Your horde is not ready: clan tier {TIER}/{MIN_TIER}, warriors {TROOPS}/{MIN_TROOPS}.");
            reason.SetTextVariable("TIER", Clan.PlayerClan.Tier);
            reason.SetTextVariable("MIN_TIER", minTier);
            reason.SetTextVariable("TROOPS", troops);
            reason.SetTextVariable("MIN_TROOPS", minTroops);
            args.Tooltip = reason;
        }

        return true;
    }

    private void ProclaimKingdomConsequence(MenuCallbackArgs args)
    {
        Homestead homestead = HomesteadBehavior.Instance?.CurrentHomestead;
        TextObject title = new TextObject("{=rf_nomad_proclaim_title}Proclaim Kingdom");
        TextObject body = new TextObject(
            "{=rf_nomad_proclaim_body}Your homestead will become the seat of your new kingdom. You will remain a nomad lord — no city or castle is required. Choose the name of your kingdom:");

        InformationManager.ShowTextInquiry(new TextInquiryData(
            title.ToString(),
            body.ToString(),
            true, true,
            GameTexts.FindText("str_done").ToString(),
            GameTexts.FindText("str_cancel").ToString(),
            (string chosenName) => FinalizeProclamation(chosenName, homestead),
            null,
            shouldInputBeObfuscated: false,
            textCondition: (string input) => string.IsNullOrWhiteSpace(input)
                ? new Tuple<bool, string>(false, new TextObject("{=rf_nomad_name_empty}Enter a kingdom name.").ToString())
                : new Tuple<bool, string>(true, string.Empty),
            defaultInputText: Clan.PlayerClan.Name.ToString()));
    }

    private static void FinalizeProclamation(string chosenName, Homestead homestead)
    {
        if (string.IsNullOrWhiteSpace(chosenName) || Clan.PlayerClan.Kingdom != null)
        {
            return;
        }

        try
        {
            TextObject kingdomName = new TextObject(chosenName.Trim());
            Campaign.Current.KingdomManager.CreateKingdom(
                kingdomName, kingdomName, Clan.PlayerClan.Culture, Clan.PlayerClan);

            TextObject notice = new TextObject(
                "{=rf_nomad_proclaimed}{KINGDOM} has been proclaimed! {HOMESTEAD} is now the seat of your nomad kingdom.");
            notice.SetTextVariable("KINGDOM", kingdomName);
            notice.SetTextVariable("HOMESTEAD", homestead?.Name ?? new TextObject("{=rf_nomad_your_homestead}Your homestead"));
            InformationManager.DisplayMessage(new InformationMessage(notice.ToString(), Colors.Green));
            HomesteadChronicle.Record($"{Hero.MainHero.Name} proclaimed the nomad kingdom of {kingdomName} from the homestead of {homestead?.Name}.");

            if (Campaign.Current.CurrentMenuContext != null)
            {
                GameMenu.SwitchToMenu("homestead_menu_main");
            }
        }
        catch (Exception ex)
        {
            TraceLogger.Write("RFNomadKingdom", "FinalizeProclamation failed: " + ex);
            InformationManager.DisplayMessage(new InformationMessage(
                "Failed to proclaim kingdom: " + ex.Message, Colors.Red));
        }
    }

    // ------------------------------------------------------------------
    // Nomad conquest: a horde does not garrison walls. When the nomad
    // kingdom acquires its FIRST fief, offer to sack it and hand it back
    // (loot payout, cooldown-gated) instead of settling down.
    // ------------------------------------------------------------------

    private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner,
        Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        try
        {
            if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
            {
                return;
            }
            if (newOwner == null || newOwner.Clan != Clan.PlayerClan)
            {
                return;
            }
            Kingdom kingdom = Clan.PlayerClan.Kingdom;
            if (kingdom == null || kingdom.RulingClan != Clan.PlayerClan || !PlayerOwnsAnyHomestead())
            {
                return;
            }
            // Only when this acquisition is the kingdom's ONLY fief — i.e. the
            // kingdom was nomad until this very moment.
            if (kingdom.Fiefs.Count != 1 || kingdom.Fiefs[0].Settlement != settlement)
            {
                return;
            }
            // Settlements chartered FROM a homestead are an intentional way of
            // settling down — never offer to sack those.
            if (settlement.StringId != null && settlement.StringId.StartsWith("hsr_settlement_", StringComparison.Ordinal))
            {
                return;
            }
            Hero recipient = ResolveReturnRecipient(oldOwner);
            if (recipient == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=rf_nomad_no_recipient}There is no one left to hand {SETTLEMENT} back to — it is yours now.")
                        .SetTextVariable("SETTLEMENT", settlement.Name).ToString(), Colors.Yellow));
                return;
            }
            ShowNomadConquestInquiry(settlement, recipient);
        }
        catch (Exception ex)
        {
            TraceLogger.Write("RFNomadKingdom", "OnSettlementOwnerChanged failed: " + ex.Message);
        }
    }

    private static Hero ResolveReturnRecipient(Hero oldOwner)
    {
        if (oldOwner != null && oldOwner.IsAlive && !oldOwner.IsChild
            && oldOwner.Clan != null && !oldOwner.Clan.IsEliminated && oldOwner.Clan != Clan.PlayerClan)
        {
            return oldOwner;
        }

        Clan oldClan = oldOwner?.Clan;
        if (oldClan != null && !oldClan.IsEliminated && oldClan != Clan.PlayerClan
            && oldClan.Leader != null && oldClan.Leader.IsAlive && !oldClan.Leader.IsChild)
        {
            return oldClan.Leader;
        }

        Kingdom oldKingdom = oldClan?.Kingdom;
        if (oldKingdom != null && !oldKingdom.IsEliminated && oldKingdom != Clan.PlayerClan.Kingdom)
        {
            Clan candidate = oldKingdom.Clans.FirstOrDefault(c =>
                c != null && !c.IsEliminated && !c.IsUnderMercenaryService
                && c.Leader != null && c.Leader.IsAlive && !c.Leader.IsChild);
            if (candidate != null)
            {
                return candidate.Leader;
            }
        }

        return null;
    }

    private void ShowNomadConquestInquiry(Settlement settlement, Hero recipient)
    {
        bool onCooldown = IsSackOnCooldown(settlement);
        TextObject title = new TextObject("{=rf_nomad_conquest_title}The Horde Takes {SETTLEMENT}")
            .SetTextVariable("SETTLEMENT", settlement.Name);
        TextObject body = new TextObject(onCooldown
            ? "{=rf_nomad_conquest_body_cd}A nomad horde does not garrison walls. {SETTLEMENT} was stripped bare not long ago — there is nothing left to sack, but you can still hand it back to {RECIPIENT} and ride on. Or claim it, and settle down."
            : "{=rf_nomad_conquest_body}A nomad horde does not garrison walls. Sack {SETTLEMENT}, hand the ruin back to {RECIPIENT} and ride on — or claim it, and settle down.");
        body.SetTextVariable("SETTLEMENT", settlement.Name);
        body.SetTextVariable("RECIPIENT", recipient.Name);

        InformationManager.ShowInquiry(new InquiryData(
            title.ToString(),
            body.ToString(),
            true, true,
            new TextObject("{=rf_nomad_sack_btn}Sack it and ride on").ToString(),
            new TextObject("{=rf_nomad_keep_btn}Keep it (settle down)").ToString(),
            () => SackAndReturn(settlement, recipient),
            () => InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=rf_nomad_kept}{SETTLEMENT} is yours. Your kingdom is no longer nomad while it holds fiefs.")
                    .SetTextVariable("SETTLEMENT", settlement.Name).ToString(), Colors.Yellow))),
            pauseGameActiveState: true);
    }

    private bool IsSackOnCooldown(Settlement settlement)
    {
        return settlement?.StringId != null
            && _sackDayBySettlementId.TryGetValue(settlement.StringId, out int lastDay)
            && (int)CampaignTime.Now.ToDays - lastDay < SackCooldownDays;
    }

    private void SackAndReturn(Settlement settlement, Hero recipient)
    {
        try
        {
            int gold = 0;
            if (!IsSackOnCooldown(settlement) && settlement.Town != null)
            {
                gold = (int)MathF.Clamp(settlement.Town.Prosperity * SackGoldPerProsperity, SackGoldMin, SackGoldMax);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold);
                settlement.Town.Prosperity = MathF.Max(0f, settlement.Town.Prosperity * 0.85f);
                settlement.Town.Loyalty = MathF.Max(0f, settlement.Town.Loyalty - 20f);
                if (settlement.StringId != null)
                {
                    _sackDayBySettlementId[settlement.StringId] = (int)CampaignTime.Now.ToDays;
                }
            }

            ChangeOwnerOfSettlementAction.ApplyByKingDecision(recipient, settlement);

            TextObject notice = gold > 0
                ? new TextObject("{=rf_nomad_sacked}{SETTLEMENT} was sacked for {GOLD} denars and handed back to {RECIPIENT}. The horde rides on.")
                : new TextObject("{=rf_nomad_returned}{SETTLEMENT} was handed back to {RECIPIENT}. The horde rides on.");
            notice.SetTextVariable("SETTLEMENT", settlement.Name);
            notice.SetTextVariable("GOLD", gold);
            notice.SetTextVariable("RECIPIENT", recipient.Name);
            InformationManager.DisplayMessage(new InformationMessage(notice.ToString(), Colors.Green));
        }
        catch (Exception ex)
        {
            TraceLogger.Write("RFNomadKingdom", "SackAndReturn failed: " + ex);
            InformationManager.DisplayMessage(new InformationMessage(
                "Failed to hand the settlement back: " + ex.Message, Colors.Red));
        }
    }
}
