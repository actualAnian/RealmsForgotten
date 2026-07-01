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

        int playerStrength = playerSide.GetTotalHealthyTroopCountOfSide();
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
        int totalOffers = 0;
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
                _promotionMerits.Remove(troopId);
                continue;
            }

            AwardPromotionMerit(troopId, kills);
            totalOffers += QueuePromotionOffers(troop, currentCount);

            if (totalOffers > 0 && !PromotedSettings.Current.AllowMultiplePromotions)
            {
                PromotedDebug.Message($"Promotion ready | troop={troop.StringId} offers={totalOffers}");
                return;
            }
        }

        PromotedDebug.Message($"Promotion scan finished | offers={totalOffers}");
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
