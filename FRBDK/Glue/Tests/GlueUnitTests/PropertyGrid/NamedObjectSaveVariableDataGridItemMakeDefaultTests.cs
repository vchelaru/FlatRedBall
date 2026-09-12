using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.PropertyGrid;
using OfficialPlugins.VariableDisplay;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2266: with a Text's Font set to <NONE> in the Variables tab, "Make Default" crashes Glue.
// "Make Default" in the grid sets DataGridItem.IsDefault = true, whose IsDefaultSet handler in
// NamedObjectSaveVariableDataGridItem runs SetVariableToDefault, element codegen, project save, and the
// plugin change notifications. This drives that exact entry point against the real Text ATI and a real
// on-disk project so every one of those steps runs for real.
[Collection(nameof(TaskManagerSequentialCollection))]
public class NamedObjectSaveVariableDataGridItemMakeDefaultTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly string _originalRelativeDirectory;
    private readonly bool _originalSynchronousMode;
    private readonly IUiThreadMarshaller _originalMarshaller;
    private readonly string _tempProjectDirectory;

    public NamedObjectSaveVariableDataGridItemMakeDefaultTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalMarshaller = TaskManager.UiThreadMarshaller;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);

        GlueState.Self.CurrentMainProject = vsProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        FlatRedBall.IO.FileManager.RelativeDirectory = _tempProjectDirectory + "\\";

        TaskManager.SynchronousMode = true;
        TaskManager.UiThreadMarshaller = new InlineUiThreadMarshaller();
    }

    public void Dispose()
    {
        GlueState.Self.CurrentElement = null;
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        TaskManager.UiThreadMarshaller = _originalMarshaller;

        try
        {
            Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    private static (ScreenSave screen, NamedObjectSave text) MakeScreenWithText()
    {
        var screen = new ScreenSave { Name = "Screens\\GameScreen" };
        ObjectFinder.Self.GlueProject.Screens.Add(screen);
        GlueState.Self.CurrentElement = screen;

        var text = new NamedObjectSave
        {
            InstanceName = "TextInstance",
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = "FlatRedBall.Graphics.Text",
        };
        screen.NamedObjects.Add(text);

        return (screen, text);
    }

    private static NamedObjectSaveVariableDataGridItem GetMember(DataUiGrid grid, string variableName)
    {
        var member = grid.Categories.SelectMany(category => category.Members)
            .OfType<NamedObjectSaveVariableDataGridItem>()
            .FirstOrDefault(item => item.NameOnInstance == variableName);
        member.ShouldNotBeNull($"the Text ATI should expose a {variableName} variable in the grid");
        return member;
    }

    [StaFact]
    public void MakeDefault_ShouldNotThrow_WhenFontWasSetToNone()
    {
        var (screen, text) = MakeScreenWithText();

        var grid = new DataUiGrid();
        NamedObjectVariableShowingLogic.UpdateShownVariables(grid, text, screen, text.GetAssetTypeInfo());
        var fontMember = GetMember(grid, "Font");

        // Step 1 of the report: pick <NONE> in the Font dropdown.
        fontMember.SetValue("<NONE>", SetPropertyCommitType.Full);
        TaskManager.Self.WaitForAllTasksFinished().Wait();

        // Step 2: "Make Default" - the grid's context menu sets IsDefault, nothing else.
        Should.NotThrow(() => fontMember.IsDefault = true);
        TaskManager.Self.WaitForAllTasksFinished().Wait();

        text.GetCustomVariable("Font").ShouldBeNull("Make Default removes the instruction");
    }

    // GitHub issue #2272: the same context menu, on the enum-typed variables the report names. Each one's
    // type is a name TypeManager has no primitive default for, so the throw that took Glue down was reached
    // without any <NONE> sentinel involved - just set the variable, then Make Default.
    [StaTheory]
    [InlineData("MaxWidthBehavior", FlatRedBall.Graphics.MaxWidthBehavior.Wrap)]
    [InlineData("HorizontalAlignment", FlatRedBall.Graphics.HorizontalAlignment.Right)]
    [InlineData("VerticalAlignment", FlatRedBall.Graphics.VerticalAlignment.Top)]
    [InlineData("BlendOperation", FlatRedBall.Graphics.BlendOperation.Add)]
    public void MakeDefault_ShouldNotThrow_WhenAnEnumVariableWasChanged(string variableName, object newValue)
    {
        var (screen, text) = MakeScreenWithText();

        var grid = new DataUiGrid();
        NamedObjectVariableShowingLogic.UpdateShownVariables(grid, text, screen, text.GetAssetTypeInfo());
        var member = GetMember(grid, variableName);

        // Step 1 of the report: change the variable.
        member.SetValue(newValue, SetPropertyCommitType.Full);
        TaskManager.Self.WaitForAllTasksFinished().Wait();

        // Step 2: "Make Default" - the grid's context menu sets IsDefault, nothing else.
        Should.NotThrow(() => member.IsDefault = true);
        TaskManager.Self.WaitForAllTasksFinished().Wait();

        text.GetCustomVariable(variableName).ShouldBeNull("Make Default removes the instruction");
    }

    private class InlineUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => action();
        public T Invoke<T>(Func<T> func) => func();
        public System.Threading.Tasks.Task Invoke(Func<System.Threading.Tasks.Task> func) => func();
        public System.Threading.Tasks.Task<T> Invoke<T>(Func<System.Threading.Tasks.Task<T>> func) => func();
        public void BeginInvoke(Action action) => action();
    }
}
