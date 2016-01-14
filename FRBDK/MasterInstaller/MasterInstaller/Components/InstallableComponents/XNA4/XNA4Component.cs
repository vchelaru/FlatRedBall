using System;
using System.Windows.Forms;
using MasterInstaller.Components.MainComponents.SetupType;
using Microsoft.Win32;

namespace MasterInstaller.Components.InstallableComponents.XNA4
{
    public class XNA4Component : InstallableComponentBase
    {
        private const string ExecutableName = "WindowsPhoneToolsWebInstaller.exe";

        public XNA4Component()
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
            get { return ComponentStorage.FrbdkComponent; }
        }

        public override string Key
        {
            get { return "XNA4"; }
        }

        public override sealed string Name
        {
            get { return "XNA Game Studio 4.0"; }
        }

        public override sealed string Description
        {
            get { return "The latest version of XNA on which most game development is done."; }
        }

        public override bool IsTypical
        {
            get { return true; }
        }

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            var exitCode = -1;

            switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
            {
                case SetupTypeComponent.SetupType.Typical:
                    if (IsTypical)
                    {
                        if (!Xna4IsInstalled)
                        {
                            exitCode = Install(new ExecutableDetails {ExecutableName = ExecutableName, Parameters = new[] {"/norestart"}});
                        }
                    }
                    break;
                case SetupTypeComponent.SetupType.Complete:
                    if (!Xna4IsInstalled)
                    {
                        exitCode = Install(new ExecutableDetails {ExecutableName = ExecutableName, Parameters = new[] {"/norestart"}});
                    }
                    break;
                case SetupTypeComponent.SetupType.Custom:
                    if (ComponentStorage.GetValue<bool>(Key))
                    {
                        var install = true;

                        if (Xna4IsInstalled)
                        {
                            if (MessageBox.Show(@"XNA 4 is already installed.  Do you want to run the installer anyways?", @"Already Installed", MessageBoxButtons.YesNo) == DialogResult.No)
                                install = false;
                        }

                        if (install)
                            exitCode = Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/norestart" } });
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (exitCode)
            {
                case -1:    //Didn't install
                    break;
                case 3010: //Restart required
                    Restarter.RestartComputerAndInstall((InstallableComponentBase)NextComponent);
                    break;
            }

            OnMoveToNext();
        }

        protected bool Xna4IsInstalled
        {
            get
            {
                var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\XNA\Game Studio\v4.0", false);
                var key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\XNA\Game Studio\v4.0", false);

                if (key32 != null)
                {
                    int value = Convert.ToInt32(key32.GetValue("Installed", 0, RegistryValueOptions.None));

                    if (value > 0)
                    {
                        return true;
                    }
                }

                if (key64 != null)
                {
                    int value = Convert.ToInt32(key64.GetValue("Installed", 0, RegistryValueOptions.None));

                    if (value > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
