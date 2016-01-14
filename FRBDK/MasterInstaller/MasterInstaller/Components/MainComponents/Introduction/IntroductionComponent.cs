namespace MasterInstaller.Components.MainComponents.Introduction
{
    public class IntroductionComponent : ComponentBase
    {
        public IntroductionComponent()
        {
            Control = new IntroductionControl();
        }

        public override ComponentBase PreviousComponent
        {
            get { return null; }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.SetupTypeComponent; }
        }

        protected override bool NextButtonEnabledByDefault
        {
            get { return true; }
        }

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            var key = ComponentStorage.GetValue<string>(SkipTo);

            foreach (var installableComponentBase in ComponentStorage.InstallableComponents)
            {
                if (installableComponentBase.Key == key)
                {
                    ComponentStorage.SetValue(SkipTo, "");

                    OnMoveTo(installableComponentBase);
                    break;
                }
            }
        }

        public static string SkipTo
        {
            get { return "SkipTo"; }
        }
    }
}
