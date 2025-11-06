using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.Models
{
    public class RFBanditDensityModel : BanditDensityModel
    {
        private BanditDensityModel _previousModel;

        public RFBanditDensityModel(BanditDensityModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt
        {
            get
            {
                return 3;
            }
        }

        public override int NumberOfMaximumBanditPartiesInEachHideout
        {
            get
            {
                return 4;
            }
        }

        public override int NumberOfMaximumBanditPartiesAroundEachHideout
        {
            get
            {
                return 4;
            }
        }

        public override int NumberOfMaximumHideoutsAtEachBanditFaction
        {
            get
            {
                return 8;
            }
        }

        public override int NumberOfInitialHideoutsAtEachBanditFaction
        {
            get
            {
                return 3;
            }
        }

        public override int NumberOfMinimumBanditTroopsInHideoutMission
        {
            get
            {
                return 10;
            }
        }

        public override int NumberOfMaximumTroopCountForFirstFightInHideout
        {
            get
            {
                return 50;
            }
        }

        public override int NumberOfMaximumTroopCountForBossFightInHideout
        {
            get
            {
                return MathF.Floor(1f + 5f * (1f + Campaign.Current.PlayerProgress));
            }
        }

        public override float SpawnPercentageForFirstFightInHideoutMission
        {
            get
            {
                return 0.75f;
            }
        }

        public override int GetMaximumTroopCountForHideoutMission(MobileParty party)
        {
            float num = 25f;
            if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics, false))
            {
                num += DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
            }
            return MathF.Round(num);
        }

        public override int GetMaxSupportedNumberOfLootersForClan(Clan clan) => _previousModel.GetMaxSupportedNumberOfLootersForClan(clan);

        public override int GetMinimumTroopCountForHideoutMission(MobileParty party) => _previousModel.GetMinimumTroopCountForHideoutMission(party);

        public override bool IsPositionInsideNavalSafeZone(CampaignVec2 position) => _previousModel.IsPositionInsideNavalSafeZone(position);
    }
}
