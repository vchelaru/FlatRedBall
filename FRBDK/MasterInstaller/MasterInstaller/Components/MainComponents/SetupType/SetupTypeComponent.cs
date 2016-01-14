using System;

namespace MasterInstaller.Components.MainComponents.SetupType
{
    public class SetupTypeComponent : ComponentBase
    {
        public const string SetupTypeName = "SetupType";
        public enum SetupType
        {
            Typical,
            Complete,
            Custom
        }

        public SetupTypeComponent()
        {
            Control = new SetupTypeControl();
        }

        public override ComponentBase PreviousComponent
        {
            get { return ComponentStorage.IntroductionComponent; }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.CustomSetupComponent; }
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
            var control = Control as SetupTypeControl;

            if(control == null)
                throw new Exception("Control unexpected type.");

            if(control.rdoTypical.Checked)
            {
                ComponentStorage.SetValue(SetupTypeName, SetupType.Typical);
            }else if(control.rdoComplete.Checked)
            {
                ComponentStorage.SetValue(SetupTypeName, SetupType.Complete);
            }else
            {
                ComponentStorage.SetValue(SetupTypeName, SetupType.Custom);
            }

            return base.MovingNextFromComponent();
        }
    }
}
