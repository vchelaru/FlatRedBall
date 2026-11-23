using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-property</c>: sets a property (not a variable) of a screen, an entity, or an object in one, the way the
/// editor's Properties grid does. The grid's own displayer (<see cref="NamedObjectPropertyGridDisplayer"/>,
/// <see cref="EntitySavePropertyGridDisplayer"/>, <see cref="ScreenSavePropertyGridDisplayer"/>, plus the plugins'
/// additions) decides which properties exist for the target and sets the value, then the reaction the grid's
/// change event ends in runs: GluxCommands.ReactToPropertyChanged for an object, ElementCommands.ReactToPropertyChanged
/// for an element. Every question those reactions ask is answered from a flag (<see cref="Questions"/>).
/// </summary>
public static class SetPropertyCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "set-property",
        Summary = "Set a property of a screen, an entity, or an object in one (LayerOn, IsContainer, BaseEntity, ImplementsICollidable, ...).",
        Usage = "set-property <project> --element <element> [--object <name>] --property <name> --value <text> [answers]",
        Description =
@"Sets the property the way the editor's Properties grid does: same stored value, same follow-up changes,
plugin reactions and code generation. Then saves the .gluj and element files and regenerates the changed
elements' code. Properties are the object's or element's own settings; variables (X, Width, a variable the
element defines) go through set-variable.

With --object, sets a property of that object (at any depth): LayerOn, AttachToCamera, AttachToContainer,
IsContainer, ExposedInDerived, SetByDerived, IncludeInIVisible, IncludeInIClickable, IncludeInICollidable,
CallActivity, IgnoresPausing, HasPublicProperty, IsDisabled, SourceClassType, SourceFile, SourceName,
CurrentState and others. Without --object, sets a property of the screen or entity: BaseEntity, BaseScreen,
NextScreen, ImplementsICollidable, ImplementsIVisible, ImplementsIClickable, CreatedByOtherEntities,
PooledByFactory, UseGlobalContent, plugin properties such as ImplementsIDamageable, and others.

Only properties the editor shows for that target can be set: AttachToCamera exists for objects in a screen,
LayerOn not on a list or a Layer. The error lists the ones that can. Name and InstanceName belong to rename.

<text> is converted to the property's type: true/false, an enum value name, a number, or text. A property
the editor picks from a list (LayerOn, BaseEntity, SourceClassType, SourceFile) must be one of the listed
values; <NONE> clears it.

Some changes make Glue ask something. Each question has an option below; without it the run takes the
answer that changes the least, and fails (exit 1, nothing saved) when that answer would undo the edit or
lose data, naming the option to pass.

The run fails when the element, object or property is unknown or read-only, the text does not convert, or
Glue undoes the change (for example a second IsContainer object). The message lists close matches, the
valid values, or what Glue said.

