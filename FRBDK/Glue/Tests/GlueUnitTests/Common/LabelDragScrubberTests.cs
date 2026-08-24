using OfficialPlugins.Common.Controls;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Common;

// Pins issue #2157 (PathInstance X/Y/Angle) and #2190 (polygon Points tab X/Y): both plugins wire
// their "X:"/"Y:" labels to LabelDragScrubber for click-and-drag value scrubbing.
// LabelDragScrubber.SnapDraggedValue is the pure snapping step its mouse-move handler calls on every
// tick; it reuses WpfDataUi's own TextBoxDisplayLogic.SnapDraggedValue so the drag feel matches every
// other numeric field in Glue.
public class LabelDragScrubberTests
{
    [Fact]
    public void SnapDraggedValue_ShouldRoundToNearestWholeNumber()
    {
        LabelDragScrubber.SnapDraggedValue(12.8).ShouldBe(13f);
        LabelDragScrubber.SnapDraggedValue(-12.8).ShouldBe(-13f);
        LabelDragScrubber.SnapDraggedValue(0.4).ShouldBe(0f);
    }

    [Fact]
    public void SnapDraggedValue_ShouldReflectAccumulatedPixelDrag()
    {
        // Mirrors HandleMouseMove: dragUnroundedValue accumulates raw pixel deltas across moves, and
        // is re-snapped on every tick rather than re-applying the delta on top of an already-rounded
        // value (issue #3191 in the ported WpfDataUi logic).
        double startingValue = 100;
        double unrounded = startingValue;

        unrounded += 3; // first move, 3px right
        LabelDragScrubber.SnapDraggedValue(unrounded).ShouldBe(103f);

        unrounded += 3; // second move, 3px right
        LabelDragScrubber.SnapDraggedValue(unrounded).ShouldBe(106f);

        unrounded -= 10; // third move, 10px left
        LabelDragScrubber.SnapDraggedValue(unrounded).ShouldBe(96f);
    }
}
