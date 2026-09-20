using System;
using System.Threading;
using System.Threading.Tasks;
using BuchstabenOS.Application.Ports;
using BuchstabenOS.Domain.Model.Security;
using BuchstabenOS.Domain.Model.Typing;
using BuchstabenOS.Domain.Services;
using BuchstabenOS.Infrastructure.Updates;
using BuchstabenOS.UI.Desktop.ViewModels;
using Xunit;

namespace BuchstabenOS.Domain.Tests;

public class UpdateServiceTests
{
    private class FakeSystemControl : ISystemControl
    {
        public bool RestartAppCalled { get; private set; }
        public void ExitToDesktop() { }
        public void PowerOff() { }
        public void Reboot() { }
        public void SetSystemVolume(int percent) { }
        public int GetSystemVolume() => 80;
        public void RestartApp() => RestartAppCalled = true;
    }

    private class FakeUpdateService : IUpdateService
    {
        public string CurrentVersion { get; set; } = "1.0.0";
        public UpdateCheckResult CheckResultToReturn { get; set; } = new(false, "1.0.0");
        public bool ApplyResultToReturn { get; set; } = true;
        public bool RestartCalled { get; private set; }

        public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CheckResultToReturn);
        }

        public Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            progress?.Report(50);
            progress?.Report(100);
            return Task.FromResult(ApplyResultToReturn);
        }

        public void RestartApplication()
        {
            RestartCalled = true;
        }
    }

    private class FakeDictionaryRepo : IWordDictionaryRepository
    {
        public Task<IReadOnlyList<string>> LoadWordsForGameAsync(string gameId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(new List<string>());
        public Task AddWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveWordForGameAsync(string gameId, string word, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeSettingsRepo : ISettingsRepository
    {
        public Task<Application.Configuration.AppSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Application.Configuration.AppSettings());
        public Task SaveSettingsAsync(Application.Configuration.AppSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Theory]
    [InlineData("1.1.0", "1.0.0", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.1.0", false)]
    [InlineData("0.9.0", "1.0.0", false)]
    public void Version_Comparison_Should_Correctly_Identify_Newer_Versions(string remote, string current, bool expected)
    {
        bool isNewer = GitHubReleaseUpdateService.IsRemoteVersionNewer(remote, current);
        Assert.Equal(expected, isNewer);
    }

    [Fact]
    public async Task ParentMenu_CheckForUpdates_Should_Update_ViewModel_State_When_Update_Found()
    {
        var fakeUpdateService = new FakeUpdateService
        {
            CurrentVersion = "1.0.0",
            CheckResultToReturn = new UpdateCheckResult(
                IsUpdateAvailable: true,
                CurrentVersion: "1.0.0",
                UpdateInfo: new UpdateInfo(
                    Version: "1.1.0",
                    ReleaseTitle: "BuchstabenOS 1.1.0",
                    ChangelogMarkdown: "- Neues Mathespiel\n- Bugfixes",
                    AssetDownloadUrl: "https://example.com/update.tar.gz",
                    FileSizeBytes: 1024 * 1024,
                    PublishedAt: DateTime.UtcNow
                )
            )
        };

        var vm = new ParentMenuViewModel(
            new FakeDictionaryRepo(),
            new FakeSystemControl(),
            new FakeSettingsRepo(),
            new WordDetector(),
            fakeUpdateService
        );

        Assert.False(vm.IsUpdateAvailable);
        Assert.Equal("1.0.0", vm.CurrentVersion);

        await vm.CheckForUpdatesAsync();

        Assert.True(vm.IsUpdateAvailable);
        Assert.Equal("1.1.0", vm.LatestAvailableVersion);
        Assert.Contains("Neues Mathespiel", vm.UpdateChangelog);
        Assert.Contains("v1.1.0 verfügbar", vm.UpdateStatusText);
    }

    [Fact]
    public async Task ParentMenu_InstallUpdate_Should_Report_Progress_And_Set_ReadyToRestart()
    {
        var fakeUpdateService = new FakeUpdateService
        {
            CurrentVersion = "1.0.0",
            CheckResultToReturn = new UpdateCheckResult(
                IsUpdateAvailable: true,
                CurrentVersion: "1.0.0",
                UpdateInfo: new UpdateInfo("1.1.0", "v1.1.0", "Notes", "https://example.com", 1000, DateTime.UtcNow)
            )
        };

        var vm = new ParentMenuViewModel(
            new FakeDictionaryRepo(),
            new FakeSystemControl(),
            new FakeSettingsRepo(),
            new WordDetector(),
            fakeUpdateService
        );

        await vm.CheckForUpdatesAsync();
        Assert.True(vm.IsUpdateAvailable);

        await vm.InstallUpdateAsync();

        Assert.False(vm.IsUpdateAvailable);
        Assert.True(vm.IsUpdateReadyToRestart);
        Assert.Contains("erfolgreich installiert", vm.UpdateStatusText);

        vm.RestartApp();
        Assert.True(fakeUpdateService.RestartCalled);
    }
}
