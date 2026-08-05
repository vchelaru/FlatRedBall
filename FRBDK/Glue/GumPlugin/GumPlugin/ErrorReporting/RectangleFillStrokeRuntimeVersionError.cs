using FlatRedBall.Glue.Errors;

namespace GumPlugin.ErrorReporting
{
    /// <summary>
    /// Issue #1967 bug #2 - a gumx v3 (ShapeVariableExpansion) project generates Rectangle codegen
    /// against RenderingLibrary.Math.Geometry.FilledStrokedRectangle, which only exists on a
    /// referenced Gum runtime assembly stamped GumSyntaxVersion 4 or later. A stale, non-source-linked
    /// binary reference predating that (Gum PR #4342) compiles fine but NREs at runtime
    /// (RectangleRuntime.Generated.cs's set_FillAlpha etc., ContainedRectangle resolves to null) - this
    /// surfaces that mismatch as a clear Glue-time error instead.
    /// </summary>
    internal class RectangleFillStrokeRuntimeVersionError : ErrorViewModel
    {
        public override string UniqueId => Details;

        public RectangleFillStrokeRuntimeVersionError(int? detectedVersion)
        {
            var detected = detectedVersion.HasValue ? detectedVersion.Value.ToString() : "not present";
            Details =
                "This project's Gum project is on gumx version 3+ (Rectangle Fill/Stroke support), " +
                "but the referenced Gum runtime assembly (GumCore.*.dll) declares GumSyntaxVersion " +
                $"{detected}, which is below the required version 4. Rectangle instances with " +
                "IsFilled/FillColor/StrokeColor/StrokeWidth set will compile but throw a " +
                "NullReferenceException at runtime. Fix: relink FlatRedBall/Gum as source (recommended " +
                "for local development), or update your GumCore NuGet package reference to a version " +
                "built from Gum commit 43e2e7a42 or later (\"Add FilledStrokedRectangle\", #4342).";
        }

        public override bool GetIfIsFixed()
        {
            // Re-evaluated by RectangleFillStrokeRuntimeVersionCheck.GetIfIsFixed() via ErrorReporter's
            // next scan - this instance's job is only to hold the message computed at report time.
            return RectangleFillStrokeRuntimeVersionCheck.GetIfCurrentlyFixed();
        }
    }
}
