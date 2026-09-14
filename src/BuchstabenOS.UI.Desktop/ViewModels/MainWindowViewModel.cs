using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace BuchstabenOS.UI.Desktop.ViewModels;

/// <summary>
/// Modelliert eine nach oben schwebende, verblassende Historienzeile.
/// </summary>
public record HistoryLineItem(string Text, double Opacity, double FontSize);

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameCoordinator _gameCoordinator;
    private readonly FreeTypingGameModule _freeTypingGame;

    [ObservableProperty]
    private string _displayText = string.Empty;

    [ObservableProperty]
    private double _fontSize = 240.0;

    [ObservableProperty]
    private bool _isWordCelebrationActive;

    [ObservableProperty]
    private string _celebratedWord = string.Empty;

    [ObservableProperty]
    private bool _isParentOverlayVisible;

    [ObservableProperty]
    private string _childHint = "Tippe einen Buchstaben!";

    public ObservableCollection<HistoryLineItem> FloatingHistoryLines { get; } = new();

    public ParentMenuViewModel ParentMenu { get; }

    public MainWindowViewModel(
        GameCoordinator gameCoordinator,
        FreeTypingGameModule freeTypingGame,
        ParentMenuViewModel parentMenu)
    {
        _gameCoordinator = gameCoordinator ?? throw new ArgumentNullException(nameof(gameCoordinator));
        _freeTypingGame = freeTypingGame ?? throw new ArgumentNullException(nameof(freeTypingGame));
        ParentMenu = parentMenu ?? throw new ArgumentNullException(nameof(parentMenu));

        ParentMenu.SpeechModeChanged += mode =>
        {
            _gameCoordinator.CurrentSpeechMode = mode;
            _freeTypingGame.Stage.SpeechMode = mode;
        };

        _gameCoordinator.EventPublished += OnDomainEventPublished;
    }

    public async Task InitializeAsync()
    {
        await _gameCoordinator.StartGameAsync("free-typing");
        await ParentMenu.InitializeAsync();
        UpdateDisplayFromStage();
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

        if (isEnter)
        {
            _freeTypingGame.Stage.CommitLine();
            UpdateDisplayFromStage();
            return;
        }

        var input = new GameInput(keyChar, isSpace, isBackspace, ctrl, alt, shift, DateTime.UtcNow);
        await _gameCoordinator.HandleInputAsync(input);

        UpdateDisplayFromStage();
    }

    public void ToggleParentOverlay()
    {
        IsParentOverlayVisible = !IsParentOverlayVisible;
        if (IsParentOverlayVisible)
        {
            ParentMenu.ResetState();
        }
    }

    private void UpdateDisplayFromStage()
    {
        DisplayText = _freeTypingGame.Stage.CurrentText;
        FontSize = _freeTypingGame.Stage.CurrentFontSize.Points;

        FloatingHistoryLines.Clear();
        var completed = _freeTypingGame.Stage.CompletedLines;
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
            ChildHint = "Tippe einen Buchstaben auf der Tastatur!";
        }
        else
        {
            ChildHint = string.Empty;
        }
    }

    private void OnDomainEventPublished(IDomainEvent domainEvent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            switch (domainEvent)
            {
                case WordRecognizedEvent wordEvent:
                    CelebratedWord = wordEvent.Word;
                    IsWordCelebrationActive = true;
                    Task.Delay(2500).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() => IsWordCelebrationActive = false);
                    });
                    break;

                case StageResetEvent:
                    IsWordCelebrationActive = false;
                    CelebratedWord = string.Empty;
                    break;
            }
        });
    }
}
