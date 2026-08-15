using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests;

/// <summary>
/// When AddObjectViewModel.IsTypePredetermined is true (e.g. adding to a typed list, or "Add Layer"),
/// there is no type to pick, so the "New Object" WPF window should never be constructed - see
/// glue-add-object-flow skill. If this regresses, the window pops up on a thread with no message
/// pump / no STA apartment, which throws instead of showing anything.
/// </summary>
public class NewObjectDialogTests
{
    public NewObjectDialogTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        ObjectFinder.Self.GlueProject = new GlueProjectSave();
    }

    [Fact]
    public void CreateAndShowAddNamedObjectWindow_TypePredetermined_DoesNotShowWindow()
    {
        var entity = new EntitySave { Name = "Entities\\Player" };
        AddObjectViewModel viewModel = new AddObjectViewModel
        {
            ForcedElementToAddTo = entity,
            SourceType = SourceType.FlatRedBallType,
            SelectedAti = AvailableAssetTypes.CommonAtis.Layer,
            IsTypePredetermined = true
        };

        var result = DialogCommands.CreateAndShowAddNamedObjectWindow(ref viewModel);

        result.ShouldBe(true);
    }
}
