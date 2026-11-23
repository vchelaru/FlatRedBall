using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCli.Commands;

/// <summary>
/// <c>remove-event</c>: removes an event from a screen or entity through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IElementCommands.RemoveEventFromElement"/>,
/// the call the editor's Delete ends in for an event. The runner regenerates and saves the element.
/// </summary>
public static class RemoveEventCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "remove-event",
        Summary = "Remove an event from a screen or entity.",
        Usage = "remove-event <project> --element <element> --name <name>",
        Description =
@"Removes the event the way the editor's Delete does on an event: the element stops generating it, and
removing the element's last event takes its .Generated.Event.cs out of the .csproj. The handler method
(On<name>) stays in the element's hand-written .Event.cs; delete it yourself if you no longer want it. Then
saves and regenerates the element.

The run fails (exit 1, nothing saved) when the element or event does not exist. The message lists close
matches.

With --json, ""edit"" holds {element, name, handlerFile, handler}: the handler method left in handlerFile,
relative to the .csproj folder.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity with the event. Required."),
            new("name", "name", "Event to remove. Required."),
        },
        Examples = new[]
        {
            ("remove-event MyGame --element GameScreen --name PuckVsGoalCollided", "Remove GameScreen's PuckVsGoalCollided event."),
        },
        IsEdit = true,
        ValidateArguments = context => context.Has("element") && context.Has("name") ? null : "remove-event needs --element and --name.",
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record RemovedEvent(string Element, string Name, string HandlerFile, string Handler);

    static Task RunAsync(CliContext context)
    {
        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return Task.CompletedTask;
        }
        GlueState.Self.CurrentElement = element;

        var name = context.Get("name")!;
        var eventResponse = element.GetEvent(name) ??
            element.Events.FirstOrDefault(item => string.Equals(item.EventName, name, StringComparison.OrdinalIgnoreCase));
        if (eventResponse == null)
        {
            context.Fail($"{element.Name} has no event named '{name}'." + (element.Events.Count == 0
                ? " It has no events."
                : CliLookup.DescribeCloseMatches(name, element.Events.Select(item => item.EventName))));
            return Task.CompletedTask;
        }

        GlueCommands.Self.GluxCommands.ElementCommands.RemoveEventFromElement(element, eventResponse);

        var removed = new RemovedEvent(element.Name, eventResponse.EventName,
            EventResponseSave.GetCustomEventFileNameForElement(element).Replace('\\', '/'), "On" + eventResponse.EventName);
        context.Report(removed, $"Removed event {removed.Name} from {removed.Element}; its handler {removed.Handler} stays in {removed.HandlerFile}.");
        return Task.CompletedTask;
    }
}
