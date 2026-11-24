using System.Linq;
using EditorObjects.IoC;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Moq;
using OfficialPlugins.ErrorPlugin;
using OfficialPlugins.ErrorPlugin.Logic;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ErrorPlugin;

/// <summary>
/// The Errors tab is filled on project load by the editor's <see cref="ProjectLoadOperations.RefreshErrorList"/>,
/// not by the Error plugin's load reaction, so GlueCli's model-only load does not compute the errors twice.
/// </summary>
public class ErrorListLoadRefreshTests
{
    public ErrorListLoadRefreshTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public void RefreshErrorList_ShouldRefreshErrors_WhenTheProjectChecksErrors()
    {
        var refreshCommands = new Mock<IRefreshCommands>();

        CreateOperations(refreshCommands, new GlueProjectSave()).RefreshErrorList();

        refreshCommands.Verify(item => item.RefreshErrors(), Times.Once);
    }

    [Fact]
    public void RefreshErrorList_ShouldNotRefreshErrors_WhenTheProjectSuppressesErrorChecking()
    {
        var refreshCommands = new Mock<IRefreshCommands>();

        CreateOperations(refreshCommands, new GlueProjectSave { IsSuppressingAutomaticErrorChecking = true })
            .RefreshErrorList();

        refreshCommands.Verify(item => item.RefreshErrors(), Times.Never);
    }

    [Fact]
    public void StartUp_ShouldNotReactToLoadedGlux()
    {
        var originalAction = RefreshCommands.RefreshErrorsAction;
        var errorManager = Container.Get<GlueErrorManager>();
        var reportersBefore = errorManager.ErrorReporters.ToList();
        var plugin = new MainErrorPlugin();
        try
        {
            plugin.StartUp();

            plugin.ReactToLoadedGlux.ShouldBeNull();
        }
        finally
        {
            RefreshCommands.RefreshErrorsAction = originalAction;
            errorManager.ErrorReporters.RemoveAll(item =>
                item is ErrorCreateRemoveLogic && !reportersBefore.Contains(item));
        }
    }

    static ProjectLoadOperations CreateOperations(Mock<IRefreshCommands> refreshCommands, GlueProjectSave project)
    {
        var glueCommands = new Mock<IGlueCommands>();
        glueCommands.Setup(item => item.RefreshCommands).Returns(refreshCommands.Object);
        var glueState = new Mock<IGlueState>();
        glueState.Setup(item => item.CurrentGlueProject).Returns(project);
        return new ProjectLoadOperations(glueCommands.Object, glueState.Object);
    }
}
