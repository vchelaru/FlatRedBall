using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-collision-relationship</c>: creates a collision relationship through the Collision Plugin's
/// <see cref="ICollisionRelationshipCommands.CreateAsync"/>, the create path its Collision tab uses (object order,
/// auto name, tile-collision defaults, codegen), then applies settings through
/// <see cref="ICollisionRelationshipCommands.SetValue"/> and an explicit name last, as a rename.
/// </summary>
public static class AddCollisionRelationshipCommand
{
    /// <summary>Option to the setting it sets, in the order they are applied.</summary>
    static readonly (string Option, string Setting)[] SettingOptions =
    {
        ("type", "CollisionType"),
        ("first-mass", "FirstCollisionMass"),
        ("second-mass", "SecondCollisionMass"),
        ("elasticity", "CollisionElasticity"),
        ("soft-coefficient", "SoftCollisionCoefficient"),
        ("apply-physics", "IsAutomaticallyApplyPhysicsChecked"),
        ("active", "IsCollisionActive"),
        ("collision-limit", "CollisionLimit"),
        ("list-vs-list", "ListVsListLoopingMode"),
        ("first-sub", FirstSub),
        ("second-sub", SecondSub),
    };

    const string FirstSub = "FirstSubCollisionSelectedItem";
    const string SecondSub = "SecondSubCollisionSelectedItem";

    public static readonly CliCommand Definition = new()
    {
        Name = "add-collision-relationship",
        Summary = "Add a collision relationship between two objects (lists, entities, ShapeCollections, TileShapeCollections) in a screen.",
        Usage = "add-collision-relationship <project> --element <element> --first <object> --second <object> [options]",
        Description =
@"Adds the relationship the way the Collision tab's Add button does: same name (PlayerBallVsGoal, numbered if
taken), same object order, same defaults, plugin reactions and code generation. Then applies the settings below
the way the Collision tab does, and saves and regenerates the element's code.

<object> is a top-level object in the element that can collide: an ICollidable entity instance, a list of
them, a ShapeCollection or a TileShapeCollection. A list can collide with itself. The relationship's first
object must be an entity or a list, so a ShapeCollection or TileShapeCollection passed as --first is moved to
second, as in the editor (--first-sub and --second-sub stay with their objects).

Defaults are the editor's: type none (bounce with elasticity 0 against a TileShapeCollection, platformer when
the first entity is a platformer), masses 1 (0 and 1 against a TileShapeCollection), elasticity 1, soft
coefficient 1, physics applied automatically, active, collision limit All, list vs list
PreventDoubleChecksPerFrame, <Entire Object> for each sub-collision, deal damage on.

The run fails (exit 1, nothing saved) when an object is unknown or cannot collide, neither object is an entity
or list, an entity instance would collide with itself, the name is taken or invalid, or a value is not valid.
The message lists close matches or valid values. The editor asks before adding a relationship to a screen that
derives from GameScreen, where relationships usually go; pass --in-derived-screen to add it there.

Other settings (damage, platformer movement values, the objects themselves) and later changes go through
set-collision-property.

