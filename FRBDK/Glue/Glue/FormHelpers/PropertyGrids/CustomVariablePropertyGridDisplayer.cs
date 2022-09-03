using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using System.ComponentModel;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class CustomVariablePropertyGridDisplayer : PropertyGridDisplayer
    {
        public IElement CurrentElement
        {
            get;
            set;
        }


        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {

                UpdateIncludedAndExcluded(value as CustomVariable);

                base.Instance = value;
            }
        }

        private void UpdateIncludedAndExcluded(CustomVariable instance)
        {

            ResetToDefault();

            bool shouldIncludeSourceObject = true;
            bool shouldIncludeSourceObjectProperty = true;
            bool shouldIncludeOverridingPropertyType = true;
            bool shouldIncludeTypeConverter = true;
            bool shouldIncludeScope = instance.DefinedByBase == false;

            ExcludeMember(nameof(CustomVariable.Properties));
            ExcludeMember(nameof(CustomVariable.FulfillsRequirement));

            DefaultValueIncludeActivity(instance);

            if (string.IsNullOrEmpty(instance.SourceObject))
            {
                shouldIncludeSourceObjectProperty = false;
            }

            if (string.IsNullOrEmpty(instance.OverridingPropertyType))
            {
                shouldIncludeTypeConverter = false;

            }


            HandleTypeProperty(instance);

            if (instance.DefinedByBase)
            {
                shouldIncludeSourceObject = false;
                shouldIncludeSourceObjectProperty = false;
                shouldIncludeOverridingPropertyType = false;

            }

            if (shouldIncludeSourceObject)
            {
                IncludeMember(nameof(CustomVariable.SourceObject), typeof(CustomVariable), new AvailableNamedObjectsAndFiles(CurrentElement));
            }
            else
            {
                ExcludeMember(nameof(CustomVariable.SourceObject));
            }

            if (shouldIncludeSourceObjectProperty)
            {
                IncludeMember(nameof(CustomVariable.SourceObjectProperty), typeof(CustomVariable), new AvailableExposedNamedObjectVariables(instance, CurrentElement));
            }
            else
            {
                ExcludeMember(nameof(CustomVariable.SourceObjectProperty));
            }

            if (shouldIncludeOverridingPropertyType)
            {
                IncludeMember(nameof(CustomVariable.OverridingPropertyType), typeof(CustomVariable), new AvailablePrimitiveTypeStringConverter());
            }
            else
            {
                ExcludeMember(nameof(CustomVariable.OverridingPropertyType));
            }

            if (shouldIncludeTypeConverter)
            {
                IncludeMember(nameof(CustomVariable.TypeConverter), typeof(CustomVariable), new AvailableCustomVariableTypeConverters());
            }
            else
            {
                ExcludeMember(nameof(CustomVariable.TypeConverter));
            }

            if(shouldIncludeScope == false)
            {
                ExcludeMember(nameof(CustomVariable.Scope));
            }

        }

        private void HandleTypeProperty(CustomVariable variable)
        {

            if (variable != null && 
                // If it's defined by base, then don't let the type change
                variable.DefinedByBase == false && variable.GetIsNewVariable(CurrentElement))
            {
                    // We want to allow the user to set the type on a variable if it's new

                ExcludeMember(nameof(CustomVariable.Type));
                TypeConverter typeConverter = new AvailableCustomVariableTypes { AllowNone = false };

                IncludeMember(nameof(CustomVariable.Type), typeof(string),
                    ChangeType,
                    () => { return variable.Type; },
                    typeConverter);
            }
        }

        private void ChangeType(object sender, MemberChangeArgs args)
        {
            var customVariable =
                (CustomVariable)args.Owner;

            customVariable.Type = args.Value as string;

        }

        private void DefaultValueIncludeActivity(CustomVariable instance)
        {

            bool handled = false;

            if (instance.GetIsCsv())
            {
                if (instance.GetIsListCsv())
                {
                    handled = true;
                    var member = IncludeMember("Set CreatesDictionary to true to assign value", typeof(string),
                        (object sender, MemberChangeArgs args) => { },
                        () => { return null; },


                        null,
                        // Let's put this in the variables list
                        new Attribute[] { new CategoryAttribute("\t") });
                    member.IsReadOnly = true;
                }
            }


            if (!handled)
            {
                Type typeToPass = instance.GetRuntimeType();
                if (typeToPass == null)
                {
                    typeToPass = typeof(string);
                }

                var typeConverter =
                    instance.GetTypeConverter(CurrentElement);

                IncludeMember(
                    nameof(CustomVariable.DefaultValue),
                    typeToPass,
                    OnMemberChanged,
                    instance.GetValue,
                    typeConverter,
                    new Attribute[] { new CategoryAttribute("\t") });
            }
            else
            {
                ExcludeMember(nameof(CustomVariable.DefaultValue));
            }
        }

        public static void OnMemberChanged(object sender, MemberChangeArgs args)
        {
            CustomVariable variable = args.Owner as CustomVariable;

            object value = args.Value;

            if (GetShouldCustomVariableBeConvertedToType(args, variable))
            {
                var variableRuntimeType = variable.GetRuntimeType();

                value = PropertyValuePair.ConvertStringToType((string)args.Value, variableRuntimeType);
            }

            if (GlueState.Self.CurrentEntitySave != null)
            {
                GlueState.Self.CurrentEntitySave.SetCustomVariable(GlueState.Self.CurrentCustomVariable.Name, value);
            }
            else
            {
                GlueState.Self.CurrentScreenSave.SetCustomVariable(GlueState.Self.CurrentCustomVariable.Name, value);

            }
        }

        public static bool GetShouldCustomVariableBeConvertedToType(MemberChangeArgs args, CustomVariable variable)
        {
            var runtimeType = variable.GetRuntimeType();

            return runtimeType != null && args.Value is string && !variable.GetIsFile() && variable.Type != "Color";
        }
    }
}
