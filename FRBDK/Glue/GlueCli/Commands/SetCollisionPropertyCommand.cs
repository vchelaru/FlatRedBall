using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GlueCli.Commands;

/// <summary>
/// <c>set-collision-property</c>: sets one setting of a collision relationship through the Collision Plugin's
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.ICollisionRelationshipCommands.SetValue"/>,
/// which its Collision tab shares. The settings are not on the Properties grid, so <c>set-property</c> does not reach them.
/// </summary>
public static class SetCollisionPropertyCommand
{
    const string AlwaysColliding = "<Always Colliding>";

    public static readonly CliCommand Definition = new()
    {
        Name = "set-collision-property",
        Summary = "Set a collision relationship's setting (CollisionType, masses, sub-collisions, damage, its objects, ...).",
        Usage = "set-collision-property <project> --element <element> --relationship <name> --property <name> --value <text>",
        Description =
@"Sets the value the way the relationship's Collision tab does: same stored value, same follow-up changes
(auto-naming, TileShapeCollection masses), plugin reactions and code generation. Then saves and regenerates.

<name> is a setting as add-collision-relationship's --json reports it under settings: CollisionType,
FirstCollisionMass, SecondCollisionMass, CollisionElasticity, SoftCollisionCoefficient,
IsAutomaticallyApplyPhysicsChecked, IsCollisionActive, CollisionLimit, ListVsListLoopingMode,
FirstCollisionName, SecondCollisionName (the objects), FirstSubCollisionSelectedItem,
SecondSubCollisionSelectedItem, IsAutoNameEnabled, IsDealDamageChecked, IsDestroyFirstOnDamageChecked,
FirstCollisionDestroyType, IsDestroySecondOnDamageChecked, SecondCollisionDestroyType,
GroundPlatformerVariableName, AirPlatformerVariableName, AfterDoubleJumpPlatformerVariableName, and others.

<text> is true/false, a number, an enum value (CollisionType also takes " + CliCollision.CollisionTypeHelp + @"),
or text. A setting the tab picks from a dropdown (the objects, sub-collisions, platformer values) must be one
of its values. Changing the objects or sub-collisions renames the relationship while IsAutoNameEnabled is true.

The run fails (exit 1, nothing saved) when the element, relationship or setting is unknown, or the value is not
valid. The message lists close matches or valid values.

With --json, ""edit"" holds {element, relationship, property, oldValue, newValue, name}: name is the
relationship's name afterwards.",
        Options = new CliOption[]
        {
            new("element", "element", "Screen or entity that has the relationship. Required."),
            new("relationship", "name", "The collision relationship. Required."),
            new("property", "name", "Setting name. Required."),
            new("value", "text", "New value. Required."),
        },
        Examples = new[]
        {
            ("set-collision-property MyGame --element GameScreen --relationship PlayerVsSolidCollision --property CollisionType --value platformer", "Make an existing relationship platformer collision."),
            ("set-collision-property MyGame --element GameScreen --relationship BallVsWalls --property CollisionElasticity --value 0.5 --json", "Change bounce elasticity, JSON output."),
            ("set-collision-property MyGame --element GameScreen --relationship BulletVsEnemy --property IsDestroyFirstOnDamageChecked --value true", "Destroy the bullet when it deals damage."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record SetCollisionProperty(string Element, string Relationship, string Property, object? OldValue, object? NewValue, string Name);

    static string? ValidateArguments(CliContext context)
    {
        foreach (var required in new[] { "element", "relationship", "property", "value" })
        {
            if (!context.Has(required))
            {
                return $"set-collision-property needs --{required}.";
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

        var relationshipName = context.Get("relationship")!;
        var relationships = element.AllNamedObjects.Where(commands.IsCollisionRelationship).ToList();
        var relationship = relationships.FirstOrDefault(item => item.InstanceName == relationshipName) ??
            relationships.FirstOrDefault(item => string.Equals(item.InstanceName, relationshipName, StringComparison.OrdinalIgnoreCase));
        if (relationship == null)
        {
            var isObject = element.AllNamedObjects.Any(item => string.Equals(item.InstanceName, relationshipName, StringComparison.OrdinalIgnoreCase));
            context.Fail((isObject
                    ? $"{relationshipName} in {element.Name} is not a collision relationship."
                    : $"{element.Name} has no collision relationship '{relationshipName}'.") +
                (relationships.Count == 0 ? " It has none." : CliLookup.DescribeCloseMatches(relationshipName, relationships.Select(item => item.InstanceName))));
            return;
        }
        if (relationship.DefinedByBase)
        {
            context.Fail($"{relationship.InstanceName} in {element.Name} comes from its base {element.BaseElement}; change it there.");
            return;
        }

        var propertyArgument = context.Get("property")!;
        var setting = CliCollision.FindSetting(commands, propertyArgument);
        if (setting == null)
        {
            var names = commands.Settings.Select(item => item.Name).ToList();
            context.Fail($"Collision relationships have no setting '{propertyArgument}'." +
                CliLookup.DescribeCloseMatches(propertyArgument, names) +
                " Settings: " + string.Join(", ", names.OrderBy(item => item, StringComparer.Ordinal)) + ".");
            return;
        }
        var property = setting.Name;

        var options = commands.GetOptions(relationship, property);
        if (!CliCollision.TryConvert(setting, context.Get("value") ?? "", options, out var value, out var error))
        {
            context.Fail(error!);
            return;
        }

        var originalName = relationship.InstanceName;
        var oldValue = CliCollision.ForReport(commands.GetValue(relationship, property));
        commands.SetValue(relationship, property, value);
        await TaskManager.Self.WaitForAllTasksFinished();

        var values = CliCollision.Values(commands, relationship);
        var newValue = values.GetValueOrDefault(property);
        // SecondCollisionName stores <Always Colliding> as no object.
        var requested = value as string == AlwaysColliding ? null : CliCollision.ForReport(value);
        if (!Equals(newValue, requested))
        {
            context.Fail($"Glue did not set {property} of {relationship.InstanceName} to {CliCollision.Describe(requested)}; it is {CliCollision.Describe(newValue)}.");
            return;
        }

        var result = new SetCollisionProperty(element.Name, originalName, property, oldValue, newValue, relationship.InstanceName);
        context.Report(result, $"Set {property} of {originalName} in {element.Name}: {CliCollision.Describe(oldValue)} -> {CliCollision.Describe(newValue)}." +
            (relationship.InstanceName != originalName ? $" Renamed to {relationship.InstanceName}." : "") +
            Environment.NewLine + AddCollisionRelationshipCommand.Describe("Now", relationship.InstanceName, element.Name, values));
    }
}
