using MasterInstaller.Components.MainComponents.SetupType;

namespace MasterInstaller.Components.MainComponents.CustomSetup
{
    public class CustomSetupComponent : ComponentBase
    {
        public CustomSetupComponent()
        {
            Control = new CustomSetupControl();
        }

        public override ComponentBase PreviousComponent
        {
            get { return ComponentStorage.SetupTypeComponent; }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.FrbdkSetupComponent; }
        }

        protected override bool PreviousButtonEnabledByDefault
        {
            get { return true; }
        }

        protected override bool NextButtonEnabledByDefault
        {
            get { return true; }
        }

        public override bool MovingNextFromComponent()
        {
            var control = Control as CustomSetupControl;

            foreach (var component in ComponentStorage.InstallableComponents)
            {
                ComponentStorage.SetValue(component.Key, control.lvComponents.Items[component.Key].Checked);
            }

            return true;
        }

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
            {
                case SetupTypeComponent.SetupType.Custom:
                    break;

                default:
                    OnMoveToNext();
                    break;
            }
        }
    }
}
