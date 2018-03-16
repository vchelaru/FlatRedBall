using FlatRedBall.Glue.Managers;
using Gum.DataTypes.Behaviors;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using GumPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GumPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GumControl.xaml
    /// </summary>
    public partial class GumControl : UserControl
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public GumControl()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ManuallyRefreshRadioButtons();
        }

        public void ManuallyRefreshRadioButtons()
        {
            // don't know why, but this doesn't update like it should:
            var viewModel = DataContext as GumViewModel;

            if(viewModel.EmbedCodeFiles)
            {
                EmbedCodeFilesRadio.IsChecked = true;
                IncludeNoFilesRadio.IsChecked = false;
            }
            else if(viewModel.IncludeNoFiles)
            {
                IncludeNoFilesRadio.IsChecked = true;
                EmbedCodeFilesRadio.IsChecked = false;
            }
        }

        private void HandleAddAllForms(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GumViewModel;

            viewModel.IncludeFormsInComponents = true;
            viewModel.IncludeComponentToFormsAssociation = true;
            HandleGenerateBehaviors(this, null);
            HandleAddFormsComponentsClick(this, null);
        }

        private void HandleGenerateBehaviors(object sender, RoutedEventArgs e)
        {
            TaskManager.Self.AddSync(() =>
           {
               bool didAdd = false;

               didAdd = AddIfDoesntHave(BehaviorGenerator.CreateButtonBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateCheckBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateComboBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateListBoxItemBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateListBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateRadioButtonBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollBarBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollViewerBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateSliderBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateTextBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateToggleBehavior());

               if (didAdd)
               {
                   AppCommands.Self.SaveGumx();
               }
           }, "Adding Gum Forms Behaviors");
        }

        private void HandleAddFormsComponentsClick(object sender, RoutedEventArgs e)
        {
            FormsControlAdder.SaveComponents(typeof(FormsControlAdder).Assembly);
        }

        private bool AddIfDoesntHave(BehaviorSave behaviorSave)
        {
            var project = AppState.Self.GumProjectSave;

            bool doesProjectAlreadyHaveBehavior =
                project.Behaviors.Any(item => item.Name == behaviorSave.Name);

            if(!doesProjectAlreadyHaveBehavior)
            {
                AppCommands.Self.AddBehavior(behaviorSave);
            }
            // in case it's changed, or in case the user has somehow corrupted their behavior, force save it
            AppCommands.Self.SaveBehavior(behaviorSave);

            return doesProjectAlreadyHaveBehavior == false;
        }

        private void AdvancedClick(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GumViewModel;
            viewModel.ShowAdvanced = true;
        }

        private void SimpleClick(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as GumViewModel;
            viewModel.ShowAdvanced = false;
        }
    }
}
