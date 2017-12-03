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

            if (viewModel.AddDll)
            {
                AddDllRadio.IsChecked = true;
                EmbedCodeFilesRadio.IsChecked = false;
                IncludeNoFilesRadio.IsChecked = false;
            }
            else if(viewModel.EmbedCodeFiles)
            {
                EmbedCodeFilesRadio.IsChecked = true;
                AddDllRadio.IsChecked = false;
                IncludeNoFilesRadio.IsChecked = false;
            }
            else if(viewModel.IncludeNoFiles)
            {
                IncludeNoFilesRadio.IsChecked = true;
                EmbedCodeFilesRadio.IsChecked = false;
                AddDllRadio.IsChecked = false;
            }
        }

        private void HandleGenerateBehaviors(object sender, RoutedEventArgs e)
        {
            bool didAdd = false;

            didAdd = AddIfDoesntHave(BehaviorGenerator.CreateButtonBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateToggleBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateRadioButtonBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateTextBoxBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollBarBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollViewerBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateListBoxItemBehavior());
            didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateComboBoxBehavior());

            if(didAdd)
            {
                AppCommands.Self.SaveGumx();
            }
        }

        private bool AddIfDoesntHave(BehaviorSave behaviorSave)
        {
            var project = AppState.Self.GumProjectSave;

            bool doesProjectAlreadyHaveBehavior =
                project.Behaviors.Any(item => item.Name == behaviorSave.Name);

            if(!doesProjectAlreadyHaveBehavior)
            {
                AppCommands.Self.AddBehavior(behaviorSave);
                AppCommands.Self.SaveBehavior(behaviorSave);
            }

            return doesProjectAlreadyHaveBehavior == false;
        }
    }
}
