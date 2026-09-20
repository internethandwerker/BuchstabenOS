using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Security;
using BuchstabenOS.Domain.Model.Typing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuchstabenOS.UI.Desktop.ViewModels;

public partial class ParentMenuViewModel : ViewModelBase
{
    private readonly IWordDictionaryRepository _dictionaryRepo;
    private readonly ISystemControl _systemControl;
    private readonly ISettingsRepository _settingsRepo;
    private readonly Action _onCloseRequested;
    private AppSettings _settings = new();

    [ObservableProperty]
    private string _pinInput = string.Empty;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string _pinErrorMessage = string.Empty;

    [ObservableProperty]
    private string _statusNotification = string.Empty;

    // =========================================================
    // 1. GRUNDLEGENDE EINSTELLUNGEN (Spiel-unabhängig)
    // =========================================================
    [ObservableProperty]
    private int _volume = 80;

    public ObservableCollection<string> AvailableAudioDevices { get; } = new()
    {
        "Standard (System-Standard PipeWire / PulseAudio)"
    };

    [ObservableProperty]
    private string _selectedAudioDevice = "Standard (System-Standard PipeWire / PulseAudio)";

    public ObservableCollection<string> AvailableThemes { get; } = new()
    {
        "Midnight Chalkboard (Dunkel / Standard)",
        "Montessori Warm (Geplant)",
        "Hoher Kontrast (Geplant)"
    };

    [ObservableProperty]
    private string _selectedTheme = "Midnight Chalkboard (Dunkel / Standard)";

