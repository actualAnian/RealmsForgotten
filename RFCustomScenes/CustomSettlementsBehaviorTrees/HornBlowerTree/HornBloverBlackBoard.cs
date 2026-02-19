using TaleWorlds.Core;

namespace RFCustomSettlements.CustomSettlementsBehaviorTrees.HornBlowerTree
{
    public class HornBlowerBlackBoard
    {
        public HornBlowerBlackBoard(AgentFlag savedFlags)
        {
            SavedFlags = savedFlags;
        }

        public AgentFlag SavedFlags { get; set; }
    }
}