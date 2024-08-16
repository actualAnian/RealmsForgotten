using RealmsForgotten.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    public class SpecialDamageCalculator
    {
        public void ApplyDamage(Agent agent, ref float damage, string damageType, string weaponId)
        {
            // Retrieve race ID from agent's character
            int raceId = agent.Character?.Race ?? RaceUtility.GetRaceId("unknown");

            InformationManager.DisplayMessage(new InformationMessage($"Agent Race ID: {raceId}"));

            string raceStringId = GetRaceStringId(raceId);
            var resistances = ExtendedInfoManager.GetRaceResistances(raceStringId);

            foreach (var resistance in resistances)
            {
                if (resistance.ResistedDamageType == damageType)
                {
                    damage *= (1 - resistance.ReductionPercent);
                }
            }

            // Check for specific weapon ID exception
            if (weaponId == "ancient_elvish_polearm")
            {
                damage = ApplySpecificWeaponDamage(agent, damage);
            }
        }

        private string GetRaceStringId(int raceId)
        {
            // Mapping race IDs to string identifiers using RaceUtility
            if (raceId == RaceUtility.GetRaceId("half_giant")) return "half_giant";
            if (raceId == RaceUtility.GetRaceId("bark")) return "bark";
            if (raceId == RaceUtility.GetRaceId("nurh")) return "nurh";
            if (raceId == RaceUtility.GetRaceId("daimo")) return "daimo";
            if (raceId == RaceUtility.GetRaceId("sillok")) return "sillok";
            return "unknown";
        }

        private float ApplySpecificWeaponDamage(Agent agent, float damage)
        {
            // Logic for applying damage with a specific weapon
            return damage; // Modify as needed
        }
    }
}