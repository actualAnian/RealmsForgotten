using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RFEffects;
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
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(WeaponEffectConsequences)).Where(x => x.IsPublic))
            {
                WeaponEffectConsequences.AllMethods.Add(method.Name, (VictimAgentConsequence)method.CreateDelegate(typeof(VictimAgentConsequence)));
            }
        }


        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);

        }

		public override void OnMissionBehaviorInitialize(Mission mission)
		{
            mission.AddMissionBehavior(new MagicEffectsBehavior());
            mission.AddMissionBehavior(new WeaponParticlesBehavior());
        }

	}

}
