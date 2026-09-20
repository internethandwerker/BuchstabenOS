using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Security;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuchstabenOS.UI.Desktop.ViewModels;

public partial class ParentMenuViewModel : ViewModelBase
{

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

    // =========================================================
    // 4. SOFTWARE-UPDATES
    // =========================================================
    [ObservableProperty]
    private string _currentVersion = "1.0.0";

    [ObservableProperty]
    private string _updateStatusText = "Noch nicht geprüft";

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private bool _isDownloadingUpdate;

    [ObservableProperty]
    private double _downloadProgressPercent;

    [ObservableProperty]
    private bool _isUpdateReadyToRestart;

    [ObservableProperty]
    private string _updateChangelog = string.Empty;

    [ObservableProperty]
    private string _latestAvailableVersion = string.Empty;

    [ObservableProperty]
    private string _updateErrorMessage = string.Empty;

    private UpdateInfo? _pendingUpdate;
    private readonly IWordDictionaryRepository _dictionaryRepo;
    private readonly ISystemControl _systemControl;
    private readonly ISettingsRepository _settingsRepo;
    private readonly WordDetector _wordDetector;
    private readonly IUpdateService? _updateService;
    private AppSettings _settings = new();

    public event Action? CloseRequested;
    public event Action<SpeechMode>? SpeechModeChanged;
    public event Action<string>? GameSwitchRequested;
    public event Action<AppSettings>? SettingsUpdated;

    public ParentMenuViewModel(
        IWordDictionaryRepository dictionaryRepo,
        ISystemControl systemControl,
        ISettingsRepository settingsRepo,
        WordDetector wordDetector,
        IUpdateService? updateService = null)
    {
        _dictionaryRepo = dictionaryRepo ?? throw new ArgumentNullException(nameof(dictionaryRepo));
        _systemControl = systemControl ?? throw new ArgumentNullException(nameof(systemControl));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
        _wordDetector = wordDetector ?? throw new ArgumentNullException(nameof(wordDetector));
        _updateService = updateService;

        _volume = _systemControl.GetSystemVolume();
        _currentVersion = _updateService?.CurrentVersion ?? "1.0.0";
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
            _wordDetector.AddWord(normalized);
            StatusNotification = $"Wort '{normalized}' hinzugefügt & sofort gespeichert!";
        }
        NewWordInput = string.Empty;
    }

    [RelayCommand]
    public async Task RemoveWordAsync(string word)
    {
        string normalized = word.Trim().ToUpperInvariant();
        if (GameWords.Remove(normalized))
        {
            await _dictionaryRepo.RemoveWordForGameAsync("free-typing", normalized);
            _wordDetector.RemoveWord(normalized);
            StatusNotification = $"Wort '{normalized}' gelöscht.";
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
        CloseRequested?.Invoke();
        ResetState();
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

    // --- Software-Update Befehle ---
    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (_updateService == null) return;

        IsCheckingForUpdates = true;
        UpdateStatusText = "Suche nach Updates...";
        UpdateErrorMessage = string.Empty;

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();
            if (result.IsUpdateAvailable && result.UpdateInfo != null)
            {
                _pendingUpdate = result.UpdateInfo;
                IsUpdateAvailable = true;
                LatestAvailableVersion = result.UpdateInfo.Version;
                UpdateChangelog = result.UpdateInfo.ChangelogMarkdown;
                UpdateStatusText = $"Update auf v{result.UpdateInfo.Version} verfügbar!";
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                IsUpdateAvailable = false;
                UpdateErrorMessage = result.ErrorMessage;
                UpdateStatusText = "Update-Prüfung fehlgeschlagen.";
            }
            else
            {
                IsUpdateAvailable = false;
                UpdateStatusText = $"BuchstabenOS ist auf dem neuesten Stand (v{CurrentVersion}).";
            }
        }
        catch (Exception ex)
        {
            UpdateErrorMessage = ex.Message;
            UpdateStatusText = "Fehler bei der Suche.";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    public async Task InstallUpdateAsync()
    {
        if (_updateService == null || _pendingUpdate == null) return;

        IsDownloadingUpdate = true;
        DownloadProgressPercent = 0;
        UpdateStatusText = "Lade Update herunter...";
        UpdateErrorMessage = string.Empty;

        var progress = new Progress<double>(p =>
        {
            DownloadProgressPercent = Math.Round(p, 1);
            UpdateStatusText = $"Lade Update herunter: {DownloadProgressPercent:F0}%";
        });

        try
        {
            bool success = await _updateService.DownloadAndApplyUpdateAsync(_pendingUpdate, progress);
            if (success)
            {
                IsUpdateAvailable = false;
                IsUpdateReadyToRestart = true;
                UpdateStatusText = $"Update auf v{_pendingUpdate.Version} erfolgreich installiert! Bitte neu starten.";
                StatusNotification = "Update installiert! Klicke auf 'Jetzt neu starten'.";
            }
        }
        catch (Exception ex)
        {
            UpdateErrorMessage = $"Installationsfehler: {ex.Message}";
            UpdateStatusText = "Installation fehlgeschlagen.";
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    [RelayCommand]
    public void RestartApp()
    {
        _updateService?.RestartApplication();
    }

    private async Task LoadWordsAsync()
    {
        GameWords.Clear();
        var words = await _dictionaryRepo.LoadWordsForGameAsync("free-typing");
        _wordDetector.LoadWords(words);
        foreach (var w in words)
        {
            GameWords.Add(w);
        }
    }
}