With --json, ""edit"" holds {element, object, property, type, oldValue, newValue, answers, messages,
changedElements}: object is null for an element property; answers lists [{question, answer}] for every
question Glue asked; messages lists what Glue reported while reacting (""IWindows must also be IVisible...""),
changedElements every screen and entity the edit changed.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the property, or holds the object. Required."),
            new("object", "name", "Object in the element whose property to set. Default: the element's own property."),
            new("property", "name", "Property name, as the Properties grid lists it (without spaces). Required."),
            new("value", "text", "New value as text. Required; pass \"\" for empty text."),
            new("apply-to-list", null, "LayerOn or AttachToCamera on a list: also set it on every object in the list.\nDefault: only the list."),
            new("container-base", "set|ignore", "IsContainer when the object's type is not the entity's base: set makes the\nentity inherit from the object's type, ignore leaves it (may not compile). Default: fail."),
            new("ignore-missing-variables", null, "SourceClassType: switch even though the new entity lacks variables this object\nsets. Default: fail."),
            new("base-object-type", "update|ignore", "SourceName when the base element's object of that name is untyped: update gives\nit this object's type, ignore leaves it. Default: fail."),
            new("derived-objects", "remove|keep", "SetByDerived false while derived elements have the object: remove them, or keep\nthem as their own objects. Default: fail."),
            new("reset-variables", null, "PooledByFactory: add reset variables for every object (turning it on) or remove\nthem (turning it off). Default: leave them."),
            new("visible-variable", "remove|keep", "ImplementsIVisible false while the entity exposes a Visible variable: remove it,\nor keep it as a plain variable. Default: fail."),
            new("non-visible-objects", "make|ignore", "ImplementsIVisible true while an object's entity is not IVisible: make that\nentity IVisible, or leave it. Default: fail."),
            new("lost-variables", "keep|drop", "BaseEntity: variables the new base does not have are kept as the entity's own\nvariables, or dropped. Default: fail."),
            new("orphaned-objects", "remove|keep", "BaseEntity/BaseScreen: objects marked defined-by-base that no base has are\nremoved, or kept as the element's own. Default: fail."),
            new("referenced-global", "set|ignore", "UseGlobalContent true: also make every entity its objects use load globally, or\nleave them (may fail at runtime). Default: fail."),
        },
        Examples = new[]
        {
            ("set-property MyGame --element GameScreen --object Player1 --property LayerOn --value HudLayer", "Put an object on a Layer in the same screen."),
            ("set-property MyGame --element Entities\\Enemy --property ImplementsICollidable --value true --json", "Make an entity ICollidable, JSON output."),
            ("set-property MyGame --element Entities\\Enemy --object SpriteInstance --property IsContainer --value true --container-base ignore", "Mark the entity's container object, leaving its base type alone."),
            ("set-property MyGame --element Entities\\Boss --property BaseEntity --value Entities\\Enemy --lost-variables keep", "Change an entity's base, keeping variables the new base lacks."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetProperty(string Element, string? Object, string Property, string Type, object? OldValue, object? NewValue,
        IReadOnlyList<Answer> Answers, IReadOnlyList<string> Messages, IReadOnlyList<string> ChangedElements);

    public sealed record Answer(string Question, string Answered);

    /// <summary>
    /// One question a property reaction can ask, the option that answers it, and the label (or button) each
    /// value picks. <see cref="Unanswered"/> is what the run answers without the option; when
    /// <see cref="FailsUnanswered"/>, the run then fails naming the option, since that answer undoes the edit or
    /// drops data and nothing gets saved either way.
    /// </summary>
    sealed record Question(string Option, Func<string, bool> Matches, IReadOnlyDictionary<string, string> Choices,
        string Unanswered, bool FailsUnanswered);

    const string Yes = "Yes";
    const string No = "No";

    static readonly Question[] Questions =
    {
        // NamedObjectSetVariableLogic, LayerOn and AttachToCamera on a list
        new("apply-to-list", text => text.StartsWith("Do you want to remove every object in the List ", StringComparison.Ordinal) ||
                text.StartsWith("Do you want to add every object contained in the List ", StringComparison.Ordinal) ||
                text.StartsWith("Do you want to attach every object contained in the list ", StringComparison.Ordinal) ||
                text.StartsWith("Do you want to detach every object contained in the list ", StringComparison.Ordinal),
            Flag(), No, FailsUnanswered: false),
        // NamedObjectSetVariableLogic.HandleChangedIsContainer
        new("container-base", text => text.StartsWith("The object is of type ", StringComparison.Ordinal) && text.Contains(" but the container is of type "),
            new Dictionary<string, string> { ["set"] = "Set the container's type to ", ["ignore"] = "Nothing - " },
            "Set 'IsContainer' back to false", FailsUnanswered: true),
        // NamedObjectSetVariableLogic.ShowPopupsForMissingVariablesInNewType
        new("ignore-missing-variables", text => text.Contains("\nDo you want to change to ") && text.EndsWith(" anyway?", StringComparison.Ordinal),
            Flag(), No, FailsUnanswered: true),
        // NamedObjectSetVariableLogic.ReactToChangedNosSourceName
        new("base-object-type", text => text.StartsWith("This object has type of ", StringComparison.Ordinal) && text.Contains(" is untyped."),
            new Dictionary<string, string> { ["update"] = "Change ", ["ignore"] = "Do nothing" },
            "Do nothing", FailsUnanswered: true),
        // SetByDerivedSetLogic.ReactToChangedSetByDerived
        new("derived-objects", text => text.StartsWith("Changing SetByDerived will result in the following ", StringComparison.Ordinal),
            new Dictionary<string, string> { ["remove"] = "Remove ", ["keep"] = "Keep " },
            "Cancel", FailsUnanswered: true),
        // EntitySaveSetPropertyLogic, PooledByFactory
        new("reset-variables", text => text.StartsWith("Would you like to add reset variables for all contained objects", StringComparison.Ordinal) ||
                text.StartsWith("Would you like to remove reset variables for all contained objects", StringComparison.Ordinal),
            Flag(), No, FailsUnanswered: false),
        // EntitySaveSetPropertyLogic.ReactToChangedImplementsIVisible
        new("visible-variable", text => text.StartsWith("This entity has a \"Visible\" variable exposed.", StringComparison.Ordinal),
            new Dictionary<string, string> { ["remove"] = "Remove this variable", ["keep"] = "Keep this as a non-functional Variable" },
            "Keep this as a non-functional Variable", FailsUnanswered: true),
        new("non-visible-objects", text => text.Contains(" implements IVisible, but its object "),
            new Dictionary<string, string> { ["make"] = "Make ", ["ignore"] = "Ignore " },
            "Do nothing", FailsUnanswered: true),
        // InheritanceManager.ReactToChangedBaseEntity
        new("lost-variables", text => text.StartsWith("The variable\n\n", StringComparison.Ordinal) && text.Contains("Is no longer part of the Entity."),
            new Dictionary<string, string> { ["keep"] = Yes, ["drop"] = No }, No, FailsUnanswered: true),
        // InheritanceManager.UpdateFromBaseType
        new("orphaned-objects", text => text.StartsWith("The following object is marked as \"defined by base\" but not contained in", StringComparison.Ordinal),
            new Dictionary<string, string> { ["remove"] = Yes, ["keep"] = No }, No, FailsUnanswered: true),
    };

    /// <summary>
    /// The start of every message the property reactions show. They describe a follow-up change (IWindow turns
    /// on IVisible) or why Glue undid the edit; the command reports them, and fails when the value did not stick.
    /// </summary>
    static readonly string[] ReactionMessages =
    {
        "You have set ExposedInDerived to true, but SetByDerived is also true.",
        "You have set SetByDerived to true, but ExposedInDerived is also true.",
        "This list is not empty, so it can't be set to \"Set By Derived\".",
        "This object comes from a file.  Files which are part of Screens",
        "Setting an odd value to the DestinationRectangle's Y",
        "The object ",
        "This object is of type ",
        "The Entity ",
        "IWindows must also be IVisible.",
        "The item type ",
        "Cannot set ",
        "There is a duplicate named object:",
        "There is a file and object both named:",
        "There are two files named:",
        "This assignment has created an inheritence cycle",
    };

    static Dictionary<string, string> Flag() => new() { [""] = Yes };

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "property", "value" })
        {
            if (!context.Has(required))
            {
                return $"set-property needs --{required}.";
            }
        }
        foreach (var question in Questions.Where(item => item.Choices.Count > 1 || !item.Choices.ContainsKey("")))
        {
            var value = context.Get(question.Option);
            if (context.Has(question.Option) && (value == null || !question.Choices.ContainsKey(value)))
            {
                return $"--{question.Option} takes {string.Join(" or ", question.Choices.Keys)}.";
            }
        }
        var referencedGlobal = context.Get("referenced-global");
        if (context.Has("referenced-global") && referencedGlobal is not ("set" or "ignore"))
        {
            return "--referenced-global takes set or ignore.";
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

        NamedObjectSave? nos = null;
        if (context.Has("object"))
        {
            var objectName = context.Get("object")!;
            var allObjects = element.AllNamedObjects.ToList();
            nos = allObjects.FirstOrDefault(item => item.InstanceName == objectName) ??
                allObjects.FirstOrDefault(item => string.Equals(item.InstanceName, objectName, StringComparison.OrdinalIgnoreCase));
            if (nos == null)
            {
                context.Fail($"{element.Name} has no object '{objectName}'." +
                    (allObjects.Count == 0 ? " It has no objects." : CliLookup.DescribeCloseMatches(objectName, allObjects.Select(item => item.InstanceName))));
                return;
            }
        }

        var propertyName = context.Get("property")!;
        var target = nos != null ? $"{nos.InstanceName} in {element.Name}" : element.Name;
        if (string.Equals(propertyName, nameof(NamedObjectSave.InstanceName), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyName, "Name", StringComparison.OrdinalIgnoreCase))
        {
            var renameArgs = nos != null ? $"--element {element.Name} --object {nos.InstanceName}" : $"--element {element.Name}";
            context.Fail($"set-property does not rename. Use: rename <project> {renameArgs} --to <new name>");
            return;
        }

        // The grid shows the selected object; the reactions also read the selection (SourceName, SetByContainer).
        if (nos != null)
        {
            GlueState.Self.CurrentNamedObjectSave = nos;
        }
        var descriptors = GetGridProperties(element, nos);
        var descriptor = descriptors.FirstOrDefault(item => item.Name == propertyName) ??
            descriptors.FirstOrDefault(item => string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        var settable = descriptors.Where(item => !item.IsReadOnly && IsSupportedType(item.PropertyType)).Select(item => item.Name).ToList();
        if (descriptor == null)
        {
            FailUnknown(context, element, nos, target, propertyName, settable);
            return;
        }
        var name = descriptor.Name;
        if (descriptor.IsReadOnly)
        {
            context.Fail($"{name} of {target} is read-only in the editor.");
            return;
        }
        if (!IsSupportedType(descriptor.PropertyType))
        {
            context.Fail($"{name} of {target} is a {TypeName(descriptor.PropertyType)}; set-property only sets text, true/false, numbers and enum values.");
            return;
        }
        if (!TryConvert(context, descriptor, context.Get("value")!, $"{name} of {target}", out var value))
        {
            return;
        }

        var oldValue = descriptor.GetValue(null);
        var answers = new List<Answer>();
        var unanswered = new List<(Question question, string text)>();
        var referencedToMakeGlobal = new List<GlueElement>();
        if (nos == null && name == nameof(GlueElement.UseGlobalContent) && value is true && !element.UseGlobalContent)
        {
            // ElementPropertyGridDisplayer asks this in a WPF list window after the member changes; ask it here instead.
            CollectReferencedEntities(element, referencedToMakeGlobal);
            if (referencedToMakeGlobal.Count > 0 && !context.Has("referenced-global"))
            {
                context.Fail($"{element.Name} uses entities that would then load with a global content manager: " +
                    string.Join(", ", referencedToMakeGlobal.Select(item => item.Name)) +
                    ". Pass --referenced-global set to make them global too, or --referenced-global ignore to leave them (may fail at runtime).");
                return;
            }
        }

        var before = SerializeElements();
        var dialogs = new CliDialogs(
            answerConfirm: (text, _, _) =>
            {
                var question = Questions.FirstOrDefault(item => item.Matches(text));
                if (question == null)
                {
                    return null;
                }
                var label = Choose(context, question, text, answers, unanswered);
                return label == Yes ? DialogButton.Yes : DialogButton.No;
            },
            answerChoice: (text, options) =>
            {
                var question = Questions.FirstOrDefault(item => item.Matches(text));
                if (question == null)
                {
                    return null;
                }
                var label = Choose(context, question, text, answers, unanswered);
                // An unknown label leaves the question to the runner, which reports it and fails the run.
                return options.FirstOrDefault(option => option.label.StartsWith(label, StringComparison.Ordinal)).value;
            },
            takeMessage: text => ReactionMessages.Any(prefix => text.StartsWith(prefix, StringComparison.Ordinal)));
        using (dialogs)
        {
            if (nos == null && name == nameof(GlueElement.UseGlobalContent))
            {
                // Skips the grid's after-change handler, whose window is answered above.
                element.UseGlobalContent = (bool)value!;
                if (context.Get("referenced-global") == "set")
                {
                    foreach (var referenced in referencedToMakeGlobal)
                    {
                        referenced.UseGlobalContent = true;
                    }
                }
            }
            else
            {
                descriptor.SetValue(null, value);
            }

            // What SetPropertyManager.ReactToPropertyChanged does after the grid's change event.
            if (nos != null)
            {
                await GlueCommands.Self.GluxCommands.ReactToPropertyChanged(nos, name, oldValue!, performSaveAndGenerateCode: false, updateUi: false);
                PluginManager.ReactToChangedProperty(name, oldValue, element, null!);
            }
            else
            {
                await GlueCommands.Self.GluxCommands.ElementCommands.ReactToPropertyChanged(element, name, oldValue!);
                if (element is ScreenSave)
                {
                    // The entity reaction notifies plugins itself.
                    PluginManager.ReactToChangedProperty(name, oldValue, element, null!);
                }
            }
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var messages = dialogs.TakenMessages.ToList();
        if (unanswered.Count > 0)
        {
            context.Fail(string.Join(Environment.NewLine, unanswered.Select(item =>
                $"Setting {name} of {target} asks:" + Environment.NewLine +
                string.Join(Environment.NewLine, Lines(item.text).Select(line => "    " + line)) + Environment.NewLine +
                $"  Pass --{item.question.Option}" +
                (item.question.Choices.ContainsKey("") ? "" : " " + string.Join(" or ", item.question.Choices.Keys)) + " to answer it.")));
            return;
        }

        var newValue = descriptor.GetValue(null);
        if (!SameValue(newValue, value))
        {
            context.Fail($"Glue did not set {name} of {target} to {Describe(value)}; it is {Describe(newValue)}." +
                (messages.Count == 0 ? "" : " Glue said: " + string.Join(" ", messages.Select(FirstLine))));
            return;
        }

        // The grid's codegen after a change; elements that only reference this one (a screen with its entity's
        // objects) are not in the runner's changed-element regeneration.
        GlueCommands.Self.GenerateCodeCommands.GenerateElementAndReferencedObjectCode(element);
        await TaskManager.Self.WaitForAllTasksFinished();

        Report(context, new SetProperty(element.Name, nos?.InstanceName, name, TypeName(descriptor.PropertyType), oldValue, newValue,
            answers, messages, ChangedElements(before)));
    }

    /// <summary>The label <paramref name="question"/>'s option picks, or its unanswered default.</summary>
    static string Choose(CliContext context, Question question, string text, List<Answer> answers, List<(Question, string)> unanswered)
    {
        string label;
        string answered;
        if (context.Has(question.Option))
        {
            var key = context.Get(question.Option) ?? "";
            label = question.Choices[key];
            answered = key.Length == 0 ? $"yes (--{question.Option})" : $"{key} (--{question.Option})";
        }
        else
        {
            label = question.Unanswered;
            answered = "no";
            if (question.FailsUnanswered)
            {
                lock (unanswered) { unanswered.Add((question, text)); }
            }
        }
        lock (answers) { answers.Add(new Answer(FirstLine(text), answered)); }
        return label;
    }

    /// <summary>
    /// The properties the editor's Properties grid shows for <paramref name="nos"/> (or the element), built by the
    /// grid's own displayer and the plugins that add to it. Each descriptor sets its member as the grid does.
    /// </summary>
    static List<PropertyDescriptor> GetGridProperties(GlueElement element, NamedObjectSave? nos)
    {
        PropertyGridDisplayer displayer;
        if (nos != null)
        {
            var nosDisplayer = new NamedObjectPropertyGridDisplayer();
            nosDisplayer.UpdateToState(GlueState.Self);
            PluginManager.AdjustDisplayedNamedObject(nos, nosDisplayer);
            displayer = nosDisplayer;
        }
        else if (element is EntitySave entity)
        {
            var entityDisplayer = new EntitySavePropertyGridDisplayer { Instance = entity };
            PluginManager.AdjustDisplayedEntity(entity, entityDisplayer);
            displayer = entityDisplayer;
        }
        else
        {
            var screen = (ScreenSave)element;
            var screenDisplayer = new ScreenSavePropertyGridDisplayer { Instance = screen };
            PluginManager.AdjustDisplayedScreen(screen, screenDisplayer);
            displayer = screenDisplayer;
        }
        displayer.RefreshOnTimer = false;
        return ((ICustomTypeDescriptor)displayer).GetProperties(Array.Empty<Attribute>()).Cast<PropertyDescriptor>().ToList();
    }

    static void FailUnknown(CliContext context, GlueElement element, NamedObjectSave? nos, string target, string propertyName,
        List<string> settable)
    {
        var ownerType = nos != null ? typeof(NamedObjectSave) : element.GetType();
        var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property != null)
        {
            context.Fail($"The editor does not show {property.Name} for {target}, so set-property does not set it there." +
                CliLookup.DescribeCloseMatches(propertyName, settable));
            return;
        }
        if (IsVariable(element, nos, propertyName))
        {
            var objectArgs = nos != null ? $" --object {nos.InstanceName}" : "";
            context.Fail($"'{propertyName}' is a variable of {target}, not a property. Use: set-variable <project> --element {element.Name}{objectArgs} --variable {propertyName} --value <text>");
            return;
        }
        context.Fail($"{target} has no property '{propertyName}'." + CliLookup.DescribeCloseMatches(propertyName, settable));
    }

    static bool IsVariable(GlueElement element, NamedObjectSave? nos, string name)
    {
        IEnumerable<string> names;
        if (nos == null)
        {
            names = element.CustomVariables.Select(item => item.Name);
        }
        else if (ObjectFinder.Self.GetEntitySave(nos) is { } entity)
        {
            names = new[] { (GlueElement)entity }.Concat(ObjectFinder.Self.GetAllBaseElementsRecursively(entity))
                .SelectMany(item => item.CustomVariables).Select(item => item.Name);
        }
        else
        {
            names = nos.GetAssetTypeInfo()?.VariableDefinitions.Select(item => item.Name) ?? Enumerable.Empty<string>();
        }
        return names.Any(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
    }

    static bool IsSupportedType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type == typeof(bool) || type.IsEnum || type == typeof(int) || type == typeof(float) ||
            type == typeof(double) || type == typeof(long) || type == typeof(decimal);
    }

    /// <summary>
    /// <paramref name="text"/> as the descriptor's type. Text the grid picks from a list (its converter's exclusive
    /// standard values) must be one of them, matched case-insensitively and returned as the list spells it.
    /// </summary>
    static bool TryConvert(CliContext context, PropertyDescriptor descriptor, string text, string target, out object? value)
    {
        value = text;
        var nullable = Nullable.GetUnderlyingType(descriptor.PropertyType);
        var type = nullable ?? descriptor.PropertyType;
        if (nullable != null && text.Length == 0)
        {
            value = null;
            return true;
        }

        if (type == typeof(string))
        {
            var allowed = StandardValues(descriptor);
            if (allowed == null || text.Length == 0)
            {
                return true;
            }
            var match = allowed.FirstOrDefault(item => item == text) ??
                allowed.FirstOrDefault(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                // Element names are listed with their folder (Entities\Enemy); accept the bare name when it is unique.
                var byName = allowed.Where(item => string.Equals(CliLookup.StripFolders(item), text, StringComparison.OrdinalIgnoreCase)).ToList();
                match = byName.Count == 1 ? byName[0] : null;
            }
            if (match == null)
            {
                context.Fail($"'{text}' is not a valid value for {target}." + (allowed.Count <= 40
                    ? " Valid values: " + string.Join(", ", allowed) + "."
                    : CliLookup.DescribeCloseMatches(text, allowed)));
                return false;
            }
            value = match;
            return true;
        }
        if (type == typeof(bool))
        {
            if (bool.TryParse(text, out var asBool))
            {
                value = asBool;
                return true;
            }
            context.Fail($"'{text}' is not a valid bool for {target}. Valid values: true, false.");
            return false;
        }
        if (type.IsEnum)
        {
            var enumName = Enum.GetNames(type).FirstOrDefault(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase));
            if (enumName == null)
            {
                context.Fail($"'{text}' is not a valid {type.Name} for {target}. Valid values: {string.Join(", ", Enum.GetNames(type))}.");
                return false;
            }
            value = Enum.Parse(type, enumName);
            return true;
        }
        try
        {
            value = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or OverflowException)
        {
            context.Fail($"'{text}' is not a valid {TypeName(type)} for {target}.");
            return false;
        }
    }

    static List<string>? StandardValues(PropertyDescriptor descriptor)
    {
        try
        {
            var converter = descriptor.Converter;
            if (converter == null || !converter.GetStandardValuesSupported() || !converter.GetStandardValuesExclusive())
            {
                return null;
            }
            return converter.GetStandardValues()?.Cast<object>().Select(item => item?.ToString() ?? "").ToList();
        }
        catch (Exception)
        {
            // A converter that needs the grid's context: leave the text as typed, as the grid's text box does.
            return null;
        }
    }

    /// <summary>ElementPropertyGridDisplayer.GetAllElementsReferencedThroughObjectsRecursively.</summary>
    static void CollectReferencedEntities(GlueElement element, List<GlueElement> found)
    {
        foreach (var nos in element.NamedObjects.Where(item => item.SourceType == SourceType.Entity))
        {
            var referenced = ObjectFinder.Self.GetElement(nos.SourceClassType);
            if (referenced != null && !found.Contains(referenced))
            {
                found.Add(referenced);
                CollectReferencedEntities(referenced, found);
            }
        }
    }

    /// <summary>The setters map &lt;NONE&gt; to empty text (LayerOn, BaseEntity), so both count as the value asked for.</summary>
    static bool SameValue(object? actual, object? requested)
    {
        static object? Normalize(object? value) => value is "<NONE>" or "" ? null : value;
        return Equals(Normalize(actual), Normalize(requested));
    }

    static string TypeName(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
        {
            return TypeName(nullable) + "?";
        }
        return type == typeof(string) ? "string" : type == typeof(bool) ? "bool" : type == typeof(int) ? "int" :
            type == typeof(float) ? "float" : type == typeof(double) ? "double" : type.Name;
    }

    static IEnumerable<string> Lines(string text) => text.Split('\n').Select(item => item.Trim()).Where(item => item.Length > 0);

    static string FirstLine(string text) => Lines(text).FirstOrDefault() ?? "";

    static Dictionary<GlueElement, string> SerializeElements()
    {
        var project = GlueState.Self.CurrentGlueProject;
        return project.Screens.Cast<GlueElement>().Concat(project.Entities)
            .ToDictionary(element => element, element => Newtonsoft.Json.JsonConvert.SerializeObject(element));
    }

    static List<string> ChangedElements(Dictionary<GlueElement, string> before) =>
        SerializeElements()
            .Where(pair => !before.TryGetValue(pair.Key, out var json) || json != pair.Value)
            .Select(pair => pair.Key.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    static string Describe(object? value) => value switch
    {
        null => "(none)",
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    static void Report(CliContext context, SetProperty result)
    {
        var target = result.Object != null ? $"{result.Object}.{result.Property} in {result.Element}" : $"{result.Element}.{result.Property}";
        var lines = new List<string> { $"Set {target} ({result.Type}): {Describe(result.OldValue)} -> {Describe(result.NewValue)}." };
        lines.AddRange(result.Answers.Select(answer => $"  asked: {answer.Question} -> {answer.Answered}"));
        lines.AddRange(result.Messages.Select(message => $"  glue: {FirstLine(message)}"));
        if (result.ChangedElements.Count > 0)
        {
            lines.Add("  changed elements: " + string.Join(", ", result.ChangedElements));
        }
        context.Report(result, string.Join(Environment.NewLine, lines));
    }
}
