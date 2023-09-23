using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Utility
{
    public static class RFUtility
    {
        public static IEnumerable<Agent> GetAgentsInRadius(Vec2 searchPoint, float radius)
        {
            AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, searchPoint, radius, extendRangeByBiggestAgentCollisionPadding: true);
            while (searchStruct.LastFoundAgent != null)
            {
                Agent lastFoundAgent = searchStruct.LastFoundAgent;
                if (lastFoundAgent.CurrentMortalityState != Agent.MortalityState.Invulnerable)
                {
                    yield return lastFoundAgent;
                }

                AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
            }
        }

        public static void ModifyCharacterSkillAttribute(BasicCharacterObject character, SkillObject skill, int value)
        {

            FieldInfo characterSkillsProperty = AccessTools.Field(typeof(BasicCharacterObject), "DefaultCharacterSkills");
            if (characterSkillsProperty == null)
                return;
            if (value < 0)
                value = 0;
            object characterSkills = characterSkillsProperty.GetValue(character);

            PropertyInfo skillsInfo = AccessTools.Property(characterSkills.GetType(), "Skills");
            object skillValue = skillsInfo.GetValue(characterSkills);
            FieldInfo attributesField = AccessTools.Field(skillValue.GetType(), "_attributes");
            Dictionary<SkillObject, int> attributes = (Dictionary<SkillObject, int>)attributesField.GetValue(skillValue);

            attributes[skill] = value;
            attributesField.SetValue(skillValue, attributes);
        }
        public static int GetNumberAfterSkillWord(string inputString, string word, bool isMainAgent = false)
        {
            int result = -1;
            int wordIndex = inputString.IndexOf(word);

            if (wordIndex >= 0)
            {
                string textAfterWord = inputString.Substring(wordIndex + word.Length);

                Match match = Regex.Match(textAfterWord, @"\d+");

                if (match.Success)
                {
                    result = int.Parse(match.Value);
                }
            }

            if (isMainAgent)
            {
                string skill = null;
                switch (word)
                {
                    case "rfonehanded":
                        skill = "One Handed";
                        break;
                    case "rftwohanded":
                        skill = "Two Handed";
                        break;
                    case "rfpolearm":
                        skill = "Polearm";
                        break;
                    case "rfbow":
                        skill = "Bow";
                        break;
                    case "rfcrossbow":
                        skill = "Crossbow";
                        break;
                    case "rfthrowing":
                        skill = "Throwing";
                        break;

                }

                InformationManager.DisplayMessage(new InformationMessage($"A weapon you're carrying has enhanced your skill in combat, increasing your {skill} by {result} points.", Color.FromUint(9424384)));
            }

            return result;
        }
    }
}
