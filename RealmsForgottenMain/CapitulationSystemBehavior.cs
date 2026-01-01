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
using TaleWorlds.Localization;

namespace RealmsForgotten
{
    public class CapitulationSystemBehavior : CampaignBehaviorBase
    {
        private Dictionary<Kingdom, CampaignTime> _lastCapitulation = new Dictionary<Kingdom, CampaignTime>();
        private readonly List<string> NonCapitulatingNations = new()
        {
            "aserai",
            "aserai_a",
            "aserai_b",
            "aserai_c",
            "aserai_d",
            "aserai_e",

        };
        // --- CONFIG ---
        private const float GraceDays = 25f; // No capitulations during first X days

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckCapitulations);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastCapitulation", ref _lastCapitulation);
        }

        private void CheckCapitulations()
        {
            foreach (var weak in Kingdom.All.ToList())
            {
                if (weak == null || weak.IsEliminated || weak.Leader == null)
                    continue;

                if (!CanCapitulate(weak))
                    continue;

                IEnumerable<Kingdom> enemies = weak.FactionsAtWarWith
                    .Where(f => f.IsKingdomFaction)
                    .Cast<Kingdom>()
                    .OrderByDescending(k => k.CurrentTotalStrength);

                foreach (var strong in enemies)
                {
                    if (strong == null || strong.IsEliminated)
                        continue;

                    if (!ShouldCapitulate(weak, strong))
                        continue;

                    if (weak == Hero.MainHero.Clan.Kingdom)
                    {
                        ShowPlayerSurrenderInquiry(weak, strong);
                    }
                    else if (strong == Hero.MainHero.Clan.Kingdom)
                    {
                        ShowAIDemandInquiry(weak, strong);
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
            if (NonCapitulatingNations.Contains(weak.StringId))
                return false;
            if (_lastCapitulation.TryGetValue(weak, out var last))
            {
                return (CampaignTime.Now - last).ToDays > 15f;
            }
            return true;
        }

        private bool ShouldCapitulate(Kingdom weak, Kingdom strong)
        {
            int weakFiefs = weak.Fiefs.Count();
            float strengthRatio = strong.CurrentTotalStrength / (weak.CurrentTotalStrength + 1f);

            return weakFiefs <= 2 && strengthRatio >= 3.0f;
        }

        private void ShowPlayerSurrenderInquiry(Kingdom playerKingdom, Kingdom victor)
        {
            TextObject title = new TextObject("Demand for Surrender");
            TextObject text = new TextObject(
                "{VICTOR_LEADER} of {VICTOR_KINGDOM} demands your unconditional surrender. " +
                "Your kingdom is on the brink of collapse. You can accept to become a vassal " +
                "under their rule, or refuse and face the consequences.");

            text.SetTextVariable("VICTOR_LEADER", victor.Leader.Name);
            text.SetTextVariable("VICTOR_KINGDOM", victor.Name);

            InformationManager.ShowInquiry(new InquiryData(
                title.ToString(),
                text.ToString(),
                true,
                true,
                "Accept Vassalage",
                "Refuse and Fight!",
                () => ApplyPlayerVassalage(playerKingdom, victor),
                () => ApplyPlayerConcessions(playerKingdom, victor)
            ));
        }

        private void ShowAIDemandInquiry(Kingdom weak, Kingdom strong)
        {
            InformationManager.ShowInquiry(new InquiryData(
                $"{weak.Name} Offers Capitulation",
                $"{weak.Leader.Name} seeks to surrender. Do you accept their unconditional surrender?",
                true,
                true,
                "Accept",
                "Decline",
                () => ApplyCapitulation(weak, strong),
                null
            ));
        }

        private void ApplyPlayerVassalage(Kingdom playerKingdom, Kingdom victor)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"You have bent the knee. Your kingdom has been dissolved, and Clan {Clan.PlayerClan.Name} now serves {victor.Name}.",
                Colors.Yellow));

            foreach (var clan in playerKingdom.Clans.ToList())
            {
                ChangeKingdomAction.ApplyByJoinToKingdom(clan, victor);
            }

            DestroyKingdomAction.Apply(playerKingdom);
        }

        private void ApplyPlayerConcessions(Kingdom playerKingdom, Kingdom victor)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"You refused to surrender, but at a great cost. Your vassals have abandoned you for the cause of {victor.Name}!",
                Colors.Red));

            foreach (var clan in playerKingdom.Clans.ToList())
            {
                if (clan != Clan.PlayerClan)
                {
                    ChangeKingdomAction.ApplyByJoinToKingdom(clan, victor);
                }
            }

            MakePeaceAction.Apply(playerKingdom, victor);
        }

        private void ApplyCapitulation(Kingdom weak, Kingdom strong)
        {
            int tribute = MBRandom.RandomInt(3000, 8000);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name} has capitulated to {strong.Name}! Tribute paid: {tribute} gold."));

            GiveGoldAction.ApplyBetweenCharacters(weak.Leader, strong.Leader, tribute, false);

            foreach (var clan in weak.Clans.ToList())
            {
                if (clan.IsUnderMercenaryService)
                    continue;

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, strong, showNotification: false);
            }

            DestroyKingdomAction.Apply(weak);
        }
    }
}