    // =========================================================
    // 2. MODERATION (Strategie & Rotation)
    // =========================================================
    public ObservableCollection<string> AvailableModerationStrategies { get; } = new()
    {
        "Gewichteter Zufall (Würfel)",
        "KI-Adaptive Steuerung (In Vorbereitung)"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiceModeration))]
    private string _selectedModerationStrategy = "Gewichteter Zufall (Würfel)";

    public bool IsDiceModeration => SelectedModerationStrategy.Contains("Würfel");

    [ObservableProperty]
    private bool _autoGameSwitching = true;

    [ObservableProperty]
    private int _mathWeight = 50;

    [ObservableProperty]
    private int _typingWeight = 50;

    // =========================================================
    // 3. SPIELE (Aktiver Status & Spezifische Einstellungen)
    // =========================================================
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFreeTypingActive))]
    [NotifyPropertyChangedFor(nameof(IsMathAdditionActive))]
    private string _activeGameId = "free-typing";

    public bool IsFreeTypingActive => ActiveGameId == "free-typing";
    public bool IsMathAdditionActive => ActiveGameId == "math-addition";

    // Buchstabenzauber-spezifisch:
    [ObservableProperty]
    private bool _isPhoneticSpeechMode = true;

    [ObservableProperty]
    private string _newWordInput = string.Empty;

    public ObservableCollection<string> GameWords { get; } = new();

    // Mathe-Addition-spezifisch:
    [ObservableProperty]
    private int _mathMaxSum = 10;

    public event Action<SpeechMode>? SpeechModeChanged;
    public event Action<string>? GameSwitchRequested;
    public event Action<AppSettings>? SettingsUpdated;

    public ParentMenuViewModel(
        IWordDictionaryRepository dictionaryRepo,
        ISystemControl systemControl,
        ISettingsRepository settingsRepo,
        Action onCloseRequested)
    {
        _dictionaryRepo = dictionaryRepo ?? throw new ArgumentNullException(nameof(dictionaryRepo));
        _systemControl = systemControl ?? throw new ArgumentNullException(nameof(systemControl));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _onCloseRequested = onCloseRequested ?? throw new ArgumentNullException(nameof(onCloseRequested));

        _volume = _systemControl.GetSystemVolume();
    }

    public async Task InitializeAsync()
    {
        _settings = await _settingsRepo.LoadSettingsAsync();
        IsPhoneticSpeechMode = _settings.SpeechMode == SpeechMode.Phonetic;
        Volume = _settings.VolumePercent;
        AutoGameSwitching = _settings.AutoGameSwitching;
        MathMaxSum = _settings.AdditionMaxSum;
        ActiveGameId = _settings.ActiveGameId;

        SelectedModerationStrategy = _settings.ModerationStrategy == "ai"
            ? "KI-Adaptive Steuerung (In Vorbereitung)"
            : "Gewichteter Zufall (Würfel)";

        if (_settings.GameWeights.TryGetValue("math-addition", out int mw))
        {
            MathWeight = mw;
        }
        if (_settings.GameWeights.TryGetValue("free-typing", out int tw))
        {
            TypingWeight = tw;
        }

        await LoadWordsAsync();
    }

    public void ResetState()
    {
        PinInput = string.Empty;
        IsAuthenticated = false;
        PinErrorMessage = string.Empty;
        StatusNotification = string.Empty;
    }

    [RelayCommand]
    public void SubmitPin()
    {
        var parentPin = new ParentPin("1337"); // Standard-PIN
        if (parentPin.Verify(PinInput))
        {
            IsAuthenticated = true;
            PinErrorMessage = string.Empty;
            PinInput = string.Empty;
        }
        else
        {
            PinErrorMessage = "Falsche PIN. Bitte erneut versuchen.";
            PinInput = string.Empty;
        }
    }

    // --- Grundlegende Einstellungen Commands ---
    [RelayCommand]
    public void ChangeVolume(int delta)
    {
        Volume = Math.Clamp(Volume + delta, 0, 100);
        _settings.VolumePercent = Volume;
        _systemControl.SetSystemVolume(Volume);
        _ = SaveSettingsAsync();
    }

    partial void OnVolumeChanged(int value)
    {
        if (_settings != null)
        {
            _settings.VolumePercent = value;
            _systemControl.SetSystemVolume(value);
        }
    }

    // --- Spiele: Umschalten ---
    [RelayCommand]
    public void SwitchActiveGame(string gameId)
    {
        ActiveGameId = gameId;
        _settings.ActiveGameId = gameId;
        GameSwitchRequested?.Invoke(gameId);
        StatusNotification = gameId == "math-addition"
            ? "Spiel gewechselt: Mathe-Addition ist jetzt aktiv!"
            : "Spiel gewechselt: Buchstaben-Zauber ist jetzt aktiv!";
        _ = SaveSettingsAsync();
    }

    // --- Buchstabenzauber-Einstellungen ---
    [RelayCommand]
    public void SetSpeechMode(string modeName)
    {
        if (modeName == "Phonetic")
        {
            IsPhoneticSpeechMode = true;
            _settings.SpeechMode = SpeechMode.Phonetic;
            SpeechModeChanged?.Invoke(SpeechMode.Phonetic);
        }
        else
        {
            IsPhoneticSpeechMode = false;
            _settings.SpeechMode = SpeechMode.Alphabet;
            SpeechModeChanged?.Invoke(SpeechMode.Alphabet);
        }
        StatusNotification = IsPhoneticSpeechMode ? "Lautieren aktiviert!" : "Alphabet-Modus aktiviert!";
        _ = SaveSettingsAsync();
    }

    [RelayCommand]
    public async Task AddWordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewWordInput)) return;

        string normalized = NewWordInput.Trim().ToUpperInvariant();
        if (!GameWords.Contains(normalized))
        {
            GameWords.Insert(0, normalized);
            await _dictionaryRepo.AddWordForGameAsync("free-typing", normalized);
            StatusNotification = $"Wort '{normalized}' hinzugefügt!";
        }
        NewWordInput = string.Empty;
    }

    [RelayCommand]
    public async Task RemoveWordAsync(string word)
    {
        if (GameWords.Remove(word))
        {
            await _dictionaryRepo.RemoveWordForGameAsync("free-typing", word);
            StatusNotification = $"Wort '{word}' entfernt.";
        }
    }

    // --- Moderation speichern ---
    [RelayCommand]
    public async Task SaveModerationSettingsAsync()
    {
        _settings.AutoGameSwitching = AutoGameSwitching;
        _settings.ModerationStrategy = SelectedModerationStrategy.Contains("KI") ? "ai" : "dice";
        _settings.GameWeights["math-addition"] = MathWeight;
        _settings.GameWeights["free-typing"] = TypingWeight;

        await SaveSettingsAsync();
        StatusNotification = "Moderations-Einstellungen gespeichert!";
        SettingsUpdated?.Invoke(_settings);
    }

    // --- Mathe-Einstellungen speichern ---
    [RelayCommand]
    public async Task SaveMathSettingsAsync()
    {
        _settings.AdditionMaxSum = MathMaxSum;

        await SaveSettingsAsync();
        StatusNotification = $"Mathespiel: Rechnen bis {MathMaxSum} gespeichert!";
        SettingsUpdated?.Invoke(_settings);
    }

    // --- Alle Spiele-Einstellungen speichern ---
    [RelayCommand]
    public async Task SaveGameSettingsAsync()
    {
        _settings.AutoGameSwitching = AutoGameSwitching;
        _settings.ModerationStrategy = SelectedModerationStrategy.Contains("KI") ? "ai" : "dice";
        _settings.AdditionMaxSum = MathMaxSum;
        _settings.GameWeights["math-addition"] = MathWeight;
        _settings.GameWeights["free-typing"] = TypingWeight;

        await SaveSettingsAsync();
        StatusNotification = "Einstellungen gespeichert!";
        SettingsUpdated?.Invoke(_settings);
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsRepo.SaveSettingsAsync(_settings);
        }
        catch
        {
            // Best effort
        }
    }

    [RelayCommand]
    public void CloseMenu()
    {
        ResetState();
        _onCloseRequested.Invoke();
    }

    // --- Linux Systemeinstellungen ---
    [RelayCommand]
    public void ExitToDesktop()
    {
        _systemControl.ExitToDesktop();
    }

    [RelayCommand]
    public void PowerOff()
    {
        _systemControl.PowerOff();
    }

    [RelayCommand]
    public void Reboot()
    {
        _systemControl.Reboot();
    }

    private async Task LoadWordsAsync()
    {
        GameWords.Clear();
        var words = await _dictionaryRepo.LoadWordsForGameAsync("free-typing");
        foreach (var w in words)
        {
            GameWords.Add(w);
        }
    }
}
