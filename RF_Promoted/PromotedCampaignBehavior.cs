using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_Promoted;

public sealed class PromotedCampaignBehavior : CampaignBehaviorBase
{
    private readonly Dictionary<string, int> _battleKills = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _preBattleRoster = new(StringComparer.Ordinal);
    private readonly Queue<PromotionOffer> _pendingOffers = new();

    private Dictionary<string, int> _promotionMerits = new(StringComparer.Ordinal);
    private List<string> _promotedHeroIds = new();
    private bool _battleEligible;
    private bool _offerOpen;
    private bool _offersReady;
    private float _battleRatio;

    public override void RegisterEvents()
    {
        CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("RFPromoted_Merits", ref _promotionMerits);
        dataStore.SyncData("RFPromoted_HeroIds", ref _promotedHeroIds);
        _promotionMerits ??= new Dictionary<string, int>(StringComparer.Ordinal);
        _promotedHeroIds ??= new List<string>();
    }

    public void RegisterBattleKill(string troopId)
    {
        if (!_battleEligible || string.IsNullOrWhiteSpace(troopId) || !_preBattleRoster.ContainsKey(troopId))
        {
            return;
        }

        _battleKills.TryGetValue(troopId, out int current);
        _battleKills[troopId] = current + 1;
    }

    public int GetActivePromotedCompanionCount()
    {
        return GetPromotedHeroes()
            .Count(hero => hero.IsPlayerCompanion && hero.IsAlive && hero.Clan == Clan.PlayerClan);
    }

    private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
    {
        ResetBattleState();

        if (mapEvent == null
            || !mapEvent.IsPlayerMapEvent
            || MobileParty.MainParty?.MemberRoster == null
            || MobileParty.MainParty.MemberRoster.TotalHealthyCount <= 0)
        {
            return;
        }

        MapEventSide? playerSide = GetPlayerSide(mapEvent);
        MapEventSide? enemySide = GetEnemySide(mapEvent);
        if (playerSide == null || enemySide == null)
        {
            return;
        }

        // Fair-fight ratio counts the PLAYER'S OWN PARTY against the whole enemy
        // side. Counting the full allied side made kingdom-war battles (ally
        // lords piling in) permanently ineligible — merit could never accrue.
        int playerStrength = MobileParty.MainParty.MemberRoster.TotalHealthyCount;
        int enemyStrength = Math.Max(1, enemySide.GetTotalHealthyTroopCountOfSide());
        _battleRatio = playerStrength / (float)enemyStrength;

        foreach (TroopRosterElement troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
        {
            if (troop.Character == null || troop.Character.IsHero || troop.Number <= 0)
            {
                continue;
            }

            _preBattleRoster[troop.Character.StringId] = troop.Number;
        }

        _battleEligible = _preBattleRoster.Count > 0 && _battleRatio < PromotedSettings.Current.RatioThreshold;
        PromotedDebug.Message($"Battle start | ratio={_battleRatio:0.00} eligible={_battleEligible} tracked={_preBattleRoster.Count}");
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        try
        {
            if (!_battleEligible
                || mapEvent == null
                || !mapEvent.IsPlayerMapEvent
                || mapEvent.WinningSide != mapEvent.PlayerSide)
            {
                return;
            }

            BuildPromotionOffers();
            _offersReady = _pendingOffers.Count > 0;
        }
        finally
        {
            ResetBattleState();
        }
    }

    private void OnTick(float dt)
    {
        if (!_offersReady || _offerOpen || _pendingOffers.Count == 0)
        {
            return;
        }

        if (PlayerEncounter.Current != null || MapEvent.PlayerMapEvent != null)
        {
            return;
        }

        _offersReady = false;
        ShowNextOffer();
    }

    private void BuildPromotionOffers()
    {
        // A stack that upgraded tiers carried its merit under the OLD troop id,
        // where it sat orphaned forever — the soldiers who killed the most (and
        // therefore levelled fastest) were exactly the ones who never got
        // promoted. Move orphaned merit down the upgrade tree first.
        ConsolidateOrphanedMerits();

        int totalOffers = 0;
        bool stopQueuing = false;
        foreach (KeyValuePair<string, int> entry in _battleKills.OrderByDescending(item => item.Value))
        {
            string troopId = entry.Key;
            int kills = entry.Value;

            CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);
            if (troop == null || troop.IsHero || troop.Occupation == Occupation.PrisonGuard)
            {
                continue;
            }

            int currentCount = MobileParty.MainParty?.MemberRoster?.GetTroopCount(troop) ?? 0;
            if (currentCount <= 0)
            {
                // Type no longer in the party: try to pass its merit (including
                // this battle's kills) to an upgraded descendant before dropping.
                CharacterObject? heir = FindUpgradedDescendantInParty(troop);
                if (heir != null)
                {
                    AwardPromotionMerit(troopId, kills);
                    TransferMerit(troopId, heir.StringId);
                }
                else
                {
                    _promotionMerits.Remove(troopId);
                }
                continue;
            }

            // Always bank this troop's merit (_battleKills is cleared right after
            // this scan). The old early return skipped AwardPromotionMerit for the
            // remaining troops, permanently discarding their earned merit.
            AwardPromotionMerit(troopId, kills);

            if (!stopQueuing)
            {
                totalOffers += QueuePromotionOffers(troop, currentCount);
                if (totalOffers > 0 && !PromotedSettings.Current.AllowMultiplePromotions)
                {
                    // Stop queuing further offers, but keep looping so remaining
                    // troops still get their merit banked for the next cycle.
                    PromotedDebug.Message($"Promotion ready | troop={troop.StringId} offers={totalOffers}");
                    stopQueuing = true;
                }
            }
        }

        PromotedDebug.Message($"Promotion scan finished | offers={totalOffers}");
    }