With --json, ""edit"" holds {element, name, first, second, collisionType, sourceClassType, swapped, settings}:
swapped is true when --first and --second were swapped, settings every setting by the name
set-collision-property takes.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen (or entity) to add the relationship to. Required."),
            new("first", "object", "First object. Required."),
            new("second", "object", "Second object. Required."),
            new("name", "name", "Relationship name. Turns off auto-naming. Default: Glue's auto name (FirstVsSecond)."),
            new("type", "type", "Collision physics: " + CliCollision.CollisionTypeHelp + "."),
            new("first-mass", "number", "First object's mass for move and bounce. Default 1."),
            new("second-mass", "number", "Second object's mass for move and bounce. Default 1."),
            new("elasticity", "number", "Bounce elasticity. Default 1."),
            new("soft-coefficient", "number", "Soft collision coefficient (move-soft). Default 1."),
            new("apply-physics", "true|false", "Apply the physics automatically (false: only detect collision). Default true."),
            new("active", "true|false", "Whether the relationship is active when the screen starts. Default true."),
            new("collision-limit", "limit", "List vs list: All, First or Closest. Default All."),
            new("list-vs-list", "mode", "List vs list: PreventDoubleChecksPerFrame or AllowDoubleChecksPerFrame."),
            new("first-sub", "shape", "Collide only this shape of --first's entity. Default <Entire Object>."),
            new("second-sub", "shape", "Collide only this shape of --second's entity. Default <Entire Object>."),
            new("in-derived-screen", null, "Add it even though the element is a screen deriving from GameScreen. Default: fail."),
        },
        Examples = new[]
        {
            ("add-collision-relationship MyGame --element GameScreen --first PlayerList --second EnemyList", "Collide two lists; only detects collisions (type none)."),
            ("add-collision-relationship MyGame --element GameScreen --first PlayerList --second SolidCollision --type platformer --json", "Platformer collision against a TileShapeCollection, JSON output."),
            ("add-collision-relationship MyGame --element GameScreen --first BallList --second Walls --type bounce --first-mass 0 --elasticity 0.8", "Bounce balls off walls."),
            ("add-collision-relationship MyGame --element GameScreen --first BulletList --second EnemyList --first-sub CircleInstance --name BulletHits", "Collide only the bullet's circle, with a custom name."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedRelationship(string Element, string Name, string? First, string? Second, string? CollisionType,
        string SourceClassType, bool Swapped, IReadOnlyDictionary<string, object?> Settings);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "first", "second" })
        {
            if (!context.Has(required))
            {
                return $"add-collision-relationship needs --{required}.";
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

        var commands = CliCollision.GetCommands(context);
        if (commands == null)
        {
            return;
        }

        var first = FindCollidable(context, commands, element, "first");
        var second = first == null ? null : FindCollidable(context, commands, element, "second");
        if (first == null || second == null)
        {
            return;
        }

        // CreateAsync puts an entity or list first.
        var swapped = !IsEntityOrList(first);
        var effectiveFirst = swapped ? second : first;
        if (!IsEntityOrList(effectiveFirst))
        {
            context.Fail($"{first.InstanceName} and {second.InstanceName} cannot collide with each other: one of them must be an entity instance or a list.");
            return;
        }
        if (first == second && !first.IsList)
        {
            context.Fail($"{first.InstanceName} cannot collide with itself; only a list can.");
            return;
        }
        if (commands.IsUnusualContainer(element) && !context.Has("in-derived-screen"))
        {
            context.Fail($"{element.Name} derives from GameScreen, where collision relationships usually go. Pass --in-derived-screen to add it here anyway.");
            return;
        }

        // Every value is checked before anything changes: the create path saves the project itself.
        var settings = new List<(string Setting, object? Value)>();
        foreach (var (option, optionSetting) in SettingOptions)
        {
            if (!context.Has(option))
            {
                continue;
            }
            var settingName = optionSetting;
            IReadOnlyList<string>? options = null;
            if (settingName is FirstSub or SecondSub)
            {
                // The option names a shape of the object passed with it; the relationship's order may differ.
                var owner = settingName == FirstSub ? first : second;
                options = commands.GetSubCollisionOptions(owner);
                if (options == null)
                {
                    context.Fail($"{owner.InstanceName} is not an entity or a list of entities, so --{option} does not apply.");
                    return;
                }
                if (swapped)
                {
                    settingName = settingName == FirstSub ? SecondSub : FirstSub;
                }
            }
            var setting = CliCollision.FindSetting(commands, settingName)!;
            if (!CliCollision.TryConvert(setting, context.Get(option) ?? "", options, out var value, out var error))
            {
                context.Fail($"--{option}: {error}");
                return;
            }
            settings.Add((setting.Name, value));
        }

        var name = context.Get("name");
        if (name != null && !CheckName(context, element, name))
        {
            return;
        }

        var relationship = await commands.CreateAsync(element, first.InstanceName, second.InstanceName);
        await TaskManager.Self.WaitForAllTasksFinished();

        foreach (var (setting, value) in settings)
        {
            commands.SetValue(relationship, setting, value);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        if (name != null && name != relationship.InstanceName)
        {
            // A name the user typed turns auto-naming off in the editor too, or the next object change renames it back.
            commands.SetValue(relationship, "IsAutoNameEnabled", false);
            await TaskManager.Self.WaitForAllTasksFinished();
            await GlueCommands.Self.GluxCommands.RenameNamedObjectSave(relationship, name, performSaveAndGenerateCode: true, updateUi: false);
            await TaskManager.Self.WaitForAllTasksFinished();
        }

        var values = CliCollision.Values(commands, relationship);
        var added = new AddedRelationship(
            Element: element.Name,
            Name: relationship.InstanceName,
            First: values.GetValueOrDefault("FirstCollisionName") as string,
            Second: values.GetValueOrDefault("SecondCollisionName") as string,
            CollisionType: values.GetValueOrDefault("CollisionType") as string,
            SourceClassType: relationship.SourceClassType,
            Swapped: swapped,
            Settings: values);
        context.Report(added, Describe("Added", added.Name, added.Element, values) +
            (swapped ? Environment.NewLine + $"  {second.InstanceName} is first: the first object must be an entity or a list." : ""));
    }

    static bool IsEntityOrList(NamedObjectSave nos) => nos.SourceType == SourceType.Entity || nos.IsList;

    static NamedObjectSave? FindCollidable(CliContext context, ICollisionRelationshipCommands commands, GlueElement element, string option)
    {
        var objectName = context.Get(option)!;
        // The Collision tab offers top-level objects only.
        var objects = element.NamedObjects;
        var nos = objects.FirstOrDefault(item => item.InstanceName == objectName) ??
            objects.FirstOrDefault(item => string.Equals(item.InstanceName, objectName, StringComparison.OrdinalIgnoreCase));
        var collidable = objects.Where(commands.CanCollide).Select(item => item.InstanceName).ToList();
        if (nos == null)
        {
            context.Fail($"{element.Name} has no top-level object '{objectName}' (--{option})." + (collidable.Count == 0
                ? " It has no objects that can collide."
                : CliLookup.DescribeCloseMatches(objectName, collidable) + " Objects that can collide: " + string.Join(", ", collidable) + "."));
            return null;
        }
        if (!commands.CanCollide(nos))
        {
            context.Fail($"{nos.InstanceName} in {element.Name} cannot collide (--{option}): it must be an ICollidable entity, a list of them, " +
                "a ShapeCollection or a TileShapeCollection. An entity becomes collidable with set-property --property ImplementsICollidable --value true." +
                (collidable.Count == 0 ? "" : " Objects that can collide: " + string.Join(", ", collidable) + "."));
            return null;
        }
        return nos;
    }

    static bool CheckName(CliContext context, GlueElement element, string name)
    {
        // TryApplyAutoName's rule: base elements' objects and derived elements' own objects count too.
        var existing = element.GetAllNamedObjectsRecurisvely()
            .Concat(ObjectFinder.Self.GetAllElementsThatInheritFrom(element).SelectMany(derived => derived.AllNamedObjects.Where(item => !item.DefinedByBase)))
            .FirstOrDefault(item => item.InstanceName == name);
        if (existing != null)
        {
            context.Fail($"{element.Name} (or an element deriving from it) already has an object named {name}.");
            return false;
        }
        if (!Builder.Get<NameVerifier>().IsNamedObjectNameValid(name, null, out var whyNameIsInvalid))
        {
            context.Fail($"'{name}' is not a valid name in {element.Name}: {whyNameIsInvalid}");
            return false;
        }
        return true;
    }

    /// <summary>The text report: the relationship and the settings the Collision tab shows first.</summary>
    internal static string Describe(string verb, string name, string element, IReadOnlyDictionary<string, object?> values)
    {
        string Value(string key) => CliCollision.Describe(values.GetValueOrDefault(key));
        var lines = new List<string>
        {
            $"{verb} {name} ({Value("FirstCollisionName")} vs {Value("SecondCollisionName")}, {Value("CollisionType")}) in {element}.",
            $"  masses {Value("FirstCollisionMass")} / {Value("SecondCollisionMass")}, elasticity {Value("CollisionElasticity")}, " +
                $"apply physics {Value("IsAutomaticallyApplyPhysicsChecked")}, active {Value("IsCollisionActive")}",
            $"  sub-collisions {Value(FirstSub)} / {Value(SecondSub)}, collision limit {Value("CollisionLimit")}, " +
                $"auto name {Value("IsAutoNameEnabled")}",
        };
        return string.Join(Environment.NewLine, lines);
    }
}
