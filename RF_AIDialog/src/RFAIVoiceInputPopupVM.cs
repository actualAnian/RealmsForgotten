using System;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    public sealed class RFAIVoiceInputPopupVM : ViewModel
    {
        private readonly Action<string> _onAsk;
        private readonly Action _onCancel;
        private readonly Action _onToggleRecording;

        private string _titleText;
        private string _promptText;
        private string _inputText;
        private string _statusText;
        private string _recordButtonText;
        private string _recordHintText;
        private string _hotkeyHint;
        private bool _canAsk;
        private bool _canToggleRecording;
        private bool _canEditText;

        public RFAIVoiceInputPopupVM(
            string titleText,
            string promptText,
            string inputText,
            Action<string> onAsk,
            Action onCancel,
            Action onToggleRecording)
        {
            _titleText = titleText;
            _promptText = promptText;
            _inputText = inputText;
            _onAsk = onAsk;
            _onCancel = onCancel;
            _onToggleRecording = onToggleRecording;
            _statusText = "Use the microphone button to dictate, or type your words below.";
            _recordButtonText = "Record";
            _hotkeyHint = AIConfig.STTHotkey;
            _recordHintText = $"Hotkey: hold {_hotkeyHint} as an alternative.";
            _canAsk = !string.IsNullOrWhiteSpace(inputText);
            _canToggleRecording = true;
            _canEditText = true;
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (value == _titleText) return;
                _titleText = value;
                OnPropertyChangedWithValue(value, nameof(TitleText));
            }
        }

        [DataSourceProperty]
        public string PromptText
        {
            get => _promptText;
            set
            {
                if (value == _promptText) return;
                _promptText = value;
                OnPropertyChangedWithValue(value, nameof(PromptText));
            }
        }

        [DataSourceProperty]
        public string InputText
        {
            get => _inputText;
            set
            {
                if (value == _inputText) return;
                _inputText = value;
                CanAsk = !string.IsNullOrWhiteSpace(value);
                OnPropertyChangedWithValue(value, nameof(InputText));
            }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value == _statusText) return;
                _statusText = value;
                OnPropertyChangedWithValue(value, nameof(StatusText));
            }
        }

        [DataSourceProperty]
        public string RecordButtonText
        {
            get => _recordButtonText;
            set
            {
                if (value == _recordButtonText) return;
                _recordButtonText = value;
                OnPropertyChangedWithValue(value, nameof(RecordButtonText));
            }
        }

        [DataSourceProperty]
        public string RecordHintText
        {
            get => _recordHintText;
            set
            {
                if (value == _recordHintText) return;
                _recordHintText = value;
                OnPropertyChangedWithValue(value, nameof(RecordHintText));
            }
        }

        [DataSourceProperty]
        public string AskLabel => "Ask";

        [DataSourceProperty]
        public string CancelLabel => "Cancel";

        [DataSourceProperty]
        public bool CanAsk
        {
            get => _canAsk;
            set
            {
                if (value == _canAsk) return;
                _canAsk = value;
                OnPropertyChangedWithValue(value, nameof(CanAsk));
            }
        }

        [DataSourceProperty]
        public bool CanToggleRecording
        {
            get => _canToggleRecording;
            set
            {
                if (value == _canToggleRecording) return;
                _canToggleRecording = value;
                OnPropertyChangedWithValue(value, nameof(CanToggleRecording));
            }
        }

        [DataSourceProperty]
        public bool CanEditText
        {
            get => _canEditText;
            set
            {
                if (value == _canEditText) return;
                _canEditText = value;
                OnPropertyChangedWithValue(value, nameof(CanEditText));
            }
        }

        public void ExecuteAsk()
        {
            if (!string.IsNullOrWhiteSpace(InputText))
                _onAsk(InputText.Trim());
        }

        public void ExecuteCancel() => _onCancel();

        public void ExecuteToggleRecording() => _onToggleRecording();

        public void SetRecordingState(bool isRecording)
        {
            RecordButtonText = isRecording ? "Stop" : "Record";
            StatusText = isRecording
                ? "Recording... click Stop when you are done speaking."
                : "Click Record to dictate, or type your words below.";
            RecordHintText = $"Hotkey: hold {_hotkeyHint} as an alternative.";
            CanToggleRecording = true;
            CanEditText = !isRecording;
        }

        public void SetTranscribingState()
        {
            RecordButtonText = "Transcribing...";
            StatusText = "Transcribing your speech...";
            RecordHintText = "Please wait while the transcription is prepared.";
            CanToggleRecording = false;
            CanEditText = false;
        }

        public void SetTranscript(string transcript)
        {
            InputText = transcript;
            StatusText = "Review the transcription, edit if needed, then press Ask.";
            RecordButtonText = "Record";
            RecordHintText = $"Hotkey: hold {_hotkeyHint} as an alternative.";
            CanToggleRecording = true;
            CanEditText = true;
        }

        public void SetError(string message)
        {
            StatusText = message;
            RecordButtonText = "Record";
            RecordHintText = $"Hotkey: hold {_hotkeyHint} as an alternative.";
            CanToggleRecording = true;
            CanEditText = true;
        }

        public void SetHotkeyHint(string hotkey)
        {
            _hotkeyHint = string.IsNullOrWhiteSpace(hotkey) ? AIConfig.STTHotkey : hotkey.Trim();
            RecordHintText = $"Hotkey: hold {_hotkeyHint} as an alternative.";
        }
    }
}
