using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;

namespace RealmsForgotten.RFCustomHorses
{
    public class RFHorseSpawningCampaignBehavior : CampaignBehaviorBase
    {
        public static readonly RFHorseSpawningCampaignBehavior Instance = new RFHorseSpawningCampaignBehavior();
        private static CustomHorseLibrary HorseLibrary = null;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, RFHorseSpawningCampaignBehavior.OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // DO NOTHING
        }

        private static void OnDailyTick()
        {
            RFHorseSpawningCampaignBehavior.SpawnHorses();
        }

        private static void SpawnHorses()
        {
            if (HorseLibrary == null)
            {
                HorseLibrary = CustomHorseLibrary.LoadFromFile(Path.Combine(ModuleHelper.GetModuleFullPath(Globals.realmsForgottenAssembly.GetName().Name), CustomHorseLibrary.LIBRARY_FILENAME));
            }
            foreach (Town town in Town.AllTowns.ToList())
            {
                List<CustomHorseLibrary.Horse> cultureHorses = HorseLibrary.Horses.Where(horse => horse.Culture.ToLower() == town.Culture.StringId.ToLower()).ToList();
                foreach (CustomHorseLibrary.Horse horse in cultureHorses)
                {
                    if (MBRandom.RandomFloat < horse.SpawnChance)
                    {
                        ItemObject horseItem = Items.All.FirstOrDefault(item => item.StringId == horse.Id);
                        if (horseItem != null)
                        {
                            town.Owner.ItemRoster.AddToCounts(horseItem, 1);
                        }
                    }
                }
            }
        }
    }
}
