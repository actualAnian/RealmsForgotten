using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using NAudio.Wave;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using HarmonyLib;

namespace RF_AIDialog
{
    internal sealed class RFAudioPlaybackManager : IDisposable
    {
        private static readonly Lazy<RFAudioPlaybackManager> _lazy =
            new Lazy<RFAudioPlaybackManager>(() => new RFAudioPlaybackManager());

        private readonly object _sync = new object();
        private WaveOutEvent? _player;
        private WaveStream? _reader;
        private Stream? _source;
        private Agent? _facialAgent;
        private bool _facialAnimationActive;
        private bool _mapFacialAnimationActive;
        private int _generation;
        private bool _disposed;

        public static RFAudioPlaybackManager Instance => _lazy.Value;

        // Facial animation / lip-sync are ENGINE calls (SetAgentFacialAnimation)
        // that must run on the main thread — but playback start (TTS Task) and
        // stop (NAudio PlaybackStopped) fire on background threads. Marshal them
        // through this queue, drained by PumpMainThread() from OnApplicationTick.
        private static readonly ConcurrentQueue<Action> MainThreadActions = new ConcurrentQueue<Action>();

        private static void RunOnMainThread(Action action) => MainThreadActions.Enqueue(action);

        public static void PumpMainThread()
        {
            while (MainThreadActions.TryDequeue(out Action action))
            {
                try { action(); }
                catch (Exception ex) { RFAIDebug.Log($"RFAudioPlaybackManager main-thread pump: {ex.GetType().Name}: {ex.Message}"); }
            }
        }

        private RFAudioPlaybackManager() { }

        public int NextGeneration()
        {
            lock (_sync)
            {
                return ++_generation;
            }
        }

        public int CurrentGeneration
        {
            get
            {
                lock (_sync)
                {
                    return _generation;
                }
            }
        }

        public void PlayAudio(byte[] audioBytes, string? formatHint, int generation, string reason)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                return;

            PlaybackState stateToStop = PlaybackState.Stopped;
            lock (_sync)
            {
                if (_disposed || generation != _generation)
                    return;

                stateToStop = _player?.PlaybackState ?? PlaybackState.Stopped;
            }

            if (stateToStop != PlaybackState.Stopped)
                StopAll($"replace playback: {reason}");

            byte[] normalizedBytes = NormalizeAudioBytes(audioBytes, formatHint);
            var source = new MemoryStream(normalizedBytes, writable: false);
            WaveStream? reader = null;
            WaveOutEvent? player = null;
            try
            {
                reader = CreateReader(source, formatHint);
                player = new WaveOutEvent();
                player.Init(reader);
                int playbackGeneration = generation;
                player.PlaybackStopped += (_, args) =>
                {
                    if (args.Exception != null)
                        RFAIDebug.Log($"RFAudioPlaybackManager playback stopped with error: {args.Exception.Message}");

                    CleanupPlayback(playbackGeneration);
                };

                lock (_sync)
                {
                    if (_disposed || generation != _generation)
                    {
                        player.Dispose();
                        reader.Dispose();
                        source.Dispose();
                        return;
                    }

                    _player = player;
                    _reader = reader;
                    _source = source;
                }

                // Marshal the facial-animation START to the main thread; guard
                // against a stale generation (playback already replaced/stopped).
                int startGeneration = generation;
                RunOnMainThread(() =>
                {
                    if (!_disposed && _generation == startGeneration)
                        TryStartFacialAnimationForCurrentConversation();
                });
                player.Play();
                RFAIDebug.Log($"RFAudioPlaybackManager started playback gen={generation} reason={reason}");
            }
            catch
            {
                try { player?.Dispose(); } catch { }
                try { reader?.Dispose(); } catch { }
                try { source.Dispose(); } catch { }
                throw;
            }
        }

