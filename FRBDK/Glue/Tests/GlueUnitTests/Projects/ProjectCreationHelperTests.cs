using System;
using System.IO;
using Npc;
using Npc.Managers;
using Shouldly;

namespace GlueUnitTests.Projects;

// Pins the UI-free core of new-project creation (the NPC / New Project Creator): project-name
// validation, the file/namespace rename pass, and GUID replacement. These are the pieces that most
// commonly break when creating a project. See GitHub issue #1892.
public class ProjectCreationHelperTests
{
    #region GetWhyProjectNameIsntValid

    [Theory]
    [InlineData("MyGame")]
    [InlineData("RaceCar")]
    [InlineData("Game2")] // digits are fine as long as they aren't the first character
    public void GetWhyProjectNameIsntValid_ShouldReturnNull_ForValidNames(string projectName)
    {
        ProjectCreationHelper.GetWhyProjectNameIsntValid(projectName).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]                 // blank
    [InlineData("2Fast")]            // starts with a digit
    [InlineData("My Game")]          // contains a space
    [InlineData("My.Game")]          // contains an invalid character
    [InlineData("FlatRedBall")]      // reserved name
    [InlineData("Task")]             // reserved name
    public void GetWhyProjectNameIsntValid_ShouldReturnReason_ForInvalidNames(string projectName)
    {
        ProjectCreationHelper.GetWhyProjectNameIsntValid(projectName).ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region RenameProject

    [Fact]
    public void RenameProject_ShouldRenameFilesAndDirectories_AndRewriteNamespace()
    {
        using var temp = new TempProject();
        temp.WriteFile("GameTemplate.csproj", "<Project><PropertyGroup><RootNamespace>GameTemplate</RootNamespace></PropertyGroup></Project>");
        temp.WriteFile("Game1.cs", "namespace GameTemplate { class Game1 { } }");
        temp.WriteFile("GameTemplateContent/placeholder.txt", "content");

        ProjectCreationHelper.RenameProject("MyGame", "GameTemplate", newNamespace: null, temp.Root);

        File.Exists(Path.Combine(temp.Root, "MyGame.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(temp.Root, "GameTemplate.csproj")).ShouldBeFalse();
        Directory.Exists(Path.Combine(temp.Root, "MyGameContent")).ShouldBeTrue();

        temp.ReadFile("MyGame.csproj").ShouldContain("<RootNamespace>MyGame</RootNamespace>");
        temp.ReadFile("Game1.cs").ShouldContain("namespace MyGame");
    }

    [Fact]
    public void RenameProject_ShouldUseProjectNameForFiles_ButCustomNamespaceForContents()
    {
        using var temp = new TempProject();
        temp.WriteFile("GameTemplate.csproj", "<Project><PropertyGroup><RootNamespace>GameTemplate</RootNamespace></PropertyGroup></Project>");
        temp.WriteFile("Game1.cs", "namespace GameTemplate { class Game1 { } }");

        ProjectCreationHelper.RenameProject("MyGame", "GameTemplate", newNamespace: "Custom.Space", temp.Root);

        // File is named after the project...
        File.Exists(Path.Combine(temp.Root, "MyGame.csproj")).ShouldBeTrue();
        // ...but the namespace inside uses the requested namespace, not the project name.
        temp.ReadFile("MyGame.csproj").ShouldContain("<RootNamespace>Custom.Space</RootNamespace>");
        temp.ReadFile("Game1.cs").ShouldContain("namespace Custom.Space");
    }

    #endregion

    #region GuidLogic.ReplaceGuids

    [Fact]
    public void ReplaceGuids_ShouldReplaceLegacyProjectGuid()
    {
        using var temp = new TempProject();
        const string oldGuid = "ABCDEF00-AAAA-BBBB-CCCC-DDDDEEEEFFFF";
        temp.WriteFile("MyGame.csproj", $"<Project><PropertyGroup><ProjectGuid>{{{oldGuid}}}</ProjectGuid></PropertyGroup></Project>");
        // A legacy (non-SDK) project always ships with a .sln, and ReplaceGuids rewrites the guid in it too.
        temp.WriteFile("MyGame.sln", $"Project(\"{{{oldGuid}}}\") = \"MyGame\"");

        GuidLogic.ReplaceGuids(temp.Root);

        var csproj = temp.ReadFile("MyGame.csproj");
        csproj.ShouldNotContain(oldGuid);
        csproj.ShouldContain("<ProjectGuid>{"); // tag preserved, just a different guid
        temp.ReadFile("MyGame.sln").ShouldNotContain(oldGuid);
    }

    [Fact]
    public void ReplaceGuids_ShouldNoOp_ForSdkStyleProjectWithoutGuid()
    {
        using var temp = new TempProject();
        const string csproj = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup></PropertyGroup></Project>";
        temp.WriteFile("MyGame.csproj", csproj);

        Should.NotThrow(() => GuidLogic.ReplaceGuids(temp.Root));

        temp.ReadFile("MyGame.csproj").ShouldBe(csproj);
    }

    #endregion

    // Creates an isolated temp directory for one test and deletes it on dispose.
    private sealed class TempProject : IDisposable
    {
        public string Root { get; }

        public TempProject()
        {
            Root = Path.Combine(Path.GetTempPath(), "FrbNpcTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void WriteFile(string relativePath, string contents)
        {
            var full = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, contents);
        }

        public string ReadFile(string relativePath) => File.ReadAllText(Path.Combine(Root, relativePath));

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }
}
