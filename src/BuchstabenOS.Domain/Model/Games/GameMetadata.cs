namespace BuchstabenOS.Domain.Model.Games;

/// <summary>
/// Pädagogische Fähigkeiten, die durch Spiele gefördert werden können.
/// Entwickelt für die semantische Interpretation durch Eltern und spätere kindgerechte LLM-Agenten.
/// </summary>
public enum PedagogicalSkill
{
    /// <summary>Lautbewusstheit (Zuordnung von Buchstabe zu Laut/Phonem).</summary>
    PhonemicAwareness,
    
    /// <summary>Optische Buchstabenerkennung (Groß-/Kleinbuchstaben).</summary>
    LetterRecognition,
    
    /// <summary>Feinmotorik & Hand-Auge-Koordination an der Tastatur.</summary>
    KeyboardCoordination,
    
    /// <summary>Wortsynthese (Zusammenziehen von Einzellauten zu einem Wort).</summary>
    WordSynthesis,
    
    /// <summary>Wortschatzerweiterung & Begriffszuordnung.</summary>
    VocabularyBuilding,
    
    /// <summary>Zahlenerkennung & Zählen von Mengen.</summary>
    NumberRecognition,
    
    /// <summary>Erste Addition & mathematisches Verständnis.</summary>
    BasicMath,
    
    /// <summary>Auditive Merkfähigkeit & Rhythmus.</summary>
    AuditoryMemory,
    
    /// <summary>Freies Entdecken und kreativer Ausdruck.</summary>
    FreeExploration
}

/// <summary>
/// Voraussetzungen, die ein Kind für ein Spiel mitbringen sollte.
/// </summary>
public record SkillPrerequisite(string SkillName, string Description);

/// <summary>
/// Altersempfehlung für ein Spielmodul.
/// </summary>
/// <param name="MinYears">Mindestalter in Jahren (z. B. 3).</param>
/// <param name="MaxYears">Höchstalter in Jahren (z. B. 6).</param>
public record AgeRecommendation(int MinYears, int MaxYears)
{
    public bool IsAppropriateFor(int childAge) => childAge >= MinYears && childAge <= MaxYears;
    public override string ToString() => $"{MinYears}–{MaxYears} Jahre";
}

/// <summary>
/// Detaillierte Metadaten-Beschreibung eines Spiels.
/// Diese Struktur ermöglicht es späteren Versionen von BuchstabenOS,
/// Spiele dynamisch als Plugins nachzuladen, zu sequenzieren (Playlist)
/// oder durch einen kindgerechten KI-Agenten basierend auf dem Entwicklungsstand
/// des Kindes empfehlen zu lassen.
/// </summary>
public record GameMetadata(
    string Id,
    string Title,
    string Description,
    string Category,
    AgeRecommendation Age,
    IReadOnlyList<PedagogicalSkill> TargetSkills,
    IReadOnlyList<SkillPrerequisite> Prerequisites,
    IReadOnlyList<string> LearningObjectives,
    int DifficultyLevel = 1
);
