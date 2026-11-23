using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// Platformer and top-down movement, which the Entity Input Movement plugin owns. The plugin assigns its
    /// implementation to <see cref="ExportedImplementations.GlueCommands.EntityInputMovementCommands"/> at startup,
    /// so the property is null when the plugin is not loaded.
    /// </summary>
    public interface IEntityInputMovementCommands
    {
        /// <summary>Gives <paramref name="entity"/> platformer movement, as checking "Platformer" on the entity's movement tab does.</summary>
        void MakeEntityPlatformer(EntitySave entity);

        /// <summary>Gives <paramref name="entity"/> top-down movement, as checking "Top-Down" on the entity's movement tab does.</summary>
        void MakeEntityTopDown(EntitySave entity);
    }
}
