namespace BuchstabenOS.Domain.Model.Games;

public enum GameConfigType
{
    NumberSlider,
    Toggle
}

/// <summary>
/// Beschreibt ein konfigurierbares Feld eines Spiels für das Elternportal.
/// </summary>
public record GameConfigDescriptor(
    string Key,
    string Label,
    string Description,
    GameConfigType Type,
    int MinValue = 0,
    int MaxValue = 100,
    int Step = 1,
    object? DefaultValue = null
);

/// <summary>
/// Schnittstelle für Spiele, die vom Elternportal konfiguriert werden können.
/// </summary>
public interface IGameConfigurable
{
    /// <summary>Liefert die Liste aller konfigurierbaren Parameter dieses Spiels.</summary>
    IReadOnlyList<GameConfigDescriptor> GetConfigurationDescriptors();

    /// <summary>Wendet neue Konfigurationswerte auf das Spiel an.</summary>
    void ApplyConfiguration(IReadOnlyDictionary<string, object> values);

    /// <summary>Gibt die aktuellen Konfigurationswerte zurück.</summary>
    IReadOnlyDictionary<string, object> GetCurrentConfiguration();
}
