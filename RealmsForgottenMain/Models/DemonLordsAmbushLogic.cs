using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class DemonLordsAmbushLogic : MissionLogic
    {
        private HashSet<int> targetRaceIds;
        private int halfGiantRaceId;

        public DemonLordsAmbushLogic()
        {
            targetRaceIds = new HashSet<int>();

            // List of existing target race names
            List<string> targetRaceNames = new List<string> { "sillok", "bark", "daimo", "nurh" };

            // Attempt to get the race ID for each target race name and add to the HashSet
            foreach (var raceName in targetRaceNames)
            {
                try
                {
                    int raceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault(raceName);
                    if (raceId != -1) // Assuming -1 is returned if the race is not found
                    {
                        targetRaceIds.Add(raceId);
                        LogMessage($"DemonLordsAmbushLogic: Added race '{raceName}' with ID {raceId}.");
                    }
                    else
                    {
                        LogMessage($"DemonLordsAmbushLogic: Race '{raceName}' not found.");
                    }
                }
                catch (KeyNotFoundException)
                {
                    LogMessage($"DemonLordsAmbushLogic: Race '{raceName}' not found.");
                }
            }

            // Attempt to get the race ID for "half_giant"
            try
            {
                halfGiantRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("half_giant");
                if (halfGiantRaceId == -1)
                {
                    LogMessage("DemonLordsAmbushLogic: Race 'half_giant' not found.");
                }
                else
                {
                    LogMessage($"DemonLordsAmbushLogic: Added race 'half_giant' with ID {halfGiantRaceId}.");
                }
            }
            catch (KeyNotFoundException)
            {
                LogMessage("DemonLordsAmbushLogic: Race 'half_giant' not found.");
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            // Check if the affected agent has any of the target race IDs and is not the player
            if (affectedAgent.Character?.Race != null && targetRaceIds.Contains(affectedAgent.Character.Race) && !affectedAgent.IsPlayerControlled)
            {
                // Check if the weapon ID is "ancient_elvish_polearm"
                string weaponId = affectorWeapon.Item?.StringId ?? string.Empty;
                if (weaponId == "ancient_elvish_polearm")
                {
                    // Apply absorb ratio of 80%
                    float absorbedDamage = blow.InflictedDamage * 0.2f;
                    affectedAgent.Health = Math.Max(0, affectedAgent.Health - absorbedDamage);
                    LogMessage($"DemonLordsAmbushLogic: {affectedAgent.Name} hit by {weaponId}, absorbed 80%, applied damage: {absorbedDamage}.");
                    return;
                }

                // Check if the damage type is fire
                if (blow.DamageType.ToString() == "Fire")
                {
                    // Apply full damage for fire attacks
                    float fireDamage = blow.InflictedDamage;
                    affectedAgent.Health = Math.Max(0, affectedAgent.Health - fireDamage);
                    LogMessage($"DemonLordsAmbushLogic: {affectedAgent.Name} hit by fire, applied damage: {fireDamage}.");
                    return;
                }

                // Apply health boost to the affected agent
                affectedAgent.Health += blow.InflictedDamage + 10;
                LogMessage($"DemonLordsAmbushLogic: Applied health boost to {affectedAgent.Name} due to target race.");
            }

            // Check if the affected agent is a half_giant and apply 95% damage reduction for piercing damage
            if (affectedAgent.Character?.Race == halfGiantRaceId)
            {
                if (blow.DamageType.ToString() == "Pierce")
                {
                    // Apply 95% damage reduction for piercing attacks
                    float reducedDamage = blow.InflictedDamage * 0.05f;
                    affectedAgent.Health = Math.Max(0, affectedAgent.Health - reducedDamage);
                    LogMessage($"DemonLordsAmbushLogic: {affectedAgent.Name} hit by piercing attack, applied 95% damage reduction, applied damage: {reducedDamage}.");
                    return;
                }
            }
        }

        private void LogMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message));
        }
    }
}
