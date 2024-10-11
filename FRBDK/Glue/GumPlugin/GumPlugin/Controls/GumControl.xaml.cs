using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using Gum.DataTypes.Behaviors;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using GumPlugin.ViewModels;
using GumPlugin.Managers;
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
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace GumPlugin.Controls
{
    /// <summary>
    /// Interaction logic for GumControl.xaml
    /// </summary>
    public partial class GumControl : UserControl
    {
        public event Action RebuildFontsClicked;
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

            if(viewModel != null)
            {
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
        }

        private async void HandleAddAllForms(object sender, RoutedEventArgs e)
        {
            var project = GlueState.Self.CurrentMainProject;
            var response = GetWhyAddingFormsIsNotSupported(project);

            if (!string.IsNullOrEmpty(response.Message))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(response.Message);
            }

            var assembly = typeof(FormsControlAdder).Assembly;

            var shouldSave = FormsControlAdder.AskToSaveIfOverwriting(assembly);
            if(!shouldSave)
            {
                response.Succeeded = false;
            }

            if(response.Succeeded)
            {
                var viewModel = DataContext as GumViewModel;

                viewModel.IncludeFormsInComponents = true;
                viewModel.IncludeComponentToFormsAssociation = true;
                await FormsControlAdder.SaveElements(assembly);
                await FormsControlAdder.SaveBehaviors(assembly);
            }
        }

        private async void HandleGenerateBehaviors(object sender, RoutedEventArgs args) =>
                await FormsControlAdder.SaveBehaviors(typeof(FormsControlAdder).Assembly);


        private GeneralResponse GetWhyAddingFormsIsNotSupported(ProjectBase project)
        {
            GeneralResponse response = GeneralResponse.SuccessfulResponse;

            if (project == null)
            {
                response.Succeeded = false;
                response.Message = "You must load a project before adding Gum";
            }
            else if (project is IosMonogameProject)
            {
                response.Succeeded = false;
                response.Message = "FlatRedBall.Forms is not yet supported on iOS. Complain in chat!";
            }
            else if (project is Xna4Project)
            {
                // Just tell the user:
                response.Succeeded = true;
                response.Message = "Forms is being added, but you may need to manually add .dlls to your project.";
            }
            return response;
        }

        private void HandleAddFormsComponentsClick(object sender, RoutedEventArgs e)
        {
            var assembly = typeof(FormsControlAdder).Assembly;

            var shouldSave = FormsControlAdder.AskToSaveIfOverwriting(assembly);
            if(shouldSave)
            {
                _ = FormsControlAdder.SaveElements(assembly);
            }
        }

        private void RegenerateFontsClicked(object sender, RoutedEventArgs e)
        {
            RebuildFontsClicked?.Invoke();
        }

        public void RemoveOrphanCustomCodeClicked(object sender, RoutedEventArgs e)
        {
            var codeProject = (VisualStudioProject) GlueState.Self.CurrentMainProject.CodeProject;

            List<Microsoft.Build.Evaluation.ProjectItem> itemsToRemove = 
                new List<Microsoft.Build.Evaluation.ProjectItem>();

            foreach(var item in codeProject.EvaluatedItems)
            {
                var name = item.EvaluatedInclude?.ToLowerInvariant();
                // continue here:
                var shouldRemove = !string.IsNullOrEmpty(name) &&
                    name.StartsWith("gumruntimes\\") &&
                    name.EndsWith("runtime.cs");

                if(shouldRemove)
                {
                    // see if there is a matching generated file
                    var nameToLookFor = item.EvaluatedInclude.Substring(0, item.EvaluatedInclude.Length - ".cs".Length) +
                        ".Generated.cs";

                    nameToLookFor = nameToLookFor.ToLowerInvariant();

                    var matching = codeProject.EvaluatedItems.Any(item => item.EvaluatedInclude?.ToLowerInvariant() == nameToLookFor);

                    shouldRemove = matching == false;
                }

                if(shouldRemove)
                {
                    itemsToRemove.Add(item);
                }
            }

            if(itemsToRemove.Count > 0)
            {
                GlueCommands.Self.DialogCommands.FocusTab("Output");
                foreach(var item in itemsToRemove)
                {
                    GlueCommands.Self.PrintOutput("Removed " + item.EvaluatedInclude);

                    codeProject.RemoveItem(item);
                }
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < 5; i++)
            {
                TaskManager.Self.Add(() =>
                {
                    TaskManager.Self.WarnIfNotInTask();
                    System.Threading.Thread.Sleep(1000);

                    TaskManager.Self.AddOrRunIfTasked(() =>
                    {
                        System.Threading.Thread.Sleep(1000);

                    }, "Inner");


                }, "Doing stuff " + i + DateTime.Now, doOnUiThread: true);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                TaskManager.Self.Add(async () =>
                {
                    TaskManager.Self.WarnIfNotInTask();
                    await Task.Delay(1000);

                    TaskManager.Self.AddOrRunIfTasked(() =>
                    {
                        System.Threading.Thread.Sleep(1000);

                    }, "Inner");
                }, "Doing async stuff " + i + DateTime.Now, doOnUiThread: true);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
                TaskManager.Self.Add(() =>
                {
                    TaskManager.Self.WarnIfNotInTask();
                    System.Threading.Thread.Sleep(1000);

                    TaskManager.Self.AddOrRunIfTasked(() =>
                    {
                        System.Threading.Thread.Sleep(1000);

                    }, "Inner");
                }, "Single task at " + DateTime.Now, doOnUiThread: true);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            TaskManager.Self.Add(async () =>
            {
                TaskManager.Self.WarnIfNotInTask();
                await Task.Delay(1000);

                TaskManager.Self.AddOrRunIfTasked(() =>
                {
                    System.Threading.Thread.Sleep(1000);

                }, "Inner");
            }, "Single async task " + DateTime.Now, doOnUiThread:true);
        }
    }
}
