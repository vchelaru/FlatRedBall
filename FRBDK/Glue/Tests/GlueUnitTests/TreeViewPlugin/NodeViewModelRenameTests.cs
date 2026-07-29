using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for NodeViewModel.BuildRenamedElementFullName, the production method the F2-rename flow
/// (NodeViewModel.HandleRenameThroughEdit) calls to compute the new full element name. Regression
/// coverage for #1871/#1872 (plain rename) and #1772 (duplicate-then-rename), both of which moved
/// the renamed entity out of its subfolder.
/// </summary>
public class NodeViewModelRenameTests
{
    [Fact]
    public void Rename_EntityInSubfolder_PreservesSubfolder()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Entities\\Weapons\\Sword", "Shield");

        result.ShouldBe("Entities\\Weapons\\Shield");
    }

    [Fact]
    public void Rename_EntityInRootFolder_PreservesRootFolder()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Entities\\Player", "Hero");

        result.ShouldBe("Entities\\Hero");
    }

    [Fact]
    public void Rename_EntityInDeeplyNestedSubfolder_PreservesFullPath()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Entities\\Weapons\\Melee\\Sword", "Axe");

        result.ShouldBe("Entities\\Weapons\\Melee\\Axe");
    }

    [Fact]
    public void Rename_Screen_PreservesScreensFolder()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Screens\\GameScreen", "MainMenu");

        result.ShouldBe("Screens\\MainMenu");
    }

    [Fact]
    public void Rename_ScreenInSubfolder_PreservesSubfolder()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Screens\\Levels\\Level1", "Level2");

        result.ShouldBe("Screens\\Levels\\Level2");
    }

    // #1772's exact repro: duplicate "Larva" in Entities\Enemies (producing "...\\LarvaCopy" per
    // GluxCommands.CopyGlueElement, which just appends "Copy" to the full name), then F2-rename it.
    [Fact]
    public void Rename_DuplicatedEntityInSubfolder_PreservesSubfolder()
    {
        var result = NodeViewModel.BuildRenamedElementFullName("Entities\\Enemies\\LarvaCopy", "DeepOne_Panel");

        result.ShouldBe("Entities\\Enemies\\DeepOne_Panel");
    }
}
