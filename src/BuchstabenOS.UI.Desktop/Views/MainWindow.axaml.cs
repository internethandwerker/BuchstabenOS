using Avalonia.Controls;
using Avalonia.Input;
using BuchstabenOS.UI.Desktop.ViewModels;
using Serilog;

namespace BuchstabenOS.UI.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TextInput += OnTextInputReceived;
    }

    /// <summary>
    /// Verarbeitet alle echten Zeicheneingaben (Buchstaben, Umlaute Ä, Ö, Ü, ß, Zahlen).
    /// Nutzt das native TextInput-Event des Fensters, wodurch das Tastatur-Layout des Betriebssystems
    /// zu 100% korrekt und fehlerfrei interpretiert wird.
    /// </summary>
    private async void OnTextInputReceived(object? sender, TextInputEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.IsParentOverlayVisible) return;
        if (string.IsNullOrEmpty(e.Text)) return;

        foreach (char c in e.Text)
        {
            // Steuerzeichen und Leertaste werden gesondert über OnKeyDown gesteuert
            if (char.IsControl(c) || c == ' ') continue;

            Log.Debug("TextInput empfangen: '{Char}'", c);
            await vm.HandleKeyInputAsync(c, isSpace: false, isBackspace: false, ctrl: false, alt: false, shift: false);
        }
        e.Handled = true;
    }

    /// <summary>
    /// Fängt Navigationstasten, Backspace, Leertaste und die geheime Eltern-Kombination ab.
    /// </summary>
    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not MainWindowViewModel vm) return;

        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // 1. Geheime Elternkombination: Ctrl + Alt + Shift + P
        if (ctrl && alt && shift && e.Key == Key.P)
        {
            vm.ToggleParentOverlay();
            e.Handled = true;
            return;
        }

        // 2. Wenn das Elternmenü offen ist:
        if (vm.IsParentOverlayVisible)
        {
            if (e.Key == Key.Escape)
            {
                vm.ToggleParentOverlay();
                e.Handled = true;
            }
            return;
        }

        // 3. Steuerungstasten im Spielmodus:
        if (e.Key == Key.Back)
        {
            await vm.HandleKeyInputAsync('\0', isSpace: false, isBackspace: true, ctrl, alt, shift);
            e.Handled = true;
        }
        else if (e.Key == Key.Space)
        {
            await vm.HandleKeyInputAsync(' ', isSpace: true, isBackspace: false, ctrl, alt, shift);
            e.Handled = true;
        }
        else if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            // Enter: Zeile abschließen und nach oben schieben
            await vm.HandleKeyInputAsync('\0', isSpace: false, isBackspace: false, ctrl, alt, shift, isEnter: true);
            e.Handled = true;
        }
    }
}