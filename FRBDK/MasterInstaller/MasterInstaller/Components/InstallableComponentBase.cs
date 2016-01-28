using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MasterInstaller.Components
{
    public abstract class InstallableComponentBase : ComponentBase
    {
        public abstract string Key { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool IsTypical { get; }

        protected int Install(ExecutableDetails ed)
        {
            var exitCode = 0;

            if (ed.ExtraLogic != null)
            {
                ed.ExtraLogic(null);
            }

            var installerName = ed.ExecutableName;

            var currentAssembly = Assembly.GetExecutingAssembly();
            // Get all imbedded resources
            var arrResources = currentAssembly.GetManifestResourceNames();

            if (ed.AdditionalFiles != null)
            {
                foreach (string file in ed.AdditionalFiles)
                {
                    foreach (string resourceName in arrResources)
                    {
                        if (resourceName.EndsWith("." + file))
                        {
                            ExtractFileFromAssembly(currentAssembly, resourceName, file);
                            break;
                        }
                    }
                }
            }

            foreach (string resourceName in arrResources)
            {
                if (resourceName.EndsWith("." + installerName))
                {
                    ExtractFileFromAssembly(currentAssembly, resourceName, installerName);
                    exitCode = RunProcessUntilFinished(installerName, ed.Parameters);
                    break;
                }
            }

            return exitCode;
        }

        private static void ExtractFileFromAssembly(Assembly currentAssembly, string resourceName, string saveAsName)
        {
            FileInfo fileInfoOutputFile = new FileInfo(saveAsName);
            //CHECK IF FILE EXISTS AND DO SOMETHING DEPENDING ON YOUR NEEDS
            if (fileInfoOutputFile.Exists)
            {
            }

            FileStream streamToOutputFile = fileInfoOutputFile.OpenWrite();
            //GET THE STREAM TO THE RESOURCES
            Stream streamToResourceFile =
                                currentAssembly.GetManifestResourceStream(resourceName);

            //---------------------------------
            //SAVE TO DISK OPERATION
            //---------------------------------
            const int size = 4096;
            byte[] bytes = new byte[4096];
            int numBytes;
            while ((numBytes = streamToResourceFile.Read(bytes, 0, size)) > 0)
            {
                streamToOutputFile.Write(bytes, 0, numBytes);
            }

            streamToOutputFile.Close();

            streamToResourceFile.Close();

            if (!File.Exists(saveAsName))
            {
                MessageBox.Show("Not found " + saveAsName);
            }
        }

        private int RunProcessUntilFinished(string saveAsName, string[] args)
        {
            try
            {
                Process process = new Process();

                string argsString = null;
                if (args != null && args.Length > 0)
                {
                    argsString = Program.ConvertToArgString(args);
                }
                process.StartInfo.Arguments = argsString;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = saveAsName;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                
                while (!process.HasExited)
                {

                    System.Threading.Thread.Sleep(10);
                    Application.DoEvents();
                }

                string errorString;

                if (process.ExitCode != 0)
                {
                    errorString = process.StandardError.ReadToEnd() + 
                        "\n\n" + process.StandardOutput.ReadToEnd();
                }

                return process.ExitCode;
            }
            catch (Win32Exception)
            {
                // probably cancelled - don't do anything.
                return 0;
            }
        }
    }
}
