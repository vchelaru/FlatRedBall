using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProjectCreator.Managers
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        public string ProjectLocation
        {
            get;
            private set;
        }

        public string DifferentNamespace
        {
            get;
            private set;
        }

        public string OpenedBy
        {
            get;
            private set;
        }

        public void ProcessCommandLineArguments()
        {
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("directory="))
                {
                    HandleDirectoryEquals(arg);
                }

                else if (arg.StartsWith("namespace="))
                {
                    HandleNamespaceEquals(arg);
                }
                else if (arg.ToLower().StartsWith("openedby="))
                {
                    HandleOpenedBy(arg);
                }
            }
        }


        private void HandleDirectoryEquals(string arg)
        {
            int lengthOfDirectory = "directory=".Length;

            string directory = arg.Substring(lengthOfDirectory, arg.Length - lengthOfDirectory);

            if (directory.StartsWith("\"") && directory.EndsWith("\""))
            {
                directory = directory.Substring(1, directory.Length - 2);
            }
            directory = directory.Replace("/", "\\");

            ProjectLocation = directory;
        }

        private void HandleNamespaceEquals(string arg)
        {
            int lengthOfNamespaceConstant = "namespace=".Length;

            string value = arg.Substring(lengthOfNamespaceConstant, arg.Length - lengthOfNamespaceConstant);

            DifferentNamespace = value;

        }

        private void HandleOpenedBy(string arg)
        {
            int indexOfEquals = arg.IndexOf('=');


            string value = arg.Substring(indexOfEquals + 1);

            OpenedBy = value;
        }
    }
}
