using System;
using System.Collections.Generic;
using System.Reflection;
using Helpers;
using SandBox;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace RF_PartyVisuals
{
    /// <summary>
    /// Core of RF_PartyVisuals. For each nearby campaign-map party it creates a small cluster of
    /// decorative troop <see cref="AgentVisuals"/> and re-positions them every visual tick relative
    /// to the party's StrategicEntity frame.
    ///
    /// Design choices (all deliberate, all verified against the 1.4.8 binaries):
    ///  - Figures are STANDALONE map-scene entities, NOT children of the party icon. We never touch
    ///    the vanilla icon build (no AddMobileIconComponents patch), so we can't trigger the
    ///    game-wide "folded characters" agent-pose corruption that early icon patches cause.
    ///  - Creation uses the exact recipe vanilla MobilePartyVisual.AddCharacterToPartyIcon uses:
    ///    FaceGen.GetBaseMonsterFromRace + the "_map" action set + Scene(MapScene). That is what
    ///    drives the map idle/walk blend, so we never index a missing clip (canary rule).
    ///  - Distance-culled around the main party and capped per party, so cost stays bounded even
    ///    with "all parties" enabled.
    /// </summary>
    public sealed class PartyVisualsEnhancer
    {
        public static PartyVisualsEnhancer Instance { get; private set; }
        public static bool Ready;

        private const float RebuildInterval = 0.5f;     // seconds between membership re-evaluations
        private const float DefaultSpacing = 0.55f;     // fallback gap between figures in the cluster
        private const float PartyScale = 0.3f;          // vanilla MobilePartyVisual.PartyScale — icon figure size

        private readonly Dictionary<PartyBase, List<Figure>> _tracked =
            new Dictionary<PartyBase, List<Figure>>();
        private readonly List<PartyBase> _scratchRemove = new List<PartyBase>();
        private float _rebuildTimer;
        private Scene _mapScene;

        private sealed class Figure
        {
            public AgentVisuals Visual;
            public Vec3 LocalOffset;

            /// <summary>ForceUpdateBoneFrames ja rodou uma vez para esta figura.
            /// Forcar TODO tick era o site do AccessViolation com esqueleto de farm
            /// animal (debugger do autor, 2026-08-19) — o vanilla so forca apos criar.</summary>
            public bool BonesForced;
        }

        public PartyVisualsEnhancer()
        {
            Instance = this;
        }

        private static Settings SafeSettings()
        {
            try { return Settings.Instance; }
            catch { return null; }
        }

        private Scene MapScene
        {
            get
            {
                if (_mapScene == null && Campaign.Current?.MapSceneWrapper != null)
                {
                    try { _mapScene = ((MapScene)Campaign.Current.MapSceneWrapper).Scene; }
                    catch { _mapScene = null; }
                }
                return _mapScene;
            }
        }

        // ---- entry point (called from the OnVisualTick postfix) ---------------

        public void OnVisualTick(float dt)
        {
            try
            {
                Settings s = SafeSettings();
                if (!Ready || (s != null && !s.Enabled) || Campaign.Current == null)
                {
                    if (_tracked.Count > 0) ClearAll();
                    return;
                }

                var manager = MobilePartyVisualManager.Current;
                if (manager == null || MapScene == null) return;

                _rebuildTimer += dt;
                if (_rebuildTimer >= RebuildInterval)
                {
                    _rebuildTimer = 0f;
                    RebuildMembership(s, manager);
                }

                foreach (var kvp in _tracked)
                {
                    UpdatePartyFigures(kvp.Key, kvp.Value, manager, dt);
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_PartyVisuals] OnVisualTick error: " + e.Message);
            }
        }

        // ---- membership ------------------------------------------------------

        private void RebuildMembership(Settings s, MobilePartyVisualManager manager)
        {
            float maxDist = s?.ViewDistance ?? 45f;
            MobileParty main = MobileParty.MainParty;
            Vec2 center = main != null ? main.GetPosition2D : Vec2.Zero;

            _membershipClock += RebuildInterval;

            // Limpa a quarentena de parties que sumiram (mortas/destruidas).
            if (_firstSeenAt.Count > 256)
            {
                _firstSeenAt.Clear();
            }

            // Drop parties that are no longer eligible.
            _scratchRemove.Clear();
            foreach (var kvp in _tracked)
            {
                if (!IsEligible(kvp.Key, center, maxDist))
                    _scratchRemove.Add(kvp.Key);
            }
            foreach (var party in _scratchRemove)
            {
                DestroySet(_tracked[party]);
                _tracked.Remove(party);
            }

            // Add newly-eligible parties.
            var all = MobileParty.All;
            for (int i = 0; i < all.Count; i++)
            {
                MobileParty mp = all[i];
                PartyBase party = mp?.Party;
                if (party == null || _tracked.ContainsKey(party)) continue;
                if (!IsEligible(party, center, maxDist)) continue;
                if (!IsPastQuarantine(party)) continue;

                var figures = BuildSet(party, s);
                if (figures != null && figures.Count > 0)
                    _tracked[party] = figures;
            }
        }

        // Parties recem-criadas ficam em quarentena por alguns segundos antes de ganharem
        // figuras: criar AgentVisuals no mesmo frame em que a party nasce e o vetor do AV
        // nativo historico visto no spawn de zone parties do RF_ResourceZones
        // (crash 2026-08-18, watchdog sem excecao gerenciada).
        private const float NewPartyQuarantineSeconds = 3f;
        private readonly Dictionary<PartyBase, float> _firstSeenAt = new Dictionary<PartyBase, float>();
        private float _membershipClock;

        private bool IsEligible(PartyBase party, Vec2 center, float maxDist)
        {
            MobileParty mp = party?.MobileParty;
            if (mp == null || !mp.IsActive || !party.IsVisible) return false;
            if (mp.CurrentSettlement != null) return false;
            if (mp.IsCurrentlyAtSea) return false;
            if (party.MemberRoster == null || party.MemberRoster.TotalHealthyCount < 1) return false;

            // O visual vanilla da party precisa existir e ter entidade estrategica pronta —
            // e a prova de que a engine ja terminou de construir a party.
            var visual = MobilePartyVisualManager.Current?.GetPartyVisual(party);
            if (visual?.StrategicEntity == null) return false;

            return mp.GetPosition2D.Distance(center) <= maxDist;
        }

        private bool IsPastQuarantine(PartyBase party)
        {
            if (_firstSeenAt.TryGetValue(party, out float firstSeen))
            {
                return _membershipClock - firstSeen >= NewPartyQuarantineSeconds;
            }
            _firstSeenAt[party] = _membershipClock;
            return false;
        }

        // ---- creation --------------------------------------------------------

        private List<Figure> BuildSet(PartyBase party, Settings s)
        {
            // Living-world parties get a themed set (herd animals, processions, pack trains).
            if ((s?.RestyleLivingWorld ?? true) &&
                TryGetLivingWorldInfo(party, out string lwType, out string herdItem))
            {
                var themed = BuildLivingWorldSet(party, s, lwType, herdItem);
                if (themed != null) return themed;
            }

            int cap = s?.AdditionalUnitCount ?? 6;
            int healthy = party.MemberRoster.TotalHealthyCount;
            int extras = Math.Min(cap, Math.Max(1, (healthy - 1) / 4));
            if (extras <= 0) return null;

            var troops = PickTroops(party, extras);
            if (troops.Count == 0) return null;

            uint color1 = party.MapFaction != null ? party.MapFaction.Color : uint.MaxValue;
            uint color2 = party.MapFaction != null ? party.MapFaction.Color2 : uint.MaxValue;

            float spacing = s?.FigureSpacing ?? DefaultSpacing;
            var list = new List<Figure>(troops.Count);
            for (int i = 0; i < troops.Count; i++)
            {
                AgentVisuals av = CreateTroopVisual(troops[i], color1, color2);
                if (av == null) continue;
                list.Add(new Figure { Visual = av, LocalOffset = ClusterOffset(i, spacing) });
            }
            return list;
        }

        // ---- living-world theming -------------------------------------------

        private const string LwComponentTypeName = "RF_LivingWorld.LivingWorldPartyComponent";
        private static bool _lwResolved;
        private static PropertyInfo _lwPartyTypeProp;
        private static PropertyInfo _lwHerdVariantProp;

        /// <summary>
        /// Reflection probe so this module stays standalone (no build/link dependency on
        /// RF_LivingWorld). Reads PartyType/HerdVariant as their enum names — robust to ordinal
        /// changes. Runs only when a party first enters range, never per frame.
        /// </summary>
        private static bool TryGetLivingWorldInfo(PartyBase party, out string typeName, out string herdItemId)
        {
            typeName = null;
            herdItemId = null;
            object comp = party?.MobileParty?.PartyComponent;
            if (comp == null) return false;

            Type t = comp.GetType();
            if (t.FullName != LwComponentTypeName) return false;

            if (!_lwResolved)
            {
                _lwPartyTypeProp = t.GetProperty("PartyType");
                _lwHerdVariantProp = t.GetProperty("HerdVariant");
                _lwResolved = true;
            }

            typeName = _lwPartyTypeProp?.GetValue(comp)?.ToString();
            string herd = _lwHerdVariantProp?.GetValue(comp)?.ToString();
            if (!string.IsNullOrEmpty(herd)) herdItemId = herd.ToLowerInvariant(); // Sheep -> "sheep"
            return !string.IsNullOrEmpty(typeName);
        }

        private List<Figure> BuildLivingWorldSet(PartyBase party, Settings s, string lwType, string herdItem)
        {
            float spacing = s?.FigureSpacing ?? DefaultSpacing;
            uint c1 = party.MapFaction != null ? party.MapFaction.Color : uint.MaxValue;
            uint c2 = party.MapFaction != null ? party.MapFaction.Color2 : uint.MaxValue;

            int humans;
            int animals;
            string animalItem = null;
            char shape; // 'C' cluster, 'L' column (file), 'S' scatter (loose ring)

            switch (lwType)
            {
                case "Herder":
                    humans = 1; animals = 5; animalItem = herdItem; shape = 'S'; break;
                case "Merchant":
                    humans = 3; animals = 2; animalItem = "mule"; shape = 'L'; break;
                case "Pilgrim":
                case "ReligiousProcession":
                case "DowryProcession":
                    humans = 5; animals = 0; shape = 'L'; break;
                case "Leper":
                    humans = 4; animals = 0; shape = 'C'; break;
                default:
                    return null; // Hunter/Healer/TaxCollector/PrisonerEscort/Wandering -> generic
            }

            var list = new List<Figure>();

            // Human travellers, cycled from the roster (parties are tiny, so reuse is expected).
            var troops = PickTroops(party, humans);
            if (troops.Count == 0)
            {
                CharacterObject leader = PartyBaseHelper.GetVisualPartyLeader(party);
                if (leader != null) troops.Add(leader);
            }
            int slot = 0;
            for (int i = 0; i < humans && troops.Count > 0; i++)
            {
                AgentVisuals av = CreateTroopVisual(troops[i % troops.Count], c1, c2);
                if (av != null) list.Add(new Figure { Visual = av, LocalOffset = ShapeOffset(shape, slot++, spacing) });
            }

            // Animal companions (herd / pack train).
            for (int i = 0; i < animals; i++)
            {
                AgentVisuals av = CreateAnimalVisual(animalItem);
                if (av == null) break; // item missing -> stop trying
                char aShape = shape == 'L' ? 'L' : 'S';
                list.Add(new Figure { Visual = av, LocalOffset = ShapeOffset(aShape, slot++, spacing) });
            }

            return list.Count > 0 ? list : null;
        }

        private AgentVisuals CreateAnimalVisual(string itemId)
        {
            try
            {
                if (string.IsNullOrEmpty(itemId)) return null;
                ItemObject item = Game.Current?.ObjectManager?.GetObject<ItemObject>(itemId);
                if (item?.HorseComponent?.Monster == null) return null;

                Monster monster = item.HorseComponent.Monster;
                Equipment equipment = new Equipment();
                equipment[EquipmentIndex.Horse] = new EquipmentElement(item, null, null, false);

                // Design do autor: todo rebanho usa o set "_map" do animal. Para os farm
                // animals ele e NOSSO (action_sets.xml do modulo) e, desde 2026-08-19, e
                // uma COPIA COMPLETA e autossuficiente do set base vanilla (skeleton +
                // todas as acoes) — a forma fina (so base_set= herdado cross-modulo)
                // carregava sem esqueleto e dava AccessViolation nativo no AgentVisuals
                // (2x no debugger). Fallback para o set base (o das cenas) se o _map
                // faltar; sem nenhum valido, figura pulada COM log.
                string setCode = monster.ActionSetCode;
                MBActionSet actionSet = MBActionSet.GetActionSet(setCode + "_map");
                if (!actionSet.IsValid)
                    actionSet = MBActionSet.GetActionSet(setCode);
                if (!actionSet.IsValid)
                {
                    Debug.Print("[RF_PartyVisuals] Animal '" + itemId + "' sem action set valido ('" + setCode + "[_map]') — figura pulada.");
                    return null;
                }

                AgentVisualsData data = new AgentVisualsData()
                    .Equipment(equipment)
                    .Monster(monster)
                    .Scene(MapScene)
                    .ActionSet(actionSet)
                    .Frame(MatrixFrame.Identity)
                    .Scale(item.ScaleFactor * PartyScale)
                    .UseScaledWeapons(true)
                    .HasClippingPlane(true)
                    .PrepareImmediately(false);

                return AgentVisuals.Create(data, "RF_PartyIcon " + itemId, false, false, false);
            }
            catch (Exception e)
            {
                Debug.Print("[RF_PartyVisuals] CreateAnimalVisual error: " + e.Message);
                return null;
            }
        }

        private static Vec3 ShapeOffset(char shape, int i, float spacing)
        {
            switch (shape)
            {
                case 'L': // single file behind the leader, gentle zig-zag so it reads as a line
                {
                    float x = (i % 2 == 0 ? -1f : 1f) * spacing * 0.18f;
                    float y = -spacing - i * spacing * 1.05f;
                    return new Vec3(x, y, 0f, -1f);
                }
                case 'S': // loose ring around the leader (golden-angle spread)
                {
                    float ang = i * 2.399963f;              // ~137.5 degrees
                    float r = spacing * (1.1f + i * 0.12f);
                    return new Vec3(MathF.Sin(ang) * r, -spacing * 0.5f - MathF.Cos(ang) * r, 0f, -1f);
                }
                default:
                    return ClusterOffset(i, spacing);
            }
        }

        private static List<CharacterObject> PickTroops(PartyBase party, int max)
        {
            var result = new List<CharacterObject>(max);
            CharacterObject leader = PartyBaseHelper.GetVisualPartyLeader(party);
            TroopRoster roster = party.MemberRoster;
            for (int i = 0; i < roster.Count && result.Count < max; i++)
            {
                TroopRosterElement el = roster.GetElementCopyAtIndex(i);
                CharacterObject c = el.Character;
                if (c == null || el.Number <= 0 || c == leader) continue;
                result.Add(c);
            }
            return result;
        }

        private AgentVisuals CreateTroopVisual(CharacterObject character, uint color1, uint color2)
        {
            try
            {
                Monster monster = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(character.Race);
                Equipment equipment = character.FirstBattleEquipment ?? character.Equipment;
                MBActionSet actionSet = MBGlobals.GetActionSetWithSuffix(monster, character.IsFemale, "_map");

                // Matches vanilla MobilePartyVisual.AddCharacterToPartyIcon. UseScaledWeapons(true)
                // is the one that shrinks the sheathed shields/spears with the body — without it
                // the weapons render at full world scale (giant on the map).
                AgentVisualsData data = new AgentVisualsData()
                    .UseMorphAnims(true)
                    .Equipment(equipment)
                    .BodyProperties(character.GetBodyProperties(equipment, -1))
                    .SkeletonType(character.IsFemale ? SkeletonType.Female : SkeletonType.Male)
                    .Scale(PartyScale)
                    .Frame(MatrixFrame.Identity)
                    .ActionSet(actionSet)
                    .Scene(MapScene)
                    .Monster(monster)
                    .PrepareImmediately(false)
                    .RightWieldedItemIndex(-1)
                    .HasClippingPlane(true)
                    .UseScaledWeapons(true)
                    .ClothColor1(color1)
                    .ClothColor2(color2)
                    .CharacterObjectStringId(character.StringId)
                    .AddColorRandomness(!character.IsHero)
                    .Race(character.Race);

                return AgentVisuals.Create(data, "RF_PartyIcon " + character.Name, false, false, false);
            }
            catch (Exception e)
            {
                Debug.Print("[RF_PartyVisuals] CreateTroopVisual error: " + e.Message);
                return null;
            }
        }

        private static Vec3 ClusterOffset(int i, float spacing)
        {
            // Staggered rows BEHIND the leader (leader sits at the icon origin, +Y = forward).
            // The whole cluster is pushed back by ~one spacing so it never crowds the leader/
            // mount/banner, and odd rows are offset half a step for a looser, less gridded look.
            const int perRow = 3;
            int col = i % perRow;
            int row = i / perRow;

            float x = (col - (perRow - 1) / 2f) * spacing;
            if ((row & 1) == 1) x += spacing * 0.5f;

            float rowDepth = spacing * 0.9f;
            float y = -spacing - row * rowDepth;   // first row already one full step behind
            return new Vec3(x, y, 0f, -1f);
        }

        // ---- per-frame positioning ------------------------------------------

        private void UpdatePartyFigures(PartyBase party, List<Figure> figures,
            MobilePartyVisualManager manager, float dt)
        {
            try
            {
                MobilePartyVisual visual = manager.GetPartyVisual(party);
                GameEntity strategic = visual?.StrategicEntity;
                if (strategic == null) return;

                MatrixFrame partyFrame = strategic.GetFrame();
                MobileParty mp = party.MobileParty;
                bool moving = mp != null && mp.Speed > 0.05f;
                float animSpeed = mp != null ? Math.Min(mp.Speed, 8f) : 0f;

                for (int i = 0; i < figures.Count; i++)
                {
                    Figure f = figures[i];
                    GameEntity entity = f.Visual?.GetEntity();
                    if (entity == null) continue;

                    MatrixFrame local = new MatrixFrame(Mat3.Identity, f.LocalOffset);
                    local.rotation.ApplyScaleLocal(f.Visual.GetScale());
                    MatrixFrame world = partyFrame.TransformToParent(local);

                    entity.SetFrame(ref world, true);
                    f.Visual.Tick(null, dt, moving, animSpeed);
                    if (!f.BonesForced)
                    {
                        // Uma vez por figura, como o vanilla — repetir por tick foi o
                        // ponto exato do AV (esqueleto nativo invalido de farm animal).
                        f.BonesForced = true;
                        entity.Skeleton?.ForceUpdateBoneFrames();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("[RF_PartyVisuals] UpdatePartyFigures error: " + e.Message);
            }
        }

        // ---- teardown --------------------------------------------------------

        private static void DestroySet(List<Figure> figures)
        {
            if (figures == null) return;
            for (int i = 0; i < figures.Count; i++)
            {
                DestroyVisual(figures[i]?.Visual);
            }
            figures.Clear();
        }

        private static void DestroyVisual(AgentVisuals visual)
        {
            if (visual == null) return;
            try
            {
                visual.GetVisuals()?.GetEntity()?.Remove(111);
                visual.Reset();
            }
            catch { /* native teardown must never crash the map */ }
        }

        public void ClearAll()
        {
            foreach (var kvp in _tracked)
                DestroySet(kvp.Value);
            _tracked.Clear();
            _rebuildTimer = 0f;
        }
    }
}
