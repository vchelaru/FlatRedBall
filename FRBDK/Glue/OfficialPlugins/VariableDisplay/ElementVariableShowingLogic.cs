using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using WpfDataUi;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Plugins;

namespace OfficialPlugins.VariableDisplay
{
    class ElementVariableShowingLogic
    {
        public static void UpdateShownVariables(DataUiGrid grid, IElement element)
        {
            grid.Categories.Clear();

            List<MemberCategory> categories = new List<MemberCategory>();
            var defaultCategory = new MemberCategory("Variables");
            defaultCategory.FontSize = 14;
            categories.Add(defaultCategory);

            CreateInstanceMembersForVariables(element, defaultCategory);

            foreach (var category in categories)
            {
                const byte brightness = 227;
                category.SetAlternatingColors(new SolidColorBrush(Color.FromRgb(brightness, brightness, brightness)), Brushes.Transparent);

                grid.Categories.Add(category);
            }

            grid.Refresh();

        }

        private static void CreateInstanceMembersForVariables(IElement element, MemberCategory category)
        {
            foreach (CustomVariable variable in element.CustomVariables)
            {
                Type type = variable.GetRuntimeType();
                if (type == null)
                {
                    type = typeof(string);
                }

                string name = variable.Name;
                //object value = variable.DefaultValue;

                // todo - do something with converter

                var instanceMember = new DataGridItem();
                instanceMember.CustomGetTypeEvent += (throwaway) => type;
                string displayName = StringFunctions.InsertSpacesInCamelCaseString(name);

                // Currently this only works on TextBox variables - eventually will expand
                instanceMember.DetailText = variable.Summary;
                
                instanceMember.DisplayName = displayName;
                instanceMember.UnmodifiedVariableName = name;

                TypeConverter converter = variable.GetTypeConverter(element);
                instanceMember.TypeConverter = converter;
                
                instanceMember.CustomSetEvent += (intance, value) =>
                {
                    instanceMember.IsDefault = false;

                    RefreshLogic.IgnoreNextRefresh();


                    var oldValue = variable.DefaultValue;

                    variable.DefaultValue = value;

                    EditorObjects.IoC.Container.Get<CustomVariableSaveSetVariableLogic>().ReactToCustomVariableChangedValue(
                        "DefaultValue", variable, oldValue);



                    GlueCommands.Self.GluxCommands.SaveGlux();

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                };

                instanceMember.CustomGetEvent += (instance) =>
                    {
                        var foundVariable = element.GetCustomVariableRecursively(name);
                        return foundVariable?.DefaultValue;
                    };

                instanceMember.IsDefaultSet += (owner, args) =>
                {
                    
                    element.GetCustomVariableRecursively(name).DefaultValue = null;


                    GlueCommands.Self.GluxCommands.SaveGlux();

                    GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                };

                instanceMember.SetValueError = (newValue) =>
                {
                    if (newValue is string && string.IsNullOrEmpty(newValue as string))
                    {
                        element.GetCustomVariableRecursively(name).DefaultValue = null;

                        GlueCommands.Self.GluxCommands.SaveGlux();

                        GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                    }
                };

                instanceMember.ContextMenuEvents.Add("Variable Properties", (sender, args) => GlueState.Self.CurrentCustomVariable = variable);

                category.Members.Add(instanceMember);
            }
        }


    }
}
