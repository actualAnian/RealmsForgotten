using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.CustomOrderofBattle
{
    public static class SpearWeaponHeuristics
    {
        // Palavras que indicam lança/long pole para infantaria.
        private static readonly string[] IncludeTokens = { "spear", "pike", "yari", "hasta" };
        // Evitar classificar dardos/lanças de arremesso como "spearman".
        private static readonly string[] ExcludeTokens = { "javelin", "throw", "pilum" };

        /// <summary>
        /// Heurística simples: examina os 5 primeiros slots de arma do CharacterObject e procura por IDs contendo "spear/pike/...".
        /// </summary>
        public static bool IsSpearmanByItemId(CharacterObject character)
        {
            if (character == null) return false;

            // A própria TW varre 0..4 para achar arma padrão (ver Helpers.CraftingHelper.GetDefaultWeapon). :contentReference[oaicite:2]{index=2}
            for (int i = 0; i <= 4; i++)
            {
                EquipmentElement el = character.Equipment.GetEquipmentFromSlot((EquipmentIndex)i);
                ItemObject item = el.Item;
                if (item == null || item.PrimaryWeapon == null) continue;

                string id = (item.StringId ?? string.Empty).ToLowerInvariant();

                // Filtra javelins, etc.
                foreach (var bad in ExcludeTokens)
                    if (id.Contains(bad)) goto nextSlot;

                foreach (var good in IncludeTokens)
                    if (id.Contains(good)) return true;

                    nextSlot:;
            }

            return false;
        }
    }
}