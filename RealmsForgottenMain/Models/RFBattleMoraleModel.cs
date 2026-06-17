using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    internal class RFBattleMoraleModel : BattleMoraleModel
    {
        BattleMoraleModel _baseModel;

        public RFBattleMoraleModel(BattleMoraleModel baseModel)
        {
            _baseModel = baseModel;
        }

        public override float CalculateCasualtiesFactor(BattleSideEnum battleSide)
        {
            return _baseModel.CalculateCasualtiesFactor(battleSide);
        }

        public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
        {
            return _baseModel.CalculateMaxMoraleChangeDueToAgentIncapacitated(affectedAgent, affectedAgentState, affectorAgent, killingBlow);
        }

        public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentPanicked(Agent agent)
        {
            return _baseModel.CalculateMaxMoraleChangeDueToAgentPanicked(agent);
        }


        public override float CalculateMoraleChangeToCharacter(Agent agent, float maxMoraleChange)
        {
            return _baseModel.CalculateMoraleChangeToCharacter(agent, maxMoraleChange);
        }

        public override bool CanPanicDueToMorale(Agent agent)
        {
            return _baseModel.CanPanicDueToMorale(agent);
        }

        public override float GetAverageMorale(Formation formation)
        {
            return _baseModel.GetAverageMorale(formation);
        }

        public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
        {
            return _baseModel.GetEffectiveInitialMorale(agent, baseMorale);
        }

        public override float CalculateMoraleChangeOnShipSunk(IShipOrigin shipOrigin) => _baseModel.CalculateMoraleChangeOnShipSunk(shipOrigin);

        public override float CalculateMoraleOnShipsConnected(Agent agent, IShipOrigin ownerShip, IShipOrigin targetShip) => _baseModel.CalculateMoraleOnShipsConnected(agent, ownerShip, targetShip);

        public override float CalculateMoraleOnRamming(Agent agent, IShipOrigin rammingShip, IShipOrigin rammedShip) => _baseModel.CalculateMoraleOnRamming(agent, rammingShip, rammedShip);
    }
}
