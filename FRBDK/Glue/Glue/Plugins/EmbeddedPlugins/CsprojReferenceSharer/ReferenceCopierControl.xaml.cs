using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    /// <summary>
    /// Interaction logic for ReferenceCopierControl.xaml
    /// </summary>
    public partial class ReferenceCopierControl : UserControl
    {
        ReferenceCopierViewModel ViewModel => this.DataContext as ReferenceCopierViewModel;

        public ReferenceCopierControl()
        {
            InitializeComponent();
        }

        private void HandleAutomaticallyCopyClick(object sender, RoutedEventArgs e)
        {
            VSSolution mainSolution = GetMainSolution();
            List<VSSolution> syncedSolutions = GetSyncedSolutions();

            foreach(var solution in syncedSolutions)
            {
                AutomaticallyUpdateProjects(mainSolution, solution);
            }

        }

        private void AutomaticallyUpdateProjects(VSSolution sourceSolution, VSSolution destinationSolution)
        {
            ReferenceCopierViewModel viewmodel = new ReferenceCopierViewModel();

            string sourceDirectory = FileManager.GetDirectory(sourceSolution.FullFileName);
            string destinationDirectory = FileManager.GetDirectory(destinationSolution.FullFileName);

            string strippedDestinationSolutionName = FileManager.RemovePath(destinationSolution.FullFileName);

            foreach (var sourceProject in sourceSolution.ReferencedProjects)
            {

                var projectName = sourceProject.Name;
                var strippedSource = FileManager.RemovePath(projectName);

                var found = destinationSolution.ReferencedProjects.FirstOrDefault(item => 
                    FileManager.RemovePath(item.Name) == strippedSource);



                if(found != null)
                {

                    ViewModel.FromFile = FileManager.RemoveDotDotSlash(sourceDirectory + sourceProject);
                    ViewModel.ToFile = FileManager.RemoveDotDotSlash( destinationDirectory + found);

                    ViewModel.PerformCopy(showPopup:false);

                    PluginManager.ReceiveOutput(String.Format(L.Texts.CopiedReferencesForProjectAInB, strippedSource, strippedDestinationSolutionName));

                }
                else
                {
                    PluginManager.ReceiveOutput(String.Format(L.Texts.SkippingProjectNotFound, strippedSource, strippedDestinationSolutionName));
                }
            }
        }

        private static List<VSSolution> GetSyncedSolutions()
        {
            return GlueState.Self.SyncedProjects
                .Select(project => VSSolution.FromFile(ProjectSyncer.LocateSolution(project.FullFileName.FullPath)))
                .ToList();
        }

        private static VSSolution GetMainSolution()
        {
            var mainSln = GlueState.Self.CurrentSlnFileName;

            var solution = VSSolution.FromFile(mainSln);

            return solution;
        }
    }
}
