using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.GuiDisplay;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    /// <summary>
    /// Shows a list of <see cref="EditorProperty"/> in a WinForms <see cref="PropertyGridDisplayer"/>. The list decides
    /// what exists, what is read-only and what values are offered; this only turns it into grid members. Call it
    /// before setting the displayer's <c>Instance</c>.
    /// </summary>
    internal static class EditorPropertyGridMapping
    {
        /// <param name="converterFor">A grid-only converter for a property, for a value the grid edits through its
        /// own type converter rather than a list (a variable's default value). Null uses the property's options.</param>
        public static void Apply(PropertyGridDisplayer displayer, object owner, IReadOnlyList<EditorProperty> properties,
            Func<EditorProperty, TypeConverter?>? converterFor = null)
        {
            displayer.ForcedReadOnlyProperties.Clear();
            displayer.ResetToDefault();

            var names = new HashSet<string>(properties.Select(item => item.Name));
            var ownerType = owner.GetType();
            var memberNames = ownerType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(item => item.Name)
                .Concat(ownerType.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(item => item.Name));
            foreach (var name in memberNames.Where(item => !names.Contains(item)).Distinct().ToList())
            {
                displayer.ExcludeMember(name);
            }

            foreach (var property in properties)
            {
                var converter = converterFor?.Invoke(property) ?? OptionsConverter(property);
                if (property.IsOwnerMember && converter == null)
                {
                    // The displayer reads the member itself, with its own attributes.
                    if (property.IsReadOnly)
                    {
                        displayer.ForcedReadOnlyProperties.Add(property.Name);
                    }
                }
                else if (property.IsOwnerMember)
                {
                    var attributes = MemberAttributes(ownerType, property.Name);
                    if (property.IsReadOnly && !attributes.OfType<ReadOnlyAttribute>().Any(item => item.IsReadOnly))
                    {
                        attributes.Add(new ReadOnlyAttribute(true));
                    }
                    displayer.IncludeMember(property.Name, property.Type, null, null, converter, attributes.ToArray());
                }
                else
                {
                    var attributes = property.Category != null ? new Attribute[] { new CategoryAttribute(property.Category) } : null;
                    MemberChangeEventHandler? setter = property.IsReadOnly ? null : (_, args) => property.SetValue(args.Value);
                    displayer.IncludeMember(property.Name, property.Type, setter, property.Getter, converter, attributes);
                }
            }
        }

        static TypeConverter? OptionsConverter(EditorProperty property) =>
            property.Options == null ? null : new DelegateBasedTypeConverter { CustomDelegate = () => property.Options().ToList() };

        static List<Attribute> MemberAttributes(Type ownerType, string name)
        {
            var member = (MemberInfo?)ownerType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public) ??
                ownerType.GetField(name, BindingFlags.Instance | BindingFlags.Public);
            return member?.GetCustomAttributes(true).OfType<Attribute>().ToList() ?? new List<Attribute>();
        }
    }
}
