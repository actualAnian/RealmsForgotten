using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldCampaignBehavior : CampaignBehaviorBase
    {
        private const int AmbientPopulation = 24;
        private const int HardPopulationCap = 28;
        private const int ContextualPopulationCap = 4;
        private const int MaximumCompletedRoutes = 6;
        private const float MaximumPartyAgeDays = 60f;

        private static LivingWorldCampaignBehavior? _instance;
        private List<LivingWorldRumorReport> _rumors = new();
        private List<LivingWorldRumorSourceState> _rumorSources = new();
        private List<LivingWorldPartyDefinition> _definitions = new();
        private List<LivingWorldContextCooldownState> _contextCooldowns = new();
        private List<LivingWorldPendingEvent> _pendingEvents = new();
        private readonly HashSet<string> _disabledDefinitions = new(StringComparer.OrdinalIgnoreCase);
        private int _spawnCounter;

        public LivingWorldCampaignBehavior()
        {
            _instance = this;
        }

        public static LivingWorldCampaignBehavior? Instance => _instance;
        public LivingWorldRumorService Rumors => new(_rumors, _rumorSources);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnBeforeHeroesMarried);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_rfLivingWorldRumors", ref _rumors);
            dataStore.SyncData("_rfLivingWorldRumorSources", ref _rumorSources);
            dataStore.SyncData("_rfLivingWorldSpawnCounter", ref _spawnCounter);
            dataStore.SyncData("_rfLivingWorldContextCooldowns", ref _contextCooldowns);
            dataStore.SyncData("_rfLivingWorldPendingEvents", ref _pendingEvents);
            _rumors ??= new List<LivingWorldRumorReport>();
            _rumorSources ??= new List<LivingWorldRumorSourceState>();
            _contextCooldowns ??= new List<LivingWorldContextCooldownState>();
            _pendingEvents ??= new List<LivingWorldPendingEvent>();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            EnsureDefinitions();
            Rumors.RemoveExpired();
        }

        private void OnDailyTick()
        {
            EnsureDefinitions();
            Rumors.RemoveExpired();
        }

        private void OnHourlyTick()
        {
            EnsureDefinitions();
            foreach (MobileParty party in ActiveParties().ToList())
            {
                if (party.Party.IsStarving)
                {
                    party.ItemRoster.AddToCounts(DefaultItems.Grain, 2);
                }

                LivingWorldPartyComponent component = (LivingWorldPartyComponent)party.PartyComponent;
                if (party.MapEvent == null && ShouldExpireInTransit(component) && PlayerEncounter.Current == null)
                {
                    DestroyPartyAction.Apply(null, party);
                    continue;
                }

                if (party.MapEvent != null)
                {
                    continue;
                }

                bool arrived = party.CurrentSettlement == component.Destination
                    || (party.CurrentSettlement == null && component.Destination != null
                        && party.Position.Distance(component.Destination.GatePosition) <= 2f);
                if (!arrived)
                {
                    continue;
                }

                component.CompleteRoute();
                if (ShouldRetire(component) && PlayerEncounter.Current == null)
                {
                    DestroyPartyAction.Apply(null, party);
                    continue;
                }

                Settlement? next = ChooseDestination(component.Destination, party.Position);
                if (next == null)
                {
                    continue;
                }

                component.SetDestination(next);
                party.SetMoveGoToPoint(next.GatePosition, MobileParty.NavigationType.Default);
            }

            ReconcileAmbientPopulation();
            TrySpawnContextualParty();
        }

        private void OnBeforeHeroesMarried(Hero first, Hero second, bool isForced)
        {
            Settlement? origin = first?.Clan?.HomeSettlement;
            Settlement? destination = second?.Clan?.HomeSettlement;
            if (origin == null || destination == null || origin == destination || origin.IsUnderSiege || destination.IsUnderSiege)
            {
                return;
            }

            if (!_pendingEvents.Any(pending => pending.DefinitionId == "dowry_processions"
                && pending.Origin == origin && pending.Destination == destination))
            {
                _pendingEvents.Add(new LivingWorldPendingEvent
                {
                    DefinitionId = "dowry_processions",
                    Origin = origin,
                    Destination = destination,
                    CreatedAt = CampaignTime.Now
                });
            }
        }

        public string Status()
        {
            EnsureDefinitions();
            List<MobileParty> parties = ActiveParties().ToList();
            List<string> lines = new()
            {
                $"RF Living World: {parties.Count}/{HardPopulationCap} parties; ambient {parties.Count(party => party.PartyComponent is LivingWorldPartyComponent component && IsAmbientComponent(component))}/{AmbientPopulation}; contextual {ActiveContextualParties().Count()}/{ContextualPopulationCap}; {Rumors.ActiveCount} active rumors."
            };

            foreach (LivingWorldPartyDefinition definition in _definitions.Where(definition => definition.IsAmbient).OrderBy(definition => definition.Id, StringComparer.Ordinal))
            {
                int current = parties.Count(party => party.PartyComponent is LivingWorldPartyComponent component && IsAmbientComponent(component)
                    && string.Equals(component.DefinitionId, definition.Id, StringComparison.OrdinalIgnoreCase));
                lines.Add($"ambient {definition.Id}: {current}/{definition.AmbientBaseline} (deficit {Math.Max(0, definition.AmbientBaseline - current)})");
            }

            foreach (LivingWorldContextCooldownState cooldown in _contextCooldowns.OrderBy(item => item.DefinitionId, StringComparer.Ordinal))
            {
                float remaining = Math.Max(0f, cooldown.CooldownDays - (float)(CampaignTime.Now - cooldown.LastTriggeredAt).ToDays);
                lines.Add($"cooldown {cooldown.DefinitionId}: {remaining:F1} day(s) remaining");
            }

            foreach (MobileParty party in parties.OrderBy(item => item.StringId, StringComparer.Ordinal))
            {
                if (party.PartyComponent is not LivingWorldPartyComponent component)
                {
                    continue;
                }

                Settlement? nearest = Settlement.All.Where(settlement => settlement != null && (settlement.IsTown || settlement.IsVillage))
                    .OrderBy(settlement => party.Position.Distance(settlement.GatePosition)).FirstOrDefault();
                float nearestDistance = nearest == null ? 0f : party.Position.Distance(nearest.GatePosition);
                float destinationDistance = component.Destination == null ? 0f : party.Position.Distance(component.Destination.GatePosition);
                string lifecycle = IsContextualComponent(component) ? $"contextual: {ContextReason(component)}" : component.IsManualSpawn ? "manual" : "ambient";
                lines.Add($"{party.StringId} | {party.Name} | {component.DefinitionId} | {lifecycle} | "
                    + $"age {(CampaignTime.Now - component.CreatedAt).ToDays:F1}d | routes {component.CompletedRoutes}/{MaximumCompletedRoutes} | ({party.Position.X:F1}, {party.Position.Y:F1}) | "
                    + $"nearest {(nearest == null ? "none" : nearest.Name.ToString())} ({nearestDistance:F1}) | "
                    + $"{(component.Origin == null ? "none" : component.Origin.Name.ToString())} -> "
                    + $"{(component.Destination == null ? "none" : component.Destination.Name.ToString())} ({destinationDistance:F1})");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public bool TrySpawnFromConsole(LivingWorldPartyType type, LivingWorldHerdVariant variant, out MobileParty? party, out string result)
        {
            EnsureDefinitions();
            int activeCount = ActiveParties().Count();
            int reservedAmbientVacancies = _definitions.Where(definition => definition.IsAmbient).Sum(definition =>
                Math.Max(0, definition.AmbientBaseline - ActiveParties().Count(activeParty =>
                    activeParty.PartyComponent is LivingWorldPartyComponent component && IsAmbientComponent(component)
                    && string.Equals(component.DefinitionId, definition.Id, StringComparison.OrdinalIgnoreCase))));
            if (activeCount + reservedAmbientVacancies >= HardPopulationCap)
            {
                party = null;
                result = $"Reserved ambient capacity prevents manual spawning ({activeCount} active, {reservedAmbientVacancies} reserved, cap {HardPopulationCap}).";
                return false;
            }

            LivingWorldPartyDefinition? definition = _definitions.FirstOrDefault(item =>
                !_disabledDefinitions.Contains(item.Id) && item.Type == type && (type != LivingWorldPartyType.Herder || item.HerdVariant == variant));
            if (definition == null)
            {
                party = null;
                result = $"No enabled XML definition for {type} {variant}.";
                return false;
            }

            bool success = TrySpawn(definition, MobileParty.MainParty?.Position, out party, null, null, null, true, null);
            result = success ? $"Spawned {party?.Name}." : $"Could not spawn definition '{definition.Id}'. Check rgl_log for the reason.";
            return success;
        }

        private void EnsureDefinitions()
        {
            if (_definitions.Count == 0)
            {
                _definitions = LivingWorldPartyManifest.Load();
            }
        }

        private LivingWorldPartyDefinition? FindDefinition(string id) => _definitions.FirstOrDefault(definition =>
            string.Equals(definition.Id, id, StringComparison.OrdinalIgnoreCase));

        private void ReconcileAmbientPopulation()
        {
            foreach (LivingWorldPartyDefinition definition in _definitions.Where(definition => definition.IsAmbient
                && !_disabledDefinitions.Contains(definition.Id) && definition.AmbientBaseline > 0).OrderBy(definition => definition.Id, StringComparer.Ordinal))
            {
                int current = ActiveParties().Count(party => party.PartyComponent is LivingWorldPartyComponent component
                    && IsAmbientComponent(component) && string.Equals(component.DefinitionId, definition.Id, StringComparison.OrdinalIgnoreCase));
                for (int i = current; i < definition.AmbientBaseline && ActiveParties().Count() < HardPopulationCap; i++)
                {
                    if (!TrySpawn(definition, null, out _, null, null, null, false, null))
                    {
                        break;
                    }
                }
            }
        }

        private void TrySpawnContextualParty()
        {
            if (ActiveParties().Count() >= HardPopulationCap || ActiveContextualParties().Count() >= ContextualPopulationCap)
            {
                return;
            }

            _ = TrySpawnQueuedDowry() || TrySpawnLepers() || TrySpawnReligiousProcession() || TrySpawnTaxCollector() || TrySpawnPrisonerEscort();
        }

        private IEnumerable<MobileParty> ActiveContextualParties() => ActiveParties().Where(party =>
            party.PartyComponent is LivingWorldPartyComponent component && IsContextualComponent(component));

        private bool TrySpawnQueuedDowry()
        {
            _pendingEvents.RemoveAll(item => item == null || item.Origin == null || item.Destination == null
                || (CampaignTime.Now - item.CreatedAt).ToDays > 7d);
            LivingWorldPendingEvent? pending = _pendingEvents.FirstOrDefault(item => item.DefinitionId == "dowry_processions"
                && item.Origin != null && item.Destination != null && !item.Origin.IsUnderSiege && !item.Destination.IsUnderSiege);
            if (pending == null)
            {
                return false;
            }

            Settlement? origin = pending.Origin;
            Settlement? destination = pending.Destination;
            if (origin == null || destination == null)
            {
                return false;
            }

            bool spawned = TrySpawnContextual("dowry_processions", origin, destination, "marriage between local families", 0f, null);
            if (spawned)
            {
                _pendingEvents.Remove(pending);
            }
            return spawned;
        }

        private bool TrySpawnLepers()
        {
            Settlement? origin = Settlement.All.FirstOrDefault(settlement => settlement != null && !settlement.IsUnderSiege
                && ((settlement.IsTown && settlement.Town != null && (settlement.Town.FoodStocks < 25f || settlement.Town.Prosperity < 400f))
                    || (settlement.IsVillage && settlement.Village != null && settlement.Village.Hearth < 200f)));
            return origin != null && TrySpawnContextual("lepers", origin, ChooseDestination(origin, origin.GatePosition), "hardship near the settlement", 21f, null);
        }

        private bool TrySpawnReligiousProcession()
        {
            if (MBRandom.RandomFloat > 0.02f)
            {
                return false;
            }

            List<Settlement> origins = Settlement.All.Where(settlement => settlement != null && !settlement.IsUnderSiege
                && (settlement.IsTown || settlement.IsVillage)).ToList();
            if (origins.Count == 0)
            {
                return false;
            }

            Settlement origin = origins[MBRandom.RandomInt(origins.Count)];
            Settlement? destination = ChooseDestination(origin, origin.GatePosition, true);
            return destination != null && TrySpawnContextual("religious_processions", origin, destination, "pilgrimage between settlements of the same culture", 14f, null);
        }

        private bool TrySpawnTaxCollector()
        {
            Settlement? origin = Settlement.All.FirstOrDefault(settlement => settlement != null && settlement.IsVillage && !settlement.IsUnderSiege
                && settlement.Village?.Bound != null && !settlement.Village.Bound.IsUnderSiege);
            return origin != null && TrySpawnContextual("tax_collectors", origin, origin.Village!.Bound, "weekly collection from a bound village", 7f, null);
        }

        private bool TrySpawnPrisonerEscort()
        {
            Settlement? origin = Settlement.All.FirstOrDefault(settlement => settlement != null && (settlement.IsTown || settlement.IsCastle)
                && !settlement.IsUnderSiege && settlement.Party?.PrisonRoster?.TotalManCount > 0);
            if (origin == null)
            {
                return false;
            }

            Settlement? destination = Settlement.All.Where(settlement => settlement != null && settlement != origin && !settlement.IsUnderSiege
                && (settlement.IsTown || settlement.IsCastle) && settlement.MapFaction == origin.MapFaction)
                .OrderBy(settlement => settlement.GatePosition.Distance(origin.GatePosition)).FirstOrDefault();
            TroopRoster? copy = destination == null ? null : CopyRepresentativePrisoners(origin.Party.PrisonRoster);
            return destination != null && copy != null && TrySpawnContextual("prisoner_escorts", origin, destination,
                "transfer of prisoners between allied settlements", 7f, copy);
        }

        private bool TrySpawnContextual(string definitionId, Settlement origin, Settlement? destination, string reason, float cooldownDays, TroopRoster? copiedPrisoners)
        {
            LivingWorldPartyDefinition? definition = FindDefinition(definitionId);
            if (definition == null || definition.IsAmbient || destination == null || !IsCooldownReady(definitionId)
                || ActiveContextualParties().Count(party => ((LivingWorldPartyComponent)party.PartyComponent).DefinitionId == definitionId) >= definition.MaxInstances)
            {
                return false;
            }

            if (!TrySpawn(definition, origin.GatePosition, out _, origin, destination, reason, false, copiedPrisoners))
            {
                return false;
            }

            if (cooldownDays > 0f)
            {
                SetCooldown(definitionId, cooldownDays);
            }
            return true;
        }

        private bool IsCooldownReady(string definitionId)
        {
            LivingWorldContextCooldownState? state = _contextCooldowns.FirstOrDefault(item => item.DefinitionId == definitionId);
            return state == null || (CampaignTime.Now - state.LastTriggeredAt).ToDays >= state.CooldownDays;
        }

        private void SetCooldown(string definitionId, float days)
        {
            LivingWorldContextCooldownState? state = _contextCooldowns.FirstOrDefault(item => item.DefinitionId == definitionId);
            if (state == null)
            {
                state = new LivingWorldContextCooldownState { DefinitionId = definitionId };
                _contextCooldowns.Add(state);
            }
            state.LastTriggeredAt = CampaignTime.Now;
            state.CooldownDays = days;
        }

        private static TroopRoster? CopyRepresentativePrisoners(TroopRoster source)
        {
            TroopRoster copy = TroopRoster.CreateDummyTroopRoster();
            foreach (TroopRosterElement element in source.GetTroopRoster())
            {
                if (element.Character != null && !element.Character.IsHero && element.Number > 0)
                {
                    copy.AddToCounts(element.Character, Math.Min(3, element.Number));
                }
            }
            return copy.TotalManCount > 0 ? copy : null;
        }

        private bool TrySpawn(LivingWorldPartyDefinition definition, CampaignVec2? overridePosition, out MobileParty? party,
            Settlement? requestedOrigin, Settlement? requestedDestination, string? contextualReason, bool isManualSpawn, TroopRoster? copiedPrisoners)
        {
            party = null;
            if (definition.Type == LivingWorldPartyType.Herder && !TryResolveHerdVisual(definition.HerdVariant))
            {
                _disabledDefinitions.Add(definition.Id);
                Debug.Print($"[RF_LivingWorld] Definition '{definition.Id}' disabled: native item for {definition.HerdVariant} was not found.");
                return false;
            }

            Settlement? origin = requestedOrigin ?? ChooseOrigin();
            if (origin == null)
            {
                return false;
            }

            CampaignVec2 spawnPosition = overridePosition ?? origin.GatePosition;
            Settlement? destination = requestedDestination ?? ChooseDestination(origin, spawnPosition);
            CharacterObject? villager = origin.Culture?.Villager ?? origin.Culture?.BasicTroop
                ?? CharacterObject.All.FirstOrDefault(character => character.Occupation == Occupation.Villager);
            if (destination == null || villager == null)
            {
                return false;
            }

            CharacterObject basicTroop = origin.Culture?.BasicTroop ?? villager;
            int size = MBRandom.RandomInt(definition.MinimumSize, definition.MaximumSize + 1);
            TroopRoster members = TroopRoster.CreateDummyTroopRoster();
            TroopRoster prisoners = copiedPrisoners ?? TroopRoster.CreateDummyTroopRoster();
            AddMembers(definition.Type, members, villager, basicTroop, size);
            if (copiedPrisoners == null && definition.Type == LivingWorldPartyType.PrisonerEscort && TryGetCharacter("looter", out CharacterObject? looter))
            {
                prisoners.AddToCounts(looter, MBRandom.RandomInt(1, Math.Max(2, size / 2 + 1)));
            }

            _spawnCounter++;
            LivingWorldPartyComponent component = new(definition.Id, definition.Type, definition.HerdVariant, origin, destination,
                definition.RumorReliability, contextualReason, isManualSpawn);
            party = MobileParty.CreateParty($"rf_living_{definition.Id}_{_spawnCounter}", component);
            party.InitializeMobilePartyAroundPosition(members, prisoners, spawnPosition, 1f);
            party.Aggressiveness = 0f;
            party.ItemRoster.AddToCounts(DefaultItems.Grain, Math.Max(4, size));
            AddCargo(party, definition);
            party.SetMoveGoToPoint(destination.GatePosition, MobileParty.NavigationType.Default);
            party.Party.SetVisualAsDirty();
            return true;
        }

        private static void AddCargo(MobileParty party, LivingWorldPartyDefinition definition)
        {
            if (definition.Type == LivingWorldPartyType.Herder)
            {
                if (TryGetItem(LivingWorldPartyComponent.HerdItemId(definition.HerdVariant), out ItemObject? animal))
                {
                    party.ItemRoster.AddToCounts(animal, MBRandom.RandomInt(8, 18));
                }
                return;
            }

            switch (definition.Type)
            {
                case LivingWorldPartyType.Merchant:
                    AddIfFound(party, "fish", MBRandom.RandomInt(4, 10));
                    AddIfFound(party, "linen", MBRandom.RandomInt(2, 7));
                    AddIfFound(party, "hardwood", MBRandom.RandomInt(4, 12));
                    break;
                case LivingWorldPartyType.Hunter:
                    AddIfFound(party, "meat", MBRandom.RandomInt(3, 8));
                    AddIfFound(party, "fur", MBRandom.RandomInt(1, 4));
                    break;
                case LivingWorldPartyType.Healer:
                    AddIfFound(party, "medicinal_plants", MBRandom.RandomInt(2, 6));
                    break;
                case LivingWorldPartyType.DowryProcession:
                    AddIfFound(party, "linen", MBRandom.RandomInt(2, 6));
                    AddIfFound(party, "velvet", MBRandom.RandomInt(1, 3));
                    AddIfFound(party, "jewelry", MBRandom.RandomInt(1, 2));
                    AddIfFound(party, "spice", MBRandom.RandomInt(1, 3));
                    break;
            }
        }

        private static void AddMembers(LivingWorldPartyType type, TroopRoster members, CharacterObject villager, CharacterObject basicTroop, int size)
        {
            switch (type)
            {
                case LivingWorldPartyType.Hunter:
                    members.AddToCounts(basicTroop, size);
                    break;
                case LivingWorldPartyType.ReligiousProcession:
                case LivingWorldPartyType.DowryProcession:
                    AddMixedMembers(members, villager, basicTroop, size, Math.Max(1, size / 4));
                    break;
                case LivingWorldPartyType.TaxCollector:
                case LivingWorldPartyType.PrisonerEscort:
                    AddMixedMembers(members, villager, basicTroop, size, Math.Max(1, size - 2));
                    break;
                default:
                    members.AddToCounts(villager, size);
                    break;
            }
        }

        private static void AddMixedMembers(TroopRoster members, CharacterObject villager, CharacterObject basicTroop, int size, int basicTroopCount)
        {
            int guards = Math.Min(size, basicTroopCount);
            members.AddToCounts(basicTroop, guards);
            if (size > guards)
            {
                members.AddToCounts(villager, size - guards);
            }
        }

        private static void AddIfFound(MobileParty party, string itemId, int count)
        {
            if (TryGetItem(itemId, out ItemObject? item))
            {
                party.ItemRoster.AddToCounts(item, count);
            }
        }

        private static bool TryGetItem(string itemId, out ItemObject? item)
        {
            item = string.IsNullOrWhiteSpace(itemId) ? null : MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            return item != null;
        }

        private static bool TryGetCharacter(string characterId, out CharacterObject? character)
        {
            character = string.IsNullOrWhiteSpace(characterId) ? null : MBObjectManager.Instance?.GetObject<CharacterObject>(characterId);
            return character != null;
        }

        private static bool TryResolveHerdVisual(LivingWorldHerdVariant variant)
        {
            return TryGetItem(LivingWorldPartyComponent.HerdItemId(variant), out ItemObject? animal)
                && !string.IsNullOrEmpty(animal?.HorseComponent?.Monster?.StringId);
        }

        private bool IsAmbientComponent(LivingWorldPartyComponent component)
        {
            if (component.IsManualSpawn)
            {
                return false;
            }

            LivingWorldPartyDefinition? definition = FindDefinition(component.DefinitionId);
            return definition == null ? component.IsAmbient : definition.IsAmbient && !component.IsContextual;
        }

        private bool IsContextualComponent(LivingWorldPartyComponent component)
        {
            if (component.IsManualSpawn)
            {
                return false;
            }

            LivingWorldPartyDefinition? definition = FindDefinition(component.DefinitionId);
            return definition == null ? component.IsContextual : !definition.IsAmbient || component.IsContextual;
        }

        private string ContextReason(LivingWorldPartyComponent component)
        {
            if (!string.IsNullOrWhiteSpace(component.ContextualReason))
            {
                return component.ContextualReason;
            }

            return "legacy contextual party";
        }

        private bool ShouldExpireInTransit(LivingWorldPartyComponent component)
        {
            return IsContextualComponent(component) && (CampaignTime.Now - component.CreatedAt).ToDays >= ContextualLifetimeDays(component.PartyType);
        }

        private static float ContextualLifetimeDays(LivingWorldPartyType type) => type switch
        {
            LivingWorldPartyType.Leper => 12f,
            LivingWorldPartyType.ReligiousProcession => 10f,
            LivingWorldPartyType.TaxCollector => 4f,
            LivingWorldPartyType.PrisonerEscort => 4f,
            LivingWorldPartyType.DowryProcession => 4f,
            _ => 6f
        };

        private bool ShouldRetire(LivingWorldPartyComponent component)
        {
            return IsContextualComponent(component) || component.CompletedRoutes >= MaximumCompletedRoutes
                || (CampaignTime.Now - component.CreatedAt).ToDays >= MaximumPartyAgeDays;
        }

        private static Settlement? ChooseOrigin()
        {
            List<Settlement> settlements = Settlement.All.Where(settlement => settlement != null && (settlement.IsTown || settlement.IsVillage)
                && !settlement.IsUnderSiege).ToList();
            return settlements.Count == 0 ? null : settlements[MBRandom.RandomInt(settlements.Count)];
        }

        private static Settlement? ChooseDestination(Settlement? origin, CampaignVec2 from, bool sameCulture = false)
        {
            List<Settlement> preferred = Settlement.All.Where(settlement => settlement != null && settlement != origin
                && (settlement.IsTown || settlement.IsVillage) && !settlement.IsUnderSiege
                && (!sameCulture || settlement.Culture == origin?.Culture) && settlement.GatePosition.Distance(from) >= 12f
                && settlement.GatePosition.Distance(from) <= 80f).ToList();
            if (preferred.Count > 0)
            {
                return preferred[MBRandom.RandomInt(preferred.Count)];
            }

            return Settlement.All.FirstOrDefault(settlement => settlement != null && settlement != origin
                && (settlement.IsTown || settlement.IsVillage) && !settlement.IsUnderSiege
                && (!sameCulture || settlement.Culture == origin?.Culture));
        }

        private static IEnumerable<MobileParty> ActiveParties() => MobileParty.All.Where(party =>
            party != null && party.IsActive && party.PartyComponent is LivingWorldPartyComponent);
    }
}
