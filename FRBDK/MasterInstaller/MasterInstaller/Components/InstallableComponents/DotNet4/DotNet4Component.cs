using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MasterInstaller.Components.MainComponents.SetupType;
using Microsoft.Win32;

namespace MasterInstaller.Components.InstallableComponents.DotNet4
{
    public class DotNet4Component : InstallableComponentBase
    {
        private const string ExecutableName = "dotNetFx40_Full_setup.exe";

        public DotNet4Component()
        {
            var control = new DefaultInstallControl { InstallName = Name, InstallDescription = Description };

            Control = control;
        }

        public override ComponentBase PreviousComponent
        {
            get { throw new System.NotImplementedException(); }
        }

        public override ComponentBase NextComponent
        {
            get { return ComponentStorage.DirectXComponent; }
        }

        public override string Key
        {
            get { return "DotNet4"; }
        }

        public override sealed string Name
        {
            get { return ".NET 4.0"; }
        }

        public override sealed string Description
        {
            get { return "The .Net 4.0 architecture is needed for XNA and the tools.\r\n\r\nRestart if asked to do so."; }
        }

        public override bool IsTypical
        {
            get { return true; }
        }

        public override void MovedToComponent()
        {
            base.MovedToComponent();

            int exitCode = -1;

            //Command line options - http://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#command_line_options
            //Return codes - http://msdn.microsoft.com/library/ee942965(v=VS.100).aspx#return_codes
            switch (ComponentStorage.GetValue<SetupTypeComponent.SetupType>(SetupTypeComponent.SetupTypeName))
            {
                case SetupTypeComponent.SetupType.Typical:
                    if (IsTypical)
                    {
                        if (!DotNet4IsInstalled)
                        {
                            exitCode =
                                Install(new ExecutableDetails
                                            {ExecutableName = ExecutableName, Parameters = new[] {"/passive", "/norestart"}});
                        }
                    }
                    break;
                case SetupTypeComponent.SetupType.Complete:
                    if (!DotNet4IsInstalled)
                    {
                        exitCode =
                            Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] { "/passive", "/norestart" } });
                    }
                    break;
                case SetupTypeComponent.SetupType.Custom:
                    if (ComponentStorage.GetValue<bool>(Key))
                    {
                        var install = true;

                        if (DotNet4IsInstalled)
                        {
                            if (MessageBox.Show(@".Net 4 is already installed.  Do you want to run the installer anyways?", @"Already Installed", MessageBoxButtons.YesNo) == DialogResult.No)
                                install = false;
                        }

                        if(install)
                            exitCode = Install(new ExecutableDetails { ExecutableName = ExecutableName, Parameters = new[] {"/passive", "/norestart"} });
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (exitCode)
            {
                case -1:    //Already Installed.
                    break;

                case 0:     //Installation completed successfully.
                    break;

                case 1602:  //The user canceled installation.
                    break;

                case 1603:  //A fatal error occurred during installation.
                    break;

                case 1641:  //A restart is required to complete the installation. This message indicates success.
                case 3010:  //A restart is required to complete the installation. This message indicates success.
                    Restarter.RestartComputerAndInstall((InstallableComponentBase) NextComponent);
                    break;

                case 5100:  //The user's computer does not meet system requirements.
                    break;

                case 5101:  //Internal state failure.
                    break;
            }

            Restarter.RestartComputerAndInstall((InstallableComponentBase) NextComponent);

            OnMoveToNext();
        }

        protected bool DotNet4IsInstalled
        {
            get
            {
                //var keyClient = Registry.LocalMachine.OpenSubKey(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client", false);
                var keyFull =   Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", false);

                //if (keyClient != null)
                //{
                //    int value = Convert.ToInt32(keyClient.GetValue("Install", 0, RegistryValueOptions.None));

                //    if (value > 0)
                //    {
                //        return true;
                //    }
                //}

                if (keyFull != null)
                {
                    int value = Convert.ToInt32(keyFull.GetValue("Install", 0, RegistryValueOptions.None));

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
