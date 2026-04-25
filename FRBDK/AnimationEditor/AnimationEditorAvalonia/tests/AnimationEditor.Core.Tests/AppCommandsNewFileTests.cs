using AnimationEditor.Core.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using Xunit;

namespace AnimationEditor.Core.Tests;

[Collection("SequentialSingletons")]
public class AppCommandsNewFileTests
{
    public AppCommandsNewFileTests()
    {
        TestHelpers.SetupFreshAcls();
    }

    [Fact]
    public void NewFile_CreatesEmptyAcls()
    {
        // Arrange – pre-populate
        ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(
            new AnimationChainSave { Name = "Existing" });

        // Act
        AppCommands.Self.NewFile();

        Assert.NotNull(ProjectManager.Self.AnimationChainListSave);
        Assert.Empty(ProjectManager.Self.AnimationChainListSave.AnimationChains);
    }

    [Fact]
    public void NewFile_ClearsFileName()
    {
        ProjectManager.Self.FileName = @"C:\some\file.achx";

        AppCommands.Self.NewFile();

        Assert.True(string.IsNullOrEmpty(ProjectManager.Self.FileName));
    }

    [Fact]
    public void NewFile_ClearsSelectedChain()
    {
        var chain = new AnimationChainSave { Name = "A" };
        ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(chain);
        SelectedState.Self.SelectedChain = chain;

        AppCommands.Self.NewFile();

        Assert.Null(SelectedState.Self.SelectedChain);
    }

    [Fact]
    public void NewFile_ClearsSelectedFrame()
    {
        var chain = new AnimationChainSave { Name = "A" };
        var frame = new AnimationFrameSave { FrameLength = 0.1f };
        chain.Frames.Add(frame);
        ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(chain);
        SelectedState.Self.SelectedChain = chain;
        SelectedState.Self.SelectedFrame = frame;

        AppCommands.Self.NewFile();

        Assert.Null(SelectedState.Self.SelectedFrame);
    }

    [Fact]
    public void NewFile_FiresRefreshTreeViewRequested()
    {
        bool fired = false;
        AppCommands.Self.RefreshTreeViewRequested += () => fired = true;

        AppCommands.Self.NewFile();

        Assert.True(fired);
    }

    [Fact]
    public void NewFile_FiresAnimationChainsChanged()
    {
        bool fired = false;
        ApplicationEvents.Self.AnimationChainsChanged += () => fired = true;

        AppCommands.Self.NewFile();

        Assert.True(fired);
    }

    [Fact]
    public void NewFile_CalledTwice_StillLeavesEmptyAcls()
    {
        AppCommands.Self.NewFile();
        AppCommands.Self.NewFile();

        Assert.Empty(ProjectManager.Self.AnimationChainListSave.AnimationChains);
    }
}
