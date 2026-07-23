using System;
using System.IO;
using System.Threading;
using NAudio.Wave;

namespace RF_AIDialog
{
    internal sealed class RFVoiceCaptureService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private MemoryStream? _memoryStream;
        private bool _isRecording;

        // NAudio's StopRecording() is asynchronous: RecordingStopped fires on a
        // separate thread. We capture the finished bytes inside that event and
        // hand them back through _capturedAudio, signalling _stoppedSignal so
        // StopRecording() can return complete, non-disposed audio. Reading
        // _memoryStream directly from StopRecording() raced the writer disposal
        // and produced ObjectDisposedException / truncated clips.
        private byte[] _capturedAudio = Array.Empty<byte>();
        private readonly ManualResetEventSlim _stoppedSignal = new ManualResetEventSlim(false);

        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording)
                return;

            try
            {
                _capturedAudio = Array.Empty<byte>();
                _stoppedSignal.Reset();
                _memoryStream = new MemoryStream();
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 100
                };

                _writer = new WaveFileWriter(_memoryStream, _waveIn.WaveFormat);
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;
                _waveIn.StartRecording();
                _isRecording = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public byte[] StopRecording()
        {
            if (!_isRecording)
                return Array.Empty<byte>();

            _isRecording = false;

            var waveIn = _waveIn;
            if (waveIn == null)
                return _capturedAudio;

            // Trigger the async stop and wait for OnRecordingStopped to flush the
            // writer and capture the bytes (bounded wait so a stuck device can't
            // hang the UI thread).
            waveIn.StopRecording();
            _stoppedSignal.Wait(TimeSpan.FromSeconds(2));
            return _capturedAudio;
        }

        // ~3 minutes at 16 kHz / 16-bit mono (~1.9 MB per minute). A forgotten
        // Record used to grow the buffer without limit and then fail the STT
        // upload only AFTER shipping a giant payload.
        private const long MaxCaptureBytes = 3L * 60 * 16000 * 2;

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (_memoryStream != null && _memoryStream.Length >= MaxCaptureBytes)
                {
                    // Auto-stop: keep what we have; the transcript path proceeds
                    // normally from StopRecording's captured bytes.
                    _waveIn?.StopRecording();
                    return;
                }

                _writer?.Write(e.Buffer, 0, e.BytesRecorded);
                _writer?.Flush();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"RFVoiceCaptureService.OnDataAvailable failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                RFAIDebug.Log($"RFVoiceCaptureService.OnRecordingStopped failed: {e.Exception.GetType().Name}: {e.Exception.Message}");

            try
            {
                // Disposing the writer flushes the final WAV header into the
                // MemoryStream. MemoryStream.ToArray() is valid even after the
                // writer has closed the underlying stream, so capture here.
                _writer?.Dispose();
                _writer = null;
                _capturedAudio = _memoryStream?.ToArray() ?? Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"RFVoiceCaptureService.OnRecordingStopped capture failed: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _waveIn?.Dispose();
                _waveIn = null;
                _stoppedSignal.Set();
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isRecording)
                    _waveIn?.StopRecording();
            }
            catch { }

            try { _waveIn?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _memoryStream?.Dispose(); } catch { }
            _waveIn = null;
            _writer = null;
            _memoryStream = null;
            _isRecording = false;
        }
    }
}
