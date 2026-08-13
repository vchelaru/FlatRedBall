using CompilerLibrary.Models;
using Shouldly;
using Xunit;

namespace GlueUnitTests.GameCommunicationPlugin;

/// <summary>
/// Live edit drags a whole object using SnapSize but drags a Polygon point using the separate
/// PolygonPointSnapSize. If that default is much finer than SnapSize, point dragging is
/// technically snapping but the increments are too small to see, so it reads as "not snapping."
/// </summary>
public class CompilerSettingsSnapDefaultsTests
{
    [Fact]
    public void SetDefaults_PolygonPointSnapSize_MatchesSnapSize()
    {
        var model = new CompilerSettingsModel();

        model.SetDefaults();

        model.PolygonPointSnapSize.ShouldBe(model.SnapSize,
            "polygon point dragging should visibly snap like whole-object dragging out of the box");
    }
}
