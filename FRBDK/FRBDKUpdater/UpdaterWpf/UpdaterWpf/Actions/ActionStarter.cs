using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using ToolsUtilities;

namespace FRBDKUpdater.Actions
{
    public static class ActionStarter
    {
        static string UserApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\"; 

        private static bool IsAdministrator
        {
            get
            {
                var wi = WindowsIdentity.GetCurrent();
                var wp = new WindowsPrincipal(wi);

                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static bool StartAsAdmin(string[] args)
        {
            for (var i = 0; i < args.Count(); i++)
            {
                args[i] = "\"" + args[i] + "\"";
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location,
                                                 "\"" + UserApplicationData + "\" " + String.Join(" ", args))
                {
                    Verb = "runas"
                }
            };
            process.StartInfo.Arguments = process.StartInfo.Arguments.Replace("\\\"", "\\\\\"");
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 1;
        }

        public static bool CleanAndZipAction(string directoryToClear, string saveFile, string extractionPath)
        {
            try
            {
                Actions.CleanAndZipAction.CleanAndZip(UserApplicationData, directoryToClear, saveFile, extractionPath);
                return true;
            }
            //Need to run in admin mode
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log("Restarting Updater in admin mode...");
                return StartAsAdmin(new[] { "CleanAndZip", directoryToClear, saveFile, extractionPath, Messaging.ShowAlerts.ToString()});
            }
            catch (InvalidOperationException ioe)
            {
                Messaging.AlertError(@"Unzip failed.", ioe);
                Logger.LogAndShowImmediately(UserApplicationData, @"Unzip failed.\n\nException Info:\n" + ioe);
                return false;
            }
        }
    }
}
