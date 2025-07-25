using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten
{
    public class CapitulationSystemBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<Kingdom, CampaignTime> _lastCapitulation = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckCapitulations);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void CheckCapitulations()
        {
            foreach (var weak in Kingdom.All)
            {
                if (weak == null || weak.IsEliminated || weak.Leader == null)
                    continue;

                if (!CanCapitulate(weak))
                    continue;

                var enemies = Kingdom.All
                    .Where(k => k != weak && weak.IsAtWarWith(k))
                    .OrderByDescending(k => k.TotalStrength);

                foreach (var strong in enemies)
                {
                    if (!ShouldCapitulate(weak, strong))
                        continue;

                    if (strong == Hero.MainHero.Clan.Kingdom)
                    {
                        ShowPlayerCapitulationInquiry(weak, strong);
                    }
                    else
                    {
                        ApplyCapitulation(weak, strong);
                    }

                    _lastCapitulation[weak] = CampaignTime.Now;
                    break;
                }
            }
        }

        private bool CanCapitulate(Kingdom weak)
        {
            if (_lastCapitulation.TryGetValue(weak, out var last))
            {
                return CampaignTime.Now - last > CampaignTime.Days(15);
            }
            return true;
        }

        private bool ShouldCapitulate(Kingdom weak, Kingdom strong)
        {
            int weakFiefs = weak.Fiefs.Count(f => f.IsTown || f.IsCastle);
            float strengthRatio = strong.TotalStrength / (weak.TotalStrength + 1);
            return weakFiefs <= 2 && strengthRatio >= 3.0f;
        }

        private void ShowPlayerCapitulationInquiry(Kingdom weak, Kingdom strong)
        {
            InformationManager.ShowInquiry(new InquiryData(
                $"{weak.Name} Offers Capitulation",
                $"{weak.Leader.Name} seeks to surrender. Do you accept their unconditional surrender?",
                true, true,
                "Accept",
                "Decline",
                () => ApplyCapitulation(weak, strong),
                null
            ));
        }

        private void ApplyCapitulation(Kingdom weak, Kingdom strong)
        {
            int tribute = MBRandom.RandomInt(3000, 8000);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name} has capitulated to {strong.Name}!\nTribute paid: {tribute} gold."
            ));

            // Gold transfer from weak to strong
            GiveGoldAction.ApplyBetweenCharacters(weak.Leader, strong.Leader, tribute, false);

            foreach (var clan in weak.Clans.ToList())
            {
                if (clan == Clan.PlayerClan || clan.IsUnderMercenaryService)
                    continue;

                ChangeKingdomAction.ApplyByLeaveKingdom(clan);
                ChangeKingdomAction.ApplyByJoinToKingdom(clan, strong, showNotification: true);

                // Optional: relation adjustments
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, weak.Leader, -15);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(clan.Leader, strong.Leader, +10);
            }

            // Force peace between the kingdoms
            MakePeaceAction.Apply(weak, strong, 0);
        }
    }
}