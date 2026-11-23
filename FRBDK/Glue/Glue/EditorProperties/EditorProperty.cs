using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// One property the editor shows for an object (a file, a variable, ...), with no UI types: its name and type,
    /// whether it can be changed, the values it can take, and how to read and write it. The WinForms Properties grid
    /// renders a list of these and GlueCli sets them, so both agree on what exists.
    /// </summary>
    public sealed class EditorProperty
    {
        public EditorProperty(object owner, string name, Type type)
        {
            Owner = owner;
            Name = name;
            Type = type;
        }

        /// <summary>The object whose property this is.</summary>
        public object Owner { get; }

        public string Name { get; }

        public Type Type { get; }

        public bool IsReadOnly { get; init; }

        /// <summary>
        /// True when the property is a public property or field of <see cref="Owner"/> with this name, so its value,
        /// attributes and description come from that member. False for a property the editor adds (an image's
        /// width, a variable's default value), which has a <see cref="Getter"/>.
        /// </summary>
        public bool IsOwnerMember { get; init; }

        /// <summary>Reads the value. Null for an owner member read directly.</summary>
        public Func<object?>? Getter { get; init; }

        /// <summary>Writes the value. Null for an owner member written directly, or a read-only property.</summary>
        public Action<object?>? Setter { get; init; }

        /// <summary>
        /// The values the editor offers, as text. When set, the value must be one of them.
        /// </summary>
        public Func<IReadOnlyList<string>>? Options { get; init; }

        /// <summary>The category the grid lists a property the editor adds under. Owner members use their own attribute.</summary>
        public string? Category { get; init; }

        public object? GetValue()
        {
            if (Getter != null)
            {
                return Getter();
            }
            var member = FindMember(Owner.GetType(), Name);
            return member switch
            {
                PropertyInfo property => property.GetValue(Owner),
                FieldInfo field => field.GetValue(Owner),
                _ => null
            };
        }

        public void SetValue(object? value)
        {
            if (Setter != null)
            {
                Setter(value);
                return;
            }
            switch (FindMember(Owner.GetType(), Name))
            {
                case PropertyInfo property:
                    property.SetValue(Owner, value);
                    break;
                case FieldInfo field:
                    field.SetValue(Owner, value);
                    break;
                default:
                    throw new InvalidOperationException($"{Owner.GetType().Name} has no member {Name} to set.");
            }
        }

        public override string ToString() => $"{Type.Name} {Name}";

        internal static MemberInfo? FindMember(Type type, string name) =>
            (MemberInfo?)type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public) ??
            type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
    }

    /// <summary>
    /// Builds an object's <see cref="EditorProperty"/> list: every public instance property and field of the object
    /// to start with, then the rules of the object's kind exclude, replace and add properties.
    /// </summary>
    public sealed class EditorPropertyListBuilder
    {
        readonly object owner;
        readonly List<EditorProperty> properties = new();

        public EditorPropertyListBuilder(object owner)
        {
            this.owner = owner;
            var type = owner.GetType();
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                properties.Add(new EditorProperty(owner, property.Name, property.PropertyType)
                {
                    IsOwnerMember = true,
                    IsReadOnly = !property.CanWrite || IsDeclaredReadOnly(property),
                });
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                properties.Add(new EditorProperty(owner, field.Name, field.FieldType) { IsOwnerMember = true });
            }
        }

        public object Owner => owner;

        public EditorProperty? Get(string name) => properties.FirstOrDefault(item => item.Name == name);

        public void Exclude(string name) => properties.RemoveAll(item => item.Name == name);

        /// <summary>Adds <paramref name="property"/>, replacing one with the same name.</summary>
        public void Set(EditorProperty property)
        {
            var index = properties.FindIndex(item => item.Name == property.Name);
            if (index == -1)
            {
                properties.Add(property);
            }
            else
            {
                properties[index] = property;
            }
        }

        /// <summary>
        /// Shows the owner member <paramref name="name"/> as it starts, picked from <paramref name="options"/>. It replaces
        /// what earlier rules did to it, and includes it again if one excluded it.
        /// </summary>
        public void Include(string name, Func<IReadOnlyList<string>> options)
        {
            var member = OwnerMember(name) ?? throw new ArgumentException($"{owner.GetType().Name} has no member {name}.", nameof(name));
            Set(Copy(member, options: options));
        }

        /// <summary>Gives the property <paramref name="name"/> a list of values to pick from, if it is shown.</summary>
        public void SetOptions(string name, Func<IReadOnlyList<string>> options)
        {
            var existing = Get(name);
            if (existing != null)
            {
                Set(Copy(existing, options: options));
            }
        }

        public void SetReadOnly(string name)
        {
            var existing = Get(name);
            if (existing != null && !existing.IsReadOnly)
            {
                Set(Copy(existing, isReadOnly: true));
            }
        }

        public IReadOnlyList<EditorProperty> Build() => properties.ToList();

        /// <summary>The owner member <paramref name="name"/> as it starts, before any rule; null when there is none.</summary>
        EditorProperty? OwnerMember(string name)
        {
            return EditorProperty.FindMember(owner.GetType(), name) switch
            {
                PropertyInfo property => new EditorProperty(owner, name, property.PropertyType)
                {
                    IsOwnerMember = true,
                    IsReadOnly = !property.CanWrite || IsDeclaredReadOnly(property),
                },
                FieldInfo field => new EditorProperty(owner, name, field.FieldType) { IsOwnerMember = true },
                _ => null
            };
        }

        static EditorProperty Copy(EditorProperty source, Func<IReadOnlyList<string>>? options = null, bool? isReadOnly = null) =>
            new(source.Owner, source.Name, source.Type)
            {
                IsOwnerMember = source.IsOwnerMember,
                IsReadOnly = isReadOnly ?? source.IsReadOnly,
                Getter = source.Getter,
                Setter = source.Setter,
                Options = options ?? source.Options,
                Category = source.Category,
            };

        static bool IsDeclaredReadOnly(PropertyInfo property) =>
            property.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Cast<ReadOnlyAttribute>().Any(item => item.IsReadOnly);
    }
}
