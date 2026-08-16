using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// GitHub issue #2085: duplicating an Entity and then renaming or deleting the duplicate was reported to
/// leave its generated files (.cs, .Generated.cs, Factory.Generated.cs) referenced in the .csproj after
/// they were physically deleted or moved, producing CS2001. These pin that both paths correctly remove the
/// duplicate's csproj entries - each also checks the entries exist beforehand, so a broken removal would
/// fail the test rather than pass it trivially.
/// </summary>
[Trait("Category", "BuildSmoke")]
public class DuplicateElementCsprojReferenceTests : DeleteDialogTestBase
{
    static async Task<EntitySave> AddAndDuplicateEntityAsync(string name)
    {
        var original = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = name });

        await GlueCommands.Self.GluxCommands.CopyGlueElement(original);

        var duplicate = (EntitySave)ObjectFinder.Self.GetElement(original.Name + "Copy");
        duplicate.ShouldNotBeNull();

        return duplicate;
    }

    static string[] OwnedFileRelativePaths(string elementName)
    {
        var strippedName = FileManager.RemovePath(elementName);

        return new[]
        {
            elementName.Replace("Entities\\", "Entities/") + ".cs",
            elementName.Replace("Entities\\", "Entities/") + ".Generated.cs",
            "Factories/" + strippedName + "Factory.Generated.cs",
        };
    }

    [StaFact]
    public async Task DuplicatingAndDeletingTheDuplicate_ShouldNotLeaveDanglingCsprojReferences()
    {
        using var project = await LoadFormsSampleAsync();

        var duplicate = await AddAndDuplicateEntityAsync("Issue2085Original");
        var mainProject = GlueState.Self.CurrentMainProject;
        var ownedFiles = OwnedFileRelativePaths(duplicate.Name);

        foreach (var relative in ownedFiles)
        {
            mainProject.IsFilePartOfProject(relative, BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue($"{relative} should be a csproj item before delete");
        }

        AnswerDeleteDialog();
        var wasRemoved = await GlueCommands.Self.DialogCommands.AskToRemoveEntityAsync(duplicate);
        wasRemoved.ShouldBeTrue();

        foreach (var relative in ownedFiles)
        {
            mainProject.IsFilePartOfProject(relative, BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeFalse($"{relative} should no longer be a csproj item after delete");
        }
    }

    [StaFact]
    public async Task DuplicatingAndRenamingTheDuplicate_ShouldNotLeaveDanglingCsprojReferencesToTheOldName()
    {
        using var project = await LoadFormsSampleAsync();

        var duplicate = await AddAndDuplicateEntityAsync("Issue2085RenameOriginal");
        var mainProject = GlueState.Self.CurrentMainProject;
        var oldOwnedFiles = OwnedFileRelativePaths(duplicate.Name);

        foreach (var relative in oldOwnedFiles)
        {
            mainProject.IsFilePartOfProject(relative, BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue($"{relative} should be a csproj item before rename");
        }

        await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(
            duplicate, "Entities\\Issue2085Renamed", showRenameWindow: false);

        foreach (var relative in oldOwnedFiles)
        {
            mainProject.IsFilePartOfProject(relative, BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeFalse($"{relative} (pre-rename name) should no longer be a csproj item after rename");
        }
    }
}
