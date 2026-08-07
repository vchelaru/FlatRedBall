using FlatRedBall.Glue.Elements;
using OfficialPlugins.PropertyGrid;
using Shouldly;
using System.Collections.Generic;
using WpfDataUi.Controls;

namespace GlueUnitTests.PropertyGrid;

public class VariableDefinitionDisplayerHelperTests
{
    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetMinValueOnly_WhenOnlyMinValueIsSpecified()
    {
        var variableDefinition = new VariableDefinition { MinValue = 0 };
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(variableDefinition, null, properties);

        properties[nameof(TextBoxDisplay.MinValue)].ShouldBe(0.0);
        properties.ShouldNotContainKey(nameof(TextBoxDisplay.MaxValue));
    }

    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetMaxValueOnly_WhenOnlyMaxValueIsSpecified()
    {
        var variableDefinition = new VariableDefinition { MaxValue = 100 };
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(variableDefinition, null, properties);

        properties[nameof(TextBoxDisplay.MaxValue)].ShouldBe(100.0);
        properties.ShouldNotContainKey(nameof(TextBoxDisplay.MinValue));
    }

    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetNothing_WhenBothMinAndMaxAreSpecified()
    {
        // Both-bounds-set is handled by the caller forcing a SliderDisplay; this helper must not
        // also stamp TextBoxDisplay-named properties onto that slider.
        var variableDefinition = new VariableDefinition { MinValue = 0, MaxValue = 100 };
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(variableDefinition, typeof(SliderDisplay), properties);

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetNothing_WhenNeitherMinNorMaxIsSpecified()
    {
        var variableDefinition = new VariableDefinition();
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(variableDefinition, null, properties);

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetNothing_WhenAPreferredDisplayerWasAlreadyChosen()
    {
        // e.g. the variable's own VariableDefinition.PreferredDisplayer already picked a custom
        // control - don't stamp unrelated MinValue/MaxValue properties onto it.
        var variableDefinition = new VariableDefinition { MinValue = 0 };
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(variableDefinition, typeof(object), properties);

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyAsymmetricMinMax_ShouldSetNothing_WhenVariableDefinitionIsNull()
    {
        var properties = new Dictionary<string, object>();

        VariableDefinitionDisplayerHelper.ApplyAsymmetricMinMax(null, null, properties);

        properties.ShouldBeEmpty();
    }
}