        public void StopAll(string reason)
        {
            WaveOutEvent? player = null;
            WaveStream? reader = null;
            Stream? source = null;
            Agent? facialAgent = null;
            bool facialAnimationActive = false;
            bool mapFacialAnimationActive = false;
            int generation;
            lock (_sync)
            {
                generation = ++_generation;
                player = _player;
                reader = _reader;
                source = _source;
                facialAgent = _facialAgent;
                facialAnimationActive = _facialAnimationActive;
                mapFacialAnimationActive = _mapFacialAnimationActive;
                _player = null;
                _reader = null;
                _source = null;
                _facialAgent = null;
                _facialAnimationActive = false;
                _mapFacialAnimationActive = false;
            }

            if (facialAnimationActive)
            {
                Agent? agentToStop = facialAgent;
                RunOnMainThread(() => StopFacialAnimation(agentToStop));
            }
            if (mapFacialAnimationActive)
                RunOnMainThread(() => MapConversationLipSyncBridge.StopTalking());

            try { player?.Stop(); } catch { }
            try { player?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { source?.Dispose(); } catch { }

            RFAIDebug.Log($"RFAudioPlaybackManager stop all gen={generation} reason={reason}");
        }

        private void CleanupPlayback(int generation)
        {
            WaveOutEvent? player = null;
            WaveStream? reader = null;
            Stream? source = null;
            Agent? facialAgent = null;
            bool facialAnimationActive = false;
            bool mapFacialAnimationActive = false;
            lock (_sync)
            {
                if (_generation != generation)
                    return;

                player = _player;
                reader = _reader;
                source = _source;
                facialAgent = _facialAgent;
                facialAnimationActive = _facialAnimationActive;
                mapFacialAnimationActive = _mapFacialAnimationActive;
                _player = null;
                _reader = null;
                _source = null;
                _facialAgent = null;
                _facialAnimationActive = false;
                _mapFacialAnimationActive = false;
            }

            if (facialAnimationActive)
            {
                Agent? agentToStop = facialAgent;
                RunOnMainThread(() => StopFacialAnimation(agentToStop));
            }
            if (mapFacialAnimationActive)
                RunOnMainThread(() => MapConversationLipSyncBridge.StopTalking());

            try { player?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { source?.Dispose(); } catch { }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            StopAll("dispose");
        }

        private static WaveStream CreateReader(Stream source, string? formatHint)
        {
            string normalized = (formatHint ?? string.Empty).Trim().ToLowerInvariant();
            source.Position = 0;

            switch (normalized)
            {
                case "wav":
                    return new WaveFileReader(source);
                case "mp3":
                    return new Mp3FileReader(source);
                default:
                    throw new NotSupportedException(
                        $"Unsupported TTS audio format '{normalized}'. Supported playback formats are wav and mp3.");
            }
        }

        private void TryStartFacialAnimationForCurrentConversation()
        {
            Agent? agent = ResolveConversationAgent();
            string animationName = ResolveConversationFaceAnimation();
            if (agent == null)
            {
                if (!MapConversationLipSyncBridge.StartTalking(animationName))
                {
                    RFAIDebug.Log(
                        $"RFAudioPlaybackManager lip sync skipped | reason=no agent and no map tableau | anim={animationName}");
                    return;
                }

                lock (_sync)
                {
                    _mapFacialAnimationActive = true;
                }

                RFAIDebug.Log(
                    $"RFAudioPlaybackManager started map facial animation | anim={animationName}");
                return;
            }

            if (!StartFacialAnimation(agent, animationName))
            {
                RFAIDebug.Log(
                    $"RFAudioPlaybackManager lip sync failed | agent={SafeAgentName(agent)} | anim={animationName}");
                return;
            }

            lock (_sync)
            {
                _facialAgent = agent;
                _facialAnimationActive = true;
            }

            RFAIDebug.Log(
                $"RFAudioPlaybackManager started facial animation | agent={SafeAgentName(agent)} | anim={animationName}");
        }

        private static Agent? ResolveConversationAgent()
        {
            object? manager = Campaign.Current?.ConversationManager;
            if (manager == null)
            {
                RFAIDebug.Log("RFAudioPlaybackManager.ResolveConversationAgent: ConversationManager null");
                return null;
            }

            try
            {
                object? speakerAgentObject = manager.GetType().GetProperty("SpeakerAgent", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(manager, null);
                if (speakerAgentObject is Agent speakerAgent && IsUsableFacialAgent(speakerAgent))
                {
                    RFAIDebug.Log(
                        $"RFAudioPlaybackManager.ResolveConversationAgent: using SpeakerAgent={SafeAgentName(speakerAgent)}");
                    return speakerAgent;
                }
            }
            catch { }

            try
            {
                object? oneToOneAgentObject = manager.GetType().GetProperty("OneToOneConversationAgent", BindingFlags.Instance | BindingFlags.Public)
                    ?.GetValue(manager, null);
                if (oneToOneAgentObject is Agent oneToOneAgent && IsUsableFacialAgent(oneToOneAgent))
                {
                    RFAIDebug.Log(
                        $"RFAudioPlaybackManager.ResolveConversationAgent: using OneToOneConversationAgent={SafeAgentName(oneToOneAgent)}");
                    return oneToOneAgent;
                }
            }
            catch { }

            RFAIDebug.Log("RFAudioPlaybackManager.ResolveConversationAgent: no usable conversation agent");
            return null;
        }

        private static string ResolveConversationFaceAnimation()
        {
            try
            {
                string? animation = Campaign.Current?.ConversationManager?.CurrentFaceAnimationRecord;
                string normalized = NormalizeTalkingFaceAnimation(animation);
                RFAIDebug.Log(
                    $"RFAudioPlaybackManager.ResolveConversationFaceAnimation: raw={animation ?? "<null>"} normalized={normalized}");
                return normalized;
            }
            catch
            {
                RFAIDebug.Log("RFAudioPlaybackManager.ResolveConversationFaceAnimation: exception, fallback=talking_mean");
                return "talking_mean";
            }
        }

        private static bool StartFacialAnimation(Agent? agent, string animationName)
        {
            if (!IsUsableFacialAgent(agent))
                return false;

            bool started = false;
            started |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)0, animationName, true);
            started |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)1, animationName, true);
            started |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)2, animationName, true);
            return started;
        }

        private static void StopFacialAnimation(Agent? agent)
        {
            if (!IsUsableFacialAgent(agent))
                return;

            if (!SetFacialAnimationOnAllChannels(agent, "idle_normal", true))
                SetFacialAnimationOnAllChannels(agent, "normal", true);
        }

        private static bool SetFacialAnimationOnAllChannels(Agent agent, string animationName, bool loop)
        {
            bool changed = false;
            changed |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)0, animationName, loop);
            changed |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)1, animationName, loop);
            changed |= TrySetFacialAnimation(agent, (Agent.FacialAnimChannel)2, animationName, loop);
            return changed;
        }

        private static bool TrySetFacialAnimation(Agent? agent, Agent.FacialAnimChannel channel, string animationName, bool loop)
        {
            if (!IsUsableFacialAgent(agent) || string.IsNullOrWhiteSpace(animationName))
                return false;

            try
            {
                agent.SetAgentFacialAnimation(channel, animationName, loop);
                RFAIDebug.Log(
                    $"RFAudioPlaybackManager.TrySetFacialAnimation ok | agent={SafeAgentName(agent)} | channel={(int)channel} | anim={animationName} | loop={loop}");
                return true;
            }
            catch
            {
                RFAIDebug.Log(
                    $"RFAudioPlaybackManager.TrySetFacialAnimation failed | agent={SafeAgentName(agent)} | channel={(int)channel} | anim={animationName} | loop={loop}");
                return false;
            }
        }

        private static bool IsUsableFacialAgent(Agent? agent)
        {
            if (agent == null)
                return false;

            try
            {
                return agent.IsActive();
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeTalkingFaceAnimation(string? animationId)
        {
            if (string.IsNullOrWhiteSpace(animationId))
                return "talking_mean";

            string trimmed = animationId.Trim();
            if (string.Equals(trimmed, "idle_normal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "normal", StringComparison.OrdinalIgnoreCase))
            {
                return "talking_mean";
            }

            return trimmed;
        }

        private static string SafeAgentName(Agent? agent)
        {
            if (agent == null)
                return "<null>";

            try
            {
                return agent.Name ?? "<unnamed>";
            }
            catch
            {
                return "<name-error>";
            }
        }

        private static byte[] NormalizeAudioBytes(byte[] audioBytes, string? formatHint)
        {
            string normalized = (formatHint ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized != "wav" || audioBytes == null || audioBytes.Length < 44)
                return audioBytes;

            byte[] repaired = (byte[])audioBytes.Clone();
            bool changed = false;

            if (HasRiffWaveHeader(repaired))
            {
                uint riffSize = ReadUInt32LE(repaired, 4);
                uint expectedRiffSize = (uint)Math.Max(0, repaired.Length - 8);
                if (riffSize == uint.MaxValue || riffSize == 0 || riffSize > expectedRiffSize)
                {
                    WriteUInt32LE(repaired, 4, expectedRiffSize);
                    changed = true;
                }

                int dataChunkOffset = FindChunk(repaired, "data");
                if (dataChunkOffset >= 0 && dataChunkOffset + 8 <= repaired.Length)
                {
                    uint dataSize = ReadUInt32LE(repaired, dataChunkOffset + 4);
                    uint expectedDataSize = (uint)Math.Max(0, repaired.Length - (dataChunkOffset + 8));
                    if (dataSize == uint.MaxValue || dataSize == 0 || dataSize > expectedDataSize)
                    {
                        WriteUInt32LE(repaired, dataChunkOffset + 4, expectedDataSize);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                RFAIDebug.Log(
                    $"RFAudioPlaybackManager repaired WAV header | bytes={repaired.Length} | riffSize={ReadUInt32LE(repaired, 4)}");
                return repaired;
            }

            return audioBytes;
        }

        private static bool HasRiffWaveHeader(byte[] bytes)
        {
            return bytes.Length >= 12
                && bytes[0] == (byte)'R'
                && bytes[1] == (byte)'I'
                && bytes[2] == (byte)'F'
                && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W'
                && bytes[9] == (byte)'A'
                && bytes[10] == (byte)'V'
                && bytes[11] == (byte)'E';
        }

        private static int FindChunk(byte[] bytes, string chunkId)
        {
            if (bytes.Length < 12 || string.IsNullOrEmpty(chunkId) || chunkId.Length != 4)
                return -1;

            int offset = 12;
            while (offset + 8 <= bytes.Length)
            {
                if (bytes[offset] == (byte)chunkId[0]
                    && bytes[offset + 1] == (byte)chunkId[1]
                    && bytes[offset + 2] == (byte)chunkId[2]
                    && bytes[offset + 3] == (byte)chunkId[3])
                {
                    return offset;
                }

                uint chunkSize = ReadUInt32LE(bytes, offset + 4);
                long nextOffset = offset + 8L + chunkSize;
                if ((chunkSize & 1) != 0)
                    nextOffset++;

                if (nextOffset <= offset || nextOffset > bytes.Length)
                    break;

                offset = (int)nextOffset;
            }

            return -1;
        }

        private static uint ReadUInt32LE(byte[] bytes, int offset)
        {
            return (uint)(
                bytes[offset]
                | (bytes[offset + 1] << 8)
                | (bytes[offset + 2] << 16)
                | (bytes[offset + 3] << 24));
        }

        private static void WriteUInt32LE(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)(value & 0xFF);
            bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
            bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
            bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
        }
    }

    internal static class MapConversationLipSyncBridge
    {
        private static readonly object _sync = new object();
        private static bool _initialized;
        private static bool _unavailableLogged;
        private static bool _successLogged;
        private static Type? _campaignMissionType;
        private static PropertyInfo? _currentProperty;
        private static Type? _mappedMissionType;
        private static PropertyInfo? _mappedConversationTableauProperty;
        private static Type? _mappedTableauType;
        private static FieldInfo? _mappedAgentVisualsField;
        private static Type? _mappedAgentVisualType;
        private static MethodInfo? _mappedGetVisualsMethod;
        private static Type? _mappedMbAgentVisualsType;
        private static MethodInfo? _mappedGetSkeletonMethod;
        private static MethodInfo? _setFacialAnimationMethod;

        public static bool StartTalking(string? faceAnimation)
        {
            if (string.IsNullOrWhiteSpace(faceAnimation))
                faceAnimation = "talking_engaged";

            return TrySetFaceAnimation(faceAnimation, true, true, true, true);
        }

        public static bool StopTalking()
        {
            return TrySetFaceAnimation("idle_normal", true, false, false, false)
                || TrySetFaceAnimation("normal", true, false, false, false);
        }

        private static bool TrySetFaceAnimation(string faceAnimation, bool loop, bool resetFirst, bool logUnavailable, bool logSuccess)
        {
            if (string.IsNullOrWhiteSpace(faceAnimation))
                return false;

            try
            {
                object? skeleton = ResolvePartnerSkeleton();
                if (skeleton == null)
                {
                    LogUnavailableOnce(logUnavailable, "Map conversation tableau skeleton is not available.");
                    return false;
                }

                if (resetFirst)
                    TryInvokeSetFacialAnimation(skeleton, "idle_normal", true);

                if (!TryInvokeSetFacialAnimation(skeleton, faceAnimation.Trim(), loop))
                    return false;

                if (logSuccess && !_successLogged)
                {
                    _successLogged = true;
                    RFAIDebug.Log("MapConversationLipSyncBridge: routed facial animation to tableau skeleton");
                }

                return true;
            }
            catch (TargetInvocationException ex)
            {
                if (logUnavailable)
                    RFAIDebug.Log("MapConversationLipSyncBridge failed: " + (ex.InnerException?.Message ?? ex.Message));
                return false;
            }
            catch (Exception ex)
            {
                if (logUnavailable)
                    RFAIDebug.Log("MapConversationLipSyncBridge failed: " + ex.Message);
                return false;
            }
        }

        private static object? ResolvePartnerSkeleton()
        {
            object? currentCampaignMission = GetCurrentCampaignMission();
            if (currentCampaignMission == null)
                return null;

            object? conversationTableau = GetConversationTableau(currentCampaignMission);
            if (conversationTableau == null)
                return null;

            object? conversationPartnerVisual = GetConversationPartnerVisual(conversationTableau);
            if (conversationPartnerVisual == null)
                return null;

            object? visuals = GetVisuals(conversationPartnerVisual);
            if (visuals == null)
                return null;

            return GetSkeleton(visuals);
        }

        private static object? GetCurrentCampaignMission()
        {
            EnsureInitialized();
            return _currentProperty?.GetValue(null, null);
        }

        private static object? GetConversationTableau(object mission)
        {
            Type type = mission.GetType();
            if (_mappedConversationTableauProperty != null && _mappedMissionType == type)
                return _mappedConversationTableauProperty.GetValue(mission, null);

            lock (_sync)
            {
                if (_mappedConversationTableauProperty != null && _mappedMissionType == type)
                    return _mappedConversationTableauProperty.GetValue(mission, null);

                PropertyInfo? property = type.GetProperty("ConversationTableau", BindingFlags.Instance | BindingFlags.Public);
                if (property == null)
                    return null;

                _mappedMissionType = type;
                _mappedConversationTableauProperty = property;
                return property.GetValue(mission, null);
            }
        }

        private static object? GetConversationPartnerVisual(object tableau)
        {
            Type type = tableau.GetType();
            if (_mappedAgentVisualsField == null || _mappedTableauType != type)
            {
                lock (_sync)
                {
                    if (_mappedAgentVisualsField == null || _mappedTableauType != type)
                    {
                        _mappedTableauType = type;
                        _mappedAgentVisualsField = type.GetField("_agentVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
                    }
                }
            }

            if (!(_mappedAgentVisualsField?.GetValue(tableau) is IList { Count: not 0 } list))
                return null;

            return list[0];
        }

        private static object? GetVisuals(object agentVisual)
        {
            Type type = agentVisual.GetType();
            if (_mappedGetVisualsMethod == null || _mappedAgentVisualType != type)
            {
                lock (_sync)
                {
                    if (_mappedGetVisualsMethod == null || _mappedAgentVisualType != type)
                    {
                        _mappedAgentVisualType = type;
                        _mappedGetVisualsMethod = type.GetMethod("GetVisuals", BindingFlags.Instance | BindingFlags.Public);
                    }
                }
            }

            return _mappedGetVisualsMethod?.Invoke(agentVisual, null);
        }

        private static object? GetSkeleton(object visuals)
        {
            Type type = visuals.GetType();
            if (_mappedGetSkeletonMethod == null || _mappedMbAgentVisualsType != type)
            {
                lock (_sync)
                {
                    if (_mappedGetSkeletonMethod == null || _mappedMbAgentVisualsType != type)
                    {
                        _mappedMbAgentVisualsType = type;
                        _mappedGetSkeletonMethod = type.GetMethod("GetSkeleton", BindingFlags.Instance | BindingFlags.Public);
                    }
                }
            }

            return _mappedGetSkeletonMethod?.Invoke(visuals, null);
        }

        private static bool TryInvokeSetFacialAnimation(object skeleton, string faceAnimation, bool loop)
        {
            EnsureInitialized();
            if (skeleton == null || _setFacialAnimationMethod == null || string.IsNullOrWhiteSpace(faceAnimation))
                return false;

            _setFacialAnimationMethod.Invoke(null, new object[5]
            {
                skeleton,
                (object)(Agent.FacialAnimChannel)1,
                faceAnimation,
                false,
                loop
            });
            return true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_sync)
            {
                if (_initialized)
                    return;

                _campaignMissionType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignMission");
                _currentProperty = _campaignMissionType?.GetProperty("Current", BindingFlags.Static | BindingFlags.Public);

                Type? type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.MBSkeletonExtensions");
                if (type != null)
                {
                    foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    {
                        if (methodInfo.Name == "SetFacialAnimation" && methodInfo.GetParameters().Length == 5)
                        {
                            _setFacialAnimationMethod = methodInfo;
                            break;
                        }
                    }
                }

                _initialized = true;
            }
        }

        private static void LogUnavailableOnce(bool shouldLog, string message)
        {
            if (shouldLog && !_unavailableLogged)
            {
                _unavailableLogged = true;
                RFAIDebug.Log("MapConversationLipSyncBridge unavailable: " + message);
            }
        }
    }
}
