using System;
using MasterInstaller.Components.MainComponents.Introduction;
using MasterInstaller.Components.MainComponents.SetupType;

namespace MasterInstaller.Components.MainComponents.BeginInstall
{
    public class BeginInstallComponent : ComponentBase
    {
        public BeginInstallComponent()
        {
            Control = new BeginInstallControl();
        }

        public override ComponentBase PreviousComponent
        {
            get
            {
                switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
                {
                    case SetupTypeComponent.SetupType.Typical:
                        return ComponentStorage.SetupTypeComponent;

                    case SetupTypeComponent.SetupType.Complete:
                        return ComponentStorage.SetupTypeComponent;

                    case SetupTypeComponent.SetupType.Custom:
                        return ComponentStorage.FrbdkSetupComponent;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.DotNet4Component; }
        }

        protected override bool PreviousButtonEnabledByDefault
        {
            get { return true; }
        }

        protected override bool NextButtonEnabledByDefault
        {
            get { return true; }
        }

        protected override string NextButtonString
        {
            get { return "Begin"; }
        }


        public override void MovedToComponent()
        {
            base.MovedToComponent();

            var key = ComponentStorage.GetValue<string>(IntroductionComponent.SkipTo);

            foreach (var installableComponentBase in ComponentStorage.InstallableComponents)
            {
                if (installableComponentBase.Key == key)
                {
                    ComponentStorage.SetValue(IntroductionComponent.SkipTo, "");
                    OnMoveTo(installableComponentBase);
                    break;
                }
            }
        }
    }
}
