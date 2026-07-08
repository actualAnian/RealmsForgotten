using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MusicSounds
{
    public class RFSoundEvent
    {
        public RFSoundEvent(SoundEvent soundEvent, int soundId, float totalLength, bool followPlayer)
        {
            SoundEvent = soundEvent;
            SoundId = soundId;
            FollowPlayer = followPlayer;
            EndTime = DateTime.Now + TimeSpan.FromSeconds(totalLength);
        }
        public int SoundId { get; private set; }
        public DateTime StartDate { get; private set; }
        public bool FollowPlayer { get; private set; }
        public DateTime EndTime { get; private set; }
        public SoundEvent SoundEvent { get; set; }
    }
    public class RFMissionSoundManager : MissionLogic
    {
        private List<RFSoundEvent> _activeSounds = new();
        private readonly object _activeSoundsLock = new();
        private bool _hasActiveSounds = false;
        private bool _shouldStop = false; 
        private readonly Thread _soundThread;

        public RFMissionSoundManager()
        {
            _soundThread = new Thread(SoundLoop)
            {
                IsBackground = true
            };
            _soundThread.Start();
        }

        protected override void OnEndMission()
        {
            StopAllSounds();
            _shouldStop = true;
            _soundThread.Join();
        }
        void StopAllSounds()
        {

            lock (_activeSoundsLock)
            {
                for (int i = _activeSounds.Count - 1; i >= 0; i--)
                {
                    RFSoundEvent sound = _activeSounds[i];
                    sound.SoundEvent.Stop();
                    sound.SoundEvent.Release();
                    _activeSounds.Remove(sound);
                }
            }
        }
        // to register an event, remember to add the sounds to module_sounds.xml
        public bool AddSoundEvent(string name, bool followPlayer)
        {
            var soundLength = OggUtils.GetSoundLength(name);
            if (soundLength == 0)
                return false;
            int eventId = SoundEvent.GetEventIdFromString(name);
            SoundEvent sEvent = SoundEvent.CreateEvent(eventId, Mission.Current.Scene);
            if (sEvent.GetSoundId() == -1) return false;

            RFSoundEvent rfSoundEvent = new(sEvent, eventId, soundLength, followPlayer);
            lock (_activeSoundsLock)
            {
                if (_activeSounds.Contains(rfSoundEvent)) return false;
                if (_activeSounds.Count == 0) _hasActiveSounds = true;
                _activeSounds.Add(rfSoundEvent);
                if (Agent.Main != null)
                    sEvent.SetPosition(Agent.Main.Position);
                sEvent.Play();
            }
            return true;
        }
        private void SoundLoop()
        {
            TimeSpan tickInterval = TimeSpan.FromMilliseconds(250); // 1/4 second

            while (!_shouldStop)
            {
                DateTime now = DateTime.Now;
                if (_hasActiveSounds)
                {
                    lock (_activeSoundsLock)
                    {
                        for (int i = _activeSounds.Count - 1; i >= 0; i--)
                        {
                            RFSoundEvent sound = _activeSounds[i];
                            if (sound.EndTime < now)
                            {
                                sound.SoundEvent.Stop();
                                sound.SoundEvent.Release();
                                _activeSounds.Remove(sound);
                                continue;
                            }
                            else
                            {
                                if (sound.FollowPlayer && Agent.Main != null)
                                    sound.SoundEvent.SetPosition(Agent.Main.Position);
                            }
                        }
                        if (_activeSounds.Count == 0) _hasActiveSounds = false;
                    }
                }
                var elapsed = DateTime.Now - now;
                var sleepTime = tickInterval - elapsed;
                if (sleepTime > TimeSpan.Zero)
                {
                    //Thread.Sleep(sleepTime);
                }
            }
        }
    }
}