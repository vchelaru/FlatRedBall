using System;
using System.IO;
using System.Linq;
using FlatRedBall.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.MonoGameContent;
using Shouldly;

namespace GlueUnitTests.ContentPipelinePlugin;

/// <summary>
/// GitHub issue #1757 - the XNB references Glue writes into a MonoGame project came out lower-cased,
/// causing diff churn (and broken content loads on case-sensitive platforms). The Include kept its case
/// because VisualStudioProject's item dictionaries are OrdinalIgnoreCase, so an existing item was found
/// and never rewritten - but the Link was re-applied on every pass, so it flipped to whatever case the
/// path had that run. The path's case came from FileManager.MakeRelative, which lower-cases whenever the
/// process-wide FileManager.PreserveCase flag is false.
/// </summary>
public class XnbReferenceCaseTests : IDisposable
{
    readonly bool wasPreservingCase = FileManager.PreserveCase;
    string directory;

    public XnbReferenceCaseTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    public void Dispose()
    {
        FileManager.PreserveCase = wasPreservingCase;

        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AddBuiltXnbReferences_ShouldWriteCasePreservingLink_WhenPreserveCaseIsFalse()
    {
        FileManager.PreserveCase = false;

        var savedProject = AddSingleXnbReferenceAndSave();

        savedProject.ShouldContain(@"<Link>Content\Gum\FontCache\Font16Titillium_Web_Italic_0.xnb</Link>", Case.Sensitive);
    }

    [Fact]
    public void AddBuiltXnbReferences_ShouldWriteCasePreservingLink_WhenPreserveCaseIsTrue()
    {
        FileManager.PreserveCase = true;

        var savedProject = AddSingleXnbReferenceAndSave();

        savedProject.ShouldContain(@"<Link>Content\Gum\FontCache\Font16Titillium_Web_Italic_0.xnb</Link>", Case.Sensitive);
    }

    [Fact]
    public void AddBuiltXnbReferences_ShouldWriteCasePreservingInclude_WhenPreserveCaseIsFalse()
    {
        FileManager.PreserveCase = false;

        var savedProject = AddSingleXnbReferenceAndSave();

        savedProject.ShouldContain(@"BuiltXnbs\DesktopGL\Content\Gum\FontCache\Font16Titillium_Web_Italic_0.xnb", Case.Sensitive);
    }

    string AddSingleXnbReferenceAndSave()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);

        var contentDirectory = new FilePath(Path.Combine(directory, "Content"));
        var sourceFile = new FilePath(Path.Combine(directory,
            @"Content\Gum\FontCache\Font16Titillium_Web_Italic_0.png"));
        // Matches what GetXnbDestinationDirectory hands the production caller: a FilePath-derived
        // absolute directory, so its own case never depends on FileManager.PreserveCase.
        var destinationDirectory = new FilePath(
            Path.Combine(directory, @"BuiltXnbs\DesktopGL\Content\Gum\FontCache")).FullPath + "/";

        BuildLogic.Self.AddBuiltXnbReferences(project, sourceFile, contentDirectory, destinationDirectory,
            new[] { "xnb" }, saveProjectAfterAdd: false);

        var savedTo = project.FullFileName.FullPath;
        project.Save(savedTo);

        return File.ReadAllText(savedTo);
    }
}
