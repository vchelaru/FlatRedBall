using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// What the editor shows for every screen and entity: the element's own members, without its custom property
    /// bag and class name, plus Name, which renames the element. <see cref="EntitySavePropertyModel"/> and
    /// <see cref="ScreenSavePropertyModel"/> add their own rules.
    /// </summary>
    public abstract class ElementPropertyModel
    {
        protected readonly IGlueCommands glueCommands;

        protected ElementPropertyModel(IGlueCommands glueCommands)
        {
            this.glueCommands = glueCommands;
        }

        protected static EditorPropertyListBuilder CreateBuilder(GlueElement element)
        {
            var builder = new EditorPropertyListBuilder(element);
            builder.Exclude(nameof(GlueElement.Properties));
            builder.Exclude(nameof(GlueElement.ClassName));
            return builder;
        }

        /// <summary>Name shows the element's name without its folder; setting it renames the element.</summary>
        protected void AddName(EditorPropertyListBuilder builder, GlueElement element, string category)
        {
            builder.Set(new EditorProperty(element, nameof(GlueElement.Name), typeof(string))
            {
                Getter = () => element.ClassName,
                Setter = value =>
                {
                    var oldName = element.Name;
                    var newName = oldName.Substring(0, oldName.LastIndexOf("\\") + 1) + (string?)value;
                    _ = glueCommands.GluxCommands.ElementCommands.RenameElement(element, newName);
                },
                Category = category,
            });
        }

        /// <summary>
        /// A property stored in the element's custom property bag (<see cref="GlueElement.Properties"/>), the way plugins
        /// add flags such as ImplementsIDamageable. Reads the type's default while unset.
        /// </summary>
        public static EditorProperty CustomProperty(GlueElement element, string name, Type type, string? category = null) =>
            new(element, name, type)
            {
                Getter = () => element.Properties.GetValue(name) ?? TypeManager.GetDefaultForTypeAsType(type.Name),
                Setter = value => element.Properties.SetValue(name, value),
                Category = category,
            };
    }
}
