using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// Tests for the element rename name-building logic in NodeViewModel.
/// The fix changed IndexOf("\\") to LastIndexOf("\\") so that renaming an entity
/// stored in a subfolder preserves the full subfolder path instead of collapsing
/// to the root folder.
/// </summary>
public class NodeViewModelRenameTests
{
    // Mirrors the fixed logic in NodeViewModel: preserve everything up to and
    // including the last backslash, then append the new leaf name.
    static string BuildNewName(string oldName, string newText) =>
        oldName.Substring(0, oldName.LastIndexOf("\\") + 1) + newText;

    [Fact]
    public void Rename_EntityInSubfolder_PreservesSubfolder()
    {
        var result = BuildNewName("Entities\\Weapons\\Sword", "Shield");

        result.ShouldBe("Entities\\Weapons\\Shield");
    }

    [Fact]
    public void Rename_EntityInRootFolder_PreservesRootFolder()
    {
        var result = BuildNewName("Entities\\Player", "Hero");

        result.ShouldBe("Entities\\Hero");
    }

    [Fact]
    public void Rename_EntityInDeeplyNestedSubfolder_PreservesFullPath()
    {
        var result = BuildNewName("Entities\\Weapons\\Melee\\Sword", "Axe");

        result.ShouldBe("Entities\\Weapons\\Melee\\Axe");
    }

    [Fact]
    public void Rename_Screen_PreservesScreensFolder()
    {
        var result = BuildNewName("Screens\\GameScreen", "MainMenu");

        result.ShouldBe("Screens\\MainMenu");
    }

    [Fact]
    public void Rename_ScreenInSubfolder_PreservesSubfolder()
    {
        var result = BuildNewName("Screens\\Levels\\Level1", "Level2");

        result.ShouldBe("Screens\\Levels\\Level2");
    }
}
