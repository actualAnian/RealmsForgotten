using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    public class RacialMixingBehavior : CampaignBehaviorBase
    {
        public static RacialMixingBehavior Instance;
        public Dictionary<CharacterObject, CharacterRacialMix> AllCharacterRacialMixes = new Dictionary<CharacterObject, CharacterRacialMix>();

        public RacialMixingBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            // Register event on game load to create race mix data for all heroes.
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, () =>
            {
                var heroes = Campaign.Current.AliveHeroes.ToList();
                heroes.AddRange(Campaign.Current.DeadOrDisabledHeroes);
                foreach (var hero in heroes)
                {
                    CharacterRacialMix.CreateForExisting(hero);
                }
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("AllCharacterRacialMixes", ref AllCharacterRacialMixes);
            if (dataStore.IsLoading)
            {
                var heroes = Campaign.Current.AliveHeroes.ToList();
                heroes.AddRange(Campaign.Current.DeadOrDisabledHeroes);
                foreach (var key in AllCharacterRacialMixes.Keys.ToList())
                {
                    // Remove data for heroes that no longer exist.
                    if (!heroes.Contains(key.HeroObject))
                    {
                        AllCharacterRacialMixes.Remove(key);
                    }
                    else
                    {
                        AllCharacterRacialMixes[key].SetCharacterRace();
                    }
                }
            }
        }
               
    }
}
