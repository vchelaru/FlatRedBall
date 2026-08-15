using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests;

/// <summary>
/// GluxCommands.AddNamedObjectToAsync (the task that actually mutates NamedObjects and updates the
/// tree/selection AddObjectAndBeginRename depends on - see glue-add-object-flow) must queue Asap, not
/// the AddAsync default of Fifo, or it sits behind unrelated Fifo-tier work and the "add feels instant"
/// goal of #2086 breaks. See glue-task-manager for why tier beats enqueue order.
/// </summary>
public class NewObjectTaskPriorityTests
{
    public NewObjectTaskPriorityTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public async Task AddNamedObjectToAsync_QueuesItsTaskAsap()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        var newNos = new NamedObjectSave { InstanceName = "TestInstance", SourceType = SourceType.FlatRedBallType };

        var recorded = new List<TaskExecutionPreference>();
        void Handler(TaskEvent evt, GlueTaskBase task)
        {
            if (task.DisplayInfo != null && task.DisplayInfo.Contains("Adding named object"))
            {
                recorded.Add(task.TaskExecutionPreference);
            }
        }
        TaskManager.Self.TaskAddedOrRemoved += Handler;
        try
        {
            // performSaveAndGenerateCode/updateUi: false - this test only cares about the queued
            // preference, not the real codegen/UI side effects of a full add.
            await GlueCommands.Self.GluxCommands.AddNamedObjectToAsync(
                newNos, entity, listToAddTo: null, selectNewNos: false,
                performSaveAndGenerateCode: false, updateUi: false);
        }
        finally
        {
            TaskManager.Self.TaskAddedOrRemoved -= Handler;
        }

        recorded.ShouldNotBeEmpty();
        recorded.ShouldAllBe(p => p == TaskExecutionPreference.Asap);
    }
}
