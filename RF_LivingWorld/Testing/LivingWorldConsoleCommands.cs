using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RF_LivingWorld
{
    public static class LivingWorldConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("status", "rf.living")]
        public static string Status(List<string> arguments)
        {
            if (Campaign.Current == null)
            {
                return "rf.living status: campaign not running.";
            }
            return LivingWorldCampaignBehavior.Instance?.Status() ?? "RF Living World behavior is not active.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn", "rf.living")]
        public static string Spawn(List<string> arguments)
        {
            if (Campaign.Current == null || MobileParty.MainParty == null)
            {
                return "rf.living spawn: campaign not running.";
            }

            if (arguments.Count == 0 || !TryParsePartyType(arguments[0], out LivingWorldPartyType type))
            {
                return "Usage: rf.living spawn <merchant|herder|pilgrim|leper|hunter|procession|healer|tax|prisoners|dowry> [sheep|cow|hog|mule|camel]";
            }

            LivingWorldHerdVariant variant = LivingWorldHerdVariant.None;
            if (type == LivingWorldPartyType.Herder)
            {
                if (arguments.Count < 2 || !Enum.TryParse(arguments[1], true, out variant) || variant == LivingWorldHerdVariant.None)
                {
                    return "Herder requires: sheep, cow, hog, mule, or camel.";
                }
            }

            LivingWorldCampaignBehavior? behavior = LivingWorldCampaignBehavior.Instance;
            if (behavior == null)
            {
                return "RF Living World behavior is not active.";
            }

            behavior.TrySpawnFromConsole(type, variant, out _, out string result);
            return result;
        }

        private static bool TryParsePartyType(string value, out LivingWorldPartyType type)
        {
            if (Enum.TryParse(value, true, out type))
            {
                return true;
            }

            switch (value.ToLowerInvariant())
            {
                case "pilgrim":
                    type = LivingWorldPartyType.Pilgrim;
                    return true;
                case "leper":
                    type = LivingWorldPartyType.Leper;
                    return true;
                case "hunter":
                    type = LivingWorldPartyType.Hunter;
                    return true;
                case "procession":
                    type = LivingWorldPartyType.ReligiousProcession;
                    return true;
                case "healer":
                    type = LivingWorldPartyType.Healer;
                    return true;
                case "tax":
                    type = LivingWorldPartyType.TaxCollector;
                    return true;
                case "prisoners":
                    type = LivingWorldPartyType.PrisonerEscort;
                    return true;
                case "dowry":
                    type = LivingWorldPartyType.DowryProcession;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
