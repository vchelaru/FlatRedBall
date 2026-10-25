using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using OfficialPlugins.VariableDisplay;
using Shouldly;

namespace GlueUnitTests.PropertyGrid;

// GitHub issue #2344: the WPF "Settings (Preview)" grid showed CSV settings (and other members that don't
// apply) on every file, e.g. an .achx. It builds its MembersToIgnore from ReferencedFileSaveSettingsLogic.
public class ReferencedFileSaveSettingsLogicTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public ReferencedFileSaveSettingsLogicTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private static ReferencedFileSave AddToEntity(string name)
    {
        var entity = new EntitySave { Name = "Entities/ObjectBall/ObjectBall" };
        var rfs = new ReferencedFileSave { Name = name };
        entity.ReferencedFiles.Add(rfs);
        ObjectFinder.Self.GlueProject.Entities.Add(entity);
        return rfs;
    }

    private static ReferencedFileSave AddToGlobalContent(string name)
    {
        var rfs = new ReferencedFileSave { Name = name };
        ObjectFinder.Self.GlueProject.GlobalFiles.Add(rfs);
        return rfs;
    }

    [Fact]
    public void Achx_IgnoresCsvMembers()
    {
        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(AddToEntity("Entities/ObjectBall/BallColor.achx"));

        ignored.ShouldContain(nameof(ReferencedFileSave.CreatesDictionary));
        ignored.ShouldContain(nameof(ReferencedFileSave.CsvDelimiter));
        ignored.ShouldContain(nameof(ReferencedFileSave.TreatAsCsv));
        ignored.ShouldContain(nameof(ReferencedFileSave.UniformRowType));
        ignored.ShouldContain(nameof(ReferencedFileSave.IsDatabaseForLocalizing));
        ignored.ShouldContain(nameof(ReferencedFileSave.IsCsvOrTreatedAsCsv));
    }

    [Fact]
    public void Achx_IgnoresEntityOnlyAndBuildMembers()
    {
        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(AddToEntity("Entities/ObjectBall/BallColor.achx"));

        ignored.ShouldContain(nameof(ReferencedFileSave.IsManuallyUpdated));
        ignored.ShouldContain(nameof(ReferencedFileSave.DestroyOnUnload));
        ignored.ShouldContain(nameof(ReferencedFileSave.BuildTool));
        ignored.ShouldContain(nameof(ReferencedFileSave.AdditionalArguments));
        ignored.ShouldContain(nameof(ReferencedFileSave.ConditionalCompilationSymbols));
    }

    [Fact]
    public void Csv_ShowsCsvMembers()
    {
        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(AddToGlobalContent("GlobalContent/Data.csv"));

        ignored.ShouldNotContain(nameof(ReferencedFileSave.CreatesDictionary));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.CsvDelimiter));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.UniformRowType));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.IsDatabaseForLocalizing));
    }

    [Fact]
    public void Txt_TreatedAsCsv_ShowsCsvMembers()
    {
        var rfs = AddToGlobalContent("GlobalContent/Data.txt");
        ReferencedFileSaveSettingsLogic.GetMembersToIgnore(rfs).ShouldContain(nameof(ReferencedFileSave.CsvDelimiter));

        rfs.TreatAsCsv = true;

        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(rfs);
        ignored.ShouldNotContain(nameof(ReferencedFileSave.CsvDelimiter));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.CreatesDictionary));
    }

    [Fact]
    public void KeepsWpfOnlyExtras()
    {
        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(AddToGlobalContent("GlobalContent/Data.csv"));

        ignored.ShouldContain(nameof(ReferencedFileSave.Properties));
        ignored.ShouldContain(nameof(ReferencedFileSave.FilePath));
        ignored.ShouldContain(nameof(ReferencedFileSave.CachedInstanceName));
        ignored.ShouldContain(nameof(ReferencedFileSave.OpensWith));
        ignored.ShouldContain(nameof(ReferencedFileSave.IsCreatedByWildcard));
        ignored.ShouldContain(nameof(ReferencedFileSave.RuntimeType));
    }

    [Fact]
    public void KeepsMembersThatApply()
    {
        var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(AddToEntity("Entities/ObjectBall/BallColor.achx"));

        ignored.ShouldNotContain(nameof(ReferencedFileSave.Name));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.Summary));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.LoadedAtRuntime));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.AddToManagers));
        ignored.ShouldNotContain(nameof(ReferencedFileSave.SourceFile));
    }

    [Theory]
    [InlineData(nameof(ReferencedFileSave.TreatAsCsv), true)]
    [InlineData(nameof(ReferencedFileSave.LoadedAtRuntime), true)]
    [InlineData(nameof(ReferencedFileSave.UseContentPipeline), true)]
    [InlineData(nameof(ReferencedFileSave.SourceFile), true)]
    [InlineData(nameof(ReferencedFileSave.Summary), false)]
    [InlineData(nameof(ReferencedFileSave.CsvDelimiter), false)]
    public void ChangeAffectsShownMembers_OnlyForMembersThatDriveTheHideRules(string member, bool expected)
    {
        ReferencedFileSaveSettingsLogic.ChangeAffectsShownMembers(member).ShouldBe(expected);
    }

    [Fact]
    public void IgnoresEverythingTheSharedRuleSetHides_ForSeveralFileKinds()
    {
        // The drift guard: whatever GetHiddenMemberNames decides, the WPF grid must follow.
        var files = new[]
        {
            AddToEntity("Entities/ObjectBall/BallColor.achx"),
            AddToGlobalContent("GlobalContent/Data.csv"),
            AddToGlobalContent("GlobalContent/Notes.txt"),
            AddToGlobalContent("GlobalContent/Texture.png"),
        };

        foreach (var file in files)
        {
            var ignored = ReferencedFileSaveSettingsLogic.GetMembersToIgnore(file);
            foreach (var hidden in file.GetHiddenMemberNames())
            {
                ignored.ShouldContain(hidden, $"{file.Name}: {hidden}");
            }
        }
    }
}
