using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using Gum.DataTypes.Behaviors;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using GumPlugin.ViewModels;
using HQ.Util.Unmanaged;
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
            var project = GlueState.Self.CurrentMainProject;
            var error = GetWhyAddingMonoGameIsNotSupported(project);

            if(!string.IsNullOrEmpty(error))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(error);
            }
            else
            {
                var viewModel = DataContext as GumViewModel;

                viewModel.IncludeFormsInComponents = true;
                viewModel.IncludeComponentToFormsAssociation = true;
                HandleGenerateBehaviors(this, null);
                HandleAddFormsComponentsClick(this, null);
            }
        }

        private string GetWhyAddingMonoGameIsNotSupported(ProjectBase project)
        {
            string errorMessage = null;

            if (project == null)
            {
                errorMessage = "You must load a project before adding Gum";
            }
            else if (project is IosMonogameProject)
            {
                errorMessage = "FlatRedBall.Forms is not yet supported on iOS. Complain in chat!";
            }
            else if (project is Xna4Project)
            {
                errorMessage = "FlatRedBall.Forms is not supported for FlatRedBall XNA projects.";
            }
            return errorMessage;
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
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreatePasswordBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateRadioButtonBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollBarBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateScrollViewerBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateSliderBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateTextBoxBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateToggleBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateTreeViewBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateTreeViewItemBehavior());
               didAdd |= AddIfDoesntHave(BehaviorGenerator.CreateUserControlBehavior());

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

        private void RegenerateFontsClicked(object sender, RoutedEventArgs e)
        {
            // --rebuildfonts "C:\Users\Victor\Documents\TestProject2\TestProject2\Content\GumProject\GumProject.gumx"
            var gumFileName = AppState.Self.GumProjectSave.FullFileName;

            var executable = FileAssociation.GetExecFileAssociatedToExtension("gumx");

            if(string.IsNullOrEmpty(executable))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(
                    "Could not find file association for Gum files - you need to set this up before performing this operation");
            }
            else
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.Arguments = $@"--rebuildfonts ""{gumFileName}""";
                startInfo.FileName = executable;
                startInfo.UseShellExecute = false;

                System.Diagnostics.Process.Start(startInfo);

            }
        }
    }
}
