using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Services;
using GlueFormsCore.ViewModels;

namespace GlueCli.Commands;

/// <summary>
/// <c>add-entity</c>: adds an entity through
/// <see cref="FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.IElementCommands.AddEntityAsync"/>,
/// the call the editor's Add Entity dialog ends in, with an <see cref="AddEntityViewModel"/> set the way the
/// dialog's checkboxes set it. Platformer and top-down movement, which the Entity Input Movement plugin adds to
/// the dialog, go through the plugin's <c>MakeEntityPlatformer</c>/<c>MakeEntityTopDown</c>, as the new
/// project wizard does.
/// </summary>
public static class AddEntityCommand
{
    public static readonly CliCommand Definition = new()
    {
        Name = "add-entity",
        Summary = "Add an entity, with the Add Entity dialog's options (shapes, ICollidable, factory, lists, movement).",
        Usage = "add-entity <project> --name <name> [--folder <folder>] [--base <entity>] [options]",
        Description =
@"Adds the entity the way the editor's Add Entity dialog does: same files, same code generation, same plugin
reactions. Then saves the .gluj, the new .glej and the .csproj, and regenerates code.

Defaults match the dialog: a factory (CreatedByOtherEntities) and a list of the entity in GameScreen and in
every screen with a tile map are created unless --no-factory / --no-list are given; nothing else is added.
Checking a shape (--circle, --rectangle, --polygon) makes the entity ICollidable, as in the dialog, unless
--no-icollidable is given. --idamageable, --idamage-area, --platformer and --top-down also make it ICollidable.

With --base, the entity inherits from that entity and, as in the dialog, only --folder and the factory option
apply: shapes, interfaces, lists and movement come from the base.

The run fails (exit 1, nothing saved) when the name is invalid or taken by an entity, screen or file, the
folder name is invalid, or the base entity is unknown. The message lists close matches.

With --json, ""edit"" holds the new entity: {name, baseEntity, objects, implementsICollidable,
implementsIVisible, implementsIClickable, implementsIWindow, implementsIDamageable, implementsIDamageArea,
createdByOtherEntities, movement, addedToScreens}. objects are the entity's own object names, movement is
Platformer, TopDown or null, and addedToScreens lists the objects added to screens (Screens\GameScreen.EnemyList).",
        Options = new CliOption[]
        {
            new("name", "name", "Entity name without folders (Enemy). Required."),
            new("folder", "folder", "Folder under Entities (Enemies, or Enemies\\Bosses). Default: Entities itself.\nCreated if it does not exist."),
            new("base", "entity", "Entity to inherit from (Enemy, Entities\\Enemy)."),
            new("sprite", null, "Add a Sprite named SpriteInstance."),
            new("text", null, "Add a Text named TextInstance."),
            new("circle", null, "Add a Circle named CircleInstance."),
            new("rectangle", null, "Add an AxisAlignedRectangle named AxisAlignedRectangleInstance."),
            new("polygon", null, "Add a square Polygon named PolygonInstance."),
            new("icollidable", null, "Implement ICollidable."),
            new("no-icollidable", null, "Do not implement ICollidable, even with a shape."),
            new("ivisible", null, "Implement IVisible (not shown in the dialog)."),
            new("iclickable", null, "Implement IClickable (not shown in the dialog)."),
            new("iwindow", null, "Implement IWindow, which also implements IVisible (not shown in the dialog)."),
            new("idamageable", null, "Implement IDamageable (takes damage)."),
            new("idamage-area", null, "Implement IDamageArea (deals damage)."),
            new("team-index", "index", "TeamIndex for --idamageable / --idamage-area. Default 0."),
            new("opposing-team-collision", null, "With --idamageable / --idamage-area, add damage collision relationships in\nGameScreen against lists of entities on other teams."),
            new("no-factory", null, "Do not create a factory (CreatedByOtherEntities)."),
            new("no-list", null, "Do not add a list of this entity to GameScreen and tile map screens."),
            new("platformer", null, "Give the entity platformer movement."),
            new("top-down", null, "Give the entity top-down movement."),
        },
        Examples = new[]
        {
            ("add-entity MyGame --name Enemy --circle --sprite", "An ICollidable Enemy with a Circle and a Sprite, a factory, and an EnemyList in GameScreen."),
            ("add-entity MyGame --name Slime --folder Enemies --base Enemy --json", "Entities\\Enemies\\Slime inheriting from Enemy, JSON output."),
            ("add-entity MyGame --name Player --rectangle --platformer --no-factory", "A platformer Player with no factory."),
            ("add-entity MyGame --name Coin --no-factory --no-list", "A bare entity: no objects, no factory, no list."),
        },
        IsEdit = true,
        ValidateArguments = ValidateArguments,
        Run = RunAsync,
    };

