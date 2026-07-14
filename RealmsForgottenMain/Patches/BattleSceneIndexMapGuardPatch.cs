using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches
{
    // DIAGNOSTIC-ONLY (no mutation) on MBMapScene.GetBattleSceneIndexMap (called once
    // during map load):
    //  (A) PREFIX: logs the index-map resolution + buffer size BEFORE the native fill.
    //  (B) POSTFIX: logs a histogram of the returned 16-bit index map (value ranges,
    //      max, high-byte usage) to characterise the format — NO snapping.
    //
    // FINDINGS (2026-07-11 probe): resolution is a healthy 1024x1024, buffer=2097152
    // (== w*h*2), overflowsInt32=False. The crash is INTERMITTENT with identical valid
    // data => NOT a resolution/format bug. The AccessViolation ("other memory is
    // corrupt") is pre-existing HEAP CORRUPTION elsewhere surfacing at this large
    // native write. Byte-wise snapping was removed (the map is 16-bit; snapping bytes
    // would corrupt it, and it never prevented the load-time native fault anyway).
    [HarmonyPatch]
    public static class BattleSceneIndexMapGuardPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                "TaleWorlds.MountAndBlade.MBMapScene:GetBattleSceneIndexMap",
                new[]
                {
                    typeof(Scene),
                    typeof(byte[]).MakeByRefType(),
                    typeof(int).MakeByRefType(),
                    typeof(int).MakeByRefType()
                });
        }

        // (A) Diagnostic — runs BEFORE the native fill.
        private static void Prefix(Scene mapScene, byte[] indexData)
        {
            try
            {
                int width = 0, height = 0;
                bool gotRes = TryGetResolution(mapScene, ref width, ref height);
                long expectedBytes = (long)width * height * 2;
                bool overflows = expectedBytes > int.MaxValue;

                Probe(
                    "PRE GetBattleSceneIndexMap | " +
                    $"resolutionRead={gotRes} width={width} height={height} " +
                    $"expectedBytes(w*h*2)={expectedBytes} overflowsInt32={overflows} " +
                    $"incomingBufferLen={(indexData?.Length ?? -1)}");

                if (!gotRes || width <= 0 || height <= 0)
                {
                    Probe("WARNING: resolucao invalida/zero -> cena de mapa corrompida ou terreno " +
                          "nao carregado. Provavel causa do AccessViolation.");
                }
                if (overflows)
                {
                    Probe("WARNING: width*height*2 excede Int32 -> o buffer vanilla (int) faz overflow. " +
                          "Battle grid grande/resolucao errada = AccessViolation.");
                }
            }
            catch (Exception ex)
            {
                // Never let the diagnostic break loading.
                Probe($"Prefix diag falhou (inofensivo): {ex.Message}");
            }
        }

        // (B) LOG-ONLY histogram — no mutation. The index map is 16-bit (w*h*2),
        // so byte-wise snapping was wrong/destructive. Instead, characterise the
        // real format: how many pixels are 0/1..157/158..255/>255, and the max
        // value, viewing the buffer both as bytes and as little-endian ushorts.
        private static void Postfix(ref byte[] indexData, int width, int height)
        {
            if (indexData == null || indexData.Length == 0)
            {
                Probe($"POST OK mas buffer vazio | width={width} height={height}");
                return;
            }

            try
            {
                int len = indexData.Length;

                // View as 16-bit little-endian ushort per pixel.
                long px0 = 0, px1_157 = 0, px158_255 = 0, pxOver255 = 0, pxMax = 0;
                long highByteNonZero = 0;
                for (int i = 0; i + 1 < len; i += 2)
                {
                    if (indexData[i + 1] != 0) highByteNonZero++;
                    int v = indexData[i] | (indexData[i + 1] << 8);
                    if (v > pxMax) pxMax = v;
                    if (v == 0) px0++;
                    else if (v <= 157) px1_157++;
                    else if (v <= 255) px158_255++;
                    else pxOver255++;
                }

                Probe($"POST OK | width={width} height={height} bytes={len} " +
                      $"[u16] px=0:{px0} 1-157:{px1_157} 158-255:{px158_255} >255:{pxOver255} " +
                      $"maxIndex={pxMax} highByteNonZero={highByteNonZero}");
            }
            catch (Exception ex)
            {
                Probe($"POST histograma falhou: {ex.Message}");
            }
        }

        // Dedicated always-on probe log — independent of RFLogger (which is compiled
        // off, Enabled=false). Writes to Documents\...\ModLogs and the game folder,
        // plus Debug.Print (rgl_log / VS Output). Best-effort, never throws.
        private static void Probe(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [BattleSceneIndexGuard] {msg}\n";
            try { Debug.Print("[RF] " + line); } catch { }

            string[] paths =
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_BattleGridProbe.log"),
                System.IO.Path.Combine(BasePath.Name, "Configs", "ModLogs", "RF_BattleGridProbe.log"),
                System.IO.Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_BattleGridProbe.log")
            };
            foreach (string p in paths)
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(p);
                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);
                    File.AppendAllText(p, line);
                }
                catch
                {
                    // try next path
                }
            }
        }

        // Reads the battle-scene index-map resolution via the internal MBAPI.IMBMapScene
        // (reflection, since it is internal). This native call only reads two ints, so
        // it is safe to call here before the crashing GetBattleSceneIndexMap fill.
        private static bool TryGetResolution(Scene mapScene, ref int width, ref int height)
        {
            try
            {
                if (mapScene == null)
                    return false;

                // MBAPI lives in TaleWorlds.MountAndBlade (NOT TaleWorlds.Engine);
                // its IMBMapScene accessor field is internal static.
                Type mbapi = AccessTools.TypeByName("TaleWorlds.MountAndBlade.MBAPI");
                FieldInfo field = mbapi?.GetField("IMBMapScene",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                object instance = field?.GetValue(null);
                if (instance == null)
                {
                    Probe($"TryGetResolution: MBAPI type/field nao resolvido (mbapi={(mbapi != null)}, field={(field != null)}).");
                    return false;
                }

                MethodInfo method = instance.GetType().GetMethod("GetBattleSceneIndexMapResolution",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    return false;

                object[] args = { mapScene.Pointer, 0, 0 };
                method.Invoke(instance, args);
                width = (int)args[1];
                height = (int)args[2];
                return true;
            }
            catch
            {
                return false;
            }
        }

            }
}
