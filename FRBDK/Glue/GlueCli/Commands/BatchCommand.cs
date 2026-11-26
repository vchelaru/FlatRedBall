using System.IO;
using System.Text;

namespace GlueCli.Commands;

/// <summary>
/// <c>batch</c>: several commands against one load of the project. The runner runs each line through the
/// same per-command step a single run uses (<see cref="GlueCliRunner"/>'s RunStep) with writes held, then
/// saves and generates once, so this file only reads and checks the lines.
/// </summary>
public static class BatchCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "batch",
        Summary = "Run several commands, one per line of a file or stdin, against one load of the project.",
        Usage = "batch <project> (--file <path> | --stdin) [--keep-going]",
        Description =
@"Starts Glue and loads the project once, then runs each line as its own command, in order. A line is a
command without the project: add-object --element GameScreen --type Sprite --name S1. Each command makes the
edit it makes on its own, against the project in memory. Nothing is checked, saved or regenerated between
lines: after the last line the batch collects Glue's errors once and reports the ones the batch added, then
saves every element it changed, the .gluj (when its own data changed) and the .csproj, and regenerates the
code of what changed, once. This saves Glue's startup, the load, and an error check, save and codegen on every
line. For errors per command, run the commands one at a time. check and describe in a batch still report the
project's errors where they run.

Arguments split on spaces. Quote one that has spaces in double or single quotes (--name ""Entire File""); a
backslash is an ordinary character, so Entities\Enemy needs no escaping. Blank lines and lines starting with
# are ignored. --json, --dry-run and --help belong on the batch command line, not on a line.

Every line is checked before Glue starts: an unknown command or option, a missing required option, a
project path on a line, or regen (which writes every generated file; run it on its own) exits 2 and runs
nothing.

If any command does not exit 0 (it could not apply its edit, or printed error output or a dialog), or the
commands together left errors the project did not have when it loaded, the batch writes nothing at all: the
project on disk stays as it was. The batch stops at the first command that does not exit 0, unless
--keep-going is given, which runs every line and reports every failure, then still writes nothing. With
--dry-run the whole batch runs against one temp copy, so each command sees the ones before it, and the batch
reports the combined changes.

The exit code is the first non-zero exit code of the commands that ran, else 1 if the batch left new errors or
the final save or codegen reported errors, else 0. With --json the output is {command: ""batch"", project,
exitCode, failure, commandCount, results, errors, preexistingErrors, filesWritten, writeErrors, changes (with
--dry-run), startupSeconds, loadSeconds, writeSeconds, elapsedSeconds}. results holds each command's own
--json object, in order (an edit's errors and preexistingErrors and every filesWritten are empty), and is
shorter than commandCount when the batch stopped early. errors are the ones the whole batch added (collected
only when every command exited 0); preexistingErrors what the project already had. filesWritten and
writeErrors are what the final save and codegen wrote and reported; writeSeconds is how long they took.",
        Options = new CliOption[]
        {
            new("file", "path", "Read the commands from this file."),
            new("stdin", null, "Read the commands from standard input."),
            new("keep-going", null, "Run every command even after one fails."),
        },
        Examples = new[]
        {
            ("batch MyGame --file edits.txt", "Run the commands in edits.txt; at the first failure, stop and write nothing."),
            ("batch MyGame --stdin --json < edits.txt", "Same, reading stdin, JSON output."),
            ("batch MyGame --file edits.txt --dry-run", "Report what the whole batch would change, changing nothing."),
        },
        ValidateArguments = context => context.Has("file") == context.Has("stdin")
            ? "batch needs exactly one of --file or --stdin."
            : null,
    };

    /// <summary>
    /// The commands in the batch's --file or --stdin, each parsed and checked as a single run checks its
    /// arguments, or null with <paramref name="error"/> naming the first bad line.
    /// </summary>
    internal static List<CliStep>? ReadSteps(CliContext context, TextReader stdin, string csproj, out string? error)
    {
        string text;
        var file = context.Get("file");
        if (file != null)
        {
            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                error = $"Could not read the batch file: {e.Message}";
                return null;
            }
        }
        else
        {
            text = stdin.ReadToEnd();
        }
        return ParseSteps(text, csproj, out error);
    }

    internal static List<CliStep>? ParseSteps(string text, string csproj, out string? error)
    {
        var steps = new List<CliStep>();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            var lineError = ParseLine(line, csproj, out var step);
            if (lineError != null)
            {
                error = $"Batch line {i + 1}: {lineError}";
                return null;
            }
            steps.Add(step!);
        }
        if (steps.Count == 0)
        {
            error = "The batch has no commands.";
            return null;
        }
        error = null;
        return steps;
    }

    static string? ParseLine(string line, string csproj, out CliStep? step)
    {
        step = null;
        var args = SplitArguments(line, out var splitError);
        if (args == null)
        {
            return splitError;
        }
        var command = GlueCliRunner.FindCommand(args[0]);
        if (command == null)
        {
            return $"unknown command '{args[0]}'.";
        }
        if (command == Definition)
        {
            return "a batch cannot run batch.";
        }
        if (command == RegenCommand.Definition)
        {
            return "regen writes every generated file, so it cannot run in a batch, which writes once at the end. " +
                "Run it on its own.";
        }

        var options = GlueCliRunner.ParseOptions(command, args.Skip(1).ToList(), out var positional, out var parseError);
        if (parseError != null)
        {
            return parseError;
        }
        var batchLevel = new[] { "json", "dry-run", "help" }.FirstOrDefault(options.ContainsKey);
        if (batchLevel != null)
        {
            return $"--{batchLevel} applies to the whole batch; put it on the batch command line.";
        }
        if (positional.Count > 0)
        {
            return $"'{command.Name}' takes no project in a batch (the batch's project is used), got: {string.Join(" ", positional)}.";
        }

        var result = new CliResult { Command = command.Name, Project = csproj };
        var context = new CliContext(options, result);
        var validationError = command.ValidateArguments?.Invoke(context);
        if (validationError != null)
        {
            return validationError;
        }
        step = new CliStep(command, context, result);
        return null;
    }

    /// <summary>
    /// Splits a line on whitespace. Double or single quotes group an argument and are removed; there are no
    /// escapes, so a backslash in a Windows-style path is kept as-is.
    /// </summary>
    internal static List<string>? SplitArguments(string line, out string? error)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        var inArgument = false;
        char? quote = null;
        foreach (var character in line)
        {
            if (quote != null)
            {
                if (character == quote)
                {
                    quote = null;
                }
                else
                {
                    current.Append(character);
                }
            }
            else if (character is '"' or '\'')
            {
                quote = character;
                inArgument = true;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (inArgument)
                {
                    args.Add(current.ToString());
                    current.Clear();
                    inArgument = false;
                }
            }
            else
            {
                current.Append(character);
                inArgument = true;
            }
        }
        if (quote != null)
        {
            error = $"the {quote} quote is not closed.";
            return null;
        }
        if (inArgument)
        {
            args.Add(current.ToString());
        }
        error = null;
        return args;
    }
}
