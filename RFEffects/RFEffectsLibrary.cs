using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using Path = System.IO.Path;

namespace RealmsForgotten.RFEffects
{
    public struct AgentEffectData
    {
        public Agent Agent;
        public string Effect;
        public Timer Timer;
        public GameEntity ParticleEntity;

        public AgentEffectData(Agent agent, string effect, Timer timer, GameEntity particleEntity)
        {
            Agent = agent;
            Effect = effect;
            Timer = timer;
            ParticleEntity = particleEntity;
        }

        public void RemoveEffect()
        {
            if (ParticleEntity != null)
            {

                Skeleton agentSkeleton = Agent.AgentVisuals.GetSkeleton();
                if (agentSkeleton != null)
                {
                    Agent.AgentVisuals.RemoveChildEntity(ParticleEntity, 0);

                    agentSkeleton.RemoveComponent(ParticleEntity.GetComponentAtIndex(0, GameEntity.ComponentType.ParticleSystemInstanced));
                }
                else
                {
                    ParticleEntity.RemoveSkeleton();
                    ParticleEntity = null;
                }

            }

        }
    }
    public struct WeaponEffectData
    {
        public string Id;
        public string ItemParticle;
        public string VictimParticle;
        public string Effect;
        public float Duration;
        public float AreaOfEffect;

        public WeaponEffectData(string id, string itemParticle, string victimParticle, string effect, string duration, string areaOfEffect)
        {
            Id = id;
            ItemParticle = itemParticle;
            VictimParticle = victimParticle;
            Effect = effect;

            float.TryParse(duration, out Duration);
            float.TryParse(areaOfEffect, out AreaOfEffect);

        }
    }
    [Serializable]
    public class RFEffectsLibrary
    {
        internal const string LIBRARY_FILENAME = "weapons_effects.xml";
        public static Dictionary<string, WeaponEffectData> CurrentWeaponEffects = new();

        public List<WeaponEffect> Effects = new List<WeaponEffect>();
        public static void Initialize()
        {
            RFEffectsLibrary Instance = LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), LIBRARY_FILENAME));

            CurrentWeaponEffects = new();
            foreach (var weaponEffect in Instance.Effects)
            {
                CurrentWeaponEffects.Add(weaponEffect.Id, new WeaponEffectData(weaponEffect.Id, weaponEffect.ItemParticle, weaponEffect.VictimParticle, weaponEffect.Effect, weaponEffect.Duration, weaponEffect.AreaOfEffect));
            }
        }
        internal static RFEffectsLibrary LoadFromFile(string path)
        {
            RFEffectsLibrary result = null;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(RFEffectsLibrary));
            using (StreamReader streamReader = File.OpenText(path))
            {
                result = (RFEffectsLibrary)xmlSerializer.Deserialize(streamReader);
            }

            return result;
        }

        [Serializable]
        public class WeaponEffect
        {
            [XmlAttribute("id")]
            public string Id = null;
            [XmlAttribute("item_particle")]
            public string ItemParticle = null;
            [XmlAttribute("victim_particle")]
            public string VictimParticle = null;
            [XmlAttribute("effect")]
            public string Effect = null;
            [XmlAttribute("duration")]
            public string Duration = null;
            [XmlAttribute("area_of_effect")]
            public string AreaOfEffect = null;
        }
    }
}
