using FlatRedBall.Glue.Headless;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// The test name for Glue's <see cref="HeadlessOutputRecorder"/>: same static <c>Errors</c>/<c>Output</c>
/// lists, so a test and GlueCli's command runner read what the same recorder captured.
/// </summary>
internal class ErrorRecordingPlugin : HeadlessOutputRecorder
{
    public override string FriendlyName => "Error Recording Plugin (test)";
}
