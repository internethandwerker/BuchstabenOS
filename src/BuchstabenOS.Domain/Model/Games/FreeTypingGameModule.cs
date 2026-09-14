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
public class FreeTypingGameModule : IGameModule
{
    private IGameContext? _context;
    private int _successfulWords;
    private readonly HashSet<char> _exploredLetters = new();
    private int _interactionCount;
    private DateTime _sessionStartTime = DateTime.UtcNow;

    public TextStage Stage { get; }

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
        DifficultyLevel: 1
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
        return Task.CompletedTask;
    }

    public ValueTask<GameInputResult> ProcessInputAsync(GameInput input)
    {
        if (!IsActive) return ValueTask.FromResult(GameInputResult.Unhandled);

        _interactionCount++;

        // Default-Viewport für Domain-Berechnungen (wird von UI übergeben oder Default)
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
        else if (input.KeyChar != '\0')
        {
            _exploredLetters.Add(char.ToUpperInvariant(input.KeyChar));
            Stage.TypeCharacter(input.KeyChar, viewportWidth, viewportHeight);
        }

        var events = Stage.DequeueEvents();

        // Prüfe auf Erfolgsereignisse für den Score
        foreach (var ev in events)
        {
            if (ev is WordRecognizedEvent)
            {
                _successfulWords++;
            }
            _context?.PublishEvent(ev);
        }

        return ValueTask.FromResult(new GameInputResult(true, events));
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

    public void Reset()
    {
        Stage.Clear();
        _interactionCount = 0;
        _successfulWords = 0;
        _exploredLetters.Clear();
        _sessionStartTime = DateTime.UtcNow;
    }
}
