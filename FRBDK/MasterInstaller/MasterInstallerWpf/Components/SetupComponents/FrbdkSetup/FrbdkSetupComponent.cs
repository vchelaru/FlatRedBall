using System;
using MasterInstaller.Components.Controls;
using MasterInstaller.Managers;

namespace MasterInstaller.Components.SetupComponents.FrbdkSetup
{
    public class FrbdkSetupComponent : ComponentBase
    {
        public const string Path = "FrbdkSetupPath";

        public FrbdkSetupComponent()
        {
            //Control = new FrbdkSetupControl();
        }

        protected override BasePage CreateControl()
        {
            throw new NotImplementedException();
        }

        //public override ComponentBase PreviousComponent
        //{
        //    get { return ComponentStorage.CustomSetupComponent; }
        //}

        //public override ComponentBase NextComponent
        //{
        //    get { return ComponentStorage.BeginInstallComponent; }
        //}

        //public override void MovedToComponent()
        //{
        //    base.MovedToComponent();

        //    switch(ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
        //    {
        //        case SetupTypeComponent.SetupType.Custom:
        //            break;

        //        default:
        //            OnMoveToNext();
        //            break;
        //    }
        //}

        //public override bool MovingNextFromComponent()
        //{
        //    switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
        //    {
        //        case SetupTypeComponent.SetupType.Custom:
        //            var control = Control as FrbdkSetupControl;
        //            ComponentStorage.SetValue(Path, control.lblPath.Text);

        //            break;

        //        default:
        //            ComponentStorage.SetValue(Path, FrbdkUpdaterManager.FrbdkInProgramFiles);
        //            break;
        //    }

        //    return base.MovingNextFromComponent();
        //}

        //protected override bool PreviousButtonEnabledByDefault
        //{
        //    get { return true; }
        //}

        //protected override bool NextButtonEnabledByDefault
        //{
        //    get { return true; }
        //}
    }
}
