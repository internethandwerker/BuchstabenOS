using System;
using System.Collections.Generic;
using BuchstabenOS.Domain.Events;
using BuchstabenOS.Domain.Services;

namespace BuchstabenOS.Domain.Model.Games;

/// <summary>
/// Das Mathespiel "Addition" für Vorschulkinder:
/// Zeigt einfache Additionsaufgaben (a + b = ?), liest sie mit wechselnden, kindgerechten
/// Formulierungen vor, bewertet Zifferneingaben direkt und fördert spielerisch das Mengenverständnis.
/// </summary>
public class AdditionGameModule : IGameModule, IRenderableGame, IGameConfigurable
{
    public static readonly string[] SpokenQuestionTemplates =
    [
        // Mit variablen Platzhaltern [a] und [b] / {0} und {1}
        "Was ist [a] plus [b]?",
        "Wie viel ist [a] plus [b]?",
        "Kannst du mir sagen, was [a] plus [b] ist?",
        "Wenn du [a] hast und [b] hinzufügst, wie viel hast du dann insgesamt?",
        "Wenn man [a] und [b] zusammenzählt, was ist das richtige Ergebnis?",
        "Rechne mal aus: Was ist [a] plus [b]?",
        "Was ergibt [a] plus [b]? Bitte tippe die Lösung ein!",
        "Du hast [a] Sterne und zauberst [b] dazu. Wie viele Sterne sind es jetzt?",

        // Ohne Platzhalter (direkte Aufforderung zum Bildschirm)
        "Wie viel ist das? Bitte rechne die Aufgabe aus.",
        "Was ist hier die Lösung?",
        "Schau mal auf den Bildschirm: Was kommt da raus?",
        "Wie viel ergibt diese Plusaufgabe? Tippe die Zahl ein.",
        "Rechne das mal aus: Was ist die richtige Zahl?"
    ];

    private readonly Random _random = new();
    private readonly FontScaleCalculator _fontCalculator = new();
    private readonly List<string> _completedLines = new();

    private IGameContext? _context;
    private int _maxSum = 10;
    private int _minOperand = 1;
    private int _lastTemplateIndex = -1;

    private int _operandA;
    private int _operandB;
    private int _expectedResult;
    private string _userAnswer = string.Empty;
    private int _attemptsForCurrentTask = 0;
    private int _totalSolvedTasks = 0;
    private DateTime _sessionStartTime = DateTime.UtcNow;

    // View State
    public string DisplayText => FormatDisplayText();
    public double CurrentFontSizePoints => CalculateFontSize();
    public string HintText => "Tippe das richtige Ergebnis als Zahl!";
    public IReadOnlyList<string> CompletedLines => _completedLines;
    public bool IsCelebrating { get; private set; }
    public string CelebrationMessage { get; private set; } = string.Empty;
    public bool IsWrongFeedback { get; private set; }

    public event Action? ViewStateChanged;

    public GameMetadata Metadata { get; } = new(
        Id: "math-addition",
        Title: "Mathe-Zwerg (Addition)",
        Description: "Löse fröhliche Plus-Aufgaben! Wie viel ist 2 plus 3? Tippe die Zahl auf der Tastatur.",
        Category: "Zahlen & Rechnen",
        Age: new AgeRecommendation(4, 6),
        TargetSkills: new[]
        {
            PedagogicalSkill.BasicMath,
            PedagogicalSkill.NumberRecognition,
            PedagogicalSkill.KeyboardCoordination
        },
        Prerequisites: new[]
        {
            new SkillPrerequisite("ZahlenKennen", "Ziffern von 0 bis 9 auf der Tastatur erkennen.")
        },
        LearningObjectives: new[]
        {
            "Verständnis von Mengen und Addition verinnerlichen.",
            "Ziffern auf der Tastatur zielsicher auffinden.",
            "Fehlertoleranz und Motivation durch sofortige zweite Chance erfahren."
        },
        DifficultyLevel: 1,
        DefaultAttentionSpanTasks: 2, // Alex: "Nimm als default 2 gelöste aufgaben mathe"
        DefaultAttentionSpanDuration: TimeSpan.FromMinutes(5),
        DefaultWeight: 50
    );

