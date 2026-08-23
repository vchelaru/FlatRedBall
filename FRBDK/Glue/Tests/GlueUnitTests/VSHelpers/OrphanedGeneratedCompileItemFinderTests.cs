using System.Collections.Generic;
using FlatRedBall.Glue.VSHelpers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.VSHelpers;

/// <summary>
/// GitHub issue #2103: a Screen/Entity deleted or renamed outside Glue's own delete/rename flow can leave
/// its ".Generated.cs" (or similar) still referenced in the .csproj, producing CS2001 on the next build.
/// These pin the pure decision logic - which candidate &lt;Compile Include&gt; entries are safe to treat as
/// orphaned - independent of any real project or Glue bootstrap. Also covers GitHub issue #2170: candidates
/// are scoped to the Screens/Entities/Factories/Forms folders, since the ownership model only understands
/// Screen/Entity ownership plus (as of GitHub issue #2185) whatever GumPlugin reports owning Forms code.
/// </summary>
public class OrphanedGeneratedCompileItemFinderTests
{
    static CandidateCompileItem Item(string include, bool isWildcard = false, bool isConditional = false) =>
        new CandidateCompileItem { UnevaluatedInclude = include, IsWildcard = isWildcard, IsConditional = isConditional };

    static List<string> Find(
        IEnumerable<CandidateCompileItem> items,
        HashSet<string> existingFiles = null,
        HashSet<string> ownedRelativePaths = null)
    {
        existingFiles ??= new HashSet<string>();
        ownedRelativePaths ??= new HashSet<string>();

        return OrphanedGeneratedCompileItemFinder.FindOrphanedIncludes(
            items,
            include => existingFiles.Contains(include),
            ownedRelativePaths);
    }

    #region IsGlueGeneratedFileName

    [Theory]
    [InlineData("MultiPartAGumForms.Generated.cs")]
    [InlineData("Screens\\MultiPartAGumForms.Generated.cs")]
    [InlineData("MyScreen.Generated.Event.cs")]
    [InlineData("Factories\\MyEntityFactory.Generated.cs")]
    [InlineData("myscreen.generated.cs")] // case-insensitive
    public void IsGlueGeneratedFileName_ShouldReturnTrue_ForGlueGeneratedNames(string fileName)
    {
        OrphanedGeneratedCompileItemFinder.IsGlueGeneratedFileName(fileName).ShouldBeTrue();
    }

    [Theory]
    [InlineData("MyScreen.cs")]
    [InlineData("MyScreen.Event.cs")] // hand-authored event file, not ".Generated.Event.cs"
    [InlineData("Program.cs")]
    [InlineData("")]
    [InlineData(null)]
    public void IsGlueGeneratedFileName_ShouldReturnFalse_ForNonGeneratedNames(string fileName)
    {
        OrphanedGeneratedCompileItemFinder.IsGlueGeneratedFileName(fileName).ShouldBeFalse();
    }

    #endregion

    #region Positive - should be removed