    /// <summary>What --json reports under <c>edit</c>.</summary>
    public sealed record AddedEntity(string Name, string? BaseEntity, List<string> Objects,
        bool ImplementsICollidable, bool ImplementsIVisible, bool ImplementsIClickable, bool ImplementsIWindow,
        bool ImplementsIDamageable, bool ImplementsIDamageArea, bool CreatedByOtherEntities, string? Movement,
        List<string> AddedToScreens);

    static readonly string[] OptionsTheBaseProvides =
    {
        "sprite", "text", "circle", "rectangle", "polygon", "icollidable", "no-icollidable", "ivisible", "iclickable",
        "iwindow", "idamageable", "idamage-area", "team-index", "opposing-team-collision", "no-list", "platformer", "top-down",
    };

    static string? ValidateArguments(CliContext context)
    {
        var nameError = ElementNaming.ValidateNameArgument(context, "add-entity");
        if (nameError != null)
        {
            return nameError;
        }
        if (context.Has("base"))
        {
            var withBase = OptionsTheBaseProvides.Where(context.Has).ToList();
            if (withBase.Count > 0)
            {
                return "With --base, the entity gets its objects, interfaces, lists and movement from the base, so " +
                    string.Join(", ", withBase.Select(option => "--" + option)) + " cannot be used.";
            }
        }
        if (context.Has("icollidable") && context.Has("no-icollidable"))
        {
            return "--icollidable and --no-icollidable cannot be combined.";
        }
        if (context.Has("platformer") && context.Has("top-down"))
        {
            return "--platformer and --top-down cannot be combined.";
        }
        var needsCollidable = new[] { "idamageable", "idamage-area", "platformer", "top-down" }.Where(context.Has).ToList();
        if (context.Has("no-icollidable") && needsCollidable.Count > 0)
        {
            return $"--{needsCollidable[0]} makes the entity ICollidable, so --no-icollidable cannot be used with it.";
        }
        var isDamage = context.Has("idamageable") || context.Has("idamage-area");
        if (!isDamage && (context.Has("team-index") || context.Has("opposing-team-collision")))
        {
            return "--team-index and --opposing-team-collision need --idamageable or --idamage-area.";
        }
        if (context.Has("team-index") && !int.TryParse(context.Get("team-index"), out _))
        {
            return $"--team-index must be a whole number, got '{context.Get("team-index")}'.";
        }
        if (context.Has("opposing-team-collision") && context.Has("no-list"))
        {
            // The relationships are made against the new entity's list in GameScreen.
            return "--opposing-team-collision needs the entity's list in GameScreen, so it cannot be used with --no-list.";
        }
        return null;
    }

