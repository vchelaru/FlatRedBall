using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// One setting of a collision relationship, as its Collision tab shows it and its NamedObjectSave stores it.
    /// </summary>
    /// <param name="Type">The value's type: string, bool, float, or an enum.</param>
    /// <param name="DefaultValue">The value when the relationship does not store one.</param>
    public sealed record CollisionRelationshipSetting(string Name, Type Type, object DefaultValue);

    /// <summary>
    /// Creates collision relationships and changes their settings without any UI. The Collision Plugin implements
    /// it and assigns it to <c>GlueCommands.Self.CollisionRelationshipCommands</c>; its Collision tab and GlueCli
    /// both go through it.
    /// </summary>
    public interface ICollisionRelationshipCommands
    {
        /// <summary>Every setting <see cref="GetValue"/> and <see cref="SetValue"/> accept.</summary>
        IReadOnlyList<CollisionRelationshipSetting> Settings { get; }

        bool IsCollisionRelationship(NamedObjectSave nos);

        /// <summary>
        /// Recomputes the relationship's SourceClassType from its two objects and settings, after one of them changed
        /// (a rename, a new collision type). Returns whether the type changed.
        /// </summary>
        bool FixNamedObjectCollisionType(NamedObjectSave relationship);

        /// <summary>Whether <paramref name="nos"/> can be in a relationship: an ICollidable entity instance, a list of
        /// them, a ShapeCollection or a TileShapeCollection.</summary>
        bool CanCollide(NamedObjectSave nos);

        /// <summary>
        /// Whether creating a relationship in <paramref name="container"/> is unusual enough for the editor to ask
        /// first: it is a screen deriving from GameScreen, where relationships usually go.
        /// </summary>
        bool IsUnusualContainer(GlueElement container);

        /// <summary>
        /// Adds a relationship between two objects in <paramref name="container"/>, as the Collision tab's Add button
        /// does: an entity or list goes first, Glue's auto name, tile-collision defaults, code generation. Asks nothing.
        /// </summary>
        Task<NamedObjectSave> CreateAsync(GlueElement container, string firstObjectName, string secondObjectName);

        /// <summary>The shapes <paramref name="collidable"/> can collide with alone, plus "&lt;Entire Object&gt;",
        /// or null when it is not an entity or a list of them.</summary>
        IReadOnlyList<string> GetSubCollisionOptions(NamedObjectSave collidable);

        /// <summary>The values a setting can take when the Collision tab picks it from a list, otherwise null.</summary>
        IReadOnlyList<string> GetOptions(NamedObjectSave relationship, string setting);

        /// <summary>The setting's value as its <see cref="CollisionRelationshipSetting.Type"/>, or its default.</summary>
        object GetValue(NamedObjectSave relationship, string setting);

        /// <summary>
        /// Sets a setting and runs what follows a change in the Collision tab: auto name, TileShapeCollection masses,
        /// error refresh, code generation, save. Does nothing when the value is unchanged.
        /// </summary>
        void SetValue(NamedObjectSave relationship, string setting, object value);
    }
}
