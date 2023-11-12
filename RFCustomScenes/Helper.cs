using SandBox.Objects.Usables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

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
            UsablePlace? usablePlace;
            if ((usablePlace = focusable as UsablePlace) != null && usablePlace.GameEntity.Name.StartsWith("rf_")) return true;
            //if (gameEntity != null && gameEntity.Name.StartsWith("rf_")) return true;
            return false;
        }

        public static bool IsCloseEnough(Agent mainAgent, IFocusable focusable)
        {
            if(((UsablePlace)focusable).GameEntity.GlobalPosition.Distance(Agent.Main.Position) < rfInteractionDistance)
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

        public static bool CanInteract { get => _canInteract;}

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
