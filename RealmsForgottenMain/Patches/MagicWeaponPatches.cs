using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{           // Correct version should be made by Cephas and added here
    //[HarmonyPatch(typeof(EquipmentElement))]
    //[HarmonyPatch("Equip")]
    //class EquipmentElementPatch
    //{
    //    static void Postfix(EquipmentElement __instance)
    //    {
    //        if (__instance.Item.StringId == "fantasy_axe_7")
    //        {
    //            Agent heroAgent = Agent.Main;
    //            if (heroAgent != null && heroAgent.IsHuman)
    //            {
    //                SkillObject oneHandedSkill = DefaultSkills.OneHanded;
    //                CharacterObject heroCharacter = heroAgent.Character as CharacterObject;
    //                if (heroCharacter != null)
    //                {
    //                    int currentXp = heroCharacter.HeroObject.GetSkillValue(oneHandedSkill);
    //                    int newXp = currentXp + (int)(currentXp * 0.25f);
    //                    heroCharacter.HeroObject.SetSkillValue(oneHandedSkill, newXp);
    //                    InformationManager.DisplayMessage(new InformationMessage("Your one-handed skill has been boosted by 25%!", Color.FromUint(0xFF00FF00)));
    //                }
    //            }
    //        }
    //        else
    //        {
    //            Agent heroAgent = Agent.Main;
    //            if (heroAgent != null && heroAgent.IsHuman)
    //            {
    //                SkillObject oneHandedSkill = DefaultSkills.OneHanded;
    //                CharacterObject heroCharacter = heroAgent.Character as CharacterObject;
    //                if (heroCharacter != null)
    //                {
    //                    int currentXp = heroCharacter.HeroObject.GetSkillValue(oneHandedSkill);
    //                    int newXp = currentXp - (int)(currentXp * 0.25f);
    //                    heroCharacter.HeroObject.SetSkillValue(oneHandedSkill, newXp);
    //                    InformationManager.DisplayMessage(new InformationMessage("Your one-handed skill has been reduced by 25%!", Color.FromUint(0xFFFF0000)));
    //                }
    //            }
    //        }
    //    }
    //}

}
