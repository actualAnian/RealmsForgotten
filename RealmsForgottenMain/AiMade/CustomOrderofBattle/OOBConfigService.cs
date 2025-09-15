using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using RealmsForgotten.AiMade.CustomOrderofBattle.Config;

namespace RealmsForgotten.AiMade.CustomOrderofBattle
{
    public static class OOBConfigService
    {
        // Caminho padrão do JSON dentro do módulo
        public static string GetConfigPath()
        {
            // Use seu módulo/estrutura; este exemplo usa ModuleData ao lado do module folder.
            // Ajuste se preferir colocar ao lado do .dll
            string moduleDir = BasePath.Name + "/Modules/RealmsForgotten";
            return Path.Combine(moduleDir, "AutoOOBConfig.json");
        }

        public static AutoOOBRootConfig LoadOrDefault()
        {
            try
            {
                var path = GetConfigPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var cfg = JsonConvert.DeserializeObject<AutoOOBRootConfig>(json);
                    return cfg ?? new AutoOOBRootConfig();
                }
            }
            catch (Exception)
            {
                // swallow -> devolve defaults vazios
            }
            return new AutoOOBRootConfig();
        }

        public static List<OOBFormationConfig> ResolveEffectiveConfigForCurrentBattle(AutoOOBRootConfig root, out string usedKey)
        {
            usedKey = "defaults";

            // Descobre cultura/facção do player (lado aliado)
            var playerKingdom = Hero.MainHero?.Clan?.Kingdom;
            var factionId = playerKingdom?.StringId;          // ex.: "vlandia", "sturgia" etc.
            var cultureId = Hero.MainHero?.Culture?.StringId; // ex.: "vlandia", "sturgia" (mesmo id em vanilla)

            // 1) base: defaults
            var merged = new Dictionary<int, OOBFormationConfig>();
            foreach (var f in root.defaults.formations) merged[f.index] = Clone(f);

            // 2) aplica cultura (se presente)
            if (!string.IsNullOrEmpty(cultureId) && root.cultures.TryGetValue(cultureId, out var cultureBlock))
            {
                usedKey = $"culture:{cultureId}";
                foreach (var f in cultureBlock.formations) merged[f.index] = Clone(f);
            }

            // 3) aplica facção (maior prioridade)
            if (!string.IsNullOrEmpty(factionId) && root.factions.TryGetValue(factionId, out var factionBlock))
            {
                usedKey = $"faction:{factionId}";
                foreach (var f in factionBlock.formations) merged[f.index] = Clone(f);
            }

            // Garante 8 slots
            for (int i = 0; i < 8; i++)
            {
                if (!merged.ContainsKey(i))
                {
                    merged[i] = new OOBFormationConfig { index = i, @class = "Unset", primaryWeight = 0, secondaryWeight = 0 };
                }
            }

            return merged.Values.OrderBy(x => x.index).ToList();
        }

        private static OOBFormationConfig Clone(OOBFormationConfig f) =>
            new OOBFormationConfig
            {
                index = f.index,
                @class = f.@class,
                primaryWeight = f.primaryWeight,
                secondaryWeight = f.secondaryWeight,
                filters = f.filters != null ? new List<string>(f.filters) : new List<string>(),
                commanderStringId = f.commanderStringId,
                heroTroopStringIds = f.heroTroopStringIds != null ? new List<string>(f.heroTroopStringIds) : null,
                enabled = f.enabled
            };
    }
}