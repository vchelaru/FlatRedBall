using MasterInstaller.Components;
using MasterInstaller.Components.InstallableComponents.FRBDK;
using MasterInstaller.Components.MainComponents.Completed;
using MasterInstaller.Components.MainComponents.ComponentInstallation;
using MasterInstaller.Components.MainComponents.CustomSetup;
using MasterInstaller.Components.MainComponents.Introduction;
using MasterInstaller.Components.MainComponents.VisualStudioInformation;
using MasterInstaller.Managers;
using MasterInstallerWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MasterInstaller
{
    public class MainFlow
    {
        public MainWindow MainForm { get; internal set; }

        public void StartFlow()
        {
            var component = new IntroductionComponent();
            MainForm.SetComponent(component);

            component.NextClicked += delegate
            {
                GoToVisualStudioInfo();
            };

        }

        private void GoToVisualStudioInfo()
        {
            var component = new VisualStudioInfoComponent();

            component.NextClicked += delegate
            {
                GoToComponentSelection();
            };

            MainForm.SetComponent(component);
            

        }

        private void GoToComponentSelection()
        {
            var component = new CustomSetupComponent();
            component.NextClicked += HandleComponentSelection;
            MainForm.SetComponent(component);
        }

        private void HandleComponentSelection(object sender, EventArgs args)
        {
            var component = sender as CustomSetupComponent;



            GoToInstallComponents();
        }

        private void GoToInstallComponents()
        {
            var component = new ComponentInstallationComponent();

            component.NextClicked += delegate
            {
                GoToFrbdkAssociation();
            };

            MainForm.SetComponent(component);
        }

        private void GoToFrbdkAssociation()
        {
            // We don't ask, we just do:
            FileAssociationManager.SetAllFileAssociations();

            GoToEnd();

        }

        private void GoToEnd()
        {

            var component = new CompletedComponent();

            component.NextClicked += delegate
            {
                // exit?
                MainForm.Close();
            };

            MainForm.SetComponent(component);
        }
    }
}
