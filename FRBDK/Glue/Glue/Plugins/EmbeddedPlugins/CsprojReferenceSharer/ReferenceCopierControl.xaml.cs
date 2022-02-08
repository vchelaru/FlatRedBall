using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    /// <summary>
    /// Interaction logic for ReferenceCopierControl.xaml
    /// </summary>
    public partial class ReferenceCopierControl : UserControl
    {
        ReferenceCopierViewModel ViewModel
        {
            get
            {
                return this.DataContext as ReferenceCopierViewModel;
            }
        }

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


                var strippedSource = FileManager.RemovePath(sourceProject);

                var found = destinationSolution.ReferencedProjects.FirstOrDefault(item => 
                    FileManager.RemovePath(item) == strippedSource);



                if(found != null)
                {

                    ViewModel.FromFile = FileManager.RemoveDotDotSlash(sourceDirectory + sourceProject);
                    ViewModel.ToFile = FileManager.RemoveDotDotSlash( destinationDirectory + found);

                    ViewModel.PerformCopy(showPopup:false);

                    PluginManager.ReceiveOutput($"Copied references for project {strippedSource} in {strippedDestinationSolutionName}.");

                }
                else
                {
                    PluginManager.ReceiveOutput($"Skipping project {strippedSource} because a matching project was not found in {strippedDestinationSolutionName}");
                }
            }
        }

        private List<VSSolution> GetSyncedSolutions()
        {
            List<VSSolution> toReturn = new List<VSSolution>();

            foreach (var project in GlueState.Self.SyncedProjects)
            {
                var syncedSolutionFileName = ProjectSyncer.LocateSolution(project.FullFileName.FullPath);

                var syncedSolution = VSSolution.FromFile(syncedSolutionFileName);

                toReturn.Add(syncedSolution);
            }

            return toReturn;
        }

        private VSSolution GetMainSolution()
        {
            var mainSln = GlueState.Self.CurrentSlnFileName;

            var solution = VSSolution.FromFile(mainSln);

            return solution;
        }
    }
}
