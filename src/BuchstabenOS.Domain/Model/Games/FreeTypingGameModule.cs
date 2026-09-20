using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;

namespace BuchstabenOS.Domain.Model.Games;

/// <summary>
/// Das erste Spiel von BuchstabenOS: "Freies Buchstaben-Zaubern".
/// Moritz tippt beliebige Tasten, hört sofort den Laut/Buchstaben,
/// sieht die Schrift dynamisch wachsen und schrumpfen und erfährt
/// die magische Worterkennung, wenn ein echtes deutsches Wort entsteht.
/// </summary>
public class FreeTypingGameModule : IGameModule, IRenderableGame, IGameConfigurable
{
    public static readonly string[] ChallengePromptTemplates =
    [
        "Schau mal: Kannst du [word] zaubern?",
        "Probier mal das Wort [word]!",
        "Hier ist ein Zauberwort für dich: [word]!",
        "Kannst du [word] schreiben?",
        "Tippe mal die Buchstaben für [word]!"
    ];

    public static readonly string[] SkipPhrases =
    [
        "Kein Problem, wir machen das ein anderes Mal!",
        "Alles klar, zaubere einfach weiter!",
        "Kein Ding, schreib worauf du Lust hast!",
        "Okay, du bist der Chef!"
    ];

    private readonly Random _random = new();
    private IGameContext? _context;
    private int _successfulWords;
    private readonly HashSet<char> _exploredLetters = new();
    private int _interactionCount;
    private DateTime _sessionStartTime = DateTime.UtcNow;

    // Konfiguration
    private bool _templatesEnabled = true;
    private int _templateIntervalKeystrokes = 25;
    private int _keystrokesSinceLastChallenge = 0;
    private int _nextTriggerThreshold = 25;

    public TextStage Stage { get; }

    // IRenderableGame
    public string DisplayText => Stage.CurrentText;
    public double CurrentFontSizePoints => Stage.CurrentFontSize.Points;
    public string HintText => IsTemplateChallengeActive
        ? "Tippe die Buchstaben ab oder drücke Leertaste zum Überspringen!"
        : "Tippe einen Buchstaben auf der Tastatur!";
    public IReadOnlyList<string> CompletedLines => Stage.CompletedLines;
    public bool IsCelebrating { get; private set; }
    public string CelebrationMessage { get; private set; } = string.Empty;
    public bool IsWrongFeedback => IsTemplateMistake;

    // Template Challenge State
    public bool IsTemplateChallengeActive { get; private set; }
    public string? TemplateTargetWord { get; private set; }
    public int TemplateProgressIndex { get; private set; }
    public bool IsTemplateMistake { get; private set; }

    public event Action? ViewStateChanged;

    public GameMetadata Metadata { get; } = new(
        Id: "free-typing",
        Title: "Buchstaben-Zauber (Freies Tippen)",
        Description: "Erkunde die Tastatur! Jeder Buchstabe spricht sofort zu dir. Finde geheime Wörter wie MAMA, PAPA oder deinen Namen MORITZ!",
        Category: "Sprache & Laute",
        Age: new AgeRecommendation(3, 6),
        TargetSkills: new[]
        {
            PedagogicalSkill.PhonemicAwareness,
            PedagogicalSkill.LetterRecognition,
            PedagogicalSkill.KeyboardCoordination,
            PedagogicalSkill.WordSynthesis,
            PedagogicalSkill.FreeExploration
        },
        Prerequisites: new[]
        {
            new SkillPrerequisite("TasteDruecken", "Freude daran, Tasten auf der Tastatur zu drücken.")
        },
        LearningObjectives: new[]
        {
            "Verbindung zwischen Tastensymbol und Sprachlaut verinnerlichen (Lautieren).",
            "Entdecken von Wortgrenzen (Leertaste).",
            "Erleben von Selbstwirksamkeit durch sofortige akustische Resonanz."
        },
        DifficultyLevel: 1,
        DefaultAttentionSpanTasks: 5,
        DefaultAttentionSpanDuration: TimeSpan.FromMinutes(5),
        DefaultWeight: 50
    );

    public bool IsActive { get; private set; }

    public FreeTypingGameModule(TextStage stage)
    {
        Stage = stage ?? throw new ArgumentNullException(nameof(stage));
    }

    public Task InitializeAsync(IGameContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        IsActive = true;
        _sessionStartTime = DateTime.UtcNow;
        _nextTriggerThreshold = RollNextThreshold();
        return Task.CompletedTask;
    }

