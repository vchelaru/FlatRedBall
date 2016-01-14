using System;
using MasterInstaller.Components.MainComponents.SetupType;

namespace MasterInstaller.Components.InstallableComponents.DirectX
{
    public class DirectXComponent : InstallableComponentBase
    {
        private const string ExecutableName = "dxwebsetup.exe";

        public DirectXComponent()
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
            get { return ComponentStorage.Xna3_1Component; }
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

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
            {
                case SetupTypeComponent.SetupType.Typical:
                    if (IsTypical)
                    {
                        Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new [] { "/Q" } });
                    }
                    break;
                case SetupTypeComponent.SetupType.Complete:
                    Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/Q" } });
                    break;
                case SetupTypeComponent.SetupType.Custom:
                    if (ComponentStorage.GetValue<bool>(Key))
                    {
                        Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/Q" } });
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            OnMoveToNext();
        }
    }
}
