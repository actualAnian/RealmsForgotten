using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career
{
    public class HeroExtendedInfo
    {
        [SaveableField(1)] public List<string> AcquiredAttributes = new();
        [SaveableField(2)] public Dictionary<string, float> CustomResources = new();
        [SaveableField(5)] private CharacterObject _baseCharacter;
        [SaveableField(8)] public string CareerID = string.Empty;
        [SaveableField(9)] public List<string> CareerChoices = new();
        public HeroExtendedInfo() 
        {
            _baseCharacter = Hero.MainHero.CharacterObject;
        }
        public CharacterObject BaseCharacter => _baseCharacter;

        //public void AddCustomResource(string id, float amount)
        //{
        //    if (!CustomResourceManager.DoesResourceObjectExist(id)) return;
        //    if (CustomResources.ContainsKey(id))
        //    {
        //        CustomResources[id] = Math.Max(0, CustomResources[id] + amount);
        //        if (id == "WindsOfMagic")
        //        {
        //            CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
        //        }
        //        else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
        //    }
        //    else
        //    {
        //        CustomResources.Add(id, amount);
        //        if (id == "WindsOfMagic")
        //        {
        //            CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
        //        }
        //        else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
        //    }
        //}

        //public void SetCustomResourceValue(string id, float amount)
        //{
        //    if (!CustomResourceManager.DoesResourceObjectExist(id)) return;
        //    if (CustomResources.ContainsKey(id))
        //    {
        //        CustomResources[id] = Math.Max(0, amount);
        //        if (id == "WindsOfMagic")
        //        {
        //            CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
        //        }
        //        else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
        //    }
        //    else
        //    {
        //        CustomResources.Add(id, amount);
        //        if (id == "WindsOfMagic")
        //        {
        //            CustomResources[id] = Math.Min(MaxWindsOfMagic, CustomResources[id]);
        //        }
        //        else CustomResources[id] = Math.Min(TORConfig.MaximumCustomResourceValue, CustomResources[id]);
        //    }
        //}

        //public float GetCustomResourceValue(string id)
        //{
        //    if (CustomResources.ContainsKey(id))
        //    {
        //        return CustomResources[id];
        //    }
        //    else return 0;
        //}

        //public Dictionary<CustomResource, float> GetCustomResources()
        //{
        //    return CustomResources.ToDictionary(x => CustomResourceManager.GetResourceObject(x.Key), x => x.Value);
        //}


        //@TODO temp
        public List<string> AllAttributes
        {
            get
            {
                return new();
                //var list = new List<string>();
                //if (_baseCharacter != null)
                //{
                //    list.AddRange(_baseCharacter.GetAttributes());
                //    if (list.Count <= 0 && _baseCharacter.OriginalCharacter != null && _baseCharacter.OriginalCharacter.IsTemplate)
                //    {
                //        list.AddRange(_baseCharacter.OriginalCharacter.GetAttributes());
                //    }
                //}
                //list.AddRange(AcquiredAttributes);
                //return list;
            }
        }
    }
}