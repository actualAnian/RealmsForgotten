using System;
using System.Collections.Generic;
using HarmonyLib;
using ParticleTester;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

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

        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);

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
            mission.AddMissionBehavior(new WeaponEffectsBehavior());
        }

	}

}