    public bool IsActive { get; private set; }

    public int MaxSum
    {
        get => _maxSum;
        set
        {
            _maxSum = Math.Clamp(value, 2, 50);
            if (IsActive)
            {
                GenerateNewTask();
            }
        }
    }

    public int OperandA => _operandA;
    public int OperandB => _operandB;
    public int ExpectedResult => _expectedResult;
    public string UserAnswer => _userAnswer;
    public string CurrentTaskSpokenPrompt { get; private set; } = string.Empty;

    public Task InitializeAsync(IGameContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        IsActive = true;
        _sessionStartTime = DateTime.UtcNow;
        GenerateNewTask(deferredNotification: true);
        return Task.CompletedTask;
    }

    public ValueTask<GameInputResult> ProcessInputAsync(GameInput input)
    {
        if (!IsActive) return ValueTask.FromResult(GameInputResult.Unhandled);

        // Feedback-Zustände bei neuem Tastendruck zurücksetzen
        if (IsWrongFeedback)
        {
            IsWrongFeedback = false;
        }

        if (input.IsBackspace)
        {
            if (_userAnswer.Length > 0)
            {
                _userAnswer = _userAnswer[..^1];
                OnViewStateChanged();
                return ValueTask.FromResult(new GameInputResult(true, Array.Empty<IDomainEvent>()));
            }
            return ValueTask.FromResult(new GameInputResult(true, Array.Empty<IDomainEvent>()));
        }

        // Akzeptiere NUR Ziffern '0' bis '9' (oder Enter zum vorzeitigen Absenden)
        char c = input.KeyChar;
        bool isDigit = char.IsDigit(c);
        bool isEnter = c == '\r' || c == '\n';

        if (!isDigit && !isEnter)
        {
            // Alle anderen Tasten (Buchstaben, Sonderzeichen) werden ignoriert!
            return ValueTask.FromResult(GameInputResult.Unhandled);
        }

        var emittedEvents = new List<IDomainEvent>();

        if (isDigit)
        {
            _userAnswer += c;
            OnViewStateChanged();
        }

        // Prüfung: Wann ist die Eingabe komplett?
        // 1. Wenn die Anzahl der Ziffern die Ziffernlänge des Zielergebnisses erreicht hat
        // 2. ODER wenn Enter gedrückt wurde (sofern etwas eingegeben wurde)
        int targetLength = _expectedResult.ToString().Length;
        bool isComplete = _userAnswer.Length >= targetLength || (isEnter && _userAnswer.Length > 0);

        if (isComplete)
        {
            _attemptsForCurrentTask++;

            if (int.TryParse(_userAnswer, out int givenAnswer) && givenAnswer == _expectedResult)
            {
                // FALL RICHTIG
                _totalSolvedTasks++;
                IsCelebrating = true;
                IsWrongFeedback = false;
                CelebrationMessage = $"{_operandA} + {_operandB} = {_expectedResult} ⭐";

                string praise = $"Prima! {NumberToGermanWord(_operandA)} plus {NumberToGermanWord(_operandB)} ist {NumberToGermanWord(_expectedResult)}!";
                var solvedEvent = new MathTaskSolvedEvent($"{_operandA} + {_operandB} = {_expectedResult}", _expectedResult, _attemptsForCurrentTask, praise);
                var roundCompletedEvent = new GameRoundCompletedEvent(Metadata.Id, _totalSolvedTasks);

                emittedEvents.Add(solvedEvent);
                emittedEvents.Add(roundCompletedEvent);
                _context?.PublishEvent(solvedEvent);
                _context?.PublishEvent(roundCompletedEvent);

                // Zur Historie hinzufügen
                _completedLines.Add($"{_operandA} + {_operandB} = {_expectedResult}");

                OnViewStateChanged();

                // Nächste Aufgabe für die folgende Runde vorbereiten
                GenerateNewTask(deferredNotification: true);
            }
            else
            {
                // FALL FALSCH:
                // Animation aktivieren und dieselbe Aufgabe neu starten (Puffer leeren)
                IsWrongFeedback = true;
                IsCelebrating = false;

                string encouragement = "Das war noch nicht ganz richtig. Probier es gleich nochmal!";
                var failedEvent = new MathTaskFailedEvent($"{_operandA} + {_operandB} = ?", _userAnswer, _expectedResult, encouragement);
                emittedEvents.Add(failedEvent);
                _context?.PublishEvent(failedEvent);

                // Antwort für neuen Versuch leeren
                _userAnswer = string.Empty;

                OnViewStateChanged();
            }
        }

        return ValueTask.FromResult(new GameInputResult(true, emittedEvents));
    }

