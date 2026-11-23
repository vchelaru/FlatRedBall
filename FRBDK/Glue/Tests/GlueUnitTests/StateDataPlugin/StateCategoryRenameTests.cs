using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.StateDataPlugin.ViewModels;
using Shouldly;
using Xunit;

namespace GlueUnitTests.StateDataPlugin;

/// <summary>
/// Renaming a state category renames what refers to it by type or by its Current{Category}State property
/// (issue #2485). <see cref="StateSaveCategoryRenameLogic"/> is what the State Data tab's rename calls.
/// </summary>
public class StateCategoryRenameTests : IDisposable
{
    readonly GlueProjectSave? _projectBefore;

    public StateCategoryRenameTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _projectBefore = ObjectFinder.Self.GlueProject;
    }

    public void Dispose() => ObjectFinder.Self.GlueProject = _projectBefore;

    [Fact]
    public void Rename_ShouldRenameEveryReference_InTheOwnerDerivedElementsAndInstances()
    {
        var owner = new EntitySave { Name = "Entities\\Base" };
        var mood = new StateSaveCategory { Name = "Mood" };
        mood.States.Add(new StateSave { Name = "Happy" });
        owner.StateCategoryList.Add(mood);
        var other = new StateSaveCategory { Name = "Other" };
        other.ExcludedVariables.Add("CurrentMoodState");
        var otherState = new StateSave { Name = "Calm" };
        otherState.InstructionSaves.Add(new InstructionSave { Member = "CurrentMoodState", Value = "Happy", Type = "Mood" });
        other.States.Add(otherState);
        owner.StateCategoryList.Add(other);
        owner.CustomVariables.Add(new CustomVariable { Name = "CurrentMoodState", Type = "Entities.Base.Mood", DefaultValue = "Happy" });
        owner.CustomVariables.Add(new CustomVariable { Name = "Fallback", Type = "Mood" });
        owner.CustomVariables.Add(new CustomVariable { Name = "Maybe", Type = "Entities.Base.Mood?" });
        owner.Events.Add(new EventResponseSave { EventName = "AfterMoodSet", SourceVariable = "CurrentMoodState" });

        var derived = new EntitySave { Name = "Entities\\Derived", BaseEntity = "Entities\\Base" };
        derived.CustomVariables.Add(new CustomVariable { Name = "CurrentMoodState", Type = "Entities.Base.Mood", DefinedByBase = true });

        // Its own Mood category is a different type: nothing in it follows.
        var unrelated = new EntitySave { Name = "Entities\\Unrelated" };
        unrelated.StateCategoryList.Add(new StateSaveCategory { Name = "Mood" });
        unrelated.CustomVariables.Add(new CustomVariable { Name = "CurrentMoodState", Type = "Mood" });

        var screen = new ScreenSave { Name = "Screens\\Level" };
        var hero = new NamedObjectSave { InstanceName = "Hero", SourceType = SourceType.Entity, SourceClassType = "Entities\\Derived" };
        hero.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "CurrentMoodState", Value = "Happy" });
        screen.NamedObjects.Add(hero);
        var stranger = new NamedObjectSave { InstanceName = "Stranger", SourceType = SourceType.Entity, SourceClassType = "Entities\\Unrelated" };
        stranger.InstructionSaves.Add(new CustomVariableInNamedObject { Member = "CurrentMoodState", Value = "Happy" });
        screen.NamedObjects.Add(stranger);
        screen.CustomVariables.Add(new CustomVariable
        {
            Name = "HeroMood", Type = "Entities.Base.Mood", SourceObject = "Hero", SourceObjectProperty = "CurrentMoodState"
        });
        screen.CustomVariables.Add(new CustomVariable
        {
            Name = "StrangerMood", Type = "Mood", SourceObject = "Stranger", SourceObjectProperty = "CurrentMoodState"
        });

        var project = new GlueProjectSave();
        project.Entities.AddRange(new[] { owner, derived, unrelated });
        project.Screens.Add(screen);
        ObjectFinder.Self.GlueProject = project;

        var changed = StateSaveCategoryRenameLogic.Rename(project, owner, mood, "Feeling");

        mood.Name.ShouldBe("Feeling");
        owner.CustomVariables.Select(item => (item.Name, item.Type)).ShouldBe(new[]
        {
            ("CurrentFeelingState", "Entities.Base.Feeling"), ("Fallback", "Feeling"), ("Maybe", "Entities.Base.Feeling?")
        });
        owner.CustomVariables[0].DefaultValue.ShouldBe("Happy");
        other.ExcludedVariables.ShouldBe(new[] { "CurrentFeelingState" });
        otherState.InstructionSaves.Single().Member.ShouldBe("CurrentFeelingState");
        otherState.InstructionSaves.Single().Type.ShouldBe("Feeling");
        owner.Events.Single().SourceVariable.ShouldBe("CurrentFeelingState");
        derived.CustomVariables.Single().Name.ShouldBe("CurrentFeelingState");
        derived.CustomVariables.Single().Type.ShouldBe("Entities.Base.Feeling");
        hero.InstructionSaves.Single().Member.ShouldBe("CurrentFeelingState");
        hero.InstructionSaves.Single().Value.ShouldBe("Happy");
        screen.CustomVariables[0].SourceObjectProperty.ShouldBe("CurrentFeelingState");
        screen.CustomVariables[0].Type.ShouldBe("Entities.Base.Feeling");

        unrelated.CustomVariables.Single().Name.ShouldBe("CurrentMoodState");
        unrelated.CustomVariables.Single().Type.ShouldBe("Mood");
        stranger.InstructionSaves.Single().Member.ShouldBe("CurrentMoodState");
        screen.CustomVariables[1].SourceObjectProperty.ShouldBe("CurrentMoodState");
        screen.CustomVariables[1].Type.ShouldBe("Mood");

        changed.ShouldBe(new GlueElement[] { owner, derived, screen }, ignoreOrder: true);
    }

    /// <summary>
    /// The issue's repro through the State Data tab's view model: PlayerBall exposes CurrentDashCategoryState and
    /// GameScreen's PlayerBall1 sets it. Before the fix the variable and the instance kept the old name, and the
    /// project no longer compiled.
    /// </summary>
    [StaFact]
    [Trait("Category", "BuildSmoke")]
    public async Task RenamingDashCategoryInTheStateDataTab_ShouldRenameItsVariables_AndTheProjectShouldBuild()
    {
        GlueTestBootstrap.EnsureGameProjectPluginsRegistered();
        using var project = GoldProject.CopyOutOfRepo("Samples/Beefball");
        var csproj = Path.Combine(project.Root, "Beefball", "Beefball.csproj");
        GoldProject.DeleteGeneratedCode(project.Root);
        await GoldProject.LoadInGlueAsync(csproj);

        var playerBall = ObjectFinder.Self.GetEntitySave("Entities\\PlayerBall");
        var dashCategory = playerBall.StateCategoryList.Single(item => item.Name == "DashCategory");
        (await GlueCommands.Self.GluxCommands.ElementCommands.AddStateCategoryCustomVariableToElementAsync(dashCategory, playerBall))
            .Succeeded.ShouldBeTrue();
        var gameScreen = ObjectFinder.Self.GetScreenSave("Screens\\GameScreen");
        gameScreen.GetNamedObjectRecursively("PlayerBall1")!.SetVariable("CurrentDashCategoryState", "Tired");
        await TaskManager.Self.WaitForAllTasksFinished();

        var viewModel = new StateCategoryViewModel(dashCategory, playerBall);
        viewModel.Name = "Dash";
        await TaskManager.Self.WaitForAllTasksFinished();

        GlueTestBootstrap.RecordedDialogMessages.ShouldBeEmpty();
        ErrorRecordingPlugin.Errors.ShouldBeEmpty();

        var entityJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.glej"))).RootElement;
        entityJson.GetProperty("StateCategoryList").EnumerateArray().Select(item => item.GetProperty("Name").GetString())
            .ShouldContain("Dash");
        var variable = entityJson.GetProperty("CustomVariables").EnumerateArray()
            .Single(item => item.GetProperty("Name").GetString()!.StartsWith("CurrentDash"));
        variable.GetProperty("Name").GetString().ShouldBe("CurrentDashState");
        // CustomVariable.Type is stored as a property.
        variable.GetProperty("Properties").EnumerateArray().Single(item => item.GetProperty("Name").GetString() == "Type")
            .GetProperty("Value").GetString().ShouldBe("Entities.PlayerBall.Dash");

        var screenText = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.glsj"));
        screenText.ShouldContain("\"CurrentDashState\"", Case.Sensitive);
        screenText.ShouldNotContain("CurrentDashCategoryState", Case.Sensitive);

        var generatedScreen = File.ReadAllText(Path.Combine(project.Root, "Beefball", "Screens", "GameScreen.Generated.cs"));
        generatedScreen.ShouldContain("PlayerBall1.CurrentDashState = Entities.PlayerBall.Dash.Tired;", Case.Sensitive);

        // Beefball's hand-written PlayerBall.cs uses the category too. Glue does not edit hand-written code, so do
        // what the user would; the build then proves the generated code compiles.
        var customCode = Path.Combine(project.Root, "Beefball", "Entities", "PlayerBall.cs");
        File.WriteAllText(customCode, File.ReadAllText(customCode).Replace("DashCategory", "Dash"));

        var (exitCode, output) = NestedDotnetCli.Run($"build \"{csproj}\" -c Debug");
        exitCode.ShouldBe(0, $"dotnet build failed after renaming the category:\n{output}");
    }
}
