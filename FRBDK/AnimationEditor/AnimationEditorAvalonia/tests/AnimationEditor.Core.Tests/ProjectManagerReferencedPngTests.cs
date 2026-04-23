using AnimationEditor.Core;
using FlatRedBall.IO;
using System.Reflection;
using FilePath = FlatRedBall.IO.FilePath;
using Xunit;

namespace AnimationEditor.Core.Tests;

public class ProjectManagerReferencedPngTests
{
    [Fact]
    public void TryLoadProjectFile_UsesReferencedFilesAndFiltersToPng()
    {
        using var temp = new TempDir();

        string projectFile = Path.Combine(temp.Path, "Game.gluj");
        string contentDir = Path.Combine(temp.Path, "Content");
        Directory.CreateDirectory(contentDir);

        File.WriteAllText(projectFile,
            """
            <Project>
              <Screens>
                <Screen>
                  <ReferencedFiles>
                    <ReferencedFileSave><Name>Sprites/Hero.png</Name></ReferencedFileSave>
                    <ReferencedFileSave><Name>Sprites/Hero.png</Name></ReferencedFileSave>
                    <ReferencedFileSave><Name>Data/Config.json</Name></ReferencedFileSave>
                  </ReferencedFiles>
                </Screen>
              </Screens>
              <Entities>
                <Entity>
                  <ReferencedFiles>
                    <ReferencedFileSave><Name>Enemies/Boss.png</Name></ReferencedFileSave>
                  </ReferencedFiles>
                </Entity>
              </Entities>
              <GlobalFiles>
                <ReferencedFileSave><Name>Ui/Hud.png</Name></ReferencedFileSave>
              </GlobalFiles>
            </Project>
            """);

        var sut = ProjectManager.Self;
        InvokeTryLoadProjectFile(sut, new FilePath(projectFile));

        var fullPaths = sut.ReferencedPngs.Select(p => p.Standardized).ToArray();

        Assert.Equal(3, fullPaths.Length);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "Sprites", "Hero.png")).Standardized, fullPaths);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "Enemies", "Boss.png")).Standardized, fullPaths);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "Ui", "Hud.png")).Standardized, fullPaths);
    }

    [Fact]
    public void TryLoadProjectFile_WhenXmlIsInvalid_FallsBackToAllPngInContent()
    {
        using var temp = new TempDir();

        string projectFile = Path.Combine(temp.Path, "Game.gluj");
        string contentDir = Path.Combine(temp.Path, "Content");
        Directory.CreateDirectory(Path.Combine(contentDir, "Sprites"));

        File.WriteAllText(projectFile, "<Project><Screens>");

        File.WriteAllText(Path.Combine(contentDir, "A.png"), "");
        File.WriteAllText(Path.Combine(contentDir, "B.PNG"), "");
        File.WriteAllText(Path.Combine(contentDir, "Note.txt"), "");
        File.WriteAllText(Path.Combine(contentDir, "Sprites", "C.png"), "");

        var sut = ProjectManager.Self;
        InvokeTryLoadProjectFile(sut, new FilePath(projectFile));

        var fullPaths = sut.ReferencedPngs.Select(p => p.Standardized).ToArray();

        Assert.Equal(3, fullPaths.Length);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "A.png")).Standardized, fullPaths);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "B.PNG")).Standardized, fullPaths);
        Assert.Contains(new FilePath(Path.Combine(contentDir, "Sprites", "C.png")).Standardized, fullPaths);
    }

    [Fact]
    public void TryLoadProjectFile_WhenProjectDoesNotExist_ClearsReferencedPngs()
    {
        var sut = ProjectManager.Self;

        InvokeTryLoadProjectFile(sut, new FilePath(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".gluj")));

        Assert.Empty(sut.ReferencedPngs);
    }

    private static void InvokeTryLoadProjectFile(ProjectManager projectManager, FilePath projectFile)
    {
        var method = typeof(ProjectManager).GetMethod("TryLoadProjectFile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(projectManager, new object[] { projectFile });
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AnimationEditorCoreTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
