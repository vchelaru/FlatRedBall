using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Renaming or deleting a Screen/Entity in an FRB2 project that opted into code generation
/// (<see cref="GlueProjectSave.GenerateCode"/>) has to take the generated/custom .cs pair with it, the
/// same way FRB1's rename and delete do.
/// </summary>
/// <remarks>
/// FRB1's paths reconcile .csproj items; an FRB2 project has none, and its code lives under
/// Content/FrbEditor/ beside the JSON rather than beside the .csproj. See GitHub issue #2060.
/// </remarks>
[Collection("Frb2ProjectLoad")]
public class Frb2RenameDeleteTests : DeleteDialogTestBase
{
    /// <summary>
    /// The same opt-in a user performs: add the elements, turn the setting on in the .gluj, reopen. The
    /// reload is what generates the pair, so it is the setup rather than an artifact.
    /// </summary>
    static async Task<string> LoadOptedIn(string root)
    {
        var csprojPath = Frb2ProjectFixture.Write(root);
        await GoldProject.LoadInGlueAsync(csprojPath);

        await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen("GameScreen");
        await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(
            new AddEntityViewModel { Name = "PlayerEntity" });

        GlueState.Self.CurrentGlueProject.GenerateCode = true;
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();

        await GoldProject.LoadInGlueAsync(csprojPath);
        return csprojPath;
    }

    static IReadOnlyList<string> CodeFilesUnder(string root) =>
        Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Where(f => f != Frb2ProjectFixture.HandWrittenCodeFile)
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

    [StaFact]
    public async Task RenamingAScreen_MovesItsGeneratedAndCustomFiles_RatherThanOrphaningThem()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2RenameCode_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);
        var screen = GlueState.Self.CurrentGlueProject.Screens.Single(item => item.Name == @"Screens\GameScreen");

        await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(
            screen, @"Screens\RenamedScreen", showRenameWindow: false);

        Assert.Equal(
            new[]
            {
                "Content/FrbEditor/Entities/PlayerEntity.Generated.cs",
                "Content/FrbEditor/Entities/PlayerEntity.cs",
                "Content/FrbEditor/Screens/RenamedScreen.Generated.cs",
                "Content/FrbEditor/Screens/RenamedScreen.cs",
            }.OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase),
            CodeFilesUnder(temp.Root));
    }

    [StaFact]
    public async Task RenamingAScreen_CarriesTheUsersCustomCodeOver_UnderTheNewClassName()
    {
        // The custom half is the user's, so a rename moves and edits it rather than regenerating it. The
        // stub the generator writes for the new name also says "partial class RenamedScreen", so the
        // assertion has to be the hand-written body - otherwise a rename that silently dropped the user's
        // code and made a fresh stub reads as a pass.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2RenameClass_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);
        var screen = GlueState.Self.CurrentGlueProject.Screens.Single(item => item.Name == @"Screens\GameScreen");

        var customFile = Path.Combine(temp.Root, "Content", "FrbEditor", "Screens", "GameScreen.cs");
        File.WriteAllText(customFile, File.ReadAllText(customFile)
            .Replace("partial class GameScreen", "partial class GameScreen // HandWrittenMarker"));

        await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(
            screen, @"Screens\RenamedScreen", showRenameWindow: false);

        var customCode = File.ReadAllText(Path.Combine(
            temp.Root, "Content", "FrbEditor", "Screens", "RenamedScreen.cs"));

        Assert.Contains("HandWrittenMarker", customCode);
        Assert.Contains("partial class RenamedScreen", customCode);
        Assert.DoesNotContain("GameScreen", customCode);
    }

    [StaFact]
    public async Task MovingAnEntityIntoASubfolder_TakesItsGeneratedAndCustomFilesWithIt()
    {
        // Dragging an element onto a folder in the tree is a rename - DragDropManager.MoveElementToDirectory
        // calls RenameElement with the new qualified name - so it reconciles files through the same path.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2MoveToFolder_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);
        var entity = GlueState.Self.CurrentGlueProject.Entities.Single(item => item.Name == @"Entities\PlayerEntity");

        await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(
            entity, @"Entities\Characters\PlayerEntity", showRenameWindow: false);

        Assert.Equal(
            new[]
            {
                "Content/FrbEditor/Entities/Characters/PlayerEntity.Generated.cs",
                "Content/FrbEditor/Entities/Characters/PlayerEntity.cs",
                "Content/FrbEditor/Screens/GameScreen.Generated.cs",
                "Content/FrbEditor/Screens/GameScreen.cs",
            }.OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase),
            CodeFilesUnder(temp.Root));
    }

    [StaFact]
    public async Task DeletingAnEntity_DeletesItsGeneratedAndCustomFiles()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2DeleteCode_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);
        var entity = GlueState.Self.CurrentGlueProject.Entities.Single(item => item.Name == @"Entities\PlayerEntity");

        AnswerDeleteDialog();

        await GlueCommands.Self.DialogCommands.AskToRemoveEntityAsync(entity);

        Assert.Equal(
            new[]
            {
                "Content/FrbEditor/Screens/GameScreen.Generated.cs",
                "Content/FrbEditor/Screens/GameScreen.cs",
            }.OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase),
            CodeFilesUnder(temp.Root));
    }

    [StaFact]
    public async Task TheDeleteDialog_ListsTheGeneratedAndCustomFiles_WhereTheyActuallyAre()
    {
        // The dialog shows the user the files a delete is about to remove. Composed the FRB1 way they
        // would be listed beside the .csproj, three directories from where they really are.
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();

        using var temp = new TempDir("Frb2DeletePlan_");
        using var dialogs = new RecordedChoiceDialogs();

        await LoadOptedIn(temp.Root);
        var entity = GlueState.Self.CurrentGlueProject.Entities.Single(item => item.Name == @"Entities\PlayerEntity");

        AnswerDeleteDialog(confirm: false);

        await GlueCommands.Self.DialogCommands.AskToRemoveEntityAsync(entity);

        var listed = TheOnlyDeleteDialog.FilesToRemove.Select(item => item.Replace('\\', '/')).ToList();

        Assert.Contains(listed, item => item.EndsWith("Content/FrbEditor/Entities/PlayerEntity.Generated.cs"));
        Assert.Contains(listed, item => item.EndsWith("Content/FrbEditor/Entities/PlayerEntity.cs"));
    }
}
