using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using FlatRedBall.Glue.SetVariable;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-variable-property</c>: sets one of a variable's own properties (Type, SetByDerived, Category, IsShared,
/// Scope, SourceObject, Name...) the way the Properties grid does when the variable is selected.
/// <see cref="CustomVariablePropertyModel"/>, which the grid renders, decides which properties exist and sets the
/// value; then the reactions <c>SetPropertyManager.ReactToPropertyChanged</c> runs for a variable follow:
/// <see cref="CustomVariableSaveSetPropertyLogic.ReactToCustomVariableChangedValue"/>, the plugins' changed-property
/// event, and code generation for the element and the elements using it.
/// </summary>
public static class SetVariablePropertyCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "set-variable-property",
        Summary = "Set a property of a screen's or entity's variable (Type, SetByDerived, Category, IsShared, Scope, Name, ...).",
        Usage = "set-variable-property <project> --element <element> --variable <name> --property <name> --value <text> [answers]",
        Description =
@"Sets the property the way the editor's Properties grid does with the variable selected: same stored value,
same follow-up changes (derived elements, instances, events), plugin reactions and code generation. Then saves
and regenerates the changed elements' code.

The properties are Name (which renames the variable and what refers to it), Type (only for a new variable,
not one that exposes or tunnels), SetByDerived, IsShared, DefinedByBase, Scope, Category, Summary,
CreatesEvent, CreatesProperty, HasAccompanyingVelocityProperty, PreferredDisplayerTypeName, SourceObject,
SourceObjectProperty (once SourceObject is set), OverridingPropertyType and TypeConverter (once
OverridingPropertyType is set). A DefinedByBase variable has no Scope, SourceObject, SourceObjectProperty or
OverridingPropertyType. Only the properties the grid shows for that variable can be set; the error lists
them. The default value is a variable value: use set-variable.

<text> is converted to the property's type: true/false, an enum value name (Public, Private, Protected,
Internal for Scope), or text. A property the grid picks from a list (Type, SourceObject, OverridingPropertyType)
must be one of the listed values.

Some changes make Glue ask something. Without the answering option the run fails (exit 1, changes not saved),
naming it:
  --change-derived-types   Type of a SetByDerived variable: also change the type of the derived elements'
                           copies.
  --remove-instance-values Scope to Private or Protected: remove the values objects using the element set for
                           this variable.

The run fails when the element, variable or property is unknown or read-only, the text does not convert, or
Glue undoes the change (an invalid or taken name, a Private SetByDerived variable). The message lists close
matches, the valid values, or what Glue said.

