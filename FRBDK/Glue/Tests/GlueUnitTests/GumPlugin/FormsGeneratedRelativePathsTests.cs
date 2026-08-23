using System;
using System.Linq;
using Gum.DataTypes;
using GumPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GumPluginTests;

/// <summary>
/// GitHub issue #2185: Glue's load-time orphaned-csproj-entry reconciliation needs to know which
/// "Forms/...Forms.Generated.cs" paths the currently-loaded Gum project still owns, since that ownership
/// isn't expressible through the Screen/Entity model core Glue already understands. These pin
/// <see cref="CodeGeneratorManager.GetFormsGeneratedRelativePaths"/> - the source GumPlugin reports that
/// ownership from - independent of the plugin-call plumbing that reaches it.
/// </summary>
public class FormsGeneratedRelativePathsTests : IDisposable
{
    readonly GumProjectSave _originalGumProjectSave;

    public FormsGeneratedRelativePathsTests()
    {
        _originalGumProjectSave = Gum.Managers.ObjectFinder.Self.GumProjectSave;
    }

    public void Dispose()
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = _originalGumProjectSave;
    }

    [Fact]
    public void GetFormsGeneratedRelativePaths_ShouldReturnEmpty_WhenNoGumProjectIsLoaded()
    {
        Gum.Managers.ObjectFinder.Self.GumProjectSave = null;

        var result = CodeGeneratorManager.Self.GetFormsGeneratedRelativePaths().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetFormsGeneratedRelativePaths_ShouldReturnPathFor_EachScreenAndComponent()
    {
        var gumProject = new GumProjectSave();
        gumProject.Screens.Add(new ScreenSave { Name = "WrapUpScreenGum" });
        gumProject.Components.Add(new ComponentSave { Name = "EndgameDemo" });
        Gum.Managers.ObjectFinder.Self.GumProjectSave = gumProject;

        var result = CodeGeneratorManager.Self.GetFormsGeneratedRelativePaths().ToList();

        result.ShouldBe(new[]
        {
            "Forms/Screens/WrapUpScreenGumForms.Generated.cs",
            "Forms/Components/EndgameDemoForms.Generated.cs",
        }, ignoreOrder: true);
    }

    [Fact]
    public void GetFormsGeneratedRelativePaths_ShouldPreserveNestedComponentSubfolders()
    {
        // Matches the real-world repro from issue #2185: a component nested under a subfolder in the
        // .gumx (Components/Elements/SummaryColumn) generates to Forms/Components/Elements/SummaryColumnForms.Generated.cs.
        var gumProject = new GumProjectSave();
        gumProject.Components.Add(new ComponentSave { Name = "Elements/SummaryColumn" });
        Gum.Managers.ObjectFinder.Self.GumProjectSave = gumProject;

        var result = CodeGeneratorManager.Self.GetFormsGeneratedRelativePaths().ToList();

        result.ShouldBe(new[] { "Forms/Components/Elements/SummaryColumnForms.Generated.cs" });
    }
}
