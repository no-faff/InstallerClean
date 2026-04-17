using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
        var badFile = _tempFile + ".bad";
        if (File.Exists(badFile))
            File.Delete(badFile);
    }

    [Fact]
    public void Load_returns_defaults_when_no_file_exists()
    {
        var svc = new SettingsService(_tempFile);

        var settings = svc.Load();

        Assert.Equal(string.Empty, settings.MoveDestination);
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var svc = new SettingsService(_tempFile);
        var original = new AppSettings
        {
            MoveDestination = @"D:\Backup"
        };

        svc.Save(original);
        var loaded = svc.Load();

        Assert.Equal(@"D:\Backup", loaded.MoveDestination);
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_corrupt()
    {
        File.WriteAllText(_tempFile, "this is not valid json {{{");
        var svc = new SettingsService(_tempFile);

        var settings = svc.Load();

        Assert.Equal(string.Empty, settings.MoveDestination);
    }

    [Fact]
    public void Load_renames_corrupt_file_to_dot_bad()
    {
        File.WriteAllText(_tempFile, "this is not valid json {{{");
        var svc = new SettingsService(_tempFile);

        svc.Load();

        Assert.False(File.Exists(_tempFile), "Corrupt file should have been moved away");
        Assert.True(File.Exists(_tempFile + ".bad"), "Corrupt content should be preserved in a .bad file for recovery");
    }

    [Fact]
    public void Load_overwrites_existing_bad_file()
    {
        var badFile = _tempFile + ".bad";
        File.WriteAllText(badFile, "previous bad");
        File.WriteAllText(_tempFile, "new corrupt {{{");
        var svc = new SettingsService(_tempFile);

        svc.Load();

        Assert.True(File.Exists(badFile));
        Assert.Equal("new corrupt {{{", File.ReadAllText(badFile));
    }

    [Fact]
    public void TrySave_returns_true_on_success()
    {
        var svc = new SettingsService(_tempFile);

        var result = svc.TrySave(new AppSettings { MoveDestination = @"D:\Backup" });

        Assert.True(result);
        Assert.True(File.Exists(_tempFile));
    }

    [Fact]
    public void TrySave_returns_false_when_path_is_invalid()
    {
        var invalidPath = Path.Combine(_tempFile, "sub", "settings.json");
        File.WriteAllText(_tempFile, "not a directory");
        var svc = new SettingsService(invalidPath);

        var result = svc.TrySave(new AppSettings());

        Assert.False(result);
    }

    [Fact]
    public void Save_never_throws_even_when_target_is_unreachable()
    {
        var invalidPath = Path.Combine(_tempFile, "sub", "settings.json");
        File.WriteAllText(_tempFile, "not a directory");
        var svc = new SettingsService(invalidPath);

        var ex = Record.Exception(() => svc.Save(new AppSettings()));

        Assert.Null(ex);
    }
}
