using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Glue.EditorProperties
{
    /// <summary>
    /// The properties the editor shows for a selected <see cref="CustomVariable"/>: which exist for the variable,
    /// which can change, and the values offered. The WinForms Properties grid
    /// (<c>CustomVariablePropertyGridDisplayer</c>) renders them and GlueCli's <c>set-variable-property</c> sets them.
    /// </summary>
    public class CustomVariablePropertyModel
    {
        /// <summary>The read-only line shown instead of DefaultValue for a variable whose type is a CSV loaded as a list.</summary>
        public const string ListCsvDefaultValueNote = "Set CreatesDictionary to true to assign value";

        /// <summary>The category the grid lists the default value under, at the top.</summary>
        public const string ValueCategory = "\t";

        public static readonly IReadOnlyList<string> OverridingPropertyTypes = new[] { "<NONE>", "int", "long", "string", "float" };

        public IReadOnlyList<EditorProperty> GetProperties(CustomVariable variable, GlueElement element)
        {
            var builder = new EditorPropertyListBuilder(variable);

            builder.Exclude(nameof(CustomVariable.Properties));
            builder.Exclude(nameof(CustomVariable.FulfillsRequirement));

            AddDefaultValue(builder, variable, element);

            if (!variable.DefinedByBase && variable.GetIsNewVariable(element))
            {
                // A new variable's type can change; one that exposes or tunnels has the type of what it exposes.
                builder.Set(new EditorProperty(variable, nameof(CustomVariable.Type), typeof(string))
                {
                    Getter = () => variable.Type,
                    Setter = value => variable.Type = value as string,
                    Options = () => ExposedVariableManager.GetAvailableNewVariableTypes(allowNone: false),
                });
            }

            if (variable.DefinedByBase)
            {
                builder.Exclude(nameof(CustomVariable.SourceObject));
                builder.Exclude(nameof(CustomVariable.SourceObjectProperty));
                builder.Exclude(nameof(CustomVariable.OverridingPropertyType));
            }
            else
            {
                builder.Include(nameof(CustomVariable.SourceObject),
                    () => AvailableNamedObjectsAndFiles.GetAvailableObjects(true, true, element, null).ToList());
                if (string.IsNullOrEmpty(variable.SourceObject))
                {
                    builder.Exclude(nameof(CustomVariable.SourceObjectProperty));
                }
                else
                {
                    builder.Include(nameof(CustomVariable.SourceObjectProperty), () => GetSourceObjectProperties(variable, element));
                }
                builder.Include(nameof(CustomVariable.OverridingPropertyType), () => OverridingPropertyTypes);
            }

            if (string.IsNullOrEmpty(variable.OverridingPropertyType))
            {
                builder.Exclude(nameof(CustomVariable.TypeConverter));
            }
            else
            {
                builder.Include(nameof(CustomVariable.TypeConverter), () => TypeConverterHelper.TypeConverters.Keys.ToList());
            }

            if (variable.DefinedByBase)
            {
                builder.Exclude(nameof(CustomVariable.Scope));
            }

            return builder.Build();
        }

        static void AddDefaultValue(EditorPropertyListBuilder builder, CustomVariable variable, GlueElement element)
        {
            if (variable.GetIsCsv() && variable.GetIsListCsv())
            {
                builder.Exclude(nameof(CustomVariable.DefaultValue));
                builder.Set(new EditorProperty(variable, ListCsvDefaultValueNote, typeof(string))
                {
                    IsReadOnly = true,
                    Getter = () => null,
                    Category = ValueCategory,
                });
                return;
            }

            builder.Set(new EditorProperty(variable, nameof(CustomVariable.DefaultValue), variable.GetRuntimeType() ?? typeof(string))
            {
                Getter = variable.GetValue,
                // The value's own type decides what it can be (set-variable converts it); the grid edits it
                // through the variable's type converter.
                Setter = value => SetDefaultValue(variable, element, value),
                Category = ValueCategory,
            });
        }

        /// <summary>
        /// Sets <paramref name="variable"/>'s default value from the grid or the CLI. Text is converted to the
        /// variable's runtime type, except for files and colors, which are stored as text.
        /// </summary>
        public static void SetDefaultValue(CustomVariable variable, GlueElement element, object? value)
        {
            if (ShouldConvertFromText(variable, value))
            {
                value = PropertyValuePair.ConvertStringToType((string)value!, variable.GetRuntimeType());
            }

            if (element is EntitySave entity)
            {
                entity.SetCustomVariable(variable.Name, value);
            }
            else
            {
                ((ScreenSave)element).SetCustomVariable(variable.Name, value);
            }
        }

        public static bool ShouldConvertFromText(CustomVariable variable, object? value) =>
            variable.GetRuntimeType() != null && value is string && !variable.GetIsFile() && variable.Type != "Color";

        static IReadOnlyList<string> GetSourceObjectProperties(CustomVariable variable, GlueElement element)
        {
            var toReturn = new List<string> { "<NONE>" };
            var nos = element.GetNamedObjectRecursively(variable.SourceObject);
            if (nos != null)
            {
                toReturn.AddRange(ExposedVariableManager.GetExposableMembersFor(nos).Select(item => item.Member));
            }
            return toReturn;
        }
    }
}
