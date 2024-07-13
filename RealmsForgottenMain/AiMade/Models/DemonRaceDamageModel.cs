using System.Collections.Generic;
using SandBox.GameComponents;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Models
{
    public static class DemonRaceDamageModel
    {
        // List of race IDs that will use the custom damage model
        private static readonly HashSet<string> customRaceIds = new HashSet<string> { "tlachiquiy", "shaitan", "thog", "kharach", "brute" };

        // Override the method responsible for calculating damage
        public static void CalculateDamage(Agent attacker, in MissionWeapon weapon, ref float resultValue)
        {
            // Check if the attacker is unarmed
            bool isUnarmed = weapon.IsEmpty || weapon.CurrentUsageItem == null;

            // Check if the attacker's race ID is in the customRaceIds set and if the attacker is unarmed
            if (attacker != null && attacker.Character != null && customRaceIds.Contains(attacker.Character.Race.ToString()) && isUnarmed)
            {
                // Apply custom damage logic for unarmed attacks by specific races
                resultValue *= 2.0f; // Example: Increase damage by a factor of 2
            }
        }
}
}