    /// <summary>
    /// Ermittelt über einen Zufallswürfel das nächste Vorlese-Template,
    /// ohne unmittelbar dasselbe Template hintereinander zu wiederholen.
    /// </summary>
    public string RollSpokenPrompt(int a, int b)
    {
        if (SpokenQuestionTemplates.Length == 0)
        {
            return $"Was ist {NumberToGermanWord(a)} plus {NumberToGermanWord(b)}?";
        }

        int nextIndex;
        if (SpokenQuestionTemplates.Length > 1)
        {
            do
            {
                nextIndex = _random.Next(SpokenQuestionTemplates.Length);
            } while (nextIndex == _lastTemplateIndex);
        }
        else
        {
            nextIndex = 0;
        }

        _lastTemplateIndex = nextIndex;
        return FormatTemplate(SpokenQuestionTemplates[nextIndex], a, b);
    }

    /// <summary>
    /// Ersetzt [a]/[b] bzw. {0}/{1} in einem Vorlese-Template durch die ausgeschriebenen deutschen Zahlwörter.
    /// Templates ohne Platzhalter bleiben unverändert.
    /// </summary>
    public static string FormatTemplate(string template, int a, int b)
    {
        string wordA = NumberToGermanWord(a);
        string wordB = NumberToGermanWord(b);

        string formatted = template
            .Replace("[a]", wordA, StringComparison.OrdinalIgnoreCase)
            .Replace("[b]", wordB, StringComparison.OrdinalIgnoreCase);

        if (formatted.Contains("{0}") || formatted.Contains("{1}"))
        {
            formatted = string.Format(formatted, wordA, wordB);
        }

        return formatted;
    }

    public MathTaskGeneratedEvent GenerateNewTask(bool deferredNotification = false)
    {
        _userAnswer = string.Empty;
        _attemptsForCurrentTask = 0;
        IsWrongFeedback = false;

        // Erzeuge eine kindgerechte Additionsaufgabe innerhalb des Limits
        // MaxSum muss mindestens 2 sein
        int safeMax = Math.Max(_maxSum, 2);

        // a zwischen 1 und MaxSum - 1
        _operandA = _random.Next(_minOperand, safeMax);
        // b so wählen, dass a + b <= MaxSum
        int maxB = safeMax - _operandA;
        _operandB = _random.Next(_minOperand, Math.Max(_minOperand + 1, maxB + 1));
        _expectedResult = _operandA + _operandB;

        // Wähle rotierendes / gewürfeltes Vorlese-Template
        CurrentTaskSpokenPrompt = RollSpokenPrompt(_operandA, _operandB);

        var generatedEvent = new MathTaskGeneratedEvent(
            $"{_operandA} + {_operandB} = ?",
            _operandA,
            _operandB,
            _expectedResult,
            CurrentTaskSpokenPrompt
        );

        if (!deferredNotification)
        {
            _context?.PublishEvent(generatedEvent);
        }

        OnViewStateChanged();

        return generatedEvent;
    }

