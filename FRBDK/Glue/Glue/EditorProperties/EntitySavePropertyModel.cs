using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// The properties the editor shows for a selected entity: which apply, which can change, and the values offered.
    /// Plugins add their own (ImplementsIDamageable, ImplementsIGumCollidable...) through
    /// <c>PluginBase.AdjustEntityProperties</c>. The WinForms Properties grid (<c>EntitySavePropertyGridDisplayer</c>)
    /// renders them and GlueCli's <c>set-property</c> sets them.
    /// </summary>
    public class EntitySavePropertyModel : ElementPropertyModel
    {
        public const string NameCategory = "\tEntity";

        static readonly string[] ScrollableListMembers =
        {
            nameof(EntitySave.VerticalOrHorizontal), nameof(EntitySave.ListTopBound), nameof(EntitySave.ListBottomBound),
            // No properties back these; scrollable entity lists are gone since GLUX/J v16.
            "ListLeftBound", "ListRightBound",
            nameof(EntitySave.SpacingBetweenItems),
        };

        readonly IGlueState glueState;
        readonly IAvailableAssetTypesCore assetTypes;
        readonly Action<EntitySave, EditorPropertyListBuilder> adjustByPlugins;

        /// <param name="adjustByPlugins">Lets plugins add or hide properties (PluginManager.AdjustEntityProperties).</param>
        public EntitySavePropertyModel(IGlueState glueState, IGlueCommands glueCommands, IAvailableAssetTypesCore assetTypes,
            Action<EntitySave, EditorPropertyListBuilder> adjustByPlugins) : base(glueCommands)
        {
            this.glueState = glueState;
            this.assetTypes = assetTypes;
            this.adjustByPlugins = adjustByPlugins;
        }

        public IReadOnlyList<EditorProperty> GetProperties(EntitySave entity)
        {
            var builder = CreateBuilder(entity);

            if (!string.IsNullOrEmpty(entity.BaseEntity) && entity.GetInheritsFromIVisible() &&
                // An entity inheriting from a FRB type is IVisible too, but still chooses.
                !entity.InheritsFromFrbType())
            {
                // The base is IVisible, so the derived entity is too.
                builder.Set(new EditorProperty(entity, nameof(EntitySave.ImplementsIVisible), typeof(bool))
                {
                    IsReadOnly = true,
                    Getter = () => true,
                });
            }

            if (glueState.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.RemoveIsScrollableEntityList)
            {
                builder.Exclude(nameof(EntitySave.IsScrollableEntityList));
            }

            if (!string.IsNullOrEmpty(entity.BaseEntity) && !entity.GetHasImplementsCollidableProperty())
            {
                builder.Exclude(nameof(EntitySave.ImplementsICollidable));
            }

            if (!entity.CreatedByOtherEntities)
            {
                builder.Exclude(nameof(EntitySave.PooledByFactory));
            }

            if (!entity.IsScrollableEntityList)
            {
                foreach (var name in ScrollableListMembers)
                {
                    builder.Exclude(name);
                }
                builder.Exclude(nameof(EntitySave.ItemType));
            }
            else
            {
                builder.Include(nameof(EntitySave.ItemType), () => GetOtherEntities(entity));
            }

            builder.Include(nameof(EntitySave.BaseEntity), () => GetBaseEntityOptions(entity));

            AddName(builder, entity, NameCategory);

            adjustByPlugins(entity, builder);

            return builder.Build();
        }

        /// <summary>&lt;NONE&gt;, every other entity, and the FRB types an entity can inherit from.</summary>
        IReadOnlyList<string> GetBaseEntityOptions(EntitySave entity) =>
            new[] { "<NONE>" }
                .Concat(glueState.CurrentGlueProject.Entities.Where(item => item != entity).Select(item => item.Name))
                .Concat(assetTypes.AllAssetTypes.Where(item => item.IsPositionedObject).Select(item => item.QualifiedRuntimeTypeName.QualifiedType))
                .ToList();

        IReadOnlyList<string> GetOtherEntities(EntitySave entity) =>
            new[] { "<NONE>" }
                .Concat(glueState.CurrentGlueProject.Entities.Where(item => item.Name != entity.Name).Select(item => item.Name))
                .ToList();
    }
}
