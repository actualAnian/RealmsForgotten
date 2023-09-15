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
    internal class RFUtility
    {
        public static void RemoveInitialStateOption(string name)
        {
            try
            {
                FieldInfo field = typeof(TaleWorlds.MountAndBlade.Module).GetField("_initialStateOptions", BindingFlags.Instance | BindingFlags.NonPublic);
                object value = field.GetValue(TaleWorlds.MountAndBlade.Module.CurrentModule);
                //bool flag = !(value.GetType() == typeof(List<InitialStateOption>));
                if (value.GetType() == typeof(List<InitialStateOption>))
                {
                    List<InitialStateOption> list = (List<InitialStateOption>)value;
                    foreach (InitialStateOption initialStateOption in list)
                    {
                        bool flag2 = initialStateOption.Id.Contains(name);
                        if (flag2)
                        {
                            list.Remove(initialStateOption);
                        }
                    }
                    field.SetValue(typeof(TaleWorlds.MountAndBlade.Module).GetField("_initialStateOptions", BindingFlags.Instance | BindingFlags.NonPublic), list);
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(ex.Message));
            }
        }

        public static void ModifyCharacterSkillAttribute(BasicCharacterObject character, SkillObject skill, int value)
        {

            FieldInfo characterSkillsProperty = AccessTools.Field(typeof(BasicCharacterObject), "DefaultCharacterSkills");
            if (characterSkillsProperty == null)
                return;
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

                InformationManager.DisplayMessage(new InformationMessage($"The weapon you are wielding has enhanced your skill in combat, increasing your {skill} by {result} points.", Color.FromUint(9424384)));
            }

            return result;
        }
    }
}
