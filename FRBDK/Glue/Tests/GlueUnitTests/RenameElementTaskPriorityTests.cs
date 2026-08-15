using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Tasks;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests;

/// <summary>
/// GitHub issue #2085 (originally reported on a different machine, lost before it was TDD'd): adding a new
/// Entity queues its follow-up codegen (ElementCommands.AddEntity -> GenerateCodeCommands.GenerateElementCode)
/// at TaskManager's AddOrMoveToEnd tier, but ElementCommands.RenameElement queues its own work at AddAsync's
/// default of Fifo. Per glue-task-manager, tier beats enqueue order - a Fifo task always runs before an
/// AddOrMoveToEnd task, regardless of which was queued first. So a rename that follows an add can run before
/// the add's own deferred codegen finishes, instead of after it as intended. The fix is for RenameElement to
/// also queue AddOrMoveToEnd, so it queues behind (and runs after) any pending add-triggered codegen for the
/// same element.
/// </summary>
public class RenameElementTaskPriorityTests
{
    public RenameElementTaskPriorityTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    [Fact]
    public async Task RenameElement_QueuesItsTaskAddOrMoveToEnd_SoItRunsAfterPendingAddCodegen()
    {
        var entity = new EntitySave { Name = "Entities\\OldName" };
        ObjectFinder.Self.GlueProject.Entities.Add(entity);

        var recorded = new List<TaskExecutionPreference>();
        void Handler(TaskEvent evt, GlueTaskBase task)
        {
            if (task.DisplayInfo != null && task.DisplayInfo.StartsWith("Renaming "))
            {
                recorded.Add(task.TaskExecutionPreference);
            }
        }
        TaskManager.Self.TaskAddedOrRemoved += Handler;
        try
        {
            // The preference is recorded at enqueue time, before the queued action runs, so this test does
            // not depend on (and does not need to tolerate failures from) the rest of RenameElement's real
            // file-system side effects actually succeeding in a bare test host.
            try
            {
                await GlueCommands.Self.GluxCommands.EntityCommands.RenameElement(entity, "Entities\\NewName", showRenameWindow: false);
            }
            catch
            {
                // Only the queued task's priority is under test here - see comment above.
            }
        }
        finally
        {
            TaskManager.Self.TaskAddedOrRemoved -= Handler;
        }

        recorded.ShouldNotBeEmpty();
        recorded.ShouldAllBe(p => p == TaskExecutionPreference.AddOrMoveToEnd);
    }
}
