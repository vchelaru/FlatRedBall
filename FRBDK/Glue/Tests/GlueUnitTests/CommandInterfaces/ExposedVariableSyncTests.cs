using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// An exposed (tunneled) variable and the instance member it points at are supposed to hold the same value, and
/// the generated code relies on that: the instance value is written first and the exposed default is written
/// over it later, so a disagreement makes the object "switch back" to the exposed value. See GitHub issue #1728.
/// Setting an instance value one at a time keeps the exposed default in step; setting several at once
/// (<see cref="IGluxCommands.SetVariableOnList"/>, used by multi-select edits and edits pushed from the game) must too.
/// </summary>
// Replaces ObjectFinder.Self.GlueProject and TaskManager.SynchronousMode, so it runs in the sequential collection.
// The project is built in memory on purpose: loading a sample project registers every plugin's asset types for
// the rest of the test run, which changes what unrelated codegen tests generate.
[Collection(nameof(TaskManagerSequentialCollection))]
public class ExposedVariableSyncTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;

    public ExposedVariableSyncTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;

        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        TaskManager.SynchronousMode = true;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        TaskManager.SynchronousMode = _originalSynchronousMode;
    }

    private static (ScreenSave screen, NamedObjectSave sprite, CustomVariable exposed) AddScreenWithExposedRed()
    {
        var screen = new ScreenSave { Name = "Screens\\SyncScreen" };
        ObjectFinder.Self.GlueProject.Screens.Add(screen);

        var sprite = new NamedObjectSave
        {
            InstanceName = "SpriteInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Sprite"
        };
        screen.NamedObjects.Add(sprite);

        var exposed = new CustomVariable
        {
            Name = "SpriteInstanceRed",
            Type = "float",
            SourceObject = sprite.InstanceName,
            SourceObjectProperty = "Red",
            DefaultValue = 0.5f
        };
        screen.CustomVariables.Add(exposed);

        return (screen, sprite, exposed);
    }

    // The single-edit path (SetVariableOnAsync) also refreshes the property grid, which needs a loaded project, so
    // this exercises the sync step it ends in directly. The list path below goes through the real command.
    [Fact]
    public void CopyingAnInstanceValueToTunneledVariables_UpdatesTheExposedDefault()
    {
        var (screen, sprite, exposed) = AddScreenWithExposedRed();

        sprite.SetVariable("Red", 0.2f);
        NamedObjectSetVariableLogic.CopyInstanceValueToTunneledVariables("Red", sprite, screen);

        exposed.DefaultValue.ShouldBe(0.2f);
    }

    [Fact]
    public async Task SettingInstanceValuesAsAList_UpdatesTheExposedDefault()
    {
        var (_, sprite, exposed) = AddScreenWithExposedRed();

        await GlueCommands.Self.GluxCommands.SetVariableOnList(
            new List<NosVariableAssignment>
            {
                new NosVariableAssignment { NamedObjectSave = sprite, VariableName = "Red", Value = 0.2f }
            },
            performSaveAndGenerateCode: false, updateUi: false, recordUndo: false);

        exposed.DefaultValue.ShouldBe(0.2f);
    }
}
