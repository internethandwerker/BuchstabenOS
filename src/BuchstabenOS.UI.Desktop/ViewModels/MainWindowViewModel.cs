using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuchstabenOS.UI.Desktop.ViewModels;

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

    public async Task HandleKeyInputAsync(char keyChar, bool isSpace, bool isBackspace, bool ctrl, bool alt, bool shift)
    {
        // 1. Prüfe auf geheime Eltern-Tastenkombination (Ctrl + Alt + Shift + P)
        if (ctrl && alt && shift && (keyChar == 'p' || keyChar == 'P'))
        {
            ToggleParentOverlay();
            return;
        }

        // Wenn das Elternmenü geöffnet ist, gehen Tastatureingaben nicht an das Kinderspiel
        if (IsParentOverlayVisible) return;

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

        if (string.IsNullOrEmpty(DisplayText))
        {
            ChildHint = "Drücke eine Taste!";
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
                    // Nach 2,5 Sekunden Feier-Effekt wieder beruhigen
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
