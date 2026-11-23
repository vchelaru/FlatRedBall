using System.Collections.Generic;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace FlatRedBall.Glue.Headless;

/// <summary>
/// A plugin whose only job is to capture what Glue prints to its error and normal output, for a host with
/// no output window (the tests, GlueCli). Registered like any other plugin.
///
/// Code generation swallows failures: <c>CodeWriter.GenerateCode</c> and
/// <c>CodeGeneratorManager.GenerateAllElements</c> catch per-element exceptions and report them to the
/// error output rather than throwing. In Glue.exe a developer sees those in the output window; in a
/// headless host, with nothing subscribed, they vanish, and a generator that failed halfway looks exactly
/// like one that had nothing to do.
///
/// The lists are static so every instance (and subclass) in a process records into the same place.
/// </summary>
public class HeadlessOutputRecorder : PluginBase
{
    public static List<string> Errors { get; } = new();

    /// <summary>
    /// Glue's normal (non-error) output. Some give-up paths report through this rather than the error
    /// channel - <c>GenerateAllCodeSync</c> abandoning the rest of the elements with "Stopping generation
    /// because the project is closing" is the notable one - so a host that only watched
    /// <see cref="Errors"/> would see a silent partial regeneration as success.
    /// </summary>
    public static List<string> Output { get; } = new();

    public override string FriendlyName => "Headless Output Recorder";
    public override System.Version Version => new(1, 0);

    public static void Clear()
    {
        lock (Errors) { Errors.Clear(); }
        lock (Output) { Output.Clear(); }
    }

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

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
}
