namespace GlueCli;

/// <summary>
/// One GlueCli command: its help text, the options it accepts, and what it does once the project is loaded.
/// Each command lives in its own file under Commands/ and is listed once in <see cref="GlueCliRunner.Commands"/>.
/// The runner parses the options, loads the project, runs <see cref="Run"/>, and does the shared tail (wait
/// for tasks, save for edit commands, collect errors, print).
/// </summary>
public sealed class CliCommand
{
    public required string Name { get; init; }

    /// <summary>One line, shown in <c>GlueCli --help</c>.</summary>
    public required string Summary { get; init; }

    /// <summary>The usage line after "GlueCli", for example <c>add-object &lt;project&gt; --element &lt;element&gt;</c>.</summary>
    public required string Usage { get; init; }

    /// <summary>Shown in <c>GlueCli &lt;command&gt; --help</c> under the usage line.</summary>
    public required string Description { get; init; }

    public IReadOnlyList<CliOption> Options { get; init; } = Array.Empty<CliOption>();

    /// <summary>Complete invocations, each with a comment saying what it does. Agents copy these.</summary>
    public IReadOnlyList<(string Invocation, string Comment)> Examples { get; init; } = Array.Empty<(string, string)>();

    /// <summary>
    /// An edit command changes the project. The runner records the errors the project already had before
    /// <see cref="Run"/>, so only errors the edit introduced fail the run, and saves the project and its
    /// element files afterwards unless the command reported a failure.
    /// </summary>
    public bool IsEdit { get; init; }

    /// <summary>
    /// Checks the combination of options before Glue starts (a missing required option, two options that
    /// exclude each other) and returns the message, or null when they are fine. A message exits 2 with this
    /// command's help. Checks that need the project (does the element exist) belong in <see cref="Run"/>.
    /// </summary>
    public Func<CliContext, string?>? ValidateArguments { get; init; }

    /// <summary>Runs after the project is loaded and its load tasks finished. Null for <c>check</c>.</summary>
    public Func<CliContext, Task>? Run { get; init; }

    public string GetHelp()
    {
        var lines = new List<string>
        {
            $"GlueCli {Name} - {Summary}",
            "",
            "Usage:",
            $"  GlueCli {Usage}",
            "",
            Description.TrimEnd(),
            "",
            "Options:",
        };

        var allOptions = Options.Concat(GlueCliRunner.GlobalOptions).ToList();
        var width = allOptions.Max(option => option.Signature.Length);
        foreach (var option in allOptions)
        {
            var descriptionLines = option.Description.Split('\n');
            lines.Add($"  {option.Signature.PadRight(width)}  {descriptionLines[0].TrimEnd()}");
            lines.AddRange(descriptionLines.Skip(1).Select(line => new string(' ', width + 4) + line.Trim()));
        }

        if (Examples.Count > 0)
        {
            lines.Add("");
            lines.Add("Examples:");
            foreach (var (invocation, comment) in Examples)
            {
                lines.Add($"  # {comment}");
                lines.Add($"  GlueCli {invocation}");
            }
        }

        lines.Add("");
        lines.Add(GlueCliRunner.ExitCodeHelp);
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// An option a command accepts. <see cref="ValueName"/> null means a flag that takes no value. Values are
/// passed as <c>--name value</c> or <c>--name=value</c>.
/// </summary>
public sealed record CliOption(string Name, string? ValueName, string Description)
{
    public bool TakesValue => ValueName != null;

    public string Signature => TakesValue ? $"--{Name} <{ValueName}>" : $"--{Name}";
}

/// <summary>
/// What a command's <see cref="CliCommand.Run"/> gets: the parsed options, and the two ways to report back.
/// </summary>
public sealed class CliContext
{
    readonly Dictionary<string, string?> options;
    readonly CliResult result;

    internal CliContext(Dictionary<string, string?> options, CliResult result)
    {
        this.options = options;
        this.result = result;
    }

    /// <summary>The value of an option that takes one, or null when it was not passed.</summary>
    public string? Get(string optionName) => options.TryGetValue(optionName, out var value) ? value : null;

    /// <summary>Whether the option (a flag or a valued option) was passed.</summary>
    public bool Has(string optionName) => options.ContainsKey(optionName);

    /// <summary>
    /// The edit could not be applied (bad input, a Glue rule it breaks). The run exits 1 with this message
    /// and the project is not saved. Call this before changing anything, then return.
    /// </summary>
    public void Fail(string message)
    {
        result.EditFailures.Add(message);
    }

    public bool HasFailed => result.EditFailures.Count > 0;

    /// <summary>
    /// What the edit did. <paramref name="data"/> is serialized as the <c>edit</c> property of the --json
    /// output, so give it stable property names; <paramref name="text"/> is the line the text output shows.
    /// </summary>
    public void Report(object data, string text)
    {
        result.Edit = data;
        result.EditSummary = text;
    }
}
