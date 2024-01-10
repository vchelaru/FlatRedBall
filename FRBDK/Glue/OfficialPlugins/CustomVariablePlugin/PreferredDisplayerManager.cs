using FlatRedBall.Glue.Elements;
using OfficialPlugins.Common.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;
using WpfDataUi;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.CustomVariablePlugin;

internal class PreferredDisplayerManager
{
    static List<InstanceMember> SelectedDisplayerMembers = new List<InstanceMember>();


    public static void AddDisplayerUi(DataUiGrid grid, CustomVariable variable)
    {
        var member = grid.Categories
            .SelectMany(item => item.Members)
            .FirstOrDefault(item => item.Name == nameof(VariableDefinition.PreferredDisplayer));

        if (member != null)
        {
            member.CustomOptions = new List<object>
        {
            typeof(AngleSelectorDisplay),
            typeof(FileSelectionDisplay),
            typeof(LocalizationStringIdComboBox),
            typeof(MultiLineTextBoxDisplay),
            typeof(PlusMinusTextBox),
            typeof(SliderDisplay),
            typeof(TextBoxDisplay),
            // If adding something here, be sure to update TryAssignPreferredDisplayerFromName to handle the type too

        };

            member.CustomSetPropertyEvent += (instance, args) =>
            {
                var value = args.Value;
                var variableDefinition = instance as VariableDefinition;
                if (value is Type type)
                {
                    variableDefinition.PreferredDisplayer = type;
                }
                else
                {
                    variableDefinition.PreferredDisplayer = null;
                }

                RefreshSelectedDisplayMembers(grid, variable);
            };
        }
    }

    internal static void TryAssignPreferredDisplayerFromName(CustomVariable variable)
    {

        var type = variable.VariableDefinition.PreferredDisplayerName;

        if (TryHandle<AngleSelectorDisplay>()) { }
        else if (TryHandle<FileSelectionDisplay>()) { }
        else if (TryHandle<LocalizationStringIdComboBox>()) { }
        else if (TryHandle<MultiLineTextBoxDisplay>()) { }
        else if (TryHandle<PlusMinusTextBox>()) { }
        else if (TryHandle<SliderDisplay>()) { }
        else if (TryHandle<TextBoxDisplay>()) { }


        bool TryHandle<T>()
        {
            if (type == typeof(T).FullName)
            {
                variable.VariableDefinition.PreferredDisplayer = typeof(T);
                return true;
            }
            return false;
        }
    }

    public static void RefreshSelectedDisplayMembers(DataUiGrid grid, CustomVariable variable)
    {
        foreach (var member in SelectedDisplayerMembers)
        {
            grid.Categories.FirstOrDefault()?.Members.Remove(member);
        }

        SelectedDisplayerMembers.Clear();

        var type = variable.VariableDefinition?.PreferredDisplayer;

        if (type == typeof(SliderDisplay))
        {
            Add<double>(nameof(SliderDisplay.MinValue));
            Add<double>(nameof(SliderDisplay.MaxValue));
        }
        else if (type == typeof(AngleSelectorDisplay))
        {
            Add<AngleType>(nameof(AngleSelectorDisplay.TypeToPushToInstance));
        }


        void Add<T>(string propertyName)
        {
            var member = new InstanceMember();
            member.Name = propertyName;
            member.CustomGetTypeEvent += (_) => typeof(T);
            member.CustomSetPropertyEvent += (_, args) =>
            {
                var newValue = args.Value;
                variable.VariableDefinition.PropertiesToSetOnDisplayer[propertyName] = newValue;
            };
            member.CustomGetEvent += (_) =>
            {
                if (variable.VariableDefinition.PropertiesToSetOnDisplayer.TryGetValue(propertyName, out object value))
                {
                    return value;
                }
                else
                {
                    return default(T);
                }
            };

            SelectedDisplayerMembers.Add(member);
            grid.Categories[0].Members.Add(member);
        }

        grid.InsertSpacesInCamelCaseMemberNames();
    }
}
