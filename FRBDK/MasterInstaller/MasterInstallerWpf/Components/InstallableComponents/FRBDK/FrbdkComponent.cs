using System;
using System.Collections.Generic;
using MasterInstaller.Managers;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using MasterInstaller.Components.SetupComponents.FrbdkSetup;
using MasterInstaller.Components.Controls;
using System.Threading.Tasks;

namespace MasterInstaller.Components.InstallableComponents.FRBDK
{
    public class FrbdkComponent : InstallableComponentBase
    {
        private const string ExecutableName = "FRBDKUpdater.exe";

        public FrbdkComponent()
        {
        }


        protected override BasePage CreateControl()
        {
            throw new NotImplementedException();
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

        //public override void MovedToComponent()
        //{
        //    base.MovedToComponent();

        //    List<string> dependencies = new List<string>();
        //    dependencies.Add("FlatRedBall.dll");
        //    dependencies.Add("Ionic.Zip.dll");
        //    dependencies.Add("FlatRedBall.Tools.dll");

        //    switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
        //    {
        //        case SetupTypeComponent.SetupType.Typical:
        //            if (IsTypical)
        //            {
        //                PerformInstall(dependencies);
        //            }
        //            break;
        //        case SetupTypeComponent.SetupType.Complete:
        //            PerformInstall(dependencies);
        //            break;
        //        case SetupTypeComponent.SetupType.Custom:
        //            if (ComponentStorage.GetValue<bool>(Key))
        //            {
        //                PerformInstall(dependencies);
        //            }
        //            break;
        //        default:
        //            throw new NotImplementedException();
        //    }

        //    OnMoveToNext();
        //}

        public override async Task<int> Install()
        {
            List<string> dependencies = new List<string>();
            dependencies.Add("FlatRedBall.dll");
            dependencies.Add("Ionic.Zip.dll");
            dependencies.Add("FlatRedBall.Tools.dll");

            return await Install(new ExecutableDetails
            {
                ExecutableName = ExecutableName,
                AdditionalFiles = dependencies,
                ExtraLogic = FrbdkUpdaterManager.Self.CreateUpdaterFileAndAddShortcutFiles,
                RunAsAdministrator = true
            });

        }
        

    }
}
