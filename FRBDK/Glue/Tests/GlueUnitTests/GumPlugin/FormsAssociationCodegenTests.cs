using System;
using System.Linq;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using GlueUnitTests.TestSupport;
using GlueUnitTests.Tasks;
using GumPlugin.CodeGeneration;
using Shouldly;

namespace GlueUnitTests.GumPluginTests;

// Issue #2332: GumIdb.Generated.cs registered component-to-Forms associations through
// FrameworkElement.DefaultFormsComponents, which Gum marked [Obsolete("Use DefaultFormsTemplates")],
// so every generated project carried a CS0618 warning it could not fix. At
// GluxVersions.GumFrameworkElementHasDefaultFormsTemplates the registration goes through
// DefaultFormsTemplates with a VisualTemplate; older projects keep the old line.
[Collection(nameof(TaskManagerSequentialCollection))]
public class FormsAssociationCodegenTests : IDisposable
{
    private readonly FlatRedBall.Glue.VSHelpers.Projects.VisualStudioProject _originalMainProject;
    private readonly GlueProjectSave _originalGlueProject;
    private readonly bool _originalSynchronousMode;
    private readonly GumProjectSave _originalGumProjectSave;
    private readonly string _tempProjectDirectory;

    public FormsAssociationCodegenTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject;
        _originalSynchronousMode = TaskManager.SynchronousMode;
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;

        var vsProject = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out _tempProjectDirectory);
        GlueState.Self.CurrentMainProject = vsProject;

        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = new GlueProjectSave
        {
            FileVersion = GlueProjectSave.LatestVersion
        };
        TaskManager.SynchronousMode = true;

        var gumProject = new GumProjectSave
        {
            FullFileName = System.IO.Path.Combine(_tempProjectDirectory, "GumProject", "GumProject.gumx"),
        };
        var button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        gumProject.Components.Add(button);
        Gum.Managers.ObjectFinder.Self.GumProjectSave = gumProject;
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject = _originalGlueProject;
        TaskManager.SynchronousMode = _originalSynchronousMode;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;

        try
        {
            System.IO.Directory.Delete(_tempProjectDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup; a stray temp dir isn't worth failing the test over
        }
    }

    [Fact]
    public void AtDefaultFormsTemplatesVersion_RegistersThroughDefaultFormsTemplates()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GumFrameworkElementHasDefaultFormsTemplates;

        var source = GueRuntimeTypeAssociationGenerator.Self.GetRuntimeRegistrationPartialClassContents(registerFormsAssociations: true);

        source.ShouldContain(
            "FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(FlatRedBall.Forms.Controls.Button)] = " +
            "new global::FlatRedBall.Forms.VisualTemplate(() => new global::" + GueDerivingClassCodeGenerator.GueRuntimeNamespace +
            ".Controls.ButtonStandardRuntime(fullInstantiation: true, tryCreateFormsObject: false));");
        source.ShouldNotContain("DefaultFormsComponents");
    }

    // FRB's TreeViewLogic (Engines/Forms, not shared with Gum) only consults DefaultFormsComponents for its
    // default item type, so a TreeViewItem registered through templates would silently stop being found.
    [Fact]
    public void AtDefaultFormsTemplatesVersion_TreeViewItemStaysOnDefaultFormsComponentsWithoutWarning()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GumFrameworkElementHasDefaultFormsTemplates;
        var treeViewItem = new ComponentSave { Name = "Controls/TreeViewItemStandard", BaseType = "Container" };
        treeViewItem.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TreeViewItemBehavior" });
        Gum.Managers.ObjectFinder.Self.GumProjectSave.Components.Add(treeViewItem);

        var source = GueRuntimeTypeAssociationGenerator.Self.GetRuntimeRegistrationPartialClassContents(registerFormsAssociations: true);

        var expectedLine =
            "FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.TreeViewItem)] = " +
            "typeof(global::" + GueDerivingClassCodeGenerator.GueRuntimeNamespace + ".Controls.TreeViewItemStandardRuntime);";
        var lines = source.Split('\n').Select(line => line.Trim()).ToList();
        var index = lines.IndexOf(expectedLine);
        index.ShouldBeGreaterThan(0, source);
        lines[index - 1].ShouldBe("#pragma warning disable CS0618");
        lines[index + 1].ShouldBe("#pragma warning restore CS0618");
        // The Button in the same project still goes through templates - the exception is per control, not per project.
        source.ShouldContain("DefaultFormsTemplates[typeof(FlatRedBall.Forms.Controls.Button)]");
    }

    [Fact]
    public void BelowDefaultFormsTemplatesVersion_RegistersThroughDefaultFormsComponents()
    {
        GlueTestBootstrap.EnsureGumPluginCodeGeneratorsInitialized();
        FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GumFrameworkElementHasDefaultFormsTemplates - 1;

        var source = GueRuntimeTypeAssociationGenerator.Self.GetRuntimeRegistrationPartialClassContents(registerFormsAssociations: true);

        source.ShouldContain(
            "FlatRedBall.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(FlatRedBall.Forms.Controls.Button)] = " +
            "typeof(global::" + GueDerivingClassCodeGenerator.GueRuntimeNamespace + ".Controls.ButtonStandardRuntime);");
        source.ShouldNotContain("DefaultFormsTemplates");
    }
}