    static async Task RunAsync(CliContext context)
    {
        var name = context.Get("name")!;
        var nameVerifier = Builder.Get<NameVerifier>();
        if (!nameVerifier.IsEntityNameValid(name, null, out var whyNameIsInvalid))
        {
            context.Fail($"'{name}' is not a valid entity name: {whyNameIsInvalid}");
            return;
        }

        var directory = ElementNaming.ResolveDirectory(context, "Entities");
        if (directory == null)
        {
            return;
        }

        var project = GlueState.Self.CurrentGlueProject;
        EntitySave? baseEntity = null;
        if (context.Has("base"))
        {
            baseEntity = ElementNaming.FindElement(context.Get("base")!, project.Entities, "entity", out var baseError);
            if (baseEntity == null)
            {
                context.Fail(baseError!);
                return;
            }
        }

        var viewModel = CreateViewModel(context, directory, name, baseEntity);
        var screenObjectsBefore = ElementNaming.SnapshotScreenObjects();

        var entity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(viewModel);

        await TaskManager.Self.WaitForAllTasksFinished();

        string? movement = context.Has("platformer") ? "Platformer" : context.Has("top-down") ? "TopDown" : null;
        if (movement != null)
        {
            // The dialog's movement radio buttons are the Entity Input Movement plugin's, applied after the entity
            // exists (NewEntityCreatedReactionLogic); MakeEntityPlatformer/TopDown are that plugin's non-UI entry.
            var method = movement == "Platformer" ? "MakeEntityPlatformer" : "MakeEntityTopDown";
            await TaskManager.Self.AddAsync(() => PluginManager.CallPluginMethod("Entity Input Movement Plugin", method, entity),
                $"Making {entity} {movement}");
            await TaskManager.Self.WaitForAllTasksFinished();

            var property = movement == "Platformer" ? "IsPlatformer" : "IsTopDown";
            if (!entity.Properties.GetValue<bool>(property))
            {
                context.Fail($"{entity.Name} was created, but the Entity Input Movement plugin did not give it {movement} movement. " +
                    "Is the plugin installed?");
                return;
            }
        }

        var added = new AddedEntity(
            Name: entity.Name,
            BaseEntity: string.IsNullOrEmpty(entity.BaseEntity) ? null : entity.BaseEntity,
            Objects: entity.NamedObjects.Select(nos => nos.InstanceName).ToList(),
            ImplementsICollidable: entity.ImplementsICollidable,
            ImplementsIVisible: entity.ImplementsIVisible,
            ImplementsIClickable: entity.ImplementsIClickable,
            ImplementsIWindow: entity.ImplementsIWindow,
            ImplementsIDamageable: entity.Properties.GetValue<bool>("ImplementsIDamageable"),
            ImplementsIDamageArea: entity.Properties.GetValue<bool>("ImplementsIDamageArea"),
            CreatedByOtherEntities: entity.CreatedByOtherEntities,
            Movement: movement,
            AddedToScreens: ElementNaming.NewScreenObjects(screenObjectsBefore));

        var traits = new List<string>();
        if (added.BaseEntity != null) traits.Add("inherits " + added.BaseEntity);
        if (added.ImplementsICollidable) traits.Add("ICollidable");
        if (added.ImplementsIVisible) traits.Add("IVisible");
        if (added.ImplementsIClickable) traits.Add("IClickable");
        if (added.ImplementsIWindow) traits.Add("IWindow");
        if (added.ImplementsIDamageable) traits.Add("IDamageable");
        if (added.ImplementsIDamageArea) traits.Add("IDamageArea");
        if (added.Movement != null) traits.Add(added.Movement);
        if (added.CreatedByOtherEntities) traits.Add("factory");
        var text = $"Added entity {added.Name}" + (traits.Count > 0 ? $" ({string.Join(", ", traits)})" : "") + ".";
        if (added.Objects.Count > 0)
        {
            text += " Objects: " + string.Join(", ", added.Objects) + ".";
        }
        if (added.AddedToScreens.Count > 0)
        {
            text += " Added to screens: " + string.Join(", ", added.AddedToScreens) + ".";
        }
        context.Report(added, text);
    }

    /// <summary>
    /// The view model as the dialog leaves it once the options are checked: shapes before ICollidable, so a
    /// shape turns ICollidable on unless --no-icollidable turns it back off, as clicking in that order does.
    /// </summary>
    static AddEntityViewModel CreateViewModel(CliContext context, string directory, string name, EntitySave? baseEntity)
    {
        var viewModel = new AddEntityViewModel
        {
            Name = name,
            Directory = directory,
            SelectedBaseEntity = baseEntity?.Name ?? "<NONE>",
            IsSpriteChecked = context.Has("sprite"),
            IsTextChecked = context.Has("text"),
            IsCircleChecked = context.Has("circle"),
            IsAxisAlignedRectangleChecked = context.Has("rectangle"),
            IsPolygonChecked = context.Has("polygon"),
        };

        if (context.Has("icollidable") || context.Has("platformer") || context.Has("top-down"))
        {
            // The movement radio buttons check ICollidable and disable it (ModifyAddEntityWindowLogic).
            viewModel.IsICollidableChecked = true;
        }
        if (context.Has("no-icollidable"))
        {
            viewModel.IsICollidableChecked = false;
        }
        if (context.Has("iwindow"))
        {
            viewModel.IsIWindowChecked = true;
        }
        if (context.Has("ivisible"))
        {
            viewModel.IsIVisibleChecked = true;
        }
        viewModel.IsIClickableChecked = context.Has("iclickable");
        viewModel.IsIDamageableChecked = context.Has("idamageable");
        viewModel.IsIDamageAreaChecked = context.Has("idamage-area");

        if (context.Has("team-index"))
        {
            var teamIndex = int.Parse(context.Get("team-index")!);
            viewModel.TeamIndexOption = teamIndex switch { 0 => TeamIndexOption.Team0, 1 => TeamIndexOption.Team1, _ => TeamIndexOption.Custom };
            viewModel.CustomTeamIndex = teamIndex;
        }
        viewModel.IsOpposingTeamIndexDamageCollisionChecked = context.Has("opposing-team-collision");

        viewModel.IsCreateFactoryChecked = !context.Has("no-factory");
        viewModel.IncludeListsInScreens = !context.Has("no-list");
        return viewModel;
    }
}
