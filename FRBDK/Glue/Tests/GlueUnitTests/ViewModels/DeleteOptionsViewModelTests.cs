using GlueFormsCore.ViewModels;
using Shouldly;
using System.Linq;
using Xunit;

namespace GlueUnitTests.ViewModels;

/// <summary>
/// The delete dialog lists the files it is about to delete while the user is still toggling options, so
/// the list has to track the options rather than being computed once. Unchecking "delete the Gum screen"
/// has to take the Gum screen's code files back out of the list, or the user approves a list that no
/// longer describes what happens. See GitHub issue #429.
/// </summary>
public class DeleteOptionsViewModelTests
{
    [Fact]
    public void FilesToRemove_ShouldStartWithTheFilesThatGoRegardless()
    {
        var viewModel = new DeleteOptionsViewModel();
        viewModel.AlwaysRemovedFiles.Add("Screens/GameScreen.cs");
        viewModel.RefreshFilesToRemove();

        viewModel.FilesToRemove.ShouldBe(new[] { "Screens/GameScreen.cs" });
    }

    [Fact]
    public void CheckingAnOption_ShouldAddItsFilesToTheList()
    {
        var viewModel = new DeleteOptionsViewModel();
        viewModel.AlwaysRemovedFiles.Add("Screens/GameScreen.cs");

        var option = viewModel.AddOption("Delete the Gum screen", isChecked: false);
        option.AdditionalFilesToRemove.Add("GumRuntimes/GameScreenRuntime.Generated.cs");

        viewModel.FilesToRemove.ShouldNotContain("GumRuntimes/GameScreenRuntime.Generated.cs");

        option.IsChecked = true;

        viewModel.FilesToRemove.ShouldContain("GumRuntimes/GameScreenRuntime.Generated.cs");
    }

    [Fact]
    public void UncheckingAnOption_ShouldTakeItsFilesBackOut()
    {
        var viewModel = new DeleteOptionsViewModel();
        var option = viewModel.AddOption("Delete the Gum screen");
        option.AdditionalFilesToRemove.Add("GumRuntimes/GameScreenRuntime.Generated.cs");
        viewModel.RefreshFilesToRemove();

        option.IsChecked = false;

        viewModel.FilesToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void FilesToRemove_ShouldNotListTheSameFileTwice()
    {
        var viewModel = new DeleteOptionsViewModel();
        viewModel.AlwaysRemovedFiles.Add("Screens/GameScreen.cs");

        var option = viewModel.AddOption("Something");
        option.AdditionalFilesToRemove.Add("Screens/GameScreen.cs");
        viewModel.RefreshFilesToRemove();

        viewModel.FilesToRemove.Count(item => item == "Screens/GameScreen.cs").ShouldBe(1);
    }

    // The list is assembled from a ReferencedFileSave's content-relative path, an element's code path and
    // a plugin's FilePath.FullPath, so left raw it showed backslashes next to forward slashes and full
    // paths next to relative ones in the same list.
    [Fact]
    public void FilesToRemoveDisplay_ShouldBeProjectRelative_WithForwardSlashes()
    {
        var viewModel = new DeleteOptionsViewModel { ProjectRootForDisplay = "C:/Projects/MyGame" };
        viewModel.AlwaysRemovedFiles.Add(@"C:\Projects\MyGame\Screens\NewScreen.cs");
        viewModel.AlwaysRemovedFiles.Add("C:/Projects/MyGame/GumRuntimes/NewScreenRuntime.Generated.cs");
        viewModel.RefreshFilesToRemove();

        viewModel.FilesToRemoveDisplay.ShouldBe(new[]
        {
            "Screens/NewScreen.cs",
            "GumRuntimes/NewScreenRuntime.Generated.cs"
        });
    }

    [Fact]
    public void FilesToRemoveDisplay_ShouldStayRelative_ForAFileOutsideTheProject()
    {
        var viewModel = new DeleteOptionsViewModel { ProjectRootForDisplay = "C:/Projects/MyGame" };
        viewModel.AlwaysRemovedFiles.Add("C:/Projects/SharedArt/Tileset.png");
        viewModel.RefreshFilesToRemove();

        viewModel.FilesToRemoveDisplay.Single().ShouldBe("../SharedArt/Tileset.png");
    }

    [Fact]
    public void FilesToRemove_ShouldNotTreatSeparatorDifferencesAsDifferentFiles()
    {
        var viewModel = new DeleteOptionsViewModel();
        viewModel.AlwaysRemovedFiles.Add(@"C:\Projects\MyGame\Screens\NewScreen.cs");
        viewModel.AlwaysRemovedFiles.Add("C:/Projects/MyGame/Screens/NewScreen.cs");
        viewModel.RefreshFilesToRemove();

        viewModel.FilesToRemove.Count.ShouldBe(1);
    }

    [Fact]
    public void AddOption_ShouldCheckTheOptionByDefault()
    {
        // Every option replaced a prompt whose expected answer was Yes, so an unattended Delete does the
        // same thing the old chain of popups did when the user clicked through it.
        new DeleteOptionsViewModel().AddOption("Reset the inheritance for Derived").IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void IsOptionChecked_ShouldBeFalse_ForATagNoOptionUses()
    {
        // A plugin that added no option must never read back "true" and act on a delete it didn't ask about.
        new DeleteOptionsViewModel().IsOptionChecked("SomeOtherPlugin.Option").ShouldBeFalse();
    }

    [Fact]
    public void IsOptionChecked_ShouldBeFalse_WhenTheUserUncheckedIt()
    {
        var viewModel = new DeleteOptionsViewModel();
        viewModel.AddOption("Delete the Gum screen", "GumPlugin.DeleteGumScreen").IsChecked = false;

        viewModel.IsOptionChecked("GumPlugin.DeleteGumScreen").ShouldBeFalse();
    }

    [Fact]
    public void FileAction_ShouldDefaultToRemovingAndDeleting()
    {
        // The listed files are the deleted element's own code and content; they go to the recycle bin.
        new DeleteOptionsViewModel().FileAction.ShouldBe(FileDeleteAction.RemoveAndDelete);
    }

    [Fact]
    public void PickingAFileActionRadioButton_ShouldClearTheOthers()
    {
        var viewModel = new DeleteOptionsViewModel { IsDoNothingWithFilesChecked = true };

        viewModel.FileAction.ShouldBe(FileDeleteAction.Nothing);
        viewModel.IsRemoveAndDeleteFilesChecked.ShouldBeFalse();
        viewModel.IsRemoveFilesFromProjectChecked.ShouldBeFalse();
    }
}
