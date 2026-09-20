using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Application.Ports;

/// <summary>
/// Port für das sofortige Abspielen vorgerenderter Audio-Samples (Buchstaben/Laute).
/// Latenz-Ziel: < 10 ms.
/// </summary>
public interface IAudioPlayer
{
    ValueTask PlayLetterAsync(char letter, SpeechMode mode, CancellationToken cancellationToken = default);
    ValueTask PlayJingleAsync(string jingleName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port für die dynamische Sprachsynthese ganzer Wörter (Piper TTS oder lokaler Fallback).
/// </summary>
public interface ITtsEngine
{
    Task SpeakWordAsync(string word, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
    void StopCurrentSpeech() { }
}

/// <summary>
/// Port für die Ablage und das Laden von Wörterlisten.
/// Der Scope ist explizit PRO SPIEL (z. B. "free-typing", "tier-quiz").
/// </summary>
public interface IWordDictionaryRepository
{
    Task<IReadOnlyList<string>> LoadWordsForGameAsync(string gameId, CancellationToken cancellationToken = default);
    Task AddWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default);
    Task RemoveWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default);
}

/// <summary>
/// Port für Linux-Betriebssystem-Aktionen im Kiosk-Modus.
/// </summary>
public interface ISystemControl
{
    void ExitToDesktop();
    void PowerOff();
    void Reboot();
    void SetSystemVolume(int percent);
    int GetSystemVolume();
}

/// <summary>
/// Port für die persistente Speicherung der Einstellungen.
/// </summary>
public interface ISettingsRepository
{
    Task<Configuration.AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(Configuration.AppSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry für alle installierten Spielmodule.
/// Ermöglicht Plugin-Architektur, Sequenzierung und semantische Abfrage durch Agenten.
/// </summary>
public interface IGameRegistry
{
    IReadOnlyList<IGameModule> GetAllGames();
    IGameModule? GetGameById(string gameId);
    void RegisterGame(IGameModule game);
}
