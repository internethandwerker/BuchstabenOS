using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Application.UseCases;

/// <summary>
/// Orchestriert das Zusammenspiel zwischen dem aktuellem Spielmodul,
/// Audioausgabe (WAV-Samples für Buchstaben vs. TTS für Wörter & Mathe-Aufgaben),
/// Spiele-Moderator (Aufmerksamkeitsspanne & Wechsel) und System-Events.
/// </summary>
public class GameCoordinator : IGameContext
{
    private readonly IGameRegistry _gameRegistry;
    private readonly IAudioPlayer _audioPlayer;
    private readonly ITtsEngine _ttsEngine;
    private readonly IGameModerator? _moderator;
    private IGameModule? _activeGame;
    private bool _isSwitchScheduled;

    public string ActiveGameId => _activeGame?.Metadata.Id ?? string.Empty;
    public IGameModule? ActiveGame => _activeGame;
    public IRenderableGame? ActiveRenderableGame => _activeGame as IRenderableGame;
    public SpeechMode CurrentSpeechMode { get; set; } = SpeechMode.Phonetic;

    public event Action<IDomainEvent>? EventPublished;
    public event Action<IGameModule>? ActiveGameChanged;

    public GameCoordinator(
        IGameRegistry gameRegistry,
        IAudioPlayer audioPlayer,
        ITtsEngine ttsEngine,
        IGameModerator? moderator = null)
    {
        _gameRegistry = gameRegistry ?? throw new ArgumentNullException(nameof(gameRegistry));
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        _ttsEngine = ttsEngine ?? throw new ArgumentNullException(nameof(ttsEngine));
        _moderator = moderator;
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
        ActiveGameChanged?.Invoke(game);

        if (game is AdditionGameModule mathGame)
        {
            await Task.Delay(300, cancellationToken);
            await _ttsEngine.SpeakWordAsync(mathGame.CurrentTaskSpokenPrompt, cancellationToken);
        }
    }

    /// <summary>
    /// Wechselt sanft zu einem neuen Spiel, spielt eine Begrüßungs-Ansage ab
    /// und initialisiert die neue Spiel-Bühne.
    /// </summary>
    public async Task SwitchGameAsync(string nextGameId, CancellationToken cancellationToken = default)
    {
        var nextGame = _gameRegistry.GetGameById(nextGameId);
        if (nextGame == null) return;

        // Bisherige Sprachausgabe abbrechen, um sauberen Übergang zu garantieren
        _ttsEngine.StopCurrentSpeech();

        string introPrompt = _moderator?.GetGameIntroPrompt(nextGameId) ?? "Neues Spiel!";

        _activeGame = nextGame;
        await nextGame.InitializeAsync(this, cancellationToken);
        ActiveGameChanged?.Invoke(nextGame);

        // Begrüßung vorlesen (wartet dank Queue nun, bis sie fertig gesprochen ist)
        await _ttsEngine.SpeakWordAsync(introPrompt, cancellationToken);

        if (nextGame is AdditionGameModule mathGame)
        {
            await Task.Delay(250, cancellationToken);
            await _ttsEngine.SpeakWordAsync(mathGame.CurrentTaskSpokenPrompt, cancellationToken);
        }
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

        // Falls eine Matheaufgabe gelöst wurde und wir weiterhin im Mathespiel bleiben:
        // Neue Aufgabe nach dem Lob vorlesen!
        if (!_isSwitchScheduled && _activeGame is AdditionGameModule mathGame && result.EmittedEvents.OfType<MathTaskSolvedEvent>().Any())
        {
            await Task.Delay(350);
            await _ttsEngine.SpeakWordAsync(mathGame.CurrentTaskSpokenPrompt);
        }

        return result;
    }

    /// <summary>
    /// Führt die konkreten Audio-Reaktionen auf Domänen-Ereignisse aus.
    /// </summary>
    public void PublishEvent(IDomainEvent domainEvent)
    {
        EventPublished?.Invoke(domainEvent);

        if (domainEvent is MathTaskGeneratedEvent generatedEvent)
        {
            _ = _ttsEngine.SpeakWordAsync(generatedEvent.SpokenPrompt);
        }
    }

