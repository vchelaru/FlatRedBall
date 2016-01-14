using System;
using MasterInstaller.Components.MainComponents.SetupType;

namespace MasterInstaller.Components.InstallableComponents.XNA3_1
{
    public class XNA3_1Component : InstallableComponentBase
    {
        private const string ExecutableName = "xnafx31_redist.msi";

        public XNA3_1Component()
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
            get { return ComponentStorage.Xna4Component; }
        }

        public override string Key
        {
            get { return "XNA3_1"; }
        }

        public override sealed string Name
        {
            get { return "XNA 3.1"; }
        }

        public override sealed string Description
        {
            get { return "XNA 3.1 is needed for some of FRBDK's tools."; }
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
                        Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new [] {"/passive"} });
                    }
                    break;
                case SetupTypeComponent.SetupType.Complete:
                    Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/passive" } });
                    break;
                case SetupTypeComponent.SetupType.Custom:
                    if (ComponentStorage.GetValue<bool>(Key))
                    {
                        Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/passive" } });
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            OnMoveToNext();
        }
    }
}
