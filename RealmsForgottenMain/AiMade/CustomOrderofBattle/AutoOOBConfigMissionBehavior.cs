using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.AiMade.CustomOrderofBattle.Config;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.CustomOrderofBattle
{
    public class AutoOOBConfigMissionBehavior : MissionLogic
    {
        private bool _applied;
        private const bool DEBUG_LOGS = true; // deixe true para testar

        public override void OnCreated()
        {
            // Aplica o quanto antes (antes do OOB ser carregado pela UI)
            TryApplyOOB();
        }

        public override void OnMissionTick(float dt)
        {
            // Fallback (caso OnCreated aconteça cedo demais em algum fluxo)
            if (!_applied)
                TryApplyOOB();
        }

        private void TryApplyOOB()
        {
            if (_applied || Campaign.Current == null || Mission.Current == null)
                return;

            var oob = Campaign.Current.GetCampaignBehavior<OrderOfBattleCampaignBehavior>();
            if (oob == null) return;

            // 1) Carrega config
            var root = OOBConfigService.LoadOrDefault();

            // 2) Resolve (defaults -> culture -> faction)
            var effective = OOBConfigService.ResolveEffectiveConfigForCurrentBattle(root, out var usedKey);

            // 3) Constrói lista final, garantindo 8 entradas
            var list = BuildEightFormationDatas(effective);

            // 4) Aplica no OOB
            oob.SetFormationInfos(list, Mission.Current.IsSiegeBattle);

            // 5) Logs de verificação
            if (DEBUG_LOGS)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[OOB] Preconfig: key={usedKey} siege={Mission.Current.IsSiegeBattle}"));
                for (int i = 0; i < 8; i++)
                {
                    var d = oob.GetFormationDataAtIndex(i, Mission.Current.IsSiegeBattle);
                    bool spear = false;
                    d?.Filters?.TryGetValue(FormationFilterType.Spear, out spear);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[OOB] slot {i}: class={d?.FormationClass} spear={spear} weights=({d?.PrimaryClassWeight}/{d?.SecondaryClassWeight})"));
                }
            }

            _applied = true;
        }

        // -------- helpers --------

        private static List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>
            BuildEightFormationDatas(List<OOBFormationConfig> effective)
        {
            // Mapa por índice (0..7) vindo da config (pode não ter todos)
            var byIndex = effective.ToDictionary(e => e.index, e => e);

            var result = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>(8);
            for (int i = 0; i < 8; i++)
            {
                OrderOfBattleCampaignBehavior.OrderOfBattleFormationData data;

                if (byIndex.TryGetValue(i, out var f) && f.enabled)
                {
                    var (deploymentClass, primary, secondary) = MapToDeploymentAndWeights(f);
                    var filters = BuildFiltersDict(f.filters);

                    // Commander e heroTroops (List<Hero>)
                    Hero commander = !string.IsNullOrWhiteSpace(f.commanderStringId)
                        ? Hero.FindFirst(h => h.StringId == f.commanderStringId)
                        : null;

                    List<Hero> heroTroops = null;
                    if (f.heroTroopStringIds != null && f.heroTroopStringIds.Count > 0)
                    {
                        heroTroops = new List<Hero>();
                        foreach (var hid in f.heroTroopStringIds)
                        {
                            var h = Hero.FindFirst(x => x.StringId == hid);
                            if (h != null) heroTroops.Add(h);
                        }
                    }

                    data = new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(
                        commander,
                        heroTroops,
                        deploymentClass,
                        primary,
                        secondary,
                        filters
                    );
                }
                else
                {
                    // Preenche com UNSET (nunca deixar NULL em nenhum slot)
                    data = new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(
                        null,
                        null,
                        DeploymentFormationClass.Unset,
                        0,
                        0,
                        new Dictionary<FormationFilterType, bool>()
                    );
                }

                result.Add(data);
            }

            return result;
        }

        private static (DeploymentFormationClass cls, int p, int s) MapToDeploymentAndWeights(OOBFormationConfig f)
        {
            var cls = ParseDeploymentClassSafe(f.@class);
            var p = Math.Max(0, f.primaryWeight);
            var s = Math.Max(0, f.secondaryWeight);
            return (cls, p, s);
        }

        private static DeploymentFormationClass ParseDeploymentClassSafe(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DeploymentFormationClass.Unset;
            switch (value.Trim().ToLowerInvariant())
            {
                case "infantry": return DeploymentFormationClass.Infantry;
                case "ranged": return DeploymentFormationClass.Ranged;
                case "cavalry": return DeploymentFormationClass.Cavalry;
                case "horsearcher": return DeploymentFormationClass.HorseArcher;
                default: return DeploymentFormationClass.Unset;
            }
        }

        private static Dictionary<FormationFilterType, bool> BuildFiltersDict(List<String> filters)
        {
            var dict = new Dictionary<FormationFilterType, bool>
            {
                [FormationFilterType.Shield] = false,
                [FormationFilterType.Spear] = false,
                [FormationFilterType.Thrown] = false,
                [FormationFilterType.Heavy] = false,
                [FormationFilterType.HighTier] = false,
                [FormationFilterType.LowTier] = false
            };

            if (filters == null) return dict;

            foreach (var raw in filters)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                switch (raw.Trim().ToLowerInvariant())
                {
                    case "shield": dict[FormationFilterType.Shield] = true; break;
                    case "spear": dict[FormationFilterType.Spear] = true; break;
                    case "thrown": dict[FormationFilterType.Thrown] = true; break;
                    case "heavy": dict[FormationFilterType.Heavy] = true; break;
                    case "hightier": dict[FormationFilterType.HighTier] = true; break;
                    case "lowtier": dict[FormationFilterType.LowTier] = true; break;
                }
            }
            return dict;
        }
    }
}