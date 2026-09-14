namespace BuchstabenOS.Domain.Model.Security;

/// <summary>
/// Schützt das Elternmenü vor versehentlichem Zugriff durch Kinder.
/// </summary>
public record ParentPin
{
    public const string DefaultPin = "1337";
    private readonly string _pinHash;

    public ParentPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            throw new ArgumentException("PIN muss mindestens 4 Zeichen lang sein.", nameof(pin));
        }
        _pinHash = Hash(pin);
    }

    public static ParentPin CreateDefault() => new(DefaultPin);

    public bool Verify(string inputPin)
    {
        if (string.IsNullOrWhiteSpace(inputPin)) return false;
        return Hash(inputPin) == _pinHash;
    }

    private static string Hash(string text)
    {
        // Einfacher deterministischer SHA-256 Hash für lokale PIN
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
