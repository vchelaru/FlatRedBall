using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CommandInterfaces;

/// <summary>
/// ElementCommands.SetStateVariableValue and SetStateCategoryExcludedVariablesAsync, which the State Data tab
/// and GlueCli's state commands share.
/// </summary>
public class StateVariableCommandsTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public StateVariableCommandsTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        StateRecordingPlugin.EnsureRegistered();
        StateRecordingPlugin.Events.Clear();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
    }

    public void Dispose() => ObjectFinder.Self.GlueProject = _originalGlueProject;

    /// <summary>A state in a category of an entity in a fresh project, since StateSave.SetValue looks up its element.</summary>
    private static (StateSaveCategory category, StateSave state) CreateState()
    {
        var state = new StateSave { Name = "Wounded" };
        var category = new StateSaveCategory { Name = "Health" };
        category.States.Add(state);
        var entity = new EntitySave { Name = "Entities\\Enemy" };
        entity.CustomVariables.Add(new CustomVariable { Name = "Speed", Type = "float" });
        entity.StateCategoryList.Add(category);
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
        ObjectFinder.Self.GlueProject.Entities.Add(entity);
        return (category, state);
    }

    [Fact]
    public void SetStateVariableValue_ShouldSetTheValue_AndNotifyPlugins()
    {
        var (category, state) = CreateState();

        GlueCommands.Self.GluxCommands.ElementCommands.SetStateVariableValue(state, category, "Speed", 80f);

        state.InstructionSaves.Single(item => item.Member == "Speed").Value.ShouldBe(80f);
        StateRecordingPlugin.Events.ShouldBe(new[] { "VariableChanged Wounded Speed" });
    }

    [Fact]
    public void SetStateVariableValue_ShouldRemoveTheValue_WhenValueIsNull()
    {
        var (category, state) = CreateState();
        state.SetValue("Speed", 80f);

        GlueCommands.Self.GluxCommands.ElementCommands.SetStateVariableValue(state, category, "Speed", null);

        state.InstructionSaves.ShouldBeEmpty();
        StateRecordingPlugin.Events.ShouldBe(new[] { "VariableChanged Wounded Speed" });
    }

    [Fact]
    public async Task SetStateCategoryExcludedVariablesAsync_ShouldReplaceTheList_AndNotifyEachChange()
    {
        var category = new StateSaveCategory { Name = "Health" };
        category.ExcludedVariables.AddRange(new[] { "Speed", "Drag" });

        var changed = await GlueCommands.Self.GluxCommands.ElementCommands
            .SetStateCategoryExcludedVariablesAsync(category, new[] { "Drag", "Color" });

        changed.ShouldBeTrue();
        category.ExcludedVariables.ShouldBe(new[] { "Drag", "Color" });
        StateRecordingPlugin.Events.ShouldBe(new[] { "Excluded Color", "Included Speed" });
    }

    [Fact]
    public async Task SetStateCategoryExcludedVariablesAsync_ShouldReturnFalse_WhenNothingChanges()
    {
        var category = new StateSaveCategory { Name = "Health" };
        category.ExcludedVariables.Add("Speed");

        var changed = await GlueCommands.Self.GluxCommands.ElementCommands
            .SetStateCategoryExcludedVariablesAsync(category, new[] { "Speed" });

        changed.ShouldBeFalse();
        StateRecordingPlugin.Events.ShouldBeEmpty();
    }

    private class StateRecordingPlugin : PluginBase
    {
        private static bool _registered;

        public static List<string> Events { get; } = new();

        public static void EnsureRegistered()
        {
            if (!_registered)
            {
                PluginManager.RegisterPlugin(new StateRecordingPlugin());
                _registered = true;
            }
        }

        public override string FriendlyName => nameof(StateRecordingPlugin);
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            ReactToStateVariableChanged += (state, _, variableName) => Events.Add($"VariableChanged {state.Name} {variableName}");
            ReactToStateCategoryExcludedVariablesChanged += (_, variableName, action) => Events.Add($"{action} {variableName}");
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
