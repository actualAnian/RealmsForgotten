using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    public class RFEffectsSubModule : MBSubModuleBase
	{
        protected override void OnSubModuleLoad()
		{
			new Harmony("RFEffectsPatcher").PatchAll();
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(WeaponEffectConsequences)).Where(x => x.IsPublic))
            {
                WeaponEffectConsequences.Methods.Add(method.Name, (VictimAgentConsequence)method.CreateDelegate(typeof(VictimAgentConsequence)));
            }
        }


		public override void OnMissionBehaviorInitialize(Mission mission)
		{
            mission.AddMissionBehavior(new MagicEffectsBehavior());
            mission.AddMissionBehavior(new WeaponParticlesBehavior());
        }

	}

}
