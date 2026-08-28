using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// ATAQUE SOB BANDEIRA NEGRA (porte do More Raiding, 2026-08-27).
    ///
    /// Assaltar uma caravana sem que ninguem saiba quem foi: a hostilidade do
    /// encounter e suprimida (sem declaracao, sem crime) e as tropas entram na
    /// batalha de preto. A chance e a pericia de Roguery — nao um dado escondido:
    /// o numero aparece no proprio texto da opcao.
    ///
    /// O limite de tropas e a alma da mecanica: um exercito nao passa
    /// despercebido, entao acima do teto a opcao aparece desabilitada com a
    /// explicacao, em vez de sumir sem dizer por que.
    /// </summary>
    public class RFBlackBannerBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "encounter",
                "rf_attack_black_banner",
                "{=rf_raid_bb_menu}Attack under black banner (Success: {RF_BB_CHANCE}%)",
                BlackBannerCondition,
                BlackBannerConsequence,
                isLeave: false,
                index: 5);

            starter.AddPlayerLine(
                "rf_black_banner_caravan",
                "caravan_talk",
                "close_window",
                "{=rf_raid_bb_threat}We are not here to talk. Prepare to die!",
                CaravanLineCondition,
                CaravanLineConsequence,
                100,
                CaravanLineClickable);
        }

        /// <summary>Roguery / (pericia de certeza), limitado a [0,1].</summary>
        internal static float BlackBannerChance()
        {
            int certainty = RFRaidConfig.BlackBannerCertaintySkill;
            if (certainty < 1)
            {
                certainty = 1;
            }
            return MBMath.ClampFloat(Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) / (float)certainty, 0f, 1f);
        }

        private static bool IsRaidableCaravan()
        {
            MobileParty target = PlayerEncounter.EncounteredMobileParty;
            return PlayerEncounter.Current != null
                   && target != null
                   && target.IsCaravan
                   && target.Owner != Hero.MainHero;
        }

        private static bool IsPartySmallEnough()
        {
            return MobileParty.MainParty.MemberRoster.TotalHealthyCount <= RFRaidConfig.BlackBannerMaxTroops;
        }

        private static TextObject TooManyTroopsText()
        {
            return new TextObject("{=rf_raid_bb_toobig}Your host is too large to pass unnoticed. Keep at most {MAX} healthy soldiers to strike under a black banner.")
                .SetTextVariable("MAX", RFRaidConfig.BlackBannerMaxTroops);
        }

        private bool BlackBannerCondition(MenuCallbackArgs args)
        {
            if (!RFRaidConfig.BlackBannerEnabled || !IsRaidableCaravan())
            {
                return false;
            }

            args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;

            if (!IsPartySmallEnough())
            {
                // Visivel porem desabilitada: o jogador aprende a regra.
                return MenuHelper.SetOptionProperties(args, true, true, TooManyTroopsText());
            }

            MBTextManager.SetTextVariable("RF_BB_CHANCE", (BlackBannerChance() * 100f).ToString("0.0"), false);
            return true;
        }

        private void BlackBannerConsequence(MenuCallbackArgs args)
        {
            if (MBRandom.RandomFloat <= BlackBannerChance())
            {
                RFRaidState.BlackBannerActive = true;
                RFRaidState.SuppressNextHostility = true;
            }
            else
            {
                RFRaidState.BlackBannerActive = false;
                RFRaidState.SuppressNextHostility = false;
                MobileParty target = PlayerEncounter.EncounteredMobileParty;
                if (target != null)
                {
                    BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, target.Party);
                }
                MBInformationManager.AddQuickInformation(
                    new TextObject("{=rf_raid_bb_failed}Your attempt to hide your identity failed!"), 0, null, null, "");
            }

            MenuHelper.EncounterAttackConsequence(args);
        }

        private bool CaravanLineCondition()
        {
            return RFRaidConfig.BlackBannerEnabled && IsRaidableCaravan();
        }

        private bool CaravanLineClickable(out TextObject explanation)
        {
            if (IsPartySmallEnough())
            {
                explanation = new TextObject("{=rf_raid_bb_attack}Attack under a black banner.");
                return true;
            }
            explanation = TooManyTroopsText();
            return false;
        }

        private void CaravanLineConsequence()
        {
            RFRaidState.BlackBannerActive = true;
            RFRaidState.SuppressNextHostility = true;
        }
    }
}
