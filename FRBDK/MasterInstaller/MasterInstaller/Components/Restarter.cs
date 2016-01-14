using System.IO;
using System.Windows.Forms;
using MasterInstaller.Components.MainComponents.BeginInstall;
using MasterInstaller.Components.MainComponents.Introduction;
using Microsoft.Win32;
using OfficialPlugins.FrbdkUpdater;
using System.Management;

namespace MasterInstaller.Components
{
    public static class Restarter
    {
        public static string SavePath()
        {
            return FrbdkUpdaterSettings.UserApplicationData + @"FRBInstaller/";
        }

        public static void RestartComputerAndInstall(InstallableComponentBase component)
        {
            //Disabled this since it wasn't quite working right.
            return;

            //Set where to skip to when returning
            ComponentStorage.SetValue(IntroductionComponent.SkipTo, component.Key);
            
            //Set path to copy to
            var newExePath = SavePath() + System.AppDomain.CurrentDomain.FriendlyName;
            if (!Directory.Exists(SavePath()))
                Directory.CreateDirectory(SavePath());
            File.Copy(Application.ExecutablePath, newExePath, true);
            
            //Save current state
            ComponentStorage.Save();

            //Write exe to registry
//#if DEBUG
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                                                             true);
//#else
//            RegistryKey rk = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
//                                                             true);
//#endif

            rk.SetValue("FRBInstaller", newExePath);

            //Prompt if the user wants to restart now
            if(MessageBox.Show(
                @"Application needs to restart your computer to continue.  Click Yes to restart or no to end the program.  Install will resume when the system is restarted.",
                "Restart Needed", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RestartComputer();
            }

            System.Environment.Exit(0);
        }

        static void RestartComputer()
        {
            ManagementBaseObject mboShutdown = null;
            var mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();

            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            var mboShutdownParams =
                     mcWin32.GetMethodParameters("Win32Shutdown");

            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            mboShutdownParams["Flags"] = "2";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                                               mboShutdownParams, null);
            }
        }
    }
}
