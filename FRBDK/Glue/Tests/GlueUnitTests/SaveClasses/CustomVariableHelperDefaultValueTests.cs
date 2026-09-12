using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

// GitHub issue #2283: exposing an enum-typed property (a Text's MaxWidthBehavior, VerticalAlignment,
// ColorOperation, ...) as a custom variable goes through the same "ask TypeManager for a type's default"
// call that crashed in #2272, just via CustomVariableHelper instead of VariableSendingManager. It never
// crashed here - the whole call is wrapped in a blanket try/catch - so the same root cause showed up
// quieter: either no default gets set at all (an enum TypeManager has no primitive entry for), or the
// wrong one (ColorOperation's hardcoded "0" is Texture, not a Text's actual default of ColorTextureAlpha).
public class CustomVariableHelperDefaultValueTests : IDisposable
{
    private readonly GlueProjectSave _originalGlueProject;
    private readonly ScreenSave _screen;

    public CustomVariableHelperDefaultValueTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalGlueProject = ObjectFinder.Self.GlueProject;

        var glueProject = new GlueProjectSave { FileVersion = GlueProjectSave.LatestVersion };
        _screen = new ScreenSave { Name = "Screens\\GameScreen" };
        glueProject.Screens.Add(_screen);
        ObjectFinder.Self.GlueProject = glueProject;
    }

    public void Dispose()
    {
        ObjectFinder.Self.GlueProject = _originalGlueProject;
    }

    private NamedObjectSave AddNamedObject(string sourceClassType, string instanceName)
    {
        var nos = new NamedObjectSave
        {
            InstanceName = instanceName,
            SourceType = SourceType.FlatRedBallType,
            SourceClassType = sourceClassType,
        };
        _screen.NamedObjects.Add(nos);
        return nos;
    }

    private CustomVariable ExposeProperty(NamedObjectSave nos, string sourceObjectProperty, string overridingPropertyType)
    {
        var customVariable = new CustomVariable
        {
            Name = nos.InstanceName + sourceObjectProperty,
            SourceObject = nos.InstanceName,
            SourceObjectProperty = sourceObjectProperty,
            OverridingPropertyType = overridingPropertyType,
        };
        CustomVariableHelper.SetDefaultValueFor(customVariable, _screen);
        return customVariable;
    }

    [Fact]
    public void SetDefaultValueFor_ShouldUseTheDeclaredDefault_NotTheStalePrimitiveTableEntry()
    {
        // Before the fix: TypeManager.TryGetDefaultForType has a hardcoded "ColorOperation" => "0" entry,
        // so this didn't crash or no-op - it silently resolved to Texture (0), a Sprite's default, even
        // though this is a Text (whose ColorOperation defaults to ColorTextureAlpha).
        var text = AddNamedObject("FlatRedBall.Graphics.Text", "TextInstance");

        var customVariable = ExposeProperty(text, "ColorOperation", "ColorOperation");

        customVariable.DefaultValue.ShouldBe(FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha);
    }

    [Fact]
    public void SetDefaultValueFor_ShouldUseTheDeclaredDefault_ForAnEnumWithNoPrimitiveTableEntry()
    {
        // Before the fix: TypeManager.TryGetDefaultForType has no "VerticalAlignment" entry, so
        // GetDefaultForType threw, the blanket catch swallowed it, and the custom variable was left with
        // no default at all.
        var text = AddNamedObject("FlatRedBall.Graphics.Text", "TextInstance");

        var customVariable = ExposeProperty(text, "VerticalAlignment", "VerticalAlignment");

        // Center is 2, not the CLR zero (Top) - proves the declared default drove this, not a lucky zero.
        customVariable.DefaultValue.ShouldBe(FlatRedBall.Graphics.VerticalAlignment.Center);
    }

    [Fact]
    public void SetDefaultValueFor_ShouldUseTheOwningTypesOwnDeclaredDefault_NotAnotherTypesSameEnum()
    {
        // Sprite and Text both expose a ColorOperation, with different declared defaults (Texture vs
        // ColorTextureAlpha) - the lookup has to be scoped by the NamedObjectSave's own AssetTypeInfo.
        var sprite = AddNamedObject("FlatRedBall.Sprite", "SpriteInstance");

        var customVariable = ExposeProperty(sprite, "ColorOperation", "ColorOperation");

        customVariable.DefaultValue.ShouldBe(FlatRedBall.Graphics.ColorOperation.Texture);
    }
}
