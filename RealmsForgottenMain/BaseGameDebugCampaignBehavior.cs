using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten
{
    public class BaseGameDebugCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, new Action(this.OnSave));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionStart));
        }

        private void OnSessionStart(CampaignGameStarter obj)
        {
            if (this._heroRaceMap.Count > 0)
            {
                foreach (Hero hero in Hero.AllAliveHeroes)
                {
                    if (this._heroRaceMap.ContainsKey(hero.StringId) && this._heroRaceMap[hero.StringId] != hero.CharacterObject.Race)
                    {
                        hero.CharacterObject.Race = this._heroRaceMap[hero.StringId];
                    }
                }
            }
        }

        private void OnSave()
        {
            this._heroRaceMap = new Dictionary<string, int>();
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (!this._heroRaceMap.ContainsKey(hero.StringId))
                {
                    this._heroRaceMap.Add(hero.StringId, hero.CharacterObject.Race);
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<Dictionary<string, int>>("_heroRaceMap", ref this._heroRaceMap);
        }
        private Dictionary<string, int> _heroRaceMap = new Dictionary<string, int>();
    }
    public class HeroRaceMapSaveableTypeDefiner : SaveableTypeDefiner
    {
        public HeroRaceMapSaveableTypeDefiner() : base(576011)
        {
        }

        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(Dictionary<string, int>));
        }
    }

}
