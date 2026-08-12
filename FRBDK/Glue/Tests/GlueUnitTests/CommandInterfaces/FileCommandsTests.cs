using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using Shouldly;

namespace GlueUnitTests.CommandInterfaces;

public class FileCommandsTests
{
    // Regression test: when Windows has no file association for an extension (e.g. .achx) and the
    // user agrees to "set the association", Glue used to just retry the exact same plain ShellExecute
    // that got it into this branch in the first place, which fails again with the same
    // Win32Exception (0x800401F5, ERROR_NO_ASSOCIATION) instead of showing Windows' "Open With" picker.
    [Fact]
    public void CreateOpenWithDialogStartInfo_ShouldUseOpenAsVerb_SoWindowsShowsThePicker()
    {
        var startInfo = FileCommands.CreateOpenWithDialogStartInfo(@"E:\Content\ResonatorAnimation.achx");

        startInfo.Verb.ShouldBe("openas");
        startInfo.UseShellExecute.ShouldBeTrue();
        startInfo.FileName.ShouldBe(@"E:\Content\ResonatorAnimation.achx");
    }
}
