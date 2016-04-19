using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MasterInstaller.Components
{
    public abstract class InstallableComponentBase : ComponentBase
    {
        public abstract string Key { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool IsTypical { get; }

        protected async Task<int> Install(ExecutableDetails ed)
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
                foreach (string unprocessedFile in ed.AdditionalFiles)
                {
                    var file = unprocessedFile.Replace("/", ".").Replace("\\", ".");

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
                    exitCode = await RunProcessUntilFinished(installerName, ed.Parameters, ed.RunAsAdministrator);
                    break;
                }
            }

            return exitCode;
        }

        public abstract Task<int> Install();

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
            
        }


        static string ConvertToArgString(string[] args)
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
                }
                else
                {
                    result += s;
                }
            }

            return result;
        }

        private async Task<int> RunProcessUntilFinished(string saveAsName, string[] args, bool runAsAdministrator)
        {
            try
            {
                Process process = new Process();

                string argsString = null;
                if (args != null && args.Length > 0)
                {
                    argsString = ConvertToArgString(args);
                }
                process.StartInfo.Arguments = argsString;
                //process.StartInfo.CreateNoWindow = true;
                // According to:
                // http://stackoverflow.com/questions/6141821/run-application-via-shortcut-using-process-start-c-sharp
                // This seems to cause problems on Win10:
                //process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = saveAsName;
                //process.StartInfo.RedirectStandardError = true;
                //process.StartInfo.RedirectStandardInput = true;
                //process.StartInfo.RedirectStandardOutput = true;
                if(runAsAdministrator)
                {
                    process.StartInfo.Verb = "runas";
                }
                process.Start();

                await Task.Run(() =>
                {

                    while (!process.HasExited)
                    {

                        System.Threading.Thread.Sleep(10);
                    }
                });

                //string errorString;

                //if (process.ExitCode != 0)
                //{
                //    errorString = process.StandardError.ReadToEnd() + 
                //        "\n\n" + process.StandardOutput.ReadToEnd();
                //}

                return process.ExitCode;
            }
            catch (Win32Exception e)
            {
                Debug.WriteLine(e.ToString());
                // probably cancelled - don't do anything.
                return 0;
            }
        }
    }
}
