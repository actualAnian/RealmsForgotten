using RFCustomSettlements;
using SandBox.AI;
using SandBox.Objects.Usables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace RealmsForgotten.RFCustomSettlements
{
    static class Helper
    {

        private static readonly float rfInteractionDistance = 2.5f;
        private static bool _canInteract;
        public enum RFUsableObjectType
        {
            Pickable,
            Passage,
            Healing
        }
        private static readonly Dictionary<string, RFUsableObjectType> RFObjectEnum = new()
        {
            {"pickable", RFUsableObjectType.Pickable},
            {"passage", RFUsableObjectType.Passage},
            {"healing", RFUsableObjectType.Healing}
        };
        public static bool IsRFObject(IFocusable focusable)
        {
            if (!Mission.Current.MissionBehaviors.Any(item => item is CustomSettlementMissionLogic)) return false;
            Agent? agent;
            if ((agent = focusable as Agent) != null && IsLootableDeadAgent(agent)) return true;
            UsablePlace? usablePlace;
            if ((usablePlace = focusable as UsablePlace) != null && usablePlace.GameEntity.Name.StartsWith("rf_")) return true;
            //if (gameEntity != null && gameEntity.Name.StartsWith("rf_")) return true;
            return false;
        }

        public static bool IsCloseEnough(Agent mainAgent, IFocusable focusable)
        {
            Agent? agent;
            if ((agent = focusable as Agent) != null && IsLootableDeadAgent(agent)) return true;
            if (((UsablePlace)focusable).GameEntity.GlobalPosition.Distance(Agent.Main.Position) < rfInteractionDistance)
            {
                _canInteract = true;
                return true;
            }
            _canInteract = false;
            return false;
        }
        public static bool IsInRFSettlement()
        {
            Settlement settlement;
            return (settlement = PlayerEncounter.EncounterSettlement) != null && settlement.SettlementComponent != null && settlement.SettlementComponent is RFCustomSettlement;
        }
        internal static int GetGoldAmount(string[] itemData)
        {
            return int.Parse(itemData.Last());
        }

        internal static string GetNameOfGoldObject(int amount)
        {
            if (0 < amount && amount < 20) return "Gold Pile";
            else if (amount < 100) return "Gold Pouch";
            else if (amount < 2000) return "Gold Chest";
            else return "Gold";
        }

        public static float maxPickableDistance = 10f;

        public static bool CanInteract { get => _canInteract; }

        public static Agent? RayCastToCheckForRFInteractableAgent(Agent agent)
        {
            // ALSO PREVENTS THE MOUNTS ENEMIES FROM HUNTABLE HERDS TO BE MOUNTABLE
            if (agent != null && agent.IsActive() && agent.Components.Any(c => c is HuntableHerds.AgentComponents.HerdAgentComponent)) return null;
            if (agent != null) return agent;
            CustomSettlementMissionLogic logic;
            if ((logic = Mission.Current.GetMissionBehavior<CustomSettlementMissionLogic>()) == null) return null;
            if (logic.LootableAgents.IsEmpty()) return null;
            float num = 10f;
            MatrixFrame cf = Mission.Current.GetCameraFrame();

            Vec3 direction = cf.rotation.u * -1;
            Vec3 vec = direction;
            Vec3 position = cf.origin;
            Vec3 position2 = Agent.Main.Position;
            float num2 = new Vec3(position.x, position.y, 0f, -1f).Distance(new Vec3(position2.x, position2.y, 0f, -1f));
            Vec3 vec2 = position * (1f - num2) + (position + direction) * num2;
            _ = Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(vec2, vec2 + vec * num, out float distance, out Vec3 closesPoint, 0.01f, BodyFlags.None);

            float RANGE_X = 1.5f;
            float RANGE_Y = 1.5f;
            float RANGE_Z = 1.5f;
            foreach (KeyValuePair<Agent, Vec3> lootableAgent in logic.LootableAgents)
            {
                Vec3 centerPosition = lootableAgent.Value;
                if (Math.Abs(centerPosition.X - closesPoint.X) < RANGE_X
                    && Math.Abs(centerPosition.Y - closesPoint.Y) < RANGE_Y
                    && Math.Abs(centerPosition.Z - closesPoint.Z) < RANGE_Z)
                    return lootableAgent.Key;
            }
            return null;
        }
        public static bool IsLootableDeadAgent(Agent agent)
        {
            return !agent.IsActive() && agent.Components.Any(c => c is LootableAgentComponent);
        }

        public static void SetVMLook(AgentInteractionInterfaceVM vm)
        {
            GameKey key = HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(13);
            string button = $@"<img src=""General\InputKeys\{key.ToString().ToLower()}"" extend=""24"">";
            vm.PrimaryInteractionMessage = button + "Loot";
            vm.IsActive = true;
        }
        public static RFUsableObjectType? ChooseObjectType(string objectName)
        {
            string objectType = objectName.Split('_')[1];
            if (RFObjectEnum.ContainsKey(objectType)) { return RFObjectEnum[objectType]; }
            else return null;
        }
        public static string GetRFPickableObjectName(string[] data)
        {
            StringBuilder itemIdBuilder = new();
            foreach (string str in data.Skip(2).Take(data.Length - 3))
                itemIdBuilder.Append(str + "_");
            itemIdBuilder.Remove(itemIdBuilder.Length - 1, 1);
            return itemIdBuilder.ToString();
        }

        internal static string? GetCharacterIdfromEntityName(string name)
        {
            if (!name.Contains("rf_Npc")) return null;
            return name.Remove(0, 7);
        }
    }
}
