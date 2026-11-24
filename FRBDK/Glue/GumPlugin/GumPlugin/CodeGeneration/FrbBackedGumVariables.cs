using System;
using System.Collections.Generic;
using System.Linq;

namespace GumPlugin.CodeGeneration;

/// <summary>
/// The Gum standard-element variables FRB's runtime backs, per standard element. Codegen emits only
/// these: a variable Gum adds to its schema later is left out until someone adds it here, instead of
/// being generated against a runtime that lacks it (CS1061 in the user's game).
/// </summary>
/// <remarks>
/// Checked by both pipelines: <see cref="StandardsCodeGenerator"/> generates a property only for a
/// <see cref="PropertyVariables"/> entry, and <see cref="StateCodeGenerator"/> assigns a standard-element
/// variable in a state only if <see cref="IsAssignableInState"/> says so. Version gates still apply on
/// top of this list (the skip lists in those classes and the per-type generators). Custom variables,
/// exposed variables and state references are not standard-element variables, so they bypass it.
///
/// GumRuntimeMemberContractTests checks each entry against the real runtime types, and
/// GumSchemaVariablesNotGeneratedReport lists the schema variables left out here.
/// </remarks>
public static class FrbBackedGumVariables
{
    /// <summary>
    /// Members of GraphicalUiElement itself, so they are assignable on every runtime and never generated
    /// as properties. Names with spaces are legacy .gutx names, renamed through
    /// StateCodeGenerator.VariableNamesToReplaceForStates.
    /// </summary>
    /// <remarks>
    /// IgnoredByParentLayout is deliberately missing: no shipped FRB GumCore has it yet. Add it behind a
    /// GluxVersions gate once one does.
    /// </remarks>
    public static readonly IReadOnlyCollection<string> GraphicalUiElementVariables = new HashSet<string>(StringComparer.Ordinal)
    {
        "X", "Y", "Width", "Height",
        "XOrigin", "YOrigin", "XUnits", "YUnits", "WidthUnits", "HeightUnits",
        "X Origin", "Y Origin", "X Units", "Y Units", "Width Units", "Height Units",
        "MinWidth", "MaxWidth", "MinHeight", "MaxHeight",
        "Visible", "Parent", "Rotation", "FlipHorizontal", "Wrap",
        "HasEvents", "ExposeChildrenEvents",
        "ChildrenLayout", "Children Layout", "ClipsChildren", "Clips Children", "WrapsChildren", "Wraps Children",
        "StackSpacing", "AutoGridHorizontalCells", "AutoGridVerticalCells", "IgnoredByParentSize",
        "TextureAddress", "TextureLeft", "TextureTop", "TextureWidth", "TextureHeight",
        "TextureWidthScale", "TextureHeightScale",
        "Texture Address", "Texture Left", "Texture Top", "Texture Width", "Texture Height",
        "Texture Width Scale", "Texture Height Scale",
    };

    static readonly string[] SkiaColors =
    {
        "Alpha", "Red", "Green", "Blue",
    };

    static readonly string[] SkiaGradientAndDropshadow =
    {
        "Alpha1", "Red1", "Green1", "Blue1",
        "Alpha2", "Red2", "Green2", "Blue2",
        "UseGradient", "GradientType",
        "GradientX1", "GradientX1Units", "GradientY1", "GradientY1Units",
        "GradientX2", "GradientX2Units", "GradientY2", "GradientY2Units",
        "GradientInnerRadius", "GradientInnerRadiusUnits", "GradientOuterRadius", "GradientOuterRadiusUnits",
        "HasDropshadow", "DropshadowOffsetX", "DropshadowOffsetY", "DropshadowBlurX", "DropshadowBlurY",
        "DropshadowAlpha", "DropshadowRed", "DropshadowGreen", "DropshadowBlue",
    };

    static readonly string[] SkiaStroke =
    {
        "IsFilled", "StrokeWidth", "StrokeDashLength", "StrokeGapLength",
    };

