using System.Text.Json;
using BuchstabenOS.Application.Configuration;
using BuchstabenOS.Application.Ports;

namespace BuchstabenOS.Infrastructure.Storage;

/// <summary>
/// Persistiert die globalen App-Einstellungen in einer JSON-Datei.
/// </summary>
public class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _settingsFilePath;

    public JsonSettingsRepository(string? customFilePath = null)
    {
        _settingsFilePath = customFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "buchstabenos", "settings.json"
        );

        string? dir = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public async Task<AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                using var stream = File.OpenRead(_settingsFilePath);
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: cancellationToken);
                if (settings != null) return settings;
            }
            catch
            {
                // Fallback zu Default
            }
        }

        var defaults = new AppSettings();
        await SaveSettingsAsync(defaults, cancellationToken);
        return defaults;
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, options, cancellationToken);
    }
}
