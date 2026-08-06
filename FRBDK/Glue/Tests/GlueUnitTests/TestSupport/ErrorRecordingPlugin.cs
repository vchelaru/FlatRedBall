using System.Collections.Generic;
using FlatRedBall.Glue.Plugins;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// A real plugin (registered through the same <see cref="PluginManager.RegisterPluginForTesting"/> path as
/// any other) whose only job is to capture what Glue prints to its error output.
///
/// This exists because code generation swallows failures: <c>CodeWriter.GenerateCode</c> and
/// <c>CodeGeneratorManager.GenerateAllElements</c> catch per-element exceptions and report them to the
/// error output rather than throwing. In Glue.exe a developer sees those in the output window; in a test
/// host, with nothing subscribed, they vanish - and a generator that failed halfway looks exactly like one
/// that had nothing to do. Subscribing makes a gold-project test able to assert that a load was actually
/// clean, instead of only that it did not crash.
/// </summary>
internal class ErrorRecordingPlugin : PluginBase
{
    public static List<string> Errors { get; } = new();

    /// <summary>
    /// Glue's normal (non-error) output. Some give-up paths report through this rather than the error
    /// channel - <c>GenerateAllCodeSync</c> abandoning the rest of the elements with "Stopping generation
    /// because the project is closing" is the notable one - so a test that only watched
    /// <see cref="Errors"/> would see a silent partial regeneration as success.
    /// </summary>
    public static List<string> Output { get; } = new();

    public override string FriendlyName => "Error Recording Plugin (test)";
    public override System.Version Version => new(1, 0);

    public override void StartUp()
    {
        OnErrorOutputHandler = error =>
        {
            lock (Errors) { Errors.Add(error); }
        };

        OnOutputHandler = output =>
        {
            lock (Output) { Output.Add(output); }
        };
    }

    public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason) => true;
}
