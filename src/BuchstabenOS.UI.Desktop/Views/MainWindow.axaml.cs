using Avalonia.Controls;
using Avalonia.Input;
using BuchstabenOS.UI.Desktop.ViewModels;

namespace BuchstabenOS.UI.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not MainWindowViewModel vm) return;

        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // Geheime Elternkombination: Ctrl + Alt + Shift + P
        if (ctrl && alt && shift && e.Key == Key.P)
        {
            vm.ToggleParentOverlay();
            e.Handled = true;
            return;
        }

        // Wenn das Elternmenü offen ist:
        if (vm.IsParentOverlayVisible)
        {
            if (e.Key == Key.Escape)
            {
                vm.ToggleParentOverlay();
                e.Handled = true;
            }
            // Ansonsten lassen wir TextBoxen normal tippen
            return;
        }

        // Im Spielmodus: Alle Tasten exklusiv abfangen
        bool isBackspace = e.Key == Key.Back;
        bool isSpace = e.Key == Key.Space;

        char keyChar = '\0';

        if (!isBackspace && !isSpace)
        {
            keyChar = ExtractCharFromKey(e.Key, e.KeySymbol, shift);
        }

        if (isBackspace || isSpace || keyChar != '\0')
        {
            await vm.HandleKeyInputAsync(keyChar, isSpace, isBackspace, ctrl, alt, shift);
            e.Handled = true;
        }
    }

    private static char ExtractCharFromKey(Key key, string? keySymbol, bool shift)
    {
        // Wenn Avalonia ein echtes Symbol liefert (z.B. bei Umlauten):
        if (!string.IsNullOrEmpty(keySymbol) && keySymbol.Length == 1)
        {
            return keySymbol[0];
        }

        // Standard Buchstabentasten
        if (key >= Key.A && key <= Key.Z)
        {
            return (char)('A' + (key - Key.A));
        }

        // Ziffern 0-9
        if (key >= Key.D0 && key <= Key.D9)
        {
            return (char)('0' + (key - Key.D0));
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return (char)('0' + (key - Key.NumPad0));
        }

        // Häufige Sonderzeichen
        return key switch
        {
            Key.OemOpenBrackets => 'ß',
            Key.OemQuotes => 'Ä',
            Key.OemSemicolon => 'Ö',
            Key.OemQuestion => 'Ü',
            _ => '\0'
        };
    }
}