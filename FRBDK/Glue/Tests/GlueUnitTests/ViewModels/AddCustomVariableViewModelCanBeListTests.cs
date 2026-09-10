using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ViewModels;

// GitHub issue #2258: int/float CustomVariables can be lists, but only on projects new enough
// to save as JSON - List<float>/List<int> crash XmlSerializer on older (.glux) projects. Gate
// CanBeList on the project's FileVersion, mirroring how AddEntityViewModel.IsDamageableV2 gates
// on GluxVersions.DamageableHasHealth.
public class AddCustomVariableViewModelCanBeListTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;

    public AddCustomVariableViewModelCanBeListTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    [Theory]
    [InlineData("float")]
    [InlineData("int")]
    public void CanBeList_ShouldBeFalse_ForNumericType_WhenProjectIsBelowVersion(string type)
    {
        ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GlueSavedToJson - 1;
        var viewModel = new AddCustomVariableViewModel(new ScreenSave { Name = "Screens/GameScreen/GameScreen" })
        {
            SelectedNewType = type
        };

        viewModel.CanBeList.ShouldBeFalse();
    }

    [Theory]
    [InlineData("float")]
    [InlineData("int")]
    public void CanBeList_ShouldBeTrue_ForNumericType_WhenProjectIsAtOrAboveVersion(string type)
    {
        ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GlueSavedToJson;
        var viewModel = new AddCustomVariableViewModel(new ScreenSave { Name = "Screens/GameScreen/GameScreen" })
        {
            SelectedNewType = type
        };

        viewModel.CanBeList.ShouldBeTrue();
    }

    [Fact]
    public void CanBeList_ShouldBeTrue_ForString_RegardlessOfVersion()
    {
        ObjectFinder.Self.GlueProject.FileVersion =
            (int)GlueProjectSave.GluxVersions.GlueSavedToJson - 1;
        var viewModel = new AddCustomVariableViewModel(new ScreenSave { Name = "Screens/GameScreen/GameScreen" })
        {
            SelectedNewType = "string"
        };

        viewModel.CanBeList.ShouldBeTrue();
    }
}
