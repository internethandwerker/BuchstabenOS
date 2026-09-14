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
    private int _volume = 80;

    [ObservableProperty]
    private bool _isPhoneticSpeechMode = true;

    [ObservableProperty]
    private string _newWordInput = string.Empty;

    [ObservableProperty]
    private string _statusNotification = string.Empty;

    public ObservableCollection<string> GameWords { get; } = new();

    public event Action<SpeechMode>? SpeechModeChanged;

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
        var parentPin = new ParentPin("1337"); // Standard-PIN oder aus Settings
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

    [RelayCommand]
    public void SetSpeechMode(string modeName)
    {
        if (modeName == "Phonetic")
        {
            IsPhoneticSpeechMode = true;
            SpeechModeChanged?.Invoke(SpeechMode.Phonetic);
        }
        else
        {
            IsPhoneticSpeechMode = false;
            SpeechModeChanged?.Invoke(SpeechMode.Alphabet);
        }
    }

    [RelayCommand]
    public void ChangeVolume(int delta)
    {
        Volume = Math.Clamp(Volume + delta, 0, 100);
        _systemControl.SetSystemVolume(Volume);
    }

    [RelayCommand]
    public void CloseMenu()
    {
        ResetState();
        _onCloseRequested.Invoke();
    }

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
