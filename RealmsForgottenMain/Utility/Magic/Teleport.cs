using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Utility.Magic
{
    public static class Teleport
    {
        private static void AddParticleEffect(Agent agent)
        {
            Scene scene = Mission.Current.Scene;
            GameEntity childEntity = GameEntity.CreateEmpty(scene);
            MatrixFrame localFrame = new(Mat3.Identity, new(0, 0, 0));
            childEntity.SetLocalPosition(agent.Position);
            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity("teleport", childEntity, ref localFrame);
        }
        private static void CreateSoundEffect(Agent agent)
        {
            int eventId = SoundEvent.GetEventIdFromString("teleport_sound");
            SoundEvent sEvent = SoundEvent.CreateEvent(eventId, Mission.Current.Scene);
            sEvent.SetPosition(agent.Position);
            sEvent.Play();
        }
        public static void TeleportToPosition(Agent agent, Vec3 position)
        {
            if (agent == null)
                return;
            CreateSoundEffect(agent);
            AddParticleEffect(agent);

            agent.TeleportToPosition(position);

            CreateSoundEffect(agent);
            AddParticleEffect(agent);
        }
    }

}
