using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RealmsForgotten.Diagnostics
{
    /// <summary>
    /// Detector de travadas do frame principal (DEV-ONLY, ligado apenas se existir o
    /// arquivo-flag rf_dev_diag.flag em Documentos\Mount and Blade II Bannerlord —
    /// nunca distribuído; em máquina de jogador isto é código morto).
    ///
    /// Motivo (2026-08-26): transições de tela (enciclopédia/character/conversa → mapa)
    /// travando 15s+. É o "Break All" automático: SubModule.OnApplicationTick bate um
    /// coração por frame; uma thread vigia e, quando o coração para por mais de
    /// ThresholdSeconds, SUSPENDE a thread principal, captura o call stack e escreve em
    /// Documentos\...\Logs\RF_HitchTrace.log com flush imediato (dá para tail com o
    /// jogo aberto). Amostra de novo a cada ~3s enquanto a travada durar — método que
    /// aparece em todas as amostras é o culpado.
    /// </summary>
    internal static class RFHitchDetector
    {
        private const double ThresholdSeconds = 1.5;
        private const double ResampleSeconds = 3.0;
        private const int MaxSamplesPerStall = 6;
        private const int MaxFrames = 40;

        private static readonly bool Enabled = File.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "rf_dev_diag.flag"));

        private static Thread _mainThread;
        private static StreamWriter _writer;
        private static long _lastBeatTicks;
        private static int _started;
        private static bool _inStall;
        private static DateTime _stallStart;
        private static DateTime _lastSample;
        private static int _samplesThisStall;

        /// <summary>Chamar uma vez por frame, da thread principal.</summary>
        public static void Heartbeat()
        {
            if (!Enabled)
            {
                return;
            }

            Interlocked.Exchange(ref _lastBeatTicks, DateTime.UtcNow.Ticks);

            if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
            {
                _mainThread = Thread.CurrentThread;
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Mount and Blade II Bannerlord", "Logs");
                    Directory.CreateDirectory(dir);
                    _writer = new StreamWriter(Path.Combine(dir, "RF_HitchTrace.log"), append: false) { AutoFlush = true };
                    Write($"=== RF_HitchTrace iniciado {DateTime.Now:HH:mm:ss} | limiar {ThresholdSeconds:0.#}s ===");
                }
                catch
                {
                    // sem arquivo, ainda escrevemos no rgl via Debug.Print
                }

                Thread watcher = new Thread(WatchLoop) { IsBackground = true, Name = "RF_HitchWatcher" };
                watcher.Start();
            }
        }

        private static void WatchLoop()
        {
            while (true)
            {
                Thread.Sleep(250);
                long last = Interlocked.Read(ref _lastBeatTicks);
                double stalled = (DateTime.UtcNow - new DateTime(last, DateTimeKind.Utc)).TotalSeconds;

                if (stalled > ThresholdSeconds)
                {
                    if (!_inStall)
                    {
                        _inStall = true;
                        _stallStart = DateTime.UtcNow;
                        _samplesThisStall = 0;
                        _lastSample = DateTime.MinValue;
                        Write($"== TRAVADA detectada ({DateTime.Now:HH:mm:ss}) — frame parado ha {stalled:0.0}s ==");
                    }

                    if (_samplesThisStall < MaxSamplesPerStall &&
                        (DateTime.UtcNow - _lastSample).TotalSeconds >= (_samplesThisStall == 0 ? 0 : ResampleSeconds))
                    {
                        _samplesThisStall++;
                        _lastSample = DateTime.UtcNow;
                        CaptureMainThreadStack(_samplesThisStall, stalled);
                    }
                }
                else if (_inStall)
                {
                    _inStall = false;
                    Write($"== travada terminou — duracao total ~{(DateTime.UtcNow - _stallStart).TotalSeconds:0.0}s ==");
                }
            }
        }

        private static void CaptureMainThreadStack(int sample, double stalledFor)
        {
            try
            {
                StackTrace trace;
#pragma warning disable 618, SYSLIB0006
                _mainThread.Suspend();
                try
                {
                    trace = new StackTrace(_mainThread, false);
                }
                finally
                {
                    _mainThread.Resume();
                }
#pragma warning restore 618, SYSLIB0006

                Write($"-- amostra {sample} (parado ha {stalledFor:0.0}s):");
                StackFrame[] frames = trace.GetFrames();
                if (frames == null || frames.Length == 0)
                {
                    Write("   (stack vazio — thread provavelmente em codigo nativo puro)");
                    return;
                }
                int count = Math.Min(frames.Length, MaxFrames);
                for (int i = 0; i < count; i++)
                {
                    var method = frames[i].GetMethod();
                    Write($"   {method?.DeclaringType?.FullName ?? "?"}.{method?.Name ?? "?"}");
                }
            }
            catch (Exception ex)
            {
                Write("   captura falhou: " + ex.Message);
            }
        }

        private static void Write(string line)
        {
            try
            {
                _writer?.WriteLine(line);
            }
            catch
            {
            }
            TaleWorlds.Library.Debug.Print("[RF_Hitch] " + line);
        }
    }
}