    [Fact]
    public void FindOrphanedIncludes_ShouldRemove_OrphanedGeneratedCs()
    {
        var items = new[] { Item(@"Screens\MultiPartAGumForms.Generated.cs") };

        var result = Find(items);

        result.ShouldBe(new[] { @"Screens\MultiPartAGumForms.Generated.cs" });
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldRemove_OrphanedGeneratedEventCs()
    {
        var items = new[] { Item(@"Screens\MyScreen.Generated.Event.cs") };

        var result = Find(items);

        result.ShouldBe(new[] { @"Screens\MyScreen.Generated.Event.cs" });
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldRemove_OrphanedFactoryGeneratedCs()
    {
        var items = new[] { Item(@"Factories\MyEntityFactory.Generated.cs") };

        var result = Find(items);

        result.ShouldBe(new[] { @"Factories\MyEntityFactory.Generated.cs" });
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldRemove_OrphanedFormsGeneratedCs()
    {
        // GitHub issue #2185: Forms\...Forms.Generated.cs is scoped in the same way as Screens/Entities/Factories.
        var items = new[] { Item(@"Forms\Screens\WrapUpScreenGumForms.Generated.cs") };

        var result = Find(items);

        result.ShouldBe(new[] { @"Forms\Screens\WrapUpScreenGumForms.Generated.cs" });
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldRemove_MultipleOrphanedEntriesInOnePass()
    {
        var items = new[]
        {
            Item(@"Screens\First.Generated.cs"),
            Item(@"Screens\Second.Generated.cs"),
            Item(@"Entities\Third.Generated.cs"),
        };

        var result = Find(items);

        result.ShouldBe(new[]
        {
            @"Screens\First.Generated.cs",
            @"Screens\Second.Generated.cs",
            @"Entities\Third.Generated.cs",
        });
    }

    #endregion

    #region Negative - must NOT be removed

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_FileThatStillExistsOnDisk_EvenIfUnowned()
    {
        var include = @"Screens\StillOnDisk.Generated.cs";
        var items = new[] { Item(include) };

        var result = Find(items, existingFiles: new HashSet<string> { include });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_EntryOwnedByALiveElement_EvenIfMissingOnDisk()
    {
        var include = @"Screens\MyScreen.Generated.cs";
        var items = new[] { Item(include) };

        var result = Find(items, ownedRelativePaths: new HashSet<string> { include });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_WildcardIncludes()
    {
        var items = new[] { Item(@"**\*.Generated.cs", isWildcard: true) };

        var result = Find(items);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_ConditionalIncludes()
    {
        var items = new[] { Item(@"Screens\PlatformOnly.Generated.cs", isConditional: true) };

        var result = Find(items);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_NonGlueOwnedMissingSourceFile()
    {
        // A hand-authored .cs (or .Event.cs) that happens to be missing is never a candidate - only
        // Glue's own generated-file naming is. Deleting it isn't reconciliation's job; the explicit
        // delete flow (DeletionPlanner) is, since it asks the user first.
        var items = new[] { Item(@"Screens\MyScreen.cs"), Item(@"Screens\MyScreen.Event.cs") };

        var result = Find(items);

        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(@"Performance\PoolList.Generated.cs")]
    [InlineData(@"Performance\IEntityFactory.Generated.cs")]
    [InlineData("Performance/PoolList.Generated.cs")]
    public void FindOrphanedIncludes_ShouldKeep_PerformanceGeneratedFiles_EvenIfMissingOnDisk(string include)
    {
        // GitHub issue #2170: these are written once by
        // FactoryElementCodeGenerator.AddGeneratedPerformanceTypes, not owned by any Screen/Entity, so the
        // ownership model always sees them as unowned - they must survive regardless of file existence.
        var items = new[] { Item(include) };

        var result = Find(items);

        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(@"SomeOtherFolder\Whatever.Generated.cs")]
    [InlineData("RootLevel.Generated.cs")]
    public void FindOrphanedIncludes_ShouldKeep_AnyGeneratedFileOutsideScreensEntitiesFactories_EvenIfMissingOnDisk(string include)
    {
        // GitHub issue #2170: reconciliation is scoped to only the Screens/Entities/Factories folders a
        // Screen/Entity's generated files can ever live under - not a list of specific known filenames - so
        // it can't misfire on some future generator's own ".Generated.cs" written anywhere else, the same way
        // it did for Performance/PoolList.Generated.cs.
        var items = new[] { Item(include) };

        var result = Find(items);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_FormsGeneratedCsOwnedByALiveGumElement_EvenIfMissingOnDisk()
    {
        // GitHub issue #2185: a fresh checkout where Forms\...Forms.Generated.cs is gitignored and hasn't
        // been regenerated yet, but the owning Gum element (reported by GumPlugin, not any GlueElement) is
        // still live - the entry must survive, same tolerance as Screens/Entities.
        var include = @"Forms\Components\EndgameDemoForms.Generated.cs";
        var items = new[] { Item(include) };

        var result = Find(items, ownedRelativePaths: new HashSet<string> { include });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void FindOrphanedIncludes_ShouldKeep_WhenOwnershipMatchIgnoresNothingButExactPath()
    {
        // A different element's generated file with a similar name must not be treated as owning this one.
        var items = new[] { Item(@"Screens\MyScreenExtra.Generated.cs") };

        var result = Find(items, ownedRelativePaths: new HashSet<string> { @"Screens\MyScreen.Generated.cs" });

        result.ShouldBe(new[] { @"Screens\MyScreenExtra.Generated.cs" });
    }

    #endregion
}
