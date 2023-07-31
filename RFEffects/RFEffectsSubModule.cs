using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace RealmsForgotten.RFEffects
{
	// Token: 0x02000002 RID: 2
	public class RFEffectsSubModule : MBSubModuleBase
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		protected override void OnSubModuleLoad()
		{
			new Harmony("RFEffectsPatcher").PatchAll();
        }


		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
            this.ReplaceModel<DefaultDamageParticleModel, AnoritDamageParticleModel>(gameStarterObject);
            if (game.GameType is Campaign)  
				gameStarterObject.AddModel(new RFVolunteerModel());
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            CampaignGameStarter campaignGameStarter = starterObject as CampaignGameStarter;
            if (campaignGameStarter != null)
            {
                campaignGameStarter.AddBehavior(new RFCampaignBehavior());
            }
        }
        private void ReplaceModel<TBaseType, TChildType>(IGameStarter gameStarterObject) where TBaseType : GameModel where TChildType : TBaseType
		{
			IList<GameModel> list = gameStarterObject.Models as IList<GameModel>;
			if (list == null)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{

				if (list[i] is TBaseType)
				{
					flag = true;
					if (!(list[i] is TChildType))
					{
						list[i] = Activator.CreateInstance<TChildType>();
					}
				}
			}
			if (!flag)
			{
				gameStarterObject.AddModel(Activator.CreateInstance<TChildType>());
			}
		}

		public override void OnMissionBehaviorInitialize(Mission mission)
		{
            RFMissionBehaviour missionBehavior = new RFMissionBehaviour();
			mission.AddMissionBehavior(missionBehavior);
        }
	}
    class RFVolunteerModel : DefaultVolunteerModel
    {

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int baseValue = base.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

            IFaction buyerKingdom = buyerHero.MapFaction;
            if (buyerKingdom == null || buyerHero.Clan != null && buyerHero.Clan.IsClanTypeMercenary && buyerHero.Clan.IsMinorFaction)
                return baseValue;
            if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction))
                return 0;

            return baseValue;
        }
    }
}
