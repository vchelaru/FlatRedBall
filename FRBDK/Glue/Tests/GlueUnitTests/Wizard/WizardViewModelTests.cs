using OfficialPlugins.Wizard.Models;
using Shouldly;

namespace GlueUnitTests.Wizard;

// WizardViewModel (OfficialPlugins/Wizard/ViewModels/WizardViewModel.cs) has no validation/gating logic
// that blocks invalid combinations - it's purely a Get<T>/Set(value) backing-store ViewModel with
// [DependsOn]-attributed computed properties used for WPF UI visibility. These tests pin the
// multi-condition boolean computed properties (the ones that AND/OR several backing properties together)
// since a future edit to one branch could silently break the others. Constructing a bare WizardViewModel
// and reading its computed properties requires no Dispatcher/window - confirmed by these tests running
// under plain xunit with no GlueTestBootstrap needed.
public class WizardViewModelTests
{
    [Theory]
    // AddPlayerEntity gates everything - false always yields false regardless of other inputs.
    [InlineData(false, false, GameType.Platformer, PlayerCreationType.SelectOptions, false)]
    [InlineData(false, true, GameType.Platformer, PlayerCreationType.ImportEntity, false)]
    // AddPlayerEntity && AddCloudCollision && PlayerControlType == Platformer
    [InlineData(true, true, GameType.Platformer, PlayerCreationType.SelectOptions, true)]
    // Cloud collision branch requires Platformer specifically - TopDown does not qualify.
    [InlineData(true, true, GameType.TopDown, PlayerCreationType.SelectOptions, false)]
    // AddCloudCollision false and not importing - false.
    [InlineData(true, false, GameType.Platformer, PlayerCreationType.SelectOptions, false)]
    // PlayerCreationType == ImportEntity satisfies the OR regardless of cloud collision/control type.
    [InlineData(true, false, GameType.TopDown, PlayerCreationType.ImportEntity, true)]
    [InlineData(true, false, GameType.Platformer, PlayerCreationType.ImportEntity, true)]
    public void ShowPlayerVsCloudCollision_ShouldReflectAllFourInputs(
        bool addPlayerEntity, bool addCloudCollision, GameType playerControlType,
        PlayerCreationType playerCreationType, bool expected)
    {
        var vm = new WizardViewModel
        {
            AddPlayerEntity = addPlayerEntity,
            AddCloudCollision = addCloudCollision,
            PlayerControlType = playerControlType,
            PlayerCreationType = playerCreationType
        };

        vm.ShowPlayerVsCloudCollision.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void ShowPlayerVsSolidCollision_ShouldRequireBothPlayerAndSolidCollision(
        bool addPlayerEntity, bool addSolidCollision, bool expected)
    {
        var vm = new WizardViewModel
        {
            AddPlayerEntity = addPlayerEntity,
            AddSolidCollision = addSolidCollision
        };

        vm.ShowPlayerVsSolidCollision.ShouldBe(expected);
    }

    [Theory]
    // All four must be true/NoVisuals for the checkbox to show.
    [InlineData(true, true, true, WithVisualType.NoVisuals, true)]
    [InlineData(false, true, true, WithVisualType.NoVisuals, false)]
    [InlineData(true, false, true, WithVisualType.NoVisuals, false)]
    [InlineData(true, true, false, WithVisualType.NoVisuals, false)]
    [InlineData(true, true, true, WithVisualType.WithVisuals, false)]
    public void ShowBorderCollisionCheckBox_ShouldRequireAllFourConditions(
        bool addGameScreen, bool includeGameplayLayerInLevels, bool includeStandardTilesetInLevels,
        WithVisualType withVisualType, bool expected)
    {
        var vm = new WizardViewModel
        {
            AddGameScreen = addGameScreen,
            IncludeGameplayLayerInLevels = includeGameplayLayerInLevels,
            IncludStandardTilesetInLevels = includeStandardTilesetInLevels,
            WithVisualType = withVisualType
        };

        vm.ShowBorderCollisionCheckBox.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void FollowPlayersWithCameraVisibility_ShouldRequireAllThreeConditions(
        bool addGameScreen, bool addPlayerEntity, bool addPlayerListToGameScreen, bool expected)
    {
        var vm = new WizardViewModel
        {
            AddGameScreen = addGameScreen,
            AddPlayerEntity = addPlayerEntity,
            AddPlayerListToGameScreen = addPlayerListToGameScreen
        };

        vm.FollowPlayersWithCameraVisibility.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void IsAddGumScreenToLayerVisible_ShouldRequireAllThreeConditions(
        bool addGameScreen, bool addHudLayer, bool addGum)
    {
        var expected = addGameScreen && addHudLayer && addGum;
        var vm = new WizardViewModel
        {
            AddGameScreen = addGameScreen,
            AddHudLayer = addHudLayer,
            AddGum = addGum
        };

        vm.IsAddGumScreenToLayerVisible.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, PlayerCreationType.SelectOptions, true)]
    [InlineData(true, PlayerCreationType.ImportEntity, false)]
    [InlineData(false, PlayerCreationType.SelectOptions, false)]
    public void IsPlayerCreationSelectingOptions_ShouldRequireAddPlayerEntityAndSelectOptions(
        bool addPlayerEntity, PlayerCreationType playerCreationType, bool expected)
    {
        var vm = new WizardViewModel
        {
            AddPlayerEntity = addPlayerEntity,
            PlayerCreationType = playerCreationType
        };

        vm.IsPlayerCreationSelectingOptions.ShouldBe(expected);
    }

    [Fact]
    public void ShowAddPlatformAnimatorController_ShouldChainThroughDerivedComputedProperty()
    {
        // ShowAddPlatformAnimatorController depends on ShowAddPlayerSpritePlatformerAnimations (itself
        // computed) AND AddPlayerSpritePlatformerAnimations - pins that computed-on-computed chaining works.
        var vm = new WizardViewModel
        {
            AddPlayerSprite = true,
            PlayerControlType = GameType.Platformer,
            AddPlayerSpritePlatformerAnimations = true
        };

        vm.ShowAddPlatformAnimatorController.ShouldBeTrue();

        vm.PlayerControlType = GameType.TopDown;

        vm.ShowAddPlatformAnimatorController.ShouldBeFalse();
    }
}
