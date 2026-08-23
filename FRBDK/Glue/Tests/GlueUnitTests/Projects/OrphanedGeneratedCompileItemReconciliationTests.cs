using System.Collections.Generic;
using System.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Projects;

/// <summary>
/// GitHub issue #2103: a Screen/Entity deleted or renamed outside Glue's own delete/rename flow can leave
/// its ".Generated.cs" still referenced in the .csproj, producing CS2001 on the next build. These pin
/// <see cref="VisualStudioProject.RemoveOrphanedGeneratedCompileItems"/> - the load-time reconciliation
/// pass - against a real, MSBuild-backed project (via <see cref="TestVisualStudioProjectFactory"/>), no
/// full Glue bootstrap needed since the method only takes an explicit element list and touches the project
/// it's called on.
/// </summary>
public class OrphanedGeneratedCompileItemReconciliationTests
{
    static void WriteFile(string directory, string relativePath)
    {
        var fullPath = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "// test file");
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldRemove_OrphanedGeneratedCsWithNoOwner()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Screens\MultiPartAGumForms.Generated.cs");

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBe(new[] { @"Screens\MultiPartAGumForms.Generated.cs" });
            project.IsFilePartOfProject(@"Screens\MultiPartAGumForms.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_EntryWhoseFileExistsOnDisk()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Screens\StillOnDisk.Generated.cs");
            WriteFile(directory, @"Screens\StillOnDisk.Generated.cs");

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBeEmpty();
            project.IsFilePartOfProject(@"Screens\StillOnDisk.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_EntryOwnedByALiveElement_EvenThoughFileIsMissing()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            // Simulates a fresh checkout: the .Generated.cs is gitignored and hasn't been regenerated yet,
            // but MyScreen is still a live element - the entry must survive.
            project.AddCodeBuildItem(@"Screens\MyScreen.Generated.cs");
            var liveScreen = new ScreenSave { Name = @"Screens\MyScreen" };

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement> { liveScreen });

            removed.ShouldBeEmpty();
            project.IsFilePartOfProject(@"Screens\MyScreen.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_WildcardCompileIncludes()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.Project.AddItem("Compile", @"Screens\**\*.Generated.cs");
            project.Project.ReevaluateIfNecessary();

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_ConditionalCompileIncludes()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            var item = project.AddCodeBuildItem(@"Screens\PlatformOnly.Generated.cs");
            item.Xml.Condition = "'$(OS)' == 'Windows_NT'";
            project.Project.ReevaluateIfNecessary();

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_HandAuthoredMissingSourceFile()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            // "MyScreen.cs" is hand-authored, not Glue-generated - even orphaned, reconciliation must not
            // touch it. That's the explicit delete flow's job (it asks the user first).
            project.AddCodeBuildItem(@"Screens\MyScreen.cs");

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBeEmpty();
            project.IsFilePartOfProject(@"Screens\MyScreen.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldKeep_KnownNonElementOwnedPerformanceFiles_EvenThoughFileIsMissing()
    {
        // GitHub issue #2170: Performance/PoolList.Generated.cs and Performance/IEntityFactory.Generated.cs
        // are written once by FactoryElementCodeGenerator.AddGeneratedPerformanceTypes, never owned by a
        // Screen/Entity, so they always look orphaned to this check whenever the backing file happens to be
        // missing - they must survive regardless.
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Performance\PoolList.Generated.cs");
            project.AddCodeBuildItem(@"Performance\IEntityFactory.Generated.cs");

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement>());

            removed.ShouldBeEmpty();
            project.IsFilePartOfProject(@"Performance\PoolList.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue();
            project.IsFilePartOfProject(@"Performance\IEntityFactory.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline)
                .ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveOrphanedGeneratedCompileItems_ShouldRemove_MultipleOrphanedEntries_AndKeepTheRest()
    {
        var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out var directory);
        try
        {
            project.AddCodeBuildItem(@"Screens\OrphanedOne.Generated.cs");
            project.AddCodeBuildItem(@"Entities\OrphanedTwo.Generated.cs");
            project.AddCodeBuildItem(@"Screens\LiveScreen.Generated.cs");
            WriteFile(directory, @"Screens\StillOnDisk.Generated.cs");
            project.AddCodeBuildItem(@"Screens\StillOnDisk.Generated.cs");

            var liveScreen = new ScreenSave { Name = @"Screens\LiveScreen" };

            var removed = project.RemoveOrphanedGeneratedCompileItems(new List<GlueElement> { liveScreen });

            removed.ShouldBe(new[] { @"Screens\OrphanedOne.Generated.cs", @"Entities\OrphanedTwo.Generated.cs" }, ignoreOrder: true);
            project.IsFilePartOfProject(@"Screens\LiveScreen.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline).ShouldBeTrue();
            project.IsFilePartOfProject(@"Screens\StillOnDisk.Generated.cs", BuildItemMembershipType.CompileOrContentPipeline).ShouldBeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
