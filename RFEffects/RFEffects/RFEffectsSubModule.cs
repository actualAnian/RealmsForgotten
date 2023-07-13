using System;
using System.Collections.Generic;
using System.Reflection;
using AnoritKingdom;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace RFEffects
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
        // Token: 0x06000009 RID: 9 RVA: 0x0000230C File Offset: 0x0000050C
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

		// Token: 0x0600000A RID: 10 RVA: 0x00002380 File Offset: 0x00000580
		public override void OnMissionBehaviorInitialize(Mission mission)
		{
			AnoritMissionBehaviour missionBehavior = new AnoritMissionBehaviour();
			mission.AddMissionBehavior(missionBehavior);
        }
	}

}