    private async Task ProcessDomainEventAsync(IDomainEvent domainEvent)
    {
        PublishEvent(domainEvent);

        switch (domainEvent)
        {
            case LetterTypedEvent letterEvent when letterEvent.IsLetter:
                // Sofortiges vorgerendertes Laut-Sample abspielen
                await _audioPlayer.PlayLetterAsync(letterEvent.Character, CurrentSpeechMode);
                break;

            case WordRecognizedEvent wordEvent:
                // Wort erkannt! Jingle + TTS
                await _audioPlayer.PlayJingleAsync("word_success");
                await _ttsEngine.SpeakWordAsync(wordEvent.Word);
                break;

            case SpacePressedEvent spaceEvent when !string.IsNullOrWhiteSpace(spaceEvent.RawWord):
                // Leertaste: Vorlesen des gebildeten Wortes (auch Quatschwörter)
                await _ttsEngine.SpeakWordAsync(spaceEvent.RawWord);
                break;

            case MathTaskSolvedEvent solvedEvent:
                // Richtig gerechnet! Jingle + Lob
                await _audioPlayer.PlayJingleAsync("word_success");
                await _ttsEngine.SpeakWordAsync(solvedEvent.SpokenPraise);
                break;

            case MathTaskFailedEvent failedEvent:
                // Falscheingabe: Liebevolles Feedback
                await _ttsEngine.SpeakWordAsync(failedEvent.SpokenEncouragement);
                break;

            case WordTemplateChallengeStartedEvent templateStarted:
                // Vorlage erscheint: Freundliche Aufforderung über die Audio-Queue
                await _ttsEngine.SpeakWordAsync(templateStarted.SpokenPrompt);
                break;

            case WordTemplateLetterMatchedEvent matchedLetter:
                // Buchstabe getroffen: Sofortigen phonetischen Laut abspielen
                await _audioPlayer.PlayLetterAsync(matchedLetter.Letter, CurrentSpeechMode);
                break;

            case WordTemplateMistakeEvent:
                // Falsche Taste: Kurzer sanfter Fehlerton
                await _audioPlayer.PlayJingleAsync("wrong");
                break;

            case WordTemplateChallengeCompletedEvent completedTemplate:
                // Vorlage gemeistert! Jingle + Lob
                await _audioPlayer.PlayJingleAsync("word_success");
                await _ttsEngine.SpeakWordAsync($"Klasse! Du hast {completedTemplate.TargetWord} gezaubert!");
                break;

            case WordTemplateChallengeSkippedEvent:
                // Kind überspringt per Leertaste: Liebevolles Quittieren ohne Druck
                int skipIdx = Random.Shared.Next(FreeTypingGameModule.SkipPhrases.Length);
                await _ttsEngine.SpeakWordAsync(FreeTypingGameModule.SkipPhrases[skipIdx]);
                break;

            case GameRoundCompletedEvent roundEv when _moderator != null:
                // Prüfe Aufmerksamkeitsspanne für automatischen Spielwechsel
                var score = _activeGame?.GetScore() ?? KnowledgeScore.Empty;
                if (_moderator.RecordRoundCompleted(roundEv.GameId, roundEv.TotalCompletedRounds, score.PlayTime))
                {
                    string nextGameId = _moderator.SelectNextGame(roundEv.GameId, _gameRegistry.GetAllGames());
                    if (!string.IsNullOrEmpty(nextGameId) && !nextGameId.Equals(roundEv.GameId, StringComparison.OrdinalIgnoreCase))
                    {
                        _isSwitchScheduled = true;
                        // Sanfte Verzögerung, damit die Erfolgs-Animation erst gefeiert werden kann
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(2600);
                            _isSwitchScheduled = false;
                            await SwitchGameAsync(nextGameId);
                        });
                    }
                }
                break;
        }
    }
}