    public ValueTask<GameInputResult> ProcessInputAsync(GameInput input)
    {
        if (!IsActive) return ValueTask.FromResult(GameInputResult.Unhandled);

        _interactionCount++;

        // =====================================================================
        // FALL A: Interaktive Wortvorlage (Lern-Impuls) ist aktiv!
        // =====================================================================
        if (IsTemplateChallengeActive && !string.IsNullOrEmpty(TemplateTargetWord))
        {
            // 1. Kind drückt Leertaste -> Sanfter Skip (pädagogische Freiwilligkeit)
            if (input.IsSpace)
            {
                string skippedWord = TemplateTargetWord;
                IsTemplateChallengeActive = false;
                TemplateTargetWord = null;
                TemplateProgressIndex = 0;
                IsTemplateMistake = false;
                _keystrokesSinceLastChallenge = 0;
                _nextTriggerThreshold = RollNextThreshold();

                var skipEv = new WordTemplateChallengeSkippedEvent(skippedWord);
                _context?.PublishEvent(skipEv);
                ViewStateChanged?.Invoke();
                return ValueTask.FromResult(new GameInputResult(true, new[] { skipEv }));
            }

            // 2. Buchstabeneingabe
            if (input.KeyChar != '\0' && !input.IsBackspace)
            {
                char typed = char.ToUpperInvariant(input.KeyChar);
                char expected = TemplateTargetWord[TemplateProgressIndex];

                if (typed == expected)
                {
                    // Treffer! Buchstabe füllt sich aus
                    IsTemplateMistake = false;
                    TemplateProgressIndex++;
                    _exploredLetters.Add(typed);

                    var matchEv = new WordTemplateLetterMatchedEvent(TemplateTargetWord, TemplateProgressIndex - 1, typed);
                    var emittedEvents = new List<IDomainEvent> { matchEv };
                    _context?.PublishEvent(matchEv);

                    // Ist das ganze Wort vollständig ausgefüllt?
                    if (TemplateProgressIndex >= TemplateTargetWord.Length)
                    {
                        string completedWord = TemplateTargetWord;
                        _successfulWords++;
                        IsCelebrating = true;
                        CelebrationMessage = $"Wort gezaubert: {completedWord}!";

                        IsTemplateChallengeActive = false;
                        TemplateTargetWord = null;
                        TemplateProgressIndex = 0;
                        _keystrokesSinceLastChallenge = 0;
                        _nextTriggerThreshold = RollNextThreshold();

                        var compEv = new WordTemplateChallengeCompletedEvent(completedWord);
                        var wordEv = new WordRecognizedEvent(completedWord, true);
                        var roundEv = new GameRoundCompletedEvent(Metadata.Id, _successfulWords);

                        emittedEvents.Add(compEv);
                        emittedEvents.Add(wordEv);
                        emittedEvents.Add(roundEv);

                        _context?.PublishEvent(compEv);
                        _context?.PublishEvent(wordEv);
                        _context?.PublishEvent(roundEv);
                    }

                    ViewStateChanged?.Invoke();
                    return ValueTask.FromResult(new GameInputResult(true, emittedEvents));
                }
                else
                {
                    // Falscher Buchstabe: Fehler-Feedback, aber Puffer bleibt stehen
                    IsTemplateMistake = true;
                    var mistakeEv = new WordTemplateMistakeEvent(TemplateTargetWord, TemplateProgressIndex, typed);
                    _context?.PublishEvent(mistakeEv);
                    ViewStateChanged?.Invoke();
                    return ValueTask.FromResult(new GameInputResult(true, new[] { mistakeEv }));
                }
            }

            // Backspace oder sonstige Tasten
            IsTemplateMistake = false;
            ViewStateChanged?.Invoke();
            return ValueTask.FromResult(new GameInputResult(true, Array.Empty<IDomainEvent>()));
        }

        // =====================================================================
        // FALL B: Freier Erkundungs-Modus ("Buchstaben-Zauber")
        // =====================================================================
        double viewportWidth = 1366;
        double viewportHeight = 768;

        if (input.IsBackspace)
        {
            Stage.PressBackspace(viewportWidth, viewportHeight);
        }
        else if (input.IsSpace)
        {
            Stage.PressSpace(viewportWidth, viewportHeight);
        }
        else if (input.KeyChar == '\n' || input.KeyChar == '\r')
        {
            Stage.CommitLine();
        }
        else if (input.KeyChar != '\0')
        {
            char upperChar = char.ToUpperInvariant(input.KeyChar);
            _exploredLetters.Add(upperChar);
            Stage.TypeCharacter(input.KeyChar, viewportWidth, viewportHeight);

            if (char.IsLetter(upperChar))
            {
                _keystrokesSinceLastChallenge++;
            }
        }

        var events = Stage.DequeueEvents();
        var additionalEvents = new List<IDomainEvent>();

        // Prüfe auf Erfolgsereignisse für den Score
        foreach (var ev in events)
        {
            if (ev is WordRecognizedEvent wordEvent)
            {
                _successfulWords++;
                IsCelebrating = true;
                CelebrationMessage = $"Wort gezaubert: {wordEvent.Word}!";
                var roundEv = new GameRoundCompletedEvent(Metadata.Id, _successfulWords);
                additionalEvents.Add(roundEv);
                _context?.PublishEvent(roundEv);
            }
            else if (ev is StageResetEvent)
            {
                IsCelebrating = false;
                CelebrationMessage = string.Empty;
            }
            _context?.PublishEvent(ev);
        }

        // Prüfe, ob die Bedingungen für einen neuen Lern-Impuls erfüllt sind
        if (_templatesEnabled && !IsCelebrating && !IsTemplateChallengeActive &&
            _keystrokesSinceLastChallenge >= _nextTriggerThreshold)
        {
            var startEv = TriggerChallenge();
            if (startEv != null)
            {
                additionalEvents.Add(startEv);
            }
        }

        var allEvents = new List<IDomainEvent>(events);
        allEvents.AddRange(additionalEvents);

        ViewStateChanged?.Invoke();

        return ValueTask.FromResult(new GameInputResult(true, allEvents));
    }

