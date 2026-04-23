using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.IO;
using FlatRedBall.Content.AnimationChain;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsSaveAsTests : IDisposable
{
    private readonly TestHelpers.TempDir _dir;

    public AppCommandsSaveAsTests()
    {
        _dir = new TestHelpers.TempDir();
        TestHelpers.SetupFreshAcls();
    }

    public void Dispose() => _dir.Dispose();

    // ── Dialog cancelled ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenDialogCancelled_DoesNotSaveFile()
    {
        AppCommands.Self.FileDialogService = new StubFileDialogService(null);

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.Empty(Directory.GetFiles(_dir.Path, "*.achx"));
    }

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenDialogCancelled_DoesNotUpdateFileName()
    {
        AppCommands.Self.FileDialogService = new StubFileDialogService(null);
        ProjectManager.Self.FileName = null;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.Null(ProjectManager.Self.FileName);
    }

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenDialogCancelled_DoesNotFireSaveAsCompleted()
    {
        AppCommands.Self.FileDialogService = new StubFileDialogService(null);
        bool fired = false;
        AppCommands.Self.SaveAsCompleted += _ => fired = true;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.False(fired);
    }

    // ── Dialog confirms ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenPathReturned_SavesFile()
    {
        var target = Path.Combine(_dir.Path, "out.achx");
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.FileDialogService = new StubFileDialogService(target);
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Walk" });
        ProjectManager.Self.AnimationChainListSave = acls;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.True(File.Exists(target));
    }

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenPathReturned_UpdatesProjectManagerFileName()
    {
        var target = Path.Combine(_dir.Path, "out.achx");
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.FileDialogService = new StubFileDialogService(target);
        ProjectManager.Self.AnimationChainListSave = acls;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.Equal(target, ProjectManager.Self.FileName);
    }

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_WhenPathReturned_FiresSaveAsCompletedWithPath()
    {
        var target = Path.Combine(_dir.Path, "out.achx");
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.FileDialogService = new StubFileDialogService(target);
        ProjectManager.Self.AnimationChainListSave = acls;
        string? received = null;
        AppCommands.Self.SaveAsCompleted += p => received = p;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        Assert.Equal(target, received);
    }

    [Fact]
    public async Task SaveCurrentAnimationChainListAsync_SavedFile_ContainsChainData()
    {
        var target = Path.Combine(_dir.Path, "data.achx");
        var acls = TestHelpers.SetupFreshAcls();
        AppCommands.Self.FileDialogService = new StubFileDialogService(target);
        acls.AnimationChains.Add(new AnimationChainSave { Name = "Run" });
        ProjectManager.Self.AnimationChainListSave = acls;

        await AppCommands.Self.SaveCurrentAnimationChainListAsync();

        var xml = File.ReadAllText(target);
        Assert.Contains("Run", xml);
    }
}

/// <summary>Test double that returns a pre-configured path (or null) from every dialog.</summary>
internal sealed class StubFileDialogService : IFileDialogService
{
    private readonly string? _path;

    public StubFileDialogService(string? path) => _path = path;

    public Task<string?> PickSaveFileAsync(string title, string defaultExtension, string fileTypeDescription)
        => Task.FromResult(_path);

    public Task<string?> PickOpenFileAsync(string title, string defaultExtension, string fileTypeDescription)
        => Task.FromResult(_path);
}
