using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuchstabenOS.UI.Desktop.ViewModels;

/// <summary>
/// Modelliert eine nach oben schwebende, verblassende Historienzeile.
/// </summary>
public record HistoryLineItem(string Text, double Opacity, double FontSize);

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameCoordinator _gameCoordinator;
    private IRenderableGame? _activeRenderableGame;

    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private double _fontSize = 240.0;

    [ObservableProperty]
    private bool _isWordCelebrationActive;

    [ObservableProperty]
    private string _celebratedWord = string.Empty;

    [ObservableProperty]
    private bool _isWrongFeedbackActive;

    [ObservableProperty]
    private bool _isParentOverlayVisible;

    [ObservableProperty]
    private string _childHint = "Tippe etwas auf der Tastatur!";

    public ObservableCollection<HistoryLineItem> FloatingHistoryLines { get; } = new();

    public ParentMenuViewModel ParentMenu { get; }

    public MainWindowViewModel(
        GameCoordinator gameCoordinator,
        ParentMenuViewModel parentMenu)
    {
        _gameCoordinator = gameCoordinator ?? throw new ArgumentNullException(nameof(gameCoordinator));
        ParentMenu = parentMenu ?? throw new ArgumentNullException(nameof(parentMenu));

        ParentMenu.CloseRequested += () =>
        {
            IsParentOverlayVisible = false;
        };

        ParentMenu.SpeechModeChanged += mode =>
        {
            _gameCoordinator.CurrentSpeechMode = mode;
        };

        ParentMenu.GameSwitchRequested += async gameId =>
        {
            await _gameCoordinator.SwitchGameAsync(gameId);
        };

        _gameCoordinator.ActiveGameChanged += OnActiveGameChanged;
        _gameCoordinator.EventPublished += OnDomainEventPublished;
    }

    public async Task InitializeAsync()
    {
        await _gameCoordinator.StartGameAsync("free-typing");
        await ParentMenu.InitializeAsync();
        AttachToActiveGame(_gameCoordinator.ActiveRenderableGame);
    }

    private void OnActiveGameChanged(IGameModule newGame)
    {
        Dispatcher.UIThread.Post(() =>
        {
            AttachToActiveGame(newGame as IRenderableGame);
        });
    }

    private void AttachToActiveGame(IRenderableGame? renderableGame)
    {
        if (_activeRenderableGame != null)
        {
            _activeRenderableGame.ViewStateChanged -= OnActiveGameStateChanged;
        }

        _activeRenderableGame = renderableGame;

        if (_activeRenderableGame != null)
        {
            _activeRenderableGame.ViewStateChanged += OnActiveGameStateChanged;
        }

        UpdateDisplay();
    }

    private void OnActiveGameStateChanged()
    {
        Dispatcher.UIThread.Post(UpdateDisplay);
    }

    public async Task HandleKeyInputAsync(char keyChar, bool isSpace, bool isBackspace, bool ctrl, bool alt, bool shift, bool isEnter = false)
    {
        // 1. Prüfe auf geheime Eltern-Tastenkombination (Ctrl + Alt + Shift + P)
        if (ctrl && alt && shift && (keyChar == 'p' || keyChar == 'P'))
        {
            ToggleParentOverlay();
            return;
        }

        if (IsParentOverlayVisible) return;

        char keyToSend = isEnter ? '\n' : keyChar;
        var input = new GameInput(keyToSend, isSpace, isBackspace, ctrl, alt, shift, DateTime.UtcNow);
        await _gameCoordinator.HandleInputAsync(input);

        UpdateDisplay();
    }

    private CancellationTokenSource? _celebrationCts;
    private CancellationTokenSource? _wrongFeedbackCts;

    public void ToggleParentOverlay()
    {
        IsParentOverlayVisible = !IsParentOverlayVisible;
        ParentMenu.ResetState();
    }

    private void UpdateDisplay()
    {
        if (_activeRenderableGame == null)
        {
            DisplayText = string.Empty;
            FontSize = 240.0;
            ChildHint = "Spiel wird geladen...";
            IsWordCelebrationActive = false;
            IsWrongFeedbackActive = false;
            FloatingHistoryLines.Clear();
            return;
        }

        DisplayText = _activeRenderableGame.DisplayText;
        FontSize = _activeRenderableGame.CurrentFontSizePoints;
        
        // Falls das Spiel eine Feier meldet und unser UI-Timer noch nicht läuft:
        if (_activeRenderableGame.IsCelebrating && !IsWordCelebrationActive)
        {
            TriggerCelebration(_activeRenderableGame.CelebrationMessage);
        }

        FloatingHistoryLines.Clear();
        var completed = _activeRenderableGame.CompletedLines;
        int count = completed.Count;

        // Die Zeilen schweben über der Bildschirmmitte nach oben und verblassen sanft:
        // Zeile direkt über der Mitte: Opacity 0.55, 42pt
        // Zeile 2 darüber:             Opacity 0.28, 34pt
        // Zeile 3 darüber:             Opacity 0.12, 26pt
        // Zeile 4 ganz oben:           Opacity 0.05, 20pt
        for (int i = 0; i < count; i++)
        {
            int distanceFromCurrent = count - i;
            double opacity = distanceFromCurrent switch
            {
                1 => 0.55,
                2 => 0.28,
                3 => 0.12,
                _ => 0.05
            };

            double lineSize = distanceFromCurrent switch
            {
                1 => 44.0,
                2 => 34.0,
                3 => 26.0,
                _ => 20.0
            };

            FloatingHistoryLines.Add(new HistoryLineItem(completed[i], opacity, lineSize));
        }

        if (string.IsNullOrEmpty(DisplayText) && count == 0)
        {
            ChildHint = _activeRenderableGame.HintText;
        }
        else
        {
            ChildHint = string.Empty;
        }
    }

    public void TriggerCelebration(string message, int durationMs = 3200)
    {
        // 1. Vorherigen Timer abbrechen (falls noch aktiv) -> Überschreiben
        _celebrationCts?.Cancel();
        _celebrationCts?.Dispose();
        _celebrationCts = new CancellationTokenSource();
        var token = _celebrationCts.Token;

        CelebratedWord = message;
        IsWordCelebrationActive = true;

        Task.Delay(durationMs, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsWordCelebrationActive = false;
                    CelebratedWord = string.Empty;
                    if (_activeRenderableGame is FreeTypingGameModule ftm)
                    {
                        ftm.ClearCelebration();
                    }
                });
            }
        }, TaskScheduler.Default);
    }

    public void TriggerWrongFeedback(int durationMs = 1400)
    {
        _wrongFeedbackCts?.Cancel();
        _wrongFeedbackCts?.Dispose();
        _wrongFeedbackCts = new CancellationTokenSource();
        var token = _wrongFeedbackCts.Token;

        IsWrongFeedbackActive = true;

        Task.Delay(durationMs, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    IsWrongFeedbackActive = false;
                });
            }
        }, TaskScheduler.Default);
    }

    private void OnDomainEventPublished(IDomainEvent domainEvent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            switch (domainEvent)
            {
                case WordRecognizedEvent wordEvent:
                    TriggerCelebration($"Wort gezaubert: {wordEvent.Word}!");
                    break;

                case MathTaskSolvedEvent solvedEvent:
                    TriggerCelebration($"{solvedEvent.TaskText} ⭐");
                    break;

                case MathTaskFailedEvent:
                    TriggerWrongFeedback();
                    break;

                case StageResetEvent:
                    _celebrationCts?.Cancel();
                    IsWordCelebrationActive = false;
                    CelebratedWord = string.Empty;
                    _wrongFeedbackCts?.Cancel();
                    IsWrongFeedbackActive = false;
                    UpdateDisplay();
                    break;
            }
        });
    }
}
