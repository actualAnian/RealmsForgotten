using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class PendingDuelMissionBehavior : CampaignBehaviorBase
    {
        private Hero _pendingDuelTarget;

        public override void RegisterEvents()
        {
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnGameTick);
        }

        public override void SyncData(IDataStore dataStore) { }

        public void QueueDuel(Hero target)
        {
            if (target != null && target.CharacterObject != null)
            {
                _pendingDuelTarget = target;
            }
        }

        private void OnGameTick(float dt)
        {
            if (_pendingDuelTarget == null)
                return;

            Hero target = _pendingDuelTarget;
            _pendingDuelTarget = null;

            if (target == null || target.CharacterObject == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Could not start duel: opponent data missing."));
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage($"⚔ Duel with {target.Name} is starting..."));

            MissionState.OpenNew(
                "DuelMission",
                new MissionInitializerRecord("arena_duel"),
                mission => new MissionBehavior[]
                {
                    new MissionOptionsComponent(),
                    new SimpleDuelMissionLogic(target)
                }
            );
        }
    }
}