using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public class AlignmentWarStarter : CampaignBehaviorBase
    {
        private bool _warDeclared = false;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_warDeclared", ref _warDeclared);
        }

        private void OnDailyTick()
        {
            if (_warDeclared) return;

            var kingdoms = Kingdom.All.Where(k => !k.IsEliminated).ToList();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    var k1 = kingdoms[i];
                    var k2 = kingdoms[j];

                    if (!FactionManager.IsAtWarAgainstFaction(k1, k2) &&
                        AlignmentsAreEnemies(k1, k2))
                    {
                        FactionManager.DeclareWar(k1, k2);
                        InformationManager.DisplayMessage(new InformationMessage($"⚔️ {k1.Name} has declared war on {k2.Name} due to alignment conflict."));
                    }
                }
            }

            _warDeclared = true;
        }

        private bool AlignmentsAreEnemies(Kingdom k1, Kingdom k2)
        {
            return (k1.Culture.IsGoodCulture() && k2.Culture.IsEvilCulture()) ||
                   (k1.Culture.IsEvilCulture() && k2.Culture.IsGoodCulture());
        }
    }
}
