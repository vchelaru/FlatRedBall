namespace MasterInstaller.Components.MainComponents.Completed
{
    public class CompletedComponent : ComponentBase
    {
        public CompletedComponent()
        {
            Control = new CompletedControl();
        }

        public override ComponentBase PreviousComponent
        {
            get { return null; }
        }

        public override ComponentBase NextComponent
        {
            get { return null; }
        }

        protected override bool NextButtonEnabledByDefault
        {
            get { return true; }
        }

        protected override string NextButtonString
        {
            get { return "Finish"; }
        }
    }
}
