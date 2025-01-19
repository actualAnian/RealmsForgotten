using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class BanditHideoutClearedBehavior : CampaignBehaviorBase
    {
        private int _hideoutsCleared;

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsHideoutBattle && mapEvent.IsPlayerMapEvent && mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                _hideoutsCleared++;
                AwardChivalryPoints();
            }
        }

        private void AwardChivalryPoints()
        {
            int pointsToAward = 0;

            if (_hideoutsCleared >= 30)
            {
                pointsToAward = 20;
            }
            else if (_hideoutsCleared >= 20)
            {
                pointsToAward = 15;
            }
            else if (_hideoutsCleared >= 10)
            {
                pointsToAward = 10;
            }
            else if (_hideoutsCleared >= 5)
            {
                pointsToAward = 5;
            }

            if (pointsToAward > 0)
            {
                var careerProgressionBehavior = Campaign.Current.GetCampaignBehavior<CareerProgressionBehavior>();
                if (careerProgressionBehavior.AddChivalryPoints(pointsToAward, CareerType.Knight))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"You have cleared {_hideoutsCleared} bandit hideouts and gained {pointsToAward} chivalry points!"));
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_hideoutsCleared", ref _hideoutsCleared);
        }
    }
}
