using RealmsForgotten.Career;
using RealmsForgotten.Career.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    internal class RFClanFinanceModel : ClanFinanceModel
    {
        private ClanFinanceModel baseModel;

        public RFClanFinanceModel(ClanFinanceModel previousModel)
        {
            baseModel = previousModel;
        }

        public override int PartyGoldLowerThreshold => baseModel.PartyGoldLowerThreshold;

        public override ExplainedNumber CalculateClanExpenses(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            return baseModel.CalculateClanExpenses(clan, includeDescriptions, applyWithdrawals, includeDetails);
        }

        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var num = baseModel.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCareerPerkBenefits(clan, ref num);
            return num;
        }

        private void AddCareerPerkBenefits(Clan clan, ref ExplainedNumber num)
        {
            if (clan != Clan.PlayerClan || !PlayerCareerExtension.HasAnyCareer()) return;

            int mercenaryAward = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) * clan.MercenaryAwardMultiplier;
            ExplainedNumber bonusGain = new(mercenaryAward);
            CareerHelper.ApplyBasicCareerPassives(ref bonusGain, PassiveEffectType.MercContractIncome);
            float NumberToAdd = bonusGain.ResultNumber - mercenaryAward;
            num.Add(NumberToAdd, new TaleWorlds.Localization.TextObject("Contract class bonus", null), null);
        }

        public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            return baseModel.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);
        }

        public override int CalculateNotableDailyGoldChange(Hero hero, bool applyWithdrawals)
        {
            return baseModel.CalculateNotableDailyGoldChange(hero, applyWithdrawals);
        }

        public override int CalculateOwnerIncomeFromCaravan(MobileParty caravan)
        {
            int value = baseModel.CalculateOwnerIncomeFromCaravan(caravan);
            if (caravan.Owner != null && caravan.Owner == Hero.MainHero)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.CaravanIncome);
            return value;
        }

        public override int CalculateOwnerIncomeFromWorkshop(Workshop workshop)
        {
            int value = baseModel.CalculateOwnerIncomeFromWorkshop(workshop);
            if (workshop.Owner != null && workshop.Owner == Hero.MainHero)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.WorkshopIncome);
            return value;
        }

        public override int CalculateTownIncomeFromProjects(Town town)
        {
            return baseModel.CalculateTownIncomeFromProjects(town);
        }

        public override ExplainedNumber CalculateTownIncomeFromTariffs(Clan clan, Town town, bool applyWithdrawals = false)
        {
            ExplainedNumber value = baseModel.CalculateTownIncomeFromTariffs(clan, town, applyWithdrawals);
            if (clan == Clan.PlayerClan)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.TownIncome);
            return value;
        }

        public override int CalculateVillageIncome(Clan clan, Village village, bool applyWithdrawals = false)
        {

            int value = baseModel.CalculateVillageIncome(clan, village, applyWithdrawals);
            if (clan == Clan.PlayerClan)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.VillageIncome);
            return value;
        }

        public override float RevenueSmoothenFraction()
        {
            return baseModel.RevenueSmoothenFraction();
        }
    }
}