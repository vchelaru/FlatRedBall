using System;
using MasterInstaller.Components.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MasterInstaller.Components.InstallableComponents.DirectX
{
    public class DirectXComponent : InstallableComponentBase
    {
        protected override BasePage CreateControl()
        {
            throw new NotImplementedException();
        }

        public DirectXComponent()
        {
            //var control = new DefaultInstallControl {InstallName = Name, InstallDescription = Description};

            //Control = control;
        }

        public override string Key
        {
            get { return "DirectX"; }
        }

        public override sealed string Name
        {
            get { return "DirectX"; }
        }

        public override sealed string Description
        {
            get { return "The latest version of DirectX is needed for some of FRBDK's tools."; }
        }

        public override bool IsTypical
        {
            get { return true; }
        }

        public override async Task<int> Install()
        {
            const string ExecutableName =
       "DXSETUP.exe";


            List<string> dependencies = new List<string>();
            dependencies.Add("APR2007_d3dx9_33_x86.cab");
            dependencies.Add("APR2007_xinput_x86.cab");
            dependencies.Add("DSETUP.dll");
            dependencies.Add("dsetup32.dll");

            dependencies.Add("dxupdate.cab");
            dependencies.Add("Feb2010_X3DAudio_x86.cab");
            dependencies.Add("Feb2010_xact_x86.cab");
            dependencies.Add("Feb2010_XAudio_x86.cab");
            dependencies.Add("Mar2009_d3dx9_41_x86.cab");

            var details =
                new ExecutableDetails
                {
                    ExecutableName = ExecutableName,
                    AdditionalFiles = dependencies
                };

            var result = await Install(details);

            return result;
        }

        //public override void MovedToComponent()
        //{
        //    base.MovedToComponent();

        //    switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
        //    {
        //        case SetupTypeComponent.SetupType.Typical:
        //            if (IsTypical)
        //            {
        //                Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new [] { "/Q" } });
        //            }
        //            break;
        //        case SetupTypeComponent.SetupType.Complete:
        //            Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/Q" } });
        //            break;
        //        case SetupTypeComponent.SetupType.Custom:
        //            if (ComponentStorage.GetValue<bool>(Key))
        //            {
        //                Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/Q" } });
        //            }
        //            break;
        //        default:
        //            throw new NotImplementedException();
        //    }

        //    OnMoveToNext();
        //}
    }
}
