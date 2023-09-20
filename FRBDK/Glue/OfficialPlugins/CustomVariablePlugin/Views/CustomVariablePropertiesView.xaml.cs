using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.EventArguments;

namespace OfficialPlugins.CustomVariablePlugin.Views;

/// <summary>
/// Interaction logic for CustomVariablePropertiesView.xaml
/// </summary>
public partial class CustomVariablePropertiesView : UserControl
{
    CustomVariable Variable => MainGrid.Instance as CustomVariable;

    public CustomVariablePropertiesView()
    {
        InitializeComponent();
        MainGrid.PropertyChange += HandleMainGridPropertyChanged;
        VariableDefinitionGrid.PropertyChange += HandleVariableDefinitionPropertyChanged;
    }

    private void AddVariableDefinitionClick(object sender, RoutedEventArgs e)
    {
        Variable.VariableDefinition = new FlatRedBall.Glue.Elements.VariableDefinition();

        RefreshVariableDefinitionGrid(Variable);

        SaveElement();
    }

    private void HandleMainGridPropertyChanged(string arg1, PropertyChangedArgs args)
    {
        SaveElement();
    }

    private void HandleVariableDefinitionPropertyChanged(string arg1, PropertyChangedArgs args)
    {
        SaveElement();
    }

    private void SaveElement()
    {
        var element = ObjectFinder.Self.GetElementContaining(Variable);

        if(element != null)
        {
            _ = GlueCommands.Self.GluxCommands.SaveElementAsync(element);
        }
    }

    public void RefreshAll(CustomVariable customVariable)
    {
        MainGrid.Instance = customVariable;
        //MainGrid.PropertyChange += HandlePropertyChanged;

        foreach (var category in MainGrid.Categories)
        {
            category.Members.RemoveAll(item => item.Name != nameof(VariableDefinition.PreferredDisplayer));
        }

        MainGrid.Categories.RemoveAll(item => item.Members.Count == 0);

        //RemoveMember(nameof(CustomVariable.Properties));
        //RemoveMember(nameof(CustomVariable.DefaultValue));
        //RemoveMember(nameof(CustomVariable.VariableDefinition));

        void RemoveMember(string memberName)
        {
            foreach (var category in MainGrid.Categories)
            {
                category.Members.RemoveAll(member => member.Name == memberName);
            }
        }

        MainGrid.InsertSpacesInCamelCaseMemberNames();

        RefreshVariableDefinitionGrid(customVariable);
    }


    private void RefreshVariableDefinitionGrid(CustomVariable customVariable)
    {
        this.VariableDefinitionGrid.Instance = customVariable.VariableDefinition;

        RemoveMember(nameof(VariableDefinition.Name));
        RemoveMember(nameof(VariableDefinition.Type));
        RemoveMember(nameof(VariableDefinition.MinValue));
        RemoveMember(nameof(VariableDefinition.MaxValue));
        RemoveMember(nameof(VariableDefinition.Category));
        RemoveMember(nameof(VariableDefinition.DefaultValue));
        RemoveMember(nameof(VariableDefinition.ForcedOptions));
        RemoveMember(nameof(VariableDefinition.HasGetter));
        RemoveMember(nameof(VariableDefinition.PreferredDisplayerName));
        RemoveMember(nameof(VariableDefinition.PropertiesToSetOnDisplayer));
        RemoveMember(nameof(VariableDefinition.UsesCustomCodeGeneration));

        PreferredDisplayerManager.AddDisplayerUi(VariableDefinitionGrid, Variable);
        PreferredDisplayerManager.RefreshSelectedDisplayMembers(VariableDefinitionGrid, Variable);

        if(VariableDefinitionGrid.Categories?.Count > 0)
        {
            VariableDefinitionGrid.Categories[0].Name = "Variable Display";

            var member = VariableDefinitionGrid.Categories
                .SelectMany(item => item.Members)
                .FirstOrDefault(item => item.Name == nameof(VariableDefinition.PreferredDisplayer));


            VariableDefinitionGrid.InsertSpacesInCamelCaseMemberNames();
        }

        this.AddVariableDefinitionButton.Visibility = (customVariable.VariableDefinition == null).ToVisibility();

        void RemoveMember(string memberName)
        {
            foreach (var category in VariableDefinitionGrid.Categories)
            {
                category.Members.RemoveAll(member => member.Name == memberName);
            }
        }
    }
}
