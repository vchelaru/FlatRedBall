using OfficialPlugins.PathPlugin.Views;
using Shouldly;
using Xunit;

namespace GlueUnitTests.PathPlugin;

// Pins issue #2157: PathInstance's X/Y/Angle fields didn't support the click-and-drag value
// scrubbing every other numeric field in Glue supports. PathSegmentView.SnapDraggedValue is the pure
// snapping step the label-drag mouse handlers call on every mouse-move; it reuses WpfDataUi's own
// TextBoxDisplayLogic.SnapDraggedValue so the drag feel matches other numeric fields exactly.
public class PathSegmentViewDragTests
{
    [Fact]
    public void SnapDraggedValue_ShouldRoundToNearestWholeNumber()
    {
        PathSegmentView.SnapDraggedValue(12.8).ShouldBe(13f);
        PathSegmentView.SnapDraggedValue(-12.8).ShouldBe(-13f);
        PathSegmentView.SnapDraggedValue(0.4).ShouldBe(0f);
    }

    [Fact]
    public void SnapDraggedValue_ShouldReflectAccumulatedPixelDrag()
    {
        // Mirrors Label_MouseMove: dragUnroundedValue accumulates raw pixel deltas across moves, and
        // is re-snapped on every tick rather than re-applying the delta on top of an already-rounded
        // value (issue #3191 in the ported WpfDataUi logic).
        double startingValue = 100;
        double unrounded = startingValue;

        unrounded += 3; // first move, 3px right
        PathSegmentView.SnapDraggedValue(unrounded).ShouldBe(103f);

        unrounded += 3; // second move, 3px right
        PathSegmentView.SnapDraggedValue(unrounded).ShouldBe(106f);

        unrounded -= 10; // third move, 10px left
        PathSegmentView.SnapDraggedValue(unrounded).ShouldBe(96f);
    }
}
