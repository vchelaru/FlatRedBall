using System;
using System.IO;
using GlueUnitTests.TestSupport;
using OfficialPlugins.MonoGameContent;
using Shouldly;

namespace GlueUnitTests.ContentPipelinePlugin;

public class BuildLogicMonoGameVersionTests
{
    [Fact]
    public void GetMonoGameFrameworkMgcbMismatchError_ShouldReturnError_WhenMgcbIsNewerThanReferencedVersion()
    {
        var error = BuildLogic.GetMonoGameFrameworkMgcbMismatchError(
            referencedMonoGameVersion: new Version("3.8.4.1"),
            mgcbVersion: new Version("3.8.5.1"));

        error.ShouldNotBeNull();
        error.ShouldContain("3.8.4.1");
        error.ShouldContain("3.8.5.1");
    }

    [Fact]
    public void GetMonoGameFrameworkMgcbMismatchError_ShouldReturnNull_WhenVersionsMatch()
    {
        var error = BuildLogic.GetMonoGameFrameworkMgcbMismatchError(
            referencedMonoGameVersion: new Version("3.8.1.303"),
            mgcbVersion: new Version("3.8.1.303"));

        error.ShouldBeNull();
    }

    [Fact]
    public void GetMonoGameFrameworkMgcbMismatchError_ShouldReturnNull_WhenMgcbIsOlderThanReferencedVersion()
    {
        var error = BuildLogic.GetMonoGameFrameworkMgcbMismatchError(
            referencedMonoGameVersion: new Version("3.8.5.1"),
            mgcbVersion: new Version("3.8.4.1"));

        error.ShouldBeNull();
    }

    [Fact]
    public void GetMonoGameFrameworkMgcbMismatchError_ShouldReturnNull_WhenEitherVersionIsUnknown()
    {
        BuildLogic.GetMonoGameFrameworkMgcbMismatchError(null, new Version("3.8.5.1")).ShouldBeNull();
        BuildLogic.GetMonoGameFrameworkMgcbMismatchError(new Version("3.8.4.1"), null).ShouldBeNull();
    }

    [Fact]
    public void GetReferencedMonoGameFrameworkVersion_ShouldReturnParsedVersion_WhenProjectReferencesMonoGameFramework()
    {
        string directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory,
                extraItemGroupXml: "<PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.4.1\" />");

            var version = BuildLogic.GetReferencedMonoGameFrameworkVersion(project);

            version.ShouldBe(new Version("3.8.4.1"));
        }
        finally
        {
            if (directory != null)
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void GetReferencedMonoGameFrameworkVersion_ShouldReturnNull_WhenProjectDoesNotReferenceMonoGameFramework()
    {
        string directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);

            var version = BuildLogic.GetReferencedMonoGameFrameworkVersion(project);

            version.ShouldBeNull();
        }
        finally
        {
            if (directory != null)
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
