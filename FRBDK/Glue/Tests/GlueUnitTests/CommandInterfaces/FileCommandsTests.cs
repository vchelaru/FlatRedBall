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

    // GitHub issue #2176: a user profile folder containing a space (e.g. "Vic Personal") broke
    // "open in Gum" because the file path was passed via ProcessStartInfo's raw Arguments string,
    // which splits on whitespace before the child process ever sees it.
    [Fact]
    public void CreateResolvedAppStartInfo_KeepsFileNameAsOneArgument_EvenWithSpaces()
    {
        var startInfo = FileCommands.CreateResolvedAppStartInfo(
            @"C:\Users\Vic Personal\Documents\GitHub\Gum\Gum\bin\Debug\Gum.exe",
            @"C:\Users\Vic Personal\Documents\FlatRedBallProjects\FRB_2_10\FRB_2_10.Common\Content\FrbEditor\GumProject\GumProject.gumx");

        startInfo.FileName.ShouldBe(@"C:\Users\Vic Personal\Documents\GitHub\Gum\Gum\bin\Debug\Gum.exe");
        startInfo.ArgumentList.Count.ShouldBe(1);
        startInfo.ArgumentList[0].ShouldBe(
            @"C:\Users\Vic Personal\Documents\FlatRedBallProjects\FRB_2_10\FRB_2_10.Common\Content\FrbEditor\GumProject\GumProject.gumx");
    }
}
