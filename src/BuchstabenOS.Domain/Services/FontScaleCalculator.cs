using BuchstabenOS.Domain.Model.Typing;

namespace BuchstabenOS.Domain.Services;

/// <summary>
/// Berechnet deterministisch die dynamische Schriftgröße basierend auf der Zeichenanzahl
/// und der Viewport-Geometrie.
/// Jeder getippte Buchstabe lässt die Schrift stufenlos kleiner werden,
/// bis die Mindestgröße erreicht ist und ein Umbruch erfolgt.
/// </summary>
public class FontScaleCalculator
{
    private readonly double _minFontSize;
    private readonly double _maxFontSize;
    private readonly double _glyphWidthToFontSizeRatio;
    private readonly double _decayPerLetter;

    /// <param name="minFontSize">Untergrenze, ab der umbrochen wird (Standard: 48pt).</param>
    /// <param name="maxFontSize">Obergrenze für den 1. Buchstaben (Standard: 240pt).</param>
    /// <param name="glyphWidthToFontSizeRatio">Durchschnittliche Zeichenbreite relativ zur Schrifthöhe (Standard: 0.65).</param>
    /// <param name="decayPerLetter">Prozentualer Skalierungsfaktor pro zusätzlichem Zeichen (Standard: 0.90).</param>
    public FontScaleCalculator(
        double minFontSize = FontSize.DefaultMin,
        double maxFontSize = FontSize.DefaultMax,
        double glyphWidthToFontSizeRatio = 0.65,
        double decayPerLetter = 0.90)
    {
        _minFontSize = minFontSize;
        _maxFontSize = maxFontSize;
        _glyphWidthToFontSizeRatio = glyphWidthToFontSizeRatio;
        _decayPerLetter = decayPerLetter;
    }

    /// <summary>
    /// Berechnet die Schriftgröße für eine gegebene Zeile im Viewport.
    /// </summary>
    public FontSize Calculate(int characterCount, double viewportWidth, double viewportHeight)
    {
        return Calculate(characterCount, viewportWidth, viewportHeight, out _);
    }

    /// <summary>
    /// Berechnet die Schriftgröße für eine gegebene Zeile im Viewport.
    /// </summary>
    /// <param name="characterCount">Anzahl der Zeichen in der Zeile.</param>
    /// <param name="viewportWidth">Verfügbare Bildschirmbreite in Pixeln.</param>
    /// <param name="viewportHeight">Verfügbare Bildschirmhöhe in Pixeln.</param>
    /// <param name="requiresLineWrap">Gibt zurück, ob die Mindestgröße unterschritten wurde und ein Zeilenumbruch nötig ist.</param>
    public FontSize Calculate(int characterCount, double viewportWidth, double viewportHeight, out bool requiresLineWrap)
    {
        requiresLineWrap = false;

        if (characterCount <= 0)
        {
            return new FontSize(_maxFontSize);
        }

        // 1. Buchstabe: Maximalgröße, begrenzt durch die Bildschirmhöhe
        double heightRestrictedMax = Math.Min(_maxFontSize, viewportHeight * 0.50);

        if (characterCount == 1)
        {
            return new FontSize(heightRestrictedMax);
        }

        // Sanftes natürliches Schrumpfen mit jedem Buchstaben
        double naturalDecayedSize = heightRestrictedMax * Math.Pow(_decayPerLetter, characterCount - 1);

        // Nutzbare Breite mit 15% kindgerechtem Rand (7.5% links, 7.5% rechts)
        double usableWidth = viewportWidth * 0.85;

        // Harte Obergrenze nach Bildschirmbreite: Alle Zeichen müssen in die Zeile passen
        double widthBoundedSize = usableWidth / (characterCount * _glyphWidthToFontSizeRatio);

        // Das Minimum aus natürlicher Schrumpfung und Breiten-Restriktion
        double targetSize = Math.Min(naturalDecayedSize, widthBoundedSize);

        // Wenn die berechnete Größe kleiner als die Mindestgröße ist: Umbruch nötig!
        if (targetSize < _minFontSize)
        {
            requiresLineWrap = true;
            return new FontSize(_minFontSize);
        }

        return new FontSize(targetSize);
    }
}
