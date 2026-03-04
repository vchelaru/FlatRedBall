using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using Glue;
using WpfDataUi.DataTypes;
using OfficialPlugins.VariableDisplay.Controls;
using OfficialPlugins.VariableDisplay.Data;
using GluePropertyGridClasses.StringConverters;
using EditorObjects.IoC;
using Gum.DataTypes.Variables;
using OfficialPlugins.PropertyGrid;
using FlatRedBall.Glue.SetVariable;

namespace OfficialPlugins.VariableDisplay
{
    static partial class NamedObjectVariableShowingLogic
    {
        #region Create InstanceMember (Variable)
        private static NamedObjectSaveVariableDataGridItem CreateInstanceMember(NamedObjectSave instance,
            GlueElement container,
            string customTypeName,
            AssetTypeInfo ati,
            VariableDefinition variableDefinition, string nameOnInstance, IEnumerable<MemberCategory> categories)
        {
            bool shouldBeSkipped =
                GetIfShouldBeSkipped(variableDefinition.Name, instance, ati);
            ///////Early Out//////////
            if (shouldBeSkipped)
            {
                return null;
            }
            ////End Early Out///////

            var instanceMember = new NamedObjectSaveVariableDataGridItem();
            instanceMember.RefreshFrom(instance, variableDefinition, container, categories, customTypeName, nameOnInstance);
            instanceMember.RefreshAddContextMenuEvents();

            return instanceMember;
        }

        #endregion

        private static InstanceMember CreateInstanceMemberForSourceName(NamedObjectSave instance)
        {

            var instanceMember = new FileInstanceMember();

            instanceMember.View += () =>
            {
                var element = GlueState.Self.CurrentElement;
                var rfs = element.ReferencedFiles.FirstOrDefault(item => item.Name == instance.SourceFile);

                if (rfs != null)
                {
                    GlueCommands.Self.SelectCommands.Select(
                        rfs,
                        instance.SourceNameWithoutParenthesis);
                }
            };

            instanceMember.FirstGridLength = new System.Windows.GridLength(140);

            instanceMember.UnmodifiedVariableName = "SourceName";
            string fileName = FlatRedBall.IO.FileManager.RemovePath(instance.SourceFile);
            instanceMember.DisplayName = $"Object in {fileName}:";

            // todo: get the type converter from the file
            var typeConverter = new AvailableNameablesStringConverter(instance, null);
            instanceMember.TypeConverter = typeConverter;

            instanceMember.CustomGetTypeEvent += (throwaway) => typeof(string);

            instanceMember.PreferredDisplayer = typeof(FileReferenceComboBox);

            instanceMember.IsDefault = instance.SourceName == null;

            instanceMember.CustomGetEvent += (throwaway) =>
            {
                return instance.SourceName;
            };

            instanceMember.CustomSetPropertyEvent += (owner, args) =>
            {
                var value = args.Value;
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                instance.SourceName = value as string;

                GlueCommands.Self.GluxCommands.SaveProjectAndElements();

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            };

            instanceMember.IsDefaultSet += (owner, args) =>
            {
                instance.SourceName = null;
            };

            instanceMember.SetValueError += (newValue) =>
            {
                if (newValue is string && string.IsNullOrEmpty(newValue as string))
                {
                    MakeDefault(instance, "SourceName");
                }
            };

            return instanceMember;

        }

        private static DataGridItem CreateNameInstanceMember(NamedObjectSave instance)
        {
            var instanceMember = new DataGridItem();
            instanceMember.DisplayName = "Name";
            instanceMember.UnmodifiedVariableName = "Name";

            // this gets updated in the CustomSetEvent below
            string oldValue = instance.InstanceName;

            if (instance.DefinedByBase)
            {
                instanceMember.MakeReadOnly();
            }

            instanceMember.CustomSetPropertyEvent += (throwaway, args) =>
            {
                var value = args.Value;
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                instance.InstanceName = value as string;

                var element = GlueState.Self.CurrentElement;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(NamedObjectSave.InstanceName), oldValue, nameof(NamedObjectSave.InstanceName), null);

                if (element != null)
                {
                    GlueCommands.Self.GluxCommands.SaveElementAsync(element);
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                oldValue = (string)value;
            };
            instanceMember.CustomGetEvent += throwaway => instance.InstanceName;

            instanceMember.CustomGetTypeEvent += throwaway => typeof(string);

            return instanceMember;
        }

        private static DataGridItem CreateIsLockedMember(NamedObjectSave instance)
        {
            var instanceMember = new DataGridItem();
            instanceMember.DisplayName =
                StringFunctions.InsertSpacesInCamelCaseString(nameof(instance.IsEditingLocked));
            instanceMember.UnmodifiedVariableName =
                nameof(instance.IsEditingLocked);

            var oldValue = instance.IsEditingLocked;

            instanceMember.CustomSetPropertyEvent += (throwaway, args) =>
            {
                var value = args.Value;
                instanceMember.IsDefault = false;
                RefreshLogic.IgnoreNextRefresh();

                var valueAsBool = value as bool? ?? false;
                instance.IsEditingLocked = valueAsBool;

                EditorObjects.IoC.Container.Get<SetPropertyManager>().ReactToPropertyChanged(
                    nameof(instance.IsEditingLocked), oldValue, nameof(instance.IsEditingLocked), null);


                //GlueCommands.Self.GluxCommands.SetVariableOn(
                //    instance,
                //    "Name",
                //    typeof(string),
                //    value);


                GlueCommands.Self.GluxCommands.SaveProjectAndElements();

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                oldValue = valueAsBool;
            };

            instanceMember.CustomGetEvent += throwaway =>
            {
                //return instance.IsEditingLocked;
                return ObjectFinder.Self.GetPropertyValueRecursively<bool>(instance, nameof(NamedObjectSave.IsEditingLocked));
            };

            instanceMember.IsDefaultSet += (sender, args) =>
            {
                instance.Properties.RemoveAll(item => item.Name == nameof(NamedObjectSave.IsEditingLocked));
            };

            instanceMember.CustomGetTypeEvent += throwaway => typeof(bool);

            return instanceMember;
        }

        private static void MakeDefault(NamedObjectSave instance, string memberName)
        {
            var oldValue = instance.GetCustomVariable(memberName)?.Value;

            PropertyGridRightClickHelper.SetVariableToDefault(instance, memberName);

            var element = ObjectFinder.Self.GetElementContaining(instance);

            if (element != null)
            {
                // do we want to run this async?
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
            }

            GlueCommands.Self.GluxCommands.SaveProjectAndElements();

            MainGlueWindow.Self.PropertyGrid.Refresh();

            PluginManager.ReactToChangedProperty(memberName, oldValue, element, new PluginManager.NamedObjectSavePropertyChange
            {
                NamedObjectSave = instance,
                ChangedPropertyName = memberName
            });

            PluginManager.ReactToNamedObjectChangedValueList(new List<VariableChangeArguments>
            {
                new VariableChangeArguments
                {
                    NamedObject = instance,
                    ChangedMember = memberName,
                    OldValue = oldValue
                }
            });
        }
    }
}
