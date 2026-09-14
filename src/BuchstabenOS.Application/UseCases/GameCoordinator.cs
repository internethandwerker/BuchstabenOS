using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Application.UseCases;

/// <summary>
/// Orchestriert das Zusammenspiel zwischen aktuellem Spielmodul,
/// Audioausgabe (WAV-Samples für Buchstaben vs. TTS für Wörter)
/// und System-Events.
/// </summary>
public class GameCoordinator : IGameContext
{
    private readonly IGameRegistry _gameRegistry;
    private readonly IAudioPlayer _audioPlayer;
    private readonly ITtsEngine _ttsEngine;
    private IGameModule? _activeGame;

    public string ActiveGameId => _activeGame?.Metadata.Id ?? string.Empty;
    public IGameModule? ActiveGame => _activeGame;
    public SpeechMode CurrentSpeechMode { get; set; } = SpeechMode.Phonetic;

    public event Action<IDomainEvent>? EventPublished;

    public GameCoordinator(
        IGameRegistry gameRegistry,
        IAudioPlayer audioPlayer,
        ITtsEngine ttsEngine)
    {
        _gameRegistry = gameRegistry ?? throw new ArgumentNullException(nameof(gameRegistry));
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        _ttsEngine = ttsEngine ?? throw new ArgumentNullException(nameof(ttsEngine));
    }

    /// <summary>
    /// Startet ein bestimmtes Spiel anhand seiner ID.
    /// </summary>
    public async Task StartGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        var game = _gameRegistry.GetGameById(gameId)
            ?? throw new InvalidOperationException($"Spiel mit ID '{gameId}' wurde nicht gefunden.");

        _activeGame = game;
        await game.InitializeAsync(this, cancellationToken);
    }

    /// <summary>
    /// Nimmt einen Tastendruck entgegen, leitet ihn an das aktive Spiel weiter
    /// und führt die resultierenden Audio-Reaktionen asynchron aus.
    /// </summary>
    public async ValueTask<GameInputResult> HandleInputAsync(GameInput input)
    {
        if (_activeGame == null) return GameInputResult.Unhandled;

        var result = await _activeGame.ProcessInputAsync(input);

        // Verarbeite alle erzeugten Ereignisse für Audio und Effekte
        foreach (var ev in result.EmittedEvents)
        {
            await ProcessDomainEventAsync(ev);
        }

        return result;
    }

    /// <summary>
    /// Führt die konkreten Audio-Reaktionen auf Domänen-Ereignisse aus.
    /// </summary>
    public void PublishEvent(IDomainEvent domainEvent)
    {
        EventPublished?.Invoke(domainEvent);
    }

    private async Task ProcessDomainEventAsync(IDomainEvent domainEvent)
    {
        PublishEvent(domainEvent);

        switch (domainEvent)
        {
            case LetterTypedEvent letterEvent when letterEvent.IsLetter:
                // Stufe 1: Sofortiges pre-rendered Sample abspielen
                await _audioPlayer.PlayLetterAsync(letterEvent.Character, CurrentSpeechMode);
                break;

            case WordRecognizedEvent wordEvent:
                // Stufe 2: Wort erkannt! Kurzer Jingle + Piper TTS Aussprache
                await _audioPlayer.PlayJingleAsync("word_success");
                await _ttsEngine.SpeakWordAsync(wordEvent.Word);
                break;

            case SpacePressedEvent spaceEvent when !string.IsNullOrWhiteSpace(spaceEvent.RawWord):
                // Leertaste: Vorlesen des gebildeten Wortes (auch Quatschwörter)
                await _ttsEngine.SpeakWordAsync(spaceEvent.RawWord);
                break;
        }
    }
}