    /// <summary>
    /// Variables generated as a property that forwards to the contained renderable (ContainedText.Foo and
    /// so on), or through one of StandardsCodeGenerator's getter/setter replacements.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> PropertyVariables =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Arc"] = Set(SkiaColors, SkiaGradientAndDropshadow, new[] { "Blend", "IsEndRounded", "StartAngle", "SweepAngle", "Thickness" }),
            ["Canvas"] = Set(new[] { "Alpha" }),
            ["Circle"] = Set(new[] { "Alpha", "Red", "Green", "Blue", "Radius" }),
            ["ColoredCircle"] = Set(SkiaColors, SkiaGradientAndDropshadow, SkiaStroke, new[] { "Blend", "FlipVertical" }),
            ["ColoredRectangle"] = Set(new[] { "Alpha", "Red", "Green", "Blue", "Blend" }),
            ["Container"] = Set(),
            ["LottieAnimation"] = Set(SkiaColors, new[] { "Blend", "Animate", "FlipVertical" }),
            ["NineSlice"] = Set(new[]
            {
                "Alpha", "Red", "Green", "Blue", "Blend", "ColorOperation", "SourceFile", "Animate",
                "BorderScale", "CustomBorderScaleBottom", "CustomBorderScaleLeft", "CustomBorderScaleRight", "CustomBorderScaleTop",
                "CustomFrameTextureCoordinateBottom", "CustomFrameTextureCoordinateLeft",
                "CustomFrameTextureCoordinateRight", "CustomFrameTextureCoordinateTop", "CustomFrameTextureCoordinateWidth",
                "IsTilingMiddleSections",
            }),
            ["Polygon"] = Set(new[] { "Alpha", "Red", "Green", "Blue" }),
            // The fill/stroke family is live-gated on the Gum project version: see
            // StandardsCodeGenerator.IsRectangleFillStrokeSupported.
            ["Rectangle"] = Set(new[] { "Alpha", "Red", "Green", "Blue" }, StandardsCodeGenerator.RectangleFillStrokeVariableNames),
            ["RoundedRectangle"] = Set(SkiaColors, SkiaGradientAndDropshadow, SkiaStroke, new[] { "Blend", "CornerRadius" }),
            ["Sprite"] = Set(new[] { "Alpha", "Red", "Green", "Blue", "Blend", "ColorOperation", "SourceFile", "Animate", "FlipVertical" }),
            ["Svg"] = Set(SkiaColors, new[] { "Blend", "FlipVertical" }),
            ["Text"] = Set(new[]
            {
                "Alpha", "Red", "Green", "Blue", "Text", "HorizontalAlignment", "VerticalAlignment",
                "FontScale", "Font Scale", "LineHeightMultiplier", "MaxLettersToShow", "MaxNumberOfLines",
                "TextOverflowHorizontalMode",
                // RenderingLibrary.Graphics.Text has a single DropshadowColor, not the per-channel color.
                "HasDropshadow", "DropshadowOffsetX", "DropshadowOffsetY",
            }),
        };

    /// <summary>
    /// Variables a per-type generator emits its own member for (TextCodeGenerator's Font family,
    /// ContainerCodeGenerator's Alpha/Blend/IsRenderTarget, SpriteCodeGenerator's CurrentChainName), so they are assignable in states but never
    /// generated by the standard property loop.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> RuntimeOnlyVariables =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Container"] = Set(new[] { "Alpha", "Blend", "IsRenderTarget" }),
            // Assigned on instances only: StateCodeGenerator's Svg/LottieAnimation SourceFile special case
            // checks the container, not the instance. Kept so existing output does not change.
            ["LottieAnimation"] = Set(new[] { "SourceFile" }),
            ["Svg"] = Set(new[] { "Animate", "SourceFile" }),
            ["Sprite"] = Set(new[] { "CurrentChainName" }),
            ["Text"] = Set(new[]
            {
                "Font", "FontSize", "IsBold", "IsItalic", "OutlineThickness", "UseFontSmoothing",
                "UseCustomFont", "CustomFontFile", "TextOverflowVerticalMode",
            }),
        };

    public static bool IsGeneratedAsProperty(string standardElementName, string variableRootName) =>
        PropertyVariables.TryGetValue(standardElementName, out var names) && names.Contains(variableRootName);

    /// <summary>
    /// Whether a state may assign this variable on a runtime whose root standard element is
    /// <paramref name="standardElementName"/>. An element with no entry gets GraphicalUiElement's members only.
    /// </summary>
    public static bool IsAssignableInState(string standardElementName, string variableRootName) =>
        GraphicalUiElementVariables.Contains(variableRootName)
        || IsGeneratedAsProperty(standardElementName, variableRootName)
        || (RuntimeOnlyVariables.TryGetValue(standardElementName, out var names) && names.Contains(variableRootName));

    static IReadOnlyCollection<string> Set(params IEnumerable<string>[] groups) =>
        new HashSet<string>(groups.SelectMany(group => group), StringComparer.Ordinal);
}
