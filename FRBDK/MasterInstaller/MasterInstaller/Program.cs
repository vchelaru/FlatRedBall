using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;
using System.Reflection;
using MasterInstaller.Components;
using Microsoft.Win32;

namespace MasterInstaller
{
    static class Program
    {
        private static bool IsAdministrator
        {
            get
            {
                var wi = WindowsIdentity.GetCurrent();
                var wp = new WindowsPrincipal(wi);

                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var args = Environment.GetCommandLineArgs();

#if !DEBUG
            if (!IsAdministrator)
            {
                args = args.Length > 1 ? SubArray(args, 1, args.Length - 1) : null;

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
                    {
                        Verb = "runas",
                        Arguments = ConvertToArgString(args)
                    }
                };
                try
                {
                    process.Start();
                }
                finally
                {
                    Environment.Exit(0);
                }
            }
            else
#endif
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                Application.Run(new InstallForm());
            }
        }

        private static void SetKey(string keyName, string value)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                                                             true);
            rk.SetValue(keyName, value);
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString(), @"Terminal Error Occured");
            Environment.Exit(0);
        }

        public static string ConvertToArgString(string[] args)
        {
            if (args == null) return "";

            var result = "";
            var isFirst = true;

            foreach (var s in args)
            {
                if (isFirst)
                    isFirst = false;
                else
                    result += " ";

                if (s.Contains(" "))
                {
                    result += "\"" + s + "\"";
                }else
                {
                    result += s;
                }
            }

            return result;
        }
    }
}