    /// <summary>
    /// Moves merit stored under troop types no longer present in the party onto
    /// their upgraded descendants that ARE present, so tiering up never loses
    /// earned merit. Runs before each promotion scan.
    /// </summary>
    private void ConsolidateOrphanedMerits()
    {
        TroopRoster? roster = MobileParty.MainParty?.MemberRoster;
        if (roster == null || _promotionMerits.Count == 0)
        {
            return;
        }

        foreach (string troopId in _promotionMerits.Keys.ToList())
        {
            CharacterObject? troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);
            if (troop == null)
            {
                _promotionMerits.Remove(troopId);
                continue;
            }

            if (roster.GetTroopCount(troop) > 0)
            {
                continue;
            }

            CharacterObject? heir = FindUpgradedDescendantInParty(troop);
            if (heir != null)
            {
                TransferMerit(troopId, heir.StringId);
            }
            // No descendant in the party either: keep the merit parked — the
            // player may still hold these troops in a garrison and re-add them.
        }
    }

    /// <summary>
    /// Breadth-first walk down a troop's upgrade tree (max 3 tiers) for the
    /// first descendant type currently in the player's party.
    /// </summary>
    private static CharacterObject? FindUpgradedDescendantInParty(CharacterObject troop)
    {
        TroopRoster? roster = MobileParty.MainParty?.MemberRoster;
        if (roster == null)
        {
            return null;
        }

        Queue<(CharacterObject Node, int Depth)> queue = new();
        queue.Enqueue((troop, 0));
        while (queue.Count > 0)
        {
            (CharacterObject node, int depth) = queue.Dequeue();
            if (depth >= 3 || node.UpgradeTargets == null)
            {
                continue;
            }

            foreach (CharacterObject target in node.UpgradeTargets)
            {
                if (target == null || target.IsHero)
                {
                    continue;
                }
                if (roster.GetTroopCount(target) > 0)
                {
                    return target;
                }
                queue.Enqueue((target, depth + 1));
            }
        }
        return null;
    }

    private void TransferMerit(string fromTroopId, string toTroopId)
    {
        int merit = GetPromotionMerit(fromTroopId);
        _promotionMerits.Remove(fromTroopId);
        if (merit > 0)
        {
            _promotionMerits[toTroopId] = GetPromotionMerit(toTroopId) + merit;
            PromotedDebug.Message($"Merit transferred | from={fromTroopId} to={toTroopId} merit={merit}");
        }
    }

    private void AwardPromotionMerit(string troopId, int kills)
    {
        int threshold = Math.Max(1, PromotedSettings.Current.MeritThreshold);
        int currentMerit = GetPromotionMerit(troopId);

        if (PromotedSettings.Current.AlwaysPromote)
        {
            _promotionMerits[troopId] = Math.Max(currentMerit, threshold);
            return;
        }

        int meritGain = Math.Max(0, kills) * Math.Max(1, PromotedSettings.Current.MeritPerKill);
        if (meritGain <= 0)
        {
            return;
        }

        _promotionMerits[troopId] = currentMerit + meritGain;
        PromotedDebug.Message($"Merit gained | troop={troopId} kills={kills} merit={_promotionMerits[troopId]}");
    }

    private int QueuePromotionOffers(CharacterObject troop, int currentCount)
    {
        int threshold = Math.Max(1, PromotedSettings.Current.MeritThreshold);
        int availableMerit = GetPromotionMerit(troop.StringId);
        int maxOffers = Math.Min(currentCount, availableMerit / threshold);
        if (maxOffers <= 0)
        {
            return 0;
        }

        int offersToQueue = PromotedSettings.Current.AllowMultiplePromotions ? maxOffers : 1;
        for (int index = 0; index < offersToQueue; index++)
        {
            _pendingOffers.Enqueue(new PromotionOffer(troop));
        }

        _promotionMerits[troop.StringId] = Math.Max(0, availableMerit - (offersToQueue * threshold));
        PromotedDebug.Message($"Promotion queued | troop={troop.StringId} queued={offersToQueue} meritLeft={_promotionMerits[troop.StringId]}");
        return offersToQueue;
    }

    private int GetPromotionMerit(string troopId)
    {
        return _promotionMerits.TryGetValue(troopId, out int merit) ? merit : 0;
    }

    private void ShowNextOffer()
    {
        if (_offerOpen || _pendingOffers.Count == 0)
        {
            return;
        }

        PromotionOffer offer = _pendingOffers.Dequeue();
        _offerOpen = true;
        TextObject body = GameTexts.FindText("rf_promoted_offer_body");
        body.SetTextVariable("TROOP_NAME", offer.Template.Name);

        InformationManager.ShowInquiry(new InquiryData(
            GameTexts.FindText("rf_promoted_offer_title").ToString(),
            body.ToString(),
            true,
            true,
            GameTexts.FindText("rf_promoted_offer_accept").ToString(),
            GameTexts.FindText("rf_promoted_offer_decline").ToString(),
            () => ShowPromotionTypeInquiry(offer),
            CloseOfferAndContinue),
            true,
            false);
    }

    private void ShowPromotionTypeInquiry(PromotionOffer offer)
    {
        bool canAddCompanion = HasCompanionRoom();

        if (!canAddCompanion)
        {
            InformationManager.ShowInquiry(new InquiryData(
                GameTexts.FindText("rf_promoted_offer_title").ToString(),
                GameTexts.FindText("rf_promoted_family_only_body").ToString(),
                true,
                true,
                GameTexts.FindText("rf_promoted_family_option").ToString(),
                GameTexts.FindText("rf_promoted_cancel").ToString(),
                () => CompletePromotion(offer, PromotionType.Family),
                CloseOfferAndContinue),
                true,
                false);
            return;
        }

        InformationManager.ShowInquiry(new InquiryData(
            GameTexts.FindText("rf_promoted_offer_title").ToString(),
            GameTexts.FindText("rf_promoted_choose_path_body").ToString(),
            true,
            true,
            GameTexts.FindText("rf_promoted_companion_option").ToString(),
            GameTexts.FindText("rf_promoted_family_option").ToString(),
            () => CompletePromotion(offer, PromotionType.Companion),
            () => CompletePromotion(offer, PromotionType.Family)),
            true,
            false);
    }

    private void CompletePromotion(PromotionOffer offer, PromotionType promotionType)
    {
        Hero? hero = CreatePromotedHero(offer.Template, promotionType);
        if (hero == null)
        {
            CloseOfferAndContinue();
            return;
        }

        RenameHero(hero, offer.Template.Name?.ToString() ?? "Promoted Soldier");
    }

    private Hero? CreatePromotedHero(CharacterObject template, PromotionType promotionType)
    {
        if (MobileParty.MainParty?.MemberRoster == null)
        {
            PromotedDebug.Message("Promotion failed | main party roster missing");
            return null;
        }

        int troopCount = MobileParty.MainParty.MemberRoster.GetTroopCount(template);
        if (troopCount <= 0)
        {
            PromotedDebug.Message($"Promotion failed | troop missing={template.StringId}");
            return null;
        }

        Settlement? creationSettlement = GetCreationSettlement(template);
        Hero hero = promotionType == PromotionType.Companion
            ? HeroCreator.CreateSpecialHero(template, creationSettlement, null, null, MBRandom.RandomInt(23, 39))
            : HeroCreator.CreateSpecialHero(template, creationSettlement, Clan.PlayerClan, null, MBRandom.RandomInt(23, 39));

        string baseName = template.Name?.ToString() ?? "Promoted";
        hero.SetName(new TextObject(baseName), new TextObject(baseName));
        hero.ChangeState(Hero.CharacterStates.Active);
        hero.SetHasMet();
        hero.ChangeHeroGold(250);

        foreach (SkillObject skill in MBObjectManager.Instance.GetObjectTypeList<SkillObject>())
        {
            hero.SetSkillValue(skill, template.GetSkillValue(skill));
        }

        hero.BattleEquipment.FillFrom(template.FirstBattleEquipment.Clone(false));
        hero.CivilianEquipment.FillFrom(template.FirstCivilianEquipment.Clone(false));

        if (promotionType == PromotionType.Companion)
        {
            hero.SetNewOccupation(Occupation.Wanderer);
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
        }
        else
        {
            hero.Clan = Clan.PlayerClan;
            hero.SetNewOccupation(Occupation.Lord);
            if (!Clan.PlayerClan.Heroes.Contains(hero))
            {
                Clan.PlayerClan.Heroes.Add(hero);
            }
        }

        AddHeroToPartyAction.Apply(hero, MobileParty.MainParty, false);
        MobileParty.MainParty.MemberRoster.AddToCounts(template, -1, false, 0, 0, true, -1);

        if (!_promotedHeroIds.Contains(hero.StringId))
        {
            _promotedHeroIds.Add(hero.StringId);
        }

        PromotedDebug.Message($"Promotion complete | hero={hero.StringId} type={promotionType} template={template.StringId}");
        return hero;
    }

    private void RenameHero(Hero hero, string fallbackName)
    {
        InformationManager.ShowTextInquiry(new TextInquiryData(
            GameTexts.FindText("rf_promoted_rename_title").ToString(),
            GameTexts.FindText("rf_promoted_rename_body").ToString(),
            true,
            true,
            GameTexts.FindText("rf_promoted_confirm").ToString(),
            GameTexts.FindText("rf_promoted_keep_default").ToString(),
            value =>
            {
                string chosenName = string.IsNullOrWhiteSpace(value) ? fallbackName : value.Trim();
                hero.SetName(new TextObject(chosenName), new TextObject(chosenName));
                CloseOfferAndContinue();
            },
            CloseOfferAndContinue,
            false,
            null,
            fallbackName,
            string.Empty),
            false,
            false);
    }

    private void CloseOfferAndContinue()
    {
        _offerOpen = false;
        ShowNextOffer();
    }

    private bool HasCompanionRoom()
    {
        if (Clan.PlayerClan == null || Campaign.Current == null)
        {
            return false;
        }

        int currentCompanions = Clan.PlayerClan.Heroes.Count(hero =>
            hero != null
            && hero.IsAlive
            && hero.IsPlayerCompanion
            && hero.Clan == Clan.PlayerClan);

        int limit = Campaign.Current.Models.ClanTierModel.GetCompanionLimit(Clan.PlayerClan);
        return currentCompanions < limit;
    }

    private IEnumerable<Hero> GetPromotedHeroes()
    {
        return _promotedHeroIds
            .Select(id => Hero.AllAliveHeroes.FirstOrDefault(hero => hero.StringId == id))
            .Where(hero => hero != null)!;
    }

    private static Settlement? GetCreationSettlement(CharacterObject template)
    {
        Settlement? currentSettlement = Settlement.CurrentSettlement ?? Hero.MainHero.CurrentSettlement;
        if (currentSettlement != null)
        {
            return currentSettlement;
        }

        Settlement? sameCultureTown = Settlement.All
            .FirstOrDefault(settlement => settlement.IsTown && settlement.Culture == template.Culture);
        if (sameCultureTown != null)
        {
            return sameCultureTown;
        }

        return Settlement.All.FirstOrDefault(settlement => settlement.IsTown);
    }

    private static MapEventSide? GetPlayerSide(MapEvent mapEvent)
    {
        if (mapEvent == null)
        {
            return null;
        }

        return mapEvent.PlayerSide == mapEvent.AttackerSide.MissionSide
            ? mapEvent.AttackerSide
            : mapEvent.DefenderSide;
    }

    private static MapEventSide? GetEnemySide(MapEvent mapEvent)
    {
        MapEventSide? playerSide = GetPlayerSide(mapEvent);
        if (playerSide == null)
        {
            return null;
        }

        return ReferenceEquals(playerSide, mapEvent.AttackerSide)
            ? mapEvent.DefenderSide
            : mapEvent.AttackerSide;
    }

    private void ResetBattleState()
    {
        _battleKills.Clear();
        _preBattleRoster.Clear();
        _battleEligible = false;
        _battleRatio = 0f;
    }

    private sealed class PromotionOffer
    {
        public PromotionOffer(CharacterObject template)
        {
            Template = template;
        }

        public CharacterObject Template { get; }
    }

    private enum PromotionType
    {
        Companion,
        Family
    }
}
