using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RealmsForgotten.MusicSounds
{

    public static class OggUtils
    {
        static readonly Dictionary<string, float> _cachedSoundData = new();
        public static float GetSoundLength(string name)
        {
            if (_cachedSoundData.TryGetValue(name, out var data)) return data;
            try
            {
                var path = Path.Combine(ModuleHelper.GetModuleFullPath("RealmsForgotten"), "ModuleSounds", name + ".ogg");
                if (!File.Exists(path))
                {
                    InformationManager.DisplayMessage(new($"Error, could not find a sound file with name: {name}. Is it present in RealmsForgotten\\ModuleSounds?" ));
                    return 0;
                }
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                int sampleRate = ReadSampleRate(fs);
                long totalSamples = ReadLastGranulePosition(fs);
                float seconds = (float)totalSamples / sampleRate;
                _cachedSoundData.Add(name, seconds);
                return seconds;
            }
            catch
            {
                InformationManager.DisplayMessage(new($"Error reading the sound file with name: {name}. Is it a Vorbis .ogg file?"));
                return 0;
            }
        }

        private static int ReadSampleRate(FileStream fs)
        {
            fs.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[4096];
            fs.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < buffer.Length - 30; i++)
            {
                if (buffer[i] == 1 && Encoding.ASCII.GetString(buffer, i + 1, 6) == "vorbis")
                {
                    return BitConverter.ToInt32(buffer, i + 12);
                }
            }
            throw new Exception("Vorbis header not found");
        }

        private static long ReadLastGranulePosition(FileStream fs)
        {
            const int searchSize = 65536;
            long start = Math.Max(0, fs.Length - searchSize);
            fs.Seek(start, SeekOrigin.Begin);
            byte[] buffer = new byte[fs.Length - start];
            fs.Read(buffer, 0, buffer.Length);

            for (int i = buffer.Length - 4; i >= 0; i--)
            {
                if (buffer[i] == 'O' && buffer[i + 1] == 'g' && buffer[i + 2] == 'g' && buffer[i + 3] == 'S')
                {
                    return BitConverter.ToInt64(buffer, i + 6);
                }
            }
            throw new Exception("Last Ogg page not found");
        }
    }
}
