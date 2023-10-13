using RealmsForgotten.RFCustomSettlements;
using RFCustomSettlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace RealmsForgotten.RFCustomSettlements
{
    public interface ISettlementStateHandler
    {
        void OnSettlementStartConsequence(MenuCallbackArgs args);
        bool OnSettlementStartCondition(MenuCallbackArgs args);
        void OnSettlementStartOnInit(MenuCallbackArgs args);
        bool OnSettlementLeaveCondition(MenuCallbackArgs args);
        bool OnSettlementWaitStartOnCondition(MenuCallbackArgs args);
        bool OnSettlementWaitEndCondition(MenuCallbackArgs args);
        void OnSettlementWaitEndConsequence(MenuCallbackArgs args);
        void OnWaitMenuTillEnterTick(MenuCallbackArgs args, CampaignTime dt);
        void OnSettlementWaitInit(MenuCallbackArgs args);
        public void InitHandler(CustomSettlementBuildData buildData);
        public bool IsInitialized();
    }
}
