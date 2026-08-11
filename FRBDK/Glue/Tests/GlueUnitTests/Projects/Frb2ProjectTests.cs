using System;
using System.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using Microsoft.Build.Evaluation;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// Glue opening and editing a FlatRedBall 2 game project: it is recognized as FRB2 from its .csproj,
/// and from then on Glue writes only the .gluj/.glsj/.glej JSON - no generated code, and no .csproj
/// writes. GitHub issue #2021.
/// </summary>
public class Frb2ProjectDetectorTests
{
    static Frb2ProjectDetector.ProjectItemInfo Item(string itemType, string include) => new(itemType, include);

    [Fact]
    public void IsFrb2Project_IsTrue_ForSourceLinkedFrb2ProjectReference()
    {
        // The shape every FRB2 game has today, straight out of GlueLoaderScratch.csproj.
        Assert.True(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("PackageReference", "MonoGame.Framework.DesktopGL"),
            Item("ProjectReference", @"..\..\src\FlatRedBall2.csproj"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsTrue_ForForwardSlashInclude()
    {
        // A .csproj authored on macOS/Linux uses forward slashes, and Path.GetFileName on Windows
        // would not split on those.
        Assert.True(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("ProjectReference", "../../src/FlatRedBall2.csproj"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsTrue_ForTheDotnetNewTemplatesPackageReference()
    {
        // What `dotnet new frb2-desktop` actually produces in MyGame.Common: the engine comes from
        // nuget, not from a ProjectReference into the FRB2 repo. Versions live in
        // Directory.Packages.props, so the Include carries no version.
        Assert.True(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("PackageReference", "FlatRedBall2.MonoGame"),
            Item("PackageReference", "MonoGame.Framework.DesktopGL"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsTrue_ForTheKniPackageReference()
    {
        // The multiplatform template's Common project takes both backends.
        Assert.True(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("PackageReference", "FlatRedBall2.Kni"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForAnFrb1PackageReference()
    {
        // The dangerous false positive: mistaking an FRB1 project for FRB2 silently stops Glue
        // generating its code. FRB1's packages are FlatRedBall.*, which must not match FlatRedBall2.*.
        Assert.False(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("PackageReference", "FlatRedBall.Forms"),
            Item("PackageReference", "FlatRedBall"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForTheDesktopLauncherProject()
    {
        // MyGame.Desktop only holds Program.cs and the content pipeline - it reaches the engine through
        // MyGame.Common, so it is not itself the project Glue edits.
        Assert.False(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("PackageReference", "MonoGame.Framework.DesktopGL"),
            Item("PackageReference", "MonoGame.Content.Builder.Task"),
            Item("ProjectReference", @"..\MyGame.Common\MyGame.Common.csproj"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForFrb1SourceLinkedProject()
    {
        // "Link Game to FRB Source" on an FRB1 project produces this. It must not be mistaken for FRB2,
        // or Glue would silently stop generating that project's code.
        Assert.False(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("ProjectReference", @"..\..\..\FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6.csproj"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForUnrelatedProjectReference()
    {
        Assert.False(Frb2ProjectDetector.IsFrb2Project(new[]
        {
            Item("ProjectReference", @"..\SomeOtherLibrary.csproj"),
        }));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForNoItems()
    {
        Assert.False(Frb2ProjectDetector.IsFrb2Project(Array.Empty<Frb2ProjectDetector.ProjectItemInfo>()));
    }

    [Fact]
    public void IsFrb2Project_IsFalse_ForNullItems()
    {
        Assert.False(Frb2ProjectDetector.IsFrb2Project((System.Collections.Generic.IEnumerable<Frb2ProjectDetector.ProjectItemInfo>)null));
    }
}

public class Frb2ProjectTests : IDisposable
{
    readonly string _directory;

    public Frb2ProjectTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_Frb2_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }

    /// <summary>
    /// A bare, non-SDK-style .csproj, for the same reason
    /// <see cref="TestVisualStudioProjectFactory"/> uses one: it evaluates with no SDK resolution at
    /// all, so these tests do not need the .NET SDK an FRB2 project actually targets.
    /// </summary>
    string WriteCsproj(string itemGroupXml, string projectName = "TestFrb2Game")
    {
        var csprojPath = Path.Combine(_directory, projectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
{itemGroupXml}
  </ItemGroup>
</Project>");

        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        return csprojPath;
    }

    const string Frb2ProjectReferenceXml = @"    <ProjectReference Include=""..\..\src\FlatRedBall2.csproj"" />";

    Project LoadCoreProject(string csprojPath) => new(csprojPath, null, null, new ProjectCollection());

    [Fact]
    public void Frb2Project_IsNotMaintainedByGlue()
    {
        var project = new Frb2Project(LoadCoreProject(WriteCsproj(Frb2ProjectReferenceXml)));

        Assert.False(project.IsMaintainedByGlue);
    }

    [Fact]
    public void OtherProjectTypes_AreStillMaintainedByGlue()
    {
        var project = new ClassLibraryProject(LoadCoreProject(WriteCsproj("")));

        Assert.True(project.IsMaintainedByGlue);
    }

    [Fact]
    public void Frb2Project_ContentDirectory_IsInsideTheEditorFolder()
    {
        // Content files are Glue's too, so they belong in the one folder that holds everything the
        // editor authored - dropping a PNG on a screen must land it in Content/FrbEditor/Screens/...,
        // not Content/Screens/..., or deleting Content/FrbEditor leaves the referenced art behind.
        // It is also the .gluj's own folder, which is what FRB2 resolves referenced files against.
        var project = new Frb2Project(LoadCoreProject(WriteCsproj(Frb2ProjectReferenceXml)));

        Assert.Equal("Content/FrbEditor/", project.ContentDirectory);
    }

    [Fact]
    public void Save_DoesNotWriteTheCsprojToDisk()
    {
        var csprojPath = WriteCsproj(Frb2ProjectReferenceXml);
        var originalText = File.ReadAllText(csprojPath);

        var project = new Frb2Project(LoadCoreProject(csprojPath));
        project.Project.AddItem("Compile", "Screens\\Level1.Generated.cs");

        project.Save(csprojPath);

        Assert.Equal(originalText, File.ReadAllText(csprojPath));
    }

    [Fact]
    public void Save_StillWritesTheCsproj_ForANormalProject()
    {
        // The guard is specific to FRB2 - every other project type must keep saving.
        var csprojPath = WriteCsproj("");
        var originalText = File.ReadAllText(csprojPath);

        var project = new ClassLibraryProject(LoadCoreProject(csprojPath));
        project.Project.AddItem("Compile", "Screens\\Level1.Generated.cs");

        project.Save(csprojPath);

        Assert.NotEqual(originalText, File.ReadAllText(csprojPath));
    }

    [Fact]
    public async System.Threading.Tasks.Task ContentPipeline_SkipsAnFrb2Project_RatherThanThrowing()
    {
        // Dropping a PNG onto an FRB2 project used to take down the plugin: BuildLogic decided the
        // project "supports the content pipeline" from a deny-list (anything but FNA) but looked up the
        // build platform from an allow-list of four project types, so a type in neither was declared
        // supported and then threw for having no platform. FRB2 does not use the content pipeline at
        // all - its content is copied, not built.
        var project = new Frb2Project(LoadCoreProject(WriteCsproj(Frb2ProjectReferenceXml)));
        var png = new ReferencedFileSave { Name = "Content/image.png" };

        var builtFiles = await OfficialPlugins.MonoGameContent.BuildLogic.Self
            .UpdateFileMembershipAndBuildReferencedFile(project, png, forcePngsToContentPipeline: false);

        Assert.Empty(builtFiles);
    }

    [Fact]
    public void TryRemoveXnbReferences_OnAnFrb2Project_DoesNothingRatherThanThrowing()
    {
        // Deleting a file from a screen took the plugin down here. MainContentPipelinePlugin's
        // HandleFileRemoved calls this directly, so guarding
        // UpdateFileMembershipAndBuildReferencedFile - the caller that the earlier PNG-drop crash went
        // through - left this entry point live.
        var project = new Frb2Project(LoadCoreProject(WriteCsproj(Frb2ProjectReferenceXml)));

        OfficialPlugins.MonoGameContent.BuildLogic.TryRemoveXnbReferences(
            project, Path.Combine(_directory, "Content", "Screens", "NewScreen", "Bear.png"), save: false);
    }

    [Fact]
    public void CreatePlatformSpecificProject_ReturnsFrb2Project_ForAnFrb2Csproj()
    {
        // Before this, an FRB2 .csproj fell through ProjectCreator's DefineConstants cascade (it sets
        // none of DESKTOP_GL/ANDROID/IOS/FNA/BLAZORGL) and Glue could not open the project at all.
        var csprojPath = WriteCsproj(Frb2ProjectReferenceXml);

        var project = WithNoProjectTypeDialog(() =>
            ProjectCreator.CreatePlatformSpecificProject(LoadCoreProject(csprojPath), csprojPath));

        Assert.IsType<Frb2Project>(project);
    }

    [Fact]
    public void CreatePlatformSpecificProject_DoesNotAskTheUserToPickAProjectType_ForAnFrb2Csproj()
    {
        var csprojPath = WriteCsproj(Frb2ProjectReferenceXml);
        var wasAsked = false;

        WithNoProjectTypeDialog(
            () => ProjectCreator.CreatePlatformSpecificProject(LoadCoreProject(csprojPath), csprojPath),
            onAsked: () => wasAsked = true);

        Assert.False(wasAsked);
    }

    /// <summary>
    /// ProjectCreator pops a real modal on the developer's desktop when it cannot determine a project
    /// type, so both the red and green runs of the tests above have to stub that out.
    /// </summary>
    static T WithNoProjectTypeDialog<T>(Func<T> action, Action onAsked = null)
    {
        var previousChoice = FlatRedBall.Glue.Controls.DialogService.ShowChoiceImpl;
        var previousMessage = FlatRedBall.Glue.Controls.DialogService.ShowMessageImpl;
        try
        {
            FlatRedBall.Glue.Controls.DialogService.ShowChoiceImpl = (_, _) =>
            {
                onAsked?.Invoke();
                return null;
            };
            FlatRedBall.Glue.Controls.DialogService.ShowMessageImpl = _ => onAsked?.Invoke();
            return action();
        }
        finally
        {
            FlatRedBall.Glue.Controls.DialogService.ShowChoiceImpl = previousChoice;
            FlatRedBall.Glue.Controls.DialogService.ShowMessageImpl = previousMessage;
        }
    }
}

public class Frb2CodeGenerationSuppressionTests : IDisposable
{
    readonly string _directory;
    readonly ProjectBase _previousMainProject;

    public Frb2CodeGenerationSuppressionTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _previousMainProject = GlueState.Self.CurrentMainProject;
        _directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_Frb2Codegen_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        // Process-wide state - every test in this assembly runs non-parallel, but leaking this would
        // still change the setup of whatever runs next.
        GlueState.Self.CurrentMainProject = _previousMainProject as VisualStudioProject;
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    T CreateProject<T>(Func<Project, T> construct, string projectName) where T : VisualStudioProject
    {
        var csprojPath = Path.Combine(_directory, projectName + ".csproj");
        File.WriteAllText(csprojPath, $@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>
</Project>");
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        return construct(new Project(csprojPath, null, null, new ProjectCollection()));
    }

    [Fact]
    public void GenerateCodeCommands_IsSuppressed_WhenTheProjectIsFrb2()
    {
        GlueState.Self.CurrentMainProject = CreateProject(p => new Frb2Project(p), "Frb2Game");

        Assert.IsType<NoCodeGenerationCommands>(GlueCommands.Self.GenerateCodeCommands);
    }

    [Fact]
    public void GenerateCodeCommands_IsTheRealOne_ForANormalProject()
    {
        GlueState.Self.CurrentMainProject = CreateProject(p => new ClassLibraryProject(p), "Frb1Game");

        Assert.IsNotType<NoCodeGenerationCommands>(GlueCommands.Self.GenerateCodeCommands);
    }

    [Fact]
    public void GenerateCodeCommands_IsTheRealOne_WhenNoProjectIsLoaded()
    {
        GlueState.Self.CurrentMainProject = null;

        Assert.IsNotType<NoCodeGenerationCommands>(GlueCommands.Self.GenerateCodeCommands);
    }

    [Fact]
    public void EveryGenerationCall_DoesNothing_WhenTheProjectIsFrb2()
    {
        // No GlueProjectSave is assigned on purpose: the real implementation dereferences
        // GlueState.CurrentGlueProject in all of these, so "does not throw" is a real assertion that
        // none of them ran, not just that they were quiet.
        GlueState.Self.CurrentMainProject = CreateProject(p => new Frb2Project(p), "Frb2Game");
        var commands = GlueCommands.Self.GenerateCodeCommands;

        commands.GenerateAllCode();
        commands.GenerateCurrentElementCode();
        commands.GenerateElementCode(new ScreenSave { Name = "Screens\\Level1" });
        commands.GenerateElementCustomCode(new ScreenSave { Name = "Screens\\Level1" });
        commands.GenerateGlobalContentCode();
        commands.GenerateGlobalContentCodeTask();
        commands.GenerateElementAndReferencedObjectCode(new ScreenSave { Name = "Screens\\Level1" });
        commands.GenerateCurrentCsvCode();
        commands.GenerateCustomClassesCode();
        commands.GenerateStartupScreenCode();
        commands.GenerateGame1();

        Assert.Empty(Directory.GetFiles(_directory, "*.cs", SearchOption.AllDirectories));
    }

    [Fact]
    public void SaveIfDiffers_DoesNotWriteCodeFiles_WhenTheProjectIsFrb2()
    {
        // Not everything that generates code goes through IGenerateCodeCommands - CodeBuildItemAdder's
        // embedded resource files and CameraSetupCodeGenerator both write straight through here on
        // glux load.
        GlueState.Self.CurrentMainProject = CreateProject(p => new Frb2Project(p), "Frb2Game");
        var codeFile = Path.Combine(_directory, "Setup", "CameraSetup.Generated.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(codeFile));

        var didWrite = GlueCommands.Self.FileCommands.SaveIfDiffers(codeFile, "// generated");

        Assert.False(didWrite);
        Assert.False(File.Exists(codeFile));
    }

    [Fact]
    public void SaveIfDiffers_StillWritesCodeFiles_ForANormalProject()
    {
        GlueState.Self.CurrentMainProject = CreateProject(p => new ClassLibraryProject(p), "Frb1Game");
        var codeFile = Path.Combine(_directory, "Setup", "CameraSetup.Generated.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(codeFile));

        var didWrite = GlueCommands.Self.FileCommands.SaveIfDiffers(codeFile, "// generated");

        Assert.True(didWrite);
        Assert.True(File.Exists(codeFile));
    }

    [Fact]
    public void SaveIfDiffers_StillWritesNonCodeFiles_WhenTheProjectIsFrb2()
    {
        // The JSON and content files are exactly what Glue is still there to author.
        GlueState.Self.CurrentMainProject = CreateProject(p => new Frb2Project(p), "Frb2Game");
        var jsonFile = Path.Combine(_directory, "Screens", "Level1.glsj");
        Directory.CreateDirectory(Path.GetDirectoryName(jsonFile));

        var didWrite = GlueCommands.Self.FileCommands.SaveIfDiffers(jsonFile, "{}");

        Assert.True(didWrite);
        Assert.True(File.Exists(jsonFile));
    }

    [Fact]
    public void QueryMethods_StillAnswer_WhenTheProjectIsFrb2()
    {
        // GetNamespaceForElement / ReplaceGlueVersionString are pure lookups that non-codegen callers
        // use too, so the suppressed implementation delegates them rather than returning null.
        GlueState.Self.CurrentMainProject = CreateProject(p => new Frb2Project(p), "Frb2Game");
        var commands = GlueCommands.Self.GenerateCodeCommands;

        Assert.Equal("Frb2Game.Screens", commands.GetNamespaceForElementName("Screens\\Level1"));
        Assert.Equal("no versions here", commands.ReplaceGlueVersionString("no versions here"));
    }

    [Fact]
    public void GenerateCodeCommands_IsTheRealOne_WhenFrb2ProjectOptsIntoCodeGeneration()
    {
        var frb2 = CreateProject(p => new Frb2Project(p), "Frb2Game");
        frb2.IsMaintainedByGlue = true;
        GlueState.Self.CurrentMainProject = frb2;

        Assert.IsNotType<NoCodeGenerationCommands>(GlueCommands.Self.GenerateCodeCommands);
    }

    [Fact]
    public void SaveIfDiffers_WritesCodeFiles_WhenFrb2ProjectOptsIntoCodeGeneration()
    {
        var frb2 = CreateProject(p => new Frb2Project(p), "Frb2Game");
        frb2.IsMaintainedByGlue = true;
        GlueState.Self.CurrentMainProject = frb2;
        var codeFile = Path.Combine(_directory, "Setup", "CameraSetup.Generated.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(codeFile));

        var didWrite = GlueCommands.Self.FileCommands.SaveIfDiffers(codeFile, "// generated");

        Assert.True(didWrite);
        Assert.True(File.Exists(codeFile));
    }
}

/// <summary>
/// <see cref="Frb2CodeGenerationSync"/> applies GlueProjectSave.GenerateCode to
/// ProjectBase.IsMaintainedByGlue - the one place project load turns an FRB2 project's opt-in setting
/// into the flag GenerateCodeCommands/CodeWritePolicy actually key off.
/// </summary>
public class Frb2CodeGenerationSyncTests : IDisposable
{
    readonly string _directory;

    public Frb2CodeGenerationSyncTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _directory = Path.Combine(Path.GetTempPath(), "GlueUnitTests_Frb2Sync_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    Frb2Project CreateFrb2Project()
    {
        var csprojPath = Path.Combine(_directory, "Frb2Game.csproj");
        File.WriteAllText(csprojPath, @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>Frb2Game</RootNamespace>
  </PropertyGroup>
</Project>");
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        return new Frb2Project(new Project(csprojPath, null, null, new ProjectCollection()));
    }

    [Fact]
    public void ApplyGenerateCodeSetting_TurnsOnCodeGeneration_WhenTheProjectOptsIn()
    {
        var project = CreateFrb2Project();
        var glueProjectSave = new GlueProjectSave { GenerateCode = true };

        Frb2CodeGenerationSync.ApplyGenerateCodeSetting(project, glueProjectSave);

        Assert.True(project.IsMaintainedByGlue);
    }

    [Fact]
    public void ApplyGenerateCodeSetting_LeavesCodeGenerationOff_WhenTheProjectDoesNotOptIn()
    {
        var project = CreateFrb2Project();
        var glueProjectSave = new GlueProjectSave { GenerateCode = false };

        Frb2CodeGenerationSync.ApplyGenerateCodeSetting(project, glueProjectSave);

        Assert.False(project.IsMaintainedByGlue);
    }

    [Fact]
    public void ApplyGenerateCodeSetting_LeavesCodeGenerationOff_WhenNoGlueProjectSaveIsLoadedYet()
    {
        // The pre-existing-project path in ProjectLoader can reach this before a GlueProjectSave is
        // assigned - must not throw, and must not turn generation on with nothing to read a setting from.
        var project = CreateFrb2Project();

        Frb2CodeGenerationSync.ApplyGenerateCodeSetting(project, null);

        Assert.False(project.IsMaintainedByGlue);
    }

    [Fact]
    public void ApplyGenerateCodeSetting_DoesNothing_ForANonFrb2Project()
    {
        // FRB1 projects are mandatory-codegen and never read this setting, so opting a non-FRB2
        // project's GlueProjectSave into GenerateCode must never flip its IsMaintainedByGlue.
        var csprojPath = Path.Combine(_directory, "Frb1Game.csproj");
        File.WriteAllText(csprojPath, @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <RootNamespace>Frb1Game</RootNamespace>
  </PropertyGroup>
</Project>");
        GlueTestBootstrap.EnsureMsBuildEnvironmentVariable();
        var project = new ClassLibraryProject(new Project(csprojPath, null, null, new ProjectCollection()));
        var glueProjectSave = new GlueProjectSave { GenerateCode = true };

        Frb2CodeGenerationSync.ApplyGenerateCodeSetting(project, glueProjectSave);

        Assert.True(project.IsMaintainedByGlue);
    }
}
