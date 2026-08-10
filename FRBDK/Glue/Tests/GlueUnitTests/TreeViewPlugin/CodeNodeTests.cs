using System;
using System.IO;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Microsoft.Build.Evaluation;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using Shouldly;

namespace GlueUnitTests.TreeViewPlugin;

/// <summary>
/// The Code node under a Screen or Entity lists that element's .cs files. Everything in it is found by
/// enumerating the disk except the factory, which <see cref="CodeWriter.GetAllCodeFilesFor"/> appends
/// by name whether or not it was ever generated - so the tree offered a file that does not exist.
/// </summary>
public class CodeNodeTests : IDisposable
{
    readonly string _directory;
    readonly VisualStudioProject _originalMainProject;
    readonly GlueProjectSave _originalGlueProject;
    readonly string _originalRelativeDirectory;

    public CodeNodeTests()
    {
        GlueTestBootstrap.EnsureInitialized();

        _originalMainProject = GlueState.Self.CurrentMainProject;
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        _originalRelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;

        _directory = Path.Combine(Path.GetTempPath(), "CodeNode_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);

        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        GlueState.Self.CurrentMainProject = _originalMainProject;
        ObjectFinder.Self.GlueProject = _originalGlueProject;
        FlatRedBall.IO.FileManager.RelativeDirectory = _originalRelativeDirectory;

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    T UseProject<T>(Func<Project, T> construct, string projectName) where T : VisualStudioProject
    {
        var csprojPath = Path.Combine(_directory, projectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>
</Project>");
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();

        var project = construct(new Project(csprojPath, null, null, new ProjectCollection()));
        GlueState.Self.CurrentMainProject = project;
        FlatRedBall.IO.FileManager.RelativeDirectory = _directory + Path.DirectorySeparatorChar;
        return project;
    }

    static EntitySave PooledEntity() =>
        new() { Name = @"Entities\Bear", CreatedByOtherEntities = true };

    [Fact]
    public void GetAllCodeFilesFor_OmitsAFactoryThatWasNeverGenerated()
    {
        UseProject(p => new ClassLibraryProject(p), "Frb1Game");

        var files = CodeWriter.GetAllCodeFilesFor(PooledEntity());

        files.Select(file => file.NoPath)
            .ShouldNotContain("BearFactory.Generated.cs",
                "The factory file is not on disk, so the Code node must not offer it.");
    }

    [Fact]
    public void GetAllCodeFilesFor_StillListsAFactoryThatExists()
    {
        // The guard against over-fixing: a real factory still has to show up.
        UseProject(p => new ClassLibraryProject(p), "Frb1Game");
        var factoriesDirectory = Path.Combine(_directory, "Factories");
        Directory.CreateDirectory(factoriesDirectory);
        File.WriteAllText(Path.Combine(factoriesDirectory, "BearFactory.Generated.cs"), "// generated");

        var files = CodeWriter.GetAllCodeFilesFor(PooledEntity());

        files.Select(file => file.NoPath).ShouldContain("BearFactory.Generated.cs");
    }

    [StaFact]
    public void AnEntityInAnFrb2Project_HasNoCodeNode()
    {
        // FRB2 owns all of its own source and Glue writes none of it, so a node listing "this element's
        // generated code" describes nothing that exists.
        UseProject(p => new Frb2Project(p), "Frb2Game");

        var node = new GlueElementNodeViewModel(null, PooledEntity(), createChildrenNodes: true);

        node.Children.Select(child => child.Text).ShouldNotContain("Code");
    }

    [StaFact]
    public void AnEntityInANormalProject_StillHasACodeNode()
    {
        UseProject(p => new ClassLibraryProject(p), "Frb1Game");

        var node = new GlueElementNodeViewModel(null, PooledEntity(), createChildrenNodes: true);

        node.Children.Select(child => child.Text).ShouldContain("Code");
    }

    [StaFact]
    public void AnEntityInAnFrb2Project_HasNoEventsNode()
    {
        // Events exist to generate the element's .Event.cs and .Generated.Event.cs, so they produce
        // nothing at all in a project Glue writes no code for.
        UseProject(p => new Frb2Project(p), "Frb2Game");

        var node = new GlueElementNodeViewModel(null, PooledEntity(), createChildrenNodes: true);

        node.Children.Select(child => child.Text).ShouldNotContain("Events");
    }

    [StaFact]
    public void AnEntityInANormalProject_StillHasAnEventsNode()
    {
        UseProject(p => new ClassLibraryProject(p), "Frb1Game");

        var node = new GlueElementNodeViewModel(null, PooledEntity(), createChildrenNodes: true);

        node.Children.Select(child => child.Text).ShouldContain("Events");
    }

    [StaFact]
    public void AnEntityInAnFrb2Project_KeepsTheNodesThatHoldData()
    {
        // Objects, Variables and States are .gluj data that an FRB2 game reads at runtime, so hiding
        // the code-shaped nodes must not take these with them.
        UseProject(p => new Frb2Project(p), "Frb2Game");

        var node = new GlueElementNodeViewModel(null, PooledEntity(), createChildrenNodes: true);
        var texts = node.Children.Select(child => child.Text).ToList();

        texts.ShouldContain("Files");
        texts.ShouldContain("Objects");
        texts.ShouldContain("Variables");
        texts.ShouldContain("States");
    }
}
