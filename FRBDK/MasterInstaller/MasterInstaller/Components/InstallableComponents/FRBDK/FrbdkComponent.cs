using System;
using System.Collections.Generic;
using MasterInstaller.Components.MainComponents.SetupType;
using MasterInstaller.Managers;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;


namespace MasterInstaller.Components.InstallableComponents.FRBDK
{
    public class FrbdkComponent : InstallableComponentBase
    {
        private const string ExecutableName = "FRBDKUpdater.exe";

        public FrbdkComponent()
        {
            var control = new DefaultInstallControl {InstallName = Name, InstallDescription = Description};

            Control = control;
        }

        public override ComponentBase PreviousComponent
        {
            get { throw new System.NotImplementedException(); }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.FileAssociationComponent; }
        }

        public override string Key
        {
            get { return "FRBDK"; }
        }

        public override sealed string Name
        {
            get { return "FRBDK"; }
        }

        public override sealed string Description
        {
            get { return "The game development tools that are included with FlatRedBall."; }
        }

        public override bool IsTypical
        {
            get { return true; }
        }

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            List<string> dependencies = new List<string>();
            dependencies.Add("FlatRedBall.dll");
            dependencies.Add("Ionic.Zip.dll");

            switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
            {
                case SetupTypeComponent.SetupType.Typical:
                    if (IsTypical)
                    {
                        PerformInstall(dependencies);
                    }
                    break;
                case SetupTypeComponent.SetupType.Complete:
                    PerformInstall(dependencies);
                    break;
                case SetupTypeComponent.SetupType.Custom:
                    if (ComponentStorage.GetValue<bool>(Key))
                    {
                        PerformInstall(dependencies);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            OnMoveToNext();
        }

        private void PerformInstall(List<string> dependencies)
        {
            Install(new ExecutableDetails { ExecutableName = ExecutableName, AdditionalFiles = dependencies, ExtraLogic = FrbdkUpdaterManager.Self.CreateUpdaterFileAndAddShortcutFiles });


        }







    }
}