With --json, ""edit"" holds {element, variable, property, type, oldValue, newValue, answers, messages,
changedElements}: variable is the variable's name after the edit, answers lists [{question, answer}] for
every question Glue asked, messages what Glue reported, changedElements every screen and entity the edit
changed.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that defines the variable. Required."),
            new("variable", "name", "The variable. Required."),
            new("property", "name", "Property name, as the Properties grid lists it. Required."),
            new("value", "text", "New value as text. Required; pass \"\" for empty text."),
            new("change-derived-types", null, "Type: also change the derived elements' copies. Default: fail."),
            new("remove-instance-values", null, "Scope Private/Protected: remove values objects set for it. Default: fail."),
        },
        Examples = new[]
        {
            ("set-variable-property MyGame --element Entities\\Enemy --variable Speed --property SetByDerived --value false", "Stop derived entities from setting Speed."),
            ("set-variable-property MyGame --element Entities\\Enemy --variable Speed --property Type --value int --change-derived-types --json", "Change a variable's type, also in derived entities, JSON output."),
            ("set-variable-property MyGame --element Entities\\Enemy --variable Speed --property Category --value Movement", "List the variable under a category."),
            ("set-variable-property MyGame --element Entities\\Enemy --variable Speed --property Name --value MoveSpeed", "Rename the variable and what refers to it."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetVariableProperty(string Element, string Variable, string Property, string Type, object? OldValue,
        object? NewValue, IReadOnlyList<SetPropertyCommand.Answer> Answers, IReadOnlyList<string> Messages,
        IReadOnlyList<string> ChangedElements);

    /// <summary>A yes/no question the reaction asks, and the flag that answers yes. Unanswered, it undoes the edit.</summary>
    sealed record Question(string Option, string Prefix);

    static readonly Question[] Questions =
    {
        // CustomVariableSaveSetPropertyLogic.HandleChangeVariableType
        new("change-derived-types", "Changing this type will also change the type on the following variables."),
        // CustomVariableSaveSetPropertyLogic.HandleScopeSet
        new("remove-instance-values", "By setting the variable's scope to "),
    };

    /// <summary>
    /// The start of every message the reactions show. They say why Glue undid the edit, or add a note; the
    /// command reports them, and fails when the value did not stick.
    /// </summary>
    static readonly string[] ReactionMessages =
    {
        "There is already a variable that is modifying ",
        "Shared CSV variables are not assigned ",
        "The variable ",
    };

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "variable", "property", "value" })
        {
            if (!context.Has(required))
            {
                return $"set-variable-property needs --{required}.";
            }
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        var element = CliLookup.FindElement(context.Get("element")!, out var elementError);
        if (element == null)
        {
            context.Fail(elementError!);
            return;
        }
        GlueState.Self.CurrentElement = element;

        var variableName = context.Get("variable")!;
        var variable = element.CustomVariables.FirstOrDefault(item => item.Name == variableName) ??
            element.CustomVariables.FirstOrDefault(item => string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
        if (variable == null)
        {
            var inBase = element.GetCustomVariableRecursively(variableName);
            context.Fail(inBase != null
                ? $"{variableName} is defined by a base of {element.Name}, {ObjectFinder.Self.GetElementContaining(inBase)?.Name}; set its properties there."
                : $"{element.Name} has no variable '{variableName}'." + (element.CustomVariables.Count == 0
                    ? " It defines no variables."
                    : CliLookup.DescribeCloseMatches(variableName, element.CustomVariables.Select(item => item.Name))));
            return;
        }

        var target = $"{element.Name}.{variable.Name}";
        var propertyName = context.Get("property")!;
        if (string.Equals(propertyName, nameof(CustomVariable.DefaultValue), StringComparison.OrdinalIgnoreCase))
        {
            context.Fail($"DefaultValue is the variable's value. Use: set-variable <project> --element {element.Name} --variable {variable.Name} --value <text>");
            return;
        }

        // The grid shows the selected variable; the reactions read the selection too (Name, Scope).
        GlueState.Self.CurrentCustomVariable = variable;
        var members = Builder.Get<CustomVariablePropertyModel>().GetProperties(variable, element)
            .Where(item => item.Name != nameof(CustomVariable.DefaultValue)).ToList();
        var member = members.FirstOrDefault(item => item.Name == propertyName) ??
            members.FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        var settable = members.Where(item => !item.IsReadOnly && SetPropertyCommand.IsSupportedType(item.Type))
            .Select(item => item.Name).ToList();
        if (member == null)
        {
            var exists = typeof(CustomVariable).GetProperty(propertyName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            context.Fail((exists != null
                ? $"The editor does not show {exists.Name} for {target}, so set-variable-property does not set it there."
                : $"{target} has no property '{propertyName}'.") + CliLookup.DescribeCloseMatches(propertyName, settable));
            return;
        }
        var name = member.Name;
        if (member.IsReadOnly)
        {
            context.Fail($"{name} of {target} is read-only in the editor." + (name == nameof(CustomVariable.Type)
                ? $" Only a new variable's type changes; {variable.Name} " + (variable.GetIsTunneling() ? "tunnels into an object." : "exposes a built-in variable.")
                : ""));
            return;
        }
        if (!SetPropertyCommand.IsSupportedType(member.Type))
        {
            context.Fail($"{name} of {target} is a {SetPropertyCommand.TypeName(member.Type)}; set-variable-property only sets text, true/false, numbers and enum values.");
            return;
        }
        if (!SetPropertyCommand.TryConvert(context, member, context.Get("value")!, $"{name} of {target}", out var value))
        {
            return;
        }

        var oldValue = member.GetValue();
        var answers = new List<SetPropertyCommand.Answer>();
        var unanswered = new List<(Question question, string text)>();
        var before = CliChanges.Snapshot();
        var dialogs = new CliDialogs(
            answerConfirm: (text, _, _) =>
            {
                var question = Questions.FirstOrDefault(item => text.StartsWith(item.Prefix, StringComparison.Ordinal));
                if (question == null)
                {
                    return null;
                }
                var yes = context.Has(question.Option);
                lock (answers) { answers.Add(new SetPropertyCommand.Answer(SetPropertyCommand.FirstLine(text), yes ? $"yes (--{question.Option})" : "no")); }
                if (!yes)
                {
                    lock (unanswered) { unanswered.Add((question, text)); }
                }
                return yes ? DialogButton.Yes : DialogButton.No;
            },
            // A refused rename explains itself with NameVerifier's reason, which has no fixed start.
            takeMessage: text => name == nameof(CustomVariable.Name) ||
                ReactionMessages.Any(prefix => text.StartsWith(prefix, StringComparison.Ordinal)));
        using (dialogs)
        {
            member.SetValue(value);

            // What SetPropertyManager.ReactToPropertyChanged does for a selected variable.
            await Builder.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(name, variable, oldValue!);
            PluginManager.ReactToChangedProperty(name, oldValue, element, null!);
            GlueCommands.Self.GenerateCodeCommands.GenerateElementAndReferencedObjectCode(element);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var messages = dialogs.TakenMessages.ToList();
        if (unanswered.Count > 0)
        {
            context.Fail(string.Join(Environment.NewLine, unanswered.Select(item =>
                $"Setting {name} of {target} asks:" + Environment.NewLine +
                string.Join(Environment.NewLine, SetPropertyCommand.Lines(item.text).Select(line => "    " + line)) + Environment.NewLine +
                $"  Pass --{item.question.Option} to answer yes.")));
            return;
        }

        var newValue = member.GetValue();
        if (!SetPropertyCommand.SameValue(newValue, value))
        {
            context.Fail($"Glue did not set {name} of {target} to {SetPropertyCommand.Describe(value)}; it is {SetPropertyCommand.Describe(newValue)}." +
                (messages.Count == 0 ? "" : " Glue said: " + string.Join(" ", messages.Select(message => message.Trim()))));
            return;
        }

        var result = new SetVariableProperty(element.Name, variable.Name, name, SetPropertyCommand.TypeName(member.Type),
            oldValue, newValue, answers, messages, CliChanges.ChangedElements(before));
        var lines = new List<string>
        {
            $"Set {name} of {element.Name}.{variable.Name} ({result.Type}): {SetPropertyCommand.Describe(oldValue)} -> {SetPropertyCommand.Describe(newValue)}.",
        };
        lines.AddRange(answers.Select(answer => $"  asked: {answer.Question} -> {answer.Answered}"));
        lines.AddRange(messages.Select(message => $"  glue: {SetPropertyCommand.FirstLine(message)}"));
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
