using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal static class RFArmyCommandActions
{
    public static void TransferInfluence(Clan senderClan, Clan receiverClan, int amount)
    {
        if (senderClan == null || receiverClan == null || amount <= 0)
        {
            return;
        }

        ChangeClanInfluenceAction.Apply(senderClan, -amount);
        ChangeClanInfluenceAction.Apply(receiverClan, amount);
    }
}
