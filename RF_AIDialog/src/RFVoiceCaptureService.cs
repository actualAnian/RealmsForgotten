using System;
using System.IO;
using NAudio.Wave;

namespace RF_AIDialog
{
    internal sealed class RFVoiceCaptureService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _writer;
        private MemoryStream? _memoryStream;
        private bool _isRecording;

        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording)
                return;

            try
            {
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

            _waveIn?.StopRecording();
            _isRecording = false;

            _writer?.Flush();
            return _memoryStream?.ToArray() ?? Array.Empty<byte>();
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
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

            _waveIn?.Dispose();
            _waveIn = null;
            _writer?.Dispose();
            _writer = null;
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
