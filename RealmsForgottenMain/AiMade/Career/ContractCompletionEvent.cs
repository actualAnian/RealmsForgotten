using System;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class ContractCompletionEvent : EventArgs
    {
        public Hero Hero { get; }
        public ContractCompletionEvent(Hero hero)
        {
            Hero = hero;
        }
    }
}
