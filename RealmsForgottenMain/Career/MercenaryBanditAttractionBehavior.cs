using RealmsForgotten.AiMade.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Career
{
    public class MercenaryBanditAttractionBehavior : CampaignBehaviorBase
    {
        private const string BanditRenownPerkId = "MercenaryLord2_3";
        private const float BaseSearchRadius = 5f;
        private const float MaxSearchRadius = 12f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnDailyTick()
        {
            try
            {
                if (!PlayerCareerExtension.HasCareerChoice(BanditRenownPerkId))
                    return;

                MobileParty mainParty = MobileParty.MainParty;
                if (mainParty == null || mainParty.MapEvent != null || mainParty.CurrentSettlement != null)
                    return;

                int freeSlots = PartyBase.MainParty.PartySizeLimit - mainParty.MemberRoster.TotalManCount;
                if (freeSlots <= 0)
                    return;

                float renown = Clan.PlayerClan?.Renown ?? 0f;
                float attractionChance = GetAttractionChance(renown);
                if (MBRandom.RandomFloat > attractionChance)
                    return;

                float searchRadius = GetSearchRadius(renown);
                MobileParty candidate = FindCandidateBanditParty(mainParty, searchRadius);
                if (candidate == null)
                    return;

                CharacterObject troop = ChooseRecruitableBandit(candidate);
                if (troop == null)
                    return;

                int recruitedCount = GetRecruitCount(candidate, troop, freeSlots, renown);
                if (recruitedCount <= 0)
                    return;

                candidate.MemberRoster.AddToCounts(troop, -recruitedCount, false, 0, 0, true, -1);
                mainParty.MemberRoster.AddToCounts(troop, recruitedCount, false, 0, 0, true, -1);

                BanditConversionManager.OnBanditConverted(Hero.MainHero, recruitedCount);

                TextObject message = new TextObject("{=rf_bandit_renown_joiners}{COUNT} {TROOP} from {PARTY} have deserted to your banner, drawn by your mercenary renown.");
                message.SetTextVariable("COUNT", recruitedCount);
                message.SetTextVariable("TROOP", troop.Name);
                message.SetTextVariable("PARTY", candidate.Name);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Mercenary Bandit Attraction] " + ex.Message));
            }
        }

        private static float GetAttractionChance(float renown)
        {
            return MathF.Clamp(0.03f + renown / 5000f, 0.03f, 0.20f);
        }

        private static float GetSearchRadius(float renown)
        {
            return MathF.Clamp(BaseSearchRadius + renown / 250f, BaseSearchRadius, MaxSearchRadius);
        }

        private static int GetRecruitCount(MobileParty candidate, CharacterObject troop, int freeSlots, float renown)
        {
            int maxByRenown = Math.Max(1, Math.Min(6, 1 + (int)(renown / 300f)));
            int available = candidate.MemberRoster.GetTroopCount(troop);
            int randomized = MBRandom.RandomInt(1, maxByRenown + 1);
            return Math.Max(0, Math.Min(Math.Min(available, randomized), freeSlots));
        }

        private static CharacterObject ChooseRecruitableBandit(MobileParty candidate)
        {
            List<CharacterObject> options = candidate.MemberRoster.GetTroopRoster()
                .Where(x => x.Number > 0 && x.Character != null && !x.Character.IsHero && x.Character.Occupation == Occupation.Bandit)
                .Select(x => x.Character)
                .ToList();

            if (options.Count == 0)
                return null;

            return options[MBRandom.RandomInt(options.Count)];
        }

        private static MobileParty FindCandidateBanditParty(MobileParty mainParty, float searchRadius)
        {
            return MobileParty.AllBanditParties
                .Where(x => x != null
                    && x.IsActive
                    && !x.IsMainParty
                    && x.MapEvent == null
                    && x.CurrentSettlement == null
                    && x.MemberRoster.TotalManCount > 0
                    && x.GetPosition2D.DistanceSquared(mainParty.GetPosition2D) <= searchRadius * searchRadius)
                .OrderBy(x => x.GetPosition2D.DistanceSquared(mainParty.GetPosition2D))
                .FirstOrDefault();
        }
    }
}