    public void RepeatCurrentTaskPrompt()
    {
        string prompt = !string.IsNullOrWhiteSpace(CurrentTaskSpokenPrompt)
            ? CurrentTaskSpokenPrompt
            : $"Wie viel ist {NumberToGermanWord(_operandA)} plus {NumberToGermanWord(_operandB)}?";

        var ev = new MathTaskGeneratedEvent(
            $"{_operandA} + {_operandB} = ?",
            _operandA,
            _operandB,
            _expectedResult,
            prompt
        );
        _context?.PublishEvent(ev);
    }

    public KnowledgeScore GetScore()
    {
        return new KnowledgeScore(
            TotalInteractions: _totalSolvedTasks + _attemptsForCurrentTask,
            SuccessfulWordsRecognized: _totalSolvedTasks,
            UniqueLettersExplored: 0,
            CurrentStreak: _totalSolvedTasks,
            PlayTime: DateTime.UtcNow - _sessionStartTime,
            LastPlayedAt: DateTime.UtcNow
        );
    }

    public void Reset()
    {
        _completedLines.Clear();
        _totalSolvedTasks = 0;
        _attemptsForCurrentTask = 0;
        _userAnswer = string.Empty;
        IsCelebrating = false;
        IsWrongFeedback = false;
        _sessionStartTime = DateTime.UtcNow;
        GenerateNewTask();
    }

    private string FormatDisplayText()
    {
        if (string.IsNullOrEmpty(_userAnswer))
        {
            return $"{_operandA} + {_operandB} = ";
        }
        return $"{_operandA} + {_operandB} = {_userAnswer}";
    }

    private double CalculateFontSize()
    {
        // Berechne die Fontgröße anhand der maximal erwarteten Zeilenlänge (inkl. erwartetem Ergebnis),
        // damit die Aufgabe von Anfang an perfekt zentriert in einer einzigen Zeile steht!
        string fullLine = $"{_operandA} + {_operandB} = {_expectedResult}";
        var size = _fontCalculator.Calculate(fullLine.Length, 1366, 768);
        return Math.Max(size.Points, 64.0);
    }

    private void OnViewStateChanged()
    {
        ViewStateChanged?.Invoke();
    }

    public static string NumberToGermanWord(int number) => number switch
    {
        0 => "null",
        1 => "eins",
        2 => "zwei",
        3 => "drei",
        4 => "vier",
        5 => "fünf",
        6 => "sechs",
        7 => "sieben",
        8 => "acht",
        9 => "neun",
        10 => "zehn",
        11 => "elf",
        12 => "zwölf",
        13 => "dreizehn",
        14 => "vierzehn",
        15 => "fünfzehn",
        16 => "sechzehn",
        17 => "siebzehn",
        18 => "achtzehn",
        19 => "neunzehn",
        20 => "zwanzig",
        _ => number.ToString()
    };

    // IGameConfigurable
    public IReadOnlyList<GameConfigDescriptor> GetConfigurationDescriptors() =>
    [
        new GameConfigDescriptor(
            Key: "MaxSum",
            Label: "Rechnen bis (Maximales Ergebnis)",
            Description: "Legt fest, wie groß das maximale Ergebnis einer Additionsaufgabe sein darf.",
            Type: GameConfigType.NumberSlider,
            MinValue: 2,
            MaxValue: 20,
            Step: 1,
            DefaultValue: 10
        )
    ];

    public void ApplyConfiguration(IReadOnlyDictionary<string, object> values)
    {
        if (values.TryGetValue("MaxSum", out var objVal))
        {
            if (objVal is int intVal)
            {
                MaxSum = intVal;
            }
            else if (int.TryParse(objVal?.ToString(), out int parsed))
            {
                MaxSum = parsed;
            }
        }
    }

    public IReadOnlyDictionary<string, object> GetCurrentConfiguration() =>
        new Dictionary<string, object>
        {
            ["MaxSum"] = MaxSum
        };
}