    /// <summary>
    /// Startet manuell oder gesteuert eine Wortvorlagen-Challenge.
    /// </summary>
    public WordTemplateChallengeStartedEvent? TriggerChallenge(string? specificWord = null)
    {
        string? targetWord = specificWord ?? Stage.WordDetector.GetRandomWord(_random);
        if (string.IsNullOrWhiteSpace(targetWord) || targetWord.Length < 2) return null;

        targetWord = targetWord.Trim().ToUpperInvariant();
        IsTemplateChallengeActive = true;
        TemplateTargetWord = targetWord;
        TemplateProgressIndex = 0;
        IsTemplateMistake = false;
        _keystrokesSinceLastChallenge = 0;
        _nextTriggerThreshold = RollNextThreshold();

        Stage.Clear();

        string prompt = RollChallengePrompt(targetWord);
        var startEv = new WordTemplateChallengeStartedEvent(targetWord, prompt);
        _context?.PublishEvent(startEv);
        ViewStateChanged?.Invoke();
        return startEv;
    }

    private int RollNextThreshold()
    {
        int min = Math.Max(5, _templateIntervalKeystrokes - 6);
        int max = _templateIntervalKeystrokes + 7;
        return _random.Next(min, max);
    }

    private string RollChallengePrompt(string word)
    {
        int idx = _random.Next(ChallengePromptTemplates.Length);
        return ChallengePromptTemplates[idx].Replace("[word]", word, StringComparison.OrdinalIgnoreCase);
    }

    public KnowledgeScore GetScore()
    {
        return new KnowledgeScore(
            TotalInteractions: _interactionCount,
            SuccessfulWordsRecognized: _successfulWords,
            UniqueLettersExplored: _exploredLetters.Count,
            CurrentStreak: _successfulWords,
            PlayTime: DateTime.UtcNow - _sessionStartTime,
            LastPlayedAt: DateTime.UtcNow
        );
    }

    public void ClearCelebration()
    {
        IsCelebrating = false;
        CelebrationMessage = string.Empty;
        ViewStateChanged?.Invoke();
    }

    public void Reset()
    {
        Stage.Clear();
        _interactionCount = 0;
        _successfulWords = 0;
        _exploredLetters.Clear();
        IsCelebrating = false;
        CelebrationMessage = string.Empty;
        IsTemplateChallengeActive = false;
        TemplateTargetWord = null;
        TemplateProgressIndex = 0;
        IsTemplateMistake = false;
        _keystrokesSinceLastChallenge = 0;
        _nextTriggerThreshold = RollNextThreshold();
        _sessionStartTime = DateTime.UtcNow;
        ViewStateChanged?.Invoke();
    }

    // =========================================================================
    // IGameConfigurable (Elternmenü-Konfiguration)
    // =========================================================================

    public IReadOnlyList<GameConfigDescriptor> GetConfigurationDescriptors() =>
    [
        new GameConfigDescriptor(
            Key: "WordTemplatesEnabled",
            Label: "Wortvorlagen (Lern-Impulse)",
            Description: "Blendet in unregelmäßigen Abständen Wort-Konturen ein, die Moritz nach-tippen kann.",
            Type: GameConfigType.Toggle,
            DefaultValue: true
        ),
        new GameConfigDescriptor(
            Key: "WordTemplateInterval",
            Label: "Häufigkeit der Lern-Impulse",
            Description: "Durchschnittliche Anzahl freier Tastenanschläge zwischen zwei Wortvorlagen.",
            Type: GameConfigType.NumberSlider,
            MinValue: 10,
            MaxValue: 50,
            Step: 5,
            DefaultValue: 25
        )
    ];

    public void ApplyConfiguration(IReadOnlyDictionary<string, object> values)
    {
        if (values.TryGetValue("WordTemplatesEnabled", out var enabledObj))
        {
            if (enabledObj is bool b) _templatesEnabled = b;
            else if (bool.TryParse(enabledObj.ToString(), out var bParsed)) _templatesEnabled = bParsed;
        }

        if (values.TryGetValue("WordTemplateInterval", out var intervalObj))
        {
            if (intervalObj is int i) _templateIntervalKeystrokes = Math.Clamp(i, 5, 100);
            else if (int.TryParse(intervalObj.ToString(), out var iParsed)) _templateIntervalKeystrokes = Math.Clamp(iParsed, 5, 100);
            _nextTriggerThreshold = RollNextThreshold();
        }
    }

    public IReadOnlyDictionary<string, object> GetCurrentConfiguration() =>
        new Dictionary<string, object>
        {
            ["WordTemplatesEnabled"] = _templatesEnabled,
            ["WordTemplateInterval"] = _templateIntervalKeystrokes
        };
}
