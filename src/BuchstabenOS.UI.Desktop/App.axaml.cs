using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Application.UseCases;
using BuchstabenOS.Domain.Model.Games;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using BuchstabenOS.Infrastructure.Audio;
using BuchstabenOS.Infrastructure.Dictionary;
using BuchstabenOS.Infrastructure.Storage;
using BuchstabenOS.Infrastructure.System;
using BuchstabenOS.UI.Desktop.ViewModels;
using BuchstabenOS.UI.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BuchstabenOS.UI.Desktop;

public partial class App : Avalonia.Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Dependency Injection Container aufbauen
            var services = new ServiceCollection();
            ConfigureServices(services, desktop.Args);
            _serviceProvider = services.BuildServiceProvider();

            var mainVm = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            await mainVm.InitializeAsync();

            bool isKiosk = desktop.Args == null || !desktop.Args.Contains("--windowed");

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            if (isKiosk)
            {
                mainWindow.WindowState = WindowState.FullScreen;
                mainWindow.SystemDecorations = SystemDecorations.None;
                mainWindow.Topmost = true;
            }
            else
            {
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Width = 1024;
                mainWindow.Height = 600;
            }

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services, string[]? args)
    {
        // Infrastructure
        services.AddSingleton<IWordDictionaryRepository, JsonWordDictionaryRepository>();
        services.AddSingleton<IAudioPlayer, LinuxAudioSamplePlayer>();
        services.AddSingleton<ITtsEngine, PiperTtsEngine>();
        services.AddSingleton<ISystemControl, LinuxSystemControl>();
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();

        // Domain Services & Models
        services.AddSingleton<FontScaleCalculator>();
        
        // Wörterbuch für Free-Typing laden
        var dictRepo = new JsonWordDictionaryRepository();
        var initialWords = dictRepo.LoadWordsForGameAsync("free-typing").GetAwaiter().GetResult();
        services.AddSingleton(new WordDetector(initialWords));

        services.AddSingleton<TextStage>();
        services.AddSingleton<FreeTypingGameModule>();

        // Application Game Registry & Coordinator
        services.AddSingleton<IGameRegistry>(sp =>
        {
            var registry = new InMemoryGameRegistry();
            registry.RegisterGame(sp.GetRequiredService<FreeTypingGameModule>());
            return registry;
        });

        services.AddSingleton<GameCoordinator>();

        // ViewModels
        services.AddSingleton<ParentMenuViewModel>(sp =>
        {
            var dict = sp.GetRequiredService<IWordDictionaryRepository>();
            var sys = sp.GetRequiredService<ISystemControl>();
            var set = sp.GetRequiredService<ISettingsRepository>();

            MainWindowViewModel? mainVmRef = null;
            var parentVm = new ParentMenuViewModel(dict, sys, set, () =>
            {
                mainVmRef?.ToggleParentOverlay();
            });
            return parentVm;
        });

        services.AddSingleton<MainWindowViewModel>();